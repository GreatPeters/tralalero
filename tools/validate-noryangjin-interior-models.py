"""Fresh Blender imports/metrics/multiview evidence for the replacement assets."""
from pathlib import Path
from concurrent.futures import ThreadPoolExecutor, as_completed
import subprocess
import sys

BLENDER = Path('C:/Program Files/Blender Foundation/Blender 4.4/blender.exe')
SKILL = Path('C:/Users/ljh/.codex/skills/blender-asset-validation/scripts')
ROOT = Path(__file__).resolve().parents[1]
RUN = ROOT/'outputs/meshy-noryangjin-interior-v2-2026-09-28'
OUT = ROOT/'outputs/noryangjin-interior-v2-2026-09-28/asset-review'

def run(asset):
    folder=OUT/asset;folder.mkdir(parents=True,exist_ok=True)
    source=RUN/'models'/asset/('rigged.glb' if asset.startswith(('N13','N14')) else 'model1.glb')
    calls=[('metrics',SKILL/'inspect_asset.py',['--input',str(source),'--output',str(folder/'metrics.json')]),
           ('views',SKILL/'render_evidence.py',['--input',str(source),'--output-dir',str(folder/'views'),'--resolution','320'])]
    for name,script,options in calls:
        with (folder/(name+'.log')).open('w',encoding='utf-8') as log:
            result=subprocess.run([str(BLENDER),'--background','--factory-startup','--python-exit-code','1','--python',str(script),'--',*options],
                stdout=log,stderr=subprocess.STDOUT,timeout=600,creationflags=subprocess.CREATE_NO_WINDOW)
            if result.returncode: raise RuntimeError(f'{asset}/{name} failed; read its log')
    print(asset,'review artifacts ready',flush=True)

if __name__=='__main__':
    chosen=sys.argv[1:] or ['N09_driven_turret','N10_crab_aquarium','N11_fish_counter','N12_foam_box','N13_merchant_male','N14_merchant_female']
    with ThreadPoolExecutor(max_workers=2) as pool:
        jobs=[pool.submit(run,asset) for asset in chosen]
        for job in as_completed(jobs): job.result()
