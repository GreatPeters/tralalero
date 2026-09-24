"""Fit a deformation rig and readable actions to the new TRELLIS bodies."""
import argparse,json,math,sys,heapq
from pathlib import Path
from statistics import median
import bpy,bmesh
import numpy as np
from mathutils import Vector,Quaternion

parser=argparse.ArgumentParser();parser.add_argument('--source',required=True);parser.add_argument('--name',required=True);parser.add_argument('--output',required=True)
args=parser.parse_args(sys.argv[sys.argv.index('--')+1:]);out=Path(args.output)
if (out/(args.name+'.blend')).exists():raise RuntimeError('Preserve previous rig; choose a new output revision')
out.mkdir(parents=True,exist_ok=True);bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=str(Path(args.source).resolve()))
meshes=[o for o in bpy.context.scene.objects if o.type=='MESH'];bpy.ops.object.select_all(action='DESELECT')
for obj in meshes:obj.select_set(True)
bpy.context.view_layer.objects.active=meshes[0];bpy.ops.object.join();body=bpy.context.object;body.name=args.name+'_Body'
bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
points=[v.co for v in body.data.vertices];zmin=min(p.z for p in points);zmax=max(p.z for p in points);raw=zmax-zmin
upper=[p for p in points if zmin+raw*.74<p.z<zmin+raw*.9];cx=median(p.x for p in upper);cy=median(p.y for p in upper)
height=3.35 if args.name in ('TireBruiser','TollgateChief') else 3.0
for v in body.data.vertices:v.co=(v.co-Vector((cx,cy,zmin)))*(height/raw)
bm=bmesh.new();bm.from_mesh(body.data);bmesh.ops.dissolve_degenerate(bm,dist=1e-6,edges=list(bm.edges));bm.to_mesh(body.data);bm.free();body.data.update()
for mat in body.data.materials:
    if mat is None or not mat.use_nodes:continue
    shader=mat.node_tree.nodes.get('Principled BSDF')
    if shader is None:continue
    for link in list(mat.node_tree.links):
        if link.to_node==shader and link.to_socket.name in ('Normal','Roughness','Metallic'):mat.node_tree.links.remove(link)
    shader.inputs['Roughness'].default_value=.82;shader.inputs['Metallic'].default_value=0
    if 'Specular IOR Level' in shader.inputs:shader.inputs['Specular IOR Level'].default_value=.12

coords=np.array([v.co[:] for v in body.data.vertices],dtype=float)/height
specs={'Root':((0,0,0),(0,0,.06),None),'Hips':((0,0,.35),(0,0,.43),'Root'),
       'Spine':((0,0,.43),(0,0,.51),'Hips'),'Chest':((0,0,.51),(0,0,.61),'Spine'),
       'Neck':((0,0,.61),(0,0,.675),'Chest'),'Head':((0,0,.675),(0,0,.97),'Neck')}
for side,sign in [('L',1),('R',-1)]:
    def fit(z0,z1,x0,fallback):
        sample=coords[(coords[:,2]>z0)&(coords[:,2]<z1)&(coords[:,0]*sign>x0)]
        return tuple(np.median(sample,axis=0)) if len(sample)>8 else fallback
    wrist=fit(.315,.365,.22,(sign*.29,0,.34));elbow=fit(.46,.5,.18,(sign*.23,0,.48))
    shoulder=(sign*(.17 if args.name=='TireBruiser' else .145),elbow[1],.595)
    hip=(sign*.10,0,.35);knee=(sign*.11,0,.20);ankle=(sign*.12,0,.065)
    specs.update({'UpperArm.'+side:(shoulder,elbow,'Chest'),'Forearm.'+side:(elbow,wrist,'UpperArm.'+side),
        'Hand.'+side:(wrist,(wrist[0]+sign*.02,wrist[1]-.015,wrist[2]-.065),'Forearm.'+side),
        'Thigh.'+side:(hip,knee,'Hips'),'Shin.'+side:(knee,ankle,'Thigh.'+side),
        'Foot.'+side:(ankle,(sign*.12,-.13,.045),'Shin.'+side)})
armature=bpy.data.armatures.new(args.name+'_Skeleton');rig=bpy.data.objects.new('Rig',armature);bpy.context.collection.objects.link(rig)
body.select_set(False);rig.select_set(True);bpy.context.view_layer.objects.active=rig;bpy.ops.object.mode_set(mode='EDIT')
for name,(a,b,parent) in specs.items():
    bone=armature.edit_bones.new(name);bone.head=Vector(a)*height;bone.tail=Vector(b)*height
    if parent:bone.parent=armature.edit_bones[parent]
