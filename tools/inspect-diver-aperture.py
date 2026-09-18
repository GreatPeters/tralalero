import json
from pathlib import Path
import numpy as np
from PIL import Image
root=Path(__file__).resolve().parent.parent
folder=root/'map-concepts/skins-reststop-2026-09-12';data=json.loads((folder/'diver-aperture-geometry.json').read_text(encoding='utf-8'))
v=np.array(data['vertices']);uv=np.array(data['uv']);faces=np.array(data['triangles']).reshape(-1,3);p=v[faces].mean(1);coords=uv[faces].mean(1)
texture=np.array(Image.open(root/data['texture']).convert('RGB'))/255
colors=texture[np.clip(((1-coords[:,1])*(texture.shape[0]-1)).astype(int),0,texture.shape[0]-1),np.clip((coords[:,0]*(texture.shape[1]-1)).astype(int),0,texture.shape[1]-1)]
low=v.min(0);high=v.max(0);size=high-low
selected=(np.max(colors,axis=1)<.22)&(p[:,1]>low[1]+size[1]*.3)&(p[:,2]>low[2]+size[2]*.6)&(abs(p[:,0])<size[0]*.36)
points=p[selected]
print(json.dumps({'bounds':[low.tolist(),high.tolist()],'darkFaces':int(selected.sum()),'darkBounds':[points.min(0).tolist(),points.max(0).tolist()]},indent=2))
(folder/'diver-aperture-candidates.json').write_text(json.dumps(np.flatnonzero(selected).tolist()),encoding='utf-8')
# The front render locates the actual inner port; side hoses and dome shadows
# must not grow the removal mask beyond the rubber gasket.
inside=(p[:,0]/.26)**2+((p[:,1]-.60)/.25)**2<1
remove=np.flatnonzero(inside&(p[:,2]>.10))
import hashlib
cut={'sourceFile':data['mesh'],'sourceSha256':hashlib.sha256((root/data['mesh']).read_bytes()).hexdigest(),'vertices':len(v),'triangles':len(faces),'removeTriangles':remove.tolist(),'portCenter':[0,.60],'portRadius':[.26,.25],'depth':.10}
(folder/'diver-aperture-cut.json').write_text(json.dumps(cut,indent=2),encoding='utf-8')
(folder/'diver-aperture-candidates.json').write_text(json.dumps(remove.tolist()),encoding='utf-8')
print(json.dumps({'removeFaces':len(remove),'portCenter':[0,.60],'portRadius':[.26,.25]},indent=2))