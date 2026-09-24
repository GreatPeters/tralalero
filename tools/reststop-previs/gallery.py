"""Build a local review gallery from finished Blender clips and labeled concepts."""
from pathlib import Path
import hashlib, html, json, shutil, sys
sys.path.insert(0,str(Path(__file__).resolve().parent))
from timeline import STAGES
ROOT=Path.cwd();OUT=ROOT/'outputs/reststop-blender-300s-2026-09-23';GALLERY=ROOT/'tmp/image-previews/reststop-blender-300s-2026-09-23';GALLERY.mkdir(exist_ok=True)

def copy(src,name=None):
    src=Path(src);dst=GALLERY/(name or src.name)
    if src.resolve()==dst.resolve():return dst.name
    if not dst.exists() or hashlib.sha256(src.read_bytes()).digest()!=hashlib.sha256(dst.read_bytes()).digest():shutil.copy2(src,dst)
    return dst.name

complete=[];cards=[]
for idx,title,a,b in STAGES:
    clip=OUT/'videos'/f'{idx}-reststop.mp4';receipt=clip.with_suffix('.json')
    if receipt.exists():
        record=json.loads(receipt.read_text(encoding='utf8'));complete.append(record)
        video=copy(clip);poster=OUT/'videos'/f'{idx}-poster.jpg';poster_attr=f' poster="{copy(poster)}"' if poster.exists() else ''
        content=f'<video controls playsinline preload="metadata"{poster_attr}><source src="{video}" type="video/mp4"></video><p>{html.escape(record["notes"])}</p><a href="{video}" download>MP4 저장 ↗</a>'
    else:content='<div class="pending">렌더링 중</div>'
    cards.append(f'<article id="step-{idx}"><div class="step"><span>{idx}</span><h2>{title}</h2><b>{b-a}초</b></div><div class="range">전체 타임라인 {a}–{b}초</div>{content}</article>')

full=OUT/'videos/reststop-300s.mp4'
full_html='<p class="pending">구간별 렌더와 인코딩이 끝나면 전체 영상이 표시됩니다.</p>'
if (OUT/'video-manifest.json').exists():
    video=copy(full);poster=copy(OUT/'videos/01-poster.jpg','full-poster.jpg')
    full_html=f'<video controls playsinline preload="metadata" poster="{poster}"><source src="{video}" type="video/mp4"></video><p><a href="{video}" download>전체 300초 MP4 저장 ↗</a></p>'
overview=OUT/'fresh-glb-overview.png'
overview_html=f'<a href="{copy(overview)}" target="_blank"><img src="{overview.name}" alt="하나의 Blender 환경 전체 배치"></a>' if overview.exists() else ''
links=[]
for path in [OUT/'reststop-master.blend',OUT/'reststop-environment.glb']:
    if path.exists():links.append(f'<a href="{copy(path)}" download>{"애니메이션 Blender 원본" if path.suffix==".blend" else "환경 GLB"} ↓</a>')

