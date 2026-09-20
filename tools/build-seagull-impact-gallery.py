"""Publish native impact footage with a clearly labeled half-speed excerpt."""
from pathlib import Path
import argparse
import json
import shutil
import subprocess
import imageio_ffmpeg

parser = argparse.ArgumentParser()
parser.add_argument('--record', default='map-concepts/seagull-impact-tumble-2026-09-20/play-v2')
parser.add_argument('--slug', default='seagull-impact-tumble')
parser.add_argument('--shadow-diameter', type=float, default=3.12)
parser.add_argument('--bird-increase', type=int, default=50)
parser.add_argument('--shadow-increase', type=int, default=30)
args = parser.parse_args()
root = Path(__file__).resolve().parents[1]
record = root / args.record
reports = {item['name']: item for item in json.loads((record / 'report.json').read_text())}
for name in ('contact', 'landed-contact'):
    item = reports[name]
    assert item['damageEvents'] == 1 and item['activeHitShapes'] == 0
    assert abs(item['initial'] - item['final'] - item['maximum'] * .2) < .001
    assert abs(item['spinDegrees'] - 720) < .1 and abs(item['birdSpinDegrees'] - 1080) < .1
    assert item['birdTravel'] > 7.4 and item['birdArcHeight'] > 4.5
for name in ('dodge-left', 'dodge-right'):
    item = reports[name]
    assert item['damageEvents'] == 0 and item['spinDegrees'] == 0 and item['birdSpinDegrees'] == 0
    assert item['landingDistance'] > 10 and item['departureProgress'] > 2
assert abs(reports['contact']['maxShadow'] - args.shadow_diameter) < .01
reset = reports['midair-pause-retry']
assert reset['acceptedMidair'] and reset['midairRise'] > 2
assert max(reset['pausePositionError'], reset['pauseRotationError'], reset['pauseSharkError']) < .001
assert reset['resetHidden'] and not reset['resetHit'] and reset['retryAnimatorEnabled']
assert reset['retryRotationError'] < .001 and reset['retryContacts'] == 13
preview = root / 'tmp/image-previews' / record.parent.name
gallery = root / 'tmp/image-previews/bonus-gallery-2026-09-19' / args.slug
for folder in (preview, gallery):
    folder.mkdir(parents=True, exist_ok=True)
hit_frame = reports['contact']['firstHit']
for name, start, count, fps in (
    ('contact-normal.mp4', 0, 180, 30),
    ('contact-slow.mp4', max(0, hit_frame - 12), 60, 15),
):
    video = preview / name
    subprocess.run([
        imageio_ffmpeg.get_ffmpeg_exe(), '-n', '-framerate', str(fps), '-start_number', str(start),
        '-i', str(record / 'contact-%03d.png'), '-frames:v', str(count), '-c:v', 'libx264', '-crf', '19',
        '-pix_fmt', 'yuv420p', '-movflags', '+faststart', str(video)
    ], check=True, capture_output=True)
    if (gallery / name).exists():
        raise FileExistsError(gallery / name)
    shutil.copy2(video, gallery / name)
for name in ('warning-dodge-left.png', 'contact-135.png', 'contact-144.png'):
    for folder in (preview, gallery):
        destination = folder / name
        if destination.exists():
            raise FileExistsError(destination)
        shutil.copy2(record / name, destination)
html = '''<!doctype html><html lang="ko"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>갈매기 · 커진 크기와 충돌 회전</title>
<style>body{margin:0;background:#f2f4f6;color:#172635;font:16px/1.6 "Segoe UI",sans-serif}main{max-width:1200px;margin:32px auto;padding:0 24px}h1{font-size:28px}p{color:#50606e}.grid{display:grid;grid-template-columns:1fr 1fr;gap:24px}video,img{display:block;width:100%;aspect-ratio:1;object-fit:contain;border-radius:12px}figure{margin:0}figcaption{padding:10px 0}a{color:#165899}h2{font-size:20px;margin-top:32px}.stills{display:grid;grid-template-columns:repeat(3,1fr);gap:20px}@media(max-width:700px){.grid,.stills{grid-template-columns:1fr}}</style>
<main><h1>상어는 두 바퀴, 갈매기는 빙글빙글 튕겨 나갑니다</h1>
<p>갈매기는 직전보다 50%, 검은 그림자는 30% 더 키웠습니다. 그림자의 최대 지름은 3.12m입니다. 충돌하면 상어가 두 바퀴 돌고, 갈매기는 날개를 펼친 채 위쪽 옆으로 튕겨 나가며 세 바퀴 회전합니다.</p>
<div class="grid"><figure><video controls muted loop playsinline preload="metadata" poster="contact-135.png" src="contact-normal.mp4"></video><figcaption>실제 Unity 주행 · 정상 속도, 전체 6초</figcaption></figure><figure><video controls muted loop playsinline preload="metadata" poster="contact-144.png" src="contact-slow.mp4"></video><figcaption>충돌 구간 · 0.5배속, 보간 없는 원본 프레임</figcaption></figure></div>
<h2>크기와 충돌 순간</h2><div class="stills"><figure><a href="warning-dodge-left.png" target="_blank"><img src="warning-dodge-left.png" alt="30퍼센트 커진 검은 착지 표시"></a><figcaption>그림자 +30%</figcaption></figure><figure><a href="contact-135.png" target="_blank"><img src="contact-135.png" alt="상어가 회전하고 갈매기가 튕겨 나가는 순간"></a><figcaption>충돌 직후 · 두 캐릭터가 함께 회전</figcaption></figure><figure><a href="contact-144.png" target="_blank"><img src="contact-144.png" alt="날개를 펼친 갈매기가 뒤집히며 날아감"></a><figcaption>갈매기 +50% · 회전하며 튕겨 나감</figcaption></figure></div><p>피하면 피해 없음 · 닿으면 최대 체력 20% 감소 · 충돌당 피해는 한 번</p></main></html>'''
html = html.replace('직전보다 50%, 검은 그림자는 30%', f'직전보다 {args.bird_increase}%, 검은 그림자는 {args.shadow_increase}%')
html = html.replace('3.12m', f'{args.shadow_diameter:g}m')
html = html.replace('30퍼센트', f'{args.shadow_increase}퍼센트').replace('그림자 +30%', f'그림자 +{args.shadow_increase}%')
html = html.replace('갈매기 +50%', f'갈매기 +{args.bird_increase}%')
for folder in (preview, gallery):
    (folder / 'index.html').write_text(html, encoding='utf-8')
print(f'http://127.0.0.1:6753/{args.slug}/')
