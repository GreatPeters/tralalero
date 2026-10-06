"""Extract unchanged production movie frames for visual review; never regenerates art."""
from pathlib import Path
import json
import subprocess
import imageio_ffmpeg
from PIL import Image, ImageDraw, ImageFont

root=Path(__file__).resolve().parents[1]
out=root/'tmp/image-previews/s22-quality-audit-2026-10-01/video'
out.mkdir(parents=True,exist_ok=True)
font=ImageFont.truetype('C:/Windows/Fonts/malgun.ttf',20)
movies=[('opening','Assets/JH/UI/Opening/Animated/Curse_Opening_Animated.mp4',[.5,4,7.7,8.3,11,15,19.6,20.4,24,29.5,30.4,34,38]),('highway-entry','Assets/JH/UI/ChapterTransitions/Highway_Entry.mp4',[.3,1.3,2.6,3.7,4.7]),('reststop-entry','Assets/JH/UI/ChapterTransitions/RestStop_Entry.mp4',[.3,1.3,2.6,3.7,4.7])]
for label,path,times in movies:
    cols=5 if len(times)<=5 else 5
    sheet=Image.new('RGB',(cols*260,((len(times)+cols-1)//cols)*490),(18,28,40));draw=ImageDraw.Draw(sheet)
    for i,sec in enumerate(times):
        target=out/f'{label}-{sec:05.1f}.png'
        if not target.exists():
            subprocess.run([imageio_ffmpeg.get_ffmpeg_exe(),'-hide_banner','-loglevel','error','-ss',str(sec),'-i',str(root/path),'-frames:v','1',str(target)],check=True)
        im=Image.open(target).convert('RGB');im.thumbnail((250,445))
        x=(i%cols)*260;y=(i//cols)*490
        sheet.paste(im,(x+(260-im.width)//2,y+32));draw.text((x+8,y+5),f'{label} {sec:.1f}s',font=font,fill='white')
    sheet.save(out/f'{label}-contact.png')
print(out)
