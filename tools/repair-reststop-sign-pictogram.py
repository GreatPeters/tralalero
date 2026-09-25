"""Correct T09's central embossed symbol using the retained frame and ink."""
import argparse
from array import array
import hashlib
import json
from pathlib import Path
import runpy
import shutil
import sys

import bpy
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'outputs/reststop-production-2026-09-24'
SOURCE_SHA256 = '5321b48a227cb991671a46b3a8b8fec3e074ca370ca09584b8da1d847b9c544f'
PAPER_FRONT_GLTF_Z = .03981133532145807
GLYPH_FRONT_GLTF_Z = .04387168679386377
INK_UV = (.9291365146636963, .4651035666465759)
parser = argparse.ArgumentParser()
parser.add_argument('--source', required=True)
parser.add_argument('--output', required=True)
parser.add_argument('--center-x', type=float, default=.498)
parser.add_argument('--top-height', type=float, default=.66)
parser.add_argument('--hem-height', type=float, default=.40)
parser.add_argument('--top-width', type=float, default=.045)
parser.add_argument('--hem-width', type=float, default=.082)
args = parser.parse_args(sys.argv[sys.argv.index('--') + 1:])
source, output = Path(args.source).resolve(), Path(args.output).resolve()
assert source.is_relative_to(OUT / 'assets') and 'T09_' in str(source)
assert hashlib.sha256(source.read_bytes()).hexdigest() == SOURCE_SHA256
assert output.is_relative_to(OUT / 'manual') and not output.exists()
assert .35 < args.center_x < .65 and .25 < args.hem_height < args.top_height < .72
assert 0 < args.top_width < args.hem_width < .12
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=str(source))
meshes = [obj for obj in bpy.context.scene.objects if obj.type == 'MESH']
assert len(meshes) == 1
original = meshes[0]
assert len(original.data.materials) == 1 and original.data.uv_layers.active is not None
positions = [tuple(vertex.co) for vertex in original.data.vertices]
uv_values = array('f', [0.]) * (len(original.data.uv_layers.active.data) * 2)
original.data.uv_layers.active.data.foreach_get('uv', uv_values)
points = [original.matrix_world @ Vector(corner) for corner in original.bound_box]
lo = Vector(tuple(min(point[i] for point in points) for i in range(3)))
hi = Vector(tuple(max(point[i] for point in points) for i in range(3)))
width, height = hi.x - lo.x, hi.z - lo.z
assert abs(width - .99999) < .0001 and abs(height - .41219014) < .0001
# GLB +Z is the sign front; standard import maps it to Blender -Y.
front = GLYPH_FRONT_GLTF_Z + height * .001
back = PAPER_FRONT_GLTF_Z - height * .005
outline = [(args.center_x - args.top_width / 2, args.top_height),
           (args.center_x - args.hem_width / 2, args.hem_height),
           (args.center_x + args.hem_width / 2, args.hem_height),
           (args.center_x + args.top_width / 2, args.top_height)]
vertices = [(lo.x + x * width, -depth, lo.z + z * height)
            for depth in (front, back) for x, z in outline]
faces = [(0, 1, 2, 3), (7, 6, 5, 4)]
faces += [(i, i + 4, (i + 1) % 4 + 4, (i + 1) % 4) for i in range(4)]
mesh = bpy.data.meshes.new('T09_reference_skirt_mesh')
mesh.from_pydata(vertices, [], faces)
mesh.update()
skirt = bpy.data.objects.new('T09_Central_Skirt_Correction', mesh)
bpy.context.collection.objects.link(skirt)
material = original.data.materials[0].copy()
material.name = 'T09_Original_Ink_On_Corrected_Symbol'
shader = next(node for node in material.node_tree.nodes if node.type == 'BSDF_PRINCIPLED')
for link in list(shader.inputs['Normal'].links):
    material.node_tree.links.remove(link)
mesh.materials.append(material)
bpy.ops.object.select_all(action='DESELECT')
skirt.select_set(True)
bpy.context.view_layer.objects.active = skirt
bevel = skirt.modifiers.new('Small manufactured pictogram edge', 'BEVEL')
bevel.width = height * .0008
bevel.segments = 3
bpy.ops.object.modifier_apply(modifier=bevel.name)
skirt.data.polygons.foreach_set('use_smooth', [True] * len(skirt.data.polygons))
skirt.data.update()
weighted = skirt.modifiers.new('Flat pictogram face normals', 'WEIGHTED_NORMAL')
weighted.mode = 'FACE_AREA_WITH_ANGLE'
weighted.keep_sharp = True
bpy.ops.object.modifier_apply(modifier=weighted.name)
layer = skirt.data.uv_layers.new(name=original.data.uv_layers.active.name)
for loop in layer.data:
    loop.uv = INK_UV
assert positions == [tuple(vertex.co) for vertex in original.data.vertices]
after_uv = array('f', [0.]) * len(uv_values)
original.data.uv_layers.active.data.foreach_get('uv', after_uv)
assert uv_values == after_uv
for obj in (original, skirt):
    obj.data.calc_loop_triangles()
triangles = sum(len(obj.data.loop_triangles) for obj in (original, skirt))
assert 0 < triangles <= 300000
output.mkdir(parents=True)
original.select_set(True)
bpy.context.view_layer.objects.active = original
bpy.ops.export_scene.gltf(filepath=str(output / 'model.glb'), use_selection=True, export_format='GLB')
bpy.ops.file.pack_all()
bpy.ops.wm.save_as_mainfile(filepath=str(output / 'model.blend'))
shutil.copy2(source, output / 'original-trellis-source.glb')
(output / 'repair.json').write_text(json.dumps({
    'source': str(source), 'source_sha256': SOURCE_SHA256,
    'method': 'Add a supported shallow flared skirt to the central symbol; sample the existing ink and preserve the original frame, other icons, geometry and UVs',
    'normalized_outline': outline, 'front_distance': front, 'back_distance': back,
    'ink_uv': INK_UV, 'original_geometry_and_uvs_unchanged': True,
    'added_triangles': len(skirt.data.loop_triangles), 'triangles': triangles,
    'extra_ai_requests': 0, 'canonical_reduction_attempts': 0,
    'visual_readability_and_attachment_review_required': True
}, indent=2), encoding='utf8')
script = ROOT / 'tools/reststop-production-review-render.py'
sys.argv = [str(script), '--', str(output)]
runpy.run_path(str(script), run_name='__main__')
shutil.copy2(output / 'quality/front.png', output / 'preview.png')
print(json.dumps({'folder': str(output), 'triangles': triangles,
                  'visual_review_required': True}), flush=True)
