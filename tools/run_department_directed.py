import json,pathlib,time,sys
from run_department_routes import ROOT,OUT,SNAP,cmd,play
if '--followup' in sys.argv:OUT=OUT/'followup-v4'
def write(name,v):(OUT/name).write_text(json.dumps(v,ensure_ascii=False,indent=2),encoding='utf-8')

results=[]
for scene,short,entry in [('ShoeTower','lifecycle','Chapters45Lifecycle'),('Jamsil','campaign-transactions','Chapters45CampaignTransactions')]:
    prepared=play('Prepare',scene,0,SNAP); folder=pathlib.Path(prepared['folder'])/short
    print(json.dumps({'starting':short,'folder':str(folder)}),flush=True)
    try:
        cmd('editor_play'); time.sleep(3)
        begin=cmd('run_script',file='tools/department-'+short+'.cs',entry=entry+'.Begin')
        deadline=time.monotonic()+115; last=0
        while not (folder/'summary.json').exists():
            if time.monotonic()>deadline: raise TimeoutError(str(folder))
            if time.monotonic()-last>15 and (folder/'status.json').exists():
                print((folder/'status.json').read_text(encoding='utf-8-sig'),flush=True);last=time.monotonic()
            time.sleep(1)
        summary=json.loads((folder/'summary.json').read_text(encoding='utf-8-sig'));time.sleep(2)
    finally:
        cmd('editor_stop');time.sleep(2);restoration=play('Restore')
        if not restoration.get('restored') or restoration.get('mismatches')!=0:raise RuntimeError(restoration)
    results.append({'fixture':short,'folder':str(folder),'summary':summary,'restoration':restoration})
    write('directed-fixtures.json',results)
    print(json.dumps({'finished':short,'summary':summary,'restored':restoration},ensure_ascii=False),flush=True)
    if summary.get('failures',0) or summary.get('errors'):raise RuntimeError('Directed fixture failed')
