from __future__ import annotations

import math
import random
from pathlib import Path

from PIL import Image, ImageChops, ImageDraw, ImageFilter

from redraw_active_stage_roads import (
    BACKGROUND,
    IMAGE_DIR,
    SIZE,
    STYLE,
    clipped,
    draw_asphalt_texture,
    draw_base,
    draw_lane_markings,
    draw_side_curbs,
    offset_points,
    path_mask,
    polygon_mask,
    rgba,
    shape_mask,
)


COMMON_STYLE = {
    **STYLE["STAGE02_HWY"],
    "top": (49, 54, 56),
    "top_light": (91, 98, 100),
    "top_dark": (28, 32, 35),
    "side": (56, 60, 64),
    "rim": (82, 87, 88),
    "seam": (36, 40, 43),
    "line": (236, 236, 226),
    "curb": (124, 129, 128),
}

TARGETS = {
    "198_COMMON_ROAD_007_Jump_ramp.png",
    "199_COMMON_ROAD_008_Segment_transition_bridge_ramp.png",
    "209_COMMON_GAMEPLAY_001_Straight_lane_module.png",
    "210_COMMON_GAMEPLAY_002_Left_curve_lane_module.png",
    "211_COMMON_GAMEPLAY_003_Right_curve_lane_module.png",
    "212_COMMON_GAMEPLAY_004_Narrowing_lane_module.png",
    "213_COMMON_GAMEPLAY_005_Obstacle_layout_preset_module.png",
    "214_COMMON_GAMEPLAY_006_Side_background_preset_module.png",
    "220_COMMON_GAMEPLAY_011_Jump_coin_line_preset.png",
    "221_COMMON_GAMEPLAY_012_Swerve_coin_line_preset.png",
    "226_COMMON_GAMEPLAY_008_Split_lane_choice_module.png",
    "227_COMMON_GAMEPLAY_009_Slope_lane_module.png",
    "228_COMMON_GAMEPLAY_010_Underpass_pass_through_module.png",
}


def draw_coin(draw: ImageDraw.ImageDraw, x: int, y: int, radius: int) -> None:
    draw.ellipse((x - radius, y - radius, x + radius, y + radius), fill=(194, 107, 8, 80))
    draw.ellipse((x - radius, y - radius * 2, x + radius, y), fill=(255, 183, 30, 255))
    draw.ellipse((x - radius + 5, y - radius * 2 + 5, x + radius - 5, y - 5), fill=(255, 210, 58, 255))
    draw.ellipse((x - radius + 12, y - radius * 2 + 12, x + radius - 12, y - 12), outline=(190, 112, 0, 180), width=3)
    points = []
    for index in range(10):
        angle = -math.pi / 2 + index * math.pi / 5
        r = radius * (0.42 if index % 2 else 0.18)
        points.append((x + int(math.cos(angle) * r), y - radius + int(math.sin(angle) * r)))
    draw.polygon(points, fill=(191, 116, 0, 155))


def draw_common_road(name: str, output: Path) -> None:
    lower = name.lower()
    if "left" in lower:
        shape = "left"
    elif "right" in lower:
        shape = "right"
    elif "narrow" in lower:
        shape = "narrow"
    elif "split" in lower:
        shape = "split"
    else:
        shape = "straight"

    top_mask, center_paths, _ = shape_mask(shape)
    rng = random.Random(f"common-road:{name}:v1")
    image = Image.new("RGBA", SIZE, BACKGROUND)
    draw_base(image, top_mask, "STAGE02_HWY", COMMON_STYLE)

    def details(draw: ImageDraw.ImageDraw) -> None:
        draw_asphalt_texture(draw, rng, COMMON_STYLE, density=470)
        draw_side_curbs(draw, center_paths, COMMON_STYLE, "STAGE02_HWY")
        draw_lane_markings(draw, center_paths, COMMON_STYLE, "STAGE02_HWY")
        for center in center_paths:
            for dx in (-190, 190):
                draw.line(offset_points(center, dx), fill=(255, 217, 71, 150), width=5, joint="curve")

    clipped(image, top_mask, details)
    image.convert("RGB").save(output, quality=95)


