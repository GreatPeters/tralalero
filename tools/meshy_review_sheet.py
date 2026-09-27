"""Combine Meshy concept views into one review sheet per batch.
Usage: py -3.11 tools/meshy_review_sheet.py <run_dir> <out.jpg> <asset[:try]> ..."""
import pathlib, sys
from PIL import Image, ImageDraw
run, out = pathlib.Path(sys.argv[1]), sys.argv[2]
rows = []
for spec in sys.argv[3:]:
    asset, _, tag = spec.partition(":")
    tag = tag or "try1"
    views = sorted((run / "concepts" / asset).glob(f"{tag}_*.png"))
    ims = []
    for v in views:
        im = Image.open(v).convert("RGBA")
        bg = Image.new("RGBA", im.size, (255, 255, 255, 255)); bg.alpha_composite(im)
        ims.append(bg.convert("RGB"))
    h = 300
    ims = [i.resize((max(1, int(i.width * h / i.height)), h)) for i in ims]
    row = Image.new("RGB", (max(900, sum(i.width for i in ims) + 10), h + 22), "white")
    x = 0
    for i in ims:
        row.paste(i, (x, 22)); x += i.width + 5
    ImageDraw.Draw(row).text((4, 4), f"{asset} {tag}", fill="black")
    rows.append(row)
W = max(r.width for r in rows)
sheet = Image.new("RGB", (W, sum(r.height for r in rows)), "white")
y = 0
for r in rows:
    sheet.paste(r, (0, y)); y += r.height
sheet.save(out, quality=85)
