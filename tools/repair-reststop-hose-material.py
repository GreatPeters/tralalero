"""Repair G02 hose color bleed using nearest approved-source color classification.

Keep the canonical smooth low mesh, UVs and all non-hose PBR connections intact.
"""
import argparse
from array import array
import json
import runpy
import shutil
import sys
from pathlib import Path

import bpy
from mathutils import Vector
from mathutils.bvhtree import BVHTree
from mathutils.geometry import barycentric_transform

ROOT = Path(__file__).resolve().parents[1]
parser = argparse.ArgumentParser()
parser.add_argument('--source-blend', required=True)
parser.add_argument('--output', required=True)
args = parser.parse_args(sys.argv[sys.argv.index('--') + 1:])
source = Path(args.source_blend).resolve()
output = Path(args.output).resolve()
assert 'G02_bc883ec0a0' in source.parts, 'This inspected-region correction is specific to G02'
assert not output.exists(), 'Choose a new correction revision'
output.mkdir(parents=True)
bpy.ops.wm.open_mainfile(filepath=str(source))
high = bpy.data.objects['Detailed_Source']
low = bpy.data.objects['Asset_Optimized']
high.data.calc_loop_triangles()
triangles = list(high.data.loop_triangles)
vertices = [high.matrix_world @ vertex.co for vertex in high.data.vertices]
tree = BVHTree.FromPolygons(vertices, [tuple(t.vertices) for t in triangles], all_triangles=True)
uv = high.data.uv_layers.active.data
images = {}
for index, material in enumerate(high.data.materials):
    if material is None:
        continue
    shader = next(node for node in material.node_tree.nodes if node.type == 'BSDF_PRINCIPLED')
    link = shader.inputs['Base Color'].links[0]
    assert link.from_node.type == 'TEX_IMAGE'
    image = link.from_node.image
    pixels = array('f', [0.]) * len(image.pixels)
    image.pixels.foreach_get(pixels)
    images[index] = (image.size[0], image.size[1], pixels)
lo = Vector(tuple(min(v[i] for v in vertices) for i in range(3)))
hi = Vector(tuple(max(v[i] for v in vertices) for i in range(3)))
center = (lo + hi) / 2
dimensions = hi - lo
def texture_color(material_index, u, v):
    width, height, pixels = images[material_index]
    x = min(width - 1, int((u % 1) * width))
    y = min(height - 1, int((v % 1) * height))
    start = (y * width + x) * 4
    return tuple(pixels[start + i] for i in range(3))
green_minima = []
for triangle in triangles:
    coordinates = [uv[index].uv for index in triangle.loops]
    u = sum(value.x for value in coordinates) / 3
    v = sum(value.y for value in coordinates) / 3
    red, green, blue = texture_color(triangle.material_index, u, v)
    if green > .15 and green > red * 1.5 and green > blue * 1.5:
        green_minima.append(min(vertices[index].z for index in triangle.vertices))
assert len(green_minima) > 30, 'Green nozzle pigment must be identified before region repair'
nozzle_min_z = min(green_minima)
hose_top_z = min(lo.z + dimensions.z * .52, nozzle_min_z - dimensions.z * .01)
def source_color(position):
    point, normal, triangle_index, distance = tree.find_nearest(position)
    if point is None or distance > dimensions.length * .008:
        return None
    triangle = triangles[triangle_index]
    coordinates = [uv[index].uv for index in triangle.loops]
    mapped = barycentric_transform(point, *(vertices[index] for index in triangle.vertices),
                                   *(Vector((value.x, value.y, 0)) for value in coordinates))
    return texture_color(triangle.material_index, mapped.x, mapped.y)
rubber = bpy.data.materials.new('Corrected black rubber hoses')
rubber.use_nodes = True
shader = rubber.node_tree.nodes.get('Principled BSDF')
shader.inputs['Base Color'].default_value = (.006, .008, .010, 1)
shader.inputs['Metallic'].default_value = 0
shader.inputs['Roughness'].default_value = .58
low.data.materials.append(rubber)
rubber_index = len(low.data.materials) - 1
before = [tuple(vertex.co) for vertex in low.data.vertices]
selected = []
candidates = 0
for polygon in low.data.polygons:
    c = low.matrix_world @ polygon.center
    if not (lo.z + dimensions.z * .035 < c.z < hose_top_z):
        continue
    if abs(c.y - center.y) < dimensions.y * .28 or abs(c.x - center.x) < dimensions.x * .15:
        continue
    candidates += 1
    points = [low.matrix_world @ low.data.vertices[index].co for index in polygon.vertices]
    sample_points = [c, points[0] * .6 + points[1] * .2 + points[2] * .2,
                     points[0] * .2 + points[1] * .6 + points[2] * .2]
    colors = [source_color(point) for point in sample_points]
    # Dark green/red nozzle pigments must not become black rubber.
    colored = any(color is not None and max(color) > .03 and (max(color)-min(color))/max(color) > .60 for color in colors)
    dark_votes = sum(color is not None and max(color) < .18 for color in colors)
    if dark_votes >= 2 and not colored:
        polygon.material_index = rubber_index
        selected.append(polygon.index)
assert 100 <= len(selected) < len(low.data.polygons) * .40, (len(selected), candidates)
assert before == [tuple(vertex.co) for vertex in low.data.vertices]
report = {'source_canonical_blend': str(source), 'method': 'Classify lower outer hose faces by nearest high-source BaseColor, then assign uniform dark nonmetallic rubber only to that region',
          'candidate_faces': candidates, 'reassigned_faces': len(selected), 'face_indices': selected,
          'source_color_max_threshold': .18, 'exclude_saturated_source_colors': True, 'required_dark_votes': 2, 'samples_per_face': 3,
          'source_green_nozzle_min_z': nozzle_min_z, 'hose_region_top_z': hose_top_z,
          'vertex_positions_and_uvs_unchanged': True, 'other_materials_unchanged': True,
          'original_automatic_attempts_preserved': 2, 'extra_ai_requests': 0,
          'material_note': 'Retain embedded materials. The hose material intentionally does not use the contaminated baked maps.'}
(output / 'repair.json').write_text(json.dumps(report, indent=2), encoding='utf8')
bpy.ops.object.select_all(action='DESELECT')
low.hide_set(False)
low.select_set(True)
bpy.context.view_layer.objects.active = low
bpy.ops.export_scene.gltf(filepath=str(output / 'model.glb'), use_selection=True, export_format='GLB')
bpy.ops.export_scene.fbx(filepath=str(output / 'model.fbx'), use_selection=True, object_types={'MESH'},
                         axis_forward='-Z', axis_up='Y', path_mode='COPY', embed_textures=True, add_leaf_bones=False)
bpy.ops.file.pack_all()
bpy.ops.wm.save_as_mainfile(filepath=str(output / 'model.blend'))
shutil.copytree(source.parent / 'textures', output / 'textures')
shutil.copy2(source.parent / 'trellis_source.glb', output / 'trellis_source.glb')
script = ROOT / 'tools/reststop-production-review-render.py'
sys.argv = [str(script), '--', str(output)]
runpy.run_path(str(script), run_name='__main__')
shutil.copy2(output / 'quality/hero.png', output / 'preview.png')
print(json.dumps({k: v for k, v in report.items() if k != 'face_indices'}), flush=True)
