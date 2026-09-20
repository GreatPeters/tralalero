"""Publish the native small-footprint dodge recordings after checking outcomes."""
from pathlib import Path
import json
import shutil
import subprocess
import imageio_ffmpeg

root = Path(__file__).resolve().parents[1]
record = root / 'map-concepts/seagull-dodge-size-2026-09-20/play-v2'
reports = json.loads((record / 'report.json').read_text())
by_name = {item['name']: item for item in reports}
for name in ('dodge-left', 'dodge-right'):
    item = by_name[name]
    assert item['damageEvents'] == 0 and item['spinDegrees'] == 0
    assert 1.3 < item['maxLane'] < 1.6 and item['maxShadow'] <= 1.601
for name in ('contact', 'landed-contact'):
    item = by_name[name]
    assert item['damageEvents'] == 1
    assert abs(item['initial'] - item['final'] - item['maximum'] * .2) < .001
    assert abs(item['spinDegrees'] - 720) < .1
preview = root / 'tmp/image-previews/seagull-dodge-size-2026-09-20'
gallery = root / 'tmp/image-previews/bonus-gallery-2026-09-19/seagull-dodge-size'
for folder in (preview, gallery):
    folder.mkdir(parents=True, exist_ok=True)
for name in ('dodge-left', 'dodge-right', 'contact'):
    video = preview / f'{name}.mp4'
    subprocess.run([
        imageio_ffmpeg.get_ffmpeg_exe(), '-n', '-framerate', '30', '-i',
        str(record / f'{name}-%03d.png'), '-c:v', 'libx264', '-crf', '19',
        '-pix_fmt', 'yuv420p', '-movflags', '+faststart', str(video)
    ], check=True, capture_output=True)
    destination = gallery / video.name
    if destination.exists():
        raise FileExistsError(destination)
    shutil.copy2(video, destination)
for name in ('warning-dodge-left.png', 'dodge-left-072.png'):
    for folder in (preview, gallery):
        destination = folder / name
        if destination.exists():
            raise FileExistsError(destination)
        shutil.copy2(record / name, destination)
html = '''<!doctype html><html lang="ko"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>갈매기 · 작아진 착지 그림자</title><style>body{margin:0;background:#f2f4f6;color:#172635;font:16px/1.6 "Segoe UI",sans-serif}main{max-width:1200px;margin:32px auto;padding:0 24px}h1{font-size:28px}p{color:#50606e}.grid{display:grid;grid-template-columns:1fr 1fr;gap:24px}video,img{display:block;width:100%;aspect-ratio:1;object-fit:contain;border-radius:12px}figure{margin:0}figcaption{padding:10px 0}a{color:#165899}.still{max-width:640px;margin-top:28px}details{margin:24px 0}details video{max-width:640px}@media(max-width:700px){.grid{grid-template-columns:1fr}}</style><main><h1>갈매기 그림자를 작게, 옆으로 피할 공간 확보</h1><p>착지 예고의 최대 지름을 1.6m로 줄였습니다. 갈매기와 접촉 범위도 함께 줄여, 그림자가 뜬 뒤 좌우 약 1.5m 이동하면 피해 없이 통과합니다. 실제 Unity 주행 영상입니다.</p><div class="grid"><figure><video controls muted loop playsinline preload="metadata" poster="warning-dodge-left.png" src="dodge-left.mp4"></video><figcaption>왼쪽으로 피하기 · 체력 500 유지</figcaption></figure><figure><video controls muted loop playsinline preload="metadata" src="dodge-right.mp4"></video><figcaption>오른쪽으로 피하기 · 체력 500 유지</figcaption></figure></div><figure class="still"><a href="warning-dodge-left.png" target="_blank"><img src="warning-dodge-left.png" alt="작아진 원형 착지 그림자와 왼쪽 회피 공간"></a><figcaption>내려오기 직전의 그림자 크기</figcaption></figure><details><summary>그대로 닿았을 때 확인</summary><p>접촉 시에는 두 바퀴 회전과 최대 체력 20% 피해가 유지됩니다.</p><video controls muted loop playsinline preload="metadata" src="contact.mp4"></video></details></main></html>'''
for folder in (preview, gallery):
    (folder / 'index.html').write_text(html, encoding='utf-8')
print('http://127.0.0.1:6753/seagull-dodge-size/')
