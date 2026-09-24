"""Run the installed, saved TRELLIS automation with task-owned inputs and review gates."""
import argparse
import importlib
import json
import os
from pathlib import Path
import shutil
import socket
import subprocess
import sys
import time

ROOT = Path(__file__).resolve().parents[1]
APP = Path('C:/Users/ljh/Desktop/AI 프로그램/Trellis 자동화/앱')
DOC = ROOT / 'map-concepts/reststop-production-2026-09-24'
OUT = ROOT / 'outputs/reststop-production-2026-09-24'
INPUT = OUT / 'inputs'
sys.path.insert(0, str(APP))
engine = importlib.import_module('engine')
from quality_staged import run_staged, Paused
from quality_review import normalize_review
from reststop_prompt_wait import wait_with_history_recheck

parser = argparse.ArgumentParser()
parser.add_argument('--only', default='')
parser.add_argument('--defer', default='', help='Explicit halted asset IDs being repaired separately; preserve their failure records')
parser.add_argument('--resume', action='store_true', help='Explicitly release the saved reboot pause; keep all generation ledgers')
args = parser.parse_args()
pause_path = OUT / 'pause-request.json'
if pause_path.exists():
    if not args.resume:
        parser.error('Production is paused for reboot. Read map-concepts/reststop-production-2026-09-24/REBOOT-RESUME.md; use --resume only when resuming is requested.')
    # A live batch must not have its pause request removed by a second launcher.
    with engine.OutputLock(OUT / 'assets/.trellis-automation/batch.lock'):
        pause_path.unlink()

def pause_at_boundary():
    if pause_path.exists():
        batch.pause()
        raise Paused()
deferred_ids = {value.strip() for value in args.defer.split(',') if value.strip()}
for folder in (DOC, OUT, INPUT, OUT / 'reviews'):
    folder.mkdir(parents=True, exist_ok=True)
saved_path = APP / '.state/settings.json'
if not (DOC / 'saved-settings.json').exists():
    shutil.copy2(saved_path, DOC / 'saved-settings.json')
saved = json.loads((DOC / 'saved-settings.json').read_text(encoding='utf8'))
opts = engine.settings({**saved, 'input_dir': str(INPUT), 'output_dir': str(OUT / 'assets'),
                       'recursive': False, 'trellis_url': 'http://127.0.0.1:8189'})
engine.atomic_json(DOC / 'effective-settings.json', opts)

class TaskClient(engine.TrellisClient):
    def wait(self, prompt_id, timeout, notify):
        return wait_with_history_recheck(self, super().wait, prompt_id, timeout, notify)

    def ensure(self, notify, stop):
        pause_at_boundary()
        def ready():
            try:
                status = self.request('/task-uv-raster-status', timeout=3)
                return status.get('implementation') == 'independent-uv-math-v1' and super(TaskClient, self).ready()
            except engine.BatchError:
                return False
        if ready():
            return
        with socket.socket() as connection:
            if connection.connect_ex(('127.0.0.1', 8189)) == 0:
                raise engine.BatchError('Port 8189 belongs to an unverified backend; left untouched.')
        with (OUT / 'backend.log').open('ab') as log:
            process = subprocess.Popen([str(Path('C:/AI/TRELLIS2-AMD/venv/Scripts/python.exe')),
                str(ROOT / 'tools/run-trellis-uv-math.py')], cwd=ROOT, stdout=log, stderr=subprocess.STDOUT,
                creationflags=getattr(subprocess, 'CREATE_NO_WINDOW', 0))
        engine.atomic_json(OUT / 'backend-owner.json', {'pid': process.pid, 'port': 8189, 'task_owned': True})
        for _ in range(120):
            if ready():
                return
            if process.poll() is not None:
                raise engine.BatchError('Backend exited; inspect backend.log.')
            notify('Preparing existing independent-UV backend')
            if stop.wait(2):
                raise engine.BatchError('Stopped while starting backend')
        raise engine.BatchError('Backend startup timeout')

