import json
from collections import defaultdict
from pathlib import Path
root=Path(__file__).resolve().parent.parent
data=json.loads((root/'map-concepts/skins-reststop-2026-09-12/unity-shark-mesh.json').read_text(encoding='utf-8'))
verts=data['vertices'];parent=list(range(len(verts)));positions={}
def find(i):
    while parent[i]!=i:
        parent[i]=parent[parent[i]];i=parent[i]
    return i
def join(a,b):parent[find(a)]=find(b)
for i,v in enumerate(verts):
    key=tuple(round(x,8) for x in v)
    if key in positions:join(i,positions[key])
    else:positions[key]=i
for indices in data['submeshes']:
    for i in range(0,len(indices),3):
        join(indices[i],indices[i+1]);join(indices[i],indices[i+2])
groups=defaultdict(list)
for i in range(len(verts)):groups[find(i)].append(i)
shoe=set(data['submeshes'][1]);rows=[]
for indices in groups.values():
    rows.append({'vertices':len(indices),'shoeVertices':len(shoe.intersection(indices)),
      'min':[min(verts[i][a] for i in indices) for a in range(3)],'max':[max(verts[i][a] for i in indices) for a in range(3)],'indices':indices})
rows.sort(key=lambda r:r['vertices'],reverse=True)
(root/'map-concepts/skins-reststop-2026-09-12/shark-components.json').write_text(json.dumps(rows))
print(json.dumps([{k:v for k,v in row.items() if k!='indices'} for row in rows[:30]],indent=2))
