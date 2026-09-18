"""Queue the reviewed mascot references on the verified installed TRELLIS2 backend."""
import importlib
import json
from pathlib import Path
import sys
import time
import urllib.request

root = Path(__file__).resolve().parent.parent
app = Path('C:/Users/ljh/Desktop/AI 프로그램/Trellis 자동화/앱')
sys.path.insert(0,str(app))
engine = importlib.import_module('engine')
with urllib.request.urlopen('http://127.0.0.1:8189/task-uv-raster-status',timeout=5) as response:
    status = json.load(response)
if status.get('implementation') != 'independent-uv-math-v1':
    raise RuntimeError('Expected verified UV backend; refusing another service.')
original = engine.build_prompt
def build(*values):
    graph = original(*values)
    graph['194']['inputs']['remove_background'] = True
    return graph
engine.build_prompt = build
output = root/'outputs/chapters-polish-2026-09-12/enemies'
options = engine.settings({'input_dir':str(root/'map-concepts/chapters-polish-2026-09-12/references/enemies'),
    'output_dir':str(output),'recursive':False,'resolution':1536,'texture_size':2048,
    'target_triangles':20000,'keep_models_loaded':False,'use_tiled_decoder':True,
    'quality_retry':False,'stage_quality':False,'remove_floor':False,'continue_on_error':True,
    'longest_side':1.6,'trellis_url':'http://127.0.0.1:8189'})
batch = engine.BatchEngine()
print(json.dumps(batch.start(options),ensure_ascii=True),flush=True)
while batch.active:
    snapshot = batch.snapshot()
    engine.atomic_json(output/'task-status.json',snapshot)
    print(json.dumps({'active':snapshot['active'],'message':snapshot['message']},ensure_ascii=True),flush=True)
    time.sleep(20)
engine.atomic_json(output/'task-status.json',batch.snapshot())
print(json.dumps(batch.snapshot(),ensure_ascii=True),flush=True)
