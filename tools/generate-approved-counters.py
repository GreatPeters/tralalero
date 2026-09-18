"""Generate only this task's two missing indoor modules on the existing local backend."""
import importlib
import json
from pathlib import Path
import sys
import time
import urllib.request

root = Path(__file__).resolve().parent.parent
record = root / 'map-concepts/approved-road-concepts-2026-09-13'
sys.path.insert(0, 'C:/Users/ljh/Desktop/AI 프로그램/Trellis 자동화/앱')
engine = importlib.import_module('engine')
with urllib.request.urlopen('http://127.0.0.1:8189/task-uv-raster-status', timeout=5) as response:
    status = json.load(response)
if status.get('implementation') != 'independent-uv-math-v1':
    raise RuntimeError('Expected the installed verified UV backend.')
with urllib.request.urlopen('http://127.0.0.1:8189/queue', timeout=5) as response:
    queue = json.load(response)
if queue.get('queue_running') or queue.get('queue_pending'):
    raise RuntimeError('Backend already has work; do not compete with it.')
original = engine.build_prompt
def build(*values):
    graph = original(*values)
    graph['194']['inputs']['remove_background'] = True
    return graph
engine.build_prompt = build
output = root / 'outputs/approved-road-concepts-2026-09-13/counters'
options = engine.settings({'input_dir': str(record / 'references'), 'output_dir': str(output),
    'recursive': False, 'resolution': 1536, 'texture_size': 2048, 'target_triangles': 25000,
    'keep_models_loaded': False, 'use_tiled_decoder': True, 'quality_retry': False,
    'stage_quality': False, 'remove_floor': False, 'continue_on_error': True,
    'longest_side': 6.4, 'trellis_url': 'http://127.0.0.1:8189',
    'blender': 'C:/Program Files/Blender Foundation/Blender 4.4/blender.exe'})
batch = engine.BatchEngine()
print(json.dumps(batch.start(options), ensure_ascii=True), flush=True)
while batch.active:
    snapshot = batch.snapshot()
    engine.atomic_json(output / 'task-status.json', snapshot)
    print(json.dumps({'active': snapshot['active'], 'message': snapshot['message']}, ensure_ascii=True), flush=True)
    time.sleep(20)
snapshot = batch.snapshot()
engine.atomic_json(output / 'task-status.json', snapshot)
print(json.dumps(snapshot, ensure_ascii=True), flush=True)
if any(job['status'] != 'done' for job in snapshot['jobs']):
    raise SystemExit('Some modules failed; inspect retained outputs.')
