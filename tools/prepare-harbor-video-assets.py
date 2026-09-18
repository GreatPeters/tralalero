"""Produce separated video UI art, native-layout manifests and reviewable PSDs.

The user requested cutting/reconstruction. Image-tool edits supply blank plates;
this script performs deterministic cutting, alpha extraction and layer assembly.
"""
from pathlib import Path
import json
import math
import runpy
import numpy as np
from PIL import Image, ImageDraw, ImageFont, ImageChops
from psd_tools import PSDImage

ROOT = Path(__file__).resolve().parents[1]
WORK = ROOT / "map-concepts/harbor-video-ui-2026-09-15/production"
SOURCE = WORK / "sources"
ART = ROOT / "Assets/ShooterSurvival/UI/HarborVideo"
PREVIEW = ROOT / "tmp/image-previews/harbor-video-ui-2026-09-15/production"
SIZE = (887, 1774)
FONT = ROOT / "Assets/JH/Font/KERISKEDU_B.ttf"
remove_key = runpy.run_path(str(ROOT / "tools/extract-harbor-ui-sprites.py"))["remove_key"]
entries = []


def key_warm_art(image):
    keyed = np.asarray(remove_key(image), dtype=np.int16).copy()
    # These plates contain only warm wood/brass/parchment: residual magenta in
    # antialiased key edges is spill, not an intended foreground color.
    spill = np.maximum(np.minimum(keyed[:, :, 0], keyed[:, :, 2]) - keyed[:, :, 1], 0)
    keyed[:, :, 0] -= spill
    keyed[:, :, 2] -= spill
    keyed[:, :, 2] = np.where(spill > 8, np.minimum(keyed[:, :, 2], keyed[:, :, 1]), keyed[:, :, 2])
    return Image.fromarray(np.clip(keyed, 0, 255).astype(np.uint8))


def rounded_mask(size, radius):
    mask = Image.new("L", (size[0] * 4, size[1] * 4))
    ImageDraw.Draw(mask).rounded_rectangle((0, 0, size[0] * 4 - 1, size[1] * 4 - 1),
                                          radius=radius * 4, fill=255)
    return mask.resize(size, Image.Resampling.LANCZOS)


def save_asset(name, image, border=(0, 0, 0, 0), source="", box=None, method="crop"):
    image = image.convert("RGBA")
    image.save(ART / f"{name}.png")
    alpha = np.asarray(image.getchannel("A"))
    entries.append({"name": name, "file": name + ".png", "width": image.width, "height": image.height,
                    "border": list(border), "source": source, "box": box, "method": method,
                    "transparent_pixels": int((alpha == 0).sum()),
                    "partial_alpha_pixels": int(((alpha > 0) & (alpha < 255)).sum())})


def crop(plate, name, box, border=(0, 0, 0, 0), radius=0, keyed=False):
    image = Image.open(SOURCE / plate).crop(box).convert("RGBA")
    if keyed:
        image = key_warm_art(image)
    if radius:
        image.putalpha(ImageChops.multiply(image.getchannel("A"), rounded_mask(image.size, radius)))
    save_asset(name, image, border, plate, box, "restored-plate crop" + (" and key" if keyed else ""))


def stretch(name, size):
    image = Image.open(ART / f"{name}.png").convert("RGBA")
    entry = next(entry for entry in entries if entry["name"] == name)
    left, bottom, right, top = entry["border"]
    if not any(entry["border"]):
        return image.resize(size, Image.Resampling.LANCZOS)
    assert size[0] >= left + right and size[1] >= top + bottom, (name, size)
    output = Image.new("RGBA", size)
    xs, ys = [0, left, image.width - right, image.width], [0, top, image.height - bottom, image.height]
    tx, ty = [0, left, size[0] - right, size[0]], [0, top, size[1] - bottom, size[1]]
    for y in range(3):
        for x in range(3):
            part = image.crop((xs[x], ys[y], xs[x + 1], ys[y + 1]))
            part = part.resize((tx[x + 1] - tx[x], ty[y + 1] - ty[y]), Image.Resampling.LANCZOS)
            output.alpha_composite(part, (tx[x], ty[y]))
    return output


