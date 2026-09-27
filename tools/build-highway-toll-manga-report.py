"""Publish the ten image-only proposals and actual native revision captures."""
from pathlib import Path
import json
import shutil
from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
EVIDENCE = ROOT / "outputs/highway-toll-manga-2026-09-27"
SITE = ROOT / "outputs/highway-concepts-2026-09-26/site/highway/toll-manga"
PREVIEWS = ROOT / "tmp/image-previews/highway-toll-manga-2026-09-27"


def main():
    gallery = SITE / "bonus"
    gallery.mkdir(parents=True, exist_ok=True)
    cards = []
    for number in range(1, 11):
        tag = f"{number:02}"
        source = EVIDENCE / "concepts" / f"bonus-{tag}{'-v2' if number == 5 else ''}.png"
        shutil.copy2(source, gallery / f"{tag}.png")
        with Image.open(source) as image:
            image.thumbnail((510, 1100))
            image.convert("RGB").save(gallery / f"{tag}.webp", quality=89)
        cards.append(f'<a class="tile" href="{tag}.png" data-number="{tag}" aria-label="{number}번 시안 크게 보기"><span>{tag}</span><img src="{tag}.webp" alt="보너스 시안 {number}" loading="lazy" width="510" height="1100"></a>')
    html = (ROOT / "tools/highway-bonus-gallery.html").read_text(encoding="utf-8")
    (gallery / "index.html").write_text(html.replace("__CARDS__", "\n".join(cards)), encoding="utf-8")

    shots = [name for name in ("toll-game", "toll-close", "rush-game", "rush-off") if (EVIDENCE / f"{name}.png").exists()]
    for name in shots:
        shutil.copy2(EVIDENCE / f"{name}.png", SITE / f"{name}.png")
        local = PREVIEWS / f"{name}-verified.png"
        if not local.exists():
            shutil.copy2(EVIDENCE / f"{name}.png", local)
    page = (ROOT / "tools/highway-toll-manga-report.html").read_text(encoding="utf-8")
    (SITE / "index.html").write_text(page, encoding="utf-8")
    manifest = json.loads((EVIDENCE / "concepts/prompts.json").read_text(encoding="utf-8"))
    manifest["selected05"] = dict(file="bonus-05-v2.png", reason="Repair generated transparent/black sky pixels; preserve the design", generator="built-in image_gen")
    (EVIDENCE / "concepts/manifest.json").write_text(json.dumps(manifest, ensure_ascii=False, indent=2), encoding="utf-8")
    old = SITE.parent / "combat-revision/index.html"
    banner = '<a data-toll-manga="true" href="../toll-manga/" style="display:block;padding:14px;text-align:center;background:#e0ad35;color:#112342;font-weight:bold">새 요금소·집중선 적용 화면 / 보너스 시안 10개 →</a>'
    text = old.read_text(encoding="utf-8")
    if 'data-toll-manga=' not in text:
        old.write_text(text.replace("<body>", "<body>" + banner, 1), encoding="utf-8")
    print(SITE)


if __name__ == "__main__":
    main()
