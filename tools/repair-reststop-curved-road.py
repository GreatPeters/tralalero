"""Bend the accepted TRELLIS R01 source into a continuous, constant-width R02 road."""
import argparse
import json
import math
from pathlib import Path
import runpy
import shutil
import sys

import bpy
from mathutils import Vector
from reststop_mesh_cleanup import clean_degenerate_preserving_normals

root = Path(__file__).resolve().parents[1]
out = root / 'outputs/reststop-production-2026-09-24'
source = out / 'assets/R01_136aa76ae2/stages/m1_t1/model.glb'
parser = argparse.ArgumentParser()
parser.add_argument('--revision', type=int, required=True)
args = parser.parse_args(sys.argv[sys.argv.index('--') + 1:])
assert args.revision >= 2
folder = out / ('manual/R02-r' + str(args.revision)) / 'source'
assert not folder.exists()
assert json.loads((source.parent / 'quality/main-agent-review.json').read_text())['verdict'] == 'pass'
folder.mkdir(parents=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=str(source))
meshes = [obj for obj in bpy.context.scene.objects if obj.type == 'MESH']
assert len(meshes) == 1
obj = meshes[0]
bpy.context.view_layer.objects.active = obj
obj.select_set(True)
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
mesh = obj.data
points = [vertex.co.copy() for vertex in mesh.vertices]
normals = [normal.vector.copy() for normal in mesh.corner_normals]
lo = Vector(tuple(min(point[i] for point in points) for i in range(3)))
hi = Vector(tuple(max(point[i] for point in points) for i in range(3)))
long_axis = 0 if hi.x - lo.x > hi.y - lo.y else 1
width_axis = 1 - long_axis
length = hi[long_axis] - lo[long_axis]
width = hi[width_axis] - lo[width_axis]
angle = math.radians(60)
radius = length / angle
assert width < radius
center = (lo + hi) * .5
angles, stretches = [], []
for vertex, point in zip(mesh.vertices, points):
    u, v = point[long_axis] - center[long_axis], point[width_axis] - center[width_axis]
    theta = u / radius
    angles.append(theta)
    stretches.append(1 + v / radius)
    vertex.co[long_axis] = (radius + v) * math.sin(theta)
    vertex.co[width_axis] = (radius + v) * math.cos(theta) - radius
    vertex.co.z = point.z - lo.z
mesh.update()
transformed = []
for loop, normal in zip(mesh.loops, normals):
    theta = angles[loop.vertex_index]
    scale = stretches[loop.vertex_index]
    nu, nv = normal[long_axis] / scale, normal[width_axis]
    value = normal.copy()
    value[long_axis] = nu * math.cos(theta) + nv * math.sin(theta)
    value[width_axis] = -nu * math.sin(theta) + nv * math.cos(theta)
    transformed.append(value.normalized())
mesh.normals_split_custom_set(transformed)
cleanup = clean_degenerate_preserving_normals(mesh)
bpy.ops.export_scene.gltf(filepath=str(folder / 'model.glb'), use_selection=True, export_format='GLB')
bpy.ops.file.pack_all()
bpy.ops.wm.save_as_mainfile(filepath=str(folder / 'model.blend'))
shutil.copy2(source, folder / 'original-trellis-source.glb')
(folder / 'repair.json').write_text(json.dumps({'source_asset': 'R01', 'source': str(source),
    'method': 'Analytic planar bend of accepted TRELLIS road geometry; UV and embedded PBR retained, corner normals transformed with inverse-transpose Jacobian',
    'angle_degrees': 60, 'centerline_length': length, 'constant_width': width, 'centerline_radius': radius,
    'cleanup': cleanup, 'extra_ai_generations': 0, 'original_R02_shape_attempts': 3,
    'note': '60-degree bend is the local repair choice; reference does not specify an exact world-space radius.'}, indent=2), encoding='utf8')
script = root / 'tools/reststop-production-review-render.py'
sys.argv = [str(script), '--', str(folder)]
runpy.run_path(str(script), run_name='__main__')
print(json.dumps({'folder': str(folder), 'visual_review_required': True}), flush=True)
