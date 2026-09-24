"""Use a remaining original Blender attempt after a recorded runtime halt.

Run inside a resource-bounded Blender process. No AI generation or approval occurs.
"""
import argparse
import hashlib
import importlib.util
import json
import shutil
import sys
import time
from pathlib import Path

import bpy

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'outputs/reststop-production-2026-09-24'
parser = argparse.ArgumentParser()
parser.add_argument('--id', required=True)
args = parser.parse_args(sys.argv[sys.argv.index('--') + 1:])
read = lambda p: json.loads(p.read_text(encoding='utf8'))
def atomic(path, value):
    temporary = path.with_name(path.name + '.pending')
    temporary.write_text(json.dumps(value, indent=2), encoding='utf8')
    temporary.replace(path)

jobs = read(OUT / 'assets/.trellis-automation/state.json')['jobs'].values()
job = next(j for j in jobs if Path(j['name']).stem == args.id)
image_hash = hashlib.sha256(Path(job['input']).read_bytes()).hexdigest()
original_path = OUT / 'quality-ledgers' / (image_hash + '.json')
original = read(original_path)
assert original['terminal'] == 'halted'
accepted = [(m, t) for m in original['meshes'] if m.get('review', {}).get('verdict') == 'pass'
            for t in m['textures'] if t.get('review', {}).get('verdict') == 'pass']
assert len(accepted) == 1, 'Select ambiguous sources explicitly instead of guessing'
mesh_record, texture = accepted[0]
source = Path(texture['folder']) / 'model.glb'
source_hash = hashlib.sha256(source.read_bytes()).hexdigest()
ledger_dir = OUT / 'reduction-recovery'
ledger_dir.mkdir(exist_ok=True)
ledger_path = ledger_dir / (image_hash + '.json')
ledger = read(ledger_path) if ledger_path.exists() else {
    'id': args.id, 'original_ledger': str(original_path), 'source': str(source),
    'source_sha256': source_hash, 'original_low_count': len(texture['lows']), 'runs': [],
}
assert ledger['source_sha256'] == source_hash
assert ledger['original_low_count'] == len(texture['lows'])
number = len(texture['lows']) + len(ledger['runs']) + 1
assert number <= 2, 'The two automatic Blender attempts are exhausted'
folder = OUT / 'manual' / (args.id + '-reduction-recovery') / ('low' + str(number))
assert not folder.exists(), 'Never overwrite or silently replay an interrupted recovery'
folder.mkdir(parents=True)
profile = 'parts' if number == 1 else 'preserve_details'
request = {'input': str(source), 'output': str(folder), 'target_triangles': original['settings']['target_triangles'],
           'texture_size': original['settings']['texture_size'], 'remove_floor': original['settings']['remove_floor'],
           'longest_side': original['settings']['longest_side'], 'refine_profile': profile}
atomic(folder / 'blender_request.json', request)
shutil.copy2(source, folder / 'trellis_source.glb')
record = {'folder': str(folder), 'profile': profile, 'started': time.time(), 'state': 'started', 'cleanup_metrics': []}
ledger['runs'].append(record)
atomic(ledger_path, ledger)
app = next(p for p in (Path.home() / 'Desktop').glob('AI */Trellis */*') if (p / 'engine.py').is_file())
spec = importlib.util.spec_from_file_location('task_refiner', app / 'blender_refine.py')
refiner = importlib.util.module_from_spec(spec)
spec.loader.exec_module(refiner)
original_clean = refiner.clean
def observed_clean(obj, epsilon, merge=False):
    original_clean(obj, epsilon, merge)
    metric = {'object': obj.name, **refiner.inspect(obj)}
    record['cleanup_metrics'].append(metric)
    atomic(ledger_path, ledger)
    print('RECOVERY_METRIC ' + json.dumps(metric), flush=True)
refiner.clean = observed_clean
try:
    refiner.refine(request)
    record['state'] = 'refined'
except Exception as error:
    record['state'] = 'failed'
    record['error'] = repr(error)
    bpy.ops.wm.save_as_mainfile(filepath=str(folder / 'failed-state.blend'))
    raise
finally:
    record['finished'] = time.time()
    atomic(ledger_path, ledger)
    atomic(folder / 'repair.json', {'method': 'Remaining original Blender profile after a runtime halt; no AI regeneration',
           'source': str(source), 'source_sha256': source_hash, 'original_ledger_unchanged': str(original_path),
           'shared_recovery_ledger': str(ledger_path), 'original_plus_recovery_runs': number, 'record': record})
