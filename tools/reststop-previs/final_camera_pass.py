"""Finish the evidence-driven restroom camera correction after the base render."""
from pathlib import Path
import argparse,json,os,shutil,subprocess,sys,time
ROOT=Path.cwd();OUT=ROOT/'outputs/reststop-blender-300s-2026-09-23'
BLENDER=Path.home()/'AppData/Local/Programs/Blender Foundation/Blender 5.2/blender.exe'
parser=argparse.ArgumentParser();parser.add_argument('--patch-file',default='restroom-camera-patch.json');parser.add_argument('--finalize-assets',action='store_true');parser.add_argument('--native',action='store_true');parser.add_argument('--skip-evidence',action='store_true');args=parser.parse_args()
def state(stage,**kwargs):
    r={'stage':stage,'time':time.time(),**kwargs};(OUT/'camera-pass-state.json').write_text(json.dumps(r,indent=2));print(json.dumps(r),flush=True)
def run(script,args=(),blender=False,log='camera-pass-operation.log'):
    cmd=([str(BLENDER),'-b','--python-exit-code','1','-t','4','--python','tools/run-limited-generation.py','--','--script',script,'--record',str(OUT/(Path(script).stem+'-camera-pass-resource.json')),'--threads','4','--'] if blender else [sys.executable])+([script] if not blender else [])+list(args)
    with (OUT/log).open('w',encoding='utf8') as f:
        result=subprocess.run(cmd,cwd=ROOT,stdout=f,stderr=subprocess.STDOUT,creationflags=subprocess.CREATE_NO_WINDOW if os.name=='nt' else 0)
    if result.returncode:raise RuntimeError(f'{script} failed ({result.returncode}), see {log}')
try:
    state('waiting_for_base_pipeline')
    while True:
        status=json.loads((OUT/'finish-state.json').read_text())
        if status['stage']=='failed':raise RuntimeError('Base pipeline failed: '+status.get('error',''))
        if status['stage']=='ready_for_visual_review':break
        time.sleep(3)
    if not args.skip_evidence:
        state('rendering_final_evidence')
        run('tools/reststop-previs/render.py',['--mode','evidence','--width','1280','--samples','16'],True,'evidence-final-camera.log')
    state('rendering_camera_correction',frames=len(json.loads((OUT/args.patch_file).read_text())['frames']))
    old=OUT/'patch-render-completed.json'
    if old.exists() and not (OUT/'route-patch-render-completed.json').exists():shutil.copy2(old,OUT/'route-patch-render-completed.json')
    run('tools/reststop-previs/render.py',['--mode','native-patch' if args.native else 'patch','--patch-file',args.patch_file,'--width','1280','--samples','8'],True,'camera-patch-render.log')
    state('encoding_camera_correction')
    archive=OUT/'review-before-restroom-camera';archive.mkdir(exist_ok=True)
    for idx in ('07','08','09'):
        for suffix in ('.mp4','.json'):
            source=OUT/'videos'/f'{idx}-reststop{suffix}'
            if not (archive/source.name).exists():shutil.copy2(source,archive/source.name)
        source=OUT/'review-final'/f'{idx}-review.jpg'
        if source.exists() and not (archive/source.name).exists():shutil.copy2(source,archive/source.name)
    run('tools/reststop-previs/encode.py',['--force-stages','07,08,09'],False,'encode-final-camera.log')
    run('tools/reststop-previs/review_videos.py',['--stages','07,08,09'],False,'review-final-camera.log')
    if args.finalize_assets:
        state('finalizing_model_exports')
        run('tools/reststop-previs/validate.py',(),True,'validation-final.log')
        run('tools/reststop-previs/verify_export.py',(),True,'reimport-final.log')
        run('tools/reststop-previs/asset_evidence.py',(),True,'asset-evidence.log')
    run('tools/reststop-previs/gallery.py',(),False,'gallery-final-camera.log')
    state('ready_for_final_review')
except Exception as exc:
    state('failed',error=str(exc));raise
