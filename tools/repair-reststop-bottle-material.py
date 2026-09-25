"""Prepare an S10 high source with selective PET opacity; never visual-approve."""
import argparse
from array import array
import hashlib
import json
from pathlib import Path
import runpy
import shutil
import struct
import sys

import bpy
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'outputs/reststop-production-2026-09-24'
SOURCE_SHA256 = 'b2ea0a77c4fb0d688dd7ea334113d1e5da57c127121926b8c245c394532d026f'
parser = argparse.ArgumentParser()
parser.add_argument('--source', required=True)
parser.add_argument('--output', required=True)
parser.add_argument('--label-bottom', type=float, default=.372)
parser.add_argument('--label-top', type=float, default=.625)
parser.add_argument('--cap-bottom', type=float, default=.887)
parser.add_argument('--pet-alpha', type=float, default=.28)
args = parser.parse_args(sys.argv[sys.argv.index('--') + 1:])
source, output = Path(args.source).resolve(), Path(args.output).resolve()
assert source.is_relative_to(OUT / 'assets') and 'S10_' in str(source)
assert hashlib.sha256(source.read_bytes()).hexdigest() == SOURCE_SHA256, 'Inspect a changed source before reusing these regions'
assert output.is_relative_to(OUT / 'manual') and not output.exists(), 'Use a new local revision'
assert .1 < args.label_bottom < args.label_top < args.cap_bottom < 1
assert .15 <= args.pet_alpha <= .5
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=str(source))
meshes = [obj for obj in bpy.context.scene.objects if obj.type == 'MESH']
assert len(meshes) == 1
obj = meshes[0]
mesh = obj.data
assert len(mesh.materials) == 1 and mesh.uv_layers


def geometry_uv_signature():
    positions = array('f', [0.]) * (len(mesh.vertices) * 3)
    mesh.vertices.foreach_get('co', positions)
    uvs = {}
    for layer in mesh.uv_layers:
        values = array('f', [0.]) * (len(layer.data) * 2)
        layer.data.foreach_get('uv', values)
        uvs[layer.name] = hashlib.sha256(values.tobytes()).hexdigest()
    mesh.calc_loop_triangles()
    return {'positions': hashlib.sha256(positions.tobytes()).hexdigest(),
            'uvs': uvs, 'triangles': len(mesh.loop_triangles)}


before = geometry_uv_signature()
points = [obj.matrix_world @ Vector(corner) for corner in obj.bound_box]
lo = Vector(tuple(min(point[i] for point in points) for i in range(3)))
hi = Vector(tuple(max(point[i] for point in points) for i in range(3)))
height = hi.z - lo.z
assert height > max(hi.x - lo.x, hi.y - lo.y) * 1.5, 'Expected upright S10 bottle'
original_material = mesh.materials[0]
clear = original_material.copy()
clear.name = 'S10_Clear_PET'
shader = next(node for node in clear.node_tree.nodes if node.type == 'BSDF_PRINCIPLED')
for socket_name, value in (('Metallic', 0.), ('Alpha', args.pet_alpha)):
    socket = shader.inputs[socket_name]
    for link in list(socket.links):
        clear.node_tree.links.remove(link)
    socket.default_value = value
if hasattr(clear, 'surface_render_method'):
    clear.surface_render_method = 'DITHERED'
clear.use_backface_culling = False
mesh.materials.append(clear)
counts = {'label': 0, 'cap': 0, 'pet': 0}
for polygon in mesh.polygons:
    fraction = ((obj.matrix_world @ polygon.center).z - lo.z) / height
    region = ('cap' if fraction >= args.cap_bottom else 'label'
              if args.label_bottom <= fraction <= args.label_top else 'pet')
    polygon.material_index = 1 if region == 'pet' else 0
    counts[region] += 1
assert all(count > 100 for count in counts.values()), counts
assert geometry_uv_signature() == before, 'Material correction must preserve geometry and UVs'
output.mkdir(parents=True)
bpy.ops.object.select_all(action='DESELECT')
obj.select_set(True)
bpy.context.view_layer.objects.active = obj
bpy.ops.export_scene.gltf(filepath=str(output / 'model.glb'), use_selection=True, export_format='GLB')
bpy.ops.file.pack_all()
bpy.ops.wm.save_as_mainfile(filepath=str(output / 'model.blend'))
with (output / 'model.glb').open('rb') as stream:
    stream.read(12)
    length, _ = struct.unpack('<II', stream.read(8))
    exported = json.loads(stream.read(length))
modes = {material.get('alphaMode', 'OPAQUE') for material in exported['materials']}
assert 'BLEND' in modes and 'OPAQUE' in modes, modes
shutil.copy2(source, output / 'original-trellis-source.glb')
(output / 'repair.json').write_text(json.dumps({
    'source': str(source), 'source_sha256': SOURCE_SHA256,
    'method': 'Keep original opaque cap/label material; change only exposed PET metallic response and alpha',
    'normalized_height_regions': {'label_bottom': args.label_bottom, 'label_top': args.label_top,
                                 'cap_bottom': args.cap_bottom},
    'pet_alpha': args.pet_alpha, 'region_face_counts': counts,
    'geometry_uv_signature': before, 'geometry_and_uvs_unchanged': True,
    'bounds_min': list(lo), 'bounds_max': list(hi),
    'original_images_unmodified': True, 'extra_ai_requests': 0,
    'canonical_reduction_attempts': 0, 'visual_review_required': True,
    'material_note': 'Alpha-blended PET approximation; cap and paper label remain opaque. Verify final GLB/FBX on contrasting backgrounds.'
}, indent=2), encoding='utf8')
script = ROOT / 'tools/reststop-production-review-render.py'
sys.argv = [str(script), '--', str(output)]
runpy.run_path(str(script), run_name='__main__')
shutil.copy2(output / 'quality/hero.png', output / 'preview.png')
print(json.dumps({'folder': str(output), 'faces_by_region': counts, 'visual_review_required': True}), flush=True)
