"""Publish only inspected native PNGs to a local gallery and verify served bytes."""
from pathlib import Path
import hashlib
import html
import json
import subprocess
import sys
import urllib.request

ROOT = Path(__file__).resolve().parent.parent
OUTPUT = ROOT / "outputs/essential-proposals-2026-10-06"
PREVIEWS = ROOT / "tmp/image-previews/essential-proposals-2026-10-06"
GALLERY = OUTPUT / "gallery"
GALLERY.mkdir(exist_ok=True)
PREVIEWS.mkdir(parents=True, exist_ok=True)
FILES = [
    (OUTPUT / "options-native.png", "옵션과 Google Play 로그인  실제 Unity UI"),
    (OUTPUT / "delete-confirm-final.png", "계정 탈퇴 확인창  삭제를 실행하지 않은 검수 화면"),
    (OUTPUT / "options-3x4-final.png", "3대4 화면  테스트 계정 표시"),
    (OUTPUT / "delete-confirm-3x4-final.png", "3대4 화면  계정 탈퇴 확인"),
    (OUTPUT / "mall-native/1F-70.png", "5챕터 현재 툰 매장  지정 위치의 Unity 렌더"),
    (OUTPUT / "style/Noryangjin_MapTool_Mode_SR18_Revamp-after.png", "1챕터 현재 목재와 바다  Edit 카메라 렌더"),
    (OUTPUT / "style/HighWay-after.png", "2챕터 현재 도로  Edit 카메라 렌더"),
    (OUTPUT / "style/RestStop-after.png", "3챕터 현재 입구  Edit 카메라 렌더"),
    (OUTPUT / "style/Jamsil-after.png", "4챕터 현재 입구  Edit 카메라 렌더"),
]
cards = []
for index, (source, caption) in enumerate(FILES, 1):
    name = f"{index:02}-{source.name}"
    data = source.read_bytes()
    for directory in (GALLERY, PREVIEWS):
        target = directory / name
        if target.exists() and target.read_bytes() != data:
            raise RuntimeError(f"Preserve existing preview: {target}")
        if not target.exists():
            target.write_bytes(data)
    cards.append(f'<figure><a href="{html.escape(name)}"><img src="{html.escape(name)}"></a><figcaption>{html.escape(caption)}</figcaption></figure>')
page = """<!doctype html><html lang="ko"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>필수 제안 적용과 계정 UI 검수</title><style>body{background:#eee7d7;color:#253b49;font:17px sans-serif;margin:32px}h1{font-size:27px}.grid{display:grid;grid-template-columns:repeat(auto-fit,minmax(260px,1fr));gap:24px}figure{margin:0;background:white;padding:14px}img{width:100%;height:480px;object-fit:contain}figcaption{margin-top:12px;line-height:1.5}p{max-width:900px;line-height:1.6}</style><h1>필수 제안 적용과 계정 UI 검수</h1><p>2026년 10월 6일. 원본 Unity PNG를 그대로 제공합니다. 계정 UI는 Play 화면을 카메라로 렌더한 검수이며 실제 Google 로그인은 아닙니다. 탈퇴 확인에는 가짜 계정을 사용했고 삭제를 실행하지 않았습니다. 챕터 화면은 지정 위치 또는 Edit 렌더이며 전체 주행이나 휴대폰 검증이 아닙니다. 5챕터 Edit 시작 카메라의 가림 화면과 갱신되지 않은 GameView 이미지는 이 갤러리에서 제외했습니다.</p><div class="grid">""" + "".join(cards) + "</div></html>"
(GALLERY / "index.html").write_text(page, encoding="utf-8")
port = 18886
base = f"http://127.0.0.1:{port}/"
try:
    urllib.request.urlopen(base, timeout=2).read()
except Exception:
    log = (OUTPUT / "gallery-server.log").open("a", encoding="utf-8")
    process = subprocess.Popen([sys.executable, "-m", "http.server", str(port), "--bind", "127.0.0.1", "--directory", str(GALLERY)], stdout=log, stderr=log, creationflags=getattr(subprocess, "CREATE_NO_WINDOW", 0))
    (OUTPUT / "gallery-server-pid.txt").write_text(str(process.pid), encoding="ascii")
    import time
    time.sleep(0.5)
rows = []
for target in GALLERY.glob("*.png"):
    served = urllib.request.urlopen(base + target.name, timeout=5).read()
    if served != target.read_bytes():
        raise RuntimeError(f"HTTP mismatch: {target}")
    rows.append({"file": target.name, "bytes": len(served), "sha256": hashlib.sha256(served).hexdigest()})
if urllib.request.urlopen(base, timeout=5).read() != (GALLERY / "index.html").read_bytes():
    raise RuntimeError("Gallery index mismatch")
(OUTPUT / "gallery-verification.json").write_text(json.dumps({"url": base, "verified": True, "images": rows}, ensure_ascii=False, indent=2), encoding="utf-8")
print(base, f"{len(rows)} original PNGs verified")
