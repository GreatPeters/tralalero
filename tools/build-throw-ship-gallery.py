"""Publish unmodified native Unity captures to the existing local gallery."""
from pathlib import Path
import shutil
import json

root=Path(__file__).resolve().parents[1]
source=root/'map-concepts/throw-ship-refinement-2026-09-20/play-final'
preview=root/'tmp/image-previews/throw-ship-refinement-2026-09-20'
gallery=root/'tmp/image-previews/bonus-gallery-2026-09-19/throw-ship-refinement'
preview.mkdir(parents=True,exist_ok=True)
gallery.mkdir(parents=True,exist_ok=True)
items=[('FatMan-frame-030.png','뚱보 · 투척 준비','상자 가장자리를 손에 맞추고 얼굴 아래에서 들도록 조정했습니다.'),('FatMan-launch-1.png','뚱보 · 발사 순간','얼굴을 가리지 않으며 기존 투척 동작으로 발사합니다.'),('Ship-frame-000.png','배 · 대기','선수와 포구가 길 쪽을 향합니다.'),('Ship-frame-020.png','배 · 발사','선체 방향을 유지하며 FirePos의 정면으로 CannonBall을 발사합니다.')]
cards=[]
for filename,title,caption in items:
    for destination in (preview/filename,gallery/filename):
        if destination.exists():
            raise FileExistsError(destination)
        shutil.copy2(source/filename,destination)
    cards.append(f'<figure><a href="{filename}" target="_blank"><img src="{filename}" alt="{title}"></a><figcaption><b>{title}</b><p>{caption}</p></figcaption></figure>')
html='''<!doctype html><html lang="ko"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>뚱보 상자 · 배 방향 수정</title><style>body{margin:0;background:#f2f4f6;color:#172635;font:16px/1.6 "Segoe UI",sans-serif}main{max-width:1040px;margin:35px auto;padding:0 24px}h1{font-size:28px}header p{color:#50606e}.grid{display:grid;grid-template-columns:1fr 1fr;gap:24px}figure{margin:0;background:white;border:1px solid #d9e0e6;border-radius:10px;overflow:hidden}img{display:block;width:100%}figcaption{padding:16px}figcaption p{margin:5px 0;color:#526272;font-size:14px}@media(max-width:700px){.grid{grid-template-columns:1fr}}</style><main><header><h1>뚱보 상자 · 배 방향 수정</h1><p>실제 Unity Play Mode 촬영입니다. 이미지를 누르면 원본이 새 탭으로 열립니다.</p></header><div class="grid">'''+''.join(cards)+'</div></main></html>'
(gallery/'index.html').write_text(html,encoding='utf-8')
(preview/'index.html').write_text(html,encoding='utf-8')
print(json.dumps({'images':len(items),'url':'http://127.0.0.1:6753/throw-ship-refinement/'}))
