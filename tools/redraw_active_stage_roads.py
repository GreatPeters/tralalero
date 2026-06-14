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

ROAD_FILE_PATTERN = re.compile(
    r"^(?P<seq>\d{3})_(?P<stage>STAGE\d+_[A-Z]+)_ROAD_(?P<asset>\d{3})_(?P<name>.+)\.png$"
)

ACTIVE_ROAD_ASSETS = {
    "STAGE01_NRY": {"038", "039", "040", "041", "042"},
    "STAGE02_HWY": {"032", "033", "034", "035", "036"},
    "STAGE03_RST": {"024", "025", "026", "027", "028"},
    "STAGE04_CITY": {"030", "031", "032", "033", "034"},
    "STAGE05_GNG": {"031", "032", "033", "034", "035"},
}

STYLE = {
    "STAGE01_NRY": {
        "top": (96, 53, 29),
        "top_light": (170, 98, 48),
        "top_dark": (44, 24, 14),
        "side": (70, 43, 28),
        "rim": (92, 55, 33),
        "seam": (42, 24, 15),
        "water": (43, 105, 168),
        "line": None,
        "curb": (92, 63, 42),
    },
    "STAGE02_HWY": {
        "top": (55, 56, 55),
        "top_light": (88, 90, 90),
        "top_dark": (30, 31, 32),
        "side": (60, 62, 64),
        "rim": (86, 88, 88),
        "seam": (41, 42, 43),
        "water": (68, 70, 70),
        "line": (235, 235, 226),
        "curb": (120, 123, 121),
    },
    "STAGE03_RST": {
        "top": (102, 111, 106),
        "top_light": (146, 157, 149),
        "top_dark": (70, 80, 75),
        "side": (82, 89, 84),
        "rim": (111, 122, 114),
        "seam": (86, 96, 90),
        "water": (125, 136, 130),
        "line": None,
        "curb": (130, 138, 128),
    },
    "STAGE04_CITY": {
        "top": (62, 68, 74),
        "top_light": (101, 109, 117),
        "top_dark": (39, 44, 50),
        "side": (74, 80, 86),
        "rim": (105, 112, 118),
        "seam": (48, 55, 62),
        "water": (82, 90, 98),
        "line": (232, 232, 222),
        "curb": (151, 158, 160),
    },
    "STAGE05_GNG": {
        "top": (32, 34, 39),
        "top_light": (72, 74, 82),
        "top_dark": (18, 20, 25),
        "side": (42, 44, 50),
        "rim": (75, 77, 84),
        "seam": (25, 27, 32),
        "water": (55, 58, 66),
        "line": (224, 224, 214),
        "curb": (115, 116, 122),
    },
}


def rgba(color: tuple[int, int, int], alpha: int = 255) -> tuple[int, int, int, int]:
    return (color[0], color[1], color[2], alpha)


def blend(a: tuple[int, int, int], b: tuple[int, int, int], t: float) -> tuple[int, int, int]:
    return tuple(int(a[i] + (b[i] - a[i]) * t) for i in range(3))


def draw_line(draw: ImageDraw.ImageDraw, points: list[tuple[int, int]], fill, width: int) -> None:
    draw.line(points, fill=fill, width=width, joint="curve")


def path_mask(points: list[tuple[int, int]], width: int) -> Image.Image:
    mask = Image.new("L", SIZE, 0)
    draw_line(ImageDraw.Draw(mask), points, 255, width)
    return mask


def polygon_mask(points: list[tuple[int, int]]) -> Image.Image:
    mask = Image.new("L", SIZE, 0)
    ImageDraw.Draw(mask).polygon(points, fill=255)
    return mask


def shift_mask(mask: Image.Image, dx: int, dy: int) -> Image.Image:
    shifted = Image.new("L", SIZE, 0)
    shifted.paste(mask, (dx, dy))
    return shifted


