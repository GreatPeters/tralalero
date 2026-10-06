"""Inspect connected parts and a neutral view of the real catalog shark mesh."""
import bpy,json,math
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[1];OUT=ROOT/'outputs/s22-polish-2026-10-01'
bpy.ops.wm.read_factory_settings(use_empty=True)
src=ROOT/'Assets/ShooterSurvival/Models/MeshyRestStop20260925/N19_live_fish_tub/N19_live_fish_tub.fbx'
bpy.ops.import_scene.fbx(filepath=str(src));records=[]
for o in [x for x in bpy.context.scene.objects if x.type=='MESH']:
 adj=[set() for _ in o.data.vertices]
 for e in o.data.edges:a,b=e.vertices;adj[a].add(b);adj[b].add(a)
 pending=set(range(len(adj)))
 while pending:
  todo=[pending.pop()];part=set(todo)
  while todo:
   v=todo.pop()
   for n in adj[v]:
    if n in pending:pending.remove(n);part.add(n);todo.append(n)
  points=[o.matrix_world@o.data.vertices[i].co for i in part];lo=[min(p[i] for p in points) for i in range(3)];hi=[max(p[i] for p in points) for i in range(3)]
  records.append({'object':o.name,'vertices':len(part),'min':lo,'max':hi,'indices':sorted(part)})
(OUT/'n19-parts.json').write_text(json.dumps(records),encoding='utf-8')
bpy.ops.wm.read_factory_settings(use_empty=True)
d=json.loads((OUT/'player-geometry-before.json').read_text(encoding='utf-8'));raw=d['vertices'];scale=2.8/(max(v[1] for v in raw)-min(v[1] for v in raw));verts=[(v[0]*scale,-v[2]*scale,v[1]*scale) for v in raw];faces=[];slots=[]
for slot,tris in enumerate(d['triangles']):
 for i in range(0,len(tris),3):faces.append(tuple(tris[i:i+3]));slots.append(slot)
data=bpy.data.meshes.new('Original catalog mesh');data.from_pydata(verts,[],faces);data.update();o=bpy.data.objects.new('Current four-foot shark',data);bpy.context.collection.objects.link(o)
uv=data.uv_layers.new(name='UVMap')
for poly,slot in zip(data.polygons,slots):
 poly.material_index=slot;poly.use_smooth=True
 for li in poly.loop_indices:uv.data[li].uv=d['uv'][data.loops[li].vertex_index]
for t in d['textures']:
 m=bpy.data.materials.new(t['key']);m.use_nodes=True;bs=m.node_tree.nodes.get('Principled BSDF');tex=m.node_tree.nodes.new('ShaderNodeTexImage');tex.image=bpy.data.images.load(str(ROOT/t['path']));m.node_tree.links.new(tex.outputs['Color'],bs.inputs['Base Color']);bs.inputs['Roughness'].default_value=.65;data.materials.append(m)
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'player-neutral-before.blend'))
groups=[]
for i,name in enumerate(d['bones']):
 indices=[j for j,w in enumerate(d['weights']) if sum(w[k+1] for k in range(0,8,2) if int(w[k])==i)>.45]
 if not indices:continue
 vs=[verts[j] for j in indices];groups.append({'bone':name,'vertices':len(indices),'min':[min(v[k] for v in vs) for k in range(3)],'max':[max(v[k] for v in vs) for k in range(3)]})
(OUT/'player-bone-regions.json').write_text(json.dumps({'scale':scale,'groups':groups},indent=2),encoding='utf-8')
print('N19 components',len(records),'player scale',scale)
