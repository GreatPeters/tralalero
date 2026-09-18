"""Cut clean ankle loops and extend the original skinned legs into each new shoe."""
from pathlib import Path
from collections import defaultdict
import json
import numpy as np

ROOT=Path(__file__).resolve().parent.parent
DATA=json.loads((ROOT/'map-concepts/skins-reststop-2026-09-12/footwear-fit-geometry.json').read_text(encoding='utf-8'))
OUT=ROOT/'outputs/skin-fit-2026-09-12';OUT.mkdir(parents=True,exist_ok=True)
assert DATA['blendShapes']==0 and DATA['colors']==0 and DATA['uv2']==0,'Export additional source attributes before extending this mesh'
PLANE=.00095
feet=[i for i,name in enumerate(DATA['bones']) if name.endswith('leg2')]
def packed_weight(values):
    pairs=sorted(((k,v) for k,v in values.items() if v>1e-8),key=lambda item:-item[1])[:4]
    total=sum(v for _,v in pairs);result=[]
    for index,weight in pairs:result.extend([index,weight/total])
    return result+[0,0]*(4-len(pairs))
def weights_dict(packed):return {int(packed[i]):packed[i+1] for i in range(0,8,2) if packed[i+1]>0}
def mix_weights(first,second,t):
    result=defaultdict(float)
    for index,value in weights_dict(first).items():result[index]+=value*(1-t)
    for index,value in weights_dict(second).items():result[index]+=value*t
    return packed_weight(result)

vertices=[np.array(v) for v in DATA['vertices']];normals=[np.array(v) for v in DATA['normals']]
uv=[np.array(v) for v in DATA['uv']];weights=[list(v) for v in DATA['weights']];triangles=[];intersections={}
def cut(a,b):
    key=tuple(sorted((a,b)))
    if key in intersections:return intersections[key]
    t=(PLANE-vertices[a][1])/(vertices[b][1]-vertices[a][1]);index=len(vertices)
    v=vertices[a]*(1-t)+vertices[b]*t;v[1]=PLANE;vertices.append(v)
    n=normals[a]*(1-t)+normals[b]*t;normals.append(n/np.linalg.norm(n));uv.append(uv[a]*(1-t)+uv[b]*t);weights.append(mix_weights(weights[a],weights[b],t));intersections[key]=index;return index
for face in np.array(DATA['triangles']).reshape(-1,3):
    # Central keel/tail fins also pass below ankle height; keep their narrow X band.
    if all(abs(vertices[i][0])<=.0003 for i in face):
        triangles.extend(int(i) for i in face);continue
    polygon=[]
    for a,b in zip(face,np.roll(face,-1)):
        inside_a=vertices[a][1]>=PLANE;inside_b=vertices[b][1]>=PLANE
        if inside_a:polygon.append(int(a))
        if inside_a!=inside_b:polygon.append(cut(int(a),int(b)))
    for i in range(1,len(polygon)-1):triangles.extend([polygon[0],polygon[i],polygon[i+1]])

# Welding here is for topology queries only. Keep the existing UV seam vertices.
def position_key(index):return tuple(np.round(vertices[index]*1e8).astype(int))
edges=defaultdict(list);graph=defaultdict(set);representative={}
for face in np.array(triangles).reshape(-1,3):
    for a,b in zip(face,np.roll(face,-1)):
        ka,kb=position_key(a),position_key(b);representative[ka]=int(a);representative[kb]=int(b)
        edges[tuple(sorted((ka,kb)))].append((int(a),int(b)))
boundary=[pair[0] for pair in edges.values() if len(pair)==1]
for a,b in boundary:
    assert abs(vertices[a][1]-PLANE)<1e-9 and abs(vertices[b][1]-PLANE)<1e-9,'Unexpected non-ankle boundary'
    ka,kb=position_key(a),position_key(b);graph[ka].add(kb);graph[kb].add(ka)
assert all(len(adjacent)==2 for adjacent in graph.values()),'Ankle loop branches'
loops=[];visited=set()
for start in graph:
    if start in visited:continue
    stack=[start];part=set()
    while stack:
        key=stack.pop()
        if key in part:continue
        part.add(key);visited.add(key);stack.extend(graph[key]-part)
    loops.append(part)
if len(loops)!=4:
    print(json.dumps([{'count':len(loop),'center':np.mean([vertices[representative[key]] for key in loop],axis=0).tolist()} for loop in loops],indent=2))