def draw_jump_ramp(output: Path) -> None:
    top = polygon_mask([(510, 190), (1026, 190), (1195, 820), (341, 820)])
    rng = random.Random("common-jump-ramp-v1")
    image = Image.new("RGBA", SIZE, BACKGROUND)
    draw_base(image, top, "STAGE02_HWY", COMMON_STYLE)

    def details(draw: ImageDraw.ImageDraw) -> None:
        draw_asphalt_texture(draw, rng, COMMON_STYLE, density=360)
        draw.polygon([(512, 190), (1024, 190), (1188, 820), (348, 820)], outline=(150, 156, 154, 215))
        for y in range(355, 675, 92):
            draw.polygon([(690, y), (768, y - 48), (846, y), (820, y + 24), (768, y - 8), (716, y + 24)], fill=(242, 198, 38, 210))
        draw.line([(618, 220), (486, 802)], fill=(236, 236, 226, 180), width=7)
        draw.line([(918, 220), (1050, 802)], fill=(236, 236, 226, 180), width=7)

    clipped(image, top, details)
    image.convert("RGB").save(output, quality=95)


def draw_bridge(output: Path) -> None:
    top = polygon_mask([(460, 170), (1076, 170), (1240, 855), (296, 855)])
    center = [[(768, 840), (768, 190)]]
    rng = random.Random("common-bridge-v1")
    image = Image.new("RGBA", SIZE, BACKGROUND)
    draw_base(image, top, "STAGE02_HWY", COMMON_STYLE)

    def details(draw: ImageDraw.ImageDraw) -> None:
        draw_asphalt_texture(draw, rng, COMMON_STYLE, density=380)
        draw_lane_markings(draw, center, COMMON_STYLE, "STAGE02_HWY")
        for dx in (-214, 214):
            draw.line(offset_points(center[0], dx), fill=(108, 113, 112, 235), width=26)
            draw.line(offset_points(center[0], dx), fill=(178, 184, 180, 215), width=10)
        for y in range(205, 835, 95):
            draw.rectangle((520, y, 1016, y + 14), fill=(72, 77, 78, 92))

    clipped(image, top, details)
    image.convert("RGB").save(output, quality=95)


def draw_obstacle_layout(output: Path) -> None:
    top_mask, center_paths, _ = shape_mask("straight")
    rng = random.Random("common-obstacle-layout-v1")
    image = Image.new("RGBA", SIZE, BACKGROUND)
    draw_base(image, top_mask, "STAGE02_HWY", COMMON_STYLE)

    def details(draw: ImageDraw.ImageDraw) -> None:
        draw_asphalt_texture(draw, rng, COMMON_STYLE, density=430)
        draw_lane_markings(draw, center_paths, COMMON_STYLE, "STAGE02_HWY")
        for x, y in [(610, 360), (922, 430), (768, 610), (616, 735), (920, 760)]:
            draw.rounded_rectangle((x - 42, y - 28, x + 42, y + 28), radius=8, fill=(44, 47, 48, 220), outline=(175, 178, 170, 160), width=3)
        for x, y in [(620, 520), (910, 585)]:
            draw.polygon([(x, y - 44), (x - 34, y + 42), (x + 34, y + 42)], fill=(224, 90, 24, 235))
            draw.rectangle((x - 24, y + 12, x + 24, y + 26), fill=(245, 236, 212, 220))

    clipped(image, top_mask, details)
    image.convert("RGB").save(output, quality=95)


def draw_side_background(output: Path) -> None:
    image = Image.new("RGBA", SIZE, BACKGROUND)
    shadow = Image.new("L", SIZE, 0)
    ImageDraw.Draw(shadow).rounded_rectangle((310, 190, 1226, 815), radius=36, fill=90)
    image.paste(Image.new("RGBA", SIZE, (0, 0, 0, 48)), (0, 0), shadow.filter(ImageFilter.GaussianBlur(18)))
    draw = ImageDraw.Draw(image, "RGBA")
    draw.rounded_rectangle((300, 170, 1215, 785), radius=32, fill=(72, 82, 83, 255), outline=(136, 145, 140, 220), width=8)
    draw.rectangle((500, 175, 1030, 785), fill=(54, 58, 59, 255))
    for x in (390, 1140):
        for y in (280, 470, 650):
            draw.rounded_rectangle((x - 70, y - 45, x + 70, y + 45), radius=10, fill=(40, 110, 155, 255), outline=(188, 198, 190, 180), width=4)
            draw.rectangle((x - 52, y - 28, x + 52, y + 28), fill=(31, 83, 118, 255))
    for x in (458, 1072):
        draw.line([(x, 185), (x, 780)], fill=(255, 210, 64, 130), width=8)
    image.convert("RGB").save(output, quality=95)


