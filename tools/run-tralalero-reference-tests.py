"""Run Editor test suites one by one and keep receipts for the reference-fit work."""
import json,subprocess,shutil,time,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1];OUT=ROOT/'outputs/tralalero-reference-2026-10-01/tests';OUT.mkdir(parents=True,exist_ok=True)
status=ROOT/'Temp/pipeline_test_status.json';failed=0
for suite in sys.argv[1:]:
 started=time.time()
 r=subprocess.run([shutil.which('unity'),'command','--project-path',str(ROOT),'run_tests','--filter',suite,'--mode','EditMode','--async_tests','true','--format','json'],capture_output=True,text=True,encoding='utf-8',timeout=90)
 if not json.loads(r.stdout).get('success'):print(suite,'request failed',r.stdout[:400]);failed+=1;continue
 deadline=time.monotonic()+420;value=None
 while time.monotonic()<deadline:
  try:
   if status.stat().st_mtime<started:raise FileNotFoundError
   value=json.loads(status.read_text(encoding='utf-8-sig'))
  except (FileNotFoundError,ValueError):time.sleep(.5);continue
  if value.get('status')=='completed' and any(x.get('FullName','').startswith(suite+'.') for x in value.get('results',[])):break
  time.sleep(.5)
 else:print(suite,'timed out');failed+=1;continue
 (OUT/(suite+'.json')).write_text(json.dumps(value,indent=2),encoding='utf-8');print(suite,value['summary'],flush=True)
 for x in value.get('results',[]):
  if 'fail' in str(x.get('ResultState','')).lower():print('  FAIL',x.get('FullName'),(x.get('Message') or '')[:400])
 failed+=value['summary']['failed']
sys.exit(1 if failed else 0)
