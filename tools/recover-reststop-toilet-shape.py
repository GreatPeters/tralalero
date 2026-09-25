"""Recover T03's cached open bowl before the destructive downstream closure."""
import argparse
import hashlib
import json
from pathlib import Path
import runpy
import shutil
import sys

import bmesh
import bpy
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'outputs/reststop-production-2026-09-24'
SOURCE = OUT / 'reviews/T03-m3-r1-cached-stages/stage161/model.glb'
parser = argparse.ArgumentParser()
parser.add_argument('--revision', type=int, required=True)
args = parser.parse_args(sys.argv[sys.argv.index('--') + 1:])
assert args.revision > 0
folder = OUT / ('manual/T03-r' + str(args.revision)) / 'source'
assert not folder.exists(), 'Keep earlier candidates and evidence'
source_sha = hashlib.sha256(SOURCE.read_bytes()).hexdigest()
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=str(SOURCE))
meshes = [obj for obj in bpy.context.scene.objects if obj.type == 'MESH']
assert len(meshes) == 1
obj = meshes[0]
assert not obj.data.uv_layers and not obj.data.materials, 'This cleanup is for the untextured cached source'
bpy.ops.object.select_all(action='DESELECT')
obj.select_set(True)
bpy.context.view_layer.objects.active = obj
obj.data.calc_loop_triangles()
before = {'vertices': len(obj.data.vertices), 'triangles': len(obj.data.loop_triangles)}
points = [obj.matrix_world @ Vector(corner) for corner in obj.bound_box]
lo = Vector(tuple(min(point[i] for point in points) for i in range(3)))
hi = Vector(tuple(max(point[i] for point in points) for i in range(3)))
height = hi.z - lo.z
assert height > 0
folder.mkdir(parents=True)

# No source UVs or supplied normals exist here. Weld only effectively coincident
# seams; do not fill holes or reconstruct the already-present bowl interior.
initial_validate_changed = obj.data.validate(verbose=True)
weld = obj.modifiers.new('Join coincident source seams', 'WELD')
weld.merge_threshold = height * 1e-6
bpy.ops.object.modifier_apply(modifier=weld.name)
obj.data.validate(verbose=True)
obj.data.polygons.foreach_set('use_smooth', [True] * len(obj.data.polygons))
obj.data.update()
obj.data.calc_loop_triangles()
after_weld = {'vertices': len(obj.data.vertices), 'triangles': len(obj.data.loop_triangles)}
decimate = obj.modifiers.new('Saved intermediate face budget', 'DECIMATE')
decimate.ratio = min(1., 298000 / len(obj.data.loop_triangles))
decimate.use_collapse_triangulate = True
bpy.ops.object.modifier_apply(modifier=decimate.name)
obj.data.polygons.foreach_set('use_smooth', [True] * len(obj.data.polygons))
obj.data.update()
# This cached source has no supplied normals or UVs. Degenerate geometry may
# produce zero derived normals, which cannot be retained as custom unit normals.
# Remove only zero-area/wire geometry, then establish normals on the result.
mesh = obj.data
threshold = ((hi - lo).length * 1e-7) ** 2 * .04
bm = bmesh.new()
bm.from_mesh(mesh)
bad_faces = [face for face in bm.faces if face.calc_area() < threshold]
cleanup = {'removed_faces': len(bad_faces),
           'removed_surface_area': sum(face.calc_area() for face in bad_faces)}
if bad_faces:
    bmesh.ops.delete(bm, geom=bad_faces, context='FACES_ONLY')
wire = [edge for edge in bm.edges if not edge.link_faces]
cleanup['removed_wire_edges'] = len(wire)
if wire:
    bmesh.ops.delete(bm, geom=wire, context='EDGES')
loose = [vertex for vertex in bm.verts if not vertex.link_edges]
cleanup['removed_unused_vertices'] = len(loose)
if loose:
    bmesh.ops.delete(bm, geom=loose, context='VERTS')
bm.normal_update()
bm.to_mesh(mesh)
bm.free()
mesh.polygons.foreach_set('use_smooth', [True] * len(mesh.polygons))
mesh.update()
cleanup['normals'] = 'Recomputed smooth normals; no original supplied normals existed'
obj.data.calc_loop_triangles()
triangles = len(obj.data.loop_triangles)
assert 0 < triangles <= 300000
bpy.ops.export_scene.gltf(filepath=str(folder / 'model.glb'), use_selection=True,
                         export_format='GLB', export_normals=True)
bpy.ops.wm.save_as_mainfile(filepath=str(folder / 'model.blend'))
shutil.copy2(SOURCE, folder / 'original-trellis-source.glb')
(folder / 'repair.json').write_text(json.dumps({
    'source': str(SOURCE), 'source_sha256': source_sha, 'cached_stage': 161,
    'method': 'Preserve decoded open bowl; validate, weld coincident untextured seams, establish smooth normals and locally decimate to the saved intermediate ceiling',
    'before': before, 'after_weld': after_weld, 'triangles': triangles,
    'weld_distance': height * 1e-6, 'initial_validate_changed': initial_validate_changed,
    'cleanup_after_decimation': cleanup, 'hole_filling_performed': False,
    'intermediate_reduction_passes': 1, 'extra_ai_shape_requests': 0,
    'canonical_final_reduction_attempts': 0,
    'source_bounds_min': list(lo), 'source_bounds_max': list(hi),
    'visual_review_required': True, 'fresh_intermediate_validation_required': True,
    'cavity_depth_validation_required': True
}, indent=2), encoding='utf8')
script = ROOT / 'tools/reststop-production-review-render.py'
sys.argv = [str(script), '--', str(folder), '--shape']
runpy.run_path(str(script), run_name='__main__')
print(json.dumps({'folder': str(folder), 'triangles': triangles,
                  'visual_and_cavity_validation_required': True}), flush=True)
