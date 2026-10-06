"""Run audit probes through the official CLI with native argv, retaining receipts."""
import datetime
import json
from pathlib import Path
import shutil
import subprocess
import sys

root = Path(__file__).resolve().parents[1]
method = sys.argv[1]
args = []
for arg in sys.argv[2:]:
    try:
        args.append(json.loads(arg))
    except ValueError:
        args.append(arg)
source, entry = method.split(':',1) if ':' in method else ('tools/audit-s22-quality-20261001.cs','S22QualityAudit.'+method)
cmd = [shutil.which('unity'), 'command', '--project-path', str(root), 'run_script', '--file', source, '--entry', entry, '--args', json.dumps(args), '--timeout', '120', '--format', 'json']
result = subprocess.run(cmd, cwd=root, capture_output=True, text=True, encoding='utf-8', timeout=145)
receipts = root/'outputs/s22-quality-audit-2026-10-01/receipts'
receipts.mkdir(exist_ok=True)
(receipts/(datetime.datetime.now().strftime('%H%M%S-%f')+'-'+entry+'.json')).write_text(result.stdout, encoding='utf-8')
try:
    parsed = json.loads(result.stdout)
    data = parsed.get('data') or {}
    value = data.get('result', parsed)
    print(json.dumps(value, ensure_ascii=True))
    if not parsed.get('success') or not value.get('success', True):
        sys.exit(1)
except ValueError:
    print(result.stdout, result.stderr)
    sys.exit(1)
