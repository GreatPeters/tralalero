"""Follow the main render with export review and same-scale comparisons."""
import subprocess,sys,time,json
from pathlib import Path
root=Path.cwd();out=root/'outputs/reststop-blender-v2-2026-09-23';scripts=root/'tools/reststop-previs-v2';last=None
while not (out/'video-manifest.json').exists():
 receipts=list((out/'videos').glob('*-reststop.json'));state=[(p.name,p.stat().st_mtime_ns) for p in receipts]
 if state!=last:
  subprocess.run([sys.executable,str(scripts/'gallery.py')],check=True);last=state
 time.sleep(10)
blender=Path.home()/'AppData/Local/Programs/Blender Foundation/Blender 5.2/blender.exe'
for name in ['verify_export','compare']:
 with (out/(name+'.log')).open('w',encoding='utf8') as log:
  subprocess.run([str(blender),'-b','-t','4','--python-exit-code','1','--python','tools/run-limited-generation.py','--','--script',str(scripts/(name+'.py')),'--record',str(out/(name+'-resource.json'))],stdout=log,stderr=subprocess.STDOUT,check=True)
 print('POST_JOB_COMPLETE',name,flush=True)
for name in ['contact.py','review_videos.py','review_gimmicks.py','gallery.py']:
 subprocess.run([sys.executable,str(scripts/name)],check=True)
print('POST_PIPELINE_COMPLETE',flush=True)
