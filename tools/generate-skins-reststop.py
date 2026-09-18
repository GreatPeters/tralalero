"""Use the installed TRELLIS automation with task-specific output and budgets."""
import argparse
import importlib
import json
from pathlib import Path
import sys
import time
import subprocess
import urllib.request

parser=argparse.ArgumentParser()
parser.add_argument('--kind',choices=['footwear','headwear','reststop'],required=True)
parser.add_argument('--prepare-only',action='store_true')
args=parser.parse_args()
root=Path(__file__).resolve().parent.parent
app=Path('C:/Users/ljh/Desktop/AI 프로그램/Trellis 자동화/앱')
sys.path.insert(0,str(app))
engine=importlib.import_module('engine')
original=engine.build_prompt
def build(*values):
    graph=original(*values);graph['194']['inputs']['remove_background']=True;return graph
engine.build_prompt=build
class TaskClient(engine.TrellisClient):
    def ensure(self,notify,stop):
        def checked():
            try:
                state=self.request('/task-uv-raster-status',timeout=3)
                return state.get('implementation')=='independent-uv-math-v1' and super(TaskClient,self).ready()
            except engine.BatchError:return False
        if checked():return
        import socket
        with socket.socket() as connection:
            if connection.connect_ex(('127.0.0.1',8189))==0:
                raise engine.BatchError('Task port8189 is occupied by an unverified backend')
        logs=root/'map-concepts/skins-reststop-2026-09-12';logs.mkdir(parents=True,exist_ok=True)
        with (logs/'trellis-uv-math-server.log').open('ab') as stream:
            process=subprocess.Popen([str(Path('C:/AI/TRELLIS2-AMD/venv/Scripts/python.exe')),str(root/'tools/run-trellis-uv-math.py')],
                cwd=str(root),stdout=stream,stderr=subprocess.STDOUT,creationflags=getattr(subprocess,'CREATE_NO_WINDOW',0))
        engine.atomic_json(logs/'task-trellis-server-owner.json',{'launcherPid':process.pid,'port':8189,'startedByTask':True})
        for _ in range(120):
            if checked():return
            if process.poll() is not None:raise engine.BatchError('Independent UV backend failed; inspect task log')
            if stop.is_set():raise engine.BatchError('Stopped while starting backend')
            notify('독립 UV 계산 경로를 준비하고 있습니다');time.sleep(2)
        raise engine.BatchError('Independent UV backend startup timeout')

out=root/'outputs/skins-reststop-2026-09-12/production'/args.kind
opts=engine.settings({'input_dir':str(root/'map-concepts/skins-reststop-2026-09-12/references'/args.kind),
    'output_dir':str(out),'recursive':False,'resolution':1536,'texture_size':2048,
    'target_triangles':15000 if args.kind!='reststop' else 35000,
    'keep_models_loaded':False,'use_tiled_decoder':True,'quality_retry':False,'stage_quality':False,
    'remove_floor':False,'continue_on_error':True,'longest_side':1.0 if args.kind!='reststop' else 8.0,'trellis_url':'http://127.0.0.1:8189'})
if args.prepare_only:
    import threading
    TaskClient(opts['trellis_url'],opts['trellis_root']).ensure(print,threading.Event())
    print('Independent UV backend ready',flush=True);raise SystemExit(0)
batch=engine.BatchEngine(client_factory=TaskClient);print(json.dumps(batch.start(opts),ensure_ascii=True),flush=True)
while batch.active:
    snapshot=batch.snapshot();engine.atomic_json(out/'task-status.json',snapshot)
    print(json.dumps({'active':snapshot['active'],'message':snapshot['message']},ensure_ascii=True),flush=True)
    time.sleep(20)
snapshot=batch.snapshot();engine.atomic_json(out/'task-status.json',snapshot)
print(json.dumps(snapshot,ensure_ascii=True),flush=True)
