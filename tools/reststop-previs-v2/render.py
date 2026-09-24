"""Native EEVEE batch render. No still-image stretching or generated video."""
import argparse,bpy,json,time,sys
from pathlib import Path
from mathutils import Vector
OUT=Path.cwd()/'outputs/reststop-blender-v2-2026-09-23'
p=argparse.ArgumentParser();p.add_argument('--mode',choices=['evidence','animation','overview'],default='evidence');p.add_argument('--start',type=int,default=1);p.add_argument('--end',type=int,default=7200);p.add_argument('--width',type=int,default=1280);p.add_argument('--samples',type=int,default=8);args=p.parse_args(sys.argv[sys.argv.index('--')+1:])
bpy.ops.wm.open_mainfile(filepath=str(OUT/'reststop-v2.blend'));s=bpy.context.scene;s.render.engine='BLENDER_EEVEE';s.eevee.taa_render_samples=args.samples;s.render.resolution_x=args.width;s.render.resolution_y=round(args.width*9/16);s.render.resolution_percentage=100;s.render.threads_mode='FIXED';s.render.threads=4
if args.mode=='evidence':
 folder=OUT/'evidence';folder.mkdir(exist_ok=True);s.render.image_settings.file_format='PNG'
 times=[8,19,31,35,43,56,65,77,91,103,115,134,153,169,179,194,206,216,223,231,242,248,258,268,284,295]
 for t in times:
  s.frame_set(round(t*24)+1);s.render.filepath=str(folder/f'view-{t:03}.png');bpy.ops.render.render(write_still=True)
 print('EVIDENCE_COMPLETE',flush=True)
elif args.mode=='overview':
 s.frame_set(1);cam=s.camera;cam.animation_data_clear();cam.data.animation_data_clear();cam.data.shift_y=0;cam.data.type='ORTHO';cam.data.ortho_scale=800;cam.data.clip_end=3000;cam.location=(820,-870,740);target=Vector((65,-40,0));cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler();s.render.resolution_x=1800;s.render.resolution_y=1400;s.render.image_settings.file_format='PNG';s.render.filepath=str(OUT/'whole-map.png');bpy.ops.render.render(write_still=True)
else:
 folder=OUT/'frames-final';folder.mkdir(exist_ok=True);s.render.image_settings.file_format='JPEG';s.render.image_settings.quality=88;s.render.filepath=str(folder/'frame-#####');s.frame_start=args.start;s.frame_end=args.end;s.frame_step=1
 started=time.time();count=0
 def written(*unused):
  global count
  count+=1
  if count%24==0:
   r={'frame':s.frame_current,'end':args.end,'count':count,'elapsed':time.time()-started,'mean_seconds_per_frame':(time.time()-started)/count};(OUT/'render-progress.json').write_text(json.dumps(r,indent=2));print('PROGRESS',json.dumps(r),flush=True)
 bpy.app.handlers.render_write.append(written);bpy.ops.render.render(animation=True)
 assert count==args.end-args.start+1
 (OUT/'render-complete.json').write_text(json.dumps({'frames':count,'first':args.start,'last':args.end,'elapsed':time.time()-started,'fps':s.render.fps,'width':args.width,'height':s.render.resolution_y},indent=2))
