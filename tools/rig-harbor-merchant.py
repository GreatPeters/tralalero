"""Rig the reviewed TRELLIS merchant for a grounded seated rest and stand/wave greeting."""
import bpy,bmesh,json,math,heapq,sys
from pathlib import Path
import numpy as np
from mathutils import Vector,Quaternion

ROOT=Path(__file__).resolve().parents[1]
SOURCE=next((ROOT/'outputs/harbor-opening-refinement-2026-09-16/trellis').glob('02-merchant_*/model.glb'))
OUT=ROOT/'outputs/feedback-2026-09-19/merchant-rigged';OUT.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.gltf(filepath=str(SOURCE))
meshes=[o for o in bpy.context.scene.objects if o.type=='MESH']
bpy.ops.object.select_all(action='DESELECT')
for o in meshes:o.select_set(True)
bpy.context.view_layer.objects.active=meshes[0];bpy.ops.object.join();mesh=bpy.context.object;mesh.name='MerchantBody'
bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
points=[v.co for v in mesh.data.vertices];minimum=Vector(tuple(min(p[i] for p in points) for i in range(3)));maximum=Vector(tuple(max(p[i] for p in points) for i in range(3)))
H=2.6;scale=H/(maximum.z-minimum.z);center=Vector(((minimum.x+maximum.x)/2,(minimum.y+maximum.y)/2,minimum.z))
for v in mesh.data.vertices:v.co=(v.co-center)*scale
bm=bmesh.new();bm.from_mesh(mesh.data);bmesh.ops.dissolve_degenerate(bm,dist=1e-6,edges=list(bm.edges));bm.to_mesh(mesh.data);bm.free()
specs={'Root':((0,0,0),(0,0,.08),None),'Hips':((0,0,.37),(0,0,.47),'Root'),'Spine':((0,0,.47),(0,0,.59),'Hips'),'Chest':((0,0,.59),(0,0,.74),'Spine'),'Neck':((0,0,.74),(0,0,.82),'Chest'),'Head':((0,0,.82),(0,0,1),'Neck')}
for side,sign in [('L',1),('R',-1)]:
 specs.update({
  'UpperArm.'+side:((sign*.205,0,.73),(sign*.32,0,.55),'Chest'),
  'Forearm.'+side:((sign*.32,0,.55),(sign*.37,-.01,.37),'UpperArm.'+side),
  'Hand.'+side:((sign*.37,-.01,.37),(sign*.39,-.01,.31),'Forearm.'+side),
  'Thigh.'+side:((sign*.105,0,.37),(sign*.115,0,.20),'Hips'),
  'Shin.'+side:((sign*.115,0,.20),(sign*.125,0,.075),'Thigh.'+side),
  'Foot.'+side:((sign*.125,0,.075),(sign*.125,-.13,.045),'Shin.'+side)})
armature=bpy.data.armatures.new('MerchantSkeleton');rig=bpy.data.objects.new('MerchantRig',armature);bpy.context.collection.objects.link(rig)
mesh.select_set(False);rig.select_set(True);bpy.context.view_layer.objects.active=rig;bpy.ops.object.mode_set(mode='EDIT')
for name,(head,tail,parent) in specs.items():
 bone=armature.edit_bones.new(name);bone.head=Vector(head)*H;bone.tail=Vector(tail)*H
 if parent:bone.parent=armature.edit_bones[parent]
bpy.ops.object.mode_set(mode='OBJECT');mesh.parent=rig;modifier=mesh.modifiers.new('Merchant deformation','ARMATURE');modifier.object=rig
groups={name:mesh.vertex_groups.new(name=name) for name in specs if name!='Root'}
# Geodesic weights keep the broad apron on the torso instead of the nearby hands.
coords=np.array([v.co[:] for v in mesh.data.vertices])/H
_,representatives,inverse=np.unique(np.round(coords/1e-5).astype(np.int64),axis=0,return_index=True,return_inverse=True)
points=coords[representatives];adj=[{} for _ in points]
for polygon in mesh.data.polygons:
 ids=[int(inverse[v]) for v in polygon.vertices]
 for a,b in zip(ids,ids[1:]+ids[:1]):
  if a!=b:distance=float(np.linalg.norm(points[a]-points[b]));adj[a][b]=distance;adj[b][a]=distance
