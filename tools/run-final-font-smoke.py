import json,pathlib,shutil,subprocess,time
root=pathlib.Path(r'D:\Tralalero Shooter\Tralalero Shooter D')
out=root/'outputs/chapters45-2026-10-02/final-verification-20261001T212400Z'
def call(name,**kwargs):
    args=[shutil.which('unity'),'command','--project-path',str(root),'--format','json',name]
    for k,v in kwargs.items():args.extend(['--'+k,json.dumps(v) if isinstance(v,list) else str(v)])
    p=subprocess.run(args,capture_output=True,text=True,encoding='utf-8',timeout=55)
    result=json.loads(p.stdout)
    if p.returncode or not result.get('success'):raise RuntimeError(p.stdout+p.stderr)
    data=result['data']['result']
    if isinstance(data,dict) and data.get('success') is False:raise RuntimeError(data)
    return data.get('result',data) if name=='run_script' else data
def qa(entry,*args):return call('run_script',file='tools/chapters45-playtest.cs',entry='Chapters45Playtest.'+entry,args=list(args))
call('run_script',file='tools/chapter45-refresh.cs',entry='Chapter45Refresh.Main')
prepared=qa('Prepare','ShoeTower',0); folder=pathlib.Path(prepared['folder']);print(json.dumps(prepared),flush=True)
try:
    call('editor_play');time.sleep(3)
    begun=call('run_script',file='tools/chapters45-visual-survey.cs',entry='Chapters45VisualSurvey.Begin')
    print(json.dumps(begun),flush=True)
    deadline=time.monotonic()+145
    report=folder/'visual-survey/summary.json'
    while not report.exists():
        if time.monotonic()>deadline:raise TimeoutError(str(report))
        time.sleep(2)
    time.sleep(2)
    summary=json.loads(report.read_text(encoding='utf-8-sig'));print(json.dumps(summary),flush=True)
finally:
    call('editor_stop');time.sleep(3)
    restored=qa('Restore');print(json.dumps(restored),flush=True)
    if not restored.get('restored') or restored.get('mismatches')!=0:raise RuntimeError(restored)
(out/'font-restored-native-smoke.json').write_text(json.dumps({'folder':str(folder),'summary':summary,'restoration':restored},indent=2),encoding='utf-8')
assert summary['outcome']=='captured' and not summary['errors']
