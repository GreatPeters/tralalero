"""Sequential official-CLI ordinary gameplay recheck with mandatory save restoration."""
import datetime as dt
import hashlib
import json
import pathlib
import shutil
import subprocess
import time

PROJECT = pathlib.Path(r'D:\Tralalero Shooter\Tralalero Shooter D')
UNITY = shutil.which('unity')
OUT = PROJECT / 'outputs/chapters45-2026-10-02' / ('final-verification-' + dt.datetime.now(dt.timezone.utc).strftime('%Y%m%dT%H%M%SZ'))
OUT.mkdir(parents=True, exist_ok=False)
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
        raise RuntimeError(f'{name}: {result.stdout}\n{result.stderr}')
    data = json.loads(result.stdout)
    if not data.get('success'):
        raise RuntimeError(data)
    value = data['data']['result']
    if isinstance(value, dict) and value.get('success') is False:
        raise RuntimeError(value)
    return value.get('result', value) if name == 'run_script' else value

def script(entry, *args):
    return command('run_script', file='tools/chapters45-playtest.cs', entry='Chapters45Playtest.' + entry, args=list(args))

def manifest():
    result = {}
    for base in ['Assets', 'Packages', 'ProjectSettings']:
        for path in (PROJECT / base).rglob('*'):
            if path.is_file():
                stat = path.stat()
                result[path.relative_to(PROJECT).as_posix()] = [stat.st_size, stat.st_mtime_ns]
    return result

def critical_hashes():
    paths = list((PROJECT / 'Assets/ShooterSurvival/Scripts').rglob('*.cs'))
    paths += [PROJECT / ('Assets/ShooterSurvival/Scenes/Tools/' + scene + '.unity') for scene in ['Jamsil', 'ShoeTower']]
    paths += list((PROJECT / 'ProjectSettings').glob('*'))
    return {p.relative_to(PROJECT).as_posix(): hashlib.sha256(p.read_bytes()).hexdigest() for p in paths if p.is_file()}

print(json.dumps({'evidenceRoot': str(OUT)}), flush=True)
write('product-file-metadata-before.json', manifest())
write('critical-hashes-before.json', critical_hashes())
branch = subprocess.run(['git', '-c', 'safe.directory=' + PROJECT.as_posix(), '-C', str(PROJECT), 'branch', '--show-current'], capture_output=True, text=True, check=True).stdout.strip()
write('conditions.json', {'utc': dt.datetime.now(dt.timezone.utc).isoformat(), 'branch': branch, 'source': 'final saved code, no product edits by this runner', 'ordinaryGameplay': True, 'driver': 'existing ordinary-input policy; animation warning capture added to QA reporting', 'routes': [['Jamsil', 0], ['Jamsil', 1], ['ShoeTower', 0], ['ShoeTower', 1]]})
results = []
try:
    for scene, choice in [('Jamsil', 0), ('Jamsil', 1), ('ShoeTower', 0), ('ShoeTower', 1)]:
        prepared = script('Prepare', scene, choice)
        folder = pathlib.Path(prepared['folder'])
        print(json.dumps({'prepared': str(folder)}), flush=True)
        try:
            command('editor_play')
            time.sleep(3)
            script('Begin')
            deadline = time.monotonic() + 650
            next_print = 0
            while not (folder / 'capture-status.json').exists():
                if time.monotonic() > deadline:
                    raise TimeoutError('Run did not finalize within watchdog margin: ' + str(folder))
                if time.monotonic() >= next_print and (folder / 'status.json').exists():
                    try:
                        status = json.loads((folder / 'status.json').read_text(encoding='utf-8-sig'))
                        print(json.dumps({k: status.get(k) for k in ['scene', 'routeChoice', 'seconds', 'distance', 'hp', 'outcome', 'finished', 'errors']}), flush=True)
                    except json.JSONDecodeError:
                        pass
                    next_print = time.monotonic() + 25
                time.sleep(2)
            time.sleep(2)
            summary = json.loads((folder / 'summary.json').read_text(encoding='utf-8-sig'))
        finally:
            command('editor_stop')
            time.sleep(3)
            restored = script('Restore')
            if not restored.get('restored') or restored.get('mismatches') != 0:
                raise RuntimeError('User preference restoration failed: ' + str(restored))
        row = {'scene': scene, 'choice': choice, 'folder': str(folder), 'summary': summary, 'restoration': restored}
        results.append(row)
        write('route-results.json', results)
        print(json.dumps({'completed': scene, 'choice': choice, 'outcome': summary['outcome'], 'seconds': summary['seconds'], 'errors': summary['errors'], 'restored': restored['restored']}), flush=True)
        if summary['outcome'] != 'clear' or summary['errors'] or summary.get('animationWarnings'):
            raise RuntimeError('Ordinary route requires investigation; remaining runs paused.')
finally:
    write('product-file-metadata-after.json', manifest())
    write('critical-hashes-after.json', critical_hashes())
    before = json.loads((OUT / 'product-file-metadata-before.json').read_text(encoding='utf-8'))
    after = json.loads((OUT / 'product-file-metadata-after.json').read_text(encoding='utf-8'))
    hbefore = json.loads((OUT / 'critical-hashes-before.json').read_text(encoding='utf-8'))
    hafter = json.loads((OUT / 'critical-hashes-after.json').read_text(encoding='utf-8'))
    write('product-boundary.json', {'metadataChanges': [key for key in sorted(before.keys() | after.keys()) if before.get(key) != after.get(key)], 'criticalHashChanges': [key for key in sorted(hbefore.keys() | hafter.keys()) if hbefore.get(key) != hafter.get(key)], 'completedRoutes': len(results)})
print(json.dumps({'finished': True, 'routes': len(results), 'evidenceRoot': str(OUT)}), flush=True)