bpy.ops.object.mode_set(mode='OBJECT');body.parent=rig;body.modifiers.new('Deformation','ARMATURE').object=rig

# Geodesic distance follows each separated limb rather than leaking across the
# small Euclidean gap between sleeve, torso and neighboring hand.
_,rep,inverse=np.unique(np.round(coords/1e-5).astype(np.int64),axis=0,return_index=True,return_inverse=True)
points=coords[rep];adj=[{} for _ in points]
for polygon in body.data.polygons:
    ids=[int(inverse[v]) for v in polygon.vertices]
    for a,b in zip(ids,ids[1:]+ids[:1]):
        if a!=b:adj[a][b]=adj[b][a]=float(np.linalg.norm(points[a]-points[b]))
names=[n for n in specs if n!='Root'];groups={n:body.vertex_groups.new(name=n) for n in names};fields=[]
for name in names:
    a,b,_=specs[name];a=np.array(a);b=np.array(b);distances=np.full(len(points),np.inf);queue=[]
    for t in [.2,.5,.8]:
        distance=np.linalg.norm(points-(a+(b-a)*t),axis=1)
        for seed in np.argsort(distance)[:7]:
            if distance[seed]<distances[seed]:distances[seed]=distance[seed];heapq.heappush(queue,(float(distance[seed]),int(seed)))
    while queue:
        distance,i=heapq.heappop(queue)
        if distance>distances[i]:continue
        for j,cost in adj[i].items():
            trial=distance+cost
            if trial<distances[j]:distances[j]=trial;heapq.heappush(queue,(trial,j))
    fields.append(distances)
fields=np.array(fields)
def segment_distance(p,a,b):
    a=np.array(a);d=np.array(b)-a;t=np.clip(np.dot(p-a,d)/max(np.dot(d,d),1e-9),0,1);return np.linalg.norm(p-a-d*t)
for v in body.data.vertices:
    p=coords[v.index]
    if p[2]>.68:groups['Head'].add([v.index],1,'REPLACE');continue
    d=fields[:,inverse[v.index]];order=np.argsort(d)[:4]
    if not np.isfinite(d[order[0]]):
        n=min(names,key=lambda n:segment_distance(p,*specs[n][:2]));groups[n].add([v.index],1,'REPLACE');continue
    dominant=names[order[0]]
    if dominant.startswith('Foot') and p[2]<.11 or dominant.startswith('Hand') and p[2]<.36:
        groups[dominant].add([v.index],1,'REPLACE');continue
    weights=np.array([1/max(float(d[i]),.012)**5 if np.isfinite(d[i]) else 0 for i in order]);weights/=weights.sum()
    for i,w in zip(order,weights):
        if w>.001:groups[names[i]].add([v.index],float(w),'REPLACE')

# A broad front vest is a torso surface, although its nearest surface path may
# start at an upper-arm bone. Blend a torso volume anchor into those weights;
# fade the anchor through the armpit rather than making a hard sleeve seam.
torso_half={'AsphaltWorker':.19,'TireBruiser':.215,'TollgateChief':.18}.get(args.name,.145)
def sstep(a,b,value):
    t=max(0,min(1,(value-a)/(b-a)));return t*t*(3-2*t)
for v in body.data.vertices:
    p=coords[v.index];pin=(1-sstep(torso_half*.85,torso_half+.045,abs(p[0])))*sstep(.32,.4,p[2])*(1-sstep(.58,.645,p[2]))
    if pin<.001:continue
    old={body.vertex_groups[g.group].name:g.weight for g in v.groups}
    target={'Hips':1-sstep(.37,.48,p[2]),'Spine':sstep(.37,.48,p[2])*(1-sstep(.48,.59,p[2])),'Chest':sstep(.48,.59,p[2])}
    weights={name:old.get(name,0)*(1-pin)+target.get(name,0)*pin for name in names}
    best=sorted(weights,key=weights.get,reverse=True)[:4];total=sum(weights[n] for n in best)
    for name in old:groups[name].remove([v.index])
    for name in best:
        if weights[name]>.0001:groups[name].add([v.index],weights[name]/total,'REPLACE')

