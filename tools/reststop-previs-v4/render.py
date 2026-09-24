import bpy,json,sys,argparse
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parent));from timeline import STAGES
OUT=Path.cwd()/'outputs/reststop-blender-v4-2026-09-23';folder=OUT/'images';folder.mkdir(exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(OUT/'reststop-v4.blend'));s=bpy.context.scene;s.render.engine='BLENDER_EEVEE';s.eevee.taa_render_samples=24;s.render.resolution_x=1600;s.render.resolution_y=900;s.render.resolution_percentage=100;s.render.image_settings.file_format='PNG';s.render.threads_mode='FIXED';s.render.threads=4
parser=argparse.ArgumentParser();parser.add_argument('--steps',default='');args=parser.parse_args(sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else []);selected=args.steps.split(',') if args.steps else []
records=[]
for (idx,label,a,b),t in zip(STAGES,[19,31,56,75.5,134,179,207,223,258,288]):
 if not selected or idx in selected:
  s.frame_set(round(t*24)+1);s.render.filepath=str(folder/f'{idx}-reststop.png');bpy.ops.render.render(write_still=True)
 records.append({'step':idx,'title':label,'seconds':t,'frame':round(t*24)+1,'file':f'{idx}-reststop.png','dimensions':[1600,900]})
if not selected:
 for idx,t in [('02-alt',26),('04-alt',82),('07-alt',194),('08-alt',237),('09-alt',266)]:
  s.frame_set(round(t*24)+1);s.render.filepath=str(folder/f'{idx}.png');bpy.ops.render.render(write_still=True)
(OUT/'images.json').write_text(json.dumps(records,ensure_ascii=False,indent=2),encoding='utf8');print('TEN_IMAGES_RENDERED',flush=True)
