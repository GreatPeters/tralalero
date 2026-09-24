"""Fixed scripted camera offset around the entrance canopy, independent of input."""
import bpy,sys
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parent));from timeline import smooth
out=Path.cwd()/'outputs/reststop-blender-v3-2026-09-23';bpy.ops.wm.open_mainfile(filepath=str(out/'reststop-v3.blend'));s=bpy.context.scene;cam=s.camera
assert not s.get('entrance_camera_adjusted')
curves=[fc for layer in cam.animation_data.action.layers for strip in layer.strips for bag in strip.channelbags for fc in bag.fcurves];fc=next(fc for fc in curves if fc.data_path=='location' and fc.array_index==0);keys=[(f,fc.evaluate(f)) for f in range(1777,1898,6)]
for f,x in keys:
 t=(f-1)/24;cam.location.x=x+3.2*min(smooth(t-74),1-smooth(t-78));cam.keyframe_insert(data_path='location',index=0,frame=f)
for k in fc.keyframe_points:k.interpolation='LINEAR'
s['entrance_camera_adjusted']=True;s.frame_set(1);bpy.ops.wm.save_as_mainfile(filepath=str(out/'reststop-v3.blend'),compress=True)
