"""Assemble native screenshots and captured-frame videos for the local review gallery."""
from pathlib import Path
import json
import shutil
import subprocess
import imageio_ffmpeg

root=Path(__file__).resolve().parents[1]
gallery=root/'tmp/image-previews/bonus-gallery-2026-09-19/mobile-feedback'
gallery.mkdir(exist_ok=True)
for chapter in ('SR18','HighWay','RestStop'):
    source=root/f'tmp/image-previews/mobile-feedback-2026-09-20/final-{chapter}'
    for image in source.glob('*.png'):
        target=gallery/f'{chapter}-{image.name}'
        if not target.exists():shutil.copy2(image,target)
woman=root/'tmp/mobile-feedback-2026-09-20/woman-skin-palm/Enemy_Woman_ControlledSlash-34.png'
if not (gallery/'woman-grip.png').exists():shutil.copy2(woman,gallery/'woman-grip.png')
for kind in ('guard','fatman-once'):
    target=gallery/f'{kind}.mp4'
    if not target.exists():
        subprocess.run([imageio_ffmpeg.get_ffmpeg_exe(),'-hide_banner','-loglevel','error','-n','-framerate','30','-i',str(root/f'tmp/mobile-feedback-2026-09-20/combat-v2/{kind}-%03d.png'),'-vf','scale=540:1170','-c:v','libx264','-crf','22','-pix_fmt','yuv420p','-movflags','+faststart',str(target)],check=True)

cards=[]
for chapter,label in [('SR18','노량진'),('HighWay','고속도로'),('RestStop','휴게소')]:
    pictures=''.join(f'<figure><a href="{chapter}-{name}.png"><img loading="lazy" src="{chapter}-{name}.png" alt="{label} {caption}"></a><figcaption>{caption}</figcaption></figure>' for name,caption in [('lobby','시작 배경 · 검은색 90%'),('tutorial','읽기 쉬운 시작 안내'),('upgrades','기존 강화 아이콘'),('talisman','붉은 매듭 · 강화와 같은 아이콘')])
    cards.append(f'<section><h2>{label}</h2><div class="screens">{pictures}</div></section>')
html='''<!doctype html><html lang="ko"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>전투·UI·모바일 수정 확인</title>
<style>body{margin:0;background:#101821;color:#edf3fa;font:16px/1.6 system-ui,sans-serif}main{max-width:1250px;margin:auto;padding:32px}h1{font-size:32px;line-height:1.2}h2{margin-top:48px}p{color:#b9c9d8;max-width:950px}a{color:#83d4ff}.screens{display:grid;grid-template-columns:repeat(4,1fr);gap:16px}.combat{display:grid;grid-template-columns:repeat(3,1fr);gap:22px}figure{margin:0}img,video{width:100%;height:auto;border-radius:12px;background:#1c2937}figcaption{font-size:14px;padding:10px 0}table{border-collapse:collapse}th,td{padding:10px 22px;border-bottom:1px solid #344556;text-align:left}strong{color:#ffcf79}.badge{color:#70e9b6}footer{margin-top:55px;color:#8295a8}@media(max-width:760px){main{padding:18px}.screens,.combat{grid-template-columns:repeat(2,1fr)}h1{font-size:27px}}@media(max-width:430px){.combat{grid-template-columns:1fr}}</style>
<main><div class="badge">Unity 실제 실행 화면 · 2026.09.20</div><h1>전투·UI·모바일 수정 확인</h1><p>뚱보는 한 번만 투척하며 공격력은 100. 여성 보스 체력은 2,458 → 3,687. 화면은 실제 프로젝트에서 촬영했습니다.</p>
<section><h2>전투 확인</h2><div class="combat"><figure><video controls playsinline preload="metadata" src="guard.mp4"></video><figcaption>가드 · Arrow2 탄환이 날아와 명중. 별도 검사에서 가드가 사라져도 발사된 탄환이 유지되는 것을 확인.</figcaption></figure><figure><video controls playsinline preload="metadata" src="fatman-once.mp4"></video><figcaption>뚱보 · 8초 동안 한 번만 투척, 명중 체력 <strong>500 → 400</strong>.</figcaption></figure><figure><a href="woman-grip.png"><img src="woman-grip.png" alt="여성 보스 칼 파지"></a><figcaption>여성 보스 · 손잡이를 실제 장갑의 손바닥 위치에 맞춤. 단독 모델 검토 화면.</figcaption></figure></div></section>
'''+''.join(cards)+'''
<section><h2>에디터 측정</h2><table><tr><th>같은 시작 구간</th><th>수정 전</th><th>수정 후</th></tr><tr><td>Batches</td><td>327</td><td>296</td></tr><tr><td>활성 애니메이터</td><td>53</td><td>17</td></tr><tr><td>평균 프레임 시간</td><td>16.70 ms</td><td>12.28 ms</td></tr></table><p>1080×2340, FPS 제한 없음, 120프레임 표본. 에디터 수치는 실기기 보장값이 아닙니다. 수정 APK는 Galaxy S22에 업데이트 설치했습니다. 마지막 휴대폰 FPS 측정은 자동 잠금 해제 대기 중입니다.</p></section>
<footer>보너스 근접 화면의 두 부적은 네이티브 아트 미리보기입니다. 보상 적용·반복 갱신은 별도 테스트로 확인했습니다. 원본 PNG는 각 이미지를 클릭하면 열립니다.</footer></main></html>'''
(gallery/'index.html').write_text(html,encoding='utf-8')
print(gallery/'index.html')