def layers_for(variant):
    layers = []
    def art(name, asset, rect, kind="image", **extra):
        layers.append(dict(name=name, asset=asset, rect=list(rect), kind=kind, **extra))
    def text(name, value, rect, size, color="#FFF1CC", align="center", outline=0):
        layers.append(dict(name=name, kind="text", text=value, rect=list(rect), fontSize=size,
                           color=color, align=align, outline=outline))
    art("WoodBacking", "WoodBackground", (0, 0, *SIZE))
    if variant == "A":
        art("HeaderWood", "A_Header", (0, 0, 620, 106))
        art("HeaderRightCap", "A_HeaderEndcap", (847, 0, 40, 106))
        art("MovieSlot", "MovieFramePreview", (20, 121, 847, 1409), "video")
        art("MovieFrame", "A_VideoFrame", (0, 102, 887, 1446))
        art("CaptionShade", "SubtitleGradient", (21, 1235, 845, 292))
        art("CaptionDivider", "CaptionDivider", (71, 1350, 390, 22))
        text("StoryCounter", "이야기 01 / 04", (60, 25, 465, 70), 43, align="left")
        art("SkipButton", "A_SkipButton", (623, 16, 225, 85), "button")
        text("SkipLabel", "건너뛰기", (650, 29, 173, 57), 30)
        text("SceneTitle", "훔친 신발", (71, 1270, 600, 77), 47, "#FFD269", "left", 1)
        text("Caption", "신단의 신발을 물고,\n상어는 항구를 빠져나갔다.", (73, 1386, 741, 122), 34, align="left", outline=1)
        for index in range(4):
            art(f"StorySegment{index + 1}", "ProgressOn" if index == 0 else "ProgressOff", (204 + index * 123, 1556, 108, 33))
        art("ReplayButton", "A_ReplayButton", (35, 1607, 272, 119), "button")
        art("NextButton", "A_NextButton", (331, 1605, 521, 122), "button")
        text("ReplayLabel", "다시 보기", (58, 1630, 226, 70), 36)
        text("NextLabel", "다음 장면", (380, 1624, 377, 79), 48, "#2C1808")
        art("NextArrow", "ChevronInk", (766, 1641, 30, 46))
    else:
        art("UpperRope", "RopeKnot", (31, -87, 73, 257))
        art("LeftMap", "ShipMap", (-31, 277, 144, 390))
        art("RightMap", "CoastMap", (782, 359, 166, 496))
        art("LeftLantern", "LanternRope", (-16, 695, 119, 363))
        art("LowerRope", "RopeKnot", (828, 938, 105, 315))
        art("TitlePlaque", "B_TitlePlaque", (192, 18, 509, 112))
        art("CounterRibbon", "B_CounterRibbon", (238, 130, 410, 68))
        art("MovieSlot", "MovieFramePreview", (160, 223, 566, 938), "video")
        art("MovieFrame", "B_VideoFrame", (123, 188, 639, 1004))
        art("CaptionCard", "B_CaptionCard", (35, 1197, 818, 298))
        art("SkipButton", "B_SkipButton", (713, 41, 156, 68), "button")
        text("ScreenTitle", "탈라 스토리", (224, 38, 445, 75), 58)
        text("StoryCounter", "이야기 01 / 04", (331, 146, 226, 40), 31, "#2C1808")
        text("SkipLabel", "건너뛰기", (725, 54, 132, 43), 27)
        text("SceneTitle", "훔친 신발", (117, 1225, 650, 86), 63, "#281506")
        text("Caption", "신단의 신발을 물고,\n상어는 항구를 빠져나갔다.", (87, 1344, 712, 107), 37, "#281506")
        for index in range(4):
            art(f"StorySegment{index + 1}", "ProgressOn" if index == 0 else "ProgressOff", (204 + index * 123, 1515, 108, 33))
        art("ReplayButton", "B_ReplayButton", (36, 1576, 334, 139), "button")
        art("NextButton", "B_NextButton", (390, 1576, 463, 139), "button")
        text("ReplayLabel", "다시 보기", (73, 1609, 260, 80), 43)
        text("NextLabel", "다음 장면", (429, 1606, 386, 80), 48, "#2C1808")
    return layers