names=list(groups);fields=[]
for name in names:
 a,b,_=specs[name];a=np.array(a);b=np.array(b);distances=np.full(len(points),np.inf);queue=[]
 for t in [.2,.5,.8]:
  euclidean=np.linalg.norm(points-(a+(b-a)*t),axis=1)
  for seed in np.argsort(euclidean)[:6]:
   initial=float(euclidean[seed])
   if initial<distances[seed]:distances[seed]=initial;heapq.heappush(queue,(initial,int(seed)))
 while queue:
  distance,current=heapq.heappop(queue)
  if distance>distances[current]:continue
  for neighbor,cost in adj[current].items():
   candidate=distance+cost
   if candidate<distances[neighbor]:distances[neighbor]=candidate;heapq.heappush(queue,(candidate,neighbor))
 fields.append(distances)
fields=np.array(fields)
component_ids=np.full(len(points),-1,dtype=int);components=[]
for seed in range(len(points)):
 if component_ids[seed]>=0:continue
 cid=len(components);stack=[seed];members=[];component_ids[seed]=cid
 while stack:
  current=stack.pop();members.append(current)
  for neighbor in adj[current]:
   if component_ids[neighbor]<0:component_ids[neighbor]=cid;stack.append(neighbor)
 components.append(members)
def segment_distance(p,a,b):
 d=b-a;t=max(0,min(1,(p-a).dot(d)/max(d.length_squared,1e-8)));return (p-(a+d*t)).length
fallback={cid:min(names,key=lambda n:segment_distance(Vector(points[members].mean(axis=0)),Vector(specs[n][0]),Vector(specs[n][1]))) for cid,members in enumerate(components)}
for vertex in mesh.data.vertices:
 p=vertex.co/H
 if .30<p.z<.48 and abs(p.x)<.25:
  groups['Hips'].add([vertex.index],1,'REPLACE');continue
 key=int(inverse[vertex.index]);dist=fields[:,key];order=np.argsort(dist)[:2]
 if (abs(p.x)<.22 or (abs(p.x)<.285 and p.y<-.065)) and .48<=p.z<.65:
  # The generated apron touches the glove in the source mesh. Prevent
  # geodesic arm seeds from dragging its front into a long strip on waving.
  # Do not rigidly pin the inner/back sleeve to the spine: it belongs to the arm.
  torso=['Hips','Spine','Chest'];name=min(torso,key=lambda n:segment_distance(p,Vector(specs[n][0]),Vector(specs[n][1])))
  groups[name].add([vertex.index],1,'REPLACE');continue
 if not np.isfinite(dist[order[0]]):
  name=fallback[int(component_ids[key])];groups[name].add([vertex.index],1,'REPLACE');continue
 if names[order[0]].startswith('Foot') and vertex.co.z/H<.13:groups[names[order[0]]].add([vertex.index],1,'REPLACE');continue
 weights=[1/max(float(dist[i]),.015)**4 if np.isfinite(dist[i]) else 0 for i in order];total=sum(weights)
 for i,w in zip(order,weights):
  if w:groups[names[i]].add([vertex.index],w/total,'REPLACE')

# Diffuse across welded seam vertices, not duplicated UV vertices. Hard torso
# masks formerly pinned one side of a sleeve triangle while its neighbour waved.
weights=np.zeros((len(points),len(names)),dtype=np.float64)
for key,index in enumerate(representatives):
 for g in mesh.data.vertices[int(index)].groups:weights[key,g.group]=g.weight
