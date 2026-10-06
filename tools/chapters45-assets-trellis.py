"""Chapter 4/5 isolated TRELLIS canary and generation receipts; no shared settings edits."""
import argparse, hashlib, importlib, json, os, shutil, socket, subprocess, sys, time
from pathlib import Path

ROOT=Path(r'D:\Tralalero Shooter\Tralalero Shooter D')
OUT=ROOT/'outputs/chapters45-2026-10-02/assets'
APP=next(Path(r'C:\Users\ljh\Desktop').rglob('Trellis*/**/engine.py')).parent
sys.path.insert(0,str(APP));sys.path.insert(0,str(ROOT/'tools'))
engine=importlib.import_module('engine')
from reststop_prompt_wait import wait_with_history_recheck

def emit(name,value):
    engine.atomic_json(OUT/name,value)
    print(json.dumps({'receipt':name,**value},ensure_ascii=True),flush=True)

class Client(engine.TrellisClient):
    def wait(self,prompt_id,timeout,notify):
        return wait_with_history_recheck(self,super().wait,prompt_id,timeout,notify)

def ready(c):
    try:return c.request('/task-uv-raster-status',timeout=3).get('implementation')=='independent-uv-math-v1' and c.ready()
    except engine.BatchError:return False

def ensure(c):
    if ready(c):return
    with socket.socket() as s:
        if s.connect_ex(('127.0.0.1',8189))==0:raise RuntimeError('Unverified port owner; untouched')
    with (OUT/'backend.log').open('ab') as log:
        proc=subprocess.Popen([r'C:\AI\TRELLIS2-AMD\venv\Scripts\python.exe',str(ROOT/'tools/run-trellis-uv-math.py')],cwd=str(ROOT/'tools'),stdout=log,stderr=subprocess.STDOUT,creationflags=subprocess.CREATE_NO_WINDOW)
    emit('backend-owner.json',{'pid':proc.pid,'port':8189,'task_owned':True,'started_utc':time.strftime('%Y-%m-%dT%H:%M:%SZ',time.gmtime())})
    for i in range(120):
        if ready(c):return
        if proc.poll() is not None:raise RuntimeError('Backend exited: '+str(proc.returncode))
        time.sleep(2)
    raise RuntimeError('Backend startup timed out; inspect owned log')

def main():
    p=argparse.ArgumentParser();p.add_argument('mode',choices=['canary','generate']);p.add_argument('--input');p.add_argument('--asset',default='shoe-crown');args=p.parse_args()
    OUT.mkdir(parents=True,exist_ok=True)
    c=Client('http://127.0.0.1:8189',r'C:\AI\TRELLIS2-AMD');ensure(c)
    if args.mode=='canary':
        graph={'1':{'class_type':'EmptyImage','inputs':{'width':64,'height':64,'batch_size':1,'color':10461087}},'2':{'class_type':'PreviewImage','inputs':{'images':['1',0]}}}
        pid=c.submit(graph,'chapters45-canary');emit('canary-request.json',{'prompt_id':pid,'graph':graph})
        history=c.wait(pid,120,lambda x:None);emit('canary-result.json',{'prompt_id':pid,'status':history['status'],'history':history,'backend':c.request('/task-uv-raster-status')})
        return
    src=Path(args.input);folder=OUT/args.asset;folder.mkdir(exist_ok=True)
    if (folder/'generation-receipt.json').exists():raise RuntimeError('Existing generation receipt; inspect before any repeat')
    inp=OUT/'inputs';inp.mkdir(exist_ok=True)
    dest=inp/(args.asset+src.suffix.lower());shutil.copy2(src,dest)
    saved=json.loads((APP/'.state/settings.json').read_text(encoding='utf-8'))
    opts=engine.settings({**saved,'input_dir':str(inp),'output_dir':str(folder),'recursive':False,'resolution':1024,'texture_size':2048,'target_triangles':15000,'intermediate_faces':200000,'seed':45678,'steps':12,'texture_steps':25,'trellis_url':c.url,'keep_models_loaded':False})
    engine.atomic_json(folder/'effective-settings.json',opts)
    key='chapters45-'+args.asset+'-'+time.strftime('%Y%m%d-%H%M%S',time.gmtime())
    uploaded=c.upload(dest,key)
    graph=engine.build_prompt(json.loads((APP/'workflow_api.json').read_text(encoding='utf-8')),opts,uploaded,key)
    graph['194']['inputs']['remove_background']=True
    engine.atomic_json(folder/'generation-request.json',{'input':str(src),'input_sha256':hashlib.sha256(src.read_bytes()).hexdigest(),'graph':graph})
    pid=c.submit(graph,key);engine.atomic_json(folder/'generation-receipt.json',{'prompt_id':pid,'status':'submitted','key':key});print('SUBMITTED '+pid,flush=True)
    last=[None]
    def notify(msg):
        if last[0]!=msg:print(msg,flush=True);last[0]=msg
    try:history=c.wait(pid,7200,notify)
    except Exception as ex:
        engine.atomic_json(folder/'generation-failure.json',{'prompt_id':pid,'error':str(ex)});raise
    engine.atomic_json(folder/'generation-history.json',history)
    generated=c.output_path(history);shutil.copy2(generated,folder/'trellis-source.glb')
    engine.atomic_json(folder/'generation-receipt.json',{'prompt_id':pid,'status':'success','source':str(generated),'source_sha256':hashlib.sha256(generated.read_bytes()).hexdigest(),'backend':c.request('/task-uv-raster-status')})
    print('GENERATED '+str(folder/'trellis-source.glb'),flush=True)

if __name__=='__main__':main()
