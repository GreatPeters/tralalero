"""Review this turn against its fresh source snapshot, never unrelated dirty work."""
from pathlib import Path
import difflib
import json

root=Path(__file__).resolve().parents[1]
before=root/'tmp/combat-feedback-2026-09-14/before'
changed=[];diff=[]
for folder in ('Assets/ShooterSurvival/Scripts','Assets/ShooterSurvival/Editor','Assets/Tests/Editor'):
    for path in (root/folder).rglob('*.cs'):
        relative=path.relative_to(root);prior=before/relative
        old=prior.read_text(encoding='utf-8-sig') if prior.exists() else ''
        new=path.read_text(encoding='utf-8-sig')
        if old==new:continue
        changed.append(str(relative));diff.extend(difflib.unified_diff(old.splitlines(True),new.splitlines(True),fromfile='before/'+str(relative),tofile=str(relative)))
target=root/'tmp/combat-feedback-2026-09-14/review.diff';target.write_text(''.join(diff),encoding='utf-8')
(target.parent/'changed-source-files.json').write_text(json.dumps(changed,indent=2),encoding='utf-8')
print('\n'.join(changed));print('Changed source files:',len(changed))