# Smooth the transition on welded surface adjacency. Core torso/head/hand/sole
# anchors stay fixed; duplicated UV vertices receive exactly the same weights.
weights=np.zeros((len(points),len(names)),dtype=float)
for key,index in enumerate(rep):
    for group in body.data.vertices[int(index)].groups:
        weights[key,names.index(body.vertex_groups[group.group].name)]=group.weight
fixed=np.zeros(len(points),dtype=bool)
for i,p in enumerate(points):
    torso_pin=(1-sstep(torso_half*.85,torso_half+.045,abs(p[0])))*sstep(.32,.4,p[2])*(1-sstep(.58,.645,p[2]))
    dominant=names[int(np.argmax(weights[i]))]
    fixed[i]=p[2]>.7 or torso_pin>.9 or dominant.startswith('Foot') and p[2]<.11 or dominant.startswith('Hand') and p[2]<.34
for _ in range(8):
    updated=weights.copy()
    for i,neighbors in enumerate(adj):
        if not fixed[i] and neighbors:updated[i]=weights[i]*.5+weights[list(neighbors)].mean(axis=0)*.5
    weights=updated
for v in body.data.vertices:
    w=weights[int(inverse[v.index])];best=np.argsort(w)[-4:];total=w[best].sum()
    for group in list(v.groups):body.vertex_groups[group.group].remove([v.index])
    for i in best:
        if w[i]>.0001:groups[names[int(i)]].add([v.index],float(w[i]/total),'REPLACE')

scene=bpy.context.scene;scene.render.fps=30;rig.animation_data_create();actions=[];checks=[]
def rotate(name,x=0,y=0,z=0):
    bone=rig.pose.bones[name];rest=bone.bone.matrix_local.to_quaternion()
    q=Quaternion((0,0,1),math.radians(z))@Quaternion((0,1,0),math.radians(y))@Quaternion((1,0,0),math.radians(x))
    bone.rotation_quaternion=rest.inverted()@q@rest
def move(name,world):
    bone=rig.pose.bones[name];bone.location=bone.bone.matrix_local.to_3x3().inverted()@Vector(world)
def smooth(t):t=max(0,min(1,t));return t*t*(3-2*t)
def envelope(t,a,b,c):
    return smooth((t-a)/(b-a)) if t<b else 1-smooth((t-b)/(c-b))
