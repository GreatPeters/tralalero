"""Sequential task-owned render completion; never starts multiple GPU jobs."""
from pathlib import Path
import json, os, shutil, subprocess, sys, time
ROOT=Path.cwd();OUT=ROOT/'outputs/reststop-blender-300s-2026-09-23'
BLENDER=Path.home()/'AppData/Local/Programs/Blender Foundation/Blender 5.2/blender.exe'
def state(stage,**extra):
    record={'stage':stage,'time':time.time(),**extra};(OUT/'finish-state.json').write_text(json.dumps(record,indent=2));print(json.dumps(record),flush=True)
def run(script,args=(),blender=False,log='finish-operation.log'):
    if blender:
        cmd=[str(BLENDER),'-b','--python-exit-code','1','-t','4','--python','tools/run-limited-generation.py','--','--script',script,'--record',str(OUT/(Path(script).stem+'-finish-resource.json')),'--threads','4','--',*args]
    else:cmd=[sys.executable,script,*args]
    with (OUT/log).open('w',encoding='utf8') as f:
        result=subprocess.run(cmd,cwd=ROOT,stdout=f,stderr=subprocess.STDOUT,creationflags=subprocess.CREATE_NO_WINDOW if os.name=='nt' else 0)
    if result.returncode:raise RuntimeError(f'{script} failed: {result.returncode}; see {log}')
try:
    state('waiting_for_patch_render')
    while not (OUT/'patch-render-completed.json').exists():time.sleep(3)
    state('rendering_remaining_frames')
    run('tools/reststop-previs/render.py',['--mode','animation','--width','1280','--samples','8','--start','1','--end','7200'],True,'resume-animation.log')
    state('waiting_for_initial_encoder')
    deadline=time.time()+240
    while not (OUT/'video-manifest.json').exists():
        if time.time()>deadline:raise RuntimeError('Encoder did not finish after render; inspect encode.log')
        time.sleep(3)
    state('encoding_final_revisions')
    archive=OUT/'review-before-turn-fix/clips';archive.mkdir(parents=True,exist_ok=True)
    for idx in ('01','02','03','04'):
        for suffix in ('.mp4','.json'):
            p=OUT/'videos'/f'{idx}-reststop{suffix}'
            if p.exists() and not (archive/p.name).exists():shutil.copy2(p,archive/p.name)
    run('tools/reststop-previs/encode.py',['--force-stages','01,02,03,04'],False,'encode-final.log')
    state('validating_and_exporting')
    run('tools/reststop-previs/render.py',['--mode','evidence','--width','1280','--samples','16'],True,'evidence-final.log')
    run('tools/reststop-previs/validate.py',(),True,'validation-final.log')
    run('tools/reststop-previs/verify_export.py',(),True,'reimport-final.log')
    run('tools/reststop-previs/asset_evidence.py',(),True,'asset-evidence.log')
    state('extracting_video_review_frames')
    run('tools/reststop-previs/review_videos.py',(),False,'review-final.log')
    run('tools/reststop-previs/gallery.py',(),False,'gallery-final.log')
    state('ready_for_visual_review')
except Exception as exc:
    state('failed',error=str(exc));raise