assert len(loops)==4,f'Expected four clean ankle loops, got {len(loops)}'

source_vertices=np.array(DATA['sourceVertices']);source_weights=DATA['sourceWeights'];shoe_ids=np.unique(DATA['shoeTriangles'])
feet=[i for i,name in enumerate(DATA['bones']) if name.endswith('leg2')]
foot_profiles={}
for index in feet:
    def influence(vertex,bone):return weights_dict(source_weights[vertex]).get(bone,0)
    ids=[v for v in shoe_ids if influence(v,index)>.1 and all(influence(v,other)<=influence(v,index) for other in feet)]
    points=source_vertices[ids];bind=np.array(DATA['bindposes'][index]).reshape(4,4,order='F');ankle=np.linalg.inv(bind)[:3,3]
    foot_profiles[index]={'origin':np.array([ankle[0],points[:,1].min(),ankle[2]]),'length':float(np.ptp(points[:,2]))}

reports=[]
for item in DATA['shoes']:
    new_vertices=[v.copy() for v in vertices];new_normals=[v.copy() for v in normals];new_uv=[v.copy() for v in uv];new_weights=[w.copy() for w in weights];new_triangles=triangles.copy()
    points=np.array(item['vertices']);near_ankle=points[np.abs(points[:,2])<.12]
    collar=float(np.quantile(near_ankle[:,1],.9));extensions=[]
    for loop in loops:
        ids=[representative[key] for key in loop];center=np.mean([vertices[i] for i in ids],axis=0)
        bone=min(feet,key=lambda index:np.linalg.norm((center-foot_profiles[index]['origin'])[[0,2]]))
        profile=foot_profiles[bone];target=profile['origin'].copy();target[1]=min(PLANE,profile['origin'][1]+profile['length']*max(.04,collar-.035))
        down=PLANE-target[1];mapping={}
        for a,b in boundary:
            if position_key(a) not in loop:continue
            for i in (a,b):
                if i in mapping:continue
                mapping[i]=len(new_vertices)
                point=vertices[i].copy()
                if down>1e-8:point=target+(vertices[i]-center)*.8;point[1]=target[1]
                new_vertices.append(point);new_normals.append(normals[i].copy());new_uv.append(uv[i].copy());new_weights.append(weights[i].copy())
            aa,bb=mapping[a],mapping[b]
            if down>1e-8:new_triangles.extend([b,a,aa,b,aa,bb])
        average=defaultdict(float)
        for i in ids:
            for index,weight in weights_dict(weights[i]).items():average[index]+=weight/len(ids)
        cap=len(new_vertices);new_vertices.append(target if down>1e-8 else center);new_normals.append(np.array([0.,-1.,0.]));new_uv.append(np.mean([uv[i] for i in ids],axis=0));new_weights.append(packed_weight(average))
        for a,b in boundary:
            if position_key(a) in loop:new_triangles.extend([mapping[b],mapping[a],cap] if down>1e-8 else [b,a,cap])
        extensions.append({'bone':DATA['bones'][bone],'extension':float(down),'collarY':float(target[1]),'ringVertices':len(ids)})
    output={'vertices':[v.tolist() for v in new_vertices],'normals':[v.tolist() for v in new_normals],'uv':[v.tolist() for v in new_uv],'weights':new_weights,'triangles':new_triangles}
    # Verify every geometric edge is paired before handing the mesh to Unity.
    counts=defaultdict(int)
    for face in np.array(new_triangles).reshape(-1,3):
        keys=[tuple(np.round(new_vertices[i]*1e8).astype(int)) for i in face]
        for a,b in zip(keys,keys[1:]+keys[:1]):counts[tuple(sorted((a,b)))]+=1
    open_edges=sum(n==1 for n in counts.values());assert open_edges==0,(item['key'],open_edges)
    (OUT/(item['key']+'-body.json')).write_text(json.dumps(output,separators=(',',':')),encoding='utf-8')
    reports.append({'key':item['key'],'triangles':len(new_triangles)//3,'vertices':len(new_vertices),'openEdges':open_edges,'extensions':extensions})
(OUT/'body-fit-report.json').write_text(json.dumps({'clipPlane':PLANE,'loopCount':len(loops),'items':reports},indent=2),encoding='utf-8')
print(json.dumps({'loops':len(loops),'items':[{k:v for k,v in row.items() if k!='extensions'} for row in reports]},indent=2))
