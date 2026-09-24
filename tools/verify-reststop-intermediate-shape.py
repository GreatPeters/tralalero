"""Fresh GLB/BLEND geometry checks before texturing a recovered TRELLIS shape."""
import argparse
import importlib.util
import json
from pathlib import Path
import sys

import bpy

parser = argparse.ArgumentParser()
parser.add_argument('--folder', required=True)
args = parser.parse_args(sys.argv[sys.argv.index('--') + 1:])
folder = Path(args.folder).resolve()
app = next(p for p in (Path.home() / 'Desktop').glob('AI */Trellis */*') if (p / 'engine.py').is_file())
spec = importlib.util.spec_from_file_location('asset_refiner_inspection', app / 'blender_refine.py')
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)
report = {'formats': {}}
for extension in ('blend', 'glb'):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    path = str(folder / ('model.' + extension))
    if extension == 'blend':
        bpy.ops.wm.open_mainfile(filepath=path)
    else:
        bpy.ops.import_scene.gltf(filepath=path)
    meshes = [obj for obj in bpy.context.scene.objects if obj.type == 'MESH']
    values = []
    for obj in meshes:
        value = module.inspect(obj)
        value['object'] = obj.name
        value['validate_changed_data'] = obj.data.validate(verbose=True)
        values.append(value)
    total = sum(value['triangles'] for value in values)
    ok=bool(meshes) and 0 < total <= 300000 and all(value['finite'] and not value['degenerate_faces'] and not value['loose_vertices']
               and not value['validate_changed_data'] for value in values)
    report['formats'][extension] = {'triangles': total, 'meshes': values,'ok':ok}
report['ok'] = all(value['ok'] for value in report['formats'].values()) and report['formats']['blend']['triangles'] == report['formats']['glb']['triangles']
(folder / 'intermediate-validation.json').write_text(json.dumps(report, indent=2), encoding='utf8')
print(json.dumps({'ok':report['ok'],'formats':{key:{'triangles':value['triangles'],'ok':value['ok'],
    'invalid_meshes':[v for v in value['meshes'] if not v['finite'] or v['degenerate_faces'] or v['loose_vertices'] or v['validate_changed_data']]}
    for key,value in report['formats'].items()}}),flush=True)
assert report['ok'],'Fresh intermediate checks failed; see intermediate-validation.json'
