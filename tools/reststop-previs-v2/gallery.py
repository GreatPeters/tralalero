"""Publish review copies to the existing external-browser gallery pattern."""
from pathlib import Path
import hashlib,html,json,shutil,sys
sys.path.insert(0,str(Path(__file__).resolve().parent))
from timeline import STAGES
ROOT=Path.cwd();OUT=ROOT/'outputs/reststop-blender-v2-2026-09-23';G=ROOT/'tmp/image-previews/reststop-blender-v2-2026-09-23';G.mkdir(exist_ok=True)
def copy(src,name=None):
 src=Path(src);dst=G/(name or src.name)
 if not dst.exists() or hashlib.sha256(src.read_bytes()).digest()!=hashlib.sha256(dst.read_bytes()).digest():shutil.copy2(src,dst)
 return dst.name
def video(name,poster,title):
 return f'<video controls playsinline preload="metadata" aria-label="{title}" poster="{poster}"><source src="{name}" type="video/mp4"></video>'
cards=[];complete=[]
for idx,title,a,b in STAGES:
 clip=OUT/'videos'/f'{idx}-reststop.mp4';receipt=clip.with_suffix('.json');poster=OUT/'videos'/f'{idx}-poster.jpg'
 if receipt.exists() and poster.exists():
  r=json.loads(receipt.read_text(encoding='utf8'));complete.append(r);name=copy(clip)
  body=video(name,copy(poster),title)+f'<p>{html.escape(r["notes"])}</p><a href="{name}" download>이 구간 MP4 저장 ↓</a>'
 else:
  times={'01':19,'02':31,'03':65,'04':91,'05':115,'06':179,'07':194,'08':223,'09':258,'10':295};name=copy(OUT/'evidence'/f'view-{times[idx]:03}.png',f'{idx}-preview.png');body=f'<img src="{name}" alt="{title} Blender 렌더"><p>영상 렌더링 중 · 정지 화면 미리보기</p>'
 cards.append(f'<article id="step-{idx}"><div class="step"><span>{idx}</span><h2>{title}</h2><b>{b-a}초</b></div><p class="range">전체 타임라인 {a}–{b}초</p>{body}</article>')
full='<p>전체 영상 렌더링 중입니다. 아래에서 완성된 구간과 정지 화면을 볼 수 있습니다.</p>'
if (OUT/'video-manifest.json').exists():
 name=copy(OUT/'videos/reststop-300s.mp4');full=video(name,copy(OUT/'videos/01-poster.jpg'),'휴게소 전체 300초')+f'<p><a href="{name}" download>전체 300초 MP4 저장 ↓</a></p>'
copy(OUT/'whole-map.png')
overview=copy(OUT/('fresh-glb-overview.png' if (OUT/'fresh-glb-overview.png').exists() else 'whole-map.png'));links=[]
for file,label in [('reststop-v2.blend','Blender 애니메이션 원본'),('reststop-v2-environment.glb','환경 GLB')]:
 if (OUT/file).exists():links.append(f'<a href="{copy(OUT/file)}" download>{label} ↓</a>')
if (G/'reststop-v2-review.zip').exists():links.append('<a href="reststop-v2-review.zip" download>영상·원본·갤러리 전체 ZIP ↓</a>')
stills=[]
for t,label in [(115,'05 쿼터뷰 방어전'),(194,'07 대형 편의점'),(223,'08 대형 화장실'),(31,'02 주차 공간')]:
 name=copy(OUT/'evidence'/f'view-{t:03}.png');stills.append(f'<figure><a href="{name}" target="_blank"><img src="{name}" alt="{label}"></a><figcaption>{label} · <a href="{name}" download>PNG 저장</a></figcaption></figure>')
comparisons=[]
defense=OUT/'comparison/comparison-defense.png'
if defense.exists():
 name=copy(defense);comparisons.append(f'<figure><a href="{name}" target="_blank"><img src="{name}" alt="154초 방어전의 이전 탑뷰와 수정 쿼터뷰 비교"></a><figcaption>5번 방어전 · 같은 시점의 이전 탑뷰와 수정 쿼터뷰</figcaption></figure>')
for subject,label in [('store','편의점'),('restroom','화장실')]:
 p=OUT/'comparison'/f'comparison-{subject}.png'
 if p.exists():
  name=copy(p);comparisons.append(f'<figure><a href="{name}" target="_blank"><img src="{name}" alt="{label} 동일 배율 전후 비교"></a><figcaption>{label} · 왼쪽 이전판 / 오른쪽 수정판 · 동일 카메라 각도와 배율</figcaption></figure>')
