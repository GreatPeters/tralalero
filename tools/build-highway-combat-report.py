"""Build the public screenshot report without exposing any save backups."""
import json
import shutil
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
EVIDENCE = ROOT / "outputs/highway-combat-revision-2026-09-27"
SITE = ROOT / "outputs/highway-concepts-2026-09-26/site/highway/combat-revision"
PREVIEWS = ROOT / "tmp/image-previews/highway-combat-revision-2026-09-27"


def main():
    SITE.mkdir(parents=True, exist_ok=True)
    PREVIEWS.mkdir(parents=True, exist_ok=True)
    shots = [
        ("random", "랜덤 보너스", "굵은 남색 간판, 황금 별, 조명과 입체 물음표 패널로 다시 만들었습니다.", "play-v4/jam-hipass/d-1418.png", "실제 주행 화면"),
        ("combat", "차량 돌파", "적 차량 233 → 351대. 앞줄을 부수면 그 차로가 열립니다.", "play-v4/jam-hipass/d-1358.png", "실제 주행 화면"),
        ("toll", "요금소 직접 획득", "좌우 보너스 중 하나에 직접 닿아 획득합니다. 요금소 팝업은 없습니다.", "toll-final.png", "지지대 위치 보정 후 실제 게임 배치 확인용 캡처"),
        ("rush", "상어 가속", "뻥 뚫린 길에서 상어 속도 ×1.5. 합류하면 속도와 스피드라인이 원래대로 돌아옵니다.", "play-v5/open-cash/d-0650.png", "실제 주행 화면"),
        ("log", "큰 통나무", "길이 3.4 → 5.1m. 커진 모양에 맞춰 충돌 판정도 바꿨습니다.", "play-v4/jam-hipass/d-1500.png", "실제 주행 화면"),
        ("police", "한국 경찰차", "흰색 세단에 청색·황색 도색, 경찰 표기와 경광등을 적용했습니다.", "police-native.png", "실제 사용 모델의 Unity 렌더"),
    ]
    gallery = []
    for key, title, description, relative, label in shots:
        source = EVIDENCE / relative
        if not source.exists():
            raise RuntimeError(f"Missing native evidence: {source}")
        shutil.copy2(source, SITE / f"{key}.png")
        local = PREVIEWS / ("toll-final.png" if key == "toll" else f"{key}-revision.png")
        if not local.exists():
            shutil.copy2(source, local)
        gallery.append(dict(key=key, title=title, description=description, label=label))
    records = []
    for cohort, route, title in (("play-v4", "first-accident", "첫 진입 사고"), ("play-v4", "jam-hipass", "정체 → 하이패스"), ("play-v5", "open-cash", "가속 구간 → 현금"), ("no-fire-v1", "open-cash", "무사격 회피 검사")):
        path = EVIDENCE / cohort / route / "result.json"
        if path.exists():
            row = json.loads(path.read_text(encoding="utf-8"))
            records.append(dict(title=title, outcome=row["outcome"], elapsed=round(row["elapsed"], 1), hp=row["initialHp"], attack=round(row["initialAttack"]), cars=row["seenCars"], noFire=row.get("noFire", False), projectileHits=row.get("VehicleProjectileHits", 0)))
    template = (ROOT / "tools/highway-combat-report.html").read_text(encoding="utf-8")
    template = template.replace("__SHOTS__", json.dumps(gallery, ensure_ascii=False)).replace("__RESULTS__", json.dumps(records, ensure_ascii=False))
    (SITE / "index.html").write_text(template, encoding="utf-8")
    (SITE / "results.json").write_text(json.dumps(records, ensure_ascii=False, indent=2), encoding="utf-8")
    banner = '<a data-latest-combat="true" href="http://127.0.0.1:8776/highway/combat-revision/" style="display:block;background:#e0ad35;color:#112342;padding:14px 20px;text-align:center;font-weight:bold">이 페이지는 이전 기록입니다. 최신 고속도로 반영 화면 보기 →</a>'
    for legacy in (SITE.parent / "implementation/index.html", SITE.parent / "fixes/index.html", ROOT / "tools/highway-chapter2-report.html", ROOT / "tools/highway-visual-fixes-report.html"):
        content = legacy.read_text(encoding="utf-8")
        if 'data-latest-combat=' not in content:
            legacy.write_text(content.replace("<body>", "<body>" + banner, 1), encoding="utf-8")
    print(SITE / "index.html")


if __name__ == "__main__":
    main()
