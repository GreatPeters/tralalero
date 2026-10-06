import datetime,json,pathlib,shutil,subprocess,time,sys,hashlib
ROOT=pathlib.Path(r'D:\Tralalero Shooter\Tralalero Shooter D')
OUT=ROOT/'outputs/department-store-2026-10-02'
SNAP='outputs/chapter4-reference-2026-10-02/current-preferences-20261002T110600Z.tsv'
def write(name,v):(OUT/name).write_text(json.dumps(v,ensure_ascii=False,indent=2),encoding='utf-8')
def cmd(name,**params):
 a=[shutil.which('unity'),'command','--project-path',str(ROOT),'--timeout','50','--format','json',name]
 for k,v in params.items():a+=['--'+k,json.dumps(v,ensure_ascii=False) if isinstance(v,(dict,list,bool)) else str(v)]
 r=subprocess.run(a,capture_output=True,text=True,encoding='utf-8',timeout=60)
 d=json.loads(r.stdout)
 if r.returncode or not d.get('success'):raise RuntimeError(d)
 v=d['data']['result']
 if isinstance(v,dict) and v.get('success') is False:raise RuntimeError(v)
 return v.get('result',v) if name=='run_script' else v
def play(entry,*args):return cmd('run_script',file='tools/chapters45-playtest.cs',entry='Chapters45Playtest.'+entry,args=list(args))
if __name__=='__main__':
 results=[]
 cases=[('ShoeTower',0),('ShoeTower',1),('Jamsil',0),('Jamsil',1)]
 filename='ordinary-routes.json'
 if '--polish' in sys.argv: cases=[('ShoeTower',1),('Jamsil',0)];filename='ordinary-polish-routes.json'
 if '--followup' in sys.argv: cases=[('ShoeTower',0),('ShoeTower',1)];filename='followup-v4/ordinary-routes.json'
 for scene,choice in cases:
  prep=play('Prepare',scene,choice,SNAP); folder=pathlib.Path(prep['folder']); print(json.dumps({'started':scene,'choice':choice,'folder':str(folder)}),flush=True)
  try:
   cmd('editor_play');time.sleep(3);play('Begin');deadline=time.monotonic()+650;last=0
   while not (folder/'capture-status.json').exists():
    if time.monotonic()>deadline:raise TimeoutError(str(folder))
    if time.monotonic()-last>25 and (folder/'status.json').exists():
     s=json.loads((folder/'status.json').read_text(encoding='utf-8-sig'));print(json.dumps({k:s.get(k) for k in ['scene','seconds','distance','hp','outcome']}),flush=True);last=time.monotonic()
    time.sleep(1)
   summary=json.loads((folder/'summary.json').read_text(encoding='utf-8-sig'))
  finally:
   cmd('editor_stop');time.sleep(2);restored=play('Restore')
   if not restored.get('restored') or restored.get('mismatches')!=0:raise RuntimeError(restored)
  results.append({'scene':scene,'choice':choice,'folder':str(folder),'summary':summary,'restoration':restored});write(filename,results)
  print(json.dumps({'finished':scene,'choice':choice,'outcome':summary['outcome'],'seconds':summary['seconds'],'lifts':summary['lifts'],'restoredMismatches':restored['mismatches']}),flush=True)
  if summary['outcome']!='clear' or summary['errors'] or summary.get('animationWarnings'):raise RuntimeError('Route requires review')
 print('All requested ordinary routes passed; preferences restored.',flush=True)
