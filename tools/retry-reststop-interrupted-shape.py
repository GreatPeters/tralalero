"""Use remaining shape budget after an audited reboot interruption.

The installed workflow and original ledger remain unchanged. Run only while a
real main-batch review gate is held, then visually review the generated shape.
"""
import argparse
import json
from pathlib import Path
import shutil
import subprocess
import sys
import time

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'outputs/reststop-production-2026-09-24'
APP = next(p for p in (Path.home() / 'Desktop').glob('AI */Trellis */*') if (p / 'engine.py').is_file())
sys.path.insert(0, str(APP))
import engine
from quality_staged import shape_prompt, validate, limits
from reststop_prompt_wait import wait_with_history_recheck

parser = argparse.ArgumentParser()
parser.add_argument('--id', required=True, choices=('V04',))
parser.add_argument('--revision', required=True)
parser.add_argument('--attempt', type=int, required=True)
parser.add_argument('--held-stage', choices=('shape','texture_source','final_compare'), default='final_compare')
args = parser.parse_args()
assert args.revision.startswith('r') and args.revision[1:].isdigit()
image = OUT / 'inputs' / (args.id + '.png')
image_hash = engine.sha_file(image)
original_path = OUT / 'quality-ledgers' / (image_hash + '.json')
original = engine.read_json(original_path, {})
validate(original, image_hash)
assert original['terminal'] == 'halted' and len(original['meshes']) == 1
parent = original['meshes'][0]
assert parent['state'] == 'submitted' and not parent['textures']
receipt_path = OUT / 'reviews/V04-interrupted-shape-20260925-1105.json'
receipt = engine.read_json(receipt_path, {})
assert receipt['prompt_id'] == parent['prompt_id'] and receipt['attempt_consumed'] is True
assert receipt['submitted_graph_sha256'] == engine.sha_file(Path(parent['folder']) / 'submitted_prompt.json')
assert not (Path(parent['folder']) / 'model.glb').exists()
assert not list(Path(receipt['output_prefix']).rglob('*.glb'))
opts = engine.read_json(ROOT / 'map-concepts/reststop-production-2026-09-24/effective-settings.json', {})
assert original['settings'] == {k: opts[k] for k in original['settings']}
client = engine.TrellisClient(opts['trellis_url'], opts['trellis_root'])
assert client.request('/task-uv-raster-status')['implementation'] == 'independent-uv-math-v1'
assert not client.request('/history/' + parent['prompt_id'])

def notify(message):
    engine.atomic_json(OUT / 'manual-generation-status.json',
                       {'asset': args.id, 'message': message, 'updated': time.time()})

def gpu_window():
    gate = engine.read_json(OUT / 'review-pending.json', {})
    assert gate.get('state') == 'pending' and gate.get('stage') == args.held_stage
    assert not Path(gate['result']).exists(), 'Hold the real gate until all manual GPU work finishes'
    queue = client.request('/queue')
    assert not queue.get('queue_running') and not queue.get('queue_pending')
    return gate['result']

