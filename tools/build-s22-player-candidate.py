"""Proportion candidate: retain the actual shark/UVs, use two feet and a shod tail.

No runtime asset is changed here. A translated proxy of the existing rear-left
limb will preserve its footfall animation without rotating the new offset.
"""
import bpy,json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1];OUT=ROOT/'outputs/s22-polish-2026-10-01'
d=json.loads((OUT/'player-geometry-before.json').read_text(encoding='utf-8'))
scale=2.8/(max(v[1] for v in d['vertices'])-min(v[1] for v in d['vertices']))
v=[(p[0]*scale,-p[2]*scale,p[1]*scale) for p in d['vertices']]
uv=d['uv'];verts=[];uvs=[];faces=[];slots=[];weights=[];mapping={}
offset=(.52,1.50,0)
def weight(i,indices):
 w=d['weights'][i];return sum(w[k+1] for k in range(0,8,2) if int(w[k]) in indices)
def addtri(tri,slot,copy=False):
 mapped=[]
 for i in tri:
  key=(i,copy)
  if key not in mapping:
   mapping[key]=len(verts);p=v[i];verts.append(tuple(p[k]+(offset[k] if copy else 0) for k in range(3)));uvs.append(uv[i]);w=list(d['weights'][i])
   if copy:
    for k in range(0,8,2):
     if int(w[k]) in (6,7,8,9):w[k]+=21
   weights.append(w)
  mapped.append(mapping[key])
 faces.append(mapped);slots.append(slot)
for slot,tris in enumerate(d['triangles']):
 for j in range(0,len(tris),3):
  tri=tris[j:j+3];center=[sum(v[i][k] for i in tri)/3 for k in range(3)]
  rear=sum(weight(i,range(6,14)) for i in tri)/3>.25 and center[2]<1.46
  tail=center[1]>1.60
  if not rear and not tail:addtri(tri,slot)
  left=sum(weight(i,range(6,10)) for i in tri)/3>.25 and center[2]<1.46 and center[0]<0
  if left:addtri(tri,slot,True)
bpy.ops.wm.read_factory_settings(use_empty=True);m=bpy.data.meshes.new('Two feet and tail candidate');m.from_pydata(verts,[],faces);m.update();o=bpy.data.objects.new('Tail-foot candidate',m);bpy.context.collection.objects.link(o)
layer=m.uv_layers.new(name='UVMap')
for p,slot in zip(m.polygons,slots):
 p.material_index=slot;p.use_smooth=True
 for li in p.loop_indices:layer.data[li].uv=uvs[m.loops[li].vertex_index]
for t in d['textures']:
 mat=bpy.data.materials.new(t['key']);mat.use_nodes=True;bs=mat.node_tree.nodes.get('Principled BSDF');tex=mat.node_tree.nodes.new('ShaderNodeTexImage');tex.image=bpy.data.images.load(str(ROOT/t['path']));mat.node_tree.links.new(tex.outputs['Color'],bs.inputs['Base Color']);bs.inputs['Roughness'].default_value=.65;m.materials.append(mat)
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'player-tail-candidate-v1.blend'))
print('candidate triangles',len(faces),'vertices',len(verts),'offset',offset)
