"""Fresh-import inspection plus neutral and walking multiview evidence for six rigs."""
from pathlib import Path
import subprocess
import argparse

parser=argparse.ArgumentParser()
parser.add_argument('--label',default='final')
run_label=parser.parse_args().label

blender = Path('C:/Users/ljh/AppData/Local/Programs/Blender Foundation/Blender 5.2/blender.exe')
scripts = Path('C:/Users/ljh/.codex/skills/blender-asset-validation/scripts')
root = Path('outputs/highway-rigged-2026-09-11').resolve()
for folder in sorted(p for p in root.iterdir() if p.is_dir()):
    for stage, script, arguments in [
        ('inspect', 'inspect_asset.py', ['--input', str(folder/(folder.name+'.glb')), '--output', str(folder/f'metrics-{run_label}.json')]),
        ('rest', 'render_evidence.py', ['--input', str(folder/(folder.name+'.glb')), '--output-dir', str(folder/f'evidence-{run_label}'), '--resolution', '320']),
        ('walk', 'render_evidence.py', ['--input', str(folder/'walk-preview.blend'), '--output-dir', str(folder/f'walk-evidence-{run_label}'), '--resolution', '320']),
    ]:
        with (folder/(stage+'-validation.log')).open('w',encoding='utf-8') as log:
            subprocess.run([str(blender), '--background', '--factory-startup', '--python', str(scripts/script), '--', *arguments], stdout=log, stderr=subprocess.STDOUT, check=True)
    print(folder.name+' inspected and rendered',flush=True)