ledger_path = OUT / 'manual-shape-ledgers' / (image_hash + '.json')
ledger_path.parent.mkdir(exist_ok=True)
with engine.OutputLock(ledger_path.with_suffix('.lock')):
    ledger = engine.read_json(ledger_path, {'image_hash': image_hash,
        'settings': original['settings'], 'original_ledger': str(original_path),
        'original_shape_count': len(original['meshes']), 'interruption_receipt': str(receipt_path), 'attempts': []})
    assert ledger['image_hash'] == image_hash and ledger['settings'] == original['settings']
    assert ledger['original_shape_count'] == len(original['meshes'])
    index = args.attempt - 1
    assert 0 <= index <= len(ledger['attempts'])
    if index == len(ledger['attempts']):
        combined = len(original['meshes']) + len(ledger['attempts'])
        assert combined < limits(original)['meshes'], 'Original interrupted attempt remains consumed'
        if index:
            previous = ledger['attempts'][-1]
            assert previous['state'] == 'generated'
            assert engine.read_json(Path(previous['folder']) / 'visual-review.json', {}).get('verdict') == 'mesh'
        held_gate = gpu_window()
        histories = list((OUT / 'assets').glob('*/stages/*/trellis_history.json')) + list((OUT / 'manual').glob('**/trellis_history.json'))
        latest = max((p.stat().st_mtime for p in histories), default=0)
        while time.time() < latest + 60:
            notify('Waiting for preserved 60-second generation cooldown')
            time.sleep(min(2, latest + 60 - time.time()))
        assert gpu_window() == held_gate
        number = combined + 1
        seed = (opts['seed'] + combined * 104729) & 0x7fffffff
        folder = OUT / 'manual' / (args.id + '-' + args.revision) / ('shape' + str(number))
        assert not folder.exists()
        folder.mkdir(parents=True)
        record = {'number': number, 'folder': str(folder), 'seed': seed, 'state': 'reserved',
                  'held_review_result': held_gate, 'held_review_stage': args.held_stage,
                  'interrupted_prompt_id': parent['prompt_id']}
        ledger['attempts'].append(record)
        engine.atomic_json(ledger_path, ledger)
        uploaded = client.upload(image, args.id + '_' + args.revision + '_shape' + str(number))
        graph = shape_prompt(engine.read_json(APP / 'workflow_api.json', {}), {**opts, 'seed': seed},
                             uploaded, 'trellis_automation/manual/' + args.id + '/' + args.revision + '/shape' + str(number) + '/asset')
        engine.atomic_json(folder / 'submitted_prompt.json', graph)
        assert gpu_window() == held_gate
        record['prompt_id'] = client.submit(graph, args.id + '-reboot-shape' + str(number))
        record['state'] = 'submitted'
        engine.atomic_json(ledger_path, ledger)
    else:
        record = ledger['attempts'][index]
        folder = Path(record['folder'])
    assert record['state'] != 'reserved', 'Ambiguous submission: preserve and audit; never blindly submit again'
    if record['state'] == 'submitted':
        history = wait_with_history_recheck(client, client.wait, record['prompt_id'], opts['timeout_minutes'] * 60, notify)
        shutil.copy2(client.output_path(history), folder / 'model.glb')
        engine.atomic_json(folder / 'trellis_history.json', history)
        record.update(state='generated', finished=time.time(), source_sha256=engine.sha_file(folder / 'model.glb'))
        engine.atomic_json(ledger_path, ledger)
    assert record['state'] == 'generated' and engine.valid_glb(folder / 'model.glb')
    engine.atomic_json(folder / 'manual-shape-recovery.json', {
        'image_hash': image_hash, 'original_prompt_id': parent['prompt_id'],
        'manual_prompt_id': record['prompt_id'], 'source_sha256': record['source_sha256'],
        'shape_seed': record['seed'], 'combined_shape_number': record['number'],
        'manual_shape_ledger': str(ledger_path), 'interruption_receipt': str(receipt_path)})
    if not all((folder / 'quality' / (name + '.png')).is_file() for name in ('hero', 'top', 'front', 'opposite', 'neutral')):
        command = [opts['blender'], '--background', '--factory-startup', '--threads', '4', '--python-exit-code', '2',
                   '--python', str(ROOT / 'tools/run-limited-generation.py'), '--', '--script',
                   str(ROOT / 'tools/reststop-production-review-render.py'), '--record', str(folder / 'render-resource.json'),
                   '--', str(folder), '--shape']
        with (folder / 'render.log').open('wb') as log:
            result = subprocess.run(command, stdout=log, stderr=subprocess.STDOUT, creationflags=getattr(subprocess, 'CREATE_NO_WINDOW', 0))
        assert result.returncode == 0
    notify('Interrupted shape recovery awaiting actual visual review')
    print(json.dumps({'folder': str(folder), 'combined_shape_count': len(original['meshes']) + len(ledger['attempts']),
                      'combined_shape_limit': limits(original)['meshes'], 'visual_review_required': True}), flush=True)
