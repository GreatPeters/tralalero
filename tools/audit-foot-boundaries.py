"""Inspect the cut shoe collars before building a clean lower-leg connection."""
import json
from pathlib import Path
from collections import defaultdict,Counter
import numpy as np
root=Path(__file__).resolve().parent.parent
folder=root/'map-concepts/skins-reststop-2026-09-12'
data=json.loads((folder/'footwear-fit-geometry.json').read_text(encoding='utf-8'))
vertices=np.array(data['vertices']);faces=np.array(data['triangles']).reshape(-1,3)
keys=np.round(vertices*1e8).astype(int);unique={};weld=[];representatives={}
for index,key in enumerate(map(tuple,keys)):
    if key not in unique:unique[key]=len(unique);representatives[unique[key]]=index
    weld.append(unique[key])
parent=list(range(len(faces)))
def find(i):
    while parent[i]!=i:parent[i]=parent[parent[i]];i=parent[i]
    return i
def join(a,b):parent[find(a)]=find(b)
edges=defaultdict(list)
for index,face in enumerate(faces):
    for a,b in zip(face,np.roll(face,-1)):edges[tuple(sorted((weld[a],weld[b])))].append(index)
for connected in edges.values():
    for other in connected[1:]:join(connected[0],other)
components=defaultdict(list)
for index in range(len(faces)):components[find(index)].append(index)
ordered=sorted(components.values(),key=len,reverse=True)
rows=[]
for group in ordered:
    ids=np.unique(faces[group]);points=vertices[ids]
    rows.append({'triangles':len(group),'min':points.min(0).tolist(),'max':points.max(0).tolist()})
kept=ordered[0];edgeFaces=defaultdict(list)
for index in kept:
    face=faces[index]
    for a,b in zip(face,np.roll(face,-1)):edgeFaces[tuple(sorted((weld[a],weld[b])))].append((int(a),int(b)))
boundaries=[edge for edge,used in edgeFaces.items() if len(used)==1]
graph=defaultdict(set)
for a,b in boundaries:graph[a].add(b);graph[b].add(a)
visited=set();loops=[]
for start in graph:
    if start in visited:continue
    stack=[start];part=[]
    while stack:
        v=stack.pop()
        if v in visited:continue
        visited.add(v);part.append(v);stack.extend(graph[v]-visited)
    points=vertices[[representatives[i] for i in part]]
    loops.append({'vertices':len(part),'degree':dict(Counter(len(graph[i]) for i in part)),'center':points.mean(0).tolist(),'min':points.min(0).tolist(),'max':points.max(0).tolist(),'weldIds':part})
report={'components':rows,'boundaries':loops,'keptTriangles':[int(i) for i in kept]}
(folder/'foot-boundary-audit.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print(json.dumps({'components':rows[:15],'boundaries':[{k:v for k,v in row.items() if k!='weldIds'} for row in loops]},indent=2))
