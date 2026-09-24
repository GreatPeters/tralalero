"""Inspect before/active/after evidence from the actual encoded clips."""
from pathlib import Path
import sys,json,subprocess
ROOT=Path.cwd();OUT=ROOT/'outputs/reststop-blender-v2-2026-09-23';FOLDER=OUT/'gimmick-review';FOLDER.mkdir(exist_ok=True)
sys.path.insert(0,str(ROOT/'tmp/ten-run-review-2026-09-22/pythonlib'));sys.path.insert(0,str(Path(__file__).resolve().parent))
from PIL import Image,ImageDraw,ImageFont
from timeline import STAGES
ffmpeg=Path.home()/'AppData/Roaming/Python/Python312/site-packages/imageio_ffmpeg/binaries/ffmpeg-win-x86_64-v7.1.exe'
events=[('후진 차량',[29.3,30.6,32.8]),('분사구 방어',[106.7,107.8,109.5]),('편의점 카트 행렬',[201.3,202.1,203.8]),('진열대 쓰러짐',[205.5,206.2,207.2]),('화장실 누수',[226.4,228.4,230.2]),('청소 로봇',[221.5,223.5,225.5]),('주유소 버스',[264.5,266.5,268.0]),('출구 차량',[281,286,294])]
font=ImageFont.truetype('C:/Windows/Fonts/malgunbd.ttf',23);records=[]
for n,(label,times) in enumerate(events):
 sheet=Image.new('RGB',(1920,405),'#e8eee4');draw=ImageDraw.Draw(sheet)
 for i,t in enumerate(times):
  idx,title,a,b=next(s for s in STAGES if s[2]<=t<s[3]);p=OUT/'videos'/f'{idx}-reststop.mp4';receipt=p.with_suffix('.json')
  if not receipt.exists():break
  frame=FOLDER/f'{n+1:02}-{i}.png';subprocess.run([str(ffmpeg),'-v','error','-threads','2','-ss',str(t-a),'-i',str(p),'-frames:v','1','-y',str(frame)],check=True)
  im=Image.open(frame).convert('RGB');im.thumbnail((640,360));sheet.paste(im,(i*640,45));draw.text((i*640+12,8),f'{label} | {t:.1f}s',font=font,fill='#213927')
 else:
  sheet.save(FOLDER/f'{n+1:02}-sequence.jpg',quality=93);records.append({'subject':label,'global_times':times,'file':f'{n+1:02}-sequence.jpg'})
(FOLDER/'manifest.json').write_text(json.dumps(records,ensure_ascii=False,indent=2),encoding='utf8');print('GIMMICKS_REVIEW',len(records),flush=True)
