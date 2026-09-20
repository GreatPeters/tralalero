"""Encode the complete native capture; fail closed if its contact/geometry checks fail."""
from pathlib import Path
import json
import shutil
import subprocess
import imageio_ffmpeg

root=Path(__file__).resolve().parents[1]
record=root/'map-concepts/two-hand-crate-2026-09-20/play-ready'
report=json.loads((record/'report.json').read_text())
assert report['shots']==3 and report['holdingFrames']>60
assert report['leftError']<.02 and report['rightError']<.02, report
assert report['maxInside']==0 and report['maxFlightInside']==0 and report['minTorsoGap']>0, report
assert all(launch['rotationJump']<.01 for launch in report['launches'])
preview=root/'tmp/image-previews/two-hand-crate-2026-09-20'
gallery=root/'tmp/image-previews/bonus-gallery-2026-09-19/two-hand-crate'
preview.mkdir(parents=True,exist_ok=True);gallery.mkdir(parents=True,exist_ok=True)
output=preview/'two-hand-throw.mp4'
if output.exists():
    raise FileExistsError(output)
subprocess.run([imageio_ffmpeg.get_ffmpeg_exe(),'-n','-framerate','30','-i',str(record/'front-%03d.png'),'-framerate','30','-i',str(record/'side-%03d.png'),'-filter_complex','[0:v][1:v]hstack=inputs=2[v]','-map','[v]','-c:v','libx264','-crf','19','-pix_fmt','yuv420p','-movflags','+faststart',str(output)],check=True,capture_output=True)
shutil.copy2(output,gallery/output.name)
for name in ('front-030.png','side-030.png','front-039.png'):
    for target in (preview/name,gallery/name):
        if target.exists():raise FileExistsError(target)
        shutil.copy2(record/name,target)
html='''<!doctype html><html lang="ko"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>뚱보 양손 투척 · 실제 동작</title><style>body{margin:0;background:#f2f4f6;color:#172635;font:16px/1.6 "Segoe UI",sans-serif}main{max-width:1120px;margin:32px auto;padding:0 24px}h1{font-size:28px}p{color:#50606e}video{width:100%;aspect-ratio:2/1;object-fit:contain;background:#1c2732;border-radius:12px}.grid{display:grid;grid-template-columns:1fr 1fr;gap:24px;margin-top:28px}figure{margin:0;background:white;border-radius:10px;overflow:hidden;border:1px solid #d9e0e6}img{display:block;width:100%}figcaption{padding:14px}@media(max-width:700px){.grid{grid-template-columns:1fr}}</style><main><h1>뚱보 양손 투척 · 실제 동작</h1><p>정면과 측면을 함께 촬영한 실제 Unity 플레이 영상입니다. 양손으로 들고 던지는 동작을 3회 촬영했습니다.</p><video controls muted loop playsinline preload="metadata" poster="front-030.png" src="two-hand-throw.mp4"></video><p>왼쪽: 정면 · 오른쪽: 측면 / 30fps, 7초</p><div class="grid"><figure><a href="front-030.png" target="_blank"><img src="front-030.png" alt="양손으로 몸 앞에서 상자를 잡는 자세"></a><figcaption>양손이 양쪽 손잡이를 잡는 자세</figcaption></figure><figure><a href="side-030.png" target="_blank"><img src="side-030.png" alt="몸과 상자 사이 간격을 보여주는 측면"></a><figcaption>몸 앞에서 떨어져 있는 상자 · 측면 확인</figcaption></figure></div></main></html>'''
(gallery/'index.html').write_text(html,encoding='utf-8');(preview/'index.html').write_text(html,encoding='utf-8')
print(json.dumps({'video':str(output),'url':'http://127.0.0.1:6753/two-hand-crate/','report':report}))
