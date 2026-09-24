"""Reconstruct reviewed references with the installed local TRELLIS 2 engine."""
import argparse,importlib,json,sys,time,urllib.request
from pathlib import Path

parser=argparse.ArgumentParser()
parser.add_argument('--app-root',required=True)
parser.add_argument('--input',required=True)
parser.add_argument('--output',required=True)
args=parser.parse_args()
if not any(p.suffix.lower() in {'.png','.jpg','.jpeg','.webp'} for p in Path(args.input).iterdir()):
    raise RuntimeError('No generation references in input folder')
sys.path.insert(0,args.app_root)
engine=importlib.import_module('engine')
with urllib.request.urlopen('http://127.0.0.1:8189/task-uv-raster-status',timeout=5) as response:
    backend=json.load(response)
if backend.get('implementation')!='independent-uv-math-v1':raise RuntimeError('Unexpected backend')
original=engine.build_prompt
def build(*values):
    graph=original(*values);graph['194']['inputs']['remove_background']=True;return graph
engine.build_prompt=build
output=Path(args.output)
options=engine.settings({'input_dir':str(Path(args.input).resolve()),'output_dir':str(output.resolve()),
    'recursive':False,'resolution':1536,'texture_size':2048,'target_triangles':22000,
    'keep_models_loaded':False,'use_tiled_decoder':True,'quality_retry':False,'stage_quality':False,
    'remove_floor':False,'continue_on_error':True,'longest_side':3.0,'trellis_url':'http://127.0.0.1:8189'})
output.mkdir(parents=True,exist_ok=True)
engine.atomic_json(output/'backend.json',backend)
engine.atomic_json(output/'production-options.json',{k:options[k] for k in ['resolution','texture_size','target_triangles','longest_side','trellis_url']})
batch=engine.BatchEngine();print(json.dumps(batch.start(options),ensure_ascii=True),flush=True)
while batch.active:
    snapshot=batch.snapshot();engine.atomic_json(output/'task-status.json',snapshot)
    print(json.dumps({'active':snapshot['active'],'message':snapshot['message']},ensure_ascii=True),flush=True)
    time.sleep(20)
engine.atomic_json(output/'task-status.json',batch.snapshot());print(json.dumps(batch.snapshot(),ensure_ascii=True),flush=True)
