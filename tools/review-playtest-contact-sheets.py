from pathlib import Path
import sys
sys.path.insert(0,str(Path('tmp/ten-run-review-2026-09-22/pythonlib').resolve()))
from PIL import Image,ImageDraw
base=Path(sys.argv[2]) if len(sys.argv)>2 else Path('tmp/image-previews/ten-run-review-2026-09-22')
run=base/f'run-{int(sys.argv[1]):02}'
files=sorted(run.glob('*.png'))
out=(base.parent if base.name=='runs' else base)/'contact-sheets';out.mkdir(exist_ok=True)
for start in range(0,len(files),24):
    page=files[start:start+24];sheet=Image.new('RGB',(960,4*375),(22,29,37));draw=ImageDraw.Draw(sheet)
    for i,path in enumerate(page):
        im=Image.open(path).convert('RGB');im.thumbnail((155,342));x=(i%6)*160;y=(i//6)*375
        sheet.paste(im,(x,y));draw.text((x+2,y+344),path.stem[:22],fill='white')
    sheet.save(out/f'{run.name}-{start//24:02}.jpg',quality=88)
print(f'{len(files)} frames; {out}')
