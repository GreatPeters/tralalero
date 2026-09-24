from pathlib import Path
import sys
ROOT=Path.cwd();OUT=ROOT/'outputs/reststop-blender-v2-2026-09-23';sys.path.insert(0,str(ROOT/'tmp/ten-run-review-2026-09-22/pythonlib'))
from PIL import Image,ImageDraw,ImageFont
font=ImageFont.truetype('C:/Windows/Fonts/malgunbd.ttf',22)
files=sorted((OUT/'evidence').glob('view-*.png'))
for offset in range(0,len(files),8):
 group=files[offset:offset+8];sheet=Image.new('RGB',(1600,4*490),'#e8eee4');d=ImageDraw.Draw(sheet)
 for i,p in enumerate(group):
  im=Image.open(p).convert('RGB');im.thumbnail((800,450));x=(i%2)*800;y=(i//2)*490;sheet.paste(im,(x,y+36));d.text((x+14,y+5),p.stem,font=font,fill='#20382a')
 sheet.save(OUT/'evidence'/f'contact-final-{offset//8+1}.jpg',quality=92)
folder=OUT/'comparison'
if (folder/'v2-restroom.png').exists():
 for subject,title in [('store','편의점 — 동일 배율 비교'),('restroom','화장실 — 동일 배율 비교')]:
  sheet=Image.new('RGB',(2400,1080),'#e8eee4');d=ImageDraw.Draw(sheet)
  for i,version in enumerate(('v1','v2')):
   sheet.paste(Image.open(folder/f'{version}-{subject}.png').convert('RGB'),(i*1200,80));d.text((i*1200+25,20),f'{title} | {version}',font=font,fill='#20382a')
  sheet.save(folder/f'comparison-{subject}.png')
