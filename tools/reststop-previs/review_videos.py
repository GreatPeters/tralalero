"""Extract actual encoded critical frames for visual review, without changing art."""
from pathlib import Path
import argparse, json, subprocess, sys
ROOT=Path.cwd();OUT=ROOT/'outputs/reststop-blender-300s-2026-09-23'
sys.path.insert(0,str(ROOT/'tmp/ten-run-review-2026-09-22/pythonlib'))
from PIL import Image,ImageDraw,ImageFont
FFMPEG=Path.home()/'AppData/Roaming/Python/Python312/site-packages/imageio_ffmpeg/binaries/ffmpeg-win-x86_64-v7.1.exe'
parser=argparse.ArgumentParser();parser.add_argument('--revision',default='review-final');parser.add_argument('--stages',default='01,02,03,04,05,06,07,08,09,10');args=parser.parse_args()
folder=OUT/args.revision;folder.mkdir(exist_ok=True)
times={'01':[.5,10,17,23],'02':[1,8,13,23],'03':[2,7,12,22],'04':[1,9,15,24.5],'05':[1,12,29,53],'06':[2,11,17,26],'07':[2,11,17,27],'08':[2,11,17,27],'09':[2,8,16,23],'10':[2,9,17,23]}
font=ImageFont.truetype('C:/Windows/Fonts/malgunbd.ttf',21)
for idx in args.stages.split(','):
    receipt=OUT/'videos'/f'{idx}-reststop.json'
    if not receipt.exists():continue
    r=json.loads(receipt.read_text(encoding='utf8'));sheet=Image.new('RGB',(1600,980),'#eff0e6');d=ImageDraw.Draw(sheet)
    for j,t in enumerate(times[idx]):
        path=folder/f'{idx}-{t:05.1f}.png'
        subprocess.run([str(FFMPEG),'-hide_banner','-v','error','-threads','2','-ss',str(t),'-i',str(OUT/'videos'/r['file']),'-frames:v','1','-y',str(path)],check=True)
        im=Image.open(path).convert('RGB');im.thumbnail((800,450));x=(j%2)*800;y=(j//2)*490
        sheet.paste(im,(x,y+38));d.text((x+15,y+6),f'{idx} {r["title"]} | global {r["start_seconds"]+t:.1f}s',font=font,fill='#213625')
    sheet.save(folder/f'{idx}-review.jpg',quality=92)
    (folder/f'{idx}-source.json').write_text(json.dumps({'source_sha256':r['sha256'],'sample_times':times[idx]},indent=2))
    print(str(folder/f'{idx}-review.jpg'),flush=True)
