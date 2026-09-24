"""Fit a compact humanoid, solve bone heat on a closed proxy, retain TRELLIS UVs."""
import argparse,json,math,runpy,sys
from collections import defaultdict
from pathlib import Path
import bpy
from mathutils import Vector,Quaternion
from mathutils.bvhtree import BVHTree
from mathutils.geometry import barycentric_transform

parser=argparse.ArgumentParser()
for key in ('source','output','name'):parser.add_argument('--'+key,required=True)
args=parser.parse_args(sys.argv[sys.argv.index('--')+1:])
source=Path(args.source).resolve();out=Path(args.output).resolve()
assert not (out/(args.name+'.blend')).exists(),'Preserve earlier candidates'
out.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=str(source))
meshes=[o for o in bpy.context.scene.objects if o.type=='MESH']
bpy.ops.object.select_all(action='DESELECT')
for o in meshes:o.select_set(True)
bpy.context.view_layer.objects.active=meshes[0];bpy.ops.object.join();body=bpy.context.object
bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
height=3.05;compact=args.name in ('ParkingMarshal','SnackChef','CoffeeVendor')
lo=Vector(tuple(min(v.co[i] for v in body.data.vertices) for i in range(3)))
hi=Vector(tuple(max(v.co[i] for v in body.data.vertices) for i in range(3)))
center=Vector(((lo.x+hi.x)/2,(lo.y+hi.y)/2,lo.z));scale=height/(hi.z-lo.z)
for v in body.data.vertices:v.co=(v.co-center)*scale
body.name=args.name+'_Body'
specs={'Root':((0,0,0),(0,0,.06),None),
       'Hips':((0,.025,.30),(0,.025,.37),'Root'),
       'Spine':((0,.025,.37),(0,.015,.45),'Hips'),
       'Chest':((0,.015,.45),(0,0,.52),'Spine'),
       'Neck':((0,0,.52),(0,0,.58),'Chest'),
       'Head':((0,0,.58),(0,0,.92),'Neck')}
for side,sign in [('L',1),('R',-1)]:
    shoulder=(sign*.15,0,.49);elbow=(sign*.23,0,.42)
    wrist=(sign*.29,-.015,.355);palm=(sign*.32,-.02,.29)
    hip=(sign*.095,.025,.30);knee=(sign*.11,.02,.20)
    ankle=(sign*.12,.025,.08);toe=(sign*.12,-.11,.035)
    specs.update({'UpperArm.'+side:(shoulder,elbow,'Chest'),
        'Forearm.'+side:(elbow,wrist,'UpperArm.'+side),'Hand.'+side:(wrist,palm,'Forearm.'+side),
        'Thigh.'+side:(hip,knee,'Hips'),'Shin.'+side:(knee,ankle,'Thigh.'+side),
        'Foot.'+side:(ankle,toe,'Shin.'+side)})
if not compact:
    # Initial fit for the longer-bodied new references; every generated body
    # still requires front/side fit and motion review before acceptance.
    z_targets={
        'Cashier':(.40,.47,.56,.65,.695,.63,.535,.46,.39,.245),
        'Cleaner':(.335,.405,.50,.585,.635,.58,.485,.405,.325,.205),
        'FuelAttendant':(.345,.42,.52,.60,.655,.60,.50,.42,.345,.21),
        'Traveler':(.35,.43,.53,.625,.675,.61,.505,.405,.335,.21),
        'Police':(.335,.405,.495,.58,.63,.575,.48,.40,.325,.20)}[args.name]
    hips,spine,chest,neck,head,shoulder_z,elbow_z,wrist_z,palm_z,knee_z=z_targets
    specs.update({'Hips':((0,.025,hips),(0,.025,spine),'Root'),
        'Spine':((0,.025,spine),(0,.015,chest),'Hips'),
        'Chest':((0,.015,chest),(0,0,neck),'Spine'),
        'Neck':((0,0,neck),(0,0,head),'Chest'),
        'Head':((0,0,head),(0,0,.95),'Neck')})
    for side,sign in [('L',1),('R',-1)]:
        shoulder=(sign*.10,0,shoulder_z);elbow=(sign*.17,0,elbow_z)
        wrist=(sign*.235,-.01,wrist_z);palm=(sign*.255,-.015,palm_z)
        hip=(sign*.078,.025,hips);knee=(sign*.087,.02,knee_z)
        ankle=(sign*.095,.025,.085);toe=(sign*.095,-.105,.035)
        specs.update({'UpperArm.'+side:(shoulder,elbow,'Chest'),
            'Forearm.'+side:(elbow,wrist,'UpperArm.'+side),'Hand.'+side:(wrist,palm,'Forearm.'+side),
            'Thigh.'+side:(hip,knee,'Hips'),'Shin.'+side:(knee,ankle,'Thigh.'+side),
            'Foot.'+side:(ankle,toe,'Shin.'+side)})
