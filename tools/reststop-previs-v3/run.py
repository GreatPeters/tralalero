import subprocess,sys
from pathlib import Path
root=Path.cwd();out=root/'outputs/reststop-blender-v3-2026-09-23';blender=Path.home()/'AppData/Local/Programs/Blender Foundation/Blender 5.2/blender.exe'
for name in ['revise','validate','render']:
 with (out/(name+'.log')).open('w',encoding='utf8') as log:
  subprocess.run([str(blender),'-b','-t','4','--python-exit-code','1','--python','tools/run-limited-generation.py','--','--script','tools/reststop-previs-v3/'+name+'.py','--record',str(out/(name+'-resource.json'))],stdout=log,stderr=subprocess.STDOUT,check=True)
 print('COMPLETE',name,flush=True)