css='''*{box-sizing:border-box}html{scroll-behavior:smooth}body{margin:0;background:#eeeee6;color:#233729;font:16px/1.65 system-ui,"Malgun Gothic",sans-serif}main{max-width:1440px;margin:auto;padding:38px 32px}header{max-width:1050px}.eyebrow{font-size:12px;letter-spacing:.16em;color:#536444}h1{font-size:clamp(30px,4vw,50px);line-height:1.2;margin:12px 0 18px}header p{color:#4f614c}.facts{font-size:18px;border-left:4px solid #b17f2a;padding:8px 20px;margin:24px 0}a{color:#315e42;text-underline-offset:4px}a:hover{color:#8a5910}a:focus-visible,video:focus-visible{outline:3px solid #9a681e;outline-offset:5px}nav{display:flex;gap:10px 22px;flex-wrap:wrap;border-block:1px solid #c4cdbd;padding:18px 0;margin:26px 0}.hero{max-width:1100px;margin:0 auto 38px}video{width:100%;aspect-ratio:16/9;background:#152319;display:block}.grid{display:grid;grid-template-columns:1fr 1fr;gap:34px 26px}article{min-width:0;scroll-margin-top:20px}.step{display:flex;align-items:baseline;gap:12px}.step span{font-size:18px;color:#8a611e}.step h2{font-size:24px;line-height:1.4;margin:0}.step b{margin-left:auto;font-weight:500}.range{color:#54674c;font-size:13px;margin:4px 0 12px}article p{font-size:14px;margin:9px 0 6px}article>a{font-size:13px}section{margin:46px 0}img{max-width:100%;height:auto;display:block}.downloads{display:flex;gap:24px;flex-wrap:wrap;margin:20px 0}figure{margin:0}figcaption{font-size:14px;margin-top:7px}footer{border-top:1px solid #c4cdbd;margin-top:40px;padding-top:22px;color:#53654c;font-size:14px}@media(max-width:760px){main{padding:26px 16px}.grid{grid-template-columns:1fr}.step h2{font-size:22px}nav{gap:10px 18px}h1{font-size:32px}}@media(prefers-reduced-motion:reduce){html{scroll-behavior:auto}}'''
page=f'<!doctype html><html lang="ko"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>휴게소 수정판 · Blender 300초</title><style>{css}</style><main><header><div class="eyebrow">REST STOP / BLENDER PREVIS / REVISION 02</div><h1>더 큰 실내, 쿼터뷰 방어전</h1><p>실제 게임의 상어·자동차 모델과 이동 속도를 기준으로 공간과 동선을 다시 만들었습니다. 하나의 휴게소에서 이어지는 300초를 10개 구간으로 나눠 볼 수 있습니다.</p><div class="facts">화장실 10.1배 · 편의점 7.7배 · 차량 91대 · 60초 쿼터뷰 방어</div></header>'
page+='<nav><a href="#full">전체 300초</a>'+''.join(f'<a href="#step-{i}">{i} {t}</a>' for i,t,a,b in STAGES)+'<a href="#stills">크게 보기</a></nav>'
page+='<section class="hero" id="full"><h2>전체 영상</h2>'+full+'</section><div class="grid">'+''.join(cards)+'</div>'
page+='<section id="stills"><h2>수정한 공간을 크게 보기</h2><div class="grid">'+''.join(stills)+'</div></section>'
if comparisons:page+='<section><h2>이전판과 수정판 비교</h2><p>면적 배수는 바로 이전 Blender 영상과 비교한 값입니다. 게임 공간의 단위이며 실제 건축 치수는 아닙니다.</p>'+''.join(comparisons)+'</section>'
page+=f'<section><h2>연결된 전체 휴게소</h2><a href="{overview}" target="_blank"><img src="{overview}" alt="전체 휴게소 배치"></a><div class="downloads">'+''.join(links)+'</div></section>'
page+='<footer>Blender에서 직접 렌더한 무음 프리비즈 · 1280×720 · 24fps<br>방어 중 위치 고정, 조준은 최대 90도/초로 연속 회전합니다. 기믹은 이번 프리비즈용 제안입니다. TRELLIS·Seedance를 실행하지 않았으며 Unity 씬 설치는 하지 않았습니다.<br>Blender 원본에는 전체 애니메이션이, GLB에는 정적 환경이 들어 있습니다. 이전판 파일은 보존했습니다.</footer></main><script>document.querySelectorAll("video").forEach(v=>v.addEventListener("play",()=>document.querySelectorAll("video").forEach(o=>{if(o!==v)o.pause()})))</script></html>'
(G/'index.html').write_text(page,encoding='utf8');(G/'gallery-status.json').write_text(json.dumps({'complete_clips':len(complete),'full_video':(OUT/'video-manifest.json').exists()},indent=2));print('GALLERY',len(complete),flush=True)