head_rigid_z=specs['Head'][0][2]+.01
armature=bpy.data.armatures.new(args.name+'_Skeleton');rig=bpy.data.objects.new('Rig',armature)
bpy.context.collection.objects.link(rig);bpy.context.view_layer.objects.active=rig
bpy.ops.object.select_all(action='DESELECT');rig.select_set(True);bpy.ops.object.mode_set(mode='EDIT')
for name,(a,b,parent) in specs.items():
    bone=armature.edit_bones.new(name);bone.head=Vector(a)*height;bone.tail=Vector(b)*height
    if parent:bone.parent=armature.edit_bones[parent]
    bone.use_deform=name!='Root'
bpy.ops.object.mode_set(mode='OBJECT')
proxy=body.copy();proxy.data=body.data.copy();bpy.context.collection.objects.link(proxy)
proxy.name='WEIGHT_PROXY';proxy.modifiers.clear();proxy.vertex_groups.clear()
bpy.ops.object.select_all(action='DESELECT');proxy.select_set(True);bpy.context.view_layer.objects.active=proxy
proxy.data.remesh_voxel_size=.025;bpy.ops.object.voxel_remesh()
bpy.ops.object.select_all(action='DESELECT');proxy.select_set(True);rig.select_set(True);bpy.context.view_layer.objects.active=rig
bpy.ops.object.parent_set(type='ARMATURE_AUTO')
assert all(v.groups for v in proxy.data.vertices),'Unweighted proxy vertices'
proxy.data.calc_loop_triangles();triangles=[tuple(t.vertices) for t in proxy.data.loop_triangles]
positions=[proxy.matrix_world@v.co for v in proxy.data.vertices]
tree=BVHTree.FromPolygons(positions,triangles,all_triangles=True)
groups={g.name:body.vertex_groups.new(name=g.name) for g in proxy.vertex_groups};cache={}
for v in body.data.vertices:
    position=body.matrix_world@v.co;key=tuple(round(x,5) for x in position)
    if key not in cache:
        point,normal,face,distance=tree.find_nearest(position);ids=triangles[face]
        bary=barycentric_transform(point,*(positions[i] for i in ids),Vector((1,0,0)),Vector((0,1,0)),Vector((0,0,1)))
        weights=defaultdict(float)
        for index,w in zip(ids,bary):
            for g in proxy.data.vertices[index].groups:weights[proxy.vertex_groups[g.group].name]+=max(0,w)*g.weight
        # Rigid shoes and head preserve the stylized hard forms. Fade only at cuffs.
        if position.z<height*.09:weights={'Foot.L' if position.x>0 else 'Foot.R':1}
        if position.z>height*head_rigid_z:weights={'Head':1}
        selected=sorted(weights,key=weights.get,reverse=True)[:4];total=sum(weights[n] for n in selected)
        assert total>0
        cache[key]={n:weights[n]/total for n in selected if weights[n]/total>.00001}
    for name,w in cache[key].items():groups[name].add([v.index],w,'REPLACE')
bpy.data.objects.remove(proxy,do_unlink=True)
body.parent=rig;body.modifiers.new('Deformation','ARMATURE').object=rig
def physical(p):return p
runpy.run_path(str(Path(__file__).with_name('reststop-rig-actions.py')),init_globals=globals(),run_name='__main__')
