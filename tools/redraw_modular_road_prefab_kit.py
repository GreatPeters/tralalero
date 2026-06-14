from __future__ import annotations

import math
import random
import re
from pathlib import Path

from PIL import Image, ImageChops, ImageDraw, ImageFilter


ROOT = Path(__file__).resolve().parents[1]
IMAGE_DIR = ROOT / "output" / "meshy_images"

WIDTH = 1536
HEIGHT = 1024
SIZE = (WIDTH, HEIGHT)
BACKGROUND = (248, 248, 246, 255)

STAGE_ROAD_PATTERN = re.compile(
    r"^(?P<seq>\d{3})_(?P<stage>STAGE\d+_[A-Z]+)_ROAD_(?P<asset>\d{3})_(?P<name>.+)\.png$"
)

ACTIVE_STAGE_ROADS = {
    "STAGE01_NRY": {"038", "039", "040", "041", "042"},
    "STAGE02_HWY": {"032", "033", "034", "035", "036"},
    "STAGE03_RST": {"024", "025", "026", "027", "028"},
    "STAGE04_CITY": {"030", "031", "032", "033", "034"},
    "STAGE05_GNG": {"031", "032", "033", "034", "035"},
}

COMMON_TARGETS = {
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

ROAD_WIDTH = 370
HALF_WIDTH = ROAD_WIDTH // 2

STYLE = {
    "STAGE01_NRY": {
        "top": (126, 74, 38),
        "top_light": (182, 113, 57),
        "top_dark": (58, 33, 19),
        "side": (64, 38, 24),
        "rim": (99, 58, 34),
        "seam": (45, 26, 16),
        "line": None,
        "curb": (94, 58, 34),
        "accent": (41, 105, 165),
    },
    "STAGE02_HWY": {
        "top": (55, 58, 59),
        "top_light": (96, 99, 98),
        "top_dark": (30, 33, 35),
        "side": (57, 59, 60),
        "rim": (105, 109, 108),
        "seam": (40, 42, 43),
        "line": (235, 235, 225),
        "curb": (128, 132, 130),
        "accent": (225, 178, 38),
    },
    "STAGE03_RST": {
        "top": (100, 111, 106),
        "top_light": (145, 157, 149),
        "top_dark": (72, 83, 78),
        "side": (82, 91, 86),
        "rim": (122, 133, 125),
        "seam": (82, 93, 88),
        "line": None,
        "curb": (137, 145, 136),
        "accent": (158, 169, 160),
    },
    "STAGE04_CITY": {
        "top": (61, 67, 73),
        "top_light": (107, 115, 121),
        "top_dark": (36, 42, 49),
        "side": (72, 78, 84),
        "rim": (128, 134, 138),
        "seam": (45, 52, 58),
        "line": (232, 232, 222),
        "curb": (154, 160, 161),
        "accent": (225, 182, 39),
    },
    "STAGE05_GNG": {
        "top": (33, 35, 42),
        "top_light": (82, 84, 92),
        "top_dark": (18, 20, 27),
        "side": (42, 44, 51),
        "rim": (88, 90, 96),
        "seam": (25, 27, 33),
        "line": (224, 224, 214),
        "curb": (118, 119, 126),
        "accent": (210, 170, 55),
    },
    "COMMON": {
        "top": (48, 52, 54),
        "top_light": (94, 99, 100),
        "top_dark": (27, 31, 34),
        "side": (55, 58, 62),
        "rim": (92, 98, 100),
        "seam": (35, 39, 42),
        "line": (235, 235, 225),
        "curb": (126, 131, 130),
        "accent": (231, 184, 41),
    },
}


def rgba(color: tuple[int, int, int], alpha: int = 255) -> tuple[int, int, int, int]:
    return (color[0], color[1], color[2], alpha)


def blend(a: tuple[int, int, int], b: tuple[int, int, int], t: float) -> tuple[int, int, int]:
    return tuple(int(a[index] + (b[index] - a[index]) * t) for index in range(3))


def draw_polyline(draw: ImageDraw.ImageDraw, points: list[tuple[int, int]], fill, width: int) -> None:
    draw.line(points, fill=fill, width=width, joint="curve")


def path_mask(center_paths: list[list[tuple[int, int]]], width: int = ROAD_WIDTH) -> Image.Image:
    mask = Image.new("L", SIZE, 0)
    draw = ImageDraw.Draw(mask)
    for points in center_paths:
        draw_polyline(draw, points, 255, width)
    return mask


def polygon_mask(points: list[tuple[int, int]]) -> Image.Image:
    mask = Image.new("L", SIZE, 0)
    ImageDraw.Draw(mask).polygon(points, fill=255)
    return mask


def add_masks(*masks: Image.Image) -> Image.Image:
    out = Image.new("L", SIZE, 0)
    for mask in masks:
        out = ImageChops.lighter(out, mask)
    return out


def clipped(image: Image.Image, mask: Image.Image, draw_fn) -> None:
    layer = Image.new("RGBA", SIZE, (0, 0, 0, 0))
    draw_fn(ImageDraw.Draw(layer, "RGBA"))
    alpha = ImageChops.multiply(layer.getchannel("A"), mask)
    layer.putalpha(alpha)
    image.alpha_composite(layer)


def shift_mask(mask: Image.Image, dx: int, dy: int) -> Image.Image:
    shifted = Image.new("L", SIZE, 0)
    shifted.paste(mask, (dx, dy))
    return shifted


def sample_polyline(points: list[tuple[int, int]], step: int) -> list[tuple[float, float]]:
    samples: list[tuple[float, float]] = []
    for (x1, y1), (x2, y2) in zip(points, points[1:]):
        length = max(1.0, math.hypot(x2 - x1, y2 - y1))
        count = max(1, int(length // step))
        for index in range(count):
            t = index / count
            samples.append((x1 + (x2 - x1) * t, y1 + (y2 - y1) * t))
    samples.append(points[-1])
    return samples


def offset_sampled_polyline(points: list[tuple[int, int]], offset: float, step: int = 20) -> list[tuple[int, int]]:
    samples = sample_polyline(points, step)
    shifted: list[tuple[int, int]] = []
    for index, (x, y) in enumerate(samples):
        prev_x, prev_y = samples[max(0, index - 1)]
        next_x, next_y = samples[min(len(samples) - 1, index + 1)]
        dx = next_x - prev_x
        dy = next_y - prev_y
        length = math.hypot(dx, dy) or 1.0
        nx = -dy / length
        ny = dx / length
        shifted.append((int(x + nx * offset), int(y + ny * offset)))
    return shifted


def shape_from_name(name: str) -> str:
    lower = name.lower()
    if "split" in lower:
        return "t"
    if "left" in lower:
        return "corner_left"
    if "right" in lower:
        return "corner_right"
    if "narrow" in lower:
        return "narrow"
    return "straight"


def common_shape_from_name(name: str) -> str:
    lower = name.lower()
    if "jump_ramp" in lower:
        return "jump"
    if "bridge" in lower:
        return "bridge"
    if "left" in lower:
        return "corner_left"
    if "right" in lower:
        return "corner_right"
    if "narrow" in lower:
        return "narrow"
    if "obstacle" in lower:
        return "obstacle"
    if "side_background" in lower:
        return "side_background"
    if "swerve" in lower:
        return "swerve_coin"
    if "coin" in lower:
        return "jump_coin"
    if "split" in lower:
        return "t"
    if "slope" in lower:
        return "slope"
    if "underpass" in lower:
        return "underpass"
    return "straight"


def shape_paths(shape: str) -> tuple[Image.Image, list[list[tuple[int, int]]]]:
    if shape == "straight":
        paths = [[(768, 860), (768, 155)]]
        return path_mask(paths), paths
    if shape == "narrow":
        polygon = [(520, 860), (1016, 860), (932, 150), (604, 150)]
        return polygon_mask(polygon), [[(768, 860), (768, 150)]]
    if shape == "corner_left":
        paths = [[(1010, 850), (1010, 595), (905, 455), (740, 395), (390, 395)]]
        return path_mask(paths), paths
    if shape == "corner_right":
        paths = [[(526, 850), (526, 595), (631, 455), (796, 395), (1146, 395)]]
        return path_mask(paths), paths
    if shape == "t":
        paths = [[(768, 870), (768, 505)], [(388, 505), (1148, 505)]]
        mask = path_mask(paths)
        ImageDraw.Draw(mask).ellipse((588, 325, 948, 685), fill=255)
        return mask, paths
    if shape == "jump":
        polygon = [(548, 850), (988, 850), (932, 160), (604, 160)]
        return polygon_mask(polygon), [[(768, 850), (768, 160)]]
    if shape == "slope":
        polygon = [(500, 860), (1045, 770), (995, 130), (558, 210)]
        return polygon_mask(polygon), [[(770, 815), (776, 170)]]
    return shape_paths("straight")


def draw_base(image: Image.Image, mask: Image.Image, style: dict[str, tuple[int, int, int]], stage: str) -> None:
    outer = mask.filter(ImageFilter.MaxFilter(29 if stage == "STAGE01_NRY" else 23))
    side = shift_mask(outer, 0, 44 if stage == "STAGE01_NRY" else 35)
    shadow = shift_mask(outer, 26, 56).filter(ImageFilter.GaussianBlur(24))

    image.paste(Image.new("RGBA", SIZE, (0, 0, 0, 58 if stage == "STAGE01_NRY" else 46)), (0, 0), shadow)
    image.paste(Image.new("RGBA", SIZE, rgba(style["side"])), (0, 0), side)
    image.paste(Image.new("RGBA", SIZE, rgba(style["rim"])), (0, 0), outer)

    gradient = Image.new("RGBA", SIZE, (0, 0, 0, 0))
    pixels = gradient.load()
    for y in range(HEIGHT):
        vertical = y / (HEIGHT - 1)
        for x in range(WIDTH):
            diagonal = (x / (WIDTH - 1)) * 0.22 + (1.0 - vertical) * 0.78
            color = blend(style["top_dark"], style["top_light"], max(0.0, min(1.0, diagonal)))
            pixels[x, y] = rgba(color)
    gradient.putalpha(mask)
    image.alpha_composite(gradient)

    highlight = shift_mask(mask.filter(ImageFilter.FIND_EDGES).filter(ImageFilter.GaussianBlur(1.1)), -5, -7)
    image.paste(Image.new("RGBA", SIZE, (255, 255, 255, 17)), (0, 0), highlight)


def draw_wood_planks(draw: ImageDraw.ImageDraw, rng: random.Random, style: dict[str, tuple[int, int, int]]) -> None:
    for y in range(145, 885, 42):
        jitter = rng.randint(-5, 5)
        draw.line([(350, y + jitter), (1188, y + jitter + rng.randint(-4, 4))], fill=rgba(style["seam"], 112), width=3)
        draw.line([(370, y + jitter - 4), (1168, y + jitter - 4)], fill=(235, 164, 88, 22), width=2)
    for x in range(540, 1020, 84):
        draw.line([(x + rng.randint(-5, 5), 150), (x + rng.randint(-8, 8), 870)], fill=rgba(style["seam"], 42), width=2)
    for _ in range(190):
        x = rng.randint(360, 1145)
        y = rng.randint(145, 880)
        length = rng.randint(45, 185)
        color = blend(style["top_dark"], style["top_light"], rng.random() * 0.55)
        draw.line([(x, y), (x + length, y + rng.randint(-6, 6))], fill=rgba(color, rng.randint(48, 118)), width=rng.randint(1, 3))
    for _ in range(52):
        x = rng.randint(385, 1120)
        y = rng.randint(160, 860)
        w = rng.randint(55, 155)
        h = rng.randint(11, 31)
        draw.ellipse((x, y, x + w, y + h), fill=rgba(style["accent"], rng.randint(58, 116)))
        draw.arc((x + 4, y + 3, x + w - 4, y + h - 3), 190, 350, fill=(225, 246, 255, 74), width=2)


def draw_rivet(draw: ImageDraw.ImageDraw, x: int, y: int, radius: int = 6) -> None:
    draw.ellipse((x - radius, y - radius, x + radius, y + radius), fill=(29, 25, 22, 180))
    draw.ellipse((x - radius + 2, y - radius + 2, x + radius - 2, y + radius - 2), fill=(131, 112, 88, 215))
    draw.ellipse((x - radius + 3, y - radius + 2, x - radius + 7, y - radius + 6), fill=(245, 218, 172, 90))


def draw_post(draw: ImageDraw.ImageDraw, x: int, y: int, style: dict[str, tuple[int, int, int]], rng: random.Random) -> None:
    w = rng.randint(24, 30)
    h = rng.randint(45, 58)
    draw.ellipse((x - w // 2, y + h - 10, x + w // 2, y + h + 7), fill=(34, 21, 13, 116))
    draw.rectangle((x - w // 2, y - h, x + w // 2, y + h // 2), fill=rgba(style["side"], 235))
    draw.rectangle((x - w // 2 + 5, y - h + 5, x + w // 2 - 5, y + h // 2 - 4), fill=rgba(style["top"], 235))
    draw.ellipse((x - w // 2, y - h - 8, x + w // 2, y - h + 10), fill=rgba(style["top_light"], 240))


def draw_edge_hardware(draw: ImageDraw.ImageDraw, center_paths: list[list[tuple[int, int]]], style: dict[str, tuple[int, int, int]], stage: str, rng: random.Random) -> None:
    for center in center_paths:
        for side in (-1, 1):
            edge = offset_sampled_polyline(center, side * (HALF_WIDTH - 24), step=28)
            inner = offset_sampled_polyline(center, side * (HALF_WIDTH - 63), step=28)
            if len(edge) < 2:
                continue
            if stage == "STAGE01_NRY":
                draw.line(edge, fill=(47, 28, 17, 230), width=16, joint="curve")
                draw.line(inner, fill=(48, 43, 39, 180), width=10, joint="curve")
                for index, (x, y) in enumerate(edge[::5]):
                    draw_post(draw, x, y, style, rng)
                for x, y in inner[::3]:
                    draw_rivet(draw, x, y, rng.randint(4, 6))
            else:
                draw.line(edge, fill=rgba(style["curb"], 230), width=20, joint="curve")
                draw.line(edge, fill=(255, 255, 255, 30), width=4, joint="curve")
                if stage == "STAGE02_HWY":
                    rail = offset_sampled_polyline(center, side * (HALF_WIDTH + 15), step=28)
                    draw.line(rail, fill=(176, 181, 178, 210), width=9, joint="curve")
                    for x, y in rail[::6]:
                        draw.rectangle((x - 6, y - 14, x + 6, y + 14), fill=(117, 122, 121, 210))


def draw_asphalt(draw: ImageDraw.ImageDraw, rng: random.Random, style: dict[str, tuple[int, int, int]], stage: str) -> None:
    for _ in range(520 if stage != "STAGE03_RST" else 280):
        x = rng.randint(320, 1210)
        y = rng.randint(125, 895)
        size = rng.randint(1, 4)
        color = rng.choice([style["top_dark"], style["top_light"], style["top"], style["seam"]])
        draw.ellipse((x, y, x + size, y + size), fill=rgba(color, rng.randint(26, 78)))
    for _ in range(26):
        x = rng.randint(360, 1120)
        y = rng.randint(150, 870)
        w = rng.randint(50, 170)
        h = rng.randint(8, 25)
        draw.ellipse((x, y, x + w, y + h), fill=rgba(style["top_light"], rng.randint(12, 34)))
    if stage in {"STAGE04_CITY", "STAGE05_GNG"}:
        for _ in range(24):
            x = rng.randint(380, 1100)
            y = rng.randint(150, 860)
            draw.line([(x, y), (x + rng.randint(70, 230), y - rng.randint(8, 30))], fill=(255, 255, 255, rng.randint(15, 42)), width=rng.randint(2, 5))


def draw_dashed_path(draw: ImageDraw.ImageDraw, points: list[tuple[int, int]], color: tuple[int, int, int], width: int, dash: int, gap: int, alpha: int = 225) -> None:
    samples = sample_polyline(points, 10)
    if len(samples) < 2:
        return
    distance = 0.0
    drawing = True
    segment_start = samples[0]
    remaining = dash
    for start, end in zip(samples, samples[1:]):
        sx, sy = start
        ex, ey = end
        seg_len = math.hypot(ex - sx, ey - sy)
        if seg_len <= 0:
            continue
        consumed = 0.0
        while consumed < seg_len:
            take = min(remaining, seg_len - consumed)
            t0 = consumed / seg_len
            t1 = (consumed + take) / seg_len
            p0 = (int(sx + (ex - sx) * t0), int(sy + (ey - sy) * t0))
            p1 = (int(sx + (ex - sx) * t1), int(sy + (ey - sy) * t1))
            if drawing:
                draw.line([p0, p1], fill=rgba(color, alpha), width=width)
            consumed += take
            remaining -= take
            if remaining <= 0:
                drawing = not drawing
                remaining = dash if drawing else gap
        distance += seg_len


def draw_lane_markings(draw: ImageDraw.ImageDraw, center_paths: list[list[tuple[int, int]]], style: dict[str, tuple[int, int, int]], stage: str) -> None:
    if stage == "STAGE03_RST":
        for center in center_paths:
            for offset in (-140, 140):
                draw.line(offset_sampled_polyline(center, offset, step=18), fill=rgba(style["curb"], 105), width=5, joint="curve")
        return

    line = style["line"] or (230, 230, 220)
    for center in center_paths:
        for offset in (-62, 62):
            draw_dashed_path(draw, offset_sampled_polyline(center, offset, step=18), line, 9, 54, 52, 225)
        for offset in (-151, 151):
            draw.line(offset_sampled_polyline(center, offset, step=18), fill=rgba(line, 140), width=5, joint="curve")
        if stage in {"STAGE02_HWY", "STAGE05_GNG", "COMMON"}:
            draw.line(center, fill=rgba(style["accent"], 130), width=4, joint="curve")

    if stage == "STAGE04_CITY":
        for y in (250, 745):
            for index in range(8):
                x0 = 560 + index * 55
                draw.rectangle((x0, y, x0 + 28, y + 110), fill=(238, 238, 230, 135))


def draw_stage_surface(image: Image.Image, mask: Image.Image, center_paths: list[list[tuple[int, int]]], style: dict[str, tuple[int, int, int]], stage: str, shape: str, rng: random.Random) -> None:
    def details(draw: ImageDraw.ImageDraw) -> None:
        if stage == "STAGE01_NRY":
            draw_wood_planks(draw, rng, style)
        else:
            draw_asphalt(draw, rng, style, stage)
            draw_lane_markings(draw, center_paths, style, stage)
        draw_edge_hardware(draw, center_paths, style, stage, rng)
        if shape == "t" and stage != "STAGE01_NRY":
            draw.polygon([(740, 526), (796, 526), (796, 585), (846, 585), (768, 675), (690, 585), (740, 585)], fill=rgba(style["line"] or style["accent"], 160))

    clipped(image, mask, details)


def draw_coin(draw: ImageDraw.ImageDraw, x: int, y: int, radius: int) -> None:
    draw.ellipse((x - radius, y + radius // 2, x + radius, y + radius * 2), fill=(145, 91, 0, 70))
    draw.ellipse((x - radius, y - radius, x + radius, y + radius), fill=(255, 183, 30, 255))
    draw.ellipse((x - radius + 5, y - radius + 5, x + radius - 5, y + radius - 5), fill=(255, 215, 64, 255))
    draw.ellipse((x - radius + 11, y - radius + 11, x + radius - 11, y + radius - 11), outline=(182, 107, 0, 175), width=3)


def draw_extra_common_details(image: Image.Image, mask: Image.Image, center_paths: list[list[tuple[int, int]]], shape: str, rng: random.Random) -> None:
    def details(draw: ImageDraw.ImageDraw) -> None:
        if shape == "jump":
            for y in range(335, 690, 88):
                draw.polygon([(690, y), (768, y - 45), (846, y), (821, y + 25), (768, y - 5), (715, y + 25)], fill=(238, 190, 36, 215))
        elif shape == "bridge":
            for y in range(200, 850, 108):
                draw.rectangle((538, y, 998, y + 13), fill=(75, 80, 82, 92))
        elif shape == "obstacle":
            for x, y in [(610, 365), (924, 430), (770, 610), (618, 745), (920, 755)]:
                draw.rounded_rectangle((x - 40, y - 27, x + 40, y + 27), radius=7, fill=(43, 47, 48, 230), outline=(174, 176, 168, 170), width=3)
            for x, y in [(620, 520), (910, 585)]:
                draw.polygon([(x, y - 43), (x - 33, y + 41), (x + 33, y + 41)], fill=(225, 91, 24, 235))
                draw.rectangle((x - 23, y + 11, x + 23, y + 25), fill=(245, 236, 212, 220))
        elif shape == "jump_coin":
            for index, (x, y) in enumerate([(575, 720), (675, 610), (768, 520), (860, 430), (960, 330)]):
                draw_coin(draw, x, y - int(math.sin(index / 4 * math.pi) * 58), 31)
        elif shape == "swerve_coin":
            for x, y in [(590, 720), (700, 610), (830, 515), (920, 420), (1000, 305)]:
                draw_coin(draw, x, y, 31)

    clipped(image, mask, details)


def draw_side_background(output: Path) -> None:
    image = Image.new("RGBA", SIZE, BACKGROUND)
    draw = ImageDraw.Draw(image, "RGBA")
    shadow = Image.new("L", SIZE, 0)
    ImageDraw.Draw(shadow).rounded_rectangle((290, 180, 1246, 820), radius=22, fill=90)
    image.paste(Image.new("RGBA", SIZE, (0, 0, 0, 42)), (0, 0), shadow.filter(ImageFilter.GaussianBlur(19)))
    draw.rounded_rectangle((302, 168, 1234, 786), radius=18, fill=(62, 69, 70, 255), outline=(128, 135, 132, 230), width=7)
    draw.rectangle((520, 175, 1016, 785), fill=(45, 50, 52, 255))
    for x in (400, 1136):
        for y in (280, 470, 650):
            draw.rounded_rectangle((x - 72, y - 43, x + 72, y + 43), radius=8, fill=(35, 111, 152, 255), outline=(186, 196, 188, 180), width=4)
            draw.rectangle((x - 52, y - 27, x + 52, y + 27), fill=(26, 81, 116, 255))
    for x in (500, 1036):
        draw.line([(x, 180), (x, 784)], fill=(232, 183, 41, 125), width=7)
    image.convert("RGB").save(output, quality=95)


def draw_underpass(output: Path) -> None:
    style = STYLE["COMMON"]
    image = Image.new("RGBA", SIZE, BACKGROUND)
    mask, paths = shape_paths("straight")
    draw_base(image, mask, style, "COMMON")
    rng = random.Random("modular-underpass-v2")
    draw_stage_surface(image, mask, paths, style, "COMMON", "straight", rng)
    draw = ImageDraw.Draw(image, "RGBA")
    draw.rounded_rectangle((455, 210, 1081, 470), radius=18, fill=(86, 89, 89, 255), outline=(146, 151, 147, 230), width=7)
    draw.rectangle((502, 325, 1034, 470), fill=(31, 35, 36, 255))
    for x in range(515, 1000, 78):
        draw.polygon([(x, 240), (x + 42, 240), (x + 20, 300), (x - 22, 300)], fill=(229, 179, 34, 230))
    image.convert("RGB").save(output, quality=95)


def draw_prefab_road(output: Path, stage: str, shape: str, seed_name: str) -> None:
    style = STYLE[stage]
    mask, center_paths = shape_paths(shape)
    rng = random.Random(f"{stage}:{shape}:{seed_name}:modular-v2")
    image = Image.new("RGBA", SIZE, BACKGROUND)
    draw_base(image, mask, style, stage)
    draw_stage_surface(image, mask, center_paths, style, stage, shape, rng)
    image = image.filter(ImageFilter.UnsharpMask(radius=1.0, percent=95, threshold=3))
    output.parent.mkdir(parents=True, exist_ok=True)
    image.convert("RGB").save(output, quality=95)


def draw_common(output: Path) -> None:
    shape = common_shape_from_name(output.name)
    if shape == "side_background":
        draw_side_background(output)
        return
    if shape == "underpass":
        draw_underpass(output)
        return
    style = STYLE["COMMON"]
    base_shape = "straight" if shape in {"bridge", "obstacle", "jump_coin", "swerve_coin"} else shape
    mask, paths = shape_paths(base_shape)
    rng = random.Random(f"COMMON:{shape}:{output.name}:modular-v2")
    image = Image.new("RGBA", SIZE, BACKGROUND)
    draw_base(image, mask, style, "COMMON")
    draw_stage_surface(image, mask, paths, style, "COMMON", base_shape, rng)
    draw_extra_common_details(image, mask, paths, shape, rng)
    image = image.filter(ImageFilter.UnsharpMask(radius=1.0, percent=95, threshold=3))
    output.convert if False else None
    image.convert("RGB").save(output, quality=95)


def active_stage_road_paths() -> list[Path]:
    paths: list[Path] = []
    for path in IMAGE_DIR.glob("*.png"):
        match = STAGE_ROAD_PATTERN.match(path.name)
        if not match:
            continue
        if match.group("asset") in ACTIVE_STAGE_ROADS.get(match.group("stage"), set()):
            paths.append(path)
    return sorted(paths, key=lambda item: int(item.name[:3]))


def target_paths() -> list[Path]:
    paths = active_stage_road_paths()
    paths.extend(IMAGE_DIR / name for name in sorted(COMMON_TARGETS, key=lambda item: int(item[:3])) if (IMAGE_DIR / name).exists())
    return sorted(paths, key=lambda item: int(item.name[:3]))


def redraw(path: Path) -> None:
    match = STAGE_ROAD_PATTERN.match(path.name)
    if match:
        draw_prefab_road(path, match.group("stage"), shape_from_name(match.group("name")), match.group("name"))
        return
    draw_common(path)


def main() -> None:
    generated: list[str] = []
    for path in target_paths():
        redraw(path)
        generated.append(path.name)
    for name in generated:
        print(name)
    print(f"Generated {len(generated)} modular road prefab image(s).")


if __name__ == "__main__":
    main()
