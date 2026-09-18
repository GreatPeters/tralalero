"""Update the existing real-file QA checkout without sharing mutable databases."""
from pathlib import Path
import shutil
import json

root = Path(__file__).resolve().parent.parent
qa = root / 'tmp/q'
assert qa.resolve().is_relative_to((root / 'tmp').resolve())
assert (qa / 'ProjectSettings/ProjectVersion.txt').exists()
folders = ['Assets/ShooterSurvival/Scripts', 'Assets/ShooterSurvival/Editor', 'Assets/Tests',
           'Assets/ShooterSurvival/Prefabs/Highway', 'Assets/ShooterSurvival/Prefabs/RestStop',
           'Assets/ShooterSurvival/Models/Chapters/Mascots', 'Assets/ShooterSurvival/Models/Highway',
           'Assets/ShooterSurvival/Models/Chapters/Props',
           'Assets/ShooterSurvival/Models/Chapters/RoadPatterns', 'Assets/ShooterSurvival/Scenes/Tools',
           'Assets/ShooterSurvival/GameData/Editor', 'Assets/ShooterSurvival/Resources/GameData']
copied = []
for folder in folders:
    for source in (root / folder).rglob('*'):
        if not source.is_file(): continue
        relative = source.relative_to(root); target = qa / relative
        if target.exists() and target.stat().st_mtime_ns == source.stat().st_mtime_ns and target.stat().st_size == source.stat().st_size: continue
        target.parent.mkdir(parents=True,exist_ok=True); shutil.copy2(source,target); copied.append(relative.as_posix())
record = root / 'map-concepts/road-patterns-2026-09-13/qa-sync.json'
record.write_text(json.dumps({'qa':str(qa),'copied':copied},indent=2),encoding='utf-8')
print(json.dumps({'copied':len(copied),'qa':str(qa)}))
