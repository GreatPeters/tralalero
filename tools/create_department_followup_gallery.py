import pathlib,shutil,json,html
ROOT=pathlib.Path(r'D:\Tralalero Shooter\Tralalero Shooter D')
OUT=ROOT/'tmp/image-previews/department-store-followup-2026-10-02'
D=ROOT/'outputs/department-store-2026-10-02'
V=D/'followup-v4/visual-20261002T170332889'
B=ROOT/'outputs/chapter4-reference-2026-10-02/department-final-v3-ShoeTower-20261002T162403898'
OUT.mkdir(parents=True,exist_ok=True)
manifest=[];rows=[]
def copy(src,name):
    target=OUT/name
    if target.exists() and target.read_bytes()!=src.read_bytes():raise RuntimeError('Preserve gallery evidence: '+name)
    if not target.exists():shutil.copy2(src,target)
    manifest.append({'file':name,'source':str(src)})
    return name
def fig(name,label):return '<figure><a href="'+name+'"><img loading="lazy" src="'+name+'" alt="'+html.escape(label)+'"></a><figcaption>'+html.escape(label)+'</figcaption></figure>'
def pair(title,a,b):rows.append('<section><h2>'+title+'</h2><div class="pair">'+a+b+'</div></section>')
for station in ['0000','0045','0175','0320']:
    a=copy(B/(station+'.png'),'before-'+station+'.png')
    b=copy(V/('final-'+station+'.png'),'after-'+station+'.png')
    pair('Ch5 백화점 · '+str(int(station))+'m',fig(a,'후속 보완 전 v3'),fig(b,'매장 깊이·유리·조명 보완 후 v5'))
pair('실제 조명 비교 · 같은 v5 구조와 카메라',fig(copy(V/'lights-off-0320.png','lights-off-0320.png'),'신규 조명 6개 OFF'),fig(copy(V/'final-0320.png','lights-on-0320.png'),'신규 조명 6개 ON · 가까운 매장 일부에 국소 효과'))
pair('건축 검토용 카메라 · 일반 플레이 시점 아님',fig(copy(V/'architecture-lights-off.png','architecture-lights-off.png'),'신규 조명 OFF'),fig(copy(V/'architecture-final.png','architecture-final.png'),'신규 조명 ON · 상층 유리 측벽과 진열 공간'))
rows.append('<section><h2>기존 TRELLIS F10 재사용</h2><p>기존 화분 3개를 키오스크에 재배치했습니다. 아래 확대 사진은 그중 하나를 확인하는 검토용 카메라이며, 일반 플레이에서 세 개 모두 보인다는 증거가 아닙니다.</p>'+fig(copy(V/'reused-F10-kiosk.png','reused-F10-kiosk.png'),'원래 재질을 유지한 기존 F10 · 신규 생성 아님')+'</section>')
runs=json.loads((D/'followup-v4/ordinary-routes.json').read_text(encoding='utf-8'))
assert len(runs)==2
cards=[]
for run in runs:
    s=run['summary'];assert s['outcome']=='clear' and not s['errors'] and not s['animationWarnings'] and run['restoration']['mismatches']==0
    source=pathlib.Path(next(p for p in s['nativeImages'] if 'end-clear' in p))
    mall=pathlib.Path(next(p for p in s['nativeImages'] if '035.0-route' in p))
    cards.append(fig(copy(mall,f"play-mall-choice-{run['choice']}.png"),f"선택 {run['choice']} · 35초 실제 백화점 플레이 · HUD 포함"))
    cards.append(fig(copy(source,f"play-choice-{run['choice']}.png"),f"선택 {run['choice']} · {s['seconds']:.1f}초 · 리프트 {s['lifts']}회 · 보스 처치/클리어"))
rows.append('<section><h2>현재 v5 실제 Unity 플레이</h2><p>자동 입력으로 기본 체력 60·공격 8, 1배속, 실제 PlayerMove·전투·충돌을 거쳤습니다. 순간이동·강제 승리·능력치 덮어쓰기는 없습니다. UI가 표시된 아래 화면은 위의 정지 비교 화면과 별도 증거입니다.</p><div class="pair">'+''.join(cards)+'</div></section>')
body='''<!doctype html><html lang="ko"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>백화점 후속 적용 · v3 → v5</title><style>*{box-sizing:border-box}body{margin:0;background:#edeae2;color:#25383b;font:16px/1.65 system-ui,sans-serif}main{max-width:1100px;margin:auto;padding:40px 24px}h1{font-size:34px;line-height:1.3}h2{margin-top:40px}a{color:inherit}.pair{display:grid;grid-template-columns:1fr 1fr;gap:20px}figure{margin:0 0 20px}img{display:block;width:100%;height:auto;border:1px solid #aaa}figcaption{margin:8px 0;font-weight:650}.note{padding:16px 20px;background:#dde3dc;border-left:3px solid #54756c}@media(max-width:600px){main{padding:24px 12px}.pair{gap:8px}h1{font-size:26px}}</style><main><small>TRALALERO SHOOTER / 2026.10.02</small><h1>백화점 매장 깊이와 부분 조명 보완</h1><p>Ch4 입구는 검증된 v3 그대로 유지하고, Ch5 매장 진열 깊이·유리 측벽·키오스크를 보완했습니다. 게임 동선·선택·전투·리프트·보스는 기존 구조를 유지합니다.</p><p class="note">신규 AI 생성 0개. 건축은 Unity 네이티브 메시, 화분 3개는 기존 TRELLIS 모델 재사용입니다. 생성 경로 차단, 단순한 상품과 반복적인 매장, 넓은 빈 바닥, 약한 플레이 시점 천창 표현, 모바일 실기기 검증은 남아 있습니다. 기능 검사 통과를 전체 아트 완성으로 표시하지 않습니다.</p><p>첫 네 비교는 동일한 경로 위치와 게임 카메라로 찍은 Unity 정지 화면이며 UI를 숨겼습니다. 실제 플레이 증거는 맨 아래에 따로 표시합니다. <a href="http://127.0.0.1:1624/">이전 Ch4 입구·Ch5 전체 적용 갤러리</a></p>'''
(OUT/'index.html').write_text(body+''.join(rows)+'</main></html>',encoding='utf-8')
(OUT/'sources.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps({'gallery':str(OUT),'images':len(manifest)},ensure_ascii=False))
