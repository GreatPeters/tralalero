"""Compare complete native recordings before and after the landing timing fix."""
from pathlib import Path
import json
import shutil
import subprocess
import imageio_ffmpeg

root = Path(__file__).resolve().parents[1]
record = root / 'map-concepts/seagull-early-landing-2026-09-20/play-v1'
reports = {item['name']: item for item in json.loads((record / 'report.json').read_text())}
for name in ('contact', 'dodge-left', 'dodge-right'):
    item = reports[name]
    assert item['warningDistance'] > 27 and item['landingDistance'] > 10
    assert item['landingFrame'] < item['passedFrame']
for name in ('dodge-left', 'dodge-right'):
    item = reports[name]
    assert item['damageEvents'] == 0 and item['spinDegrees'] == 0
    assert item['departureProgress'] >= 2
    assert item['landingFrame'] < 60, 'The bird must be down by the 2-second reference moment.'
for name in ('contact', 'landed-contact'):
    item = reports[name]
    assert item['damageEvents'] == 1
    assert abs(item['initial'] - item['final'] - item['maximum'] * .2) < .001
    assert abs(item['spinDegrees'] - 720) < .1
preview = root / 'tmp/image-previews/seagull-early-landing-2026-09-20'
gallery = root / 'tmp/image-previews/bonus-gallery-2026-09-19/seagull-early-landing'
for folder in (preview, gallery):
    folder.mkdir(parents=True, exist_ok=True)
video = preview / 'after.mp4'
subprocess.run([
    imageio_ffmpeg.get_ffmpeg_exe(), '-n', '-framerate', '30', '-i',
    str(record / 'dodge-left-%03d.png'), '-c:v', 'libx264', '-crf', '19',
    '-pix_fmt', 'yuv420p', '-movflags', '+faststart', str(video)
], check=True, capture_output=True)
files = {
    'before.mp4': root / 'tmp/image-previews/seagull-warning-readability-2026-09-20/dodge-left.mp4',
    'before-2s.png': root / 'map-concepts/seagull-warning-readability-2026-09-20/play-v1/dodge-left-060.png',
    'after-2s.png': record / 'dodge-left-060.png',
}
for name, source in files.items():
    for folder in (preview, gallery):
        destination = folder / name
        if destination.exists():
            raise FileExistsError(destination)
        shutil.copy2(source, destination)
if (gallery / video.name).exists():
    raise FileExistsError(gallery / video.name)
shutil.copy2(video, gallery / video.name)
html = '''<!doctype html><html lang="ko"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>갈매기 · 상어 도착 전에 착지</title>
<style>body{margin:0;background:#f2f4f6;color:#172635;font:16px/1.6 "Segoe UI",sans-serif}main{max-width:1200px;margin:32px auto;padding:0 24px}h1{font-size:28px}p{color:#50606e}.grid{display:grid;grid-template-columns:1fr 1fr;gap:24px}video,img{display:block;width:100%;aspect-ratio:1;object-fit:contain;border-radius:12px}figure{margin:0}figcaption{padding:10px 0}a{color:#165899}h2{font-size:20px;margin-top:32px}@media(max-width:700px){.grid{grid-template-columns:1fr}}</style>
<main><h1>상어가 오기 전에 갈매기가 먼저 착지합니다</h1>
<p>검은 표시는 28m 앞에서 시작하고, 예고 1초 + 하강 0.4초 뒤 착지합니다. 실제 주행에서는 약 15m 앞에서 완전히 내려와 기다립니다. 크기와 진한 검은색은 유지했습니다.</p>
<div class="grid"><figure><video controls muted loop playsinline preload="metadata" poster="before-2s.png" src="before.mp4"></video><figcaption>이전 · 이 시점에는 아직 검은 표시</figcaption></figure><figure><video controls muted loop playsinline preload="metadata" poster="after-2s.png" src="after.mp4"></video><figcaption>수정 · 이미 착지하고 상어를 기다림</figcaption></figure></div>
<h2>같은 영상 2초 시점</h2><div class="grid"><figure><a href="before-2s.png" target="_blank"><img src="before-2s.png" alt="수정 전 2초 시점, 아직 그림자만 보임"></a><figcaption>이전</figcaption></figure><figure><a href="after-2s.png" target="_blank"><img src="after-2s.png" alt="수정 후 2초 시점, 갈매기가 길에 내려와 있음"></a><figcaption>수정 후</figcaption></figure></div><p>피하면 피해 없음 · 닿으면 최대 체력 20% 감소와 두 바퀴 회전 유지</p></main></html>'''
for folder in (preview, gallery):
    (folder / 'index.html').write_text(html, encoding='utf-8')
print('http://127.0.0.1:6753/seagull-early-landing/')
