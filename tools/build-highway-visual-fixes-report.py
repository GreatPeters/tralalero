"""Publish selected native evidence only; never copy QA preference backups."""
import json
import shutil
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
EVIDENCE = ROOT / "outputs/highway-visual-fixes-2026-09-27"
SITE = ROOT / "outputs/highway-concepts-2026-09-26/site/highway/fixes"
PREVIEWS = ROOT / "tmp/image-previews/highway-visual-fixes-2026-09-27"


def main():
    SITE.mkdir(parents=True, exist_ok=True)
    PREVIEWS.mkdir(parents=True, exist_ok=True)
    old = ROOT / "outputs/highway-chapter2-2026-09-27/play-v4/jam-hipass"
    current = EVIDENCE / "play-v2"
    visuals = EVIDENCE / "play-v3/jam-hipass"
    shots = [
        ("overview", "갈림길 전체", "초록색은 왼쪽 정체 구간, 빨간색은 오른쪽 뻥 뚫린 길입니다. 두 길이 갈라지는 실제 게임 모습입니다.", old / "d-0520.png", visuals / "d-0490.png"),
        ("fork", "분기 도로", "왼쪽은 초록색, 오른쪽은 빨간색. 겹치던 차선을 정리하고 지선이 완만하게 벌어지도록 바꿨습니다.", old / "d-0520.png", visuals / "d-0520.png"),
        ("accident", "사고 차량", "과한 찌그러짐을 걷어내고 원래 차체와 바퀴 형태를 복원했습니다. 사고 위치와 비상 삼각대는 유지합니다.", old / "d-0205.png", current / "first-accident/d-0205.png"),
        ("entry", "초반 교통", "초반에 차량 8대를 추가했습니다. 기존 차량은 등장 시점을 1.5초 앞당기고, 멀리 있는 차체를 숨기던 제한도 해제했습니다.", old / "d-0030.png", current / "first-accident/d-0030.png"),
        ("traffic", "차체·바퀴", "경차와 SUV를 추가하고 차량 하부의 도로 접지 높이를 보정했습니다. 움직이는 차량의 외형과 충돌 크기도 함께 맞췄습니다.", old / "d-0650.png", visuals / "d-0650.png"),
    ]
    gallery = []
    for key, title, description, before, after in shots:
        if not after.exists():
            raise RuntimeError(f"Native capture not ready: {after}")
        shutil.copy2(before, SITE / f"{key}-before.png")
        shutil.copy2(after, SITE / f"{key}-after.png")
        cohort = "play-v3" if key in ("overview", "fork", "traffic") else "play-v2"
        secondary = PREVIEWS / f"{key}-{cohort}.png"
        if not secondary.exists():
            shutil.copy2(after, secondary)
        gallery.append(dict(key=key, title=title, description=description))
    shutil.copy2(EVIDENCE / "vehicle-library.png", SITE / "vehicles.png")
    results = []
    for route in ("first-accident", "jam-hipass", "open-cash"):
        path = current / route / "result.json"
        if path.exists():
            row = json.loads(path.read_text(encoding="utf-8"))
            results.append({key: row[key] for key in ("label", "outcome", "elapsed", "initialHp", "initialAttack", "health", "wrongDirection", "overlapFrames", "fullWallFrames", "hiddenBodyFrames")})
    template = (ROOT / "tools/highway-visual-fixes-report.html").read_text(encoding="utf-8")
    template = template.replace("__GALLERY__", json.dumps(gallery, ensure_ascii=False)).replace("__RESULTS__", json.dumps(results, ensure_ascii=False))
    (SITE / "index.html").write_text(template, encoding="utf-8")
    (SITE / "results.json").write_text(json.dumps(results, ensure_ascii=False, indent=2), encoding="utf-8")
    print(SITE / "index.html")


if __name__ == "__main__":
    main()
