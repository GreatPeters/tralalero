"""Sequential official-Pipeline real-time coverage; saves remain in the task snapshot."""
import json,subprocess,sys,time,shutil
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'outputs/s22-polish-2026-10-01'
UNITY=shutil.which('unity')
def script(method,*args):
 r=subprocess.run([sys.executable,str(ROOT/'tools/audit-s22-driver.py'),method,*[json.dumps(x) for x in args]],cwd=ROOT,capture_output=True,text=True,encoding='utf-8',timeout=155)
 if r.returncode:raise RuntimeError(r.stdout+r.stderr)
 return r.stdout
def command(name):
 r=subprocess.run([UNITY,'command','--project-path',str(ROOT),name,'--format','json'],capture_output=True,text=True,encoding='utf-8',timeout=65)
 if r.returncode:raise RuntimeError(r.stdout+r.stderr)
 return json.loads(r.stdout)
def wait_result(folder,timeout):
 deadline=time.monotonic()+timeout
 while time.monotonic()<deadline:
  for fail in ('harness-error.txt','error.txt'):
   if (folder/fail).exists():raise RuntimeError((folder/fail).read_text(encoding='utf-8'))
  if (folder/'result.json').exists():return json.loads((folder/'result.json').read_text(encoding='utf-8'))
  time.sleep(2)
 raise TimeoutError(str(folder))
def start(scene):
 command('editor_stop');time.sleep(1)
 script('tools/s22-polish-verification.cs:S22PolishVerification.Prepare',scene,False)
 command('editor_play');time.sleep(3)
results={}
try:
 label='s22-verified-inside-1x'
 script('tools/verify-noryangjin-revamp-fix.cs:VerifyNoryangjinRevampFix.Begin',label,0,True,1)
 script('tools/log-s22-play.cs:S22PlayLog.Main',label)
 results['inside']=wait_result(ROOT/'tmp/image-previews/noryangjin-revamp-fix-2026-09-28'/('play-'+label),650)
 (OUT/'route-validation.json').write_text(json.dumps(results,indent=2),encoding='utf-8');print('Inside',results['inside']['elapsed'],flush=True)
 start('Noryangjin_MapTool_Mode_SR18_Revamp');label='s22-verified-outside-1x'
 script('tools/log-s22-play.cs:S22PlayLog.Main',label)
 script('tools/verify-noryangjin-revamp-fix.cs:VerifyNoryangjinRevampFix.Begin',label,1,True,1)
 results['outside']=wait_result(ROOT/'tmp/image-previews/noryangjin-revamp-fix-2026-09-28'/('play-'+label),650)
 (OUT/'route-validation.json').write_text(json.dumps(results,indent=2),encoding='utf-8');print('Outside',results['outside']['elapsed'],flush=True)
 start('RestStop');label='s22-verified-reststop-1x'
 script('tools/log-s22-play.cs:S22PlayLog.Main',label)
 script('tools/reststop-korean-playtest.cs:RestStopKoreanPlaytest.Begin',label,0,320,-1.9,True)
 results['reststop']=wait_result(ROOT/'outputs/meshy-reststop-2026-09-25/live'/label,500)
 (OUT/'route-validation.json').write_text(json.dumps(results,indent=2),encoding='utf-8');print('RestStop',results['reststop']['elapsed'],flush=True)
 command('editor_stop');time.sleep(1)
 (OUT/'route-validation-complete.txt').write_text('Real-time coverage complete; inspect outcomes, not all clears.',encoding='utf-8')
except Exception as e:
 (OUT/'route-validation-error.txt').write_text(str(e),encoding='utf-8');raise
