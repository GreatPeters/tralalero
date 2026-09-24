"""Give only P01's clear shield panes an alpha-blended material.

Preserve geometry, UVs, dark rim/band and protruding rear grip.
"""
import argparse
from array import array
import json
import runpy
import shutil
import struct
import sys
from pathlib import Path

import bpy
from mathutils import Vector
from mathutils.bvhtree import BVHTree
from mathutils.geometry import barycentric_transform

ROOT = Path(__file__).resolve().parents[1]
parser = argparse.ArgumentParser()
mode = parser.add_mutually_exclusive_group(required=True)
mode.add_argument('--source-glb')
mode.add_argument('--source-blend')
parser.add_argument('--output', required=True)
args = parser.parse_args(sys.argv[sys.argv.index('--') + 1:])
source = Path(args.source_glb or args.source_blend).resolve()
output = Path(args.output).resolve()
assert any(part.startswith('P01') for part in source.parts)
assert not output.exists(), 'Preserve earlier material revisions'
output.mkdir(parents=True)
if args.source_blend:
    bpy.ops.wm.open_mainfile(filepath=str(source))
    body = bpy.data.objects['Asset_Optimized']
else:
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.gltf(filepath=str(source))
    meshes = [obj for obj in bpy.context.scene.objects if obj.type == 'MESH']
    assert len(meshes) == 1
    body = meshes[0]
bpy.ops.object.select_all(action='DESELECT')
body.hide_set(False)
body.select_set(True)
bpy.context.view_layer.objects.active = body
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
before = [tuple(vertex.co) for vertex in body.data.vertices]
body.data.calc_loop_triangles()
triangles = list(body.data.loop_triangles)
vertices = [body.matrix_world @ vertex.co for vertex in body.data.vertices]
tree = BVHTree.FromPolygons(vertices, [tuple(t.vertices) for t in triangles], all_triangles=True)
uv = body.data.uv_layers.active.data
images = {}
for index, material in enumerate(body.data.materials):
    shader = next(node for node in material.node_tree.nodes if node.type == 'BSDF_PRINCIPLED')
    node = shader.inputs['Base Color'].links[0].from_node
    assert node.type == 'TEX_IMAGE'
    image = node.image
    pixels = array('f', [0.]) * len(image.pixels)
    image.pixels.foreach_get(pixels)
    images[index] = (image.size[0], image.size[1], pixels)
lo = Vector(tuple(min(vertex[i] for vertex in vertices) for i in range(3)))
hi = Vector(tuple(max(vertex[i] for vertex in vertices) for i in range(3)))
dimensions = hi - lo
center = (lo + hi) / 2
assert dimensions.z > dimensions.x * 1.5 and dimensions.x > dimensions.y * 2
def color_at(point, triangle_index):
    triangle = triangles[triangle_index]
    coordinates = [uv[index].uv for index in triangle.loops]
    mapped = barycentric_transform(point, *(vertices[index] for index in triangle.vertices),
                                   *(Vector((value.x, value.y, 0)) for value in coordinates))
    width, height, pixels = images[triangle.material_index]
    x = min(width-1, int((mapped.x % 1) * width))
    y = min(height-1, int((mapped.y % 1) * height))
    start = (y * width + x) * 4
    return tuple(pixels[start+i] for i in range(3))
def front_hit(x, z):
    return tree.ray_cast(Vector((x, lo.y - dimensions.length, z)), Vector((0, 1, 0)))
dark_rows = []
for index in range(201):
    fraction = .25 + .5 * index / 200
    z = lo.z + dimensions.z * fraction
    votes = 0
    for offset in (-.12, 0, .12):
        point, normal, triangle_index, distance = front_hit(center.x + dimensions.x * offset, z)
        if point is not None and max(color_at(point, triangle_index)) < .16:
            votes += 1
    dark_rows.append(votes >= 2)
runs = []
start = None
for index, dark in enumerate(dark_rows + [False]):
    if dark and start is None:
        start = index
    elif not dark and start is not None:
        runs.append((start, index - 1))
        start = None
