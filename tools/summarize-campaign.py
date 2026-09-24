"""Compact evidence table from real campaign sessions; no projected clear claims."""
import json
from pathlib import Path
import sys
sys.stdout.reconfigure(encoding='utf-8')
folder=Path(sys.argv[1])
runs=json.loads((folder/'cohort.json').read_text(encoding='utf-8'))
print('run ch seconds outcome startHP startATT upgrades levels earned bank fatal')
for i,r in enumerate(runs,1):
    start=next(e for e in r['events'] if e['kind']=='start')
    buy=json.loads((folder/f'purchases-{i:02}.json').read_text(encoding='utf-8'))
    chapter={'Noryangjin_MapTool_Mode_SR18':1,'HighWay':2,'RestStop':3}[r['scene']]
    print(i,chapter,round(r['seconds'],1),r['outcome'],round(start['health']),round(start['attack']),len(buy['purchases']),buy['levels'][:3],r['earnedCoins'],r['bank'],r['fatalCause'])
