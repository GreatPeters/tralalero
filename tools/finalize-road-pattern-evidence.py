from pathlib import Path
from hashlib import sha256
import json
import shutil

root=Path(__file__).resolve().parent.parent
record=root/'map-concepts/road-patterns-2026-09-13'
qa=root/'tmp/q/tmp/road-pattern-playtest'
read=lambda p:json.loads(p.read_bytes())
suites=['pattern','turn-regression','combat-regression','chapter-integration','analytics','workbook']
tests={name:read(record/f'native-{name}-tests.json')['summary'] for name in suites}
assert all(r['total']==r['passed'] and r['failed']==0 for r in tests.values())
original=read(record/'original-state-final.json')['data']['result']['result']
assert original['unchanged'] and original['keys']==63 and original['archiveVerified']
selected=['cycle2-highway-green','accepted-highway-main-opening','final-highway-full-main','accepted-reststop-late-chapter']
reports={name:read(qa/name/'result.json') for name in selected}
assert reports['final-highway-full-main']['distance']>2328 and reports['final-highway-full-main']['trafficLaunched']==11
holdout=reports['accepted-reststop-late-chapter']
assert holdout['holdoutCompleted'] and holdout['holdoutSeconds']==30 and holdout['maximumDrift']==0 and holdout['spawnPositionError']==0 and all(holdout['spawnedByDoor'])
assert not holdout['movementLocked'] and not holdout['endurance']
for name in selected:
    target=record/'playtests'/name;target.mkdir(parents=True,exist_ok=True)
    for file in ['result.json','timeline.csv']:
        shutil.copy2(qa/name/file,target/file)
preview=root/'tmp/image-previews/road-patterns-2026-09-13';preview.mkdir(parents=True,exist_ok=True)
images={
    'highway-oncoming.png':qa/'accepted-highway-main-opening/frame-007.png',
    'highway-curve.png':qa/'cycle2-highway-green/frame-041.png',
    'reststop-holdout.png':qa/'accepted-reststop-late-chapter/frame-039.png',
    'reststop-opposite-doors.png':qa/'accepted-reststop-late-chapter/frame-031.png'}
for name,source in images.items():
    destination=preview/name
    if destination.exists():assert destination.read_bytes()==source.read_bytes()
    else:shutil.copy2(source,destination)
paths=['Assets/ShooterSurvival/Scenes/Tools/HighWay.unity','Assets/ShooterSurvival/Scenes/Tools/RestStop.unity','Assets/ShooterSurvival/GameData/Editor/Data.xlsx','Assets/ShooterSurvival/Resources/GameData/Data.bytes']
report={'tests':tests,'passedTests':sum(r['passed'] for r in tests.values()),'playtests':reports,'originalState':original,
        'hashes':{p:sha256((root/p).read_bytes()).hexdigest() for p in paths},'previews':{k:str((preview/k).relative_to(root)).replace('\\','/') for k in images},
        'limits':['Highway full traversal uses a maintained health override, not ordinary balance certification.','RestStop success uses seeded late-chapter upgrades ATT37 HP46 speed30 and section entry setup.','The prior 17/22/22 progression cohorts and Android APK predate these new patterns.']}
(record/'final-verification.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps({'passedTests':report['passedTests'],'previews':report['previews'],'originalStateUnchanged':True},ensure_ascii=False))
