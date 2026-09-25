"""Project T01's inspected front wood mapping onto the rear infill only."""
import argparse
from array import array
import hashlib
import json
import math
from pathlib import Path
import runpy
import shutil
import sys

import bpy
from mathutils import Vector
from mathutils.bvhtree import BVHTree
from mathutils.geometry import barycentric_transform

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'outputs/reststop-production-2026-09-24'
SOURCE_SHA256 = '359bb1f282f487c0203837d3235f11cfdb6f3398f02678caea6b2409a5035aa0'
parser = argparse.ArgumentParser()
parser.add_argument('--source', required=True)
parser.add_argument('--output', required=True)
parser.add_argument('--panel-x', type=float, nargs=2, default=(.075, .925))
parser.add_argument('--panel-height', type=float, nargs=2, default=(.145, .925))
args = parser.parse_args(sys.argv[sys.argv.index('--') + 1:])
source, output = Path(args.source).resolve(), Path(args.output).resolve()
assert source.is_relative_to(OUT / 'assets') and 'T01_' in str(source)
assert hashlib.sha256(source.read_bytes()).hexdigest() == SOURCE_SHA256
assert output.is_relative_to(OUT / 'manual') and not output.exists()
assert 0 < args.panel_x[0] < args.panel_x[1] < 1
assert 0 < args.panel_height[0] < args.panel_height[1] < 1
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=str(source))
meshes = [obj for obj in bpy.context.scene.objects if obj.type == 'MESH']
assert len(meshes) == 1
obj = meshes[0]
mesh = obj.data
assert mesh.uv_layers.active is not None and len(mesh.materials) == 1
mesh.update()
world = [obj.matrix_world @ vertex.co for vertex in mesh.vertices]
lo = Vector(tuple(min(point[i] for point in world) for i in range(3)))
hi = Vector(tuple(max(point[i] for point in world) for i in range(3)))
height, width = hi.z - lo.z, hi.x - lo.x
assert abs(height - .833666) < .0001 and abs(width - .99999) < .0001, 'Inspect a changed coordinate frame'
# Source GLB is Y-up, not longest-axis-up. Its negative-Z wood face becomes
# positive-Y in Blender, and its rear plane is near Blender Y=-0.015016.
back_plane = -.015016311469
mesh.calc_loop_triangles()
triangles = [tuple(triangle.vertices) for triangle in mesh.loop_triangles]
triangle_loops = [tuple(triangle.loops) for triangle in mesh.loop_triangles]
tree = BVHTree.FromPolygons(world, triangles, all_triangles=True)
uv = mesh.uv_layers.active.data
original_uv = array('f', [0.]) * (len(uv) * 2)
uv.foreach_get('uv', original_uv)
positions = array('f', [0.]) * (len(mesh.vertices) * 3)
mesh.vertices.foreach_get('co', positions)
position_hash = hashlib.sha256(positions.tobytes()).hexdigest()
normal_matrix = obj.matrix_world.to_3x3().inverted().transposed()
diagonal = (hi - lo).length
def projected_uv(position):
    origin = position.copy()
    origin.y = hi.y + diagonal
    hit, normal, index, _ = tree.ray_cast(origin, Vector((0, -1, 0)), diagonal * 3)
    if hit is None or normal.y < .65:
        return None
    points = [world[i] for i in triangles[index]]
    coords = [Vector((original_uv[i * 2], original_uv[i * 2 + 1], 0)) for i in triangle_loops[index]]
    value = barycentric_transform(hit, *points, *coords)
    assert math.isfinite(value.x) and math.isfinite(value.y)
    return (value.x, value.y)


edited = bytearray(len(uv))
modified_faces = skipped_faces = candidates = 0
for polygon in mesh.polygons:
    center = obj.matrix_world @ polygon.center
    relative_x = (center.x - lo.x) / width
    relative_height = (center.z - lo.z) / height
    normal = (normal_matrix @ polygon.normal).normalized()
    if not (args.panel_x[0] <= relative_x <= args.panel_x[1]
            and args.panel_height[0] <= relative_height <= args.panel_height[1]
            and abs(center.y - back_plane) <= height * .008 and normal.y < -.65):
        continue
    candidates += 1
    value = projected_uv(center)
    if value is None:
        skipped_faces += 1
        continue
    # Sampling one atlas point per small source triangle avoids interpolating
    # between unrelated UV islands on the front. Canonical baking follows QA.
    for index in polygon.loop_indices:
        uv[index].uv = value
        edited[index] = 1
    modified_faces += 1
assert modified_faces > 1000 and skipped_faces <= max(10, candidates * .01), (modified_faces, skipped_faces)
mesh.update()
after_positions = array('f', [0.]) * len(positions)
mesh.vertices.foreach_get('co', after_positions)
assert hashlib.sha256(after_positions.tobytes()).hexdigest() == position_hash
after_uv = array('f', [0.]) * len(original_uv)
uv.foreach_get('uv', after_uv)
assert all(edited[i] or original_uv[i * 2:i * 2 + 2] == after_uv[i * 2:i * 2 + 2] for i in range(len(uv)))
mesh.calc_loop_triangles()
assert len(mesh.loop_triangles) == len(triangles)
output.mkdir(parents=True)
bpy.ops.object.select_all(action='DESELECT')
obj.select_set(True)
bpy.context.view_layer.objects.active = obj
bpy.ops.export_scene.gltf(filepath=str(output / 'model.glb'), use_selection=True, export_format='GLB')
bpy.ops.file.pack_all()
bpy.ops.wm.save_as_mainfile(filepath=str(output / 'model.blend'))
shutil.copy2(source, output / 'original-trellis-source.glb')
(output / 'repair.json').write_text(json.dumps({
    'source': str(source), 'source_sha256': SOURCE_SHA256,
    'method': 'Center-sample original front material per rear source triangle; avoid interpolating between unrelated front UV islands; retain geometry, original maps and other UV loops',
    'panel_x': list(args.panel_x), 'panel_height': list(args.panel_height),
    'back_plane_blender_y': back_plane, 'modified_faces': modified_faces,
    'modified_uv_loops': sum(edited), 'skipped_faces': skipped_faces,
    'triangles': len(triangles), 'position_sha256': position_hash,
    'positions_unchanged': True, 'untargeted_uvs_unchanged': True,
    'original_material_images_unmodified': True, 'extra_ai_requests': 0,
    'canonical_reduction_attempts': 0, 'visual_review_required': True
}, indent=2), encoding='utf8')
script = ROOT / 'tools/reststop-production-review-render.py'
sys.argv = [str(script), '--', str(output)]
runpy.run_path(str(script), run_name='__main__')
shutil.copy2(output / 'quality/hero.png', output / 'preview.png')
print(json.dumps({'folder': str(output), 'modified_faces': modified_faces,
                  'visual_review_required': True}), flush=True)
