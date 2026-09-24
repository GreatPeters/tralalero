from pathlib import Path
import csv,hashlib,html,json,shutil,subprocess,sys
sys.path.insert(0,str(Path('tmp/ten-run-review-2026-09-22/pythonlib').resolve()))
from PIL import Image,ImageDraw

base=Path('tmp/image-previews/highway-enemy-rebuild-2026-09-23')
gallery=Path('tmp/image-previews/bonus-gallery-2026-09-19/highway-rebuild');gallery.mkdir(parents=True,exist_ok=True)
ffmpeg=Path.home()/'AppData/Roaming/Python/Python312/site-packages/imageio_ffmpeg/binaries/ffmpeg-win-x86_64-v7.1.exe'
roles=['ConeMechanic','AsphaltWorker','TrafficPatrol','TireBruiser','DeliveryRider','TollgateChief']
labels=['라바콘 정비공','아스팔트 작업자','교통 순찰대','타이어 중장병','배달 라이더','톨게이트 대장']
def copy(path):
    path=Path(path);token=hashlib.sha256(path.read_bytes()).hexdigest()[:10];dest=gallery/(token+'-'+path.name)
    if not dest.exists():shutil.copy2(path,dest)
    return dest.name
def encode(folder,name,game=False):
    target=folder/(name+'.mp4')
    if not target.exists():
        args=[str(ffmpeg),'-hide_banner','-loglevel','error','-framerate','30','-i',str(folder/'frame-%04d.jpg'),'-c:v','libx264','-threads','4','-preset','fast','-crf','20','-pix_fmt','yuv420p','-movflags','+faststart']
        if game:args+=['-vf','scale=720:-2']
        subprocess.run(args+[str(target)],check=True)
    return target