band = max(runs, key=lambda pair: pair[1] - pair[0])
assert 20 < band[1] - band[0] < 150, band
band_lo = lo.z + dimensions.z * (.25 + .5 * band[0] / 200 - .008)
band_hi = lo.z + dimensions.z * (.25 + .5 * band[1] / 200 + .008)
glass = bpy.data.materials.new('Clear polycarbonate panes')
glass.use_nodes = True
glass.diffuse_color = (.78, .84, .88, .18)
glass.use_backface_culling = False
if hasattr(glass, 'surface_render_method'):
    glass.surface_render_method = 'DITHERED'
shader = glass.node_tree.nodes.get('Principled BSDF')
shader.inputs['Base Color'].default_value = (.78, .84, .88, 1)
shader.inputs['Metallic'].default_value = 0
shader.inputs['Roughness'].default_value = .16
shader.inputs['IOR'].default_value = 1.49
shader.inputs['Alpha'].default_value = .18
body.data.materials.append(glass)
glass_index = len(body.data.materials) - 1
selected = []
for polygon in body.data.polygons:
    c = body.matrix_world @ polygon.center
    if abs(c.x-center.x) > dimensions.x * .45 or not (lo.z+dimensions.z*.04 < c.z < hi.z-dimensions.z*.04):
        continue
    if band_lo <= c.z <= band_hi or abs(polygon.normal.y) < .55:
        continue
    point, normal, triangle_index, distance = front_hit(c.x, c.z)
    if point is None or not (-dimensions.z*.001 <= c.y-point.y <= dimensions.z*.028):
        continue
    if max(color_at(point, triangle_index)) < .10:
        continue
    selected.append(polygon.index)
assert 100 < len(selected) < len(body.data.polygons) * .9, len(selected)
for index in selected:
    body.data.polygons[index].material_index = glass_index
assert before == [tuple(vertex.co) for vertex in body.data.vertices]
report = {'source': str(source), 'method': 'Source-color central-band detection and front-surface depth filtering; assign clear material only to upper/lower panes',
          'pane_alpha': .18, 'band_z_range': [band_lo, band_hi], 'pane_faces': len(selected), 'face_indices': selected,
          'vertex_positions_and_uvs_unchanged': True, 'rear_grip_depth_excluded': True,
          'material_note': 'Alpha-blended clear-panel approximation; dark rim, reinforcement and grip stay opaque. Preserve separate materials.'}
(output/'repair.json').write_text(json.dumps(report, indent=2), encoding='utf8')
bpy.ops.export_scene.gltf(filepath=str(output/'model.glb'), use_selection=True, export_format='GLB')
bpy.ops.export_scene.fbx(filepath=str(output/'model.fbx'), use_selection=True, object_types={'MESH'},
                         axis_forward='-Z', axis_up='Y', path_mode='COPY', embed_textures=True, add_leaf_bones=False)
bpy.ops.file.pack_all()
bpy.ops.wm.save_as_mainfile(filepath=str(output/'model.blend'))
raw = (output/'model.glb').read_bytes()
length, kind = struct.unpack_from('<II', raw, 12)
gltf = json.loads(raw[20:20+length])
assert any(m.get('alphaMode') == 'BLEND' and m.get('pbrMetallicRoughness', {}).get('baseColorFactor', [1,1,1,1])[3] < .3 for m in gltf['materials'])
if (source.parent/'textures').is_dir():
    shutil.copytree(source.parent/'textures', output/'textures')
if (source.parent/'trellis_source.glb').exists():
    shutil.copy2(source.parent/'trellis_source.glb', output/'trellis_source.glb')
script = ROOT/'tools/reststop-production-review-render.py'
sys.argv = [str(script), '--', str(output)]
runpy.run_path(str(script), run_name='__main__')
shutil.copy2(output/'quality/front.png', output/'preview.png')
print(json.dumps({k:v for k,v in report.items() if k!='face_indices'}), flush=True)
