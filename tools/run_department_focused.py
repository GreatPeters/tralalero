import hashlib,json,pathlib,shutil,time,sys
from run_department_routes import ROOT,OUT,SNAP,cmd,play
BASE_OUT=OUT
if '--followup' in sys.argv:OUT=OUT/'followup-v4'
def write(name,v):(OUT/name).write_text(json.dumps(v,ensure_ascii=False,indent=2),encoding='utf-8')
def observe(entry='Main',args=None):return cmd('run_script',file='tools/department-observe.cs',entry='DepartmentObserve.'+entry,**({'args':args} if args else {}))
def status():
    value=cmd('test_status')
    return json.loads(value) if isinstance(value,str) else value
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
manifest=json.loads((BASE_OUT/'recovery-manifest.json').read_text(encoding='utf-8'))
shared=[r for r in manifest if not r['path'].endswith('/Jamsil.unity') and not r['path'].endswith('/ShoeTower.unity')]
if any(sha(ROOT/r['path'])!=r['sha256'] or sha(pathlib.Path(r['recoveryPath']))!=r['sha256'] for r in shared):raise RuntimeError('Shared state no longer matches verified local recovery')
before=observe();write('focused-state-before.json',before)
prep=play('Prepare','ShoeTower',0,SNAP)
try:
    request=cmd('run_tests',mode='editor',filter='Chapter4',async_tests=True,timeout=120);write('focused-request.json',request)
    deadline=time.monotonic()+150
    while True:
        state=status();write('focused-test-status.json',state)
        print(json.dumps({'testStatus':state.get('status'),'summary':state.get('summary')}),flush=True)
        if state.get('status') not in ['running','pending','queued']:break
        if time.monotonic()>deadline:raise TimeoutError('Tests did not finish')
        time.sleep(2)
finally:
    # Always finish polling before restoration, including an orchestration error.
    pending=status(); cleanup_deadline=time.monotonic()+150
    while pending.get('status') in ['running','pending','queued']:
        if time.monotonic()>cleanup_deadline:raise TimeoutError('Still running: do not restore concurrently with tests')
        time.sleep(2);pending=status()
    restored=play('Restore');write('focused-preferences-restored.json',restored)
    current=observe()
    if (current['editorCleanupExists'],current['editorCleanup'])!=(before['editorCleanupExists'],before['editorCleanup']):
        current=observe('RestoreCleanup',[before['editorCleanupExists'],before['editorCleanup']])
    write('focused-state-after.json',current)
    changed=[r for r in shared if sha(ROOT/r['path'])!=r['sha256']]
    for row in changed:
        assert sha(pathlib.Path(row['recoveryPath']))==row['sha256']
        shutil.copy2(row['recoveryPath'],ROOT/row['path'])
    if changed:observe('Reimport',[[r['path'] for r in changed if r['path'].startswith('Assets/') and not r['path'].endswith('.meta')]])
    mismatch=[r['path'] for r in shared if sha(ROOT/r['path'])!=r['sha256']]
    write('focused-assets-restored.json',{'changedDuringTests':[r['path'] for r in changed],'remainingMismatches':mismatch,'verified':len(shared)})
    if restored['mismatches'] or mismatch:raise RuntimeError('Restoration mismatch')
if state.get('status')!='completed' or state.get('summary',{}).get('failed')!=0:raise RuntimeError('Focused test result requires review')
