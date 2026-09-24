import bpy,math,sys,json
from pathlib import Path
from mathutils import Vector
sys.path.insert(0,str(Path(__file__).resolve().parent))
from timeline import center_route
out=Path.cwd()/'outputs/reststop-blender-v2-2026-09-23'
bpy.ops.wm.open_mainfile(filepath=str(out/'reststop-v2.blend'));s=bpy.context.scene
for n in range(2):
 r=bpy.data.objects['Exit_convoy_%d'%n];r.animation_data_clear();last=None
 for i in range(201):
  t=275+i*.125;ct=max(275,min(300,t+(2.2 if n==0 else -2.2)));q=Vector((*center_route(ct),.08));a=center_route(max(275,ct-.1));b=center_route(min(300,ct+.1));f=Vector((b[0]-a[0],b[1]-a[1],0)).normalized();right=Vector((f.y,-f.x,0));yaw=math.atan2(f.x,-f.y)
  if last is not None:yaw=last+(yaw-last+math.pi)%(math.tau)-math.pi
  last=yaw;r.location=q+right*4.4;r.rotation_euler.z=yaw;r.keyframe_insert(data_path='location',frame=round(t*24)+1);r.keyframe_insert(data_path='rotation_euler',frame=round(t*24)+1)
 for layer in r.animation_data.action.layers:
  for strip in layer.strips:
   for bag in strip.channelbags:
    for fc in bag.fcurves:
     for k in fc.keyframe_points:k.interpolation='LINEAR'
s.frame_set(1);bpy.ops.wm.save_as_mainfile(filepath=str(out/'reststop-v2.blend'),compress=True)
print('EXIT_REPAIRED',flush=True)
