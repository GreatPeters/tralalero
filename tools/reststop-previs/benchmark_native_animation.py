import bpy,json,time
from pathlib import Path
OUT=Path.cwd()/'outputs/reststop-blender-300s-2026-09-23';folder=OUT/'native-animation-benchmark';folder.mkdir(exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(OUT/'reststop-master.blend'));s=bpy.context.scene
s.render.engine='BLENDER_EEVEE';s.eevee.taa_render_samples=8;s.render.resolution_x=1280;s.render.resolution_y=720;s.render.resolution_percentage=100;s.render.image_settings.file_format='JPEG';s.render.image_settings.quality=88;s.render.filepath=str(folder/'frame-#####');s.frame_start=5500;s.frame_end=5511;s.frame_step=1;s.render.threads_mode='FIXED';s.render.threads=4
times=[];started=time.time()
def written(*args):times.append(time.time())
bpy.app.handlers.render_write.append(written);bpy.ops.render.render(animation=True)
report={'frames':len(times),'elapsed':time.time()-started,'steady_seconds_per_frame':sum(b-a for a,b in zip(times,times[1:]))/max(1,len(times)-1),'filenames':[p.name for p in sorted(folder.glob('*.jpg'))]}
(OUT/'native-animation-benchmark.json').write_text(json.dumps(report,indent=2));print('NATIVE_ANIMATION_BENCHMARK',json.dumps(report),flush=True)
