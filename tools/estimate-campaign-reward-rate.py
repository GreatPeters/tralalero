"""Planning estimate from a measured chapter's upgrade path; never a playtest result."""
import argparse
from bisect import bisect_left
from collections import defaultdict
import json
from pathlib import Path
import runpy

parser=argparse.ArgumentParser();parser.add_argument('cohort');parser.add_argument('--chapter',required=True,type=int);parser.add_argument('--measured-rate',required=True,type=float);parser.add_argument('--rates',required=True);args=parser.parse_args()
folder=Path(args.cohort);records=json.loads((folder/'cohort.json').read_text(encoding='utf-8'))
scene={1:'Noryangjin_MapTool_Mode_SR18',2:'HighWay',3:'RestStop'}[args.chapter]
indices=[i for i,r in enumerate(records) if r['scene']==scene]
if not indices or records[indices[-1]]['outcome']!='clear':raise ValueError('A completed measured chapter is required')
read=runpy.run_path(str(Path(__file__).with_name('read-campaign-workbook.py')))['read']
sheet=next(iter(read().values()));upgrades={}
for address,cell in sheet.items():
    if not address.startswith('B') or cell['value'] not in (1,2,3):continue
    row=address[1:];level=sheet.get('D'+row,{}).get('value');amount=sheet.get('F'+row,{}).get('value');price=sheet.get('I'+row,{}).get('value')
    if level is not None and amount is not None and price is not None:upgrades[int(cell['value']),int(level)]=(amount,int(price))
entry_index=indices[0]
if entry_index==0:levels=[0,0,0];bank=0
else:
    levels=json.loads((folder/f'purchases-{entry_index:02}.json').read_text(encoding='utf-8'))['levels'][:3]
    bank=records[entry_index-1]['bank']
start=next(e for e in records[entry_index]['events'] if e['kind']=='start')
first_levels=json.loads((folder/f'purchases-{entry_index+1:02}.json').read_text(encoding='utf-8'))['levels'][:3]
amount=lambda actor,level:upgrades.get((actor,level),(0,0))[0]
base_hp=start['health']-amount(2,first_levels[1]);base_attack=start['attack']-amount(1,first_levels[0])
def next_upgrade(state):
    candidates=[]
    for actor in (1,2,3):
        previous=amount(actor,state[actor-1]);following=upgrades.get((actor,state[actor-1]+1))
        if following is None:continue
        denominator=base_attack+previous if actor==1 else base_hp+amount(2,state[1]) if actor==2 else 100+previous
        utility=(following[0]-previous)/denominator*(.9 if actor==2 else 1)/max(1,following[1])
        candidates.append((utility,-actor,actor,following[1]))
    return max(candidates)[2:] if candidates else None
# Confirm this analytical policy matches every observed actual purchase first.
check=levels.copy();samples=defaultdict(list)
for i in indices:
    receipt=json.loads((folder/f'purchases-{i+1:02}.json').read_text(encoding='utf-8'))
    for purchase in receipt['purchases']:
        expected=next_upgrade(check)
        if expected!=(purchase['id'],purchase['price']):raise ValueError('Observed purchase policy differs; do not extrapolate')
        check[purchase['id']-1]+=1
    units=records[i]['earnedCoins']/args.measured_rate
    if abs(units-round(units))>.001:raise ValueError('Mixed coin sources require a separate model')
    samples[sum(check)].append(units)
threshold=sum(check);keys=sorted(samples);means={k:sum(samples[k])/len(samples[k]) for k in keys}
def earning(position):
    j=bisect_left(keys,position)
    if j==0:return means[keys[0]]
    if j==len(keys):return means[keys[-1]]
    a,b=keys[j-1],keys[j];return means[a]+(means[b]-means[a])*(position-a)/(b-a)
results=[]
for rate in map(float,args.rates.split(',')):
    state=levels.copy();wallet=float(bank)
    for attempt in range(1,201):
        for _ in range(12):
            choice=next_upgrade(state)
            if choice is None or choice[1]>wallet:break
            actor,price=choice;wallet-=price;state[actor-1]+=1
        if sum(state)>=threshold:break
        wallet+=earning(sum(state))*rate
    results.append({'rate':rate,'estimatedAttempts':attempt,'thresholdUpgradeCount':threshold,'note':'Interpolated kill/progress income, fixed measured clear-power threshold; ignores seed variance. Must verify with native gameplay.'})
print(json.dumps(results,indent=2))
