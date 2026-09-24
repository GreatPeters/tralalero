"""Wait at most 45 seconds for the next real review gate; never approve it."""
import json,subprocess,sys,time
from pathlib import Path

ROOT=Path(__file__).resolve().parents[1];OUT=ROOT/'outputs/reststop-production-2026-09-24'
deadline=time.monotonic()+45
while time.monotonic()<deadline:
    try:
        gate=json.loads((OUT/'review-pending.json').read_text(encoding='utf8'))
        ready=gate.get('state')=='pending' and not Path(gate['result']).exists()
    except (OSError,ValueError,KeyError):ready=False
    if ready:
        result=subprocess.run([sys.executable,str(ROOT/'tools/make-reststop-review-sheet.py')],capture_output=True,text=True,encoding='utf8')
        if result.returncode:
            print(result.stderr,file=sys.stderr);raise SystemExit(result.returncode)
        sheet=json.loads(result.stdout);folder=Path(gate['folder']);validation=folder/'validation.json'
        technical=json.loads(validation.read_text(encoding='utf8')) if validation.exists() else {}
        print(json.dumps({'ready':True,'id':Path(gate['reference']).stem,'stage':gate['stage'],
            'folder':str(folder),'result':gate['result'],'sheet':sheet['sheet'],
            'hero':str(folder/'quality/hero.png'),'technical_ok':technical.get('ok'),
            'triangles':technical.get('triangles')}),flush=True)
        raise SystemExit(0)
    time.sleep(min(2,max(0,deadline-time.monotonic())))
state=json.loads((OUT/'status.json').read_text(encoding='utf8'))
print(json.dumps({'ready':False,'message':state.get('message'),'active':state.get('active')}),flush=True)
