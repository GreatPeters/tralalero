"""Extract user-requested reusable RGBA sprites from retained chroma-key sheets.

Run with the task's Pillow/numpy environment. This does no AI generation; the
retained image-tool sources and prompts document the visual reconstruction.
"""
from pathlib import Path
import json
import numpy as np
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "map-concepts/harbor-ui-production-2026-09-15/sources"
OUTPUT = ROOT / "Assets/ShooterSurvival/UI/HarborWorkshop"
PREVIEW = ROOT / "tmp/image-previews/harbor-ui-production-2026-09-15"


def remove_key(image):
    rgb = np.asarray(image.convert("RGB"), dtype=np.float32)
    # Warm bronze, neutral steel and blue sneakers are disjoint from this key.
    magenta = np.minimum(rgb[:, :, 0], rgb[:, :, 2]) - rgb[:, :, 1]
    alpha = np.clip((210.0 - magenta) / 160.0, 0.0, 1.0)
    key = np.array([246.0, 7.0, 248.0], dtype=np.float32)
    color = np.clip((rgb - (1.0 - alpha[:, :, None]) * key) /
                    np.maximum(alpha[:, :, None], 0.001), 0, 255)
    rgba = np.dstack((color, alpha * 255)).astype(np.uint8)
    rgba[alpha == 0] = 0
    return Image.fromarray(rgba)


def extract(image, name, box, border):
    cut = remove_key(image.crop(box))
    bounds = cut.getchannel("A").getbbox()
    if bounds is None:
        raise ValueError(f"Empty sprite: {name}")
    # Four transparent pixels around the tightly trimmed artwork prevent bleed.
    tight = cut.crop(bounds)
    result = Image.new("RGBA", (tight.width + 8, tight.height + 8))
    result.paste(tight, (4, 4))
    result.save(OUTPUT / f"{name}.png")
    alpha = np.asarray(result.getchannel("A"))
    assert alpha.min() == 0 and alpha.max() == 255, name
    return {"name": name, "path": f"{OUTPUT.relative_to(ROOT).as_posix()}/{name}.png",
            "source_box": box, "width": result.width, "height": result.height,
            "border": border, "transparent_pixels": int((alpha == 0).sum()),
            "partial_alpha_pixels": int(((alpha > 0) & (alpha < 255)).sum())}


