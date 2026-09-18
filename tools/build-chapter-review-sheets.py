"""Assemble diagnostic contact sheets without modifying production images."""
import json
from pathlib import Path
from PIL import Image, ImageDraw, ImageOps

root = Path(__file__).resolve().parent.parent
output = root/'map-concepts/chapters-polish-2026-09-12/review'
output.mkdir(exist_ok=True)

def sheet(items, name, columns=3, size=(384, 384)):
    if not items:
        return
    width, height = size
    canvas = Image.new('RGB', (columns*width, ((len(items)+columns-1)//columns)*(height+24)), '#151b20')
    draw = ImageDraw.Draw(canvas)
    for index, (label, path) in enumerate(items):
        picture = ImageOps.contain(Image.open(path).convert('RGB'), size)
        x, y = (index % columns)*width, (index//columns)*(height+24)
        canvas.paste(picture, (x+(width-picture.width)//2, y))
        draw.text((x+8, y+height+4), label, fill='white')
    canvas.save(output/(name+'.png'))

rigs = root/'outputs/chapters-polish-2026-09-12/rigged/v2'
for kind, filename in [('front', 'front.png'), ('back', 'back.png'), ('attack', 'frame_0024.png'), ('walk', 'frame_0008.png')]:
    folder = 'evidence-fresh-grounded-attack_once' if kind == 'attack' else 'evidence-fresh-grounded-walk'
    items = [(p.name, p/folder/filename) for p in sorted(rigs.iterdir()) if (p/folder/filename).exists()]
    sheet(items, 'mascots-'+kind)

rows = json.loads((root/'map-concepts/highway-enemies-2026-09-11/props-imported.json').read_text(encoding='utf-8'))
sheet([(str(row['id']), Path(row['source'])/'preview.png') for row in rows if (Path(row['source'])/'preview.png').exists()],
      'props-original', columns=5, size=(256,256))
props = root/'outputs/chapters-polish-2026-09-12/props-v3'
sheet([(p.name, p/'evidence-fresh-model/perspective.png') for p in sorted(props.iterdir()) if (p/'evidence-fresh-model/perspective.png').exists()],
      'props-repaired')
for folder in (root/'outputs/chapters-polish-2026-09-12/transitions').iterdir():
    frames = [(f'{frame/24:.1f}s', folder/'frames'/f'{frame:05d}.png') for frame in [0,24,48,72,96,120]]
    if all(path.exists() for _,path in frames):
        sheet(frames, folder.name, size=(288,512))
print(output)
