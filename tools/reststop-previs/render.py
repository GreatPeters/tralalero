"""Render actual frames from the one saved Blender timeline, with resumable output."""
import argparse, bpy, json, sys, time
from pathlib import Path
ROOT=Path.cwd();OUT=ROOT/'outputs/reststop-blender-300s-2026-09-23'
parser=argparse.ArgumentParser();parser.add_argument('--mode',choices=['evidence','animation','patch','native-patch','benchmark','overview'],default='evidence');parser.add_argument('--engine',default='BLENDER_EEVEE');parser.add_argument('--width',type=int,default=960);parser.add_argument('--samples',type=int,default=8);parser.add_argument('--start',type=int,default=1);parser.add_argument('--end',type=int,default=7200);parser.add_argument('--step',type=int,default=1)
parser.add_argument('--patch-file',default='route-turn-patch.json')
args=parser.parse_args(sys.argv[sys.argv.index('--')+1:])
bpy.ops.wm.open_mainfile(filepath=str(OUT/'reststop-master.blend'));s=bpy.context.scene
s.render.engine=args.engine;s.render.resolution_x=args.width;s.render.resolution_y=round(args.width*9/16);s.render.resolution_percentage=100;s.render.threads_mode='FIXED';s.render.threads=4
if args.engine=='BLENDER_EEVEE':s.eevee.taa_render_samples=args.samples
if args.engine=='BLENDER_WORKBENCH':
    s.display.shading.light='STUDIO';s.display.shading.studiolight_rotate_z=.5;s.display.shading.color_type='MATERIAL';s.display.shading.show_shadows=True;s.display.shading.show_cavity=True;s.display.shading.cavity_type='BOTH';s.display.shading.curvature_ridge_factor=1.4;s.display.shading.curvature_valley_factor=1.1;s.display.shading.show_specular_highlight=True;s.display.shading.background_type='WORLD';s.world.color=(.60,.72,.82);s.display.render_aa='8'
if args.mode in ('evidence','benchmark'):
    times=[12,38,62,90,112,140,177,207,222,231,236,247,266,289]
    if args.mode=='benchmark':times=[12,12.05,12.1,112,112.05,112.1,207,207.05,207.1]
    folder=OUT/('evidence' if args.mode=='evidence' else 'benchmark');folder.mkdir(exist_ok=True)
    s.render.image_settings.file_format='PNG';results=[]
    for t in times:
        f=round(t*24)+1;s.frame_set(f);s.render.filepath=str(folder/f'{args.engine}-{t:07.2f}.png');tic=time.time();bpy.ops.render.render(write_still=True);results.append({'time':t,'frame':f,'seconds':time.time()-tic,'file':s.render.filepath});print('FRAME',json.dumps(results[-1]),flush=True)
    (folder/(args.engine+'-timings.json')).write_text(json.dumps(results,indent=2),encoding='utf8')
elif args.mode=='overview':
    from mathutils import Vector
    s.frame_set(1);cam=s.camera;cam.animation_data_clear();cam.location=(175,-195,176);target=Vector((13,-18,0));cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.type='ORTHO';cam.data.ortho_scale=240
    s.render.image_settings.file_format='PNG';s.render.filepath=str(OUT/'overview.png');bpy.ops.render.render(write_still=True)
elif args.mode=='native-patch':
    frames=json.loads((OUT/args.patch_file).read_text())['frames'];assert frames==list(range(frames[0],frames[-1]+1))
    folder=OUT/'frames';folder.mkdir(exist_ok=True);s.render.image_settings.file_format='JPEG';s.render.image_settings.quality=88;s.render.filepath=str(folder/'frame-#####');s.frame_start=frames[0];s.frame_end=frames[-1];s.frame_step=1
    started=time.time();written=[]
    def on_write(*unused):
        written.append(time.time())
        if len(written)%24==0:
            intervals=[b-a for a,b in zip(written[-25:],written[-24:])] if len(written)>24 else []
            progress={'mode':'native-patch','frame':s.frame_current,'end':frames[-1],'rendered':len(written),'elapsed':time.time()-started,'width':args.width,'engine':args.engine,'step':1}
            (OUT/'render-progress.json').write_text(json.dumps(progress,indent=2));print('PROGRESS',json.dumps(progress),flush=True)
    bpy.app.handlers.render_write.append(on_write);bpy.ops.render.render(animation=True)
    assert len(written)==len(frames),(len(written),len(frames))
    (OUT/'patch-render-completed.json').write_text(json.dumps({'start':frames[0],'end':frames[-1],'frames_rendered':len(written),'elapsed':time.time()-started,'width':args.width,'engine':args.engine,'renderer_call':'animation=True'},indent=2))
else:
    folder=OUT/'frames';folder.mkdir(exist_ok=True);s.render.image_settings.file_format='JPEG';s.render.image_settings.quality=88
    times=[];total_start=time.time()
    frames=json.loads((OUT/args.patch_file).read_text())['frames'] if args.mode=='patch' else range(args.start,args.end+1,args.step)
    for f in frames:
        dest=folder/f'frame-{f:05d}.jpg'
        if args.mode!='patch' and dest.exists() and dest.stat().st_size>5000:continue
        tic=time.time();s.frame_set(f);s.render.filepath=str(dest);bpy.ops.render.render(write_still=True);times.append(time.time()-tic)
        if len(times)%24==0:
            progress={'frame':f,'end':args.end,'rendered':len(times),'elapsed':time.time()-total_start,'last_24_mean':sum(times[-24:])/24,'width':args.width,'engine':args.engine,'step':args.step};(OUT/'render-progress.json').write_text(json.dumps(progress,indent=2));print('PROGRESS',json.dumps(progress),flush=True)
    (OUT/('patch-render-completed.json' if args.mode=='patch' else 'render-completed.json')).write_text(json.dumps({'start':args.start,'end':args.end,'step':args.step,'frames_rendered':len(times),'elapsed':time.time()-total_start,'width':args.width,'engine':args.engine},indent=2))
