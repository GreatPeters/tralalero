"""Export cached shape stages for diagnosis; refuse any new AI computation."""
import argparse
import copy
import json
from pathlib import Path
import re
import shutil
import sys

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'outputs/reststop-production-2026-09-24'
APP = next(p for p in (Path.home() / 'Desktop').glob('AI */Trellis */*') if (p / 'engine.py').is_file())
sys.path.insert(0, str(APP))
import engine
from reststop_prompt_wait import wait_with_history_recheck

parser = argparse.ArgumentParser()
parser.add_argument('--id', required=True)
parser.add_argument('--mesh-number', type=int, choices=(1, 2, 3), required=True)
parser.add_argument('--revision', type=int, default=1)
args = parser.parse_args()
assert re.fullmatch(r'[A-Z]\d{2}', args.id) and args.revision > 0
assert (OUT / 'inputs' / (args.id + '.png')).is_file()
label = args.id + '-m' + str(args.mesh_number) + '-r' + str(args.revision)
gate = engine.read_json(OUT / 'review-pending.json', {})
assert Path(gate['reference']).stem == args.id and gate['stage'] == 'shape'
assert Path(gate['folder']).name == 'm' + str(args.mesh_number) and not Path(gate['result']).exists()
folder = OUT / 'reviews' / (label + '-cached-stages')
assert not folder.exists(), 'Keep diagnostic requests and evidence'
folder.mkdir()
opts = engine.read_json(ROOT / 'map-concepts/reststop-production-2026-09-24/effective-settings.json', {})
client = engine.TrellisClient(opts['trellis_url'], opts['trellis_root'])
queue = client.request('/queue')
assert not queue['queue_running'] and not queue['queue_pending']
original = engine.read_json(Path(gate['folder']) / 'submitted_prompt.json', {})
graph = copy.deepcopy(original)
# This output depends only on loading the existing PNG, never on a model node.
graph['9900'] = {'class_type': 'PreviewImage', 'inputs': {'images': ['6', 0]}}
probe = {'prompt': graph, 'client_id': 'reststop-' + label + '-cache-probe', 'partial_execution_targets': ['9900']}
engine.atomic_json(folder / 'probe-request.json', probe)
pid = client.request('/prompt', probe)['prompt_id']
engine.atomic_json(folder / 'probe-id.json', {'prompt_id': pid})
history = wait_with_history_recheck(client, client.wait, pid, 90, lambda message: None)
engine.atomic_json(folder / 'probe-history.json', history)
cached = set(next(data['nodes'] for event, data in history['status']['messages'] if event == 'execution_cached'))
required = {'39', '213', '214', '215', '216', '217', '193', '161'}
assert required <= cached, ('Refusing regeneration; missing cache', sorted(required - cached))
targets = []
views = {}
for index, stage in enumerate(('217', '193', '161')):
    convert, export, preview = (str(9910 + index * 3 + n) for n in range(3))
    graph[convert] = {'class_type': 'Trellis2MeshWithVoxelToTrimesh',
                      'inputs': {'mesh': [stage, 0], 'reorient_vertices': '90 degrees'}}
    graph[export] = {'class_type': 'Trellis2ExportMesh', 'inputs': {'trimesh': [convert, 0],
        'filename_prefix': 'trellis_automation/' + label + '_cached_diagnostic/stage' + stage, 'file_format': 'glb'}}
    graph[preview] = {'class_type': 'Preview3D', 'inputs': {'model_file': [export, 0], 'image': ''}}
    targets.append(preview)
    views[stage] = preview
assert all(graph[key] == value for key, value in original.items())
assert not Path(gate['result']).exists()
queue = client.request('/queue')
assert not queue['queue_running'] and not queue['queue_pending']
payload = {'prompt': graph, 'client_id': 'reststop-' + label + '-cache-export', 'partial_execution_targets': targets}
engine.atomic_json(folder / 'export-request.json', payload)
pid = client.request('/prompt', payload)['prompt_id']
engine.atomic_json(folder / 'export-id.json', {'prompt_id': pid})
history = wait_with_history_recheck(client, client.wait, pid, 120, lambda message: None)
engine.atomic_json(folder / 'export-history.json', history)
export_cached = set(next(data['nodes'] for event, data in history['status']['messages'] if event == 'execution_cached'))
assert required <= export_cached, 'Investigate unexpected cache loss; do not repeat'
for stage, preview in views.items():
    destination = folder / ('stage' + stage)
    destination.mkdir()
    source = client.output_path({'outputs': {preview: history['outputs'][preview]}})
    shutil.copy2(source, destination / 'model.glb')
engine.atomic_json(folder / 'receipt.json', {'extra_ai_generations': 0, 'original_graph_unchanged': True,
    'cached_required_nodes': sorted(required), 'stages': {'217': 'raw decoded shape', '193': 'hole-filled shape', '161': 'quad reconstruction'}})
print(json.dumps({'folder': str(folder), 'extra_ai_generations': 0}), flush=True)
