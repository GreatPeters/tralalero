"""Run related suites sequentially through official CLI async test commands."""
import json
import argparse
from pathlib import Path
import shutil
import subprocess
import time

root=Path(__file__).resolve().parent.parent
project=root/'tmp/q'
output=project/'map-concepts/chapters-polish-2026-09-12/tests'
parser=argparse.ArgumentParser()
parser.add_argument('--run', help='Separate result directory; retains previous test evidence')
parser.add_argument('--suite', action='append')
args=parser.parse_args()
if args.run:
    if Path(args.run).name!=args.run or args.run in {'.','..'}:raise ValueError('Use one result directory name')
    output=output/args.run
output.mkdir(parents=True,exist_ok=True)
unity=shutil.which('unity')
if not unity:raise RuntimeError('Unity CLI unavailable')
def command(*args):
    result=subprocess.run([unity,'command','--project-path',str(project),'--timeout','20','--format','json',*args],capture_output=True,text=True,encoding='utf-8',timeout=35)
    if result.returncode:raise RuntimeError(result.stdout+'\n'+result.stderr)
    return json.loads(result.stdout)
summary=[]
for suite in args.suite or ['ChapterEquipmentRevisionTests','HighwayChapterIntegrationTests','EncounterPlacementTablesTests','OpeningMovieTimingTests','RewardedCoinOfferTests','MapToolChapterControlsTests','GameDataWorkbookTests']:
    result_path=output/(suite+'.json')
    if result_path.exists():
        completed=json.loads(result_path.read_text(encoding='utf-8'))
        summary.append({'suite':suite,**completed['summary']});continue
    request_path=output/(suite+'-request.json')
    if not request_path.exists():
        command('save_all')
        started=command('run_tests','--mode','editor','--filter',suite,'--async_tests','true')
        request_path.write_text(json.dumps(started,indent=2),encoding='utf-8')
    deadline=time.monotonic()+180
    while time.monotonic()<deadline:
        try:status=json.loads((project/'Temp/pipeline_test_status.json').read_text(encoding='utf-8-sig'))
        except (FileNotFoundError,json.JSONDecodeError):time.sleep(.5);continue
        if status.get('status')=='error':raise RuntimeError(json.dumps(status))
        if status.get('status')=='completed' and any(r.get('FullName','').startswith(suite+'.') for r in status.get('results',[])):
            break
        time.sleep(.5)
    else:raise TimeoutError('Test suite did not finish: '+suite)
    (output/(suite+'.json')).write_text(json.dumps(status,indent=2),encoding='utf-8')
    row={'suite':suite,**status['summary']};summary.append(row);print(json.dumps(row),flush=True)
(output/'summary.json').write_text(json.dumps(summary,indent=2),encoding='utf-8')
if any(row['failed'] for row in summary):raise SystemExit(1)
