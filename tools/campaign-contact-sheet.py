"""Contact sheet of actual unretouched GameView captures for visual inspection."""
import argparse
from pathlib import Path
import sys
sys.path.insert(0,str(Path('tmp/ten-run-review-2026-09-22/pythonlib').resolve()))
from PIL import Image, ImageDraw
parser=argparse.ArgumentParser();parser.add_argument('output');parser.add_argument('files',nargs='+');args=parser.parse_args()
dest=Path(args.output)
if dest.exists():raise FileExistsError(dest)
width=320;height=734;columns=min(4,len(args.files));rows=(len(args.files)+columns-1)//columns
sheet=Image.new('RGB',(width*columns,height*rows),(18,25,34));draw=ImageDraw.Draw(sheet)
for i,file in enumerate(args.files):
    p=Path(file);img=Image.open(p).convert('RGB');img.thumbnail((width,height-42));x=i%columns*width;y=i//columns*height
    sheet.paste(img,(x,y));draw.text((x+4,y+height-36),p.parent.name+'/'+p.name,fill='white')
dest.parent.mkdir(parents=True,exist_ok=True);sheet.save(dest);print(dest)
