"""Preserve approved bodies and skinning; replace backward attacks with grounded forward gestures."""
import argparse
import json
import math
from pathlib import Path
import sys
import bpy
from mathutils import Quaternion, Vector

parser=argparse.ArgumentParser();parser.add_argument('--role',required=True);parser.add_argument('--revision',default='v2');args=parser.parse_args(sys.argv[sys.argv.index('--')+1:])
role=args.role
if role not in ['ParkingMarshal','CoffeeVendor','SnackChef']:raise ValueError(role)
root=Path.cwd();source=root/f'outputs/approved-road-concepts-2026-09-13/humans/{role}/{role}.blend'
out=root/f'outputs/campaign-balance-2026-09-23/reststop-motion-{args.revision}/{role}';out.mkdir(parents=True,exist_ok=True)
if (out/'motion-report.json').exists():raise FileExistsError('Preserve completed revision')
bpy.ops.wm.open_mainfile(filepath=str(source))
rig=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE')
meshes=[o for o in bpy.context.scene.objects if o.type=='MESH']
body_meshes=[o for o in meshes if not o.name.endswith('_Equipment')]
for obj in meshes:
    if obj not in body_meshes:obj.hide_render=True
original_triangles=sum(len(p.vertices)-2 for o in meshes for p in o.data.polygons)
scene=bpy.context.scene;scene.render.fps=30
if scene.world is None:scene.world=bpy.data.worlds.new('Inspection world')
old=next(a for a in bpy.data.actions if a.name.endswith('attack_once'))
rig.animation_data.action=old
if old.slots:rig.animation_data.action_slot=old.slots[0]
scene.frame_set(20);bpy.ops.file.pack_all();bpy.ops.wm.save_as_mainfile(filepath=str(out/'before-attack.blend'))
rig.animation_data_clear()
for action in list(bpy.data.actions):bpy.data.actions.remove(action)
rig.animation_data_create()

def rotate(name,x=0,y=0,z=0):
    bone=rig.pose.bones[name];rest=bone.bone.matrix_local.to_quaternion()
    world=Quaternion((0,0,1),math.radians(z))@Quaternion((0,1,0),math.radians(y))@Quaternion((1,0,0),math.radians(x))
    bone.rotation_quaternion=rest.inverted()@world@rest

def move(name,world):
    bone=rig.pose.bones[name];bone.location=bone.bone.matrix_local.to_3x3().inverted()@Vector(world)

def smooth(t):
    t=max(0,min(1,t));return t*t*(3-2*t)

def pulse(t,start,peak,end):
    return smooth((t-start)/(peak-start)) if t<peak else 1-smooth((t-peak)/(end-peak))

