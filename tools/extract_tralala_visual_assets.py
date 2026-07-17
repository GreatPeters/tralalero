from __future__ import annotations

import argparse
import json
import shutil
from pathlib import Path

from PIL import Image, ImageDraw, ImageOps


def ensure_dirs(root: Path, names: list[str]) -> dict[str, Path]:
    paths = {name: root / name for name in names}
    for path in paths.values():
        path.mkdir(parents=True, exist_ok=True)
    return paths


def save_crop(
    image: Image.Image,
    box: tuple[int, int, int, int],
    destination: Path,
    *,
    rounded_radius: int | None = None,
    polygon: list[tuple[int, int]] | None = None,
    mask_inset: tuple[int, int, int, int] = (0, 0, 0, 0),
) -> None:
    crop = image.crop(box).convert("RGBA")
    if rounded_radius is not None or polygon is not None:
        mask = Image.new("L", crop.size, 0)
        draw = ImageDraw.Draw(mask)
        if polygon is not None:
            draw.polygon(polygon, fill=255)
        else:
            left, top, right, bottom = mask_inset
            draw.rounded_rectangle(
                (left, top, crop.width - 1 - right, crop.height - 1 - bottom),
                radius=rounded_radius or 0,
                fill=255,
            )
        crop.putalpha(mask)
    crop.save(destination, optimize=True)


