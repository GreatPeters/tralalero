from pathlib import Path
import json,shutil,sys,hashlib
ROOT=Path.cwd();OUT=ROOT/'outputs/reststop-blender-v4-2026-09-23';G=ROOT/'tmp/image-previews/reststop-blender-v4-2026-09-23';G.mkdir(exist_ok=True)
sys.path.insert(0,str(ROOT/'tmp/ten-run-review-2026-09-22/pythonlib'))
from PIL import Image,ImageDraw,ImageFont
records=json.loads((OUT/'images.json').read_text(encoding='utf8'));font=ImageFont.truetype('C:/Windows/Fonts/malgunbd.ttf',24);sheet=Image.new('RGB',(1600,2510),'#eeeee6');draw=ImageDraw.Draw(sheet)
def copy(p):
 dst=G/p.name
 if not dst.exists() or hashlib.sha256(dst.read_bytes()).digest()!=hashlib.sha256(p.read_bytes()).digest():shutil.copy2(p,dst)
 return p.name
notes=['진입로의 정해진 곡선을 따라 자동 전진','차종 6종을 섞은 주차장 · 경로 안 좌우 회피','카트를 제거한 건물 앞 휴식 공간','정해진 입구 동선을 따라 자동 진입','제자리 쿼터뷰 방어 · 60초 · 연속 회전 최대 90도/초','카트를 제거한 식당 · 고정된 통과 경로','넓은 매장 통로 · 진열대 기믹','확대 화장실의 지정 통로 · 청소 로봇','여러 차량과 버스가 있는 주유소 동선','정해진 곡선 출구를 따라 자동 전진']
cards=[]
for i,r in enumerate(records):
 name=copy(OUT/'images'/r['file']);im=Image.open(G/name).convert('RGB');im.thumbnail((800,450));x=(i%2)*800;y=(i//2)*502;sheet.paste(im,(x,y+42));draw.text((x+15,y+7),r['step']+' '+r['title'],font=font,fill='#203a2a')
 cards.append(f'<article id="step-{r["step"]}"><div class="title"><span>{r["step"]}</span><h2>{r["title"]}</h2></div><a href="{name}" target="_blank"><img src="{name}" alt="{r["step"]} {r["title"]} 수정된 Blender 장면"></a><p>{notes[i]}</p><a class="save" href="{name}" download>원본 PNG 저장 ↓</a></article>')
sheet.save(G/'01-10-contact-sheet.png');copy(OUT/'vehicle-lineup.png');copy(OUT/'vehicle-before.png')
comparison=Image.new('RGB',(3600,1460),'#eeeee6');d=ImageDraw.Draw(comparison)
for i,(name,label) in enumerate([('vehicle-before.png','이전 크기'),('vehicle-lineup.png','수정 · 가로/세로/높이 1.8배')]):
 comparison.paste(Image.open(G/name).convert('RGB'),(i*1800,60));d.text((i*1800+24,16),label,font=font,fill='#203a2a')
comparison.save(G/'vehicle-scale-comparison.png')
downloads=[]
for file,label in [('reststop-v4.blend','수정된 Blender 원본'),('reststop-v4-environment.glb','정적 환경 GLB')]:
 if (OUT/file).exists():downloads.append(f'<a href="{copy(OUT/file)}" download>{label} ↓</a>')
css='''*{box-sizing:border-box}html{scroll-behavior:smooth}body{margin:0;background:#eeeee6;color:#233729;font:16px/1.65 system-ui,"Malgun Gothic",sans-serif}main{max-width:1500px;margin:auto;padding:42px 32px}.eyebrow{font-size:12px;letter-spacing:.16em;color:#536444}h1{font-size:clamp(30px,4vw,48px);line-height:1.25;margin:12px 0 20px}header{max-width:1050px}header p{color:#4f614c}.legend{display:flex;gap:28px;flex-wrap:wrap;border-left:4px solid #b17f2a;padding:10px 18px;margin:24px 0}.gold{color:#84530c}.blue{color:#126274}nav{display:flex;gap:12px 24px;flex-wrap:wrap;border-block:1px solid #c4cdbd;padding:18px 0;margin:30px 0}a{color:#315e42;text-underline-offset:4px}a:hover{color:#8a5910}a:focus-visible{outline:3px solid #986715;outline-offset:4px}.grid{display:grid;grid-template-columns:1fr 1fr;gap:38px 28px}article{scroll-margin-top:20px;min-width:0}.title{display:flex;align-items:baseline;gap:12px;margin-bottom:12px}.title span{color:#916a22;font-size:20px}h2{font-size:25px;margin:0}.title h2{margin:0}img{display:block;max-width:100%;height:auto}article p{font-size:14px;margin:10px 0 6px}.save{font-size:13px}section{margin:50px 0}.downloads{display:flex;gap:26px;flex-wrap:wrap;margin:25px 0}footer{margin-top:36px;border-top:1px solid #c4cdbd;padding-top:20px;font-size:14px;color:#53654c}@media(max-width:760px){main{padding:28px 16px}.grid{grid-template-columns:1fr}.title h2{font-size:23px}}@media(prefers-reduced-motion:reduce){html{scroll-behavior:auto}}'''
page=f'<!doctype html><html lang="ko"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>휴게소 차량 확대 · 01–10</title><style>{css}</style><main><header><div class="eyebrow">REST STOP / BLENDER / REVISION 04</div><h1>차량을 1.8배 키웠습니다</h1><p>상어·사람 크기를 기준으로 차량 6종 전체를 확대하고 주차 위치와 간격을 다시 맞췄습니다. 아래는 수정된 Blender 원본에서 촬영한 1–10번 이미지입니다.</p><div class="legend"><span class="gold">노란 점선·화살표: 자동 진행 경로</span><span class="blue">파란 경계: 좌우 이동 통로</span></div></header>'
page+='<nav>'+''.join(f'<a href="#step-{r["step"]}">{r["step"]} {r["title"]}</a>' for r in records)+'<a href="01-10-contact-sheet.png" target="_blank">10장 한눈에 보기</a></nav><div class="grid">'+''.join(cards)+'</div>'
page+='<section><h2>같은 카메라에서 크기 비교</h2><p>상어·사람과 카메라 배율은 고정했습니다. 승용차·트럭·버스의 가로·세로·높이를 각각 1.8배 확대했습니다.</p><a href="vehicle-scale-comparison.png" target="_blank"><img src="vehicle-scale-comparison.png" alt="차량 확대 전후 비교"></a><p><a href="vehicle-lineup.png" target="_blank">커진 차량과 캐릭터를 크게 보기 ↗</a></p></section><div class="downloads">'+''.join(downloads)+'</div><footer>1600×900 PNG 10장 · 차량 6종 91대 · 바닥 안내선은 이동 규칙을 확인하기 위한 프리비즈 표시입니다.<br>5번은 제자리 방어 구간입니다. Blender 원본에는 수정된 300초 타임라인이 들어 있습니다. Unity 씬 설치와 TRELLIS·Seedance 실행은 하지 않았습니다.</footer></main></html>'
(G/'index.html').write_text(page,encoding='utf8');(G/'gallery-status.json').write_text(json.dumps({'images':10,'source':'native Blender v4','video_rendered_this_revision':False},indent=2));print('GALLERY_READY',str(G),flush=True)