def draw_coin_line(output: Path, arc: bool) -> None:
    top_mask, center_paths, _ = shape_mask("straight")
    rng = random.Random(f"common-coin-line:{arc}:v1")
    image = Image.new("RGBA", SIZE, BACKGROUND)
    draw_base(image, top_mask, "STAGE02_HWY", COMMON_STYLE)

    def details(draw: ImageDraw.ImageDraw) -> None:
        draw_asphalt_texture(draw, rng, COMMON_STYLE, density=310)
        if arc:
            points = [(520, 720), (610, 570), (720, 440), (850, 345), (1000, 290)]
        else:
            points = [(520, 760), (650, 640), (780, 560), (905, 475), (1020, 340)]
        for index, (x, y) in enumerate(points):
            lift = int(math.sin(index / max(1, len(points) - 1) * math.pi) * 70) if arc else 0
            draw_coin(draw, x, y - lift, 34)

    clipped(image, top_mask, details)
    image.convert("RGB").save(output, quality=95)


def draw_slope(output: Path) -> None:
    top = polygon_mask([(460, 250), (1080, 105), (1230, 795), (320, 915)])
    center = [[(770, 855), (780, 165)]]
    rng = random.Random("common-slope-v1")
    image = Image.new("RGBA", SIZE, BACKGROUND)
    draw_base(image, top, "STAGE02_HWY", COMMON_STYLE)

    def details(draw: ImageDraw.ImageDraw) -> None:
        draw_asphalt_texture(draw, rng, COMMON_STYLE, density=390)
        draw_lane_markings(draw, center, COMMON_STYLE, "STAGE02_HWY")
        for y in range(310, 760, 120):
            draw.line([(500, y), (1120, y - 80)], fill=(255, 210, 54, 150), width=5)

    clipped(image, top, details)
    image.convert("RGB").save(output, quality=95)


def draw_underpass(output: Path) -> None:
    image = Image.new("RGBA", SIZE, BACKGROUND)
    draw = ImageDraw.Draw(image, "RGBA")
    draw.rounded_rectangle((300, 205, 1235, 820), radius=24, fill=(61, 65, 67, 255), outline=(145, 150, 146, 220), width=8)
    draw.rectangle((410, 420, 1126, 820), fill=(45, 50, 53, 255))
    draw.rectangle((310, 205, 1225, 400), fill=(111, 113, 111, 255))
    draw.rectangle((380, 255, 1155, 335), fill=(32, 34, 35, 255))
    for x in range(410, 1110, 105):
        draw.polygon([(x, 255), (x + 46, 255), (x + 22, 335), (x - 24, 335)], fill=(238, 190, 34, 235))
    draw.line([(768, 435), (768, 800)], fill=(238, 238, 226, 220), width=9)
    for x in (610, 926):
        draw.line([(x, 435), (x, 800)], fill=(238, 238, 226, 150), width=6)
    shadow = Image.new("L", SIZE, 0)
    ImageDraw.Draw(shadow).rounded_rectangle((315, 220, 1235, 835), radius=26, fill=75)
    image.paste(Image.new("RGBA", SIZE, (0, 0, 0, 36)), (0, 0), shadow.filter(ImageFilter.GaussianBlur(16)))
    image.convert("RGB").save(output, quality=95)


def redraw(path: Path) -> None:
    name = path.name
    if name.startswith("198_"):
        draw_jump_ramp(path)
    elif name.startswith("199_"):
        draw_bridge(path)
    elif name.startswith("213_"):
        draw_obstacle_layout(path)
    elif name.startswith("214_"):
        draw_side_background(path)
    elif name.startswith("220_"):
        draw_coin_line(path, arc=True)
    elif name.startswith("221_"):
        draw_coin_line(path, arc=False)
    elif name.startswith("227_"):
        draw_slope(path)
    elif name.startswith("228_"):
        draw_underpass(path)
    else:
        draw_common_road(name, path)


def main() -> None:
    generated = []
    for name in sorted(TARGETS, key=lambda item: int(item[:3])):
        path = IMAGE_DIR / name
        if not path.exists():
            continue
        redraw(path)
        generated.append(name)

    for name in generated:
        print(name)
    print(f"Generated {len(generated)} common module image(s).")


if __name__ == "__main__":
    main()
