"""Task-owned sequential TRELLIS2 generation using the installed automation engine."""
import importlib,json,socket,subprocess,sys,threading,time
from pathlib import Path

ROOT=Path(__file__).resolve().parents[1]
WORK=ROOT/'map-concepts/harbor-opening-refinement-2026-09-16'
OUT=ROOT/'outputs/harbor-opening-refinement-2026-09-16/trellis'
sys.path.insert(0,'C:/Users/ljh/Desktop/AI 프로그램/Trellis 자동화/앱')
engine=importlib.import_module('engine')
original=engine.build_prompt
def build(*args):
    graph=original(*args);graph['194']['inputs']['remove_background']=True;return graph
engine.build_prompt=build

class TaskClient(engine.TrellisClient):
    def ensure(self,notify,stop):
        def ready():
            try:
                state=self.request('/task-uv-raster-status',timeout=3)
                return state.get('implementation')=='independent-uv-math-v1' and super(TaskClient,self).ready()
            except engine.BatchError:return False
        if not ready():
            with socket.socket() as connection:
                if connection.connect_ex(('127.0.0.1',8189))==0:raise engine.BatchError('Unverified occupied task backend')
            with (WORK/'trellis-server.log').open('ab') as log:
                process=subprocess.Popen(['C:/AI/TRELLIS2-AMD/venv/Scripts/python.exe',str(ROOT/'tools/run-trellis-uv-math.py')],cwd=ROOT,stdout=log,stderr=subprocess.STDOUT,creationflags=subprocess.CREATE_NO_WINDOW)
            engine.atomic_json(WORK/'trellis-server-owner.json',dict(pid=process.pid,startedByTask=True,port=8189))
            for _ in range(120):
                if ready():break
                if process.poll() is not None:raise engine.BatchError('Backend exited; see retained log')
                if stop.is_set():raise engine.BatchError('Cancelled')
                time.sleep(2)
            else:raise engine.BatchError('Backend startup timed out')
        queue=self.request('/queue',timeout=5)
        if queue.get('queue_running') or queue.get('queue_pending'):raise engine.BatchError('Backend has existing work')

options=engine.settings(dict(input_dir=str(WORK/'references'),output_dir=str(OUT),recursive=False,
    resolution=1536,texture_size=2048,target_triangles=25000,longest_side=6.0,
    keep_models_loaded=False,use_tiled_decoder=True,quality_retry=False,stage_quality=False,
    remove_floor=False,continue_on_error=True,trellis_url='http://127.0.0.1:8189',
    blender='C:/Program Files/Blender Foundation/Blender 4.4/blender.exe'))
batch=engine.BatchEngine(client_factory=TaskClient)
print(json.dumps(batch.start(options)),flush=True)
while batch.active:
    status=batch.snapshot();engine.atomic_json(OUT/'task-status.json',status)
    print(json.dumps(dict(active=status['active'],message=status['message']),ensure_ascii=True),flush=True);time.sleep(20)
status=batch.snapshot();engine.atomic_json(OUT/'task-status.json',status)
if any(job['status']!='done' for job in status['jobs']):raise SystemExit('Generation incomplete; inspect task-status.json')
print('COMPLETE',flush=True)
