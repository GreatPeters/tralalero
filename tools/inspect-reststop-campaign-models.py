"""Fresh technical inspection of the three existing RestStop rigs, without changing them."""
import json
from pathlib import Path
import subprocess

root=Path(__file__).resolve().parents[1]
blender=Path.home()/'AppData/Local/Programs/Blender Foundation/Blender 5.2/blender.exe'
inspector=Path.home()/'.codex/skills/blender-asset-validation/scripts/inspect_asset.py'
out=root/'tmp/campaign-balance-2026-09-23/reststop-model-inspection';out.mkdir(parents=True,exist_ok=True)
for role in ['ParkingMarshal','CoffeeVendor','SnackChef']:
    for kind in ['fbx','glb']:
        source=root/(f'Assets/ShooterSurvival/Models/Chapters/Mascots/{role}/{role}.fbx' if kind=='fbx' else f'outputs/approved-road-concepts-2026-09-13/humans/{role}/{role}.glb')
        target=out/(role+'-'+kind+'.json')
        if target.exists():continue
        with (out/(role+'-'+kind+'.log')).open('w',encoding='utf-8') as log:
            subprocess.run([str(blender),'--background','--factory-startup','--threads','4','--python-exit-code','1','--python',str(inspector),'--','--input',str(source),'--output',str(target)],stdout=log,stderr=subprocess.STDOUT,check=True,timeout=120,creationflags=subprocess.CREATE_NO_WINDOW)
        print(role,kind,'inspected',flush=True)
