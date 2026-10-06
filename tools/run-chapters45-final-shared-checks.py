"""Final native regressions and isolated lifecycle/contact checks; root-only editor owner."""
import json
import pathlib
import shutil
import subprocess
import sys
import time

PROJECT = pathlib.Path(r'D:\Tralalero Shooter\Tralalero Shooter D')
OUT = pathlib.Path(sys.argv[1]) / 'shared-regressions'
OUT.mkdir(parents=True, exist_ok=False)
UNITY = shutil.which('unity')
counter = 0

def write(name, value):
    (OUT / name).write_text(json.dumps(value, ensure_ascii=False, indent=2), encoding='utf-8')

def command(name, **params):
    global counter
    counter += 1
    args = [UNITY, 'command', '--project-path', str(PROJECT), '--timeout', '45', '--format', 'json', name]
    for key, value in params.items():
        args.extend(['--' + key, json.dumps(value, ensure_ascii=False) if isinstance(value, (list, dict, bool)) else str(value)])
    result = subprocess.run(args, capture_output=True, text=True, encoding='utf-8', timeout=58)
    (OUT / f'{counter:02d}-{name}-raw.json').write_text(result.stdout, encoding='utf-8')
    if result.returncode:
        raise RuntimeError(result.stdout + '\n' + result.stderr)
    data = json.loads(result.stdout)
    if not data.get('success'):
        raise RuntimeError(data)
    value = data['data']['result']
    if isinstance(value, dict) and value.get('success') is False:
        raise RuntimeError(value)
    return value.get('result', value) if name == 'run_script' else value

def playtest(entry, *args):
    return command('run_script', file='tools/chapters45-playtest.cs', entry='Chapters45Playtest.' + entry, args=list(args))

def restore():
    receipt = playtest('Restore')
    if not receipt.get('restored') or receipt.get('mismatches') != 0:
        raise RuntimeError('Save restoration failed: ' + str(receipt))
    return receipt

# Use the established snapshot implementation. Immediately restore selection
# before tests, then restore the same original snapshot again after tests.
snapshot = playtest('Prepare', 'ShoeTower', 0)
restore()
write('full-suite-snapshot.json', snapshot)
requested_at = time.time()
suite_finished = False
try:
    command('run_tests', mode='editor', async_tests=True)
    deadline = time.monotonic() + 300
    status_path = PROJECT / 'Temp/pipeline_test_status.json'
    next_print = 0
    while time.monotonic() < deadline:
        try:
            status = json.loads(status_path.read_text(encoding='utf-8-sig'))
        except (FileNotFoundError, json.JSONDecodeError):
            time.sleep(1)
            continue
        if time.monotonic() >= next_print:
            print(json.dumps({'fullSuiteStatus': status.get('status'), 'summary': status.get('summary')}), flush=True)
            next_print = time.monotonic() + 25
        if status_path.stat().st_mtime >= requested_at and status.get('status') == 'completed':
            write('full-editmode-final.json', status)
            suite_finished = True
            break
        if status.get('status') in ['error', 'failed']:
            raise RuntimeError(status)
        time.sleep(1)
    else:
        raise TimeoutError('Full native suite still running; inspect before further editor operations.')
finally:
    if suite_finished:
        write('full-suite-restored.json', restore())
    else:
        write('full-suite-restore-pending.json', {'reason': 'Confirm test execution has stopped before restoring the saved snapshot.', 'snapshot': snapshot})

fixtures = [
    ('ShoeTower', 'chapters45-lifecycle.cs', 'Chapters45Lifecycle', 'lifecycle'),
    ('Jamsil', 'chapters45-hazard-physics.cs', 'Chapters45HazardPhysics', 'hazard-physics'),
    ('ShoeTower', 'chapters45-hazard-physics.cs', 'Chapters45HazardPhysics', 'hazard-physics'),
    ('ShoeTower', 'chapters45-lift-disposal.cs', 'Chapters45LiftDisposal', 'lift-disposal'),
]
results = []
for scene, filename, typename, subdir in fixtures:
    prepared = playtest('Prepare', scene, 0)
    folder = pathlib.Path(prepared['folder'])
    print(json.dumps({'fixture': typename, 'scene': scene, 'folder': str(folder)}), flush=True)
    try:
        command('editor_play')
        time.sleep(3)
        command('run_script', file='tools/' + filename, entry=typename + '.Begin')
        deadline = time.monotonic() + 120
        while not (folder / subdir / 'summary.json').exists():
            if time.monotonic() > deadline:
                raise TimeoutError('Fixture watchdog margin reached: ' + str(folder))
            time.sleep(1)
        time.sleep(2)
        summary = json.loads((folder / subdir / 'summary.json').read_text(encoding='utf-8-sig'))
    finally:
        command('editor_stop')
        time.sleep(3)
        receipt = restore()
    results.append({'scene': scene, 'fixture': typename, 'folder': str(folder), 'summary': summary, 'restoration': receipt})
    write('fixture-results.json', results)
    print(json.dumps({'fixture': typename, 'outcome': summary['outcome'], 'checks': len(summary['checks']), 'failures': summary['failures'], 'errors': summary['errors']}), flush=True)
    if summary.get('failures') or summary.get('errors') or summary.get('outcome') != 'passed':
        raise RuntimeError('Directed fixture requires investigation.')
print(json.dumps({'finished': True, 'fixtures': len(results), 'evidenceRoot': str(OUT)}), flush=True)
