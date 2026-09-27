"""Tile playtest frames into contact sheets for review.

Usage: py -3.11 tools/contact_sheet.py <frame_dir> <out_prefix> [cols=6] [rows=3] [width=1800]
Frames are taken in name order; each sheet is labelled with the frame file names.
"""
import pathlib
import sys

from PIL import Image, ImageDraw


def main():
    src, prefix = pathlib.Path(sys.argv[1]), pathlib.Path(sys.argv[2])
    cols = int(sys.argv[3]) if len(sys.argv) > 3 else 6
    rows = int(sys.argv[4]) if len(sys.argv) > 4 else 3
    width = int(sys.argv[5]) if len(sys.argv) > 5 else 1800
    frames = sorted(src.glob("*.png"))
    if not frames:
        raise SystemExit("no frames in " + str(src))
    first = Image.open(frames[0])
    cell_w = width // cols
    cell_h = int(cell_w * first.height / first.width)
    per = cols * rows
    prefix.parent.mkdir(parents=True, exist_ok=True)
    for sheet_index in range(0, len(frames), per):
        sheet = Image.new("RGB", (cell_w * cols, cell_h * rows), (20, 20, 20))
        draw = ImageDraw.Draw(sheet)
        for i, path in enumerate(frames[sheet_index:sheet_index + per]):
            img = Image.open(path).convert("RGB").resize((cell_w, cell_h))
            x, y = (i % cols) * cell_w, (i // cols) * cell_h
            sheet.paste(img, (x, y))
            draw.rectangle([x, y, x + 70, y + 18], fill=(0, 0, 0))
            draw.text((x + 4, y + 3), path.stem, fill=(255, 255, 0))
        out = prefix.with_name(f"{prefix.name}-{sheet_index // per:02d}.jpg")
        sheet.save(out, quality=82)
        print(out)


if __name__ == "__main__":
    main()