page='''<!doctype html><html lang="ko"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>휴게소 · Blender 300초 / 10구간</title><style>
*{box-sizing:border-box}html{scroll-behavior:smooth}body{margin:0;background:#eeeee6;color:#233729;font:16px/1.65 system-ui,'Malgun Gothic',sans-serif}main{max-width:1440px;margin:auto;padding:44px 32px}header{max-width:1050px}.eyebrow{font-size:12px;letter-spacing:.16em;color:#6c755d}h1{font-size:clamp(30px,4vw,54px);line-height:1.2;margin:12px 0 18px}header p{color:#5a685a}.facts{font-size:18px;color:#2e5939;border-left:4px solid #b17f2a;padding:10px 20px;margin:24px 0}a{color:#315e42;text-underline-offset:4px}nav{display:flex;gap:12px 24px;flex-wrap:wrap;border-block:1px solid #c4cdbd;padding:20px 0;margin:30px 0}.hero{max-width:1100px;margin:0 auto 38px}video{width:100%;aspect-ratio:16/9;background:#152319;display:block}.grid{display:grid;grid-template-columns:1fr 1fr;gap:36px 26px}article{min-width:0;scroll-margin-top:20px}.step{display:flex;align-items:baseline;gap:12px}.step span{font-size:17px;color:#9b7128}.step h2{font-size:24px;line-height:1.4;margin:0}.step b{margin-left:auto;font-weight:500;color:#627461}.range{color:#76806e;font-size:13px;margin:4px 0 12px}article p{font-size:14px;color:#5d6c5c;margin:9px 0 6px}article>a{font-size:13px}section{margin:48px 0}.pending{display:grid;place-items:center;background:#dce3d6;color:#6e7b68;min-height:220px}img{max-width:100%;height:auto;display:block}.concepts{display:grid;grid-template-columns:1fr 1fr;gap:26px}.concepts p{font-size:14px;color:#5d6c5c}.downloads{display:flex;gap:28px;flex-wrap:wrap;margin:20px 0}footer{border-top:1px solid #c4cdbd;margin-top:40px;padding-top:22px;color:#697765;font-size:14px}@media(max-width:760px){main{padding:26px 16px}.grid,.concepts{grid-template-columns:1fr}.step h2{font-size:22px}nav{gap:10px 18px}h1{font-size:32px}}
</style><main><header><div class="eyebrow">REST STOP / BLENDER PREVIS / 2026.09.23</div><h1>하나의 휴게소, 300초의 이동</h1><p>진입로에서 주차장과 건물을 지나 주유소 출구까지 연결된 Blender 마스터 씬입니다. 아래 10개 영상은 같은 타임라인을 구간 경계에서 나눈 것입니다.</p><div class="facts">전체 300초 · 10개 구간 · 탑뷰 방어 60초 · 연속 회전 최대 90°/초 · 화장실 면적 5배</div></header>'''
page+='<nav><a href="#full">전체 영상</a>'+''.join(f'<a href="#step-{i}">{i} {t}</a>' for i,t,a,b in STAGES)+'<a href="#concepts">새 인물·편의점 시안</a></nav>'
page+='<section class="hero" id="full"><h2>전체 300초</h2>'+full_html+'</section><div class="grid">'+''.join(cards)+'</div>'
page+='<section><h2>전체 환경과 편집 가능한 원본</h2><p>건물·진입로·주차장·주유소는 하나의 공간에 있습니다. 실내와 주유 구간에서는 지붕을 걷어내 내부가 보이도록 촬영했습니다.</p>'+overview_html+'<div class="downloads">'+''.join(links)+'</div></section>'
native_people=OUT/'people-native-blender.png'
if native_people.exists():
    people_file=copy(native_people)
    page+=f'<section><h2>영상에 사용한 Blender 인물</h2><a href="{people_file}" target="_blank"><img src="{people_file}" alt="실제 Blender 인물 8종"></a><p>새 시안의 복장과 역할을 반영한 프리비즈 캐릭터입니다.</p></section>'
page+='''<section id="concepts"><h2>새 인물과 편의점 시안</h2><p>아래 두 장은 별도로 그린 ImageGen 콘셉트입니다. 위 영상들은 직접 만든 Blender 장면의 실제 렌더입니다.</p><div class="concepts"><div><a href="people-concept.png" target="_blank"><img src="people-concept.png" alt="휴게소 인물 8종 콘셉트"></a><p>주차 안내원 · 간식 조리사 · 바리스타 · 점원 · 청소원 · 주유원 · 경찰 · 방문객</p></div><div><a href="convenience-concept.png" target="_blank"><img src="convenience-concept.png" alt="휴게소 편의점 콘셉트"></a><p>본관과 같은 지붕·목재·석재를 쓰고, 화장실로 이어지는 편의점</p></div></div></section><footer>형태·동선·카메라·적과 기믹의 타이밍을 검토하는 무음 프리비즈입니다. 인물과 건축은 간소화한 직접 모델링이며 최종 게임 아트가 아닙니다. TRELLIS 및 Seedance는 실행하지 않았으며 Unity 씬도 수정하지 않았습니다.<br>Blender 원본에는 전체 애니메이션이, GLB에는 정적 환경 모델이 들어 있습니다.</footer></main></html>'''
(GALLERY/'index.html').write_text(page,encoding='utf8')
(GALLERY/'gallery-status.json').write_text(json.dumps({'complete_clips':len(complete),'total_clips':10,'full_video':(OUT/'video-manifest.json').exists()},indent=2),encoding='utf8')
print(json.dumps({'gallery':str(GALLERY),'complete_clips':len(complete),'full_video':(OUT/'video-manifest.json').exists()}))
