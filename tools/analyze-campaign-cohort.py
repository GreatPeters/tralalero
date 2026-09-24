"""Summarize only measured attempts and reconcile the complete earned wallet."""
import argparse
import csv
import json
from pathlib import Path
import statistics

parser=argparse.ArgumentParser();parser.add_argument('folder');args=parser.parse_args()
folder=Path(args.folder);runs=json.loads((folder/'cohort.json').read_text(encoding='utf-8'))
scenes={'Noryangjin_MapTool_Mode_SR18':1,'HighWay':2,'RestStop':3}
rows=[];previous=0
for i,r in enumerate(runs,1):
    purchase=json.loads((folder/f'purchases-{i:02}.json').read_text(encoding='utf-8'))
    spent=sum(p['price'] for p in purchase['purchases'])
    assert previous-spent==r['initialCoins'],f'Unexplained currency before run{i}'
    assert r['initialCoins']+r['earnedCoins']==r['bank'],f'Unexplained currency after run{i}'
    previous=r['bank'];start=next(e for e in r['events'] if e['kind']=='start')
    rows.append({'run':i,'chapter':scenes[r['scene']],'seconds':round(r['seconds'],2),'outcome':r['outcome'],
                 'start_hp':start['health'],'start_attack':start['attack'],'attack_level':purchase['levels'][0],
                 'health_level':purchase['levels'][1],'attack_speed_level':purchase['levels'][2],
                 'purchases':len(purchase['purchases']),'spent':spent,'earned':r['earnedCoins'],'bank':r['bank'],'fatal_cause':r['fatalCause'] or ''})
summary=[]
for chapter in sorted({r['chapter'] for r in rows}):
    group=[r for r in rows if r['chapter']==chapter];clear=next((r for r in group if r['outcome']=='clear'),None)
    visits=sum(r['purchases']>0 for r in group)
    growth_visits=sum(r['purchases']>0 for r in group[1:])
    summary.append({'chapter':chapter,'attempts':len(group),'clearRun':clear['run'] if clear else None,
                    'firstSeconds':group[0]['seconds'],'clearSeconds':clear['seconds'] if clear else None,
                    'purchaseVisits':visits,'purchaseVisitsAfterFirstAttempt':growth_visits,'endpointSecondsPerPurchaseVisit':round((clear['seconds']-group[0]['seconds'])/growth_visits,3) if clear and growth_visits else None,
                    'totalEarned':sum(r['earned'] for r in group),'totalSpent':sum(r['spent'] for r in group),
                    'meanSeconds':round(statistics.mean(r['seconds'] for r in group),2),
                    'measureNote':'Endpoint gain divided by purchase visits, not a guarantee for individual upgrades.'})
with (folder/'measured-runs.csv').open('w',encoding='utf-8-sig',newline='') as f:
    writer=csv.DictWriter(f,fieldnames=list(rows[0]));writer.writeheader();writer.writerows(rows)
(folder/'measured-summary.json').write_text(json.dumps({'chapters':summary,'walletReconciled':True,'finalBank':previous},indent=2),encoding='utf-8')
print(json.dumps(summary))