attack_seconds={'AsphaltWorker':1.5,'TireBruiser':1.6,'TrafficPatrol':1.0,'DeliveryRider':1.1,'TollgateChief':1.4}.get(args.name,1.2)
durations={'idle':2.4,'walk':.9,'run':.65,'attack_loop':attack_seconds,'attack_once':attack_seconds,'hit':.3,'die':1.0}
for action_name,duration in durations.items():
    frames=round(duration*30);action=bpy.data.actions.new(action_name);action.use_fake_user=True;rig.animation_data.action=action;scene.frame_start=1;scene.frame_end=frames+1
    for frame in range(1,frames+2):
        scene.frame_set(frame);t=(frame-1)/frames;wave=math.sin(t*math.tau)
        for bone in rig.pose.bones:bone.rotation_mode='QUATERNION';bone.rotation_quaternion=Quaternion();bone.location=(0,0,0);bone.scale=(1,1,1)
        rotate('Chest',x=wave*2.5,z=wave*.8);rotate('Head',y=wave*3.5,z=math.sin(t*math.tau+.8)*2)
        for side,sign in [('L',1),('R',-1)]:
            rotate('UpperArm.'+side,x=-8+wave*2.5*sign,y=sign*12);rotate('Forearm.'+side,x=-18+wave*3*sign)
        if action_name in ('walk','run'):
            amplitude=24 if action_name=='walk' else 35;rotate('Chest',x=5 if action_name=='walk' else 9)
            for side,sign in [('L',1),('R',-1)]:
                phase=wave*sign;rotate('Thigh.'+side,x=phase*amplitude);rotate('Shin.'+side,x=-max(0,phase)*38)
                rotate('Foot.'+side,x=-phase*8);rotate('UpperArm.'+side,x=-8-phase*18,y=sign*12);rotate('Forearm.'+side,x=-25-max(0,-phase)*15)
            move('Hips',(0,0,.035*abs(wave)))
        elif action_name.startswith('attack'):
            wind=envelope(t,0,.26,.52);strike=envelope(t,.26,.46,.82)
            rotate('Chest',x=-7*wind+13*strike,z=7*wind-10*strike);rotate('Head',x=4*wind-7*strike,z=-5*wind+5*strike)
            rotate('UpperArm.R',x=-8-98*wind-45*strike,y=-12-10*wind,z=-7*strike)
            rotate('Forearm.R',x=-18-42*wind+7*strike);rotate('Hand.R',x=-5*strike)
            rotate('UpperArm.L',x=-8-35*wind+12*strike,y=12);rotate('Forearm.L',x=-18-25*wind)
            move('Hips',(0,-.07*strike,0))
            if args.name=='AsphaltWorker':
                rotate('Chest',x=15*strike,z=15*wind-18*strike);rotate('UpperArm.R',x=-8-75*wind-55*strike,y=-12,z=10*wind-18*strike)
            elif args.name=='TireBruiser':
                rotate('Chest',x=-12*wind+18*strike,z=18*wind-22*strike);rotate('UpperArm.R',x=-8-110*wind-65*strike,y=-18);move('Hips',(0,-.12*strike,0))
            elif args.name=='TrafficPatrol':
                rotate('Chest',x=-7*strike,z=2*wind);rotate('UpperArm.R',x=-55+12*strike,y=-12);rotate('Forearm.R',x=-28);rotate('UpperArm.L',x=-22,y=15);rotate('Head',x=-3*strike)
            elif args.name=='DeliveryRider':
                rotate('UpperArm.R',x=-8+35*wind-85*strike,y=-12);rotate('Forearm.R',x=-18-12*wind+8*strike);rotate('Chest',x=-6*wind+13*strike,z=9*wind-13*strike)
            elif args.name=='TollgateChief':
                rotate('UpperArm.L',x=-35-35*wind,y=20,z=12*strike);rotate('Chest',x=-5*wind+9*strike,z=14*wind-16*strike)
        elif action_name=='hit':
            hit=envelope(t,0,.23,1);rotate('Chest',x=-15*hit,z=6*hit);rotate('Head',x=-12*hit);move('Hips',(0,.09*hit,0))
        elif action_name=='die':
            fall=smooth(t/.72);rotate('Root',x=-88*fall,z=-8*fall);rotate('Chest',x=-12*fall)
            rotate('UpperArm.R',x=-35*fall,y=-20);rotate('UpperArm.L',x=-45*fall,y=20)
        bpy.context.view_layer.update();dg=bpy.context.evaluated_depsgraph_get();evaluated=body.evaluated_get(dg);mesh=evaluated.to_mesh();low=min(v.co.z for v in mesh.vertices);evaluated.to_mesh_clear()
        rig.pose.bones['Root'].location+=rig.pose.bones['Root'].bone.matrix_local.to_3x3().inverted()@Vector((0,0,-low))
        bpy.context.view_layer.update()
        for bone in rig.pose.bones:bone.keyframe_insert('rotation_quaternion',frame=frame);bone.keyframe_insert('location',frame=frame)
        evaluated=body.evaluated_get(dg);mesh=evaluated.to_mesh();checked=min(v.co.z for v in mesh.vertices);evaluated.to_mesh_clear();checks.append({'action':action_name,'frame':frame,'ground':checked})
    actions.append(action)
rig.animation_data.action=actions[0];scene.frame_start=1;scene.frame_end=73;scene.frame_set(1)
bpy.ops.object.select_all(action='DESELECT');body.select_set(True);rig.select_set(True);bpy.context.view_layer.objects.active=rig
bpy.ops.wm.save_as_mainfile(filepath=str(out/(args.name+'.blend')))
bpy.ops.export_scene.fbx(filepath=str(out/(args.name+'.fbx')),use_selection=True,add_leaf_bones=False,bake_anim=True,bake_anim_use_all_actions=True,bake_anim_use_nla_strips=False,bake_anim_simplify_factor=0,path_mode='COPY',embed_textures=True)
bpy.ops.export_scene.gltf(filepath=str(out/(args.name+'.glb')),export_format='GLB',use_selection=True,export_animations=True,export_animation_mode='ACTIONS')
report={'name':args.name,'source':args.source,'height':height,'triangles':sum(len(p.vertices)-2 for p in body.data.polygons),'bones':len(armature.bones),'actions':durations,'skeleton':specs,'maximumGroundError':max(abs(c['ground']) for c in checks),'rigged':True,'equipmentPendingNativeBinding':True}
(out/'rig-report.json').write_text(json.dumps(report,indent=2));(out/'ground-checks.json').write_text(json.dumps(checks,indent=2))
print(json.dumps(report),flush=True)
