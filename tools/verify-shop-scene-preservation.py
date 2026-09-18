"""Compare YAML documents outside the Canvas subtree with this task's baseline."""
import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
BACKUP = ROOT / 'tmp/backups/shop-fidelity-2026-09-17'


def outside_canvas(path):
    text = path.read_text(encoding='utf-8-sig')
    blocks = {}
    for block in re.split(r'(?=^--- !u!)', text, flags=re.M):
        match = re.match(r'--- !u!(\d+) &(-?\d+)', block)
        if match:
            blocks[int(match[2])] = (int(match[1]), block)
    canvas_ids = {key for key, (kind, block) in blocks.items()
                  if kind == 1 and re.search(r'^  m_Name: Canvas$', block, re.M)}
    excluded_transforms = set()
    excluded_objects = set(canvas_ids)
    changed = True
    while changed:
        changed = False
        for key, (kind, block) in blocks.items():
            if kind not in (4, 224) or key in excluded_transforms:
                continue
            owner = re.search(r'm_GameObject: \{fileID: (-?\d+)\}', block)
            parent = re.search(r'm_Father: \{fileID: (-?\d+)\}', block)
            if owner and (int(owner[1]) in excluded_objects or
                          parent and int(parent[1]) in excluded_transforms):
                excluded_transforms.add(key)
                excluded_objects.add(int(owner[1]))
                changed = True
    retained = {}
    for key, (kind, block) in blocks.items():
        owner = re.search(r'm_GameObject: \{fileID: (-?\d+)\}', block)
        if key in excluded_objects or owner and int(owner[1]) in excluded_objects:
            continue
        retained[key] = block
    return retained


results = []
for name in ('Noryangjin_MapTool_Mode_SR18', 'HighWay', 'RestStop'):
    before = outside_canvas(BACKUP / f'{name}.unity')
    after = outside_canvas(ROOT / f'Assets/ShooterSurvival/Scenes/Tools/{name}.unity')
    differences = [key for key in before.keys() | after.keys() if before.get(key) != after.get(key)]
    results.append({'scene': name, 'non_canvas_documents': len(before), 'changed_ids': differences})
output = ROOT / 'map-concepts/shop-fidelity-2026-09-17/verification/world-preservation.json'
output.write_text(json.dumps(results, indent=2), encoding='utf-8')
print(json.dumps(results, indent=2))
if any(row['changed_ids'] for row in results):
    raise SystemExit('Inspect unexpected changes outside Canvas')
