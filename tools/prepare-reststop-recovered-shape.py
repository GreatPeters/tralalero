"""Prepare a separately recovered high shape without overwriting the failed source."""
import argparse
import json
from pathlib import Path
import runpy
import shutil
import sys

parser = argparse.ArgumentParser()
parser.add_argument('--source', required=True)
parser.add_argument('--output', required=True)
parser.add_argument('--pbr', action='store_true', help='Review preserved PBR instead of clay for a textured source')
args = parser.parse_args(sys.argv[sys.argv.index('--') + 1:])
source, output = Path(args.source).resolve(), Path(args.output).resolve()
root = Path(__file__).resolve().parents[1]
assert source.is_relative_to(root / 'outputs') and output.is_relative_to(root / 'outputs')
assert not output.exists()
output.mkdir(parents=True)

import bpy
from reststop_mesh_cleanup import clean_degenerate_preserving_normals

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=str(source))
meshes = [obj for obj in bpy.context.scene.objects if obj.type == 'MESH']
reports = []
for obj in meshes:
    validated = obj.data.validate(clean_customdata=False)
    reports.append({'object': obj.name, 'initial_validate_changed_data': validated,
                    **clean_degenerate_preserving_normals(obj.data)})
triangles = sum(len(polygon.vertices) - 2 for obj in meshes for polygon in obj.data.polygons)
assert 0 < triangles <= 300000
bpy.ops.export_scene.gltf(filepath=str(output / 'model.glb'), export_format='GLB')
bpy.ops.wm.save_as_mainfile(filepath=str(output / 'model.blend'))
shutil.copy2(source, output / 'source-before-cleanup.glb')
(output / 'cleanup.json').write_text(json.dumps({'source': str(source), 'triangles': triangles,
    'objects': reports, 'extra_ai_generations': 0}, indent=2), encoding='utf8')
script = root / 'tools/reststop-production-review-render.py'
sys.argv = [str(script), '--', str(output)] + ([] if args.pbr else ['--shape'])
runpy.run_path(str(script), run_name='__main__')
print(json.dumps({'folder': str(output), 'triangles': triangles}), flush=True)
