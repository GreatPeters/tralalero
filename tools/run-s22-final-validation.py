"""Final real-time routes, runtime metrics, focused tests and an Android build."""
from pathlib import Path
import json,subprocess,sys,time,shutil
ROOT=Path(__file__).resolve().parents[1];OUT=ROOT/'outputs/s22-polish-2026-10-01';UNITY=shutil.which('unity')
def script(entry,*args):
 r=subprocess.run([sys.executable,str(ROOT/'tools/audit-s22-driver.py'),entry,*[json.dumps(a) for a in args]],capture_output=True,text=True,encoding='utf-8',cwd=ROOT,timeout=150)
 if r.returncode:raise RuntimeError(r.stdout+r.stderr)
 return json.loads(r.stdout).get('result')
def command(name):
 r=subprocess.run([UNITY,'command','--project-path',str(ROOT),name,'--format','json'],capture_output=True,text=True,encoding='utf-8',timeout=65)
 if r.returncode:raise RuntimeError(r.stdout+r.stderr)
 return json.loads(r.stdout)
def wait_file(path,seconds=500):
 end=time.monotonic()+seconds
 while time.monotonic()<end:
  if path.exists():return path.read_text(encoding='utf-8-sig')
  if (path.parent/'harness-error.txt').exists():raise RuntimeError((path.parent/'harness-error.txt').read_text(encoding='utf-8'))
  time.sleep(2)
 raise TimeoutError(str(path))
def fresh(scene='Noryangjin_MapTool_Mode_SR18_Revamp'):
 command('editor_stop');time.sleep(1);script('tools/s22-polish-verification.cs:S22PolishVerification.Prepare',scene,False);command('editor_play');time.sleep(3)
records={}
def save(): (OUT/'final-runtime.json').write_text(json.dumps(records,indent=2,ensure_ascii=False),encoding='utf-8')
try:
 script('tools/capture-s22-motion.cs:S22MotionReview.Shoes');time.sleep(3)
 script('tools/s22-polish-verification.cs:S22PolishVerification.Subject','Noryangjin_Player/Original','player-final')
 for label,route in [('applied-inside',0),('applied-outside',1)]:
  fresh();script('tools/log-s22-play.cs:S22PlayLog.Main',label)
  start=script('tools/claude-noryangjin-run.cs:ClaudeNoryangjinRun.Begin',label,route,1,5)
  result=wait_file(ROOT/start['folder']/'result.txt',650)
  records[label]=dict(folder=start['folder'],result=result);save();print(label,result,flush=True)
 fresh('HighWay');script('tools/log-s22-play.cs:S22PlayLog.Main','applied-highway');script('tools/s22-polish-verification.cs:S22PolishVerification.Start');script('tools/s22-polish-verification.cs:S22PolishVerification.Watch','applied-highway',110)
 records['highway']=json.loads(wait_file(OUT/'applied-highway-watch.json',250));save();print('Highway observed',records['highway']['elapsed'],flush=True)
 command('editor_stop');time.sleep(1)
 script('tools/verify-s22-native-assets.cs:S22NativeAssets.Main')
 suites=['NoryangjinCameraOcclusionTests','NoryangjinGradeSeparationTests','MapToolOpeningVideoTests','OpeningMovieTimingTests','CosmeticShopIntegrationTests','MobileRenderingQualityTests','SharkTailFootRigTests']
 r=subprocess.run([sys.executable,'tools/run-s22-focused-tests.py',*suites],cwd=ROOT,timeout=550)
 if r.returncode:raise RuntimeError('Final focused tests failed')
 fresh();script('tools/probe-s22-cameras.cs:S22Cameras.Focus');script('tools/s22-polish-verification.cs:S22PolishVerification.Sample','optimized-final-start',90,240)
 records['timing']=json.loads(wait_file(OUT/'optimized-final-start-timing.json',80));records['render']=script('tools/s22-polish-verification.cs:S22PolishVerification.Live','optimized-final-start');save()
 command('editor_stop');time.sleep(1)
 records['build']=script('tools/build-s22-android.cs:S22AndroidBuild.Main',False);save();print('Android build scheduled',flush=True)
except Exception as ex:
 (OUT/'final-validation-error.txt').write_text(str(ex),encoding='utf-8');raise
