"""Make layered source files and an exact-scene lobby composite from separated art."""
from pathlib import Path
import json
import math
from PIL import Image, ImageDraw, ImageFont
from psd_tools import PSDImage

ROOT = Path(__file__).resolve().parents[1]
ART = ROOT / "Assets/ShooterSurvival/UI/HarborWorkshop"
WORK = ROOT / "map-concepts/harbor-ui-production-2026-09-15"
PREVIEW = ROOT / "tmp/image-previews/harbor-ui-production-2026-09-15"
FONT = ROOT / "Assets/JH/Font/KERISKEDU_B.ttf"
ENTRIES = json.loads((WORK / "sprite-manifest.json").read_text(encoding="utf-8"))
BY_NAME = {entry["name"]: entry for entry in ENTRIES}


def nine_slice(name, size):
    source = Image.open(ART / f"{name}.png").convert("RGBA")
    left, bottom, right, top = BY_NAME[name]["border"]
    if not any((left, bottom, right, top)):
        return source.resize(size, Image.Resampling.LANCZOS)
    out = Image.new("RGBA", size)
    xs = [0, left, source.width - right, source.width]
    ys = [0, top, source.height - bottom, source.height]
    tx = [0, left, size[0] - right, size[0]]
    ty = [0, top, size[1] - bottom, size[1]]
    for row in range(3):
        for col in range(3):
            part = source.crop((xs[col], ys[row], xs[col + 1], ys[row + 1]))
            part = part.resize((tx[col + 1] - tx[col], ty[row + 1] - ty[row]), Image.Resampling.LANCZOS)
            out.alpha_composite(part, (tx[col], ty[row]))
    return out


def main():
    (WORK / "layered").mkdir(exist_ok=True)
    atlas = PSDImage.new(mode="RGB", size=(2048, math.ceil(len(ENTRIES) / 4) * 512), color=(35, 30, 26))
    for index, entry in enumerate(ENTRIES):
        sprite = Image.open(ROOT / entry["path"]).convert("RGBA")
        sprite.thumbnail((470, 470), Image.Resampling.LANCZOS)
        atlas.create_pixel_layer(sprite, name=entry["name"],
                                 left=(index % 4) * 512 + (512 - sprite.width) // 2,
                                 top=(index // 4) * 512 + (512 - sprite.height) // 2)
    atlas_path = WORK / "layered/harbor-ui-sprites.psd"
    atlas.save(atlas_path)
    reopened = PSDImage.open(atlas_path)
    assert len(reopened) == len(ENTRIES)
    # Build an actual screenshot with separately assembled artwork; no generated scenery.
    size = (1080, 2340)
    scene = Image.open(ROOT / "tmp/image-previews/harbor-ui-production-2026-09-14/scene-reference.png").convert("RGBA")
    scene = scene.resize(size, Image.Resampling.NEAREST)
    psd = PSDImage.new(mode="RGB", size=size)
    psd.create_pixel_layer(scene, name="Actual Unity scene reference")
    overlay = Image.new("RGBA", size)
    layer_count = 1
    def put(name, image, x, y):
        nonlocal layer_count
        overlay.alpha_composite(image, (x, y))
        psd.create_pixel_layer(image, name=name, left=x, top=y)
        layer_count += 1
    def art(name, box, contain=False):
        x, y, w, h = box
        if contain:
            image = Image.open(ART / f"{name}.png").convert("RGBA")
            image.thumbnail((w, h), Image.Resampling.LANCZOS)
            x += (w - image.width) // 2; y += (h - image.height) // 2
        else:
            image = nine_slice(name, (w, h))
        put(name, image, x, y)
    def text(value, box, font_size, color, outline=0):
        x, y, w, h = box
        image = Image.new("RGBA", (w, h))
        draw = ImageDraw.Draw(image)
        font = ImageFont.truetype(str(FONT), font_size)
        draw.text((w / 2, h / 2), value, font=font, fill=color, anchor="mm",
                  stroke_width=outline, stroke_fill="#2e1e0d")
        # Keep legacy PSD Pascal layer names ASCII; Korean glyphs remain in pixels.
        put(f"Text_{layer_count:02}", image, x, y)
    for name, x, label in [("Coin", 40, "3,047"), ("Jewel", 435, "0")]:
        art("WalletPill", (x, 70, 370, 110))
        art(name, (x + 20, 71, 90, 100), True)
        text(label, (x + 120, 78, 220, 90), 48, "#251a10")
    art("BrassPlaque", (875, 58, 138, 138))
    art("Settings", (893, 76, 103, 103), True)
    art("WoodSign", (180, 230, 720, 205))
    text("CHAPTER 01", (220, 259, 640, 50), 37, "#fff1ce", 2)
    text("노량진 수산시장", (200, 330, 680, 76), 57, "#fff1ce", 2)
    text("좌우로 움직여", (140, 1090, 800, 100), 67, "#fff2d4", 3)
    text("게임시작", (140, 1190, 800, 120), 90, "#fff2d4", 3)
    art("GestureLeft", (317, 1365, 140, 88), True)
    art("GestureRight", (626, 1365, 140, 88), True)
    art("GestureHand", (480, 1335, 116, 150), True)
    # Preview health indicator is a separate layer; the runtime component tracks it live.
    health = Image.new("RGBA", (165, 28))
    d = ImageDraw.Draw(health)
    d.rounded_rectangle((0, 0, 164, 27), radius=6, fill="#15200e")
    d.rounded_rectangle((5, 5, 159, 22), radius=3, fill="#37db16")
    put("Player health bar preview", health, 448, 1715)
    for index, (icon, label) in enumerate([("LateralSneaker", "꾸미기"), ("AttackIcon", "강화"), ("StoryTileLegacy", "스토리")]):
        x = 28 + index * 350
        art("NavigationTile", (x, 2020, 325, 270))
        art(icon, (x + 85, 2040, 155, 150), True)
        text(label, (x + 25, 2200, 275, 70), 54, "#fff1ce", 2)
    overlay.save(PREVIEW / "start-ui-overlay.png")
    composite = Image.alpha_composite(scene, overlay)
    composite.save(PREVIEW / "start-actual-scene-composite.png")
    psd_path = WORK / "layered/start-actual-scene.psd"
    psd.save(psd_path)
    restored = PSDImage.open(psd_path)
    assert len(restored) == layer_count
    (WORK / "layered/verification.json").write_text(json.dumps({
        "sprite_layers": len(reopened), "lobby_layers": len(restored),
        "scene_source": "unaltered actual camera capture, resized 2x nearest-neighbor",
        "text_layers": "rasterized separately; exact strings/font remain in this reproducible script",
        "png_assets": "native resolution; PSD sprite sheet layers are fitted review copies"
    }, indent=2), encoding="utf-8")
    print(json.dumps({"sprite_layers": len(reopened), "lobby_layers": len(restored), "lobby_psd": str(psd_path)}))


if __name__ == "__main__":
    main()
