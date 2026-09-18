"""Slice the approved production sheets into real-alpha Unity sprites and a PSD.

Run: tmp/coastal-ui-venv/Scripts/python.exe tools/prepare-coastal-ui.py
"""
from pathlib import Path
import json
import numpy as np
from PIL import Image, ImageDraw
from psd_tools import PSDImage

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "map-concepts/coastal-enamel-ui-2026-09-16/sources"
OUTPUT = ROOT / "Assets/ShooterSurvival/UI/CoastalEnamel"
REVIEW = ROOT / "tmp/image-previews/coastal-enamel-ui-2026-09-16"
OUTPUT.mkdir(parents=True, exist_ok=True)
REVIEW.mkdir(parents=True, exist_ok=True)
entries = []


def write(name, image, border=(0, 0, 0, 0), source=None, box=None):
    image = image.convert("RGBA")
    image.save(OUTPUT / f"{name}.png")
    alpha = np.asarray(image.getchannel("A"))
    entries.append(dict(name=name, width=image.width, height=image.height,
                        border=border, source=source, box=box,
                        transparent=int((alpha == 0).sum()),
                        partial=int(((alpha > 0) & (alpha < 255)).sum())))


def cut(sheet, name, box, border=(0, 0, 0, 0)):
    rgb = np.asarray(sheet.crop(box).convert("RGB"), dtype=np.float32)
    key_distance = np.minimum(rgb[:, :, 0], rgb[:, :, 2]) - rgb[:, :, 1]
    alpha = np.clip((210 - key_distance) / 160, 0, 1)
    color = np.clip((rgb - (1-alpha[:, :, None])*np.array([255, 0, 255])) /
                    np.maximum(alpha[:, :, None], .001), 0, 255)
    rgba = np.dstack((color, alpha*255)).astype(np.uint8)
    rgba[alpha < .02] = 0
    image = Image.fromarray(rgba)
    bounds = image.getchannel("A").getbbox()
    if bounds is None:
        raise ValueError(f"Empty sprite: {name}")
    image = image.crop(bounds)
    padded = Image.new("RGBA", (image.width+8, image.height+8))
    padded.paste(image, (4, 4))
    write(name, padded, border, Path(sheet.filename).name, box)


surfaces = Image.open(SOURCE / "surfaces-magenta.png")
assert surfaces.size == (1536, 1024)
for name, box, border in [
    ("IvoryPanel", (16, 18, 750, 252), (88, 82, 88, 82)),
    ("NavyPanel", (775, 18, 1523, 252), (82, 82, 82, 82)),
    ("YellowButton", (16, 262, 750, 480), (84, 76, 84, 76)),
    ("NavyButton", (775, 262, 1523, 480), (84, 76, 84, 76)),
    ("NavigationTile", (200, 487, 570, 779), (55, 85, 55, 48)),
    ("SquareButton", (985, 487, 1300, 779), (50, 50, 50, 50)),
    ("Wallet", (17, 790, 750, 990), (80, 78, 80, 78)),
    ("Frame", (775, 780, 1523, 995), (80, 84, 80, 84)),
]:
    cut(surfaces, name, box, border)

# Simple native UI shapes extend the extracted palette for compact list rows.
# Their border is deliberately thin; heavy panel fasteners belong to major frames.
row = Image.new("RGBA", (128, 128))
mask = Image.new("L", row.size)
ImageDraw.Draw(mask).rounded_rectangle((2, 2, 125, 125), radius=9, fill=255)
center = Image.open(OUTPUT / "IvoryPanel.png").crop((110, 70, 580, 135)).resize(row.size)
row.paste(center, (0, 0), mask)
ImageDraw.Draw(row).rounded_rectangle((2, 2, 125, 125), radius=9, outline=(84, 115, 146, 255), width=2)
write("RowPanel", row, (12, 12, 12, 12), source="native shape + extracted ivory center")
selection = Image.new("RGBA", (128, 128))
ImageDraw.Draw(selection).rounded_rectangle((2, 2, 125, 125), radius=9, outline=(2, 195, 222, 255), width=4)
write("SelectionFrame", selection, (12, 12, 12, 12), source="native selection outline")

icons = Image.open(SOURCE / "icons-magenta.png")
icon_names = ["Coin", "Jewel", "Settings", "Sneaker", "Attack", "Story", "Heart", "Replay",
              "Swipe", "Left", "Right", "Anchor"]
for i, name in enumerate(icon_names):
    col, row = i % 4, i // 4
    box = (col*384, row*341, (col+1)*384, min((row+1)*341, 1024))
    if name == "Swipe":
        box = (15, 690, 408, 994)
    elif name == "Left":
        box = (440, 690, 757, 994)
    cut(icons, name, box)

skins = Image.open(SOURCE / "skins-magenta.png")
for i, name in enumerate(["skin_original", "skin_coral", "skin_ice", "skin_sand",
                          "skin_armor", "skin_raider", "skin_relic", "skin_diver"]):
    x, y = i % 4*384, i // 4*512
    cut(skins, name, (x, y, x+384, y+512))

background = Image.open(SOURCE / "shop-background.png")
write("ShopBackdrop", background, source="shop-background.png")
write("PlatformWood", background.crop((120, 1200, 820, 1620)), source="shop-background.png", box=(120, 1200, 820, 1620))
reference = Image.open(ROOT / "tmp/image-previews/harbor-ui-all-2026-09-15/1/03-upgrades.png")
for name, box in [("Merchant", (146, 234, 714, 394)),
                  ("Chapter1", (830, 563, 952, 678)),
                  ("Chapter2", (830, 702, 952, 817)),
                  ("Chapter3", (830, 845, 952, 954))]:
    write(name, reference.crop(box), source="approved style 1 upgrades", box=box)

# Existing alpha icons for the remaining ten permanent-upgrade entries and
# tutorial topics remain artwork references; prices/effects are never rasterized.
legacy = ROOT / "Assets/ShooterSurvival/UI/HarborWorkshop"
for name in ["SteelToe", "CushionInsole", "SpringCoil", "RocketCharm", "CoinPouch",
             "BossBreaker", "HealingInsert", "SahurShield", "BomberCharm", "LateralSneaker"]:
    write(name, Image.open(legacy / f"{name}.png"), source=f"HarborWorkshop/{name}.png")

sheet = Image.new("RGB", (1440, ((len(entries)+3)//4)*230), "#0c2346")
draw = ImageDraw.Draw(sheet)
psd = PSDImage.new("RGB", (2048, 2048), color=(12, 35, 70))
for i, entry in enumerate(entries):
    original = Image.open(OUTPUT / (entry["name"]+".png"))
    image = original.copy()
    image.thumbnail((320, 174))
    x, y = i % 4*360+20, i // 4*230+26
    sheet.paste(image, (x, y), image)
    draw.text((x, y+180), entry["name"], fill="white")
    layer = psd.create_pixel_layer(original, name=entry["name"],
                                   top=(2048-original.height)//2, left=(2048-original.width)//2)
    layer.visible = i == 0
sheet.save(REVIEW / "sprite-review.png")
psd.save(SOURCE.parent / "CoastalEnamel-Sprites.psd")
assert len(PSDImage.open(SOURCE.parent / "CoastalEnamel-Sprites.psd")) == len(entries)
(SOURCE.parent / "sprite-manifest.json").write_text(json.dumps(entries, indent=2), encoding="utf-8")
print(json.dumps({"sprites": len(entries), "psd_layers": len(entries), "review": str(REVIEW / "sprite-review.png")}))
