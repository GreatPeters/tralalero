"""Recover P04 before the destructive downstream simplification/hole-fill chain."""
import json
from pathlib import Path
import runpy
import shutil
import sys

import bpy
from reststop_mesh_cleanup import clean_degenerate_preserving_normals

root = Path(__file__).resolve().parents[1]
out = root / 'outputs/reststop-production-2026-09-24'
source = out / 'reviews/P04-cached-stages/stage161/model.glb'
folder = out / 'manual/P04-r1/source'
assert not folder.exists()
folder.mkdir(parents=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=str(source))
meshes = [obj for obj in bpy.context.scene.objects if obj.type == 'MESH']
assert len(meshes) == 1
obj = meshes[0]
bpy.context.view_layer.objects.active = obj
obj.select_set(True)
obj.data.calc_loop_triangles()
original_triangles = len(obj.data.loop_triangles)
cleanup_before = clean_degenerate_preserving_normals(obj.data)
obj.data.calc_loop_triangles()
modifier = obj.modifiers.new('Preserve valid shape to saved intermediate budget', 'DECIMATE')
modifier.ratio = min(1., 298000 / len(obj.data.loop_triangles))
modifier.use_collapse_triangulate = True
bpy.ops.object.modifier_apply(modifier=modifier.name)
cleanup_after = clean_degenerate_preserving_normals(obj.data)
obj.data.calc_loop_triangles()
triangles = len(obj.data.loop_triangles)
assert 0 < triangles <= 300000
bpy.ops.export_scene.gltf(filepath=str(folder / 'model.glb'), use_selection=True, export_format='GLB')
bpy.ops.wm.save_as_mainfile(filepath=str(folder / 'model.blend'))
shutil.copy2(source, folder / 'original-trellis-source.glb')
(folder / 'repair.json').write_text(json.dumps({'method': 'Recover cached m3 reconstruction before the destructive downstream simplification/hole-fill chain; use Blender Decimate for the saved 300000-face intermediate budget',
    'source': str(source), 'source_triangles': original_triangles, 'triangles': triangles,
    'cleanup_before': cleanup_before, 'cleanup_after': cleanup_after,
    'extra_ai_generations': 0, 'canonical_final_reduction_attempts': 0}, indent=2), encoding='utf8')
script = root / 'tools/reststop-production-review-render.py'
sys.argv = [str(script), '--', str(folder), '--shape']
runpy.run_path(str(script), run_name='__main__')
print(json.dumps({'folder': str(folder), 'triangles': triangles}), flush=True)
