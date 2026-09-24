"""Reprocess a glazed cabinet's cached shape without new AI sampling."""
import argparse
import copy
import json
from pathlib import Path
import shutil
import sys
import time

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'outputs/reststop-production-2026-09-24'
APP = next(p for p in (Path.home() / 'Desktop').glob('AI */Trellis */*') if (p / 'engine.py').is_file())
sys.path.insert(0, str(APP))
import engine
from reststop_prompt_wait import wait_with_history_recheck

parser=argparse.ArgumentParser()
parser.add_argument('--id',choices=('S02','S08'),default='S02')
parser.add_argument('--revision',type=int,default=1)
parser.add_argument('--mesh-number',type=int,choices=(1,2,3),default=1)
parser.add_argument('--export-high',action='store_true',help='Optional large reconstruction; source export is always capped')
args=parser.parse_args()
assert args.revision>0

gate = engine.read_json(OUT / 'review-pending.json', {})
assert Path(gate['reference']).stem == args.id and gate['stage'] == 'shape'
assert Path(gate['folder']).name == 'm'+str(args.mesh_number) and not Path(gate['result']).exists()
folder = OUT / 'manual' / (args.id+'-preserve-inner-r'+str(args.revision))
assert not folder.exists()
folder.mkdir()
opts = engine.read_json(ROOT / 'map-concepts/reststop-production-2026-09-24/effective-settings.json', {})
client = engine.TrellisClient(opts['trellis_url'], opts['trellis_root'])
queue = client.request('/queue')
assert not queue['queue_running'] and not queue['queue_pending']
original = engine.read_json(Path(gate['folder']) / 'submitted_prompt.json', {})
graph = copy.deepcopy(original)
graph['9900'] = {'class_type': 'PreviewImage', 'inputs': {'images': ['6', 0]}}
probe = {'prompt': graph, 'client_id': 'reststop-'+args.id+'-inner-cache-probe', 'partial_execution_targets': ['9900']}
engine.atomic_json(folder / 'probe-request.json', probe)
pid = client.request('/prompt', probe)['prompt_id']
engine.atomic_json(folder / 'probe-id.json', {'prompt_id': pid})
history = wait_with_history_recheck(client, client.wait, pid, 90, lambda message: None)
engine.atomic_json(folder / 'probe-history.json', history)
required = {'39', '213', '214', '215', '216', '217'}
cached = set(next(data['nodes'] for event, data in history['status']['messages'] if event == 'execution_cached'))
assert required <= cached, ('Refuse new sampling; missing cached nodes', sorted(required - cached))
graph['9971'] = {'class_type': 'Trellis2ReconstructMeshWithQuad', 'inputs': {
    'mesh': ['217', 0], 'remesh_band': 1.0, 'resolution': 1024,
    'remove_floaters': False, 'remove_inner_faces': False}}
graph['9972'] = {'class_type': 'Trellis2SimplifyMesh', 'inputs': {
    'mesh': ['9971', 0], 'target_face_num': opts['intermediate_faces'], 'method': 'Cumesh'}}
views = {}
exports=[('source','9972',9990)]
if args.export_high:exports.insert(0,('high','9971',9980))
for label, mesh_node, base in exports:
    convert, export, preview = str(base), str(base + 1), str(base + 2)
    graph[convert] = {'class_type': 'Trellis2MeshWithVoxelToTrimesh', 'inputs': {
        'mesh': [mesh_node, 0], 'reorient_vertices': '90 degrees'}}
    graph[export] = {'class_type': 'Trellis2ExportMesh', 'inputs': {'trimesh': [convert, 0],
        'filename_prefix': 'trellis_automation/'+args.id+'_preserve_inner_r'+str(args.revision)+'/' + label + '/asset', 'file_format': 'glb'}}
    graph[preview] = {'class_type': 'Preview3D', 'inputs': {'model_file': [export, 0], 'image': ''}}
    views[label] = preview
assert all(graph[key] == value for key, value in original.items())
assert not Path(gate['result']).exists()
queue = client.request('/queue')
assert not queue['queue_running'] and not queue['queue_pending']
payload = {'prompt': graph, 'client_id': 'reststop-'+args.id+'-inner-recovery', 'partial_execution_targets': list(views.values())}
engine.atomic_json(folder / 'postprocess-request.json', payload)
pid = client.request('/prompt', payload)['prompt_id']
engine.atomic_json(folder / 'postprocess-id.json', {'prompt_id': pid})
history = wait_with_history_recheck(client, client.wait, pid, opts['timeout_minutes'] * 60,
    lambda message: engine.atomic_json(OUT / 'manual-generation-status.json', {'asset': args.id, 'message': 'Recovering cached interior without AI resampling','updated':time.time()}))
engine.atomic_json(folder / 'postprocess-history.json', history)
cached = set(next(data['nodes'] for event, data in history['status']['messages'] if event == 'execution_cached'))
assert required <= cached, 'Audit unexpected cache loss before any retry'
for label, preview in views.items():
    destination = folder / label
    destination.mkdir()
    source = client.output_path({'outputs': {preview: history['outputs'][preview]}})
    shutil.copy2(source, destination / 'model.glb')
engine.atomic_json(folder / 'receipt.json', {'extra_ai_generations': 0, 'original_graph_unchanged': True,
    'cached_ai_nodes': sorted(required), 'local_postprocess': {'remove_inner_faces': False, 'remove_floaters': False,
    'additional_hole_fill': False, 'target_faces': opts['intermediate_faces']}})
print(json.dumps({'folder': str(folder), 'extra_ai_generations': 0}), flush=True)
