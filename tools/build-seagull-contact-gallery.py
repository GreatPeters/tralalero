"""Publish the actual recorded contact sequence after checking measured results."""
from pathlib import Path
import json
import shutil
import subprocess
import imageio_ffmpeg

root=Path(__file__).resolve().parents[1]
record=root/'map-concepts/seagull-contact-2026-09-20/play-final'
report=json.loads((record/'report.json').read_text())
by_name={r['name']:r for r in report}
for name in ('contact','landed-contact'):
    r=by_name[name]
    assert r['damageEvents']==1 and abs((r['initial']-r['final'])-r['maximum']*.2)<.001
    assert abs(r['spinDegrees']-720)<.1 and r['headingChange']<.01 and not r['damageBeforeVisible']
assert by_name['dodge']['damageEvents']==0 and by_name['dodge']['spinDegrees']==0
preview=root/'tmp/image-previews/seagull-contact-2026-09-20'
gallery=root/'tmp/image-previews/bonus-gallery-2026-09-19/seagull-contact'
preview.mkdir(parents=True,exist_ok=True);gallery.mkdir(parents=True,exist_ok=True)
video=preview/'seagull-contact.mp4'
subprocess.run([imageio_ffmpeg.get_ffmpeg_exe(),'-n','-framerate','30','-i',str(record/'frame-%03d.png'),'-c:v','libx264','-crf','19','-pix_fmt','yuv420p','-movflags','+faststart',str(video)],check=True,capture_output=True)
shutil.copy2(video,gallery/video.name)
for name in ('frame-064.png','frame-070.png','hit-hud.png'):
    for target in (preview/name,gallery/name):
        if target.exists():raise FileExistsError(target)
        shutil.copy2(record/name,target)
html='''<!doctype html><html lang="ko"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>갈매기 접촉 · 두 바퀴 회전과 체력 피해</title><style>body{margin:0;background:#f2f4f6;color:#172635;font:16px/1.6 "Segoe UI",sans-serif}main{max-width:1000px;margin:32px auto;padding:0 24px}h1{font-size:27px}p{color:#50606e}video{display:block;width:min(100%,640px);aspect-ratio:1;object-fit:contain;background:#1c2732;border-radius:12px}.grid{display:grid;grid-template-columns:1fr 1fr;gap:24px;margin-top:28px}figure{margin:0;background:white;border-radius:10px;overflow:hidden;border:1px solid #d9e0e6}img{display:block;width:100%}figcaption{padding:14px}a{color:#165899}@media(max-width:700px){.grid{grid-template-columns:1fr}}</style><main><h1>갈매기에 닿으면 두 바퀴 회전 + 최대 체력 20% 피해</h1><p>실제 Unity 주행 촬영입니다. 이 캐릭터는 최대 체력 500으로, 접촉 후 400이 남았습니다. 옆으로 피하면 피해와 회전이 발생하지 않습니다.</p><video controls muted loop playsinline preload="metadata" poster="frame-064.png" src="seagull-contact.mp4"></video><p>캐릭터 회전 720도 · 접촉당 피해 1회 · 30fps, 6초</p><div class="grid"><figure><a href="frame-064.png" target="_blank"><img src="frame-064.png" alt="갈매기와 실제 접촉하는 순간"></a><figcaption>몸과 날개가 움직이는 위치에서 접촉 판정</figcaption></figure><figure><a href="frame-070.png" target="_blank"><img src="frame-070.png" alt="접촉 후 회전과 마이너스 100 피해 표시"></a><figcaption>회전과 -100 피해 표시</figcaption></figure></div><p><a href="hit-hud.png" target="_blank">실제 게임 HUD 원본 보기</a></p></main></html>'''
(gallery/'index.html').write_text(html,encoding='utf-8');(preview/'index.html').write_text(html,encoding='utf-8')
print(json.dumps({'url':'http://127.0.0.1:6753/seagull-contact/','cases':report}))