edge_a=np.array([a for a,neighbors in enumerate(adj) for _ in neighbors],dtype=int)
edge_b=np.array([b for neighbors in adj for b in neighbors],dtype=int)
degree=np.bincount(edge_a,minlength=len(points)).clip(1)[:,None]
fixed=(np.abs(points[:,0])<.16)|((np.abs(points[:,0])>.35)&(points[:,2]<.46))|(points[:,2]<.15)|(points[:,2]>.8)
for _ in range(18):
 sums=np.zeros_like(weights);np.add.at(sums,edge_a,weights[edge_b])
 relaxed=.45*weights+.55*sums/degree;relaxed[fixed]=weights[fixed];weights=relaxed
for group in groups.values():group.remove(range(len(mesh.data.vertices)))
for vertex in mesh.data.vertices:
 row=weights[int(inverse[vertex.index])];order=np.argsort(row)[-4:];total=row[order].sum()
 for i in order:
  if row[i]>0:groups[names[i]].add([vertex.index],float(row[i]/total),'REPLACE')
def rotate(name,axis,degrees):
 bone=rig.pose.bones[name];rest=bone.bone.matrix_local.to_quaternion();bone.rotation_mode='QUATERNION';bone.rotation_quaternion=rest.inverted()@Quaternion(Vector(axis),math.radians(degrees))@rest
def smooth(value):v=max(0,min(1,value));return v*v*(3-2*v)
scene=bpy.context.scene;scene.render.fps=30;rig.animation_data_create();actions=[]
for action_name in ['SeatedIdle','StandGreet']:
 action=bpy.data.actions.new(action_name);action.use_fake_user=True;rig.animation_data.action=action
 for frame in range(1,122):
  t=(frame-1)/30;rise=0 if action_name=='SeatedIdle' else smooth(t/.7)*(1-smooth((t-2.7)/1.3));sitting=1-rise
  for bone in rig.pose.bones:bone.rotation_mode='QUATERNION';bone.rotation_quaternion=Quaternion();bone.location=(0,0,0)
  drop=H*.17*(1-math.cos(math.radians(80*sitting)))
  hips=rig.pose.bones['Hips'];hips.location=hips.bone.matrix_local.to_3x3().inverted()@Vector((0,0,-drop))
  for side in ['L','R']:
   rotate('Thigh.'+side,(1,0,0),-80*sitting);rotate('Shin.'+side,(1,0,0),80*sitting)
   rotate('UpperArm.'+side,(1,0,0),-12*sitting);rotate('Forearm.'+side,(1,0,0),-24*sitting)
  rotate('Chest',(1,0,0),math.sin(t*math.tau/4)*1.2)
  if rise:
   rotate('UpperArm.R',(0,1,0),55*rise);rotate('Forearm.R',(0,1,0),35*rise);rotate('Hand.R',(0,0,1),math.sin(t*math.tau*1.7)*14*rise)
  for bone in rig.pose.bones:bone.keyframe_insert('rotation_quaternion',frame=frame);bone.keyframe_insert('location',frame=frame)
 actions.append(action)
rig.animation_data.action=actions[0];scene.frame_set(1)
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'Merchant.blend'))
bpy.ops.object.select_all(action='DESELECT');mesh.select_set(True);rig.select_set(True);bpy.context.view_layer.objects.active=rig
bpy.ops.export_scene.fbx(filepath=str(OUT/'Merchant.fbx'),use_selection=True,add_leaf_bones=False,bake_anim=True,bake_anim_use_all_actions=True,bake_anim_use_nla_strips=False,path_mode='COPY',embed_textures=True)
bpy.ops.export_scene.gltf(filepath=str(OUT/'Merchant.glb'),export_format='GLB',use_selection=True,export_animations=True,export_animation_mode='ACTIONS')
report=dict(source=str(SOURCE.relative_to(ROOT)),height=H,triangles=sum(len(p.vertices)-2 for p in mesh.data.polygons),bones=len(armature.bones),actions=[a.name for a in actions],seatedHipDrop=H*.17*(1-math.cos(math.radians(80))))
(OUT/'rig-report.json').write_text(json.dumps(report,indent=2),encoding='utf8')
rig.animation_data.action=actions[1];scene.frame_set(31);bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'Merchant-Greeting.blend'))
print(json.dumps(report),flush=True)
