"""Copy native evidence without overwriting existing previews and write its index."""
import shutil
from pathlib import Path

root = Path(__file__).resolve().parents[1]
out = root / "tmp/image-previews/chapter4-reference-2026-10-02"
out.mkdir(parents=True, exist_ok=True)
evidence = root / "outputs/chapter4-reference-2026-10-02"
rows = []
for station in ("0230", "0390", "0990", "1410"):
    for label, directory in (("before", "before-Jamsil-20261002T105749575"), ("after", "accepted-Jamsil-20261002T111546118")):
        source = evidence / directory / f"{station}.png"
        target = out / f"{label}-{station}.png"
        if not target.exists():
            shutil.copy2(source, target)
        elif source.read_bytes() != target.read_bytes():
            raise RuntimeError(f"Preserve existing preview: {target}")
    rows.append(f'<section><h2>{int(station)}m</h2><div class="pair"><figure><a href="before-{station}.png"><img src="before-{station}.png" alt="수정 전 {station}m"></a><figcaption>수정 전</figcaption></figure><figure><a href="after-{station}.png"><img src="after-{station}.png" alt="적용 후 {station}m"></a><figcaption>적용 후</figcaption></figure></div></section>')
html = '''<!doctype html><html lang="ko"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>잠실 거리 · 실제 Unity 전후 비교</title><style>
*{box-sizing:border-box}body{margin:0;background:#f1eee6;color:#1b3032;font:16px/1.65 system-ui,sans-serif}main{max-width:1120px;margin:auto;padding:48px 24px}h1{font-size:36px;letter-spacing:-1.3px;margin:12px 0}h2{font-size:20px;margin-top:40px}p{max-width:820px}small{letter-spacing:2px}a{color:inherit}.pair{display:grid;grid-template-columns:1fr 1fr;gap:20px}figure{margin:0}img{display:block;width:100%;height:auto;border:1px solid #c6cdc5}figcaption{font-weight:650;margin-top:8px}.note{padding:16px 20px;border-left:3px solid #568274;background:#e2e6de}@media(max-width:600px){main{padding:24px 12px}h1{font-size:27px}.pair{gap:8px}}
</style><main><small>TRALALERO SHOOTER / 2026.10.02</small><h1>잠실 거리, 실제 Unity 전후 비교</h1><p>연속된 상점, 쇼윈도·간판·소품, 보행자 흐름과 붉은 포장. 기존 타워·운동화와 전투 경로를 유지한 챕터 4 적용 화면입니다.</p><p class="note">아래 비교는 같은 거리·카메라 설정의 Unity 편집 모드 촬영입니다. UI를 숨겼고 플레이 성공 증거와는 구분합니다. 이미지를 누르면 원본 PNG가 열립니다. 백화점은 적용 위치 답변 대기 중입니다.</p>''' + "".join(rows) + "</main></html>"
(out / "index.html").write_text(html, encoding="utf-8")
print(out)
