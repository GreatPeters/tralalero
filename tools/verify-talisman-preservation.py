"""Read-only check: Unity may serialize previously implicit defaults on prefab save."""
from pathlib import Path
import json
import re
import subprocess

guid = re.search(r'guid: (\w+)', Path('Assets/ShooterSurvival/Scripts/Walls/WallScript.cs.meta').read_text()).group(1)
defaults = {'wallType':'0','buffType':'0','nerfType':'0','isRandom':'0','rarity':'0','healthBoostAmt':'25','fireRateIncMultipier':'4','healthReduceAmt':'0','fireRateDecMultipier':'0.25'}

def values(text):
    result = []
    for block in re.split(r'(?m)^--- ', text):
        if 'guid: ' + guid not in block:
            continue
        row = {}
        for key, default in defaults.items():
            match = re.search(r'^  ' + key + r': (.*)$', block, re.M)
            row[key] = match.group(1).strip() if match else default
        result.append(row)
    return result

paths = subprocess.check_output(['git', 'ls-files', 'Assets/ShooterSurvival/Prefabs/Walls']).decode().splitlines()
results = []
for name in paths:
    if not name.endswith('.prefab'):
        continue
    before = values(subprocess.check_output(['git', 'show', 'HEAD:' + name]).decode('utf-8'))
    if not before:
        continue
    after = values(Path(name).read_text(encoding='utf-8'))
    results.append({'path':name,'wall_count':len(before),'gameplay_fields_preserved':before == after})
assert all(row['gameplay_fields_preserved'] for row in results), [r for r in results if not r['gameplay_fields_preserved']]
Path('map-concepts/common-talisman-applied-2026-09-20/preservation.json').write_text(json.dumps(results,indent=2),encoding='utf-8')
print(f'Preserved gameplay fields in {len(results)} prefab files.')
