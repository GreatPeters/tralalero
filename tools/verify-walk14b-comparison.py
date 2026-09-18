"""Decode delivered clips, check the preserved source and retain comparison frames."""
import csv
import hashlib
import json
from pathlib import Path
import re
import subprocess

ROOT=Path(__file__).resolve().parent.parent
OUT=ROOT/'map-concepts/opening-walk-14b-2026-09-12'
PREVIEW=ROOT/'tmp/image-previews/opening-walk-14b-2026-09-12'
PREVIEW.mkdir(parents=True,exist_ok=True)
FFMPEG='C:/AI/ComfyUI-Creative-AMD/venv/Lib/site-packages/imageio_ffmpeg/binaries/ffmpeg-win-x86_64-v7.1.exe'
def sha(path):
    with path.open('rb') as stream:return hashlib.file_digest(stream,'sha256').hexdigest()
contract=json.loads((OUT/'comparison-contract.json').read_text(encoding='utf-8'))
clips=[]
for name,size in [('shot-04-5b-existing.mp4',[576,1024]),('shot-04-14b-native.mp4',[512,896]),('shot-04-14b-fp8.mp4',[576,1024]),('comparison-5b-vs-14b.mp4',[1152,1088]),('comparison-half-speed.mp4',[1152,1088])]:
    path=OUT/name
    header=subprocess.run([FFMPEG,'-hide_banner','-i',str(path)],capture_output=True,text=True,encoding='utf-8',errors='replace')
    line=next(line for line in header.stderr.splitlines() if 'Video:' in line)
    width,height=map(int,re.search(r'(\d{2,5})x(\d{2,5})',line).groups())
    decoded=subprocess.run([FFMPEG,'-hide_banner','-loglevel','error','-i',str(path),'-map','0:v:0','-progress','pipe:1','-nostats','-f','null','-'],capture_output=True,text=True,check=True)
    progress=dict(row.split('=',1) for row in decoded.stdout.splitlines() if '=' in row)
    frames=int(progress['frame'])
    expected_frames=range(241,243) if name=='comparison-half-speed.mp4' else (121,)
    assert [width,height]==size and frames in expected_frames,(name,line,progress)
    clips.append({'file':name,'size':[width,height],'frames':frames,'durationSeconds':float(progress['out_time_us'])/1e6,'bytes':path.stat().st_size,'sha256':sha(path),'decodedWithoutError':True})
source_unchanged=sha(Path(contract['source']))==contract['sourceSha256']
old_unchanged=sha(Path(contract['existingClip']))==contract['existingClipSha256']==sha(OUT/'shot-04-5b-existing.mp4')
assert source_unchanged and old_unchanged
for seconds,label in [(1.25,'early'),(2.5,'middle'),(5,'last')]:
    target=PREVIEW/f'comparison-{label}.png'
    if not target.exists():
        subprocess.run([FFMPEG,'-hide_banner','-loglevel','error','-n','-ss',str(seconds),'-i',str(OUT/'comparison-5b-vs-14b.mp4'),'-frames:v','1',str(target)],check=True)
memory=list(csv.DictReader((OUT/'windows-memory.csv').open(encoding='utf-8-sig')))
report={'clips':clips,'originalSourceUnchanged':source_unchanged,'original5BUnchanged':old_unchanged,'observedWindowsMemory':{'samples':len(memory),'peakDedicatedGiB':max(int(row['dedicated_bytes']) for row in memory)/2**30,'peakSharedGiB':max(int(row['shared_bytes']) for row in memory)/2**30},'browserValidation':'Unavailable: CUA inventory returned no browsers. MP4 decoding and extracted frames are verified; optional HTML controls are not claimed as browser-tested.'}
(OUT/'validation.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print(json.dumps({k:v for k,v in report.items() if k!='clips'},ensure_ascii=False,indent=2))
print('All5 clips decode correctly; originals have121 frames and the half-speed derivative has241-242 frames. Comparison PNGs retained.')
