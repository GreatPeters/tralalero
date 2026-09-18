"""Single placement contract for RestStop scene authoring and workbook rows."""
import json
import math
from pathlib import Path

root = Path(__file__).resolve().parent.parent
points = [[0,0,-420],[0,0,0],[240,0,0],[240,0,440],[-100,0,440],[-100,0,780],[220,0,780]]
lengths = [math.dist(a,b) for a,b in zip(points,points[1:])]
starts = [sum(lengths[:i]) for i in range(len(lengths))]
total = sum(lengths)
names = ['주차장 진입','매점 거리','야외 식사 공간','전기차 충전 구역','물류·정비 구역','출구 정원']
allowed = [(start+(50 if i==0 else 35),start+length-35) for i,(start,length) in enumerate(zip(starts,lengths))]
usable = sum(b-a for a,b in allowed)

def at_usable(distance):
    for a,b in allowed:
        if distance <= b-a:
            return round(a+distance,3)
        distance -= b-a
    raise ValueError('Placement exceeds route')

def section_at(distance):
    return next(i for i,(start,length) in enumerate(zip(starts,lengths)) if distance <= start+length)

roles = ['ParkingMarshal','SnackChef','CoffeeVendor','DeliveryRider','TireBruiser']
enemies, bonuses, gimmicks = [], [], []
schedule = [at_usable((i+.5)*usable/75) for i in range(75)]
for station in range(25):
    bonus_distance, enemy_distance, hazard_distance = schedule[station*3:station*3+3]
    role = 'TireBruiser' if station==24 else roles[station%len(roles)]
    ranged = role in ['CoffeeVendor','DeliveryRider','TireBruiser']
    section = section_at(enemy_distance)
    lead = min(32, enemy_distance-starts[section]-8)
    # Alternate the safe side; do not leave the center empty for every formation.
    lanes = [0,3.1] if station%2==0 else [-3.1,0]
    for side,lane in enumerate(lanes):
        enemies.append({'id':f'RST_E{station+1:02d}_{role}'+('_Right' if side else ''),
            'station':station+1,'distance':enemy_distance,'lane':lane,'model':role,
            'mode':'사격' if ranged else '왕복','speed':2.2,'move':0 if ranged else 1.2,
            'lead':round(lead,3),'delay':.65,'projectileSpeed':12,'ranged':ranged,
            'tier':'Boss' if station==24 else 'Elite' if role=='TireBruiser' else 'Normal'})
    bonuses.append({'id':f'RST_B{station+1:02d}','station':station+1,'distance':bonus_distance,
                    'rarity':'Rare' if station%5==4 else 'Normal'})
    kind = ['HighwayRoadblock','HighwayTraffic','HighwayRoadblock','HighwayToll','HighwayTraffic'][station%5]
    gimmicks.append({'id':f'RST_G{station+1:02d}','station':station+1,'distance':hazard_distance,
        'pattern':kind,'lane':(-3.5 if station%2==0 else 3.5) if kind=='HighwayRoadblock' else -4.5 if kind=='HighwayTraffic' else 0,
        'theme':'정비용 안전 바리케이드' if kind=='HighwayRoadblock' else '주차장에서 나오는 차량' if kind=='HighwayTraffic' else '주차장 차단기',
        'warning':2.0,'operation':5.5 if kind=='HighwayTraffic' else 7,'crossingDistance':9})
assert len(enemies)==50 and len(bonuses)==25 and len(gimmicks)==25
assert all(e['lead']>=20 for e in enemies)
assert min(b-a for a,b in zip(schedule,schedule[1:])) > 18
assert len({e['id'] for e in enemies+bonuses+gimmicks})==100
plan={'scene':'RestStop','chapter':3,'length':total,'speed':7,'durationTarget':300,'points':points,
      'zones':[{'index':i,'name':name,'start':start,'length':length} for i,(name,start,length) in enumerate(zip(names,starts,lengths))],
      'enemies':enemies,'bonuses':bonuses,'gimmicks':gimmicks,
      'verification':{'encounterCounts':[25,50,25],'minimumBeatSpacing':min(b-a for a,b in zip(schedule,schedule[1:])),
                      'cornerExclusion':35,'livePlay':'pending'}}
target=root/'map-concepts/chapters-polish-2026-09-12/reststop-layout.json'
target.write_text(json.dumps(plan,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps({'file':str(target),'length':total,'enemyCount':len(enemies),'ranged':sum(e['ranged'] for e in enemies),
                   'bonusPairs':len(bonuses),'gimmickStations':len(gimmicks),'minSpacing':plan['verification']['minimumBeatSpacing']}))