def make_contact_sheet(files: list[Path], destination: Path) -> None:
    thumbnails: list[tuple[Path, Image.Image]] = []
    for file in files:
        image = Image.open(file).convert("RGBA")
        image.thumbnail((220, 260), Image.Resampling.LANCZOS)
        thumbnails.append((file, image.copy()))

    columns = 3
    cell_width = 240
    cell_height = 300
    rows = (len(thumbnails) + columns - 1) // columns
    sheet = Image.new("RGBA", (columns * cell_width, rows * cell_height), (30, 34, 40, 255))
    draw = ImageDraw.Draw(sheet)
    for index, (file, image) in enumerate(thumbnails):
        x = (index % columns) * cell_width
        y = (index // columns) * cell_height
        px = x + (cell_width - image.width) // 2
        py = y + 8
        checker = Image.new("RGBA", image.size, (210, 210, 210, 255))
        checker_draw = ImageDraw.Draw(checker)
        block = 12
        for cy in range(0, image.height, block):
            for cx in range(0, image.width, block):
                if ((cx // block) + (cy // block)) % 2:
                    checker_draw.rectangle(
                        (cx, cy, min(cx + block - 1, image.width - 1), min(cy + block - 1, image.height - 1)),
                        fill=(245, 245, 245, 255),
                    )
        checker.alpha_composite(image)
        sheet.alpha_composite(checker, (px, py))
        draw.text((x + 8, y + 272), file.stem[:34], fill=(255, 255, 255, 255))
    sheet.save(destination, optimize=True)


def extract_comic(source: Path, target_root: Path) -> None:
    folders = ensure_dirs(target_root, ["00_전체", "01_패널", "99_검수"])
    image = Image.open(source).convert("RGB")
    shutil.copy2(source, folders["00_전체"] / "img_intro_comic_final.png")

    panels = [
        ("img_intro_panel_01_theft.png", (3, 3, 938, 447)),
        ("img_intro_panel_02_curse.png", (3, 451, 938, 892)),
        ("img_intro_panel_03_condition.png", (3, 897, 938, 1308)),
        ("img_intro_panel_04_departure.png", (3, 1311, 938, 1669)),
    ]
    for filename, box in panels:
        save_crop(image, box, folders["01_패널"] / filename)

    make_contact_sheet(
        [folders["01_패널"] / filename for filename, _ in panels],
        folders["99_검수"] / "preview_intro_panels.png",
    )
    manifest = {
        "source": str(source),
        "canvas": {"width": image.width, "height": image.height},
        "unity": {"textureType": "Sprite (2D and UI)", "alpha": False, "suggestedPivot": [0.5, 0.5]},
        "panels": [{"file": f"01_패널/{filename}", "sourceCrop": list(box)} for filename, box in panels],
    }
    (target_root / "layout_manifest.json").write_text(
        json.dumps(manifest, ensure_ascii=False, indent=2), encoding="utf-8"
    )


def extract_shop(source: Path, shared_root: Path, target_root: Path) -> None:
    folders = ensure_dirs(
        target_root,
        [
            "00_전체시안",
            "01_배경",
            "02_캐릭터_오브젝트",
            "03_간판_소품",
            "04_업그레이드_공용",
            "05_업그레이드_완성카드",
            "06_상단UI",
            "07_하단UI",
            "98_생성원본",
            "99_검수",
        ],
    )
    image = Image.open(source).convert("RGB")
    shutil.copy2(source, folders["00_전체시안"] / "img_shoe_modifier_shop_final.png")
    save_crop(image, (0, 90, 864, 709), folders["01_배경"] / "img_shop_upper_scene_reference.png")

    crops: list[dict[str, object]] = []

    def crop(
        folder: str,
        filename: str,
        box: tuple[int, int, int, int],
        *,
        radius: int | None = None,
        polygon: list[tuple[int, int]] | None = None,
        mask_inset: tuple[int, int, int, int] = (0, 0, 0, 0),
    ) -> None:
        save_crop(
            image,
            box,
            folders[folder] / filename,
            rounded_radius=radius,
            polygon=polygon,
            mask_inset=mask_inset,
        )
        crops.append({"file": f"{folder}/{filename}", "sourceCrop": list(box)})

    crop("03_간판_소품", "img_sign_shop_title.png", (40, 88, 302, 244), radius=12)
    crop("03_간판_소품", "img_sign_good_shoes.png", (50, 241, 228, 312), radius=10)
    crop("03_간판_소품", "img_sign_cash_only.png", (48, 330, 220, 383), radius=9)
    crop("03_간판_소품", "img_sign_secret_modify.png", (48, 382, 220, 438), radius=9)
    crop("03_간판_소품", "img_sign_no_report.png", (48, 434, 222, 487), radius=9)
    crop("03_간판_소품", "img_sign_secret_discount.png", (650, 102, 854, 252), radius=11)
    crop("03_간판_소품", "img_cash_box.png", (682, 454, 858, 632), radius=12)
    crop("03_간판_소품", "img_cash_only_board.png", (600, 610, 773, 708), radius=16)
    crop("03_간판_소품", "img_tool_chest_reference.png", (0, 482, 207, 708), radius=10)
    crop("03_간판_소품", "img_hanging_shoes_reference.png", (592, 188, 864, 454), radius=10)
    crop("02_캐릭터_오브젝트", "img_shopkeeper_reference.png", (204, 202, 656, 708), radius=18)
    crop("02_캐릭터_오브젝트", "img_modified_shoes_reference.png", (228, 548, 564, 708), radius=14)

    top_ui = [
        ("img_top_back.png", (0, 0, 94, 94), 15),
        ("img_top_coin_bar.png", (294, 0, 562, 92), 21),
        ("img_top_gem_bar.png", (576, 0, 864, 92), 21),
    ]
    for filename, box, radius in top_ui:
        crop("06_상단UI", filename, box, radius=radius)

    card_names = [
        "attack",
        "health",
        "attack_speed",
        "projectile_speed",
        "boss_damage",
        "coin_bonus",
        "health_regen",
        "summon_sahur",
        "bombardilo_support",
    ]
    card_boxes = [
        (48, 709, 294, 1038),
        (302, 709, 540, 1038),
        (545, 709, 767, 1038),
        (48, 1041, 294, 1353),
        (302, 1041, 540, 1353),
        (545, 1041, 767, 1353),
        (48, 1355, 294, 1663),
        (302, 1355, 540, 1663),
        (545, 1355, 767, 1663),
    ]
    for name, box in zip(card_names, card_boxes, strict=True):
        crop(
            "05_업그레이드_완성카드",
            f"img_upgrade_card_{name}.png",
            box,
            radius=13,
            mask_inset=(10, 3, 6, 3),
        )

    bottom_ui = [
        ("img_bottom_back.png", (44, 1670, 150, 1792), 15),
        ("img_bottom_ad_refresh.png", (219, 1670, 531, 1792), 17),
        ("img_bottom_free_refresh.png", (546, 1670, 831, 1792), 17),
    ]
    for filename, box, radius in bottom_ui:
        crop("07_하단UI", filename, box, radius=radius)

    shared_assets = {
        "img_lobby_upgrade_bg.png": "img_shop_upgrade_card_bg.png",
        "img_lobby_upgrade_sword.png": "img_upgrade_attack.png",
        "img_lobby_upgrade_heart.png": "img_upgrade_health.png",
        "img_lobby_upgrade_speed.png": "img_upgrade_attack_speed.png",
        "img_lobby_upgrade_rocket.png": "img_upgrade_projectile_speed.png",
        "img_lobby_upgrade_boss.png": "img_upgrade_boss_damage.png",
        "img_lobby_upgrade_coin.png": "img_upgrade_coin_bonus.png",
        "img_lobby_upgrade_heal.png": "img_upgrade_health_regen.png",
        "img_lobby_upgrade_sahur.png": "img_upgrade_summon_sahur.png",
        "img_lobby_upgrade_croco.png": "img_upgrade_bombardilo_support.png",
        "src_arrow.png": "src_value_arrow.png",
        "src_coin.png": "src_coin.png",
        "src_diamond.png": "src_diamond.png",
        "img_nav_bar.png": "img_nav_bar.png",
        "img_nav_coin.png": "img_nav_coin.png",
        "img_nav_diamond.png": "img_nav_diamond.png",
        "img_nav_arrow.png": "img_nav_back_arrow.png",
    }
    for source_name, target_name in shared_assets.items():
        shutil.copy2(shared_root / source_name, folders["04_업그레이드_공용"] / target_name)

    card_files = sorted(folders["05_업그레이드_완성카드"].glob("*.png"))
    make_contact_sheet(card_files, folders["99_검수"] / "preview_upgrade_cards.png")

    manifest = {
        "source": str(source),
        "canvas": {"width": image.width, "height": image.height},
        "unity": {
            "textureType": "Sprite (2D and UI)",
            "alpha": True,
            "recommendedCardMode": "Use the blank card and icons from 04_업그레이드_공용; render labels and values with TextMeshPro.",
        },
        "elements": crops,
        "generatedCutouts": [
            "02_캐릭터_오브젝트/img_shopkeeper_cutout.png",
            "02_캐릭터_오브젝트/img_modified_shoes_cutout.png",
            "03_간판_소품/img_hanging_shoes_cutout.png",
            "03_간판_소품/img_cash_box_cutout.png",
            "01_배경/img_shop_background_clean.png",
        ],
    }
    (target_root / "layout_manifest.json").write_text(
        json.dumps(manifest, ensure_ascii=False, indent=2), encoding="utf-8"
    )
    readme = (
        "인게임 신발 개조상 리소스 구성\n\n"
        "00_전체시안: 최종 상점 시안 원본. 배치와 색감 참고용.\n"
        "01_배경: UI와 NPC가 없는 상점 배경 및 원본 상단 장면 참고본.\n"
        "02_캐릭터_오브젝트: 투명 상인, 개조 신발 작업대, 원본 크롭 참고본.\n"
        "03_간판_소품: 간판 크롭, 투명 매달린 신발, 투명 현금 상자.\n"
        "04_업그레이드_공용: 빈 카드, 업그레이드 아이콘, 코인/다이아/화살표.\n"
        "05_업그레이드_완성카드: 최종 시안에서 분리한 9개 완성 카드.\n"
        "06_상단UI, 07_하단UI: 최종 시안에서 분리한 내비게이션 요소.\n"
        "98_생성원본: 배경 제거 전 크로마키 원본. Unity 적용 대상이 아님.\n"
        "99_검수: 패널/카드/투명 요소 및 재조합 미리보기. Unity 적용 대상이 아님.\n\n"
        "권장 조립 순서: 배경 -> 매달린 신발/현금 상자 -> 상인 -> 신발 작업대 -> UI.\n"
        "업그레이드 카드는 04 폴더의 빈 카드와 아이콘을 사용하고, 이름/레벨/수치/가격은 TextMeshPro로 표시하는 방식을 권장.\n"
        "PNG Import Settings: Texture Type = Sprite (2D and UI), Alpha Is Transparency = On.\n"
    )
    (target_root / "README_사용법.txt").write_text(readme, encoding="utf-8")


def paste_scaled(canvas: Image.Image, source: Path, width: int, position: tuple[int, int]) -> None:
    image = Image.open(source).convert("RGBA")
    height = round(image.height * width / image.width)
    image = image.resize((width, height), Image.Resampling.LANCZOS)
    canvas.alpha_composite(image, position)


def make_shop_ai_previews(target_root: Path) -> None:
    background = target_root / "01_배경" / "img_shop_background_clean.png"
    character = target_root / "02_캐릭터_오브젝트" / "img_shopkeeper_cutout.png"
    workbench = target_root / "02_캐릭터_오브젝트" / "img_modified_shoes_cutout.png"
    hanging = target_root / "03_간판_소품" / "img_hanging_shoes_cutout.png"
    cash_box = target_root / "03_간판_소품" / "img_cash_box_cutout.png"
    files = [character, workbench, hanging, cash_box]
    if not background.exists() or not all(file.exists() for file in files):
        return

    review = target_root / "99_검수"
    make_contact_sheet(files, review / "preview_transparent_cutouts.png")
    composition = Image.open(background).convert("RGBA")
    paste_scaled(composition, hanging, 250, (660, 100))
    paste_scaled(composition, cash_box, 180, (710, 430))
    paste_scaled(composition, character, 530, (205, 235))
    paste_scaled(composition, workbench, 830, (55, 555))
    composition.save(review / "preview_shop_reassembled.png", optimize=True)


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--comic", type=Path, required=True)
    parser.add_argument("--shop", type=Path, required=True)
    parser.add_argument("--target-root", type=Path, required=True)
    parser.add_argument("--shared-ui", type=Path, required=True)
    args = parser.parse_args()

    comic_root = args.target_root / "기획서" / "스토리" / "인트로_4컷"
    shop_root = args.target_root / "UI" / "신버전" / "인게임_신발개조상"
    extract_comic(args.comic, comic_root)
    extract_shop(args.shop, args.shared_ui, shop_root)
    make_shop_ai_previews(shop_root)
    print(comic_root)
    print(shop_root)


if __name__ == "__main__":
    main()
