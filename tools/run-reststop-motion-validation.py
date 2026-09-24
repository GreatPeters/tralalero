"""Sequential creation, fresh import and fixed-camera evidence without touching live Unity assets."""
from pathlib import Path
import subprocess
root=Path(__file__).resolve().parents[1]
blender=Path.home()/'AppData/Local/Programs/Blender Foundation/Blender 5.2/blender.exe'
skill=Path.home()/'.codex/skills/blender-asset-validation/scripts'
base=root/'outputs/campaign-balance-2026-09-23/reststop-motion-v2'
evidence=root/'tmp/image-previews/campaign-balance-2026-09-23/reststop-motion-v2'
def run(script,args,log):
    log.parent.mkdir(parents=True,exist_ok=True)
    with log.open('w',encoding='utf-8') as stream:
        subprocess.run([str(blender),'--background','--factory-startup','--threads','4','--python-exit-code','1','--python',str(script),'--',*map(str,args)],cwd=root,stdout=stream,stderr=subprocess.STDOUT,check=True,timeout=120,creationflags=subprocess.CREATE_NO_WINDOW)
for role in ['ParkingMarshal','CoffeeVendor','SnackChef']:
    out=base/role;view=evidence/role
    if not (out/'motion-report.json').exists():run(root/'tools/refine-reststop-motion.py',['--role',role,'--revision','v2'],view/'generation.log')
    for kind in ['blend','fbx','glb']:
        path=out/(role+'.'+kind)
        if not (view/(kind+'-metrics.json')).exists():run(skill/'inspect_asset.py',['--input',path,'--output',view/(kind+'-metrics.json')],view/(kind+'-inspection.log'))
    for name,path,frames in [('before',out/'before-attack.blend','1,9,20,28,31'),('after',out/(role+'.blend'),'1,9,19,28,37'),('fresh-glb-clean',out/(role+'.glb'),'1,8,15,22,30')]:
        renderer=root/'tools/render-campaign-evidence.py' if path.suffix=='.glb' else skill/'render_evidence.py'
        if not (view/name/'evidence.json').exists():run(renderer,['--input',path,'--output-dir',view/name,'--resolution','256','--frames',frames],view/(name+'-render.log'))
    print(role,'motion/export/evidence complete',flush=True)
