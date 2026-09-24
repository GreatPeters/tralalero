"""Reuse six existing project vehicle meshes with their packed base textures."""
import bpy,bmesh,math
from pathlib import Path
from mathutils import Vector,Matrix
import geometry as g
ROOT=Path.cwd()
SPECS={'054':('배송 트럭',(3.0,6.5,3.15)),'067':('흰색 승용차',(2.7688,4.6671,1.737)),'080':('파란 버스',(3.55,10.4,3.7)),'081':('빨간 소형차',(2.6,4.25,1.73)),'082':('초록 SUV',(2.9549,4.6808,2.2115)),'083':('노란 박스 트럭',(3.25,6.8,3.8))}
def load(number):
 folder=ROOT/'Assets/ShooterSurvival/Models/Highway/Props'/number
 file=ROOT/'Assets/ShooterSurvival/Models/Chapters/Props/067/Model.fbx' if number=='067' else folder/'Model.fbx'
 before=set(bpy.context.scene.objects);bpy.ops.import_scene.fbx(filepath=str(file));items=[o for o in bpy.context.scene.objects if o not in before];names=[o.name for o in items];meshes=[o for o in items if o.type=='MESH']
 for o in items:o.animation_data_clear()
 for o in meshes:matrix=o.matrix_world.copy();o.parent=None;o.matrix_world=matrix
 bpy.ops.object.select_all(action='DESELECT')
 for o in meshes:o.select_set(True)
 bpy.context.view_layer.objects.active=meshes[0];bpy.ops.object.join();o=bpy.context.object;bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
 def dims():return [max(v.co[i] for v in o.data.vertices)-min(v.co[i] for v in o.data.vertices) for i in range(3)]
 # These native FBX imports already have Z up and their body length along Y.
 # A truck can be taller than it is wide; dimensions alone cannot infer up.
 ds=dims();print('VEHICLE_RAW_AXES',number,ds,flush=True)
 if number in ('054','080'):
  bm=bmesh.new();bm.from_mesh(o.data);bmesh.ops.remove_doubles(bm,verts=list(bm.verts),dist=max(ds)*1e-6);bm.normal_update();base=min(v.co.z for v in bm.verts)
  body=[v.co for v in bm.verts if v.co.z>base+ds[2]*.25]
  for axis in (0,1):
   low=min(v[axis] for v in body)-ds[2]*.018;high=max(v[axis] for v in body)+ds[2]*.018
   for bound,sign in ((low,-1),(high,1)):
    point=[0,0,0];point[axis]=bound;normal=[0,0,0];normal[axis]=sign;bmesh.ops.bisect_plane(bm,geom=list(bm.verts)+list(bm.edges)+list(bm.faces),plane_co=point,plane_no=normal,clear_outer=True,clear_inner=False,dist=.000001)
  remaining=set(bm.verts);remove=[]
  while remaining:
   seed=remaining.pop();component={seed};todo=[seed]
   while todo:
    v=todo.pop()
    for edge in v.link_edges:
     other=edge.other_vert(v)
     if other in remaining:remaining.remove(other);component.add(other);todo.append(other)
   low=min(v.co.z for v in component);high=max(v.co.z for v in component);span=[max(v.co[i] for v in component)-min(v.co[i] for v in component) for i in range(2)]
   if high-base<ds[2]*.10 and high-low<ds[2]*.05 and max(span)>.35*max(ds[:2]):remove.extend(component)
  if remove:bmesh.ops.delete(bm,geom=remove,context='VERTS')
  # Generated source support planes can be welded to tires. Remove only the
  # upward-facing, almost horizontal bottom sheet, keeping downward tire faces.
  bm.normal_update();faces=[f for f in bm.faces if abs(f.normal.z)>.90 and max(v.co.z for v in f.verts)<base+ds[2]*.18]
  if faces:bmesh.ops.delete(bm,geom=faces,context='FACES')
  loose=[v for v in bm.verts if not v.link_faces]
  if loose:bmesh.ops.delete(bm,geom=loose,context='VERTS')
  bm.to_mesh(o.data);bm.free();print('VEHICLE_SUPPORT_REMOVED',number,len(remove),len(faces),dims(),flush=True)
 lo=Vector(tuple(min(v.co[i] for v in o.data.vertices) for i in range(3)));hi=Vector(tuple(max(v.co[i] for v in o.data.vertices) for i in range(3)));center=Vector(((lo.x+hi.x)/2,(lo.y+hi.y)/2,lo.z));target=SPECS[number][1]
 for v in o.data.vertices:
  p=v.co-center;v.co=tuple(p[i]*target[i]/(hi[i]-lo[i]) for i in range(3))
 mat=g.material('V3_vehicle_'+number,(.6,.65,.67),.53,.08);bs=mat.node_tree.nodes.get('Principled BSDF');im=bpy.data.images.load(str(folder/'BaseColor.png'),check_existing=True);im.pack();tex=mat.node_tree.nodes.new('ShaderNodeTexImage');tex.image=im;mat.node_tree.links.new(tex.outputs['Color'],bs.inputs['Base Color']);mat.node_tree.nodes.active=tex
 o.data.materials.clear();o.data.materials.append(mat)
 for p in o.data.polygons:p.material_index=0
 data=o.data;data.name='Vehicle_'+number+'_shared';data.use_fake_user=True
 for name in names:
  item=bpy.data.objects.get(name)
  if item is not None:bpy.data.objects.remove(item,do_unlink=True)
 return data
def attach(root,number,data):
 o=bpy.data.objects.new(root.name+'_body_'+number,data);bpy.context.scene.collection.objects.link(o);o.parent=root;o['zone']='VEHICLES';root['source_model']=number;root['vehicle_type']=SPECS[number][0];return o
