"""Encode ten exact native timeline intervals and their 300s concatenation."""
import argparse, hashlib, json, re, subprocess, sys, time
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parent))
from timeline import FPS, STAGES

ROOT=Path.cwd();OUT=ROOT/'outputs/reststop-blender-v2-2026-09-23';VIDEOS=OUT/'videos';VIDEOS.mkdir(exist_ok=True)
LABELS=OUT/'labels';LABELS.mkdir(exist_ok=True)
FFMPEG=Path.home()/'AppData/Roaming/Python/Python312/site-packages/imageio_ffmpeg/binaries/ffmpeg-win-x86_64-v7.1.exe'
NOTES=[
    '게임 이동 속도 7.8 기준 · 진입 차단기 통과',
    '혼잡한 주차장 · 후진 경고등을 보고 옆으로 회피',
    '간식 매점과 휴식 공간 · 방문객 대피 · 이동 카트',
    '자동문 개방 · 넓어진 중앙 홀로 진입',
    '쿼터뷰 · 위치 고정 · 연속 회전 최대 90도/초 · 분사구로 적 지연',
    '긴 식당 동선 · 적 접근 · 배식 카트 회피',
    '바닥 면적 7.7배 · 카트 행렬 · 쓰러지는 진열대와 굴러가는 캔',
    '바닥 면적 10.1배 · 넓은 통로 · 물 분사 · 청소 로봇',
    '주유기 사이 이동 · 경고 후 버스 교차',
    '출구 교통 흐름 · 마지막 적 · 차단기 통과',
]

def run(args):
    proc=subprocess.run([str(FFMPEG),'-hide_banner',*args],cwd=ROOT,stdout=subprocess.PIPE,stderr=subprocess.PIPE)
    if proc.returncode:raise RuntimeError(proc.stderr.decode('utf8','replace')[-6000:])
    return proc

def verify(path,expected_frames,duration):
    md5=path.with_suffix('.framemd5')
    run(['-v','error','-i',str(path),'-map','0:v:0','-f','framemd5','-y',str(md5)])
    lines=[l for l in md5.read_text().splitlines() if l and not l.startswith('#')]
    assert len(lines)==expected_frames,(path,len(lines),expected_frames)
    probe=subprocess.run([str(FFMPEG),'-hide_banner','-i',str(path)],capture_output=True,text=True,encoding='utf8',errors='replace')
    m=re.search(r'Duration: (\d+):(\d+):(\d+\.\d+)',probe.stderr);assert m,probe.stderr
    measured=int(m[1])*3600+int(m[2])*60+float(m[3]);assert abs(measured-duration)<.03,(measured,duration)
    return {'file':path.name,'duration_seconds':measured,'frames':len(lines),'fps':FPS,'bytes':path.stat().st_size,'sha256':hashlib.sha256(path.read_bytes()).hexdigest(),'decoded_without_errors':True}

parser=argparse.ArgumentParser();parser.add_argument('--watch',action='store_true');parser.add_argument('--force-stages',default='');args=parser.parse_args();force_stages=set(args.force_stages.split(','))
receipts=[]
for i,(idx,label,a,b) in enumerate(STAGES):
    first=round(a*FPS)+1;last=round(b*FPS);path=VIDEOS/f'{idx}-reststop.mp4';receipt=path.with_suffix('.json')
    if receipt.exists() and idx not in force_stages:receipts.append(json.loads(receipt.read_text(encoding='utf8')));continue
    while not ((OUT/'frames-final'/f'frame-{last:05d}.jpg').exists() and ((OUT/'frames-final'/f'frame-{last+1:05d}.jpg').exists() or (last==7200 and (OUT/'render-complete.json').exists()))):
        if not args.watch:raise RuntimeError('Frame interval not finished: '+idx)
        time.sleep(4)
    title=LABELS/f'{idx}-title.txt';note=LABELS/f'{idx}-note.txt'
    title.write_text(f'{idx}  {label}   |   {a}–{b}초',encoding='utf8');note.write_text(NOTES[i],encoding='utf8')
    font="fontfile='C\\:/Windows/Fonts/malgunbd.ttf'"
    vf=["drawbox=x=0:y=0:w=iw:h=72:color=0x12251d@0.84:t=fill",f"drawtext={font}:textfile='{title.relative_to(ROOT).as_posix()}':fontsize=29:fontcolor=white:x=24:y=21",f"drawtext={font}:text='%{{eif\\:floor(t)+{a}\\:d}} / 300s':fontsize=24:fontcolor=white:x=w-tw-24:y=24","drawbox=x=0:y=ih-49:w=iw:h=49:color=0x12251d@0.77:t=fill",f"drawtext={font}:textfile='{note.relative_to(ROOT).as_posix()}':fontsize=23:fontcolor=white:x=24:y=h-37"]
    if idx=='05':vf.append(f"drawtext={font}:text='DEFENSE  %{{eif\\:max(0,ceil(60-t))\\:d}}s':fontsize=26:fontcolor=0x9aedff:x=w-tw-26:y=95:box=1:boxcolor=0x12251d@0.8:boxborderw=10")
    run(['-v','warning','-framerate',str(FPS),'-start_number',str(first),'-i',str(OUT/'frames-final/frame-%05d.jpg'),'-frames:v',str(last-first+1),'-vf',','.join(vf),'-an','-c:v','libx264','-threads','2','-preset','fast','-crf','20','-pix_fmt','yuv420p','-movflags','+faststart','-y',str(path)])
    result=verify(path,last-first+1,b-a);result.update(stage=idx,title=label,start_seconds=a,end_seconds=b,notes=NOTES[i]);receipt.write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf8');receipts.append(result)
    (OUT/'video-progress.json').write_text(json.dumps(receipts,ensure_ascii=False,indent=2),encoding='utf8')
    print('CLIP_COMPLETE',idx,json.dumps(result,ensure_ascii=True),flush=True)
    run(['-v','error','-ss',str((b-a)*.42),'-i',str(path),'-frames:v','1','-y',str(VIDEOS/f'{idx}-poster.jpg')])

concat=VIDEOS/'concat.txt';concat.write_text(''.join("file '"+r['file']+"'\n" for r in receipts),encoding='utf8')
full=VIDEOS/'reststop-300s.mp4'
run(['-v','warning','-f','concat','-safe','0','-i',str(concat),'-c','copy','-movflags','+faststart','-y',str(full)])
full_receipt=verify(full,7200,300)
manifest={'clips':receipts,'full':full_receipt,'rendering':'Every source frame is rendered in Blender EEVEE from the same native timeline. No generative video or frame interpolation.','audio':'silent previs'}
(OUT/'video-manifest.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2),encoding='utf8');print('ALL_VIDEOS_COMPLETE',json.dumps(full_receipt),flush=True)
