"""Open the generated helmet's face port without deleting the surrounding rim."""
import json,hashlib,math
from pathlib import Path
import numpy as np
root=Path(__file__).resolve().parent.parent;folder=root/'map-concepts/skins-reststop-2026-09-12'
data=json.loads((folder/'diver-aperture-geometry.json').read_text(encoding='utf-8'))
attrs=np.column_stack([data['vertices'],data['normals'],data['uv']]);faces=np.array(data['triangles']).reshape(-1,3)
vertices=[];normals=[];uv=[];triangles=[];cache={};removed=0
def split(poly,axis,distance):
    inside=[];outside=[]
    for a,b in zip(poly,poly[1:]+poly[:1]):
        da=float(np.dot(axis,a[:3])-distance);db=float(np.dot(axis,b[:3])-distance)
        (inside if da<=1e-9 else outside).append(a)
        if (da< -1e-9 and db>1e-9) or (da>1e-9 and db< -1e-9):
            point=a+(b-a)*(da/(da-db));inside.append(point);outside.append(point)
    return inside,outside
def emit(poly):
    if len(poly)<3:return
    indices=[]
    for item in poly:
        key=tuple(np.round(item*1e7).astype(int))
        if key not in cache:
            cache[key]=len(vertices);vertices.append(item[:3].tolist());n=item[3:6];normals.append((n/max(1e-12,np.linalg.norm(n))).tolist());uv.append(item[6:8].tolist())
        indices.append(cache[key])
    for i in range(1,len(indices)-1):
        a,b,c=(np.array(vertices[j]) for j in [indices[0],indices[i],indices[i+1]])
        if np.linalg.norm(np.cross(b-a,c-a))>1e-12:triangles.extend([indices[0],indices[i],indices[i+1]])
planes=[]
for index in range(48):
    angle=2*math.pi*(index+.5)/48;nx=math.cos(angle)/.258;ny=math.sin(angle)/.244
    planes.append((np.array([nx,ny,0]),1+ny*.60))
for face in faces:
    poly=[attrs[i].copy() for i in face];points=attrs[face,:3]
    if points[:,2].max()<=.1 or points[:,0].min()>.258 or points[:,0].max()<-.258 or points[:,1].min()>.844 or points[:,1].max()<.356:
        emit(poly);continue
    front,back=split(poly,np.array([0,0,-1]),-.1);emit(back)
    for axis,distance in planes:
        if len(front)<3:break
        front,outside=split(front,axis,distance);emit(outside)
    if len(front)>=3:removed+=1
result={'sourceFile':data['mesh'],'sourceSha256':hashlib.sha256((root/data['mesh']).read_bytes()).hexdigest(),'vertices':vertices,'normals':normals,'uv':uv,'triangles':triangles,'clippedFaces':removed,'portCenter':[0,.60],'portRadius':[.258,.244],'depth':.1}
(folder/'diver-open-port.json').write_text(json.dumps(result,separators=(',',':')),encoding='utf-8')
print(json.dumps({'vertices':len(vertices),'triangles':len(triangles)//3,'clippedFaces':removed}))
