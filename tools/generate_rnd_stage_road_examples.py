from __future__ import annotations

import argparse
import html
import json
import math
import random
import re
import shutil
from dataclasses import dataclass
from pathlib import Path

from PIL import Image, ImageChops, ImageDraw, ImageFilter


ROOT = Path(__file__).resolve().parents[1]
RND_DIR = ROOT / "output" / "meshy_images" / "RnD"

WIDTH = 1280
HEIGHT = 896
SIZE = (WIDTH, HEIGHT)
BACKGROUND = (246, 246, 242, 255)


@dataclass(frozen=True)
class StageSpec:
    code: str
    folder: str
    label: str
    family: str
    top: tuple[int, int, int]
    top_light: tuple[int, int, int]
    top_dark: tuple[int, int, int]
    side: tuple[int, int, int]
    rim: tuple[int, int, int]
    line: tuple[int, int, int] | None
    accent: tuple[int, int, int]
    prop_a: tuple[int, int, int]
    prop_b: tuple[int, int, int]


@dataclass(frozen=True)
class ShapeSpec:
    key: str
    kind: str
    label: str
    width: int = 238


@dataclass(frozen=True)
class ConceptSpec:
    index: int
    shape: ShapeSpec
    material: str
    edge: str
    detail: str
    finish: str
    slug: str


STAGES: tuple[StageSpec, ...] = (
    StageSpec(
        "STAGE01_NRY",
        "01_noryangjin",
        "Noryangjin wet fish-market pier road",
        "wood",
        (124, 72, 37),
        (205, 131, 64),
        (52, 29, 17),
        (55, 33, 21),
        (106, 64, 36),
        None,
        (37, 119, 172),
        (37, 132, 184),
        (238, 241, 232),
    ),
    StageSpec(
        "STAGE02_HWY",
        "02_highway",
        "Korean expressway modular asphalt road",
        "highway",
        (58, 60, 61),
        (108, 111, 109),
        (26, 29, 32),
        (52, 54, 56),
        (122, 126, 123),
        (236, 236, 226),
        (226, 178, 38),
        (72, 126, 83),
        (177, 182, 178),
    ),
    StageSpec(
        "STAGE03_RST",
        "03_rest_stop",
        "highway rest-stop service pavement road",
        "rest_stop",
        (101, 113, 107),
        (158, 168, 159),
        (67, 80, 75),
        (78, 88, 84),
        (138, 148, 139),
        (231, 231, 219),
        (44, 143, 132),
        (72, 151, 130),
        (213, 96, 42),
    ),
    StageSpec(
        "STAGE04_CITY",
        "04_city",
        "dense city street modular road",
        "city",
        (61, 67, 73),
        (115, 122, 127),
        (32, 39, 47),
        (68, 75, 82),
        (146, 151, 154),
        (235, 235, 225),
        (224, 182, 40),
        (77, 137, 88),
        (218, 224, 220),
    ),
    StageSpec(
        "STAGE05_GNG",
        "05_gangnam",
        "Gangnam premium glossy boulevard road",
        "gangnam",
        (34, 36, 43),
        (92, 94, 103),
        (15, 17, 24),
        (39, 42, 50),
        (99, 101, 109),
        (227, 227, 217),
        (213, 174, 58),
        (29, 122, 157),
        (194, 162, 83),
    ),
)


SHAPES: tuple[ShapeSpec, ...] = (
    ShapeSpec("straight_snap_1x", "straight", "straight snap module", 238),
    ShapeSpec("straight_long_2x", "straight_long", "long straight snap module", 226),
    ShapeSpec("wide_three_lane", "straight_wide", "wide three-lane module", 292),
    ShapeSpec("narrow_gate", "straight_narrow", "narrow gate module", 198),
    ShapeSpec("left_corner_90", "corner_left", "left 90-degree corner", 238),
    ShapeSpec("right_corner_90", "corner_right", "right 90-degree corner", 238),
    ShapeSpec("soft_left_curve", "curve_left", "soft left curve", 236),
    ShapeSpec("soft_right_curve", "curve_right", "soft right curve", 236),
    ShapeSpec("s_curve_left_first", "s_left", "S-curve left-first road", 232),
    ShapeSpec("s_curve_right_first", "s_right", "S-curve right-first road", 232),
    ShapeSpec("left_chicane", "chicane_left", "left chicane module", 214),
    ShapeSpec("right_chicane", "chicane_right", "right chicane module", 214),
    ShapeSpec("t_junction", "t", "T-junction module", 226),
    ShapeSpec("cross_junction", "cross", "cross junction module", 224),
    ShapeSpec("y_split", "y_split", "Y-split module", 214),
    ShapeSpec("y_merge", "y_merge", "Y-merge module", 214),
    ShapeSpec("merge_left", "merge_left", "left lane merge module", 198),
    ShapeSpec("merge_right", "merge_right", "right lane merge module", 198),
    ShapeSpec("narrowing_connector", "narrowing", "narrowing connector module", 244),
    ShapeSpec("widening_connector", "widening", "widening connector module", 244),
    ShapeSpec("end_cap", "end_cap", "blocked end-cap module", 232),
    ShapeSpec("bridge_transition", "bridge", "raised bridge transition", 230),
    ShapeSpec("ramp_up", "ramp_up", "uphill ramp module", 230),
    ShapeSpec("ramp_down", "ramp_down", "downhill ramp module", 230),
    ShapeSpec("underpass_lane", "underpass", "underpass lane module", 228),
    ShapeSpec("checkpoint_lane", "checkpoint", "checkpoint gate lane", 230),
    ShapeSpec("obstacle_lane", "obstacle", "obstacle lane module", 230),
    ShapeSpec("roundabout_quarter", "roundabout", "roundabout entry module", 222),
    ShapeSpec("left_side_bay", "side_bay_left", "left side-bay road module", 228),
    ShapeSpec("right_side_bay", "side_bay_right", "right side-bay road module", 228),
    ShapeSpec("plaza_plate", "plaza", "wide plaza plate module", 298),
    ShapeSpec("left_hairpin", "hairpin_left", "left hairpin turn module", 216),
    ShapeSpec("right_hairpin", "hairpin_right", "right hairpin turn module", 216),
    ShapeSpec("fork_round", "fork_round", "fork around center island", 208),
    ShapeSpec("tunnel_entry", "tunnel_entry", "tunnel entry road module", 228),
    ShapeSpec("elevated_split", "elevated_split", "elevated split ramp module", 210),
    ShapeSpec("zigzag", "zigzag", "zigzag modular road", 210),
    ShapeSpec("service_loop", "service_loop", "service loop road module", 206),
    ShapeSpec("island_crossing", "island_crossing", "road around central island", 220),
    ShapeSpec("offset_straight", "offset_straight", "offset straight connector", 226),
)


