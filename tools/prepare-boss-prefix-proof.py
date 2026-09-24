"""Prove only the distant boss changed before reusing unaffected early failures."""
import json
from pathlib import Path
import re
import runpy

read=runpy.run_path(str(Path(__file__).with_name('read-campaign-workbook.py')))['read']
source=Path('tmp/image-previews/campaign-balance-2026-09-23/r12-highway-62')
before=read();after=read(Path('outputs/campaign-balance-2026-09-23/r12-highway-boss185/Data.xlsx'))
names=list(before);changes=[(names.index(s),k) for s,cells in before.items() for k,v in cells.items() if v!=after[s][k]]
assert sorted(changes)==[(8,'O101'),(8,'O102'),(11,'C37')],changes
records=json.loads((source/'cohort.json').read_text(encoding='utf-8'));frames=[]
for run in range(31,54):
    record=records[run-1];assert record['scene']=='HighWay' and record['outcome']=='death' and record['seconds']<210
    receipt=json.loads((source/f'purchases-{run:02}.json').read_text(encoding='utf-8'));assert receipt['levels'][3]==0
    bonus=sum(float(re.search(r'([0-9.]+)%',e['label']).group(1)) for e in record['events'] if e['kind']=='bonus' and e['choice'].lower()=='missiledistance')
    positions=[json.loads(line)['position'] for line in (source/f'run-{run:02}/timeline.jsonl').read_text(encoding='utf-8').splitlines() if '"position"' in line]
    # Range bonuses are additive.32speed/1.25duration were verified natively.
    for position in positions:frames.append({'run':run,'position':position,'maximumProjectileRange':32*1.25*(1+bonus/100)})
path=Path('tmp/later-chapters-30-2026-09-23/boss-prefix.json')
path.write_text(json.dumps({'source':'r12-highway-62','through':53,'changedCells':changes,'frames':frames}),encoding='utf-8')
print(path,'frames',len(frames),'max projectile range',max(f['maximumProjectileRange'] for f in frames))