def shape_from_name(name: str) -> str:
    lower = name.lower()
    if "left" in lower:
        return "left"
    if "right" in lower:
        return "right"
    if "narrow" in lower:
        return "narrow"
    if "split" in lower:
        return "split"
    return "straight"


def shape_mask(shape: str) -> tuple[Image.Image, list[list[tuple[int, int]]], list[tuple[int, int]] | None]:
    if shape == "straight":
        polygon = [(478, 120), (1058, 120), (1230, 900), (306, 900)]
        return polygon_mask(polygon), [[(768, 880), (768, 140)]], polygon
    if shape == "narrow":
        polygon = [(615, 120), (921, 120), (1200, 900), (336, 900)]
        return polygon_mask(polygon), [[(768, 880), (768, 140)]], polygon
    if shape == "left":
        center = [(900, 890), (790, 710), (650, 455), (520, 130)]
        return path_mask(center, 325), [center], None
    if shape == "right":
        center = [(636, 890), (746, 710), (886, 455), (1016, 130)]
        return path_mask(center, 325), [center], None

    trunk = [(768, 900), (768, 560)]
    left = [(768, 560), (602, 390), (446, 135)]
    right = [(768, 560), (934, 390), (1090, 135)]
    mask = Image.new("L", SIZE, 0)
    draw = ImageDraw.Draw(mask)
    for points in (trunk, left, right):
        draw_line(draw, points, 255, 282)
    draw.ellipse((634, 420, 902, 694), fill=255)
    return mask, [trunk, left, right], None


def clipped(image: Image.Image, mask: Image.Image, draw_fn) -> None:
    layer = Image.new("RGBA", SIZE, (0, 0, 0, 0))
    draw_fn(ImageDraw.Draw(layer, "RGBA"))
    alpha = ImageChops.multiply(layer.getchannel("A"), mask)
    layer.putalpha(alpha)
    image.alpha_composite(layer)


def draw_gradient_surface(image: Image.Image, mask: Image.Image, style: dict[str, tuple[int, int, int]]) -> None:
    gradient = Image.new("RGBA", SIZE, (0, 0, 0, 0))
    pixels = gradient.load()
    for y in range(HEIGHT):
        vertical = y / (HEIGHT - 1)
        for x in range(WIDTH):
            diagonal = (x / (WIDTH - 1)) * 0.25 + (1.0 - vertical) * 0.75
            color = blend(style["top_dark"], style["top_light"], max(0.0, min(1.0, diagonal)))
            pixels[x, y] = rgba(color)
    gradient.putalpha(mask)
    image.alpha_composite(gradient)


def draw_base(image: Image.Image, top_mask: Image.Image, stage: str, style: dict[str, tuple[int, int, int]]) -> None:
    if stage == "STAGE01_NRY":
        outer = top_mask.filter(ImageFilter.MaxFilter(45))
        side = shift_mask(outer, 0, 66)
        shadow = shift_mask(outer, 30, 86).filter(ImageFilter.GaussianBlur(28))
        shadow_alpha = 82
    else:
        outer = top_mask.filter(ImageFilter.MaxFilter(33))
        side = shift_mask(outer, 0, 44)
        shadow = shift_mask(outer, 24, 62).filter(ImageFilter.GaussianBlur(24))
        shadow_alpha = 54

    image.paste(Image.new("RGBA", SIZE, (0, 0, 0, shadow_alpha)), (0, 0), shadow)
    image.paste(Image.new("RGBA", SIZE, rgba(style["side"])), (0, 0), side)
    image.paste(Image.new("RGBA", SIZE, rgba(style["rim"])), (0, 0), outer)
    draw_gradient_surface(image, top_mask, style)

    top_glow = shift_mask(top_mask.filter(ImageFilter.FIND_EDGES).filter(ImageFilter.GaussianBlur(1)), -6, -8)
    image.paste(Image.new("RGBA", SIZE, (255, 255, 255, 18)), (0, 0), top_glow)
    if stage == "STAGE01_NRY":
        lower_edge = shift_mask(top_mask.filter(ImageFilter.FIND_EDGES).filter(ImageFilter.GaussianBlur(1.2)), 0, 20)
        image.paste(Image.new("RGBA", SIZE, (36, 18, 8, 92)), (0, 0), lower_edge)


