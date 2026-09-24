"""Direct asset correction retaining original UVs/PBR through decimation.

Does not weld imported texture seams or rebake onto nearby unrelated surfaces.
"""
import argparse
import importlib.util
import json
import runpy
import shutil
import sys
from pathlib import Path

import bpy
from mathutils import Vector
from reststop_mesh_cleanup import clean_degenerate_preserving_normals

ROOT = Path(__file__).resolve().parents[1]
parser = argparse.ArgumentParser()
parser.add_argument('--source', required=True)
parser.add_argument('--output', required=True)
parser.add_argument('--clean-first', action='store_true', help='Remove invisible degenerates before allocating the triangle budget')
parser.add_argument('--original-attempts', type=int, choices=(0,1,2), default=2, help='Recorded original/canonical attempts retained for this correction')
args = parser.parse_args(sys.argv[sys.argv.index('--') + 1:])
source = Path(args.source).resolve()
output = Path(args.output).resolve()
assert not output.exists(), 'Preserve earlier manual revisions'
output.mkdir(parents=True)
settings = json.loads((ROOT / 'map-concepts/reststop-production-2026-09-24/effective-settings.json').read_text(encoding='utf8'))
target = settings['target_triangles']
assert target == 15000 and settings['longest_side'] == 0
app = next(p for p in (Path.home() / 'Desktop').glob('AI */Trellis */*') if (p / 'engine.py').is_file())
spec = importlib.util.spec_from_file_location('task_refiner', app / 'blender_refine.py')
refiner = importlib.util.module_from_spec(spec)
spec.loader.exec_module(refiner)
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=str(source))
meshes = [obj for obj in bpy.context.scene.objects if obj.type == 'MESH']
assert len(meshes) == 1, 'Inspect multi-mesh inputs before using this correction'
high = meshes[0]
refiner.active(high)
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
high.name = 'Detailed_Source'
lo, hi = refiner.bounds(high)
offset = Vector(((lo.x + hi.x) / 2, (lo.y + hi.y) / 2, lo.z))
for vertex in high.data.vertices:
    vertex.co -= offset
low = high.copy()
low.data = high.data.copy()
bpy.context.collection.objects.link(low)
low.name = 'Asset_Optimized'
refiner.active(low)
before = refiner.inspect(low)
cleanup = []
if args.clean_first:
    cleanup.append(clean_degenerate_preserving_normals(low.data))
history = []
for index in range(3):
    count = refiner.tris(low)
    if count <= target:
        break
    modifier = low.modifiers.new('UV-preserving direct reduction', 'DECIMATE')
    modifier.ratio = max(.001, (target - 128) / count)
    modifier.use_collapse_triangulate = True
    bpy.ops.object.modifier_apply(modifier=modifier.name)
    if args.clean_first:
        cleanup.append(clean_degenerate_preserving_normals(low.data))
    metric = refiner.inspect(low)
    history.append(metric)
    print('PRESERVED_UV_REDUCTION ' + json.dumps(metric), flush=True)
report = {'source': str(source), 'method': 'Direct Decimate on original UV/PBR mesh without seam welding, re-unwrapping or selected-to-active baking',
          'before': before, 'reduction_metrics': history, 'target_triangles': target,
          'ai_regeneration': False, 'original_automatic_attempts_preserved': args.original_attempts,
          'clean_first': args.clean_first, 'cleanup': cleanup}
(output / 'repair.json').write_text(json.dumps(report, indent=2), encoding='utf8')
if refiner.tris(low) > target:
    bpy.ops.wm.save_as_mainfile(filepath=str(output / 'failed-state.blend'))
    raise RuntimeError('Original UV boundaries prevent meeting the saved triangle target')
high.hide_render = True
high.hide_set(True)
refiner.active(low)
bpy.ops.export_scene.gltf(filepath=str(output / 'model.glb'), use_selection=True, export_format='GLB')
bpy.ops.export_scene.fbx(filepath=str(output / 'model.fbx'), use_selection=True, object_types={'MESH'},
                         axis_forward='-Z', axis_up='Y', path_mode='COPY', embed_textures=True, add_leaf_bones=False)
bpy.ops.file.pack_all()
bpy.ops.wm.save_as_mainfile(filepath=str(output / 'model.blend'))
shutil.copy2(source, output / 'trellis_source.glb')
script = ROOT / 'tools/reststop-production-review-render.py'
sys.argv = [str(script), '--', str(output)]
runpy.run_path(str(script), run_name='__main__')
shutil.copy2(output / 'quality/hero.png', output / 'preview.png')
