"""Carry verified earlier chapters forward without replaying or inventing their earnings."""
import argparse
import json
import os
from pathlib import Path
import shutil

parser=argparse.ArgumentParser();parser.add_argument('source');parser.add_argument('destination');parser.add_argument('--through',required=True,type=int);parser.add_argument('--boss-prefix-proof');args=parser.parse_args()
base=Path('tmp/image-previews/campaign-balance-2026-09-23')
source=base/args.source;destination=base/args.destination
if destination.exists():raise FileExistsError(destination)
runs=json.loads((source/'cohort.json').read_text(encoding='utf-8'))[:args.through]
verified_prefix=False
if args.boss_prefix_proof:
    proof_path=Path(args.boss_prefix_proof);proof=json.loads(proof_path.read_text(encoding='utf-8'));verified=json.loads(Path(str(proof_path)+'.verified.json').read_text(encoding='utf-8'))
    verified_prefix=proof['source']==args.source and proof['through']==args.through and verified['passed']
    if not verified_prefix:raise ValueError('Boss-only source/range proof does not match this prefix')
if len(runs)!=args.through or runs[-1]['outcome']!='clear' and not verified_prefix:raise ValueError('A verified chapter-clear checkpoint or boss-only invariant prefix is required')
destination.mkdir(parents=True)
def copy(src,dst):
    dst.parent.mkdir(parents=True,exist_ok=True)
    try:os.link(src,dst)
    except OSError:shutil.copy2(src,dst)
for i in range(1,args.through+1):
    for f in (source/f'run-{i:02}').rglob('*'):
        if f.is_file():copy(f,destination/f.relative_to(source))
    for name in [f'purchases-{i:02}.json',f'state-{i:02}.tsv',f'conditions-{i:02}.json']:
        if (source/name).exists():copy(source/name,destination/name)
(destination/'cohort.json').write_text(json.dumps(runs,ensure_ascii=False,indent=2),encoding='utf-8')
(destination/'carried-checkpoint.json').write_text(json.dumps({'source':str(source),'through':args.through,'earlierRunsReusedNotReplayed':True,'lastBank':runs[-1]['bank'],'bossPrefixProof':args.boss_prefix_proof},indent=2),encoding='utf-8')
print(destination)