def assemble(variant):
    layers = layers_for(variant)
    psd = PSDImage.new(mode="RGB", size=SIZE)
    composite = Image.new("RGBA", SIZE)
    overlay = Image.new("RGBA", SIZE)
    for layer in layers:
        x, y, width, height = layer["rect"]
        if layer["kind"] == "text":
            image = Image.new("RGBA", (width, height))
            font = ImageFont.truetype(str(FONT), layer["fontSize"])
            draw = ImageDraw.Draw(image)
            center = layer["align"] == "center"
            draw.multiline_text((width / 2 if center else 3, height / 2), layer["text"], font=font,
                                fill=layer["color"], anchor="mm" if center else "lm",
                                align="center" if center else "left", spacing=10,
                                stroke_width=layer["outline"], stroke_fill="#1F150B")
        elif layer["kind"] == "video":
            image = Image.new("RGBA", (width, height), "#08121A")
            frame = Image.open(ART / "MovieFramePreview.png").convert("RGBA")
            scale = min(width / frame.width, height / frame.height)
            frame = frame.resize((round(frame.width * scale), round(frame.height * scale)), Image.Resampling.LANCZOS)
            image.alpha_composite(frame, ((width - frame.width) // 2, (height - frame.height) // 2))
            layer["aspect"] = frame.width / frame.height
        else:
            image = stretch(layer["asset"], (width, height))
            if layer["kind"] == "progress":
                image.paste((0, 0, 0, 0), (round(width * layer["fill"]), 0, width, height))
        psd.create_pixel_layer(image, name=layer["name"], left=x, top=y)
        composite.alpha_composite(image, (x, y))
        if layer["kind"] != "video":
            overlay.alpha_composite(image, (x, y))
    # The reusable overlay must have an aperture through its wood backing too.
    slot = next(layer for layer in layers if layer["kind"] == "video")
    x, y, width, height = slot["rect"]
    aperture = Image.new("L", SIZE)
    ImageDraw.Draw(aperture).rectangle((x + 2, y + 2, x + width - 3, y + height - 3), fill=255)
    # Punch only the backing, then restore all foreground layers. This retains
    # rounded frame corners and A's independent subtitle gradient inside the slot.
    clean = Image.new("RGBA", SIZE)
    for layer in psd:
        if layer.name not in ("WoodBacking", "MovieSlot"):
            clean.alpha_composite(layer.topil().convert("RGBA"), (layer.left, layer.top))
    backing = Image.open(ART / "WoodBackground.png").convert("RGBA").resize(SIZE)
    backing.putalpha(ImageChops.invert(aperture))
    overlay = Image.alpha_composite(backing, clean)
    composite.save(PREVIEW / f"{variant}-reassembled.png")
    overlay.save(PREVIEW / f"{variant}-overlay.png")
    (WORK / f"layout-{variant}.json").write_text(json.dumps({"variant": variant, "width": SIZE[0], "height": SIZE[1], "layers": layers}, ensure_ascii=False, indent=2), encoding="utf-8")
    path = WORK / "layered" / f"video-ui-{variant}.psd"
    psd.save(path)
    reread = PSDImage.open(path)
    assert len(reread) == len(layers)
    return {"variant": variant, "psd_layers": len(reread), "video_layer": "MovieSlot", "text_layers": sum(layer["kind"] == "text" for layer in layers)}


def main():
    ART.mkdir(parents=True, exist_ok=True)
    PREVIEW.mkdir(parents=True, exist_ok=True)
    (WORK / "layered").mkdir(exist_ok=True)
    for name in ("cinema-clean-source.png", "journal-clean-source.png"):
        assert Image.open(SOURCE / name).size == SIZE, name
    save_asset("WoodBackground", Image.open(SOURCE / "wood-background.png"), source="wood-background.png", method="reconstructed blank background")
    crops = [
        ("cinema-clean-source.png", "A_Header", (0, 0, 620, 106), (36, 15, 15, 15), 0, False),
        ("cinema-clean-source.png", "A_HeaderEndcap", (847, 0, 887, 106), (0, 0, 0, 0), 0, False),
        ("cinema-clean-source.png", "A_VideoFrame", (0, 102, 887, 1548), (29, 32, 29, 32), 13, True),
        ("cinema-clean-source.png", "A_SkipButton", (623, 16, 848, 101), (25, 22, 25, 22), 14, False),
        ("cinema-clean-source.png", "A_ReplayButton", (35, 1607, 307, 1726), (31, 29, 31, 29), 18, False),
        ("cinema-clean-source.png", "A_NextButton", (331, 1605, 852, 1727), (32, 30, 32, 30), 18, False),
        ("journal-clean-source.png", "B_TitlePlaque", (192, 18, 701, 130), (45, 31, 45, 31), 17, False),
        ("journal-clean-source.png", "B_CounterRibbon", (238, 130, 648, 198), (105, 16, 105, 16), 7, False),
        ("journal-clean-source.png", "B_VideoFrame", (123, 188, 762, 1192), (56, 56, 56, 56), 29, True),
        ("journal-clean-source.png", "B_SkipButton", (713, 41, 869, 109), (17, 16, 17, 16), 10, False),
        ("journal-clean-source.png", "B_CaptionCard", (35, 1197, 853, 1495), (0, 0, 0, 0), 22, False),
        ("journal-clean-source.png", "B_ReplayButton", (36, 1576, 370, 1715), (33, 31, 33, 31), 19, False),
        ("journal-clean-source.png", "B_NextButton", (390, 1576, 853, 1715), (33, 31, 33, 31), 19, False),
        ("journal-clean-source.png", "ProgressOff", (326, 1514, 435, 1547), (14, 14, 14, 14), 10, False),
        ("journal-clean-source.png", "ProgressOn", (202, 1514, 312, 1547), (14, 14, 14, 14), 10, False),
    ]
    for args in crops:
        crop(*args)
    # A Filled Image stretches its sprite, ignoring nine-slice borders. A straight
    # center strip avoids enlarging rounded segment caps across the long A track.
    fill = Image.open(ART / "ProgressOn.png").crop((40, 0, 56, 33))
    save_asset("ProgressFill", fill, source="ProgressOn.png", box=[40, 0, 56, 33], method="straight center strip for native horizontal fill")
    decor = Image.open(SOURCE / "decor-magenta.png")
    assert decor.size == (1536, 1024)
    for name, box in [("LanternRope", (145, 0, 389, 559)), ("ShipMap", (607, 20, 925, 580)),
                      ("CoastMap", (1131, 15, 1448, 582)), ("RopeKnot", (145, 607, 370, 976)),
                      ("CaptionDivider", (489, 744, 1048, 813)), ("ChevronOrnament", (1204, 635, 1414, 929))]:
        cut = key_warm_art(decor.crop(box))
        bounds = cut.getchannel("A").getbbox()
        cut = cut.crop(bounds)
        padded = Image.new("RGBA", (cut.width + 8, cut.height + 8)); padded.paste(cut, (4, 4))
        save_asset(name, padded, source="decor-magenta.png", box=box, method="reconstructed ornament and key")
    gradient = Image.new("RGBA", (32, 256))
    for y in range(256):
        ImageDraw.Draw(gradient).line((0, y, 31, y), fill=(5, 15, 20, int(205 * (y / 255) ** 0.8)))
    save_asset("SubtitleGradient", gradient, method="procedural UI alpha gradient")
    arrow = Image.new("RGBA", (32, 48))
    ImageDraw.Draw(arrow).line([(7, 5), (25, 24), (7, 43)], fill="#2C1808", width=8)
    save_asset("ChevronInk", arrow, method="procedural simple UI chevron")
    save_asset("MovieFramePreview", Image.open(SOURCE / "movie-frame-2s.png"), source="movie-frame-2s.png", method="actual installed movie frame at 2 seconds, preview only")
    (WORK / "sprite-manifest.json").write_text(json.dumps({"sprites": entries}, indent=2, ensure_ascii=False), encoding="utf-8")
    checks = [assemble("A"), assemble("B")]
    # Full cleaned shells are retained only as fixed-layout references, not the native prefab art.
    for variant, file in [("A", "cinema-clean-source.png"), ("B", "journal-clean-source.png")]:
        key_warm_art(Image.open(SOURCE / file)).save(PREVIEW / f"{variant}-clean-reference-shell.png")
    sheet = Image.new("RGB", (1200, math.ceil(len(entries) / 3) * 230), "#28211A")
    draw = ImageDraw.Draw(sheet)
    for index, entry in enumerate(entries):
        x, y = (index % 3) * 400, (index // 3) * 230
        draw.text((x + 8, y + 7), entry["name"], fill="white")
        sprite = Image.open(ART / entry["file"]).convert("RGBA")
        sprite.thumbnail((174, 172), Image.Resampling.LANCZOS)
        for part, color in enumerate(["#FFF4DE", "#17485A"]):
            tile = Image.new("RGBA", (190, 190), color)
            tile.alpha_composite(sprite, ((190 - sprite.width) // 2, (190 - sprite.height) // 2))
            sheet.paste(tile.convert("RGB"), (x + 5 + part * 197, y + 30))
    sheet.save(PREVIEW / "all-assets-alpha-review.png")
    for name in ("A_VideoFrame", "B_VideoFrame"):
        image = Image.open(ART / f"{name}.png")
        assert image.getpixel((image.width // 2, image.height // 2))[3] == 0
    (WORK / "verification.json").write_text(json.dumps({"sprites": len(entries), "layered_documents": checks, "transparent_video_frame_centers": True}, indent=2), encoding="utf-8")
    print(json.dumps({"sprites": len(entries), "psd": checks, "previews": str(PREVIEW)}))


if __name__ == "__main__":
    import argparse
    parser = argparse.ArgumentParser()
    parser.add_argument("--layout-only", choices=["A", "B"])
    args = parser.parse_args()
    if args.layout_only:
        entries.extend(json.loads((WORK / "sprite-manifest.json").read_text(encoding="utf-8"))["sprites"])
        report = assemble(args.layout_only)
        (WORK / f"layout-verification-{args.layout_only}.json").write_text(json.dumps(report, indent=2), encoding="utf-8")
        print(json.dumps(report))
    else:
        main()
