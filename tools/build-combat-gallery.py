"""Copy native Unity evidence into the existing local preview gallery."""
from pathlib import Path
import json
import shutil

root = Path(__file__).resolve().parents[1]
evidence = root / 'map-concepts/combat-route-fixes-2026-09-20'
preview = root / 'tmp/image-previews/combat-route-2026-09-20'
gallery = root / 'tmp/image-previews/bonus-gallery-2026-09-19/combat-route'
preview.mkdir(parents=True, exist_ok=True)
gallery.mkdir(parents=True, exist_ok=True)
camera = json.loads((evidence / 'camera-playmode-probe.json').read_text())
items = [
    ('Guard-launch-1.png', evidence / 'play/Guard-launch-1.png', '가드 · Arrow2 발사', '실제 발사 순간. 손잡이를 오른손에 고정한 조준 자세.'),
    ('FatMan-frame-030.png', evidence / 'play/FatMan-frame-030.png', '뚱보 · 던지기 준비', '걷지 않고 제자리에서 상자를 들어 올립니다.'),
    ('FatMan-launch-1.png', evidence / 'play/FatMan-launch-1.png', '뚱보 · 직선 투척', '약 5초 거리에서 시작. 위치 이동 0, 반복 투척 확인.'),
    ('Ship-frame-020.png', evidence / 'play/Ship-frame-020.png', '배 · CannonBall 발사', '선체 길이 9.5m. FirePos에서 실제 포탄이 출발합니다.'),
]
for file, title in zip(camera['files'], ['첫 번째 오르막', '첫 번째 내리막', '두 번째 오르막', '두 번째 내리막']):
    items.append((Path(file).name, root / file, title, '기존 플레이 카메라에서 촬영한 경사 구간입니다.'))
cards=[]
for filename, source, title, caption in items:
    if not source.is_file():
        raise FileNotFoundError(source)
    shutil.copy2(source, preview / filename)
    shutil.copy2(source, gallery / filename)
    cards.append(f'<figure><a href="{filename}" target="_blank"><img src="{filename}" alt="{title}"></a><figcaption><strong>{title}</strong><p>{caption}</p></figcaption></figure>')
html='''<!doctype html><html lang="ko"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>전투 수정 · 실제 Unity 화면</title><style>
body{margin:0;background:#f2f4f6;color:#172635;font:16px/1.6 "Segoe UI",sans-serif}main{max-width:1180px;margin:40px auto;padding:0 24px}h1{font-size:29px;line-height:1.3}header p{color:#50606e}.grid{display:grid;grid-template-columns:repeat(3,minmax(0,1fr));gap:22px}figure{margin:0;background:white;border:1px solid #d9e0e6;border-radius:10px;overflow:hidden}img{display:block;width:100%;height:340px;object-fit:contain;background:#dce4e9}figcaption{padding:15px}figcaption p{font-size:14px;color:#526272;margin:6px 0}a{color:inherit}@media(max-width:800px){.grid{grid-template-columns:1fr}img{height:auto;max-height:680px}}</style><main><header><h1>전투 수정 · 실제 Unity 화면</h1><p>가드·뚱보·배는 동작을 확인하기 위한 근접 촬영, 경사 구간은 기존 플레이 카메라입니다. 이미지를 누르면 원본을 새 탭에서 볼 수 있습니다.</p></header><section class="grid">'''+''.join(cards)+'</section></main></html>'
(gallery/'index.html').write_text(html,encoding='utf-8')
(preview/'index.html').write_text(html,encoding='utf-8')
print(json.dumps({'gallery':'http://127.0.0.1:6753/combat-route/','images':len(items)},ensure_ascii=False))
