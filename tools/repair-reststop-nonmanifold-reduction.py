"""Repair an inspected stalled low mesh by splitting non-manifold edge fans.

Preserve the two failed automatic attempts; this is a separate direct correction.
"""
import argparse
import importlib.util
import json
import math
import runpy
import shutil
import sys
from pathlib import Path

import bmesh
import bpy

parser = argparse.ArgumentParser()
parser.add_argument('--failed-blend', required=True)
parser.add_argument('--output', required=True)
args = parser.parse_args(sys.argv[sys.argv.index('--') + 1:])
source = Path(args.failed_blend).resolve()
output = Path(args.output).resolve()
assert not output.exists(), 'Choose a new manual correction revision'
output.mkdir(parents=True)
app = next(p for p in (Path.home() / 'Desktop').glob('AI */Trellis */*') if (p / 'engine.py').is_file())
spec = importlib.util.spec_from_file_location('task_refiner', app / 'blender_refine.py')
refiner = importlib.util.module_from_spec(spec)
spec.loader.exec_module(refiner)
bpy.ops.wm.open_mainfile(filepath=str(source))
high = bpy.data.objects['Detailed_Source']
low = bpy.data.objects['Asset_Optimized']
refiner.active(low)
lo, hi = refiner.bounds(high)
diagonal = (hi - lo).length
before = refiner.inspect(low)
positions = {tuple(vertex.co) for vertex in low.data.vertices}
bm = bmesh.new()
bm.from_mesh(low.data)
bad_edges = [edge for edge in bm.edges if len(edge.link_faces) > 2]
split_count = len(bad_edges)
assert split_count, 'This correction requires observed non-manifold edge fans'
bmesh.ops.split_edges(bm, edges=bad_edges)
bm.to_mesh(low.data)
bm.free()
low.data.update()
assert positions == {tuple(vertex.co) for vertex in low.data.vertices}, 'Splitting must not move the surface'
refiner.clean(low, diagonal * 3e-7, merge=False)
after_split = refiner.inspect(low)
assert after_split['nonmanifold_edges'] == 0
history = []
for index in range(3):
    count = refiner.tris(low)
    if count <= 15000:
        break
    modifier = low.modifiers.new('Manual budget after edge-fan repair', 'DECIMATE')
    modifier.ratio = max(.001, (15000 - 64) / count)
    modifier.use_collapse_triangulate = True
    bpy.ops.object.modifier_apply(modifier=modifier.name)
    # Do not weld the separated fans back together.
    refiner.clean(low, diagonal * 3e-7, merge=False)
    metric = refiner.inspect(low)
    history.append(metric)
    print('REPAIRED_REDUCTION ' + json.dumps(metric), flush=True)
report = {'source_failed_blend': str(source), 'method': 'Split non-manifold edge fans without moving the surface, then reduce the repaired low mesh without re-welding those fans',
          'before': before, 'split_edges': split_count, 'after_split': after_split, 'reduction_metrics': history,
          'original_automatic_attempts_preserved': 2, 'ai_regeneration': False}
(output / 'repair.json').write_text(json.dumps(report, indent=2), encoding='utf8')
if refiner.tris(low) > 15000:
    bpy.ops.wm.save_as_mainfile(filepath=str(output / 'failed-state.blend'))
    raise RuntimeError('The repaired mesh still exceeds the saved triangle target')
for polygon in low.data.polygons:
    polygon.use_smooth = True
low.data.set_sharp_from_angle(angle=math.radians(50))
for layer in list(low.data.uv_layers):
    low.data.uv_layers.remove(layer)
low.data.uv_layers.new(name='UVMap')
bpy.ops.object.mode_set(mode='EDIT')
bpy.ops.mesh.select_all(action='SELECT')
bpy.ops.uv.smart_project(angle_limit=math.radians(66), island_margin=.006)
bpy.ops.object.mode_set(mode='OBJECT')
refiner.bake(high, low, output, 2048, diagonal)
refiner.active(low)
bpy.ops.export_scene.gltf(filepath=str(output / 'model.glb'), use_selection=True, export_format='GLB')
bpy.ops.export_scene.fbx(filepath=str(output / 'model.fbx'), use_selection=True, object_types={'MESH'},
                         axis_forward='-Z', axis_up='Y', path_mode='COPY', embed_textures=True, add_leaf_bones=False)
bpy.ops.file.pack_all()
bpy.ops.wm.save_as_mainfile(filepath=str(output / 'model.blend'))
shutil.copy2(source.parent / 'trellis_source.glb', output / 'trellis_source.glb')
script = Path(__file__).with_name('reststop-production-review-render.py')
sys.argv = [str(script), '--', str(output)]
runpy.run_path(str(script), run_name='__main__')
shutil.copy2(output / 'quality/hero.png', output / 'preview.png')