def sample_polyline(points: list[tuple[int, int]], step: int) -> list[tuple[int, int]]:
    samples: list[tuple[int, int]] = []
    for (x1, y1), (x2, y2) in zip(points, points[1:]):
        length = math.hypot(x2 - x1, y2 - y1)
        count = max(1, int(length // step))
        for index in range(count):
            t = index / count
            samples.append((int(x1 + (x2 - x1) * t), int(y1 + (y2 - y1) * t)))
    samples.append(points[-1])
    return samples


def draw_rivets_along(draw: ImageDraw.ImageDraw, points: list[tuple[int, int]], dx: int, rng: random.Random) -> None:
    for x, y in sample_polyline(points, 42):
        radius = rng.randint(5, 7)
        rx = x + dx + rng.randint(-2, 2)
        ry = y + rng.randint(-2, 2)
        draw.ellipse((rx - radius, ry - radius, rx + radius, ry + radius), fill=(28, 24, 22, 174))
        draw.ellipse((rx - radius + 2, ry - radius + 2, rx + radius - 2, ry + radius - 2), fill=(132, 118, 98, 190))
        draw.ellipse((rx - radius + 3, ry - radius + 2, rx - radius + 7, ry - radius + 6), fill=(246, 222, 180, 78))


def draw_wood_surface(
    draw: ImageDraw.ImageDraw,
    rng: random.Random,
    style: dict[str, tuple[int, int, int]],
    center_paths: list[list[tuple[int, int]]],
) -> None:
    # Long wet pier planks: keep the runner direction readable and avoid a checkerboard tile read.
    for y in range(160, 895, 112):
        left_x = int(486 - (y - 155) * 0.20)
        right_x = int(1050 + (y - 155) * 0.20)
        draw.line([(left_x, y), (right_x, y)], fill=rgba(style["seam"], 58), width=3)
        draw.line([(left_x + 10, y - 5), (right_x - 10, y - 5)], fill=(236, 156, 82, 14), width=2)

    for center in center_paths:
        for dx in (-190, 190):
            draw.line(offset_points(center, dx), fill=(55, 30, 17, 230), width=36, joint="curve")
            draw.line(offset_points(center, dx), fill=(126, 74, 38, 235), width=25, joint="curve")
            draw.line(offset_points(center, dx - 6 if dx > 0 else dx + 6), fill=(224, 145, 73, 72), width=4, joint="curve")
        for dx in (-142, 142):
            draw.line(offset_points(center, dx), fill=(42, 39, 38, 178), width=12, joint="curve")
            draw_rivets_along(draw, center, dx, rng)

    for _ in range(250):
        x = rng.randint(360, 1130)
        y = rng.randint(145, 880)
        length = rng.randint(95, 280)
        color = blend(style["top_dark"], style["top_light"], rng.random() * 0.45)
        draw.line(
            [(x, y), (x + length // 2, y + rng.randint(-5, 5)), (x + length, y + rng.randint(-4, 4))],
            fill=rgba(color, rng.randint(42, 112)),
            width=rng.randint(1, 3),
        )

    for _ in range(70):
        x = rng.randint(365, 1110)
        y = rng.randint(170, 860)
        draw.line(
            [(x, y), (x + rng.randint(70, 250), y - rng.randint(4, 20))],
            fill=(255, 232, 184, rng.randint(42, 95)),
            width=rng.randint(2, 7),
        )

    for center in center_paths:
        samples = sample_polyline(center, 66)
        for x, y in samples[1:-1:2]:
            side = rng.choice([-1, 1])
            px = x + side * rng.randint(86, 132)
            py = y + rng.randint(-18, 18)
            w = rng.randint(74, 170)
            h = rng.randint(12, 34)
            draw.ellipse((px - w // 2, py - h // 2, px + w // 2, py + h // 2), fill=rgba(style["water"], rng.randint(74, 126)))
            draw.arc((px - w // 2 + 5, py - h // 2 + 3, px + w // 2 - 4, py + h // 2 - 3), 190, 350, fill=(220, 244, 255, 80), width=3)

    for _ in range(16):
        x = rng.randint(360, 1110)
        y = rng.randint(155, 875)
        w = rng.randint(80, 220)
        h = rng.randint(12, 36)
        draw.arc((x, y, x + w, y + h), 190, 350, fill=(255, 245, 210, rng.randint(45, 88)), width=3)


def draw_asphalt_texture(
    draw: ImageDraw.ImageDraw,
    rng: random.Random,
    style: dict[str, tuple[int, int, int]],
    density: int,
    gloss: bool = False,
) -> None:
    for _ in range(density):
        x = rng.randint(320, 1210)
        y = rng.randint(125, 895)
        s = rng.randint(1, 4)
        color = rng.choice([style["top_dark"], style["top_light"], style["top"], style["seam"]])
        draw.ellipse((x, y, x + s, y + s), fill=rgba(color, rng.randint(28, 78)))

    for _ in range(28):
        x = rng.randint(350, 1120)
        y = rng.randint(145, 870)
        w = rng.randint(38, 150)
        h = rng.randint(8, 28)
        color = rng.choice([style["top_dark"], style["top_light"], style["water"]])
        draw.ellipse((x, y, x + w, y + h), fill=rgba(color, rng.randint(16, 42)))

    if gloss:
        for _ in range(34):
            x = rng.randint(410, 1060)
            y = rng.randint(145, 840)
            w = rng.randint(80, 240)
            draw.line([(x, y), (x + w, y - rng.randint(10, 36))], fill=(255, 255, 255, rng.randint(18, 45)), width=rng.randint(2, 6))


def draw_pavement_seams(draw: ImageDraw.ImageDraw, style: dict[str, tuple[int, int, int]], stage: str) -> None:
    if stage == "STAGE03_RST":
        for y in range(190, 855, 126):
            draw.line([(385, y), (1155, y + 4)], fill=rgba(style["seam"], 68), width=4)
        return
    if stage == "STAGE04_CITY":
        for y in range(190, 855, 118):
            draw.line([(380, y), (1160, y)], fill=rgba(style["seam"], 60), width=4)
        for x in (614, 922):
            draw.line([(x, 145), (x - 14, 885)], fill=rgba(style["seam"], 42), width=3)


def offset_points(points: list[tuple[int, int]], dx: int) -> list[tuple[int, int]]:
    return [(x + dx, y) for x, y in points]


def draw_dashed_polyline(
    draw: ImageDraw.ImageDraw,
    points: list[tuple[int, int]],
    color: tuple[int, int, int],
    width: int,
    dash: int,
    gap: int,
    alpha: int = 230,
) -> None:
    for (x1, y1), (x2, y2) in zip(points, points[1:]):
        length = math.hypot(x2 - x1, y2 - y1)
        if length <= 0:
            continue
        ux = (x2 - x1) / length
        uy = (y2 - y1) / length
        distance = 0.0
        while distance < length:
            start = distance
            end = min(distance + dash, length)
            p1 = (int(x1 + ux * start), int(y1 + uy * start))
            p2 = (int(x1 + ux * end), int(y1 + uy * end))
            draw.line([p1, p2], fill=rgba(color, alpha), width=width)
            distance += dash + gap


def draw_lane_markings(
    draw: ImageDraw.ImageDraw,
    center_paths: list[list[tuple[int, int]]],
    style: dict[str, tuple[int, int, int]],
    stage: str,
) -> None:
    line = style["line"]
    if line is None:
        return

    for center in center_paths:
        for dx in (-92, 92):
            draw_dashed_polyline(draw, offset_points(center, dx), line, 13, 72, 72, 225)

        edge_color = blend(line, style["top_light"], 0.25)
        for dx in (-158, 158):
            draw.line(offset_points(center, dx), fill=rgba(edge_color, 175), width=7, joint="curve")

    if stage == "STAGE05_GNG":
        for center in center_paths:
            draw.line(center, fill=(155, 150, 112, 115), width=5, joint="curve")


def draw_side_curbs(draw: ImageDraw.ImageDraw, center_paths: list[list[tuple[int, int]]], style: dict[str, tuple[int, int, int]], stage: str) -> None:
    if stage == "STAGE01_NRY":
        return
    curb = style["curb"]
    for center in center_paths:
        for dx in (-176, 176):
            draw.line(offset_points(center, dx), fill=rgba(curb, 205), width=20, joint="curve")
            draw.line(offset_points(center, dx + (8 if dx < 0 else -8)), fill=(255, 255, 255, 26), width=3, joint="curve")


def draw_stage_details(
    image: Image.Image,
    top_mask: Image.Image,
    center_paths: list[list[tuple[int, int]]],
    stage: str,
    shape: str,
    rng: random.Random,
) -> None:
    style = STYLE[stage]

    def details(draw: ImageDraw.ImageDraw) -> None:
        if stage == "STAGE01_NRY":
            draw_wood_surface(draw, rng, style, center_paths)
            return
        if stage == "STAGE03_RST":
            draw_pavement_seams(draw, style, stage)
            draw_asphalt_texture(draw, rng, style, density=260)
            draw_side_curbs(draw, center_paths, style, stage)
            return

        draw_pavement_seams(draw, style, stage)
        draw_asphalt_texture(draw, rng, style, density=520 if stage != "STAGE05_GNG" else 360, gloss=stage == "STAGE05_GNG")
        draw_side_curbs(draw, center_paths, style, stage)
        draw_lane_markings(draw, center_paths, style, stage)

        if stage == "STAGE02_HWY" and shape == "split":
            draw.polygon([(748, 604), (788, 604), (788, 690), (828, 690), (768, 774), (708, 690), (748, 690)], fill=(232, 232, 222, 200))

    clipped(image, top_mask, details)


def draw_road(stage: str, name: str, output: Path) -> None:
    shape = shape_from_name(name)
    top_mask, center_paths, _ = shape_mask(shape)
    style = STYLE[stage]
    rng = random.Random(f"{stage}:{name}:active-road-v1")

    image = Image.new("RGBA", SIZE, BACKGROUND)
    draw_base(image, top_mask, stage, style)
    draw_stage_details(image, top_mask, center_paths, stage, shape, rng)

    image = image.filter(ImageFilter.UnsharpMask(radius=1.0, percent=95, threshold=3))
    output.parent.mkdir(parents=True, exist_ok=True)
    image.convert("RGB").save(output, quality=95)


def active_road_names() -> list[str]:
    names: set[str] = set()
    for directory in (IMAGE_DIR, IMAGE_DIR / "old"):
        if not directory.exists():
            continue
        for path in directory.glob("*.png"):
            match = ROAD_FILE_PATTERN.match(path.name)
            if not match:
                continue
            stage = match.group("stage")
            asset = match.group("asset")
            if asset in ACTIVE_ROAD_ASSETS.get(stage, set()):
                names.add(path.name)
    return sorted(names, key=lambda name: int(name[:3]))


def main() -> None:
    generated = []
    for name in active_road_names():
        match = ROAD_FILE_PATTERN.match(name)
        if not match:
            continue
        stage = match.group("stage")
        output = IMAGE_DIR / name
        draw_road(stage, match.group("name"), output)
        generated.append(name)

    for name in generated:
        print(name)
    print(f"Generated {len(generated)} active stage road image(s).")


if __name__ == "__main__":
    main()
