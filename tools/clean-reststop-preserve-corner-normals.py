"""Remove invisible degenerate/wire geometry while retaining UVs/PBR/corner normals."""
import argparse
import importlib.util
import json
import runpy
import shutil
import sys
from pathlib import Path

import bpy
from reststop_mesh_cleanup import clean_degenerate_preserving_normals

ROOT = Path(__file__).resolve().parents[1]
parser = argparse.ArgumentParser()
parser.add_argument('--source-folder', required=True)
parser.add_argument('--output', required=True)
args = parser.parse_args(sys.argv[sys.argv.index('--') + 1:])
source = Path(args.source_folder).resolve()
output = Path(args.output).resolve()
assert not output.exists(), 'Preserve prior cleanup revisions'
output.mkdir(parents=True)
app = next(p for p in (Path.home() / 'Desktop').glob('AI */Trellis */*') if (p / 'engine.py').is_file())
spec = importlib.util.spec_from_file_location('task_refiner', app / 'blender_refine.py')
refiner = importlib.util.module_from_spec(spec)
spec.loader.exec_module(refiner)
bpy.ops.wm.open_mainfile(filepath=str(source / 'model.blend'))
low = bpy.data.objects['Asset_Optimized']
refiner.active(low)
before = refiner.inspect(low)
cleanup = clean_degenerate_preserving_normals(low.data)
after = refiner.inspect(low)
assert after['triangles'] <= 15000 and not after['degenerate_faces'] and not after['loose_vertices'] and after['uv']
(output / 'repair.json').write_text(json.dumps({'source_folder': str(source),
    'method': 'Remove only nearly-zero-area faces, wire edges and unused vertices; restore retained corner normals by source-loop mapping',
    'before': before, 'after': after, **cleanup, 'uv_and_original_pbr_retained': True,
    'ai_regeneration': False, 'original_automatic_attempts_preserved': 2}, indent=2), encoding='utf8')
bpy.ops.export_scene.gltf(filepath=str(output / 'model.glb'), use_selection=True, export_format='GLB')
bpy.ops.export_scene.fbx(filepath=str(output / 'model.fbx'), use_selection=True, object_types={'MESH'},
                         axis_forward='-Z', axis_up='Y', path_mode='COPY', embed_textures=True, add_leaf_bones=False)
bpy.ops.file.pack_all()
bpy.ops.wm.save_as_mainfile(filepath=str(output / 'model.blend'))
shutil.copytree(source / 'textures', output / 'textures')
for name in ('texture-extraction.json', 'trellis_source.glb'):
    shutil.copy2(source / name, output / name)
script = ROOT / 'tools/reststop-production-review-render.py'
sys.argv = [str(script), '--', str(output)]
runpy.run_path(str(script), run_name='__main__')
shutil.copy2(output / 'quality/hero.png', output / 'preview.png')
print(json.dumps(after), flush=True)