durations={'idle':2.2,'walk':1,'run':.75,'attack_loop':1.2,'attack_once':1.2,'hit':.3,'die':1.05}
actions=[];ground=[];hand=[]
for name,duration in durations.items():
    frames=round(duration*30);action=bpy.data.actions.new(name);action.use_fake_user=True;rig.animation_data.action=action
    scene.frame_start=1;scene.frame_end=frames+1
    for frame in range(1,frames+2):
        scene.frame_set(frame);t=(frame-1)/frames;wave=math.sin(t*math.tau)
        for bone in rig.pose.bones:
            bone.rotation_mode='QUATERNION';bone.rotation_quaternion=Quaternion();bone.location=(0,0,0);bone.scale=(1,1,1)
        rotate('Chest',x=wave*1.5);rotate('Head',y=wave*2,z=wave*.8)
        for side,sign in [('L',1),('R',-1)]:
            rotate('UpperArm.'+side,x=-8+wave*2*sign,y=sign*7);rotate('Forearm.'+side,x=-12)
        if name in ('walk','run'):
            amplitude=21 if name=='walk' else 30
            for side,sign in [('L',1),('R',-1)]:
                phase=wave*sign;rotate('Thigh.'+side,x=phase*amplitude);rotate('Shin.'+side,x=-max(0,phase)*30)
                rotate('Foot.'+side,x=-phase*6);rotate('UpperArm.'+side,x=-8-phase*11,y=sign*7)
            move('Hips',(0,0,abs(wave)*.018))
        elif name.startswith('attack'):
            wind=pulse(t,0,.23,.5);strike=pulse(t,.25,.5,.88)
            rotate('Chest',x=-5*wind+10*strike,z=6*wind-8*strike)
            rotate('Head',x=2*wind-3*strike)
            rotate('UpperArm.R',x=-8+20*wind-57*strike,y=-8,z=-4*strike)
            rotate('Forearm.R',x=-12-16*wind-9*strike)
            rotate('UpperArm.L',x=-8-17*wind+6*strike,y=7)
            if role=='SnackChef':rotate('Chest',x=9*strike,z=14*wind-18*strike)
            if role=='ParkingMarshal':rotate('UpperArm.R',x=-8-25*wind-40*strike,y=-8)
            move('Hips',(0,-.04*strike,0))
        elif name=='hit':
            hit=pulse(t,0,.25,1);rotate('Chest',x=-12*hit,z=4*hit);rotate('Head',x=-8*hit)
        elif name=='die':
            fall=smooth(t/.78);rotate('Root',x=-83*fall,z=-6*fall);rotate('Chest',x=-8*fall)
            rotate('UpperArm.R',x=-28*fall,y=-15);rotate('UpperArm.L',x=-32*fall,y=15)
        bpy.context.view_layer.update();dg=bpy.context.evaluated_depsgraph_get()
        low=float('inf')
        for obj in meshes:
            evaluated=obj.evaluated_get(dg);mesh=evaluated.to_mesh()
            low=min(low,min((evaluated.matrix_world@v.co).z for v in mesh.vertices));evaluated.to_mesh_clear()
        root_bone=rig.pose.bones['Root'];root_bone.location+=root_bone.bone.matrix_local.to_3x3().inverted()@Vector((0,0,-low))
        bpy.context.view_layer.update()
        for bone in rig.pose.bones:
            bone.keyframe_insert('rotation_quaternion',frame=frame);bone.keyframe_insert('location',frame=frame);bone.keyframe_insert('scale',frame=frame)
        ground.append({'action':name,'frame':frame,'correction':-low})
        if name=='attack_once':
            position=rig.matrix_world@rig.pose.bones['Hand.R'].head
            hand.append({'seconds':(frame-1)/30,'position':list(position)})
    actions.append(action)

rig.animation_data.action=next(a for a in actions if a.name=='attack_once')
if rig.animation_data.action.slots:rig.animation_data.action_slot=rig.animation_data.action.slots[0]
scene.frame_start=1;scene.frame_end=37;scene.frame_set(19)
bpy.ops.object.select_all(action='DESELECT')
for obj in [rig,*meshes]:obj.select_set(True)
bpy.context.view_layer.objects.active=rig
bpy.ops.wm.save_as_mainfile(filepath=str(out/(role+'.blend')))
bpy.ops.export_scene.fbx(filepath=str(out/(role+'.fbx')),use_selection=True,add_leaf_bones=False,bake_anim=True,bake_anim_use_all_actions=True,bake_anim_use_nla_strips=False,bake_anim_simplify_factor=0,path_mode='COPY',embed_textures=True)
for obj in meshes:
    if obj not in body_meshes:obj.select_set(False)
bpy.ops.export_scene.gltf(filepath=str(out/(role+'.glb')),export_format='GLB',use_selection=True,export_animations=True,export_animation_mode='ACTIONS')
assert sum(len(p.vertices)-2 for o in meshes for p in o.data.polygons)==original_triangles
report={'role':role,'source':str(source),'blender':bpy.app.version_string,'triangles':original_triangles,'bodyTriangles':sum(len(p.vertices)-2 for o in body_meshes for p in o.data.polygons),'bones':len(rig.data.bones),'actions':durations,'releaseSeconds':.6,'geometryPreserved':True,'legacyEquipment':'retained hidden in FBX for stable imported node IDs; omitted from body-only GLB; replace with native fitted tools','handTrajectory':hand,'groundCorrection':ground}
(out/'motion-report.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print(role,'forward strike',min(hand,key=lambda p:p['position'][1]),flush=True)
