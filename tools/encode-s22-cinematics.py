"""Encode real animation frames, retaining the supplied first 30 seconds of the opening."""
from pathlib import Path
import subprocess,json,hashlib
import imageio_ffmpeg
from PIL import Image,ImageDraw,ImageFont
ROOT=Path(__file__).resolve().parents[1];OUT=ROOT/'outputs/s22-polish-2026-10-01/cinematics-v2'
FF=imageio_ffmpeg.get_ffmpeg_exe()
def run(args):subprocess.run([FF,'-hide_banner','-loglevel','error','-y',*map(str,args)],check=True)
assert (OUT/'record-complete.txt').exists()
records=[]
for shot,name,n in [(3,'Market_Arrival',216),(4,'Highway_Aligned',121),(5,'RestStop_Aligned',121)]:
 files=sorted((OUT/f'shot-{shot}').glob('frame-*.jpg'));assert len(files)==n,(shot,len(files))
 assert len({hashlib.sha256(p.read_bytes()).hexdigest() for p in files})>n*.8,'Motion frames are unexpectedly duplicated'
 run(['-framerate',24,'-i',OUT/f'shot-{shot}/frame-%04d.jpg','-frames:v',n,'-c:v','libx264','-preset','slow','-crf',18,'-profile:v','main','-pix_fmt','yuv420p','-movflags','+faststart',OUT/(name+'.mp4')])
 run(['-ss',2,'-i',OUT/(name+'.mp4'),'-frames:v',1,OUT/(name+'.png')])
 count,duration=imageio_ffmpeg.count_frames_and_secs(str(OUT/(name+'.mp4')));assert count==n
 records.append(dict(name=name,frames=count,seconds=duration,bytes=(OUT/(name+'.mp4')).stat().st_size))
old=ROOT/'Assets/JH/UI/Opening/Animated/Curse_Opening_Animated.mp4'
run(['-i',old,'-i',OUT/'Market_Arrival.mp4','-filter_complex','[0:v]trim=start=0:end=30,setpts=PTS-STARTPTS,setsar=1[a];[1:v]scale=720:1280,setsar=1,setpts=PTS-STARTPTS[b];[a][b]concat=n=2:v=1:a=0[v]','-map','[v]','-map','0:a?','-c:v','libx264','-preset','slow','-crf',19,'-profile:v','main','-pix_fmt','yuv420p','-r',24,'-frames:v',936,'-c:a','copy','-movflags','+faststart',OUT/'Opening_Aligned.mp4'])
count,duration=imageio_ffmpeg.count_frames_and_secs(str(OUT/'Opening_Aligned.mp4'));assert count==936
records.append(dict(name='Opening_Aligned',frames=count,seconds=duration,retainedOriginalSeconds=30,bytes=(OUT/'Opening_Aligned.mp4').stat().st_size))
(OUT/'encoding.json').write_text(json.dumps(records,indent=2),encoding='utf-8')
font=ImageFont.truetype('C:/Windows/Fonts/malgun.ttf',18)
sheet=Image.new('RGB',(1000,1470),'#182432');draw=ImageDraw.Draw(sheet)
for row,shot in enumerate((3,4,5)):
 for col,fraction in enumerate((.02,.33,.66,.96)):
  files=sorted((OUT/f'shot-{shot}').glob('frame-*.jpg'));p=files[round((len(files)-1)*fraction)];im=Image.open(p);im.thumbnail((242,440));sheet.paste(im,(col*250,row*490+35));draw.text((col*250+5,row*490+8),f'Shot {shot} / {fraction:.0%}',font=font,fill='white')
sheet.save(ROOT/'tmp/image-previews/s22-polish-2026-10-01/cinematics-motion.png')
print(json.dumps(records))
