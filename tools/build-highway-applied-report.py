"""Publish native Highway verification captures, excluding all preference backups."""
import json
import shutil
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
EVIDENCE = ROOT / "outputs/highway-three-lane-rebuild-2026-09-26"
SITE = ROOT / "outputs/highway-concepts-2026-09-26/site/three-lanes/applied"
PREVIEWS = ROOT / "tmp/image-previews/highway-applied-2026-09-26"
COHORTS = {"main": "play-v5", "bypass": "play-v6"}


def main():
    runs = [json.loads((EVIDENCE / COHORTS[name] / name / "result.json").read_text()) for name in ("main", "bypass")]
    if any(r["outcome"] != "clear" for r in runs):
        raise RuntimeError("Keep failed cohorts as evidence; do not publish them as clear verification")
    SITE.mkdir(parents=True, exist_ok=True)
    PREVIEWS.mkdir(parents=True, exist_ok=True)
    shots = [
        ("main", 40, "3차선 시작", "차로 폭 3.6m. 노란 실선과 중앙분리대 너머로 옆 도로가 보입니다."),
        ("main", 180, "본선 · 정면 차량", "승용차·트럭·버스가 모두 정면으로 다가옵니다. 빈 차로로 피하며 전투합니다."),
        ("main", 1020, "곡선 도로", "차선과 중앙분리대가 실제 도로의 곡선·높이를 따라 이어집니다."),
        ("main", 1740, "두 번째 본선", "본선으로 계속 진행하면 차량과 적을 상대합니다."),
        ("main", 1350, "통나무 트럭", "트럭도 정면 접근. 중앙·오른쪽 통나무를 피해 왼쪽으로 이동합니다."),
        ("bypass", 180, "첫 회복 우회로", "도로를 가리던 산·나무를 정리했습니다. 오른쪽 분기를 타면 회복 우회로로 진입합니다."),
        ("bypass", 1830, "두 번째 회복 우회로", "두 번째 분기도 실제 주행으로 확인했습니다. 회복한 뒤 본선에 다시 합류합니다."),
        ("main", 2790, "고속도로 끝", "기존의 휴게소 방향 진행과 마지막 전투를 유지합니다."),
    ]
    gallery = []
    for route, distance, title, description in shots:
        name = f"{route}-{distance:04}.png"
        source = EVIDENCE / COHORTS[route] / route / f"d-{distance:04}.png"
        shutil.copy2(source, SITE / name)
        if not (PREVIEWS / name).exists():
            shutil.copy2(source, PREVIEWS / name)
        gallery.append(dict(image=name, title=title, description=description, distance=distance))
    for route in ("main", "bypass"):
        shutil.copy2(EVIDENCE / COHORTS[route] / route / "result.json", SITE / f"{route}-result.json")
    template = (ROOT / "tools/highway-applied-report.html").read_text(encoding="utf-8")
    output = template.replace("__GALLERY__", json.dumps(gallery, ensure_ascii=False))
    output = output.replace("__MAIN_CARS__", str(runs[0]["seenCars"])).replace("__BYPASS_CARS__", str(runs[1]["seenCars"]))
    output = output.replace("__DIRECTION__", str(sum(r["reversedFrames"] + r["wrongTruckDirectionFrames"] for r in runs)))
    output = output.replace("__OVERLAP__", str(sum(r["roadblockOverlapFrames"] for r in runs)))
    output = output.replace("__WALLS__", str(sum(r["fullWallFrames"] for r in runs)))
    (SITE / "index.html").write_text(output, encoding="utf-8")
    print(SITE / "index.html")


if __name__ == "__main__":
    main()
