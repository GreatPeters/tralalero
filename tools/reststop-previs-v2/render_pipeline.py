"""Run one Blender job at a time and overlap the lightweight video encoder."""
from pathlib import Path
import subprocess,sys,json,time
root=Path.cwd();out=root/'outputs/reststop-blender-v2-2026-09-23';scripts=root/'tools/reststop-previs-v2'
blender=Path.home()/'AppData/Local/Programs/Blender Foundation/Blender 5.2/blender.exe'
def job(name,script,*args):
 with (out/(name+'.log')).open('w',encoding='utf8') as log:
  subprocess.run([str(blender),'-b','-t','4','--python-exit-code','1','--python','tools/run-limited-generation.py','--','--script',str(scripts/script),'--record',str(out/(name+'-resource.json')),'--',*args],stdout=log,stderr=subprocess.STDOUT,check=True)
 print('JOB_COMPLETE',name,flush=True)
assert (out/'validation.json').exists()
job('repair-exit','repair_exit.py')
job('evidence-final2','render.py','--mode','evidence')
job('overview-final','render.py','--mode','overview')
with (out/'encode-final.log').open('w',encoding='utf8') as log:
 encoder=subprocess.Popen([sys.executable,str(scripts/'encode.py'),'--watch','--force-stages','01,02,03,04,05,06,07,08,09,10'],stdout=log,stderr=subprocess.STDOUT)
 try:job('animation-final','render.py','--mode','animation')
 except BaseException:
  encoder.terminate();raise
 if encoder.wait()!=0:raise RuntimeError('Encoding failed; inspect encode.log')
print('PIPELINE_COMPLETE',flush=True)
