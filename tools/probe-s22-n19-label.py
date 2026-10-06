import bpy,json,numpy as np
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.fbx(filepath=str(ROOT/'Assets/ShooterSurvival/Models/MeshyRestStop20260925/N19_live_fish_tub/N19_live_fish_tub.fbx'))
rects=[(.34,.43,.855,.95),(.575,.81,.38,.575),(.60,.75,.075,.19)]
image=bpy.data.images.load(str(ROOT/'Assets/ShooterSurvival/Models/MeshyRestStop20260925/N19_live_fish_tub/texture_0.png'));pixels=np.empty(image.size[0]*image.size[1]*4,dtype=np.float32);image.pixels.foreach_get(pixels);pixels=pixels.reshape(image.size[1],image.size[0],4)
def white(p):
 c=pixels[min(image.size[1]-1,max(0,int(p.y*image.size[1]))),min(image.size[0]-1,max(0,int(p.x*image.size[0]))),:3]
 return min(c)>.70 and max(c)-min(c)<.18
rows=[]
for o in bpy.context.scene.objects:
 if o.type!='MESH':continue
 uv=o.data.uv_layers.active
 for face in o.data.polygons:
  points=[uv.data[li].uv for li in face.loop_indices]
  center=sum(points,points[0]*0)/len(points)
  samples=points+[center]+[(points[i]+points[(i+1)%len(points)])*.5 for i in range(len(points))]
  if any(all(x0<=p.x<=x1 and y0<=p.y<=y1 for p in points) for x0,x1,y0,y1 in rects) and sum(white(p) for p in samples)>=len(samples)*.55:
   ps=[o.matrix_world@o.data.vertices[i].co for i in face.vertices];rows.append({'face':face.index,'min':[min(p[k] for p in ps) for k in range(3)],'max':[max(p[k] for p in ps) for k in range(3)]})
(ROOT/'outputs/s22-polish-2026-10-01/n19-label-faces.json').write_text(json.dumps(rows),encoding='utf-8');print(rows)
