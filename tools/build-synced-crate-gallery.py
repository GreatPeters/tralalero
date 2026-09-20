"""Encode unmodified native frames at normal and half speed after release checks."""
from pathlib import Path
import json
import shutil
import subprocess
import imageio_ffmpeg

root=Path(__file__).resolve().parents[1]
source=root/'map-concepts/synced-crate-throw-2026-09-20/play-v1'
report=json.loads((source/'report.json').read_text())
assert report['shots']==3 and report['earlyShots']==0
assert report['inside']==0 and report['flightInside']==0 and report['gripError']<.02
assert report['restJitter']<.0001 and report['restElbowJitter']<.0001 and report['enabledAnimatorFrames']==0
assert all(r['ThrowProgress']==1 and r['StrokeProgress']==1 and r['handoffError']<.001 and r['pushMetres']>.3 for r in report['launches'])
assert report['pause']['before']==report['pause']['after']==0
assert report['pause']['progressBefore']==report['pause']['progressAfter']
assert report['death']['animatorEnabled'] and report['death']['state']=='Dead'
preview=root/'tmp/image-previews/synced-crate-throw-2026-09-20'
gallery=root/'tmp/image-previews/bonus-gallery-2026-09-19/synced-crate-throw'
preview.mkdir(parents=True,exist_ok=True);gallery.mkdir(parents=True,exist_ok=True)
ffmpeg=imageio_ffmpeg.get_ffmpeg_exe()
normal=preview/'throw-normal.mp4'
subprocess.run([ffmpeg,'-n','-framerate','30','-i',str(source/'front-%03d.png'),'-framerate','30','-i',str(source/'side-%03d.png'),'-filter_complex','[0:v][1:v]hstack=inputs=2[v]','-map','[v]','-c:v','libx264','-crf','19','-pix_fmt','yuv420p','-movflags','+faststart',str(normal)],check=True,capture_output=True)
slow=preview/'throw-half-speed.mp4'
subprocess.run([ffmpeg,'-n','-i',str(normal),'-vf','setpts=2*PTS','-an','-c:v','libx264','-crf','19','-pix_fmt','yuv420p','-movflags','+faststart',str(slow)],check=True,capture_output=True)
for file in (normal,slow):shutil.copy2(file,gallery/file.name)
for name in ('front-010.png','front-037.png','front-039.png','side-037.png'):
    for target in (preview/name,gallery/name):
        if target.exists():raise FileExistsError(target)
        shutil.copy2(source/name,target)
html='''<!doctype html><html lang="ko"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>상자 투척 · 동작과 발사 시점 수정</title><style>body{margin:0;background:#f2f4f6;color:#172635;font:16px/1.6 "Segoe UI",sans-serif}main{max-width:1080px;margin:32px auto;padding:0 24px}h1{font-size:28px}h2{font-size:19px;margin:30px 0 10px}p{color:#526272}video{display:block;width:100%;aspect-ratio:2/1;object-fit:contain;background:#1c2732;border-radius:10px}.grid{display:grid;grid-template-columns:repeat(3,1fr);gap:18px;margin-top:25px}figure{margin:0;background:white;border:1px solid #d9e0e6;border-radius:9px;overflow:hidden}img{display:block;width:100%}figcaption{padding:12px;font-size:14px}@media(max-width:700px){.grid{grid-template-columns:1fr}}</style><main><h1>양손으로 밀고, 손을 놓는 순간 발사</h1><p>실제 Unity 플레이를 정면·측면으로 함께 촬영했습니다. 몸 안쪽에서 떨리던 검은 윤곽선을 정리하고, 동작이 끝나는 손 위치에서 상자가 출발하도록 맞췄습니다.</p><h2>정상 속도</h2><video controls muted loop playsinline preload="metadata" src="throw-normal.mp4" poster="front-010.png"></video><h2>0.5배속 · 발사 시점 확인</h2><video controls muted loop playsinline preload="metadata" src="throw-half-speed.mp4" poster="side-037.png"></video><div class="grid"><figure><a href="front-010.png" target="_blank"><img src="front-010.png" alt="안정된 대기 자세"></a><figcaption>대기 · 양손으로 잡기</figcaption></figure><figure><a href="front-037.png" target="_blank"><img src="front-037.png" alt="놓기 직전 앞으로 미는 자세"></a><figcaption>발사 직전 · 앞으로 밀기</figcaption></figure><figure><a href="front-039.png" target="_blank"><img src="front-039.png" alt="손을 떠난 상자"></a><figcaption>손을 놓은 뒤 · 비행 시작</figcaption></figure></div></main></html>'''
(gallery/'index.html').write_text(html,encoding='utf-8');(preview/'index.html').write_text(html,encoding='utf-8')
print(json.dumps({'url':'http://127.0.0.1:6753/synced-crate-throw/','report':report}))
