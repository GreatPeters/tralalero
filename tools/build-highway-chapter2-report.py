"""Build the visual acceptance report from explicit successful native cohorts."""
import argparse
import json
import shutil
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
EVIDENCE = ROOT / "outputs/highway-chapter2-2026-09-27"
REFERENCE = ROOT / "outputs/highway-concepts-2026-09-26/site/highway"
SITE = REFERENCE / "implementation"
PREVIEW = ROOT / "tmp/image-previews/highway-chapter2-2026-09-27"


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--jam", default="play-v4")
    parser.add_argument("--open", default="play-v4")
    parser.add_argument("--cash", default="play-v5")
    parser.add_argument("--open-cash", default="play-v6")
    args = parser.parse_args()
    cohorts = {"jam-hipass": args.jam, "jam-cash": args.cash, "open-hipass": args.open, "open-cash": args.open_cash}
    rows = []
    for route, cohort in cohorts.items():
        result = json.loads((EVIDENCE / cohort / route / "result.json").read_text())
        if result["outcome"] != "clear":
            raise RuntimeError(f"{cohort}/{route} is not a clear; preserve failed evidence")
        rows.append(dict(route=route, cohort=cohort, elapsed=round(result["elapsed"], 1),
                         hp=round(result["health"]), attack=round(result["initialAttack"]),
                         startHp=round(result["initialHp"]), cars=result["seenCars"],
                         wrong=result["wrongDirection"], overlaps=result["overlapFrames"], walls=result["fullWallFrames"]))
    SITE.mkdir(parents=True, exist_ok=True)
    PREVIEW.mkdir(parents=True, exist_ok=True)
    images = [
        ("accident", "첫 사고 정체", "가로로 선 사고차와 옆 차. 약한 옆 차를 부수면 그 차로가 열립니다.",
         "accident-gate.jpg", args.jam, "jam-hipass", "d-0205.png"),
        ("fork", "두 갈래 선택", "60m 앞에서 느려지고, 카드로 길을 고릅니다. 5초 무응답이면 정체 구간입니다.",
         "fork-popup.jpg", args.open, "open-hipass", "popup-1.png"),
        ("jam", "정체 구간", "느린 차를 상대하며 전진합니다. 접촉 피해는 남은 체력, 코인은 1.5배입니다.",
         "left-jam.jpg", args.jam, "jam-hipass", "d-0650.png"),
        ("open", "뻥 뚫린 길", "차량은 빠르게 남쪽으로 다가옵니다. 접촉은 최대 체력의 25%, 코인은 0.8배입니다.",
         "right-open.jpg", args.open, "open-hipass", "d-0650.png"),
        ("swarm", "순찰차·견인차", "경광등을 켠 차량들이 예고 없이 엇갈려 접근합니다.",
         "swarm.jpg", args.jam, "jam-hipass", "d-1358.png"),
        ("log", "통나무 한 개", "반대편에서 북쪽으로 가는 트럭의 통나무 한 개가 넘어옵니다. 내 도로 차는 잠시 멈춥니다.",
         "log-single.jpg", args.open, "open-hipass", "d-1500.png"),
        ("work", "공사 구간", "콘으로 닫힌 차로와 작업자 둘, 신호수, LED 작업차를 배치했습니다.",
         "work.jpg", args.open, "open-hipass", "d-1780.png"),
        ("random", "랜덤 보너스", "물음표 카드가 지나가는 순간 결과를 정합니다. 한 판에 두 번 등장합니다.",
         "randomwall.jpg", args.open, "open-hipass", "d-1440.png"),
        ("exit", "요금소 출구", "하이패스·현금 중 선택하고 유니크 보너스를 받습니다. 차단기는 없습니다.",
         "toll-exit.jpg", args.open, "open-hipass", "popup-2.png"),
    ]
    gallery = []
    for key, title, description, reference, cohort, route, shot in images:
        native = EVIDENCE / cohort / route / shot
        shutil.copy2(native, SITE / f"{key}.png")
        shutil.copy2(REFERENCE / reference, SITE / f"{key}-reference.jpg")
        direct = PREVIEW / f"{key}-{cohort}.png"
        if not direct.exists():
            shutil.copy2(native, direct)
        gallery.append(dict(key=key, title=title, description=description, native=f"{key}.png", reference=f"{key}-reference.jpg"))
    for row in rows:
        shutil.copy2(EVIDENCE / row["cohort"] / row["route"] / "result.json", SITE / f'{row["route"]}.json')
    template = (ROOT / "tools/highway-chapter2-report.html").read_text(encoding="utf-8")
    template = template.replace("__GALLERY__", json.dumps(gallery, ensure_ascii=False)).replace("__RUNS__", json.dumps(rows, ensure_ascii=False))
    (SITE / "index.html").write_text(template, encoding="utf-8")
    (SITE / "evidence.json").write_text(json.dumps(dict(runs=rows, gallery=gallery), ensure_ascii=False, indent=2), encoding="utf-8")
    print(SITE / "index.html")


if __name__ == "__main__":
    main()