cards=[]
sheet=Image.new('RGB',(1200,1000),(30,43,55));draw=ImageDraw.Draw(sheet)
for i,(name,label) in enumerate(zip(roles,labels)):
    folder=base/'showcase-final'/name
    still=folder/'idle.png';clip=encode(folder,name);photo=Image.open(still).convert('RGB');photo.thumbnail((400,470));x=(i%3)*400;y=(i//3)*500;sheet.paste(photo,(x,y));draw.text((x+10,y+475),name,fill='white')
    cards.append(f'<article><h3>{label}</h3><video controls muted loop playsinline preload="metadata" poster="{copy(still)}"><source src="{copy(clip)}" type="video/mp4"></video><p>실제 Unity 프리팹 · 대기 → 공격 → 피격 클립 → 사망</p></article>')
roster=base/'roster-final.png'
if not roster.exists():sheet.save(roster)
game=base/'gameplay-final-grounded'
if not (game/'complete.txt').exists():raise RuntimeError('Gameplay capture incomplete')
video=encode(game,'highway-gameplay-final',True)
rows=list(csv.DictReader((game/'timeline.csv').open()));initial=float(rows[0]['health']);impact=next((i for i,r in enumerate(rows) if float(r['health'])<initial),len(rows)-1)
still=game/f'frame-{max(0,impact-20):04}.jpg'
refs=[]
preview_refs=base/'references';preview_refs.mkdir(exist_ok=True)
for p,label in zip(sorted(Path('map-concepts/highway-enemy-rebuild-2026-09-23/references').glob('*.png')),labels):
    target=preview_refs/p.name
    if not target.exists():shutil.copy2(p,target)
    refs.append(f'<figure><img loading="lazy" src="{copy(p)}" alt="{label} 생성 참조"><figcaption>{label} · 생성 참조</figcaption></figure>')
page='''<!doctype html><html lang="ko"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>고속도로 적 재제작 · 실제 적용</title><style>
body{margin:0;background:#111b25;color:#eef2f4;font:16px/1.7 system-ui,sans-serif}main{max-width:1300px;margin:auto;padding:34px}h1{font-size:36px;line-height:1.35}h2{font-size:25px}h3{font-size:20px}a{color:#90ddff}p{color:#b6c8d4}.summary{background:#233b4d;border-left:4px solid #ffbf6a;padding:18px 24px}section{padding:30px 0;border-bottom:1px solid #344858}.grid{display:grid;grid-template-columns:repeat(3,1fr);gap:20px}article{background:#1b2b39;padding:16px;border-radius:8px}article p,figcaption,.note{font-size:13px;color:#a9bccb}video,img{max-width:100%;height:auto;border-radius:6px}article video{width:100%}.game{display:grid;grid-template-columns:minmax(260px,450px) 1fr;gap:28px;align-items:start}.game video{width:100%;max-height:820px}figure{margin:0}.references img{width:100%}table{border-collapse:collapse;width:100%;font-size:15px}td,th{text-align:left;padding:12px;border-bottom:1px solid #405567}nav{display:flex;gap:22px;flex-wrap:wrap}@media(max-width:760px){main{padding:20px}.grid{grid-template-columns:repeat(2,1fr)}.game{grid-template-columns:1fr}h1{font-size:28px}}</style><main>
'''
page+='<h1>고속도로 적 6종을 새로 만들었습니다</h1><div class="summary">새 TRELLIS 2 모델 6종 · 고속도로 50개 배치 적용 · 146개 회귀 검사 통과</div><p>이번에는 새 참조 이미지에서 3D를 다시 생성하고, 뼈·동작·무기 연결까지 적용했습니다.</p><nav><a href="#gameplay">실제 진행</a><a href="#characters">6종 동작</a><a href="#checks">검증 결과</a><a href="#references">새 레퍼런스</a></nav>'
page+=f'<section id="gameplay"><h2>옆이나 가운데로 살아 있는 적을 그냥 통과할 수 없게</h2><div class="game"><video controls muted playsinline preload="metadata" poster="{copy(still)}"><source src="{copy(video)}" type="video/mp4"></video><div><p>적 구간은 눈에 보이는 공사 차단물로 좁아집니다. 왼쪽 또는 오른쪽 적을 미리 처치하거나, 부딪혀 남은 체력만큼 서로 피해를 주고 지나갑니다.</p><p>좌우 왕복이 벌려 놓던 가운데 틈은 각자 차로 안에서 앞뒤로 걷게 바꿔 막았습니다. 두 적에 동시에 걸쳐도 접촉 피해는 한쪽만 계산합니다.</p><p>일반 구간의 차량 회피와 기존 초록색 회복 우회로는 유지했습니다. 실제 적 캡슐과 부딪혔을 때 접촉 피해를 줍니다.</p><p><strong>영상:</strong> 현재 저장 상태 HP500 / 공격력68에서 왼쪽으로 계속 피하는 실제 조작입니다. 적 접촉으로 체력이 소진되어 게임오버가 나옵니다.</p><p class="note">고정 30fps 시뮬레이션 캡처이며 기기 FPS 측정이 아닙니다. 영상에는 실제 새 모델·차단물·피해·종료 화면이 나옵니다.</p></div></div></section>'
page+='<section id="characters"><h2>새 모델과 실제 Unity 동작</h2><p>캐릭터별로 재생할 수 있습니다. 아래는 모델 확인용 고정 카메라입니다. 게임에서는 피격 반응을 공격에 더해 재생하고, 손이나 총구의 최종 자세가 갱신된 뒤 투사체를 발사합니다.</p><div class="grid">'+''.join(cards)+'</div></section>'
page+='<section id="checks"><h2>확인한 결과</h2><table><tr><th>검사</th><th>결과</th></tr><tr><td>왼쪽 끝 · 오른쪽 끝 · 가운데 입력</td><td>세 접촉 진단 모두 적 접촉 → HP0. 적 사이 통과 실패</td></tr><tr><td>실제 일반 진행</td><td>차량을 유지한 진행에서도 적 접촉 후 게임오버 확인</td></tr><tr><td>권총 · 타이어 · 보따리 · 라바콘</td><td>발사 위치 오차 0m, 실제 피격 HP500 → 400</td></tr><tr><td>원거리 적 연결 / 중복 모델</td><td>34개 모두 연결 / 50개 배치에서 중복 0</td></tr><tr><td>내보낸 모델</td><td>각 18개 뼈 · 7개 동작 · 약 2만 삼각형 · Blender 및 Unity 재검증</td></tr><tr><td>회귀 검사</td><td>146개 통과</td></tr></table><p class="note">접촉 진단 세 건은 차량만 잠시 끈 검사입니다. 투척 진단은 플레이어를 정지시키고 시험 적 HP10000 / 공격력100을 사용했습니다. 실제 게임 데이터의 체력·공격력을 바꾼 것이 아닙니다. 기존 저장값은 작업 후 복원합니다.</p></section>'
page+='<section id="references"><h2>새로 만든 생성 참조</h2><p>아래는 3D 생성에 넣은 참조 이미지입니다. 위의 게임 화면과 모델 동작은 실제 Unity 결과입니다. 도구는 프로젝트의 기존 완성 모델을 사용했습니다.</p><div class="grid references">'+''.join(refs)+'</div></section></main></html>'
page=page.replace('기존 저장값은 작업 후 복원합니다.','원래 코인·강화 레벨·튜토리얼 상태는 복원했습니다.')
(gallery/'index.html').write_text(page,encoding='utf-8');print(gallery/'index.html')
