from pathlib import Path
import subprocess,sys
ROOT=Path.cwd();OUT=ROOT/'outputs/reststop-blender-v2-2026-09-23/comparison';OUT.mkdir(exist_ok=True)
sys.path.insert(0,str(ROOT/'tmp/ten-run-review-2026-09-22/pythonlib'))
from PIL import Image,ImageDraw,ImageFont
ffmpeg=Path.home()/'AppData/Roaming/Python/Python312/site-packages/imageio_ffmpeg/binaries/ffmpeg-win-x86_64-v7.1.exe'
sheet=Image.new('RGB',(2560,785),'#e8eee4');d=ImageDraw.Draw(sheet);font=ImageFont.truetype('C:/Windows/Fonts/malgunbd.ttf',28)
for i,(folder,label) in enumerate([('reststop-blender-300s-2026-09-23','이전 · 수직 탑뷰'),('reststop-blender-v2-2026-09-23','수정 · 사선 쿼터뷰')]):
 p=OUT/f'defense-v{i+1}.png';subprocess.run([str(ffmpeg),'-v','error','-threads','2','-ss','54','-i',str(ROOT/'outputs'/folder/'videos/05-reststop.mp4'),'-frames:v','1','-y',str(p)],check=True);sheet.paste(Image.open(p).convert('RGB'),(1280*i,65));d.text((1280*i+20,14),f'{label} | 전체 154초',font=font,fill='#203927')
sheet.save(OUT/'comparison-defense.png')
