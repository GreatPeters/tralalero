"""Sequential reproducible rig/export checks while the TRELLIS queue finishes."""
import argparse,json,subprocess,time
from pathlib import Path
root=Path(__file__).resolve().parents[1]
blender=Path.home()/'AppData/Local/Programs/Blender Foundation/Blender 5.2/blender.exe'
production=root/'outputs/highway-enemy-rebuild-2026-09-23/production'
parser=argparse.ArgumentParser();parser.add_argument('--revision',default='v1');parser.add_argument('--rig-script',default='tools/rig-highway-rebuild.py');args=parser.parse_args()
output=root/'outputs/highway-enemy-rebuild-2026-09-23/rigged'/args.revision
evidence=root/'tmp/image-previews/highway-enemy-rebuild-2026-09-23'
work=root/'tmp/highway-enemy-rebuild-2026-09-23';work.mkdir(parents=True,exist_ok=True)
def run(script,args,log):
    with log.open('w',encoding='utf-8') as stream:
        subprocess.run([str(blender),'--background','--factory-startup','--python-exit-code','1','--python',str(root/script),'--',*map(str,args)],cwd=root,stdout=stream,stderr=subprocess.STDOUT,check=True)
for name in ['ConeMechanic','AsphaltWorker','TrafficPatrol','TireBruiser','DeliveryRider','TollgateChief']:
    while True:
        matches=list(production.glob('*_'+name+'_*/validation.json'))
        if matches:break
        (work/'rig-status.json').write_text(json.dumps({'name':name,'state':'waiting-for-generation'}));time.sleep(10)
    source=matches[0].parent/'model.glb';folder=output/name
    (work/'rig-status.json').write_text(json.dumps({'name':name,'state':'rigging'}))
    if not (folder/'rig-report.json').exists():run(args.rig_script,['--source',source,'--name',name,'--output',folder],work/(name+'-'+args.revision+'-rig.log'))
    for extension in ['fbx','glb']:
        check=evidence/(name+'-'+extension+'-'+args.revision)
        if (check/'inspection.json').exists():continue
        (work/'rig-status.json').write_text(json.dumps({'name':name,'state':'checking-'+extension}))
        params=['--input',folder/(name+'.'+extension),'--output',check]
        if extension=='glb':params.append('--render')
        run('tools/inspect-highway-rebuild.py',params,work/(name+'-'+extension+'-check.log'))
    print('READY '+name,flush=True)
(work/'rig-status.json').write_text(json.dumps({'state':'completed','count':6}))