def main():
    OUTPUT.mkdir(parents=True, exist_ok=True)
    PREVIEW.mkdir(parents=True, exist_ok=True)
    surfaces = Image.open(SOURCE / "surfaces-magenta.png")
    if surfaces.size != (1254, 1254):
        raise ValueError("Surface source dimensions differ from reviewed crop contract")
    entries = []
    boxes = [
        ("ParchmentPanel", (32, 70, 620, 304), [38, 38, 38, 38]),
        ("WoodSign", (637, 70, 1226, 304), [58, 52, 58, 52]),
        ("GoldButton", (29, 370, 620, 564), [36, 36, 36, 36]),
        ("WoodButton", (640, 370, 1224, 564), [36, 36, 36, 36]),
        ("NavigationTile", (175, 610, 490, 909), [68, 68, 68, 68]),
        ("BrassPlaque", (770, 610, 1085, 909), [48, 48, 48, 48]),
        ("WalletPill", (92, 1008, 570, 1143), [64, 40, 64, 40]),
        ("WindowFrame", (640, 930, 1221, 1200), [60, 58, 60, 58]),
    ]
    for name, box, border in boxes:
        entries.append(extract(surfaces, name, box, border))
    icons = Image.open(SOURCE / "upgrade-parts-magenta.png")
    if icons.size != (1536, 1024):
        raise ValueError("Icon source dimensions differ from reviewed crop contract")
    for index, name in enumerate(["SteelToe", "CushionInsole", "SpringCoil", "RocketCharm", "LateralSneaker", "CoinPouch"]):
        x, y = (index % 3) * 512, (index // 3) * 512
        entries.append(extract(icons, name, (x, y, x + 512, y + 512), [0, 0, 0, 0]))
    equipment = Image.open(SOURCE / "equipment-magenta.png")
    if equipment.size != (1536, 1024):
        raise ValueError("Equipment source dimensions differ from reviewed crop contract")
    for index, name in enumerate(["ClassicHighTops", "HarborBoots", "SteelBoots", "RelicSneakers", "SkinTabFin", "HatTabCap"]):
        x, y = (index % 3) * 512, (index // 3) * 512
        entries.append(extract(equipment, name, (x, y, x + 512, y + 512), [0, 0, 0, 0]))
    reused = {
        "Coin": "Assets/JH/Image/Lobby/Upgrade/img_nav_coin.png",
        "Jewel": "Assets/JH/Image/Lobby/Upgrade/img_nav_diamond.png",
        "BackArrow": "Assets/JH/Image/Lobby/Upgrade/img_nav_arrow.png",
        "Settings": "Assets/JH/Image/Lobby/Upgrade/img_nav_setting.png",
        "GestureHand": "Assets/JH/Image/Lobby/Main/img_main_point.png",
        "GestureLeft": "Assets/JH/Image/Lobby/Main/img_main_arrow_L.png",
        "GestureRight": "Assets/JH/Image/Lobby/Main/img_main_arrow_R.png",
        "AttackIcon": "Assets/JH/UI/Upgrade/공격력.png",
        "BossDamageIcon": "Assets/JH/UI/Upgrade/보스피해.png",
        "HealthRegenIcon": "Assets/JH/UI/Upgrade/체력회복.png",
        "SahurIcon": "Assets/JH/UI/Upgrade/퉁퉁퉁사후르.png",
        "BoomBarIcon": "Assets/JH/UI/Upgrade/붐바르딜로.png",
        "StoryTileLegacy": "Assets/JH/Image/Lobby/Main/img_main_story.png",
        "ShopBackdrop": "Assets/JH/UI/Upgrade/Workshop_Background.png",
    }
    for name, path in reused.items():
        image = Image.open(ROOT / path).convert("RGBA")
        image.save(OUTPUT / f"{name}.png")
        entries.append({"name": name, "path": f"{OUTPUT.relative_to(ROOT).as_posix()}/{name}.png",
                        "reused_from": path, "width": image.width, "height": image.height,
                        "border": [0, 0, 0, 0]})
    approved = Image.open(ROOT / "tmp/image-previews/ui-original-variation-2026-09-14/ui-concept-c-v2-gameplay-start-numeric-levels.png")
    merchant = approved.crop((551, 116, 982, 287)).convert("RGBA")
    merchant.save(OUTPUT / "MerchantBackdrop.png")
    entries.append({"name": "MerchantBackdrop", "path": f"{OUTPUT.relative_to(ROOT).as_posix()}/MerchantBackdrop.png",
                    "source_box": [551, 116, 982, 287], "width": merchant.width, "height": merchant.height,
                    "border": [0, 0, 0, 0], "role": "opaque decorative header, intentionally retains workshop background"})
    (SOURCE.parent / "sprite-manifest.json").write_text(json.dumps(entries, indent=2, ensure_ascii=False), encoding="utf-8")
    # Review every sprite on both a bright and dark background, at UI-like sizes.
    sheet = Image.new("RGB", (1440, ((len(entries) + 2) // 3) * 230), "#28211a")
    draw = ImageDraw.Draw(sheet)
    for index, entry in enumerate(entries):
        col, row = index % 3, index // 3
        x, y = col * 480, row * 230
        draw.text((x + 14, y + 8), entry["name"], fill="white")
        src = Image.open(ROOT / entry["path"])
        for part, bg in enumerate(["#fff6e4", "#183c4b"]):
            tile = Image.new("RGBA", (228, 188), bg)
            thumb = src.copy()
            thumb.thumbnail((208, 170), Image.Resampling.LANCZOS)
            tile.alpha_composite(thumb, ((228 - thumb.width) // 2, (188 - thumb.height) // 2))
            sheet.paste(tile.convert("RGB"), (x + 8 + part * 234, y + 32))
    sheet.save(PREVIEW / "sprite-alpha-review.png")
    print(json.dumps({"sprites": len(entries), "output": str(OUTPUT), "alpha_review": str(PREVIEW / "sprite-alpha-review.png")}))


if __name__ == "__main__":
    main()