STAGE_TOKENS: dict[str, dict[str, tuple[str, ...]]] = {
    "STAGE01_NRY": {
        "materials": (
            "wet cedar plank deck",
            "oil-dark reclaimed pier boards",
            "patched plywood fish-market deck",
            "blue puddled floating pontoon wood",
            "tar-stained dock planks",
            "salt-bleached boardwalk slab",
            "metal-rimmed timber pier",
            "green algae edge timber",
            "ice-streaked seafood market planks",
            "rope-framed harbor gangway boards",
        ),
        "edges": (
            "round piling posts and sagging rope rails",
            "raised side beams with black rivet strips",
            "chunky corner pilings with wrapped rope",
            "low fish-market curb beams and wet nail heads",
            "steel bracket plates bolted into the wood",
            "uneven old pier rails with broken board ends",
            "floating dock pontoons visible under the sides",
            "thick mooring posts and cleat blocks",
            "weathered side fenders and tire bumpers",
            "stacked timber sleepers forming high curbs",
        ),
        "details": (
            "attached blue fish crates",
            "white styrofoam boxes with ice chunks",
            "small aquarium tubs along the sides",
            "red life rings and orange cones",
            "fishing net bundles tied to posts",
            "black tire fenders hanging from beams",
            "wet footprints and scattered scales",
            "small rope coils and metal cleats",
            "fish-market lamps clipped to side posts",
            "sea-water puddles reflecting cyan highlights",
        ),
        "finishes": (
            "chunky mobile-game 3D prop proportions",
            "hand-painted stylized bevels",
            "strong Meshy image-to-3D silhouette",
            "modular prefab kit with flat snap ends",
            "single clean road object on a light background",
        ),
    },
    "STAGE02_HWY": {
        "materials": (
            "fresh dark expressway asphalt",
            "older patched highway surface",
            "rain-slick asphalt slab",
            "elevated concrete deck with asphalt top",
            "tunnel-worn blacktop",
            "ribbed bridge-deck asphalt",
            "bus-lane asphalt with green tint",
            "construction-zone road plates",
            "toll-lane asphalt apron",
            "shoulder-lane coarse blacktop",
        ),
        "edges": (
            "silver guardrails with thick posts",
            "concrete K-rail barriers",
            "yellow-black hazard curb blocks",
            "sound-wall panels mounted on the sides",
            "metal bridge railings",
            "flexible green lane bollards",
            "orange construction drums along both edges",
            "low median barrier with reflector studs",
            "overhead gantry support feet",
            "raised shoulder curbs and drainage slots",
        ),
        "details": (
            "white dashed lane markings",
            "yellow center guide line",
            "merge arrows painted on the road",
            "chevron curve signs attached to rails",
            "portable LED arrow trailer markings",
            "tollgate lane stripes and stop line",
            "rumble strips near the edges",
            "speed-camera mast bases",
            "maintenance hatch panels",
            "rain reflections and small oil patches",
        ),
        "finishes": (
            "chunky mobile-game 3D prop proportions",
            "clean highway prefab module read",
            "thick extruded slab with strong shadows",
            "Meshy-friendly image-to-3D reference",
            "single centered asset with no schematic labels",
        ),
    },
    "STAGE03_RST": {
        "materials": (
            "light concrete rest-stop service lane",
            "painted parking-lot asphalt",
            "gas-station forecourt pavement",
            "EV charger bay concrete",
            "food-court delivery lane paving",
            "bus stop layby asphalt",
            "brick-pattern service pavers",
            "oil-spotted truck bay concrete",
            "cream curbside walkway slab",
            "weathered rest-area access road",
        ),
        "edges": (
            "rounded concrete curbs",
            "parking wheel stops on the sides",
            "short teal bollards",
            "planter boxes clipped to the edges",
            "low service-lane guard curbs",
            "painted red-and-white curb blocks",
            "EV charger bases and cable posts",
            "picnic-area timber curb rails",
            "trash-bin alcove blocks",
            "delivery loading dock bumpers",
        ),
        "details": (
            "parking bay stripes",
            "EV floor icon simplified as geometric marks",
            "gas-pump island hints",
            "rest-stop arrow markings",
            "speed bumps crossing the lane",
            "bus bay yellow box paint",
            "food-truck service arrows",
            "drain grates and patch seams",
            "vending-machine color blocks at the side",
            "small cafe sign base with no readable text",
        ),
        "finishes": (
            "chunky mobile-game 3D prop proportions",
            "soft concrete bevels and visible slab depth",
            "clean modular prefab with flat snap ends",
            "Meshy-friendly single road asset",
            "light background with strong contact shadow",
        ),
    },
    "STAGE04_CITY": {
        "materials": (
            "dense downtown asphalt",
            "patched residential street surface",
            "crosswalk-heavy intersection asphalt",
            "bus-lane city road",
            "construction steel-plate street",
            "tram-track asphalt strip",
            "bike-lane side street",
            "rain-dark urban road",
            "stone curb street apron",
            "market-alley asphalt and concrete mix",
        ),
        "edges": (
            "raised sidewalks and granite curbs",
            "bollards and pedestrian signal bases",
            "storm drains embedded at the curbs",
            "tree grates and small planters",
            "construction cones and temporary barriers",
            "painted curb stones",
            "bus stop curb extensions",
            "bike lane separators",
            "manhole-rim curb hardware",
            "narrow alley building plinths",
        ),
        "details": (
            "zebra crosswalk blocks",
            "left-turn arrows and stop lines",
            "bus-only lane paint with no text",
            "bike lane green panels with no text",
            "tram rail grooves",
            "utility covers and manholes",
            "construction plate seams",
            "taxi pickup curb markings",
            "rain reflections around drain grates",
            "streetlight base shadows",
        ),
        "finishes": (
            "stylized but grounded 3D road module",
            "thick extruded city prefab slab",
            "Meshy image-to-3D clean centered asset",
            "flat snap ends for modular assembly",
            "no labels, no UI annotations, no 2D guide strokes",
        ),
    },
    "STAGE05_GNG": {
        "materials": (
            "glossy dark Gangnam boulevard asphalt",
            "black valet-lane paving",
            "premium hotel driveway stone",
            "dark asphalt with gold curb trim",
            "polished shopping-street road",
            "rain-reflective luxury boulevard",
            "glass-tower entrance pavement",
            "marble-inlaid road apron",
            "neon-lit club district asphalt",
            "department-store forecourt paving",
        ),
        "edges": (
            "gold-trim curbs and black stone sides",
            "short chrome bollards",
            "planters with premium stone bases",
            "velvet rope posts attached to the road edge",
            "LED light strips along the sides",
            "marble curb blocks",
            "glass storefront plinths",
            "valet cone rows and brass stanchions",
            "luxury median stones",
            "polished drainage channels",
        ),
        "details": (
            "thin white lane lines",
            "gold accent lane dividers",
            "valet stop marks with no text",
            "diamond-shaped pavement inlays",
            "subtle neon reflections",
            "showroom entrance stripe",
            "premium driveway arrows",
            "black mirror-like puddles",
            "small spotlight pools along the curb",
            "department-store threshold panels",
        ),
        "finishes": (
            "premium stylized 3D prop proportions",
            "thick glossy road slab with strong bevels",
            "Meshy-friendly image-to-3D reference",
            "single clean modular road object",
            "white background, no text, no watermark",
        ),
    },
}


MATERIAL_SWATCHES: dict[str, tuple[tuple[tuple[int, int, int], tuple[int, int, int], tuple[int, int, int], tuple[int, int, int], tuple[int, int, int]], ...]] = {
    "STAGE01_NRY": (
        ((124, 72, 37), (205, 131, 64), (52, 29, 17), (106, 64, 36), (37, 119, 172)),
        ((78, 45, 28), (151, 89, 49), (26, 17, 12), (72, 43, 28), (26, 86, 116)),
        ((151, 93, 48), (224, 152, 80), (62, 37, 22), (118, 75, 42), (55, 132, 166)),
        ((112, 76, 51), (185, 132, 84), (42, 31, 22), (87, 68, 52), (30, 154, 196)),
        ((71, 52, 38), (133, 96, 67), (24, 18, 14), (62, 49, 38), (34, 93, 124)),
        ((166, 124, 78), (236, 184, 112), (78, 56, 36), (132, 96, 59), (87, 157, 180)),
        ((122, 83, 52), (195, 139, 84), (53, 38, 28), (86, 88, 84), (118, 134, 140)),
        ((94, 92, 54), (157, 143, 80), (39, 44, 26), (74, 91, 55), (41, 143, 92)),
        ((135, 92, 58), (214, 155, 96), (54, 37, 27), (105, 76, 51), (128, 201, 225)),
        ((143, 89, 45), (217, 146, 74), (60, 34, 18), (116, 67, 34), (194, 139, 58)),
    ),
    "STAGE02_HWY": (
        ((58, 60, 61), (108, 111, 109), (26, 29, 32), (122, 126, 123), (226, 178, 38)),
        ((72, 74, 73), (130, 132, 128), (36, 38, 39), (132, 136, 131), (229, 185, 44)),
        ((45, 49, 52), (105, 110, 112), (18, 22, 25), (102, 110, 112), (96, 174, 211)),
        ((75, 78, 78), (142, 145, 139), (42, 45, 46), (156, 160, 154), (224, 174, 42)),
        ((39, 42, 45), (90, 94, 96), (17, 20, 23), (105, 109, 111), (230, 178, 35)),
        ((57, 60, 61), (118, 121, 116), (28, 31, 33), (135, 138, 133), (216, 171, 38)),
        ((42, 72, 63), (84, 132, 103), (22, 38, 35), (100, 130, 112), (236, 236, 226)),
        ((70, 71, 70), (137, 139, 134), (34, 35, 35), (96, 100, 96), (235, 126, 38)),
        ((64, 61, 56), (129, 124, 113), (34, 32, 30), (142, 137, 126), (231, 196, 62)),
        ((49, 51, 53), (96, 100, 101), (22, 25, 27), (116, 121, 119), (238, 235, 210)),
    ),
    "STAGE03_RST": (
        ((101, 113, 107), (158, 168, 159), (67, 80, 75), (138, 148, 139), (44, 143, 132)),
        ((70, 76, 75), (134, 140, 137), (45, 52, 51), (126, 132, 128), (232, 230, 214)),
        ((139, 139, 128), (198, 194, 175), (92, 91, 82), (159, 153, 133), (230, 112, 42)),
        ((89, 116, 111), (148, 174, 166), (58, 78, 75), (116, 150, 142), (50, 162, 185)),
        ((126, 112, 88), (185, 164, 126), (80, 70, 55), (150, 130, 96), (203, 82, 48)),
        ((73, 78, 79), (137, 143, 142), (43, 48, 49), (128, 133, 132), (239, 199, 48)),
        ((129, 105, 82), (190, 153, 116), (77, 61, 50), (146, 119, 91), (66, 146, 122)),
        ((106, 105, 99), (172, 168, 156), (62, 63, 59), (142, 140, 130), (224, 114, 46)),
        ((159, 144, 109), (220, 204, 162), (99, 88, 66), (174, 156, 118), (72, 151, 130)),
        ((88, 97, 92), (150, 160, 151), (55, 64, 60), (132, 142, 134), (230, 230, 218)),
    ),
    "STAGE04_CITY": (
        ((61, 67, 73), (115, 122, 127), (32, 39, 47), (146, 151, 154), (224, 182, 40)),
        ((76, 75, 72), (136, 132, 124), (43, 42, 40), (152, 148, 140), (236, 236, 225)),
        ((52, 56, 61), (108, 114, 119), (26, 31, 38), (134, 140, 144), (230, 230, 218)),
        ((53, 70, 63), (99, 132, 114), (27, 43, 37), (120, 151, 135), (236, 236, 226)),
        ((83, 84, 82), (154, 154, 148), (44, 45, 44), (119, 122, 120), (239, 137, 42)),
        ((56, 59, 62), (112, 116, 118), (28, 32, 35), (136, 140, 142), (190, 190, 180)),
        ((45, 84, 71), (83, 144, 114), (25, 48, 42), (94, 128, 113), (235, 235, 225)),
        ((43, 48, 53), (98, 106, 111), (20, 24, 29), (120, 128, 132), (94, 173, 214)),
        ((82, 79, 73), (145, 137, 124), (47, 45, 41), (151, 144, 132), (224, 182, 40)),
        ((69, 63, 58), (126, 116, 105), (39, 35, 31), (139, 128, 116), (218, 94, 50)),
    ),
    "STAGE05_GNG": (
        ((34, 36, 43), (92, 94, 103), (15, 17, 24), (99, 101, 109), (213, 174, 58)),
        ((23, 24, 31), (72, 74, 82), (8, 10, 15), (86, 88, 96), (218, 180, 63)),
        ((72, 67, 59), (148, 138, 120), (36, 32, 28), (165, 152, 127), (219, 184, 82)),
        ((28, 31, 38), (86, 91, 98), (12, 14, 21), (102, 96, 76), (218, 178, 49)),
        ((39, 40, 47), (96, 98, 105), (17, 18, 25), (110, 112, 118), (79, 178, 218)),
        ((24, 27, 33), (78, 84, 91), (9, 11, 16), (92, 99, 106), (96, 194, 230)),
        ((45, 53, 62), (103, 124, 136), (20, 27, 35), (118, 137, 145), (194, 162, 83)),
        ((86, 82, 76), (170, 160, 145), (42, 39, 35), (188, 174, 150), (222, 190, 93)),
        ((28, 26, 38), (85, 78, 112), (12, 10, 20), (82, 75, 106), (209, 75, 218)),
        ((57, 54, 49), (126, 120, 106), (27, 25, 22), (154, 143, 120), (220, 180, 62)),
    ),
}


