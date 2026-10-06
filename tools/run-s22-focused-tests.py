"""Run explicit Unity Editor suites sequentially and retain per-suite receipts."""
from pathlib import Path
import json,subprocess,shutil,time,sys
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'outputs/s22-polish-2026-10-01/tests';OUT.mkdir(exist_ok=True)
status=ROOT/'Temp/pipeline_test_status.json'
for suite in sys.argv[1:]:
 p=OUT/(suite+'.json')
 if p.exists():
  archive=OUT/'previous';archive.mkdir(exist_ok=True)
  shutil.copy2(p,archive/(suite+'-'+str(time.time_ns())+'.json'))
 request=subprocess.run([shutil.which('unity'),'command','--project-path',str(ROOT),'run_tests','--filter',suite,'--mode','EditMode','--async_tests','true','--format','json'],capture_output=True,text=True,encoding='utf-8',timeout=50)
 (OUT/(suite+'-request.json')).write_text(request.stdout,encoding='utf-8')
 if not json.loads(request.stdout).get('success'):raise RuntimeError(request.stdout)
 deadline=time.monotonic()+240
 while time.monotonic()<deadline:
  try:value=json.loads(status.read_text(encoding='utf-8-sig'))
  except (FileNotFoundError,ValueError):time.sleep(.4);continue
  if value.get('status')=='completed' and any(x.get('FullName','').startswith(suite+'.') for x in value.get('results',[])):break
  time.sleep(.4)
 else:raise TimeoutError(suite)
 p.write_text(json.dumps(value,indent=2),encoding='utf-8');print(suite,value['summary'],flush=True)
 if value['summary']['failed']:sys.exit(1)