def review_gate(reference, folder, blender, notify, stage, source_folder=None):
    """Retain original staged quality policy; the main agent supplies each visual verdict."""
    pause_at_boundary()
    quality = Path(folder) / 'quality'
    quality.mkdir(parents=True, exist_ok=True)
    result = quality / 'main-agent-review.json'
    if result.exists():
        return normalize_review(json.loads(result.read_text(encoding='utf8')))
    images = [quality / (name + '.png') for name in ('hero', 'top', 'opposite', 'front', 'neutral')]
    if not all(p.is_file() for p in images):
        with (quality / 'render.log').open('w', encoding='utf8') as log:
            subprocess.run([str(blender), '--background', '--factory-startup', '--threads', '4',
                '--python-exit-code', '2', '--python', str(ROOT / 'tools/reststop-production-review-render.py'), '--', str(folder)]
                + (['--shape'] if stage == 'shape' else []), stdout=log, stderr=subprocess.STDOUT,
                timeout=300, check=True, creationflags=getattr(subprocess, 'CREATE_NO_WINDOW', 0))
    if not all(p.is_file() for p in images):
        raise RuntimeError('Review images missing')
    if source_folder:
        images.extend(Path(source_folder) / 'quality' / (n + '.png') for n in ('hero', 'neutral'))
    request = {'reference': str(reference), 'stage': stage, 'folder': str(folder),
               'images': [str(p) for p in images], 'result': str(result), 'state': 'pending'}
    engine.atomic_json(OUT / 'review-pending.json', request)
    notify('Awaiting main-agent visual review: ' + stage)
    while not result.exists():
        pause_at_boundary()
        time.sleep(2)
    pause_at_boundary()
    value = normalize_review(json.loads(result.read_text(encoding='utf8')))
    request.update(state='reviewed', verdict=value['verdict'])
    engine.atomic_json(OUT / 'review-pending.json', request)
    return value

def quality_runner(*values):
    job = values[0]
    if Path(job['name']).stem in deferred_ids:
        assert job.get('status') == 'failed' and job.get('quality_status') == 'halted'
        # This explicit recovery hold does not reset or approve the failed ledger.
        return {'status': 'halted', 'stop': False, 'deferred': True}
    return run_staged(*values, reviewer=review_gate, ledger_root=OUT / 'quality-ledgers')

class TaskEngine(engine.BatchEngine):
    def prepare(self, options):
        db, jobs = super().prepare(options)
        by_id = {Path(job['name']).stem: job for job in jobs}
        for key in deferred_ids:
            if key not in by_id or by_id[key].get('status') != 'failed' or by_id[key].get('quality_status') != 'halted':
                raise engine.BatchError('Only an explicitly halted existing asset can be deferred: ' + key)
        if args.only:
            selected = set(args.only.split(','))
            jobs = [j for j in jobs if Path(j['name']).stem in selected]
        jobs.sort(key=lambda j: (0 if Path(j['name']).stem == 'F08' else 1 if Path(j['name']).stem.startswith('H') else 2, engine.natural_key(j['name'])))
        return db, jobs

batch = TaskEngine(client_factory=TaskClient, quality_runner=quality_runner)
engine.atomic_json(OUT / 'runner.json', {'pid': os.getpid(), 'only': args.only, 'defer': sorted(deferred_ids), 'started': time.time()})
batch.start(opts)
last = None
while batch.active:
    if pause_path.exists() and not batch.stop.is_set():
        batch.pause()
    state = batch.snapshot()
    engine.atomic_json(OUT / 'status.json', state)
    summary = {'active': state['active'], 'message': state['message'],
               'done': sum(j['status'] == 'done' for j in state['jobs']), 'total': len(state['jobs'])}
    if summary != last:
        print(json.dumps(summary, ensure_ascii=True), flush=True)
        last = summary
        subprocess.run([sys.executable, str(ROOT / 'tools/build-reststop-production-gallery.py')], cwd=ROOT, stdout=subprocess.DEVNULL, check=True)
    time.sleep(5)
engine.atomic_json(OUT / 'status.json', batch.snapshot())
subprocess.run([sys.executable, str(ROOT / 'tools/build-reststop-production-gallery.py')], cwd=ROOT, stdout=subprocess.DEVNULL, check=True)
print(json.dumps({'finished': True, 'message': batch.snapshot()['message']}, ensure_ascii=True), flush=True)
