"""Assemble diagnostic contact sheets from real renders; never alter source assets."""
import argparse,json,math
from pathlib import Path
from PIL import Image,ImageDraw,ImageFont

ROOT=Path(__file__).resolve().parents[1];OUT=ROOT/'outputs/reststop-production-2026-09-24'
parser=argparse.ArgumentParser();parser.add_argument('--folder');parser.add_argument('--reference');parser.add_argument('--stage',choices=('shape','texture_source','final_compare'))
parser.add_argument('--source-folder',help='Explicit accepted high source for manual-correction comparisons')
args=parser.parse_args()
if args.folder:
    assert args.reference and args.stage,'Manual sheets need explicit reference and stage'
    pending={'folder':str(Path(args.folder).resolve()),'reference':str(Path(args.reference).resolve()),'stage':args.stage}
else:
    pending=json.loads((OUT/'review-pending.json').read_text(encoding='utf8'))
    assert pending['state']=='pending','No current review gate'
folder=Path(pending['folder']);reference=Path(pending['reference']);quality=folder/'quality'
entries=[('REFERENCE '+reference.stem,reference)]
entries += [(name.upper(),quality/(name+'.png')) for name in ('hero','top','opposite','front','neutral')]
entries += [(name.upper(),quality/(name+'.png')) for name in ('bottom','bottom-flat','bottom-neutral')]
if pending['stage']=='final_compare':
    source_folder=Path(args.source_folder).resolve() if args.source_folder else folder.parent
    entries += [('SOURCE '+name.upper(),source_folder/'quality'/(name+'.png')) for name in ('hero','neutral','bottom','bottom-neutral')]
entries=[(label,path) for label,path in entries if path.exists()]
signature=[{'label':label,'path':str(path),'mtime':path.stat().st_mtime_ns,'bytes':path.stat().st_size} for label,path in entries]
number=1
while True:
    stem='review-contact' if number==1 else f'review-contact-r{number}'
    target=quality/(stem+'.png');receipt=quality/(stem+'.json')
    if not target.exists():break
    if receipt.exists() and json.loads(receipt.read_text(encoding='utf8'))==signature:
        print(json.dumps({'sheet':str(target),'reference':reference.stem,'stage':pending['stage']}));raise SystemExit(0)
    number+=1
width=600;height=536;columns=3;rows=math.ceil(len(entries)/columns)
sheet=Image.new('RGB',(columns*width,rows*height),(235,237,230));draw=ImageDraw.Draw(sheet)
font=ImageFont.truetype('C:/Windows/Fonts/arial.ttf',18)
for i,(label,path) in enumerate(entries):
    x=(i%columns)*width;y=(i//columns)*height
    draw.rectangle((x,y,x+width,y+25),fill=(37,58,47));draw.text((x+9,y+3),label,fill='white',font=font)
    with Image.open(path) as source:
        rgba=source.convert('RGBA');rgba.thumbnail((width-8,height-30),Image.Resampling.LANCZOS)
        tile=Image.new('RGBA',rgba.size,'white');tile.alpha_composite(rgba)
        sheet.paste(tile.convert('RGB'),(x+(width-rgba.width)//2,y+27+(height-29-rgba.height)//2))
sheet.save(target);receipt.write_text(json.dumps(signature,indent=2),encoding='utf8')
print(json.dumps({'sheet':str(target),'reference':reference.stem,'stage':pending['stage']}))
