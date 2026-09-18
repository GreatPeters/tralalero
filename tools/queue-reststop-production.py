"""Continue the authorized asset queue after the current footwear job finishes."""
import json
from pathlib import Path
import subprocess
import sys
import time

root=Path(__file__).resolve().parent.parent
status=root/'outputs/skins-reststop-2026-09-12/production/footwear/task-status.json'
while True:
    state=json.loads(status.read_text(encoding='utf-8'))
    if not state['active']:
        break
    if time.time()-status.stat().st_mtime>600:
        raise RuntimeError('Footwear status stalled; inspect before continuing')
    time.sleep(20)
for kind in ['headwear','reststop']:
    time.sleep(60)
    print('Starting '+kind,flush=True)
    subprocess.run([sys.executable,str(root/'tools/generate-skins-reststop.py'),'--kind',kind],cwd=root,check=True)
print('Production queue complete',flush=True)
