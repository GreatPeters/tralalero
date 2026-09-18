"""Compare scene components outside the Canvas against this task's fresh backups."""
from pathlib import Path
import json
import re

ROOT = Path(__file__).resolve().parents[1]


def gameplay_blocks(path):
    text = path.read_text(encoding="utf-8")
    blocks = {}
    for match in re.finditer(r"^--- !u!(\d+) &(\d+)[^\n]*\n(.*?)(?=^--- !u!|\Z)", text, re.M | re.S):
        kind, identity, body = match.groups()
        blocks[int(identity)] = (int(kind), body)
    canvases = {identity for identity, (kind, body) in blocks.items()
                if kind == 1 and re.search(r"^  m_Name: Canvas$", body, re.M)}
    parents, owners = {}, {}
    for identity, (kind, body) in blocks.items():
        owner = re.search(r"^  m_GameObject: \{fileID: (\d+)\}", body, re.M)
        if owner:
            owners[identity] = int(owner[1])
        parent = re.search(r"^  m_Father: \{fileID: (\d+)\}", body, re.M)
        if kind in (4, 224) and parent:
            parents[identity] = int(parent[1])
    ui_transforms = {identity for identity in parents if owners.get(identity) in canvases}
    while True:
        descendants = {identity for identity, parent in parents.items() if parent in ui_transforms}
        if descendants <= ui_transforms:
            break
        ui_transforms |= descendants
    ui_objects = canvases | {owners[t] for t in ui_transforms if t in owners}
    result = {}
    for identity, (kind, body) in blocks.items():
        if kind == 1 and identity not in ui_objects:
            result[identity] = body
        elif identity in owners and owners[identity] not in ui_objects:
            result[identity] = body
    return result


report = {}
for name in ("Noryangjin_MapTool_Mode_SR18", "HighWay", "RestStop"):
    before = gameplay_blocks(ROOT / "tmp/backups/coastal-enamel-ui-2026-09-16/scenes" / f"{name}.unity")
    after = gameplay_blocks(ROOT / "Assets/ShooterSurvival/Scenes/Tools" / f"{name}.unity")
    changed = [key for key in before if after.get(key) != before[key]]
    added = [key for key in after if key not in before]
    report[name] = dict(compared=len(before), changed=changed, added=added)
print(json.dumps(report, indent=2))
(ROOT / "map-concepts/coastal-enamel-ui-2026-09-16/gameplay-scene-comparison.json").write_text(json.dumps(report, indent=2), encoding="utf-8")
if any(row["changed"] or row["added"] for row in report.values()):
    raise SystemExit(1)