def rgba(color: tuple[int, int, int], alpha: int = 255) -> tuple[int, int, int, int]:
    return color[0], color[1], color[2], alpha


def blend(a: tuple[int, int, int], b: tuple[int, int, int], t: float) -> tuple[int, int, int]:
    return tuple(int(a[i] + (b[i] - a[i]) * t) for i in range(3))


def adjust(color: tuple[int, int, int], amount: int) -> tuple[int, int, int]:
    return tuple(max(0, min(255, value + amount)) for value in color)


def slugify(text: str) -> str:
    text = text.lower()
    text = re.sub(r"[^a-z0-9]+", "_", text).strip("_")
    return re.sub(r"_+", "_", text)


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


def offset_points(points: list[tuple[int, int]], offset: float, step: int = 14) -> list[tuple[int, int]]:
    samples = sample_polyline(points, step)
    out: list[tuple[int, int]] = []
    for index, (x, y) in enumerate(samples):
        px, py = samples[max(0, index - 1)]
        nx, ny = samples[min(len(samples) - 1, index + 1)]
        dx = nx - px
        dy = ny - py
        length = math.hypot(dx, dy) or 1.0
        out.append((int(x - dy / length * offset), int(y + dx / length * offset)))
    return out


def perspective_width(base_width: int, y: float) -> float:
    t = y / HEIGHT
    return base_width * (0.66 + 0.48 * t)


def variable_path_mask(paths: list[list[tuple[int, int]]], width: int) -> Image.Image:
    mask = Image.new("L", SIZE, 0)
    draw = ImageDraw.Draw(mask)
    for path in paths:
        samples = sample_polyline(path, 10)
        left: list[tuple[int, int]] = []
        right: list[tuple[int, int]] = []
        for index, (x, y) in enumerate(samples):
            px, py = samples[max(0, index - 1)]
            nx, ny = samples[min(len(samples) - 1, index + 1)]
            dx = nx - px
            dy = ny - py
            length = math.hypot(dx, dy) or 1.0
            half = perspective_width(width, y) / 2
            left.append((int(x - dy / length * half), int(y + dx / length * half)))
            right.append((int(x + dy / length * half), int(y - dx / length * half)))
        if len(left) > 2:
            draw.polygon(left + right[::-1], fill=255)
    return mask.filter(ImageFilter.MaxFilter(5))


def polygon_mask(points: list[tuple[int, int]]) -> Image.Image:
    mask = Image.new("L", SIZE, 0)
    ImageDraw.Draw(mask).polygon(points, fill=255)
    return mask.filter(ImageFilter.MaxFilter(5))


def combine_masks(*masks: Image.Image) -> Image.Image:
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


def shape_paths(shape: ShapeSpec) -> tuple[Image.Image, list[list[tuple[int, int]]]]:
    kind = shape.kind
    width = shape.width
    if kind == "straight":
        paths = [[(640, 820), (640, 110)]]
        return variable_path_mask(paths, width), paths
    if kind == "straight_long":
        paths = [[(640, 850), (640, 65)]]
        return variable_path_mask(paths, width), paths
    if kind == "straight_wide":
        paths = [[(640, 820), (640, 100)]]
        return variable_path_mask(paths, width), paths
    if kind == "straight_narrow":
        paths = [[(640, 820), (640, 105)]]
        return variable_path_mask(paths, width), paths
    if kind == "offset_straight":
        paths = [[(575, 830), (620, 640), (695, 420), (735, 105)]]
        return variable_path_mask(paths, width), paths
    if kind == "corner_left":
        paths = [[(885, 805), (875, 575), (760, 438), (595, 390), (350, 390)]]
        return variable_path_mask(paths, width), paths
    if kind == "corner_right":
        paths = [[(395, 805), (405, 575), (520, 438), (685, 390), (930, 390)]]
        return variable_path_mask(paths, width), paths
    if kind == "curve_left":
        paths = [[(860, 820), (815, 650), (695, 500), (540, 365), (380, 230)]]
        return variable_path_mask(paths, width), paths
    if kind == "curve_right":
        paths = [[(420, 820), (465, 650), (585, 500), (740, 365), (900, 230)]]
        return variable_path_mask(paths, width), paths
    if kind == "s_left":
        paths = [[(450, 820), (650, 668), (826, 540), (655, 388), (450, 225)]]
        return variable_path_mask(paths, width), paths
    if kind == "s_right":
        paths = [[(830, 820), (630, 668), (454, 540), (625, 388), (830, 225)]]
        return variable_path_mask(paths, width), paths
    if kind == "chicane_left":
        paths = [[(535, 820), (535, 650), (750, 560), (750, 425), (575, 335), (575, 120)]]
        return variable_path_mask(paths, width), paths
    if kind == "chicane_right":
        paths = [[(745, 820), (745, 650), (530, 560), (530, 425), (705, 335), (705, 120)]]
        return variable_path_mask(paths, width), paths
    if kind == "t":
        paths = [[(640, 830), (640, 455)], [(315, 455), (965, 455)]]
        mask = variable_path_mask(paths, width)
        ImageDraw.Draw(mask).ellipse((500, 315, 780, 595), fill=255)
        return mask, paths
    if kind == "cross":
        paths = [[(640, 830), (640, 105)], [(300, 455), (980, 455)]]
        mask = variable_path_mask(paths, width)
        ImageDraw.Draw(mask).ellipse((500, 315, 780, 595), fill=255)
        return mask, paths
    if kind == "y_split":
        paths = [[(640, 830), (640, 505)], [(640, 505), (475, 360), (340, 160)], [(640, 505), (795, 350), (935, 150)]]
        return variable_path_mask(paths, width), paths
    if kind == "y_merge":
        paths = [[(475, 830), (585, 610), (640, 470)], [(805, 830), (695, 610), (640, 470)], [(640, 470), (640, 105)]]
        return variable_path_mask(paths, width), paths
    if kind == "merge_left":
        paths = [[(720, 820), (650, 620), (640, 100)], [(460, 820), (590, 630), (640, 430)]]
        return variable_path_mask(paths, width), paths
    if kind == "merge_right":
        paths = [[(560, 820), (630, 620), (640, 100)], [(820, 820), (690, 630), (640, 430)]]
        return variable_path_mask(paths, width), paths
    if kind == "narrowing":
        return polygon_mask([(445, 820), (835, 820), (735, 115), (545, 115)]), [[(640, 820), (640, 115)]]
    if kind == "widening":
        return polygon_mask([(535, 820), (745, 820), (830, 115), (450, 115)]), [[(640, 820), (640, 115)]]
    if kind == "end_cap":
        paths = [[(640, 820), (640, 285)]]
        cap = polygon_mask([(455, 160), (825, 160), (825, 305), (455, 305)])
        return combine_masks(variable_path_mask(paths, width), cap), paths
    if kind == "bridge":
        paths = [[(640, 830), (640, 95)]]
        return variable_path_mask(paths, width), paths
    if kind == "ramp_up":
        return polygon_mask([(492, 820), (788, 820), (842, 145), (438, 215)]), [[(640, 815), (640, 180)]]
    if kind == "ramp_down":
        return polygon_mask([(438, 820), (842, 750), (788, 120), (492, 120)]), [[(640, 785), (640, 130)]]
    if kind == "underpass":
        paths = [[(640, 820), (640, 100)]]
        return variable_path_mask(paths, width), paths
    if kind == "checkpoint":
        paths = [[(640, 820), (640, 100)]]
        return variable_path_mask(paths, width), paths
    if kind == "obstacle":
        paths = [[(640, 820), (640, 100)]]
        return variable_path_mask(paths, width), paths
    if kind == "roundabout":
        mask = Image.new("L", SIZE, 0)
        draw = ImageDraw.Draw(mask)
        draw.ellipse((400, 245, 880, 725), outline=255, width=210)
        draw.line([(640, 820), (640, 650)], fill=255, width=210)
        draw.line([(640, 320), (640, 100)], fill=255, width=190)
        return mask.filter(ImageFilter.MaxFilter(5)), [[(640, 820), (640, 650)], [(640, 320), (640, 100)]]
    if kind == "side_bay_left":
        paths = [[(640, 820), (640, 100)], [(640, 505), (455, 505), (390, 445)]]
        return variable_path_mask(paths, width), paths
    if kind == "side_bay_right":
        paths = [[(640, 820), (640, 100)], [(640, 505), (825, 505), (890, 445)]]
        return variable_path_mask(paths, width), paths
    if kind == "plaza":
        return polygon_mask([(410, 795), (870, 795), (890, 185), (390, 185)]), [[(640, 790), (640, 190)]]
    if kind == "hairpin_left":
        paths = [[(840, 820), (840, 620), (720, 500), (500, 500), (390, 388), (390, 210), (610, 160)]]
        return variable_path_mask(paths, width), paths
    if kind == "hairpin_right":
        paths = [[(440, 820), (440, 620), (560, 500), (780, 500), (890, 388), (890, 210), (670, 160)]]
        return variable_path_mask(paths, width), paths
    if kind == "fork_round":
        paths = [[(640, 820), (640, 570)], [(640, 570), (475, 420), (420, 230)], [(640, 570), (805, 420), (860, 230)]]
        mask = variable_path_mask(paths, width)
        ImageDraw.Draw(mask).ellipse((535, 430, 745, 640), fill=0)
        return mask.filter(ImageFilter.MaxFilter(11)), paths
    if kind == "tunnel_entry":
        paths = [[(640, 820), (640, 95)]]
        return variable_path_mask(paths, width), paths
    if kind == "elevated_split":
        paths = [[(640, 830), (640, 520)], [(640, 520), (515, 365), (405, 130)], [(640, 520), (765, 365), (875, 130)]]
        return variable_path_mask(paths, width), paths
    if kind == "zigzag":
        paths = [[(500, 820), (700, 685), (520, 550), (760, 410), (560, 270), (715, 125)]]
        return variable_path_mask(paths, width), paths
    if kind == "service_loop":
        mask = Image.new("L", SIZE, 0)
        draw = ImageDraw.Draw(mask)
        draw.rounded_rectangle((440, 245, 840, 720), radius=150, outline=255, width=190)
        draw.line([(640, 820), (640, 690)], fill=255, width=190)
        return mask.filter(ImageFilter.MaxFilter(5)), [[(640, 820), (640, 690)]]
    if kind == "island_crossing":
        paths = [[(560, 820), (560, 555), (640, 455), (720, 355), (720, 105)], [(720, 820), (720, 555), (640, 455), (560, 355), (560, 105)]]
        return variable_path_mask(paths, width), paths
    return shape_paths(SHAPES[0])


