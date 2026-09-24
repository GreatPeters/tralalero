"""Use a steeper interior camera so the restroom's front wall cannot hide play."""
import bpy,json,sys,shutil
from pathlib import Path
from mathutils import Vector
sys.path.insert(0,str(Path(__file__).resolve().parent))
from timeline import route,smooth
OUT=Path.cwd()/'outputs/reststop-blender-300s-2026-09-23';source=OUT/'reststop-master.blend'
backup=OUT/'reststop-master-before-restroom-camera.blend'
if not backup.exists():shutil.copy2(source,backup)
bpy.ops.wm.open_mainfile(filepath=str(source));s=bpy.context.scene;cam=s.camera
if s.get('restroom_camera_refined'):raise RuntimeError('Camera correction already applied; preserve the completed revision')
for i in range(153):
    t=215+i/4;s.frame_set(round(t*24)+1);w=min(smooth((t-215)/3),1-smooth((t-250)/3))
    pos=cam.location.copy()+Vector((-4,16,4))*w;x,y=route(t);target=Vector((x,y+4,1.2))
    cam.location=pos;cam.rotation_euler=(target-pos).to_track_quat('-Z','Y').to_euler()
    cam.keyframe_insert(data_path='location',frame=round(t*24)+1);cam.keyframe_insert(data_path='rotation_euler',frame=round(t*24)+1)
for layer in cam.animation_data.action.layers:
    for strip in layer.strips:
        for bag in strip.channelbags:
            for fc in bag.fcurves:
                for k in fc.keyframe_points:k.interpolation='LINEAR'
s['restroom_camera_refined']=True;s.frame_set(1)
bpy.ops.wm.save_as_mainfile(filepath=str(source),compress=True)
report={'frames':list(range(5162,6073)),'count':911,'stages':['07','08','09'],'reason':'Steeper camera clears the tall front wall, with three-second transitions. Geometry and player/encounter timelines unchanged.'}
(OUT/'restroom-camera-patch.json').write_text(json.dumps(report,indent=2));print('RESTROOM_CAMERA_PATCH',911,flush=True)
