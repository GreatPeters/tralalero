"""Build the recorded-run ledger without rewriting original evidence."""
from pathlib import Path
import json
from collections import Counter

base=Path('tmp/image-previews/review-fixes-20-runs-2026-09-22')
target=Path('map-concepts/review-fixes-20-runs-2026-09-22')
target.mkdir(parents=True,exist_ok=True)
reports=[]
for p in sorted((base/'runs').glob('run-*/summary.json')):
    r=json.loads(p.read_text(encoding='utf-8'));r['run']=int(p.parent.name[-2:]);reports.append(r)
if len(reports)!=20:raise SystemExit(f'Wait for twenty completed attempts: {len(reports)}')
counts=Counter(r['outcome'] for r in reports)
pngs=sum(len(list((base/'runs'/f'run-{r["run"]:02}').glob('*.png'))) for r in reports)
lines=['# Selected review corrections and twenty playtests','',
f'Completed 20 recorded attempts: {counts["clear"]} clears, {counts["death"]} deaths, {counts["time-limit"]} time limits. {pngs} original native PNG captures, excluding separate visual probes and archived attempts.','',
'Primary local gallery: <http://127.0.0.1:6753/review-fixes/>. Repetition evidence: <http://127.0.0.1:6753/review-fixes/#repetition>. Original run evidence is under `tmp/image-previews/review-fixes-20-runs-2026-09-22/runs/run-NN/`.','',
'## Applied and preserved','',
'- Severe gray-gantry/sign obstruction fades to40% opacity in about0.2s only when the body and immediate road are both substantially blocked. Small/edge occlusion remains. Actual1x passage includes full restoration. Bound the actual gantry/sign groups and rebaked SR18/HighWay after excluding them from static occluders.',
'- Flat bonus amounts retain the earlier fix: actual +12 pickup68→80 and +14HP500→514. One resolved amount drives caption/application; no additional workbook changes in this review.',
'- Nearby dangerous enemy contact shows predicted HP loss or a lethal warning based on live health. Existing current-health exchange remains.',
'- Defeat UI shows actual final source, actual HP lost, source-specific advice and final attack/max HP. Vehicles, toll barriers, projectiles and contact are distinguished.',
'- Woman knife handle/finger pose is calibrated on the actual visible glove. Carry/slash clips inspected at eight times each using evaluated mesh geometry, handle error<=0.005m. Apron material has blue-gray form shading and no deprecated interior outline. Existing boss HP unchanged.',
'- Six highway source enemy models and50placements are about40% larger, with adjusted glove/head proportions and capsule height. Capsule radii and actor root positions remain unchanged.',
'- Oncoming-traffic risk uses broad orange diagonal strips and stays visible until passing. Green/pink route guidance remains.',
'- Helper count/formation and repeated shop/sign/enemy placement were not changed. Original repetition screenshots are included. `concepts/repetition-options.png` is an AI-generated comparison proposal only, not applied art.','',
'## Test conditions and correction to the first matrix','',
'Runs use ordinary PlayerMove input, real collisions, normal rewards and real purchased upgrade levels. There is no runtime HP/damage override, invulnerability, teleport, forced pickup or disabled enemy. Simulation is3x for the matrix; separate visual passages are1x. These are automated attempts, not human win-rate or FPS measurements.',
'',
'The first matrix contained12 later-chapter attempts with defective automatic avoidance: a moving crossing car made steering flip sides, and toll selection ignored the lane open at arrival. Those attempts are retained under `steering-diagnostics/` and replaced by12 new attempts with predictive steering and the same growth profiles. Together there were32 terminal attempts plus one earlier Editor-paused partial run retained under `interrupted/`. Enemy balance was not weakened to compensate for test input errors.',
'',
'Purchased levels are reproducible test saves, not a proven natural campaign economy. Cumulative coin costs are3126(current),9369(harbor growth),51238(highway entry),81723(highway ready),143932(rest-stop entry),195811(rest-stop ready). Affordability/required grind at chapter unlock remains unverified.',
'',
'| Run | Map | Profile | Start HP / ATT | ATT/HP/AS levels | Seconds | Bonuses | Outcome | Terminal source |',
'|---|---|---|---:|---|---:|---:|---|---|']
for r in reports:
    s=next(e for e in r['events'] if e['kind']=='start')
    cause=r['fatalCause'] if r['outcome']=='death' else '—'
    lines.append(f'|{r["run"]:02}|{r["scene"]}|{r["profile"]}|{s["health"]:.0f} / {s["attack"]:.0f}|{s["levels"]}|{r["seconds"]:.1f}|{r["bonuses"]}|{r["outcome"]}|{cause}|')
lines+=['','## What the comparison established','',
'SR18 current-save runs had no clears; the growth fixture cleared twice. The highway entry fixture had no clears; its stronger health-priority fixture cleared once. These small, differently rolled runs describe observations, not causal win-rate estimates.',
'',
'RestStop entry runs survived 44.7-96.9 seconds. Stronger fixtures survived 62.6-189.7 seconds. Run19 captured the restaurant holdout at117.8-142.1 seconds and exit/recovery at146.1 seconds, then died to a crossing vehicle at189.6 seconds. The remaining later section and full RestStop clear are not established by this matrix.',
'',
'Of the17deaths,13ended in enemy contact,2in FatMan crates and2in crossing vehicles. Dangerous contact remains consequential by design; warnings now communicate it and results identify it. Further tuning should distinguish evasion skill, bonus rolls and entry affordability instead of treating all deaths as insufficient HP.',
'','## Evidence and reproduction','',
'- Native before/after gantry: `preflight-harbor-v1/frame-031.png`, with opacity timeline and restoration in `occlusion.csv`.',
'- Readable threat/result UI: `hud-v1/` (directed collider diagnostic, not a matrix run) and actual run result screenshots.',
'- Final characters: `characters-final/`; evaluated pose inspection: `woman-motion-final/`. Earlier grip prototypes are rejected experiments, not final evidence.',
'- Hazard strip/readable enemy passage: `preflight-highway-v1/` and matrixHighWaycaptures.',
'- Rebuild gallery: `python -X utf8 tools/build-review-fixes-gallery.py`.',
'- Regenerate ledger: `python -X utf8 tools/write-review-run-report.py`.',
'- Native authoring: `tools/apply-review-readability.cs` via Unity Pipeline; source backups under `tmp/review-fixes-20-runs-2026-09-22/before/`.',
'- Sequential matrix: `python tools/run-review-batch.py --from-run 1` skips completed folders and refuses incomplete existing folders. Archive reviewed evidence before requesting a fresh matrix. See run-configs/profile preparation tools for exact conditions.',
'',
'Verification and restored-state checks are recorded in the completed execution plan. This work does not include a new phone build, merge or push.']
(target/'README.md').write_text('\n'.join(lines)+'\n',encoding='utf-8')
(target/'run-summary.json').write_text(json.dumps(reports,ensure_ascii=False,indent=2),encoding='utf-8')
print(f'{counts}; {pngs} native PNGs')