def make_concepts(stage: StageSpec) -> list[ConceptSpec]:
    tokens = STAGE_TOKENS[stage.code]
    materials = tokens["materials"]
    edges = tokens["edges"]
    details = tokens["details"]
    finishes = tokens["finishes"]
    concepts: list[ConceptSpec] = []
    used_slugs: set[str] = set()
    for index in range(1, 101):
        material = materials[(index - 1) % len(materials)]
        detail = details[((index - 1) // len(materials)) % len(details)]
        edge = edges[((index - 1) * 3 + (index - 1) // 7) % len(edges)]
        finish = finishes[((index - 1) * 5 + (index - 1) // 3) % len(finishes)]
        shape = SHAPES[(index - 1) % len(SHAPES)]
        base_slug = f"{shape.key}_{slugify(material)}_{slugify(detail)}"
        slug = base_slug[:112]
        suffix = 2
        while slug in used_slugs:
            slug = f"{base_slug[:106]}_{suffix}"
            suffix += 1
        used_slugs.add(slug)
        concepts.append(ConceptSpec(index, shape, material, edge, detail, finish, slug))
    return concepts


def material_slot(concept: ConceptSpec) -> int:
    return (concept.index - 1) % 10


def detail_slot(concept: ConceptSpec) -> int:
    return ((concept.index - 1) // 10) % 10


def style_palette(stage: StageSpec, concept: ConceptSpec) -> dict[str, tuple[int, int, int]]:
    top, light, dark, rim, accent = MATERIAL_SWATCHES[stage.code][material_slot(concept)]
    amount = ((concept.index * 7) % 11) - 5
    side = blend(stage.side, dark, 0.45)
    return {
        "top": adjust(top, amount),
        "top_light": adjust(light, amount + 3),
        "top_dark": adjust(dark, amount - 3),
        "side": adjust(side, amount - 4),
        "rim": adjust(rim, amount + 2),
        "line": stage.line or (232, 228, 212),
        "accent": adjust(accent, amount // 2),
        "prop_a": adjust(stage.prop_a, amount),
        "prop_b": adjust(stage.prop_b, amount),
    }


def draw_surroundings(draw: ImageDraw.ImageDraw, stage: StageSpec, concept: ConceptSpec, rng: random.Random) -> None:
    if stage.family == "wood":
        water = (22, 103, 149, 96)
        for x in (190, 1090):
            draw.rectangle((x - 100, 0, x + 100, HEIGHT), fill=water)
            for _ in range(22):
                y = rng.randint(60, 850)
                draw.arc((x - rng.randint(85, 145), y, x + rng.randint(35, 90), y + rng.randint(14, 36)), 180, 350, fill=(126, 204, 226, 85), width=2)
    elif stage.family == "highway":
        for x in (170, 1110):
            for y in range(80, 850, 82):
                draw.ellipse((x - 28, y - 17, x + 28, y + 17), fill=(51, 108, 62, 130))
                draw.rectangle((x + 34, y - 24, x + 42, y + 24), fill=(170, 174, 168, 95))
    elif stage.family == "rest_stop":
        for x in (230, 1050):
            for y in range(130, 810, 118):
                draw.rounded_rectangle((x - 42, y - 26, x + 42, y + 26), radius=7, fill=(72, 146, 126, 120))
                if (y // 118 + concept.index) % 2 == 0:
                    draw.rectangle((x - 28, y - 8, x + 28, y + 8), fill=(230, 100, 40, 100))
    elif stage.family == "city":
        for x in (175, 1100):
            for y in range(90, 840, 112):
                draw.rectangle((x - 42, y - 42, x + 42, y + 42), fill=(92, 98, 105, 110))
                draw.rectangle((x - 27, y - 25, x + 27, y + 25), fill=(181, 188, 185, 80))
    else:
        for x in (205, 1065):
            for y in range(120, 810, 145):
                draw.rounded_rectangle((x - 42, y - 54, x + 42, y + 54), radius=9, fill=(24, 83, 115, 105), outline=(196, 164, 82, 105), width=3)
                draw.rectangle((x - 18, y - 44, x + 18, y + 44), fill=(210, 230, 238, 55))


def draw_base(image: Image.Image, mask: Image.Image, stage: StageSpec, concept: ConceptSpec, palette: dict[str, tuple[int, int, int]]) -> None:
    lift = 56 if stage.family == "wood" else 42
    if stage.family == "gangnam":
        lift = 46
    outer = mask.filter(ImageFilter.MaxFilter(29 if stage.family == "wood" else 23))
    side = shift_mask(outer, 0, lift)
    shadow = shift_mask(outer, 30, lift + 32).filter(ImageFilter.GaussianBlur(20))
    image.paste(Image.new("RGBA", SIZE, (0, 0, 0, 68)), (0, 0), shadow)
    image.paste(Image.new("RGBA", SIZE, rgba(palette["side"])), (0, 0), side)
    image.paste(Image.new("RGBA", SIZE, rgba(palette["rim"])), (0, 0), outer)

    surface = Image.new("RGBA", SIZE, (0, 0, 0, 0))
    draw = ImageDraw.Draw(surface, "RGBA")
    gloss = stage.family == "gangnam" or "rain" in concept.material or "wet" in concept.material
    for y in range(HEIGHT):
        t = y / HEIGHT
        color = blend(palette["top_light"], palette["top_dark"], 0.16 + t * 0.74)
        if gloss and 0.18 < t < 0.58:
            color = blend(color, (255, 255, 255), 0.05 + (0.58 - t) * 0.05)
        draw.line([(0, y), (WIDTH, y)], fill=rgba(color))
    surface.putalpha(mask)
    image.alpha_composite(surface)

    edge = mask.filter(ImageFilter.FIND_EDGES).filter(ImageFilter.GaussianBlur(1.0))
    image.paste(Image.new("RGBA", SIZE, (255, 255, 255, 28)), (0, 0), edge)


def draw_wood_surface(draw: ImageDraw.ImageDraw, rng: random.Random, palette: dict[str, tuple[int, int, int]], concept: ConceptSpec) -> None:
    mat = material_slot(concept)
    pitch = 32 + (concept.index % 7) * 5
    skew = (concept.index % 5) - 2
    for y in range(95, 845, pitch):
        draw.line([(300, y + rng.randint(-4, 4)), (990, y + skew + rng.randint(-4, 4))], fill=rgba(palette["top_dark"], 135), width=3)
        draw.line([(330, y - 6), (955, y - 7 + skew)], fill=(255, 184, 91, 28), width=2)
    for x in range(450, 850, 52 + concept.index % 17):
        draw.line([(x + rng.randint(-4, 4), 95), (x + rng.randint(-10, 10), 842)], fill=rgba(palette["top_dark"], 45), width=2)
    for _ in range(210):
        x = rng.randint(305, 975)
        y = rng.randint(95, 840)
        length = rng.randint(36, 170)
        color = blend(palette["top_dark"], palette["top_light"], rng.random() * 0.58)
        draw.line([(x, y), (x + length, y + rng.randint(-6, 6))], fill=rgba(color, rng.randint(42, 120)), width=rng.randint(1, 3))
    puddle_count = 32 + concept.index % 26
    for _ in range(puddle_count):
        x = rng.randint(345, 900)
        y = rng.randint(120, 805)
        w = rng.randint(42, 138)
        h = rng.randint(10, 29)
        draw.ellipse((x, y, x + w, y + h), fill=rgba(palette["accent"], rng.randint(42, 118)))
        draw.arc((x + 4, y + 3, x + w - 4, y + h - 3), 190, 350, fill=(220, 246, 255, 72), width=2)
    if mat in {3, 8}:
        for _ in range(18):
            x = rng.randint(350, 900)
            y = rng.randint(120, 790)
            draw.rounded_rectangle((x, y, x + rng.randint(58, 145), y + rng.randint(8, 22)), radius=4, fill=(142, 214, 232, 82))
    if mat == 5:
        for _ in range(26):
            x = rng.randint(330, 930)
            y = rng.randint(115, 815)
            draw.line([(x, y), (x + rng.randint(55, 150), y + rng.randint(-3, 3))], fill=(242, 218, 166, 74), width=3)
    if mat == 6:
        for y in range(160, 780, 130):
            draw.rectangle((405, y, 875, y + 22), fill=(82, 84, 80, 98), outline=(178, 171, 145, 95), width=2)
    if mat == 7:
        for x in (420, 860):
            for y in range(120, 810, 70):
                draw.ellipse((x - 28, y - 8, x + 28, y + 8), fill=(47, 132, 75, 86))
    if mat == 9:
        for y in range(150, 800, 115):
            draw.arc((420, y - 34, 860, y + 42), 8, 172, fill=(205, 151, 72, 110), width=4)


def draw_pavement_surface(draw: ImageDraw.ImageDraw, rng: random.Random, palette: dict[str, tuple[int, int, int]], concept: ConceptSpec, stage: StageSpec) -> None:
    mat = material_slot(concept)
    density = 520
    if stage.family == "rest_stop":
        density = 310
    if stage.family == "gangnam":
        density = 360
    for _ in range(density):
        x = rng.randint(285, 995)
        y = rng.randint(85, 845)
        s = rng.randint(1, 4)
        color = rng.choice([palette["top_dark"], palette["top_light"], palette["top"]])
        draw.ellipse((x, y, x + s, y + s), fill=rgba(color, rng.randint(24, 82)))
    for _ in range(24):
        x = rng.randint(320, 930)
        y = rng.randint(110, 810)
        draw.ellipse((x, y, x + rng.randint(44, 150), y + rng.randint(7, 23)), fill=rgba(palette["top_light"], rng.randint(12, 38)))
    if stage.family == "rest_stop":
        for y in range(130, 810, 92 + concept.index % 18):
            draw.line([(365, y), (915, y + rng.randint(-4, 4))], fill=rgba(palette["top_dark"], 62), width=3)
        for x in range(430, 860, 86):
            draw.line([(x, 130), (x + rng.randint(-8, 8), 805)], fill=rgba(palette["top_dark"], 38), width=2)
        if mat == 1:
            for x in range(470, 805, 54):
                draw.line([(x, 170), (x + 34, 790)], fill=(236, 236, 220, 92), width=4)
        if mat == 3:
            draw.rounded_rectangle((500, 230, 780, 705), radius=20, fill=(48, 142, 149, 70), outline=(224, 240, 234, 82), width=5)
        if mat == 6:
            for y in range(145, 790, 54):
                draw.line([(385, y), (895, y)], fill=(102, 75, 58, 80), width=3)
            for x in range(400, 890, 70):
                draw.line([(x, 145), (x, 790)], fill=(102, 75, 58, 55), width=2)
    if stage.family == "city":
        for y in (230, 685):
            for index in range(7):
                x0 = 520 + index * 45
                draw.rectangle((x0, y, x0 + 26, y + 94), fill=(238, 238, 230, 138))
        for _ in range(6):
            x = rng.randint(500, 760)
            y = rng.randint(140, 760)
            draw.ellipse((x - 22, y - 10, x + 22, y + 10), fill=(28, 31, 34, 155), outline=(128, 132, 130, 130), width=3)
        if mat == 4:
            for y in range(190, 760, 140):
                draw.rounded_rectangle((455, y, 825, y + 56), radius=5, fill=(109, 111, 109, 110), outline=(184, 184, 176, 105), width=3)
        if mat == 5:
            for offset in (-62, 62):
                draw.line([(640 + offset, 130), (640 + offset, 810)], fill=(174, 174, 164, 118), width=5)
                draw.line([(640 + offset + 14, 130), (640 + offset + 14, 810)], fill=(30, 30, 30, 82), width=2)
        if mat == 6:
            draw.rounded_rectangle((482, 150, 586, 800), radius=22, fill=(34, 128, 95, 86), outline=(232, 232, 220, 92), width=4)
    if stage.family == "gangnam":
        for y in range(130, 780, 130):
            draw.line([(430, y), (850, y - 18)], fill=rgba(palette["accent"], 44), width=4)
        for _ in range(18):
            x = rng.randint(390, 875)
            y = rng.randint(105, 825)
            draw.ellipse((x, y, x + rng.randint(70, 170), y + rng.randint(10, 32)), fill=(85, 180, 220, rng.randint(22, 50)))
        if mat in {2, 7, 9}:
            for y in range(155, 790, 88):
                draw.line([(420, y), (860, y)], fill=(232, 218, 174, 92), width=3)
            for x in range(460, 845, 96):
                draw.line([(x, 150), (x + 12, 800)], fill=(232, 218, 174, 58), width=2)
        if mat == 8:
            for y in range(150, 805, 110):
                draw.line([(430, y), (850, y + 28)], fill=(214, 76, 225, 90), width=5)
                draw.line([(430, y + 18), (850, y + 44)], fill=(77, 202, 236, 80), width=4)
    if stage.family == "highway":
        if mat == 6:
            draw.rounded_rectangle((492, 120, 590, 815), radius=20, fill=(39, 116, 78, 78), outline=(238, 238, 225, 86), width=4)
        if mat == 7:
            for y in range(160, 770, 125):
                draw.rounded_rectangle((450, y, 830, y + 58), radius=5, fill=(102, 104, 101, 106), outline=(188, 186, 172, 105), width=3)
        if mat == 8:
            for y in (310, 390, 470):
                draw.line([(420, y), (860, y)], fill=(246, 246, 230, 142), width=6)
        if mat == 2:
            for _ in range(18):
                x = rng.randint(390, 820)
                y = rng.randint(120, 800)
                draw.ellipse((x, y, x + rng.randint(70, 180), y + rng.randint(10, 32)), fill=(92, 180, 220, rng.randint(24, 58)))


def draw_dash(draw: ImageDraw.ImageDraw, path: list[tuple[int, int]], color: tuple[int, int, int], width: int, dash: int, gap: int, alpha: int = 220) -> None:
    samples = sample_polyline(path, 10)
    drawing = True
    remaining = dash
    for start, end in zip(samples, samples[1:]):
        sx, sy = start
        ex, ey = end
        length = math.hypot(ex - sx, ey - sy)
        if length <= 0:
            continue
        consumed = 0.0
        while consumed < length:
            take = min(remaining, length - consumed)
            t0 = consumed / length
            t1 = (consumed + take) / length
            p0 = (int(sx + (ex - sx) * t0), int(sy + (ey - sy) * t0))
            p1 = (int(sx + (ex - sx) * t1), int(sy + (ey - sy) * t1))
            if drawing:
                draw.line([p0, p1], fill=rgba(color, alpha), width=width)
            consumed += take
            remaining -= take
            if remaining <= 0:
                drawing = not drawing
                remaining = dash if drawing else gap


def draw_lane_markings(draw: ImageDraw.ImageDraw, paths: list[list[tuple[int, int]]], stage: StageSpec, palette: dict[str, tuple[int, int, int]], concept: ConceptSpec) -> None:
    if stage.family == "wood":
        return
    line = palette["line"]
    for path in paths:
        if stage.family in {"highway", "city", "gangnam"}:
            for offset in (-44, 44):
                draw_dash(draw, offset_points(path, offset), line, 7, 48, 46, 210)
            for offset in (-96, 96):
                draw.line(offset_points(path, offset), fill=rgba(line, 96), width=4, joint="curve")
        if stage.family in {"highway", "gangnam"}:
            center_alpha = 125 if stage.family == "highway" else 165
            draw.line(path, fill=rgba(palette["accent"], center_alpha), width=4, joint="curve")
        if stage.family == "rest_stop":
            for offset in (-72, 72):
                draw_dash(draw, offset_points(path, offset), line, 5, 38, 36, 120)
    if stage.family == "rest_stop" and concept.index % 3 == 0:
        for x in range(505, 795, 58):
            draw.rectangle((x, 520, x + 34, 700), outline=rgba(line, 110), width=4)
    if stage.family == "city" and concept.index % 4 in (0, 1):
        for y in (242, 652):
            for index in range(6):
                x0 = 515 + index * 48
                draw.rectangle((x0, y, x0 + 28, y + 108), fill=(240, 240, 230, 126))
    if stage.family == "gangnam" and concept.index % 5 == 0:
        for y in range(235, 690, 92):
            draw.polygon([(640, y - 22), (682, y + 18), (640, y + 58), (598, y + 18)], fill=rgba(palette["accent"], 95))


def draw_rivet(draw: ImageDraw.ImageDraw, x: int, y: int, radius: int) -> None:
    draw.ellipse((x - radius, y - radius, x + radius, y + radius), fill=(24, 21, 19, 175))
    draw.ellipse((x - radius + 2, y - radius + 2, x + radius - 2, y + radius - 2), fill=(132, 116, 91, 215))


def draw_post(draw: ImageDraw.ImageDraw, x: int, y: int, palette: dict[str, tuple[int, int, int]], rng: random.Random) -> None:
    w = rng.randint(20, 28)
    h = rng.randint(44, 62)
    draw.ellipse((x - w // 2, y + h - 8, x + w // 2, y + h + 8), fill=(30, 20, 13, 90))
    draw.rectangle((x - w // 2, y - h, x + w // 2, y + h // 2), fill=rgba(palette["side"], 235))
    draw.rectangle((x - w // 2 + 5, y - h + 6, x + w // 2 - 5, y + h // 2 - 4), fill=rgba(palette["top"], 235))
    draw.ellipse((x - w // 2, y - h - 8, x + w // 2, y - h + 10), fill=rgba(palette["top_light"], 245))


def draw_edge_hardware(draw: ImageDraw.ImageDraw, paths: list[list[tuple[int, int]]], stage: StageSpec, palette: dict[str, tuple[int, int, int]], concept: ConceptSpec, rng: random.Random) -> None:
    for path in paths:
        for side in (-1, 1):
            edge = offset_points(path, side * (concept.shape.width * 0.42))
            inner = offset_points(path, side * (concept.shape.width * 0.30))
            if stage.family == "wood":
                draw.line(edge, fill=(48, 28, 17, 238), width=16, joint="curve")
                draw.line(inner, fill=(44, 40, 36, 172), width=8, joint="curve")
                for x, y in edge[:: max(5, 8 - concept.index % 3)]:
                    draw_post(draw, x, y, palette, rng)
                for x, y in inner[::4]:
                    draw_rivet(draw, x, y, rng.randint(4, 6))
            elif stage.family == "highway":
                draw.line(edge, fill=rgba(palette["rim"], 230), width=16, joint="curve")
                rail = offset_points(path, side * (concept.shape.width * 0.50))
                draw.line(rail, fill=(180, 185, 180, 216), width=8, joint="curve")
                for x, y in rail[::7]:
                    draw.rectangle((x - 5, y - 13, x + 5, y + 13), fill=(112, 117, 116, 216))
            elif stage.family == "rest_stop":
                draw.line(edge, fill=rgba(palette["rim"], 230), width=18, joint="curve")
                draw.line(edge, fill=(255, 255, 255, 40), width=5, joint="curve")
                for x, y in edge[::9]:
                    draw.rounded_rectangle((x - 18, y - 7, x + 18, y + 7), radius=3, fill=rgba(palette["prop_a"], 160))
            elif stage.family == "city":
                draw.line(edge, fill=rgba(palette["rim"], 236), width=19, joint="curve")
                sidewalk = offset_points(path, side * (concept.shape.width * 0.53))
                draw.line(sidewalk, fill=(154, 155, 150, 155), width=17, joint="curve")
                for x, y in sidewalk[::10]:
                    draw.ellipse((x - 7, y - 7, x + 7, y + 7), fill=(52, 55, 58, 190))
            else:
                draw.line(edge, fill=rgba(palette["rim"], 238), width=18, joint="curve")
                draw.line(edge, fill=rgba(palette["accent"], 175), width=5, joint="curve")
                for x, y in edge[::10]:
                    draw.ellipse((x - 8, y - 8, x + 8, y + 8), fill=rgba(palette["prop_b"], 190))


def draw_shape_extra(draw: ImageDraw.ImageDraw, shape: ShapeSpec, stage: StageSpec, palette: dict[str, tuple[int, int, int]], concept: ConceptSpec) -> None:
    kind = shape.kind
    if kind in {"ramp_up", "ramp_down"}:
        for y in range(360, 666, 76):
            draw.polygon([(585, y), (640, y - 36), (695, y), (675, y + 20), (640, y - 3), (605, y + 20)], fill=rgba(palette["accent"], 205))
    if kind in {"underpass", "tunnel_entry"}:
        draw.rounded_rectangle((430, 195, 850, 405), radius=18, fill=(45, 49, 52, 252), outline=rgba(palette["rim"], 230), width=7)
        draw.rectangle((470, 310, 810, 410), fill=(20, 24, 27, 252))
        for x in range(488, 790, 58):
            draw.polygon([(x, 225), (x + 34, 225), (x + 16, 286), (x - 18, 286)], fill=rgba(palette["accent"], 220))
    if kind == "checkpoint":
        draw.rounded_rectangle((430, 270, 850, 352), radius=12, fill=(38, 47, 52, 238), outline=(180, 185, 178, 190), width=4)
        for x in range(460, 820, 58):
            fill = (232, 232, 222, 230) if x // 58 % 2 else (35, 35, 35, 230)
            draw.rectangle((x, 283, x + 28, 344), fill=fill)
    if kind == "obstacle":
        for x, y in [(565, 380), (725, 475), (640, 610), (570, 705), (730, 720)]:
            draw.rounded_rectangle((x - 32, y - 22, x + 32, y + 22), radius=7, fill=(42, 47, 48, 226), outline=(172, 176, 168, 160), width=3)
        for x, y in [(555, 525), (735, 575)]:
            draw.polygon([(x, y - 38), (x - 28, y + 36), (x + 28, y + 36)], fill=(224, 90, 25, 235))
    if kind in {"roundabout", "fork_round", "service_loop", "island_crossing"}:
        draw.ellipse((560, 390, 720, 550), fill=rgba(palette["prop_a"], 150), outline=rgba(palette["rim"], 170), width=5)
        draw.ellipse((595, 425, 685, 515), fill=rgba(palette["top_dark"], 76))
    if kind in {"side_bay_left", "side_bay_right", "plaza"}:
        for x in range(500, 790, 62):
            draw.rectangle((x, 545, x + 36, 680), outline=rgba(palette["line"], 110), width=3)
    if kind == "bridge":
        for x in (472, 808):
            draw.line([(x, 125), (x, 815)], fill=rgba(palette["rim"], 185), width=10)
            for y in range(170, 800, 115):
                draw.line([(x - 28, y), (x + 28, y + 36)], fill=rgba(palette["rim"], 125), width=5)


def draw_attached_props(draw: ImageDraw.ImageDraw, stage: StageSpec, palette: dict[str, tuple[int, int, int]], concept: ConceptSpec, rng: random.Random) -> None:
    detail = detail_slot(concept)
    slots = [(380, 250), (900, 320), (365, 610), (910, 680)]
    if stage.family == "wood":
        for index, (x, y) in enumerate(slots):
            if detail == 0:
                draw.rounded_rectangle((x - 44, y - 30, x + 44, y + 30), radius=8, fill=rgba(palette["prop_a"], 220), outline=(235, 240, 238, 180), width=3)
                draw.rectangle((x - 34, y - 5, x + 34, y + 5), fill=(235, 245, 246, 90))
            elif detail == 1:
                draw.rounded_rectangle((x - 46, y - 25, x + 46, y + 25), radius=6, fill=(236, 240, 232, 224), outline=(91, 128, 166, 150), width=3)
                for _ in range(5):
                    ox = rng.randint(-28, 18)
                    oy = rng.randint(-14, 8)
                    draw.ellipse((x + ox, y + oy, x + ox + rng.randint(12, 30), y + oy + rng.randint(8, 18)), fill=(185, 225, 240, 115))
            elif detail == 2:
                draw.rounded_rectangle((x - 48, y - 30, x + 48, y + 30), radius=7, fill=(36, 137, 185, 190), outline=(235, 244, 246, 160), width=3)
                draw.rectangle((x - 35, y - 18, x + 35, y + 18), fill=(95, 194, 222, 105))
            elif detail == 3:
                draw.ellipse((x - 34, y - 18, x + 34, y + 18), outline=(230, 70, 45, 225), width=7)
                draw.polygon([(x + 54, y - 30), (x + 26, y + 36), (x + 82, y + 36)], fill=(236, 104, 36, 215))
            elif detail == 4:
                draw.arc((x - 50, y - 36, x + 50, y + 36), 20, 335, fill=(66, 92, 80, 180), width=5)
                for offset in range(-34, 36, 17):
                    draw.line([(x - 40, y + offset), (x + 40, y - offset)], fill=(72, 100, 88, 120), width=2)
            elif detail == 5:
                draw.ellipse((x - 38, y - 24, x + 38, y + 24), fill=(24, 24, 22, 210))
                draw.ellipse((x - 21, y - 12, x + 21, y + 12), fill=BACKGROUND)
            elif detail == 6:
                for _ in range(8):
                    ox = rng.randint(-35, 35)
                    oy = rng.randint(-20, 20)
                    draw.ellipse((x + ox - 5, y + oy - 3, x + ox + 5, y + oy + 3), fill=(215, 220, 210, 140))
            elif detail == 7:
                for radius in (24, 17, 10):
                    draw.arc((x - radius, y - radius, x + radius, y + radius), 0, 310, fill=(208, 153, 78, 190), width=4)
                draw.rectangle((x + 36, y - 7, x + 68, y + 7), fill=(92, 86, 72, 170))
            elif detail == 8:
                draw.rectangle((x - 6, y - 54, x + 6, y + 20), fill=(62, 42, 26, 220))
                draw.ellipse((x - 24, y - 74, x + 24, y - 34), fill=(240, 164, 52, 200))
                draw.ellipse((x - 14, y - 64, x + 14, y - 42), fill=(255, 224, 126, 210))
            else:
                draw.ellipse((x - 52, y - 20, x + 52, y + 20), fill=rgba(palette["accent"], 116))
                draw.arc((x - 44, y - 14, x + 44, y + 14), 190, 350, fill=(222, 246, 255, 100), width=3)
    elif stage.family == "highway":
        for index, (x, y) in enumerate(slots):
            if detail in {0, 6}:
                for n in range(5):
                    draw.rectangle((x - 46 + n * 20, y - 7, x - 34 + n * 20, y + 7), fill=(235, 235, 220, 165))
            elif detail == 1:
                draw.rounded_rectangle((x - 48, y - 12, x + 48, y + 12), radius=4, fill=rgba(palette["accent"], 190))
            elif detail == 2:
                draw.polygon([(x - 42, y - 12), (x + 16, y - 12), (x + 16, y - 30), (x + 58, y), (x + 16, y + 30), (x + 16, y + 12), (x - 42, y + 12)], fill=(240, 240, 220, 190))
            elif detail == 3:
                draw.rounded_rectangle((x - 44, y - 22, x + 44, y + 22), radius=5, fill=(45, 49, 48, 225), outline=(232, 232, 210, 170), width=3)
                for n in range(3):
                    draw.polygon([(x - 30 + n * 22, y - 15), (x - 12 + n * 22, y), (x - 30 + n * 22, y + 15)], fill=rgba(palette["accent"], 230))
            elif detail == 4:
                draw.rounded_rectangle((x - 56, y - 25, x + 56, y + 25), radius=4, fill=(35, 42, 44, 226), outline=(232, 170, 35, 190), width=3)
                for n in range(5):
                    draw.rectangle((x - 42 + n * 18, y - 6, x - 34 + n * 18, y + 6), fill=(242, 187, 44, 220))
            elif detail == 5:
                draw.rounded_rectangle((x - 48, y - 25, x + 48, y + 25), radius=8, fill=(116, 112, 98, 205), outline=(236, 236, 220, 165), width=3)
                draw.rectangle((x - 35, y - 4, x + 35, y + 5), fill=(236, 236, 220, 160))
            elif detail == 7:
                draw.rectangle((x - 6, y - 58, x + 6, y + 26), fill=(82, 86, 84, 220))
                draw.rounded_rectangle((x - 24, y - 72, x + 24, y - 38), radius=5, fill=(52, 58, 60, 230), outline=(230, 230, 220, 150), width=2)
            elif detail == 8:
                draw.rounded_rectangle((x - 42, y - 22, x + 42, y + 22), radius=4, fill=(70, 72, 70, 150), outline=(178, 178, 166, 150), width=3)
                draw.line([(x - 34, y), (x + 34, y)], fill=(34, 36, 36, 120), width=3)
            else:
                draw.ellipse((x - 52, y - 18, x + 52, y + 18), fill=(30, 38, 42, 82))
                draw.ellipse((x - 32, y - 11, x + 32, y + 11), fill=(90, 178, 215, 62))
            if detail not in {0, 1, 2, 3, 4, 5, 7, 8, 9}:
                draw.polygon([(x, y - 34), (x - 28, y + 34), (x + 28, y + 34)], fill=(230, 90, 26, 230))
                draw.rectangle((x - 18, y + 4, x + 18, y + 12), fill=(245, 245, 235, 210))
    elif stage.family == "rest_stop":
        for index, (x, y) in enumerate(slots):
            if detail == 0:
                draw.rectangle((x - 42, y - 4, x + 42, y + 4), fill=(236, 236, 220, 150))
                draw.rectangle((x - 42, y + 18, x + 42, y + 26), fill=(236, 236, 220, 120))
            elif detail == 1:
                draw.rounded_rectangle((x - 25, y - 42, x + 25, y + 42), radius=6, fill=(36, 146, 150, 210), outline=(232, 236, 228, 160), width=3)
                draw.line([(x, y + 38), (x + 34, y + 58)], fill=(28, 42, 44, 150), width=4)
            elif detail == 2:
                draw.rectangle((x - 18, y - 50, x + 18, y + 34), fill=(230, 104, 42, 190))
                draw.rectangle((x - 10, y - 35, x + 10, y - 5), fill=(240, 236, 220, 125))
            elif detail == 3:
                draw.polygon([(x - 40, y - 12), (x + 18, y - 12), (x + 18, y - 30), (x + 58, y), (x + 18, y + 30), (x + 18, y + 12), (x - 40, y + 12)], fill=(236, 236, 220, 150))
            elif detail == 4:
                draw.rounded_rectangle((x - 50, y - 12, x + 50, y + 12), radius=6, fill=(232, 198, 58, 185), outline=(52, 52, 48, 120), width=2)
            elif detail == 5:
                draw.rounded_rectangle((x - 52, y - 20, x + 52, y + 20), radius=7, fill=(230, 190, 66, 150), outline=(236, 236, 220, 120), width=2)
                draw.rectangle((x - 35, y + 22, x + 35, y + 34), fill=(72, 78, 76, 120))
            elif detail == 6:
                draw.rounded_rectangle((x - 54, y - 26, x + 54, y + 26), radius=6, fill=(202, 92, 42, 165), outline=(236, 236, 220, 115), width=2)
            elif detail == 7:
                draw.rectangle((x - 44, y - 14, x + 44, y + 14), fill=(45, 52, 50, 170))
                for n in range(5):
                    draw.line([(x - 36 + n * 18, y - 13), (x - 36 + n * 18, y + 13)], fill=(150, 160, 150, 130), width=2)
            elif detail == 8:
                draw.rounded_rectangle((x - 25, y - 42, x + 25, y + 42), radius=6, fill=rgba(palette["prop_a"], 205), outline=(232, 236, 228, 160), width=3)
            else:
                draw.rounded_rectangle((x - 50, y - 20, x + 50, y + 20), radius=5, fill=rgba(palette["prop_b"], 190), outline=(236, 236, 220, 100), width=2)
    elif stage.family == "city":
        for index, (x, y) in enumerate(slots):
            if detail == 0:
                for n in range(4):
                    draw.rectangle((x - 48 + n * 24, y - 18, x - 34 + n * 24, y + 18), fill=(238, 238, 230, 145))
            elif detail == 1:
                draw.polygon([(x - 38, y - 12), (x + 12, y - 12), (x + 12, y - 28), (x + 48, y), (x + 12, y + 28), (x + 12, y + 12), (x - 38, y + 12)], fill=rgba(palette["accent"], 160))
            elif detail == 2:
                draw.rounded_rectangle((x - 48, y - 20, x + 48, y + 20), radius=5, fill=(42, 92, 128, 145), outline=(235, 235, 225, 115), width=2)
            elif detail == 3:
                for n in range(3):
                    draw.rectangle((x - 30 + n * 30, y - 32, x - 18 + n * 30, y + 32), fill=(36, 128, 88, 165))
            elif detail == 4:
                draw.line([(x - 38, y + 28), (x + 38, y - 28)], fill=(166, 166, 156, 160), width=6)
                draw.line([(x - 38, y - 28), (x + 38, y + 28)], fill=(166, 166, 156, 130), width=4)
            elif detail == 5:
                draw.ellipse((x - 28, y - 18, x + 28, y + 18), fill=(32, 35, 38, 180), outline=(146, 151, 154, 160), width=4)
            elif detail == 6:
                draw.rounded_rectangle((x - 42, y - 22, x + 42, y + 22), radius=4, fill=(102, 104, 100, 150), outline=(210, 208, 190, 120), width=3)
                draw.polygon([(x + 58, y - 30), (x + 30, y + 36), (x + 86, y + 36)], fill=(230, 90, 26, 190))
            elif detail == 7:
                draw.rounded_rectangle((x - 52, y - 16, x + 52, y + 16), radius=6, fill=(232, 190, 44, 155))
            elif detail == 8:
                draw.rectangle((x - 44, y - 12, x + 44, y + 12), fill=(38, 46, 50, 150))
                for n in range(5):
                    draw.line([(x - 34 + n * 17, y - 10), (x - 34 + n * 17, y + 10)], fill=(130, 140, 140, 120), width=2)
            else:
                draw.rectangle((x - 8, y - 48, x + 8, y + 32), fill=(38, 42, 45, 220))
                draw.ellipse((x - 18, y - 64, x + 18, y - 28), fill=rgba(palette["accent"], 190))
    else:
        for index, (x, y) in enumerate(slots):
            if detail == 0:
                draw.ellipse((x - 10, y - 10, x + 10, y + 10), fill=(214, 214, 205, 210))
                draw.rectangle((x - 5, y - 45, x + 5, y + 32), fill=(168, 168, 160, 180))
            elif detail == 1:
                draw.line([(x - 58, y), (x + 58, y)], fill=rgba(palette["accent"], 205), width=6)
            elif detail == 2:
                draw.polygon([(x, y - 32), (x - 26, y + 32), (x + 26, y + 32)], fill=(230, 92, 32, 190))
                draw.rectangle((x - 14, y + 2, x + 14, y + 9), fill=(236, 236, 220, 170))
            elif detail == 3:
                draw.polygon([(x, y - 34), (x + 38, y), (x, y + 34), (x - 38, y)], fill=rgba(palette["accent"], 145), outline=(238, 220, 160, 100))
            elif detail == 4:
                draw.line([(x - 54, y - 18), (x + 54, y + 18)], fill=(214, 76, 225, 135), width=5)
                draw.line([(x - 54, y + 2), (x + 54, y + 38)], fill=(77, 202, 236, 115), width=4)
            elif detail == 5:
                draw.rounded_rectangle((x - 52, y - 18, x + 52, y + 18), radius=6, fill=(220, 205, 160, 145), outline=rgba(palette["accent"], 140), width=2)
            elif detail == 6:
                draw.polygon([(x - 42, y - 12), (x + 18, y - 12), (x + 18, y - 30), (x + 58, y), (x + 18, y + 30), (x + 18, y + 12), (x - 42, y + 12)], fill=rgba(palette["accent"], 160))
            elif detail == 7:
                draw.ellipse((x - 56, y - 18, x + 56, y + 18), fill=(10, 14, 20, 110))
                draw.ellipse((x - 36, y - 10, x + 36, y + 10), fill=(100, 190, 230, 45))
            elif detail == 8:
                draw.ellipse((x - 32, y - 16, x + 32, y + 16), fill=(255, 235, 156, 105))
                draw.ellipse((x - 10, y - 5, x + 10, y + 5), fill=(255, 255, 224, 155))
            else:
                draw.rounded_rectangle((x - 30, y - 46, x + 30, y + 46), radius=9, fill=rgba(palette["prop_a"], 155), outline=rgba(palette["prop_b"], 170), width=3)


def render(stage: StageSpec, concept: ConceptSpec, output: Path) -> None:
    rng = random.Random(f"{stage.code}:{concept.index}:{concept.slug}:rnd-road-v2")
    palette = style_palette(stage, concept)
    mask, paths = shape_paths(concept.shape)
    image = Image.new("RGBA", SIZE, BACKGROUND)
    draw = ImageDraw.Draw(image, "RGBA")
    draw_surroundings(draw, stage, concept, rng)
    draw_base(image, mask, stage, concept, palette)

    def details(d: ImageDraw.ImageDraw) -> None:
        if stage.family == "wood":
            draw_wood_surface(d, rng, palette, concept)
        else:
            draw_pavement_surface(d, rng, palette, concept, stage)
            draw_lane_markings(d, paths, stage, palette, concept)
        draw_edge_hardware(d, paths, stage, palette, concept, rng)
        draw_shape_extra(d, concept.shape, stage, palette, concept)

    clipped(image, mask, details)
    draw_attached_props(ImageDraw.Draw(image, "RGBA"), stage, palette, concept, rng)
    image = image.filter(ImageFilter.UnsharpMask(radius=1.0, percent=95, threshold=3))
    output.parent.mkdir(parents=True, exist_ok=True)
    image.convert("RGB").save(output, quality=94)


def make_brief(stage: StageSpec, concept: ConceptSpec) -> str:
    return (
        f"Single {stage.label}, {concept.shape.label}, {concept.material}, "
        f"{concept.edge}, {concept.detail}, {concept.finish}. "
        "MeshyAI Image to 3D reference, modular road prefab kit, 3/4 top view, "
        "visible thick side walls, flat snap connection ends where applicable, clean light background, "
        "no 2D flat tile, no labels, no watermark, no detached guide strokes."
    )


def manifest_row(stage: StageSpec, concept: ConceptSpec, path: Path) -> dict[str, object]:
    return {
        "index": concept.index,
        "stage": stage.code,
        "stage_label": stage.label,
        "folder": stage.folder,
        "filename": path.name,
        "road_form": concept.shape.label,
        "material": concept.material,
        "edge_treatment": concept.edge,
        "attached_detail": concept.detail,
        "finish": concept.finish,
        "recommended_input": "Image to 3D",
        "brief": make_brief(stage, concept),
        "negative_prompt": "flat 2D tile, schematic top-down diagram, text labels, watermark, colored guide lines, unrelated floating props, tiny unreadable details",
    }


def make_contact_sheet(stage: StageSpec, paths: list[Path]) -> Path:
    folder = RND_DIR / stage.folder
    out = folder / f"{stage.code}_100_examples_contact_sheet.png"
    columns = 10
    thumb = (170, 119)
    pad = 10
    label_h = 36
    rows = (len(paths) + columns - 1) // columns
    sheet = Image.new("RGB", (columns * (thumb[0] + pad) + pad, rows * (thumb[1] + label_h + pad) + pad), (245, 245, 242))
    draw = ImageDraw.Draw(sheet)
    for index, path in enumerate(paths):
        col = index % columns
        row = index // columns
        x = pad + col * (thumb[0] + pad)
        y = pad + row * (thumb[1] + label_h + pad)
        with Image.open(path) as source:
            img = source.convert("RGB")
            img.thumbnail(thumb, Image.Resampling.LANCZOS)
        sheet.paste(img, (x + (thumb[0] - img.width) // 2, y + (thumb[1] - img.height) // 2))
        draw.rectangle((x, y, x + thumb[0], y + thumb[1]), outline=(190, 190, 185), width=1)
        draw.text((x, y + thumb[1] + 5), f"{index + 1:03d} {path.stem[16:31]}", fill=(35, 35, 35))
    sheet.save(out, quality=95)
    return out


def make_representative_sheet(stage_first_paths: list[Path]) -> Path:
    out = RND_DIR / "stage_representatives_contact_sheet.png"
    columns = 5
    thumb = (220, 154)
    pad = 16
    label_h = 38
    sheet = Image.new("RGB", (columns * (thumb[0] + pad) + pad, thumb[1] + label_h + pad * 2), (245, 245, 242))
    draw = ImageDraw.Draw(sheet)
    for index, path in enumerate(stage_first_paths):
        x = pad + index * (thumb[0] + pad)
        y = pad
        with Image.open(path) as source:
            img = source.convert("RGB")
            img.thumbnail(thumb, Image.Resampling.LANCZOS)
        sheet.paste(img, (x + (thumb[0] - img.width) // 2, y + (thumb[1] - img.height) // 2))
        draw.rectangle((x, y, x + thumb[0], y + thumb[1]), outline=(190, 190, 185), width=1)
        draw.text((x, y + thumb[1] + 6), path.parent.name, fill=(35, 35, 35))
    sheet.save(out, quality=95)
    return out


def row_text(row: dict[str, object], key: str) -> str:
    return str(row.get(key, ""))


def write_gallery_index(rows: list[dict[str, object]]) -> Path:
    out = RND_DIR / "index.html"
    by_stage: dict[str, list[dict[str, object]]] = {}
    for row in rows:
        by_stage.setdefault(row_text(row, "folder"), []).append(row)

    stage_links = []
    sections = []
    for stage in STAGES:
        stage_rows = sorted(by_stage.get(stage.folder, []), key=lambda item: int(item.get("index", 0)))
        stage_links.append(f'<a href="#{stage.folder}">{html.escape(stage.folder)} ({len(stage_rows)})</a>')
        cards = []
        for row in stage_rows:
            filename = row_text(row, "filename")
            image_path = f"{stage.folder}/{filename}"
            title = f'{int(row.get("index", 0)):03d} {row_text(row, "road_form")}'
            meta = " | ".join(
                [
                    row_text(row, "material"),
                    row_text(row, "edge_treatment"),
                    row_text(row, "attached_detail"),
                ]
            )
            brief = row_text(row, "brief")
            negative = row_text(row, "negative_prompt")
            cards.append(
                "\n".join(
                    [
                        '<article class="card">',
                        f'  <a href="{html.escape(image_path)}"><img src="{html.escape(image_path)}" alt="{html.escape(title)}"></a>',
                        f'  <h3>{html.escape(title)}</h3>',
                        f'  <p class="meta">{html.escape(meta)}</p>',
                        "  <details>",
                        "    <summary>Meshy prompt</summary>",
                        f'    <textarea readonly>{html.escape(brief)}</textarea>',
                        f'    <p class="negative">{html.escape(negative)}</p>',
                        "  </details>",
                        "</article>",
                    ]
                )
            )
        sections.append(
            "\n".join(
                [
                    f'<section id="{stage.folder}">',
                    f"  <h2>{html.escape(stage.folder)} - {html.escape(stage.label)}</h2>",
                    '  <div class="grid">',
                    *cards,
                    "  </div>",
                    "</section>",
                ]
            )
        )

    document = "\n".join(
        [
            "<!doctype html>",
            '<html lang="en">',
            "<head>",
            '  <meta charset="utf-8">',
            '  <meta name="viewport" content="width=device-width, initial-scale=1">',
            "  <title>Meshy RnD Road Gallery</title>",
            "  <style>",
            "    :root { color-scheme: light; font-family: Arial, sans-serif; background: #f5f5f1; color: #202124; }",
            "    body { margin: 0; }",
            "    header { position: sticky; top: 0; z-index: 2; background: rgba(245, 245, 241, 0.96); border-bottom: 1px solid #d3d3ca; padding: 14px 18px; }",
            "    h1 { margin: 0 0 8px; font-size: 20px; }",
            "    nav { display: flex; flex-wrap: wrap; gap: 8px; }",
            "    nav a { color: #174f78; text-decoration: none; border: 1px solid #b9c7cf; padding: 5px 8px; border-radius: 4px; background: #fff; }",
            "    main { padding: 18px; }",
            "    section { margin-bottom: 36px; }",
            "    h2 { font-size: 18px; margin: 0 0 12px; }",
            "    .grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(220px, 1fr)); gap: 12px; }",
            "    .card { background: #fff; border: 1px solid #d4d4ce; border-radius: 6px; padding: 10px; box-shadow: 0 2px 8px rgba(0,0,0,0.04); }",
            "    img { width: 100%; aspect-ratio: 10 / 7; object-fit: contain; background: #f8f8f5; border: 1px solid #e0e0db; }",
            "    h3 { margin: 8px 0 5px; font-size: 14px; line-height: 1.25; }",
            "    .meta { min-height: 52px; margin: 0 0 8px; font-size: 12px; line-height: 1.35; color: #555; }",
            "    summary { cursor: pointer; font-size: 12px; color: #174f78; }",
            "    textarea { box-sizing: border-box; width: 100%; min-height: 120px; margin-top: 8px; resize: vertical; font-size: 12px; line-height: 1.35; }",
            "    .negative { font-size: 11px; color: #7a3c22; line-height: 1.35; }",
            "  </style>",
            "</head>",
            "<body>",
            "  <header>",
            "    <h1>Meshy RnD Road Gallery</h1>",
            f"    <nav>{''.join(stage_links)}</nav>",
            "  </header>",
            "  <main>",
            *sections,
            "  </main>",
            "</body>",
            "</html>",
        ]
    )
    out.write_text(document, encoding="utf-8")
    return out


def load_manifest_rows(path: Path) -> list[dict[str, object]]:
    return [json.loads(line) for line in path.read_text(encoding="utf-8").splitlines() if line.strip()]


def clear_rnd_dir() -> None:
    resolved = RND_DIR.resolve()
    expected = (ROOT / "output" / "meshy_images" / "RnD").resolve()
    if resolved != expected:
        raise RuntimeError(f"Refusing to clear unexpected path: {resolved}")
    RND_DIR.mkdir(parents=True, exist_ok=True)
    for child in RND_DIR.iterdir():
        if child.is_dir():
            shutil.rmtree(child)
        else:
            child.unlink()


def main() -> None:
    parser = argparse.ArgumentParser(description="Generate exploratory MeshyAI road RnD images and prompt manifests.")
    parser.add_argument(
        "--gallery-only",
        action="store_true",
        help="Rebuild only output/meshy_images/RnD/index.html from the existing combined JSONL manifest.",
    )
    parser.add_argument(
        "--allow-legacy-procedural",
        action="store_true",
        help="Allow the legacy procedural generator to clear and replace the current RnD folder.",
    )
    args = parser.parse_args()
    if args.gallery_only:
        manifest_path = RND_DIR / "meshy_rnd_road_prompts.jsonl"
        gallery_path = write_gallery_index(load_manifest_rows(manifest_path))
        print(f"gallery -> {gallery_path}")
        return
    if not args.allow_legacy_procedural:
        raise SystemExit(
            "Refusing to overwrite output/meshy_images/RnD with legacy procedural output. "
            "Current RnD uses road-only full 3D generated references. "
            "Pass --allow-legacy-procedural only if you intentionally want the old procedural set."
        )

    clear_rnd_dir()
    representative_paths: list[Path] = []
    manifest_rows: list[dict[str, object]] = []
    total = 0
    for stage in STAGES:
        folder = RND_DIR / stage.folder
        folder.mkdir(parents=True, exist_ok=True)
        generated: list[Path] = []
        stage_manifest: list[dict[str, object]] = []
        for concept in make_concepts(stage):
            output = folder / f"{concept.index:03d}_{stage.code}_{concept.slug}.png"
            render(stage, concept, output)
            row = manifest_row(stage, concept, output)
            generated.append(output)
            stage_manifest.append(row)
            manifest_rows.append(row)
        representative_paths.append(generated[0])
        sheet = make_contact_sheet(stage, generated)
        stage_manifest_path = folder / f"{stage.code}_meshy_prompts.jsonl"
        stage_manifest_path.write_text("\n".join(json.dumps(row, ensure_ascii=True) for row in stage_manifest) + "\n", encoding="utf-8")
        total += len(generated)
        print(f"{stage.code}: {len(generated)} examples -> {folder}")
        print(f"{stage.code}: contact sheet -> {sheet}")
        print(f"{stage.code}: prompt manifest -> {stage_manifest_path}")
    manifest_path = RND_DIR / "meshy_rnd_road_prompts.jsonl"
    manifest_path.write_text("\n".join(json.dumps(row, ensure_ascii=True) for row in manifest_rows) + "\n", encoding="utf-8")
    rep_sheet = make_representative_sheet(representative_paths)
    gallery_path = write_gallery_index(manifest_rows)
    print(f"representatives -> {rep_sheet}")
    print(f"prompt manifest -> {manifest_path}")
    print(f"gallery -> {gallery_path}")
    print(f"Generated {total} RnD road example image(s).")


if __name__ == "__main__":
    main()
