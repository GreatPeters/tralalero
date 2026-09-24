"""Author, export-check and preview one candidate during a TRELLIS review pause.

This never writes visual approvals or adds a rig to the accepted manifest.
"""
import argparse
import json
from pathlib import Path
import subprocess

ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'outputs/reststop-production-2026-09-24'
ROLES={'H01':'ParkingMarshal','H02':'SnackChef','H03':'CoffeeVendor','H04':'Cashier',
       'H05':'Cleaner','H06':'FuelAttendant','H07':'Traveler','H08':'Police'}
parser=argparse.ArgumentParser()
parser.add_argument('--id',required=True,choices=ROLES)
parser.add_argument('--revision',required=True)
parser.add_argument('--source',required=True,type=Path)
args=parser.parse_args()
assert args.revision.startswith('r') and args.revision[1:].isdigit()
source=args.source.resolve();assert source.is_file()
folder=OUT/'rigged'/f'{args.id}-{args.revision}';name=ROLES[args.id]
assert not (folder/(name+'.blend')).exists(),'Use a new revision to preserve prior evidence'
settings=json.loads((ROOT/'map-concepts/reststop-production-2026-09-24/effective-settings.json').read_text(encoding='utf8'))
def check_gpu_window():
    gate=json.loads((OUT/'review-pending.json').read_text(encoding='utf8'))
    assert gate['state']=='pending' and not Path(gate['result']).exists(), 'Wait for an unapproved review gate before rendering'
check_gpu_window()
stages=[('rig','rig-reststop-proxy.py',['--source',str(source),'--output',str(folder),'--name',name]),
        ('verify','verify-reststop-rig.py',['--folder',str(folder),'--name',name]),
        ('compare','compare-reststop-export-poses.py',['--folder',str(folder),'--name',name]),
        ('video','render-reststop-rig-preview.py',['--folder',str(folder),'--name',name])]
for stage,script,arguments in stages:
    if stage=='video':check_gpu_window()
    label=f'{stage}-{args.id}-{args.revision}'
    command=[settings['blender'],'--background','--factory-startup','--python',str(ROOT/'tools/run-limited-generation.py'),
             '--','--script',str(ROOT/'tools'/script),'--record',str(OUT/(label+'-resource.json')),'--',*arguments]
    with (OUT/(label+'.log')).open('wb') as log:
        result=subprocess.run(command,stdout=log,stderr=subprocess.STDOUT,creationflags=getattr(subprocess,'CREATE_NO_WINDOW',0))
    print(label,result.returncode,flush=True)
    if result.returncode:raise SystemExit(result.returncode)
print('Candidate ready for main-agent visual review: '+str(folder),flush=True)
