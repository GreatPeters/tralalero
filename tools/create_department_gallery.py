import pathlib,shutil,json,html
ROOT=pathlib.Path(r'D:\Tralalero Shooter\Tralalero Shooter D')
OUT=ROOT/'tmp/image-previews/department-store-2026-10-02'
OUT.mkdir(parents=True,exist_ok=True)
E=ROOT/'outputs/chapter4-reference-2026-10-02'
D=ROOT/'outputs/department-store-2026-10-02'
rows=[]; manifest=[]
def copy(src,name):
    target=OUT/name
    if target.exists() and target.read_bytes()!=src.read_bytes():raise RuntimeError('Preserve existing gallery image: '+name)
    if not target.exists():shutil.copy2(src,target)
    manifest.append({'file':name,'source':str(src)})
    return name
def fig(name,label):return '<figure><a href="'+name+'"><img loading="lazy" src="'+name+'" alt="'+html.escape(label)+'"></a><figcaption>'+html.escape(label)+'</figcaption></figure>'
def pair(title,a,b):rows.append('<section><h2>'+title+'</h2><div class="pair">'+a+b+'</div></section>')
for station in ['0000','0045','0175','0320']:
    a=copy(E/'handoff-before-ShoeTower-20261002T153906186'/f'{station}.png',f'tower-before-{station}.png')
    b=copy(E/'department-final-v3-ShoeTower-20261002T162403898'/f'{station}.png',f'tower-after-{station}.png')
    pair(f'Ch5 · {int(station)}m',fig(a,'인계 당시'),fig(b,'백화점 적용 후'))
details=sorted(D.glob('art-detail-*'))[-1]
for station in ['1720','1800','1830']:
    a=copy(details/f'entrance-before-{station}.png',f'entrance-before-{station}.png')
    b=copy(details/f'entrance-after-{station}.png',f'entrance-after-{station}.png')
    pair('Ch4 입구 · '+station+'m',fig(a,'이번 입구 추가 전'),fig(b,'이번 입구 추가 후'))
rows.append('<section><h2>구조 검토용 별도 카메라</h2><p>아래 두 장은 일반 플레이 카메라가 아닙니다. 곡선 보이드와 계단 도착부의 연결을 보는 건축 검토용입니다.</p><div class="pair">'+''.join(fig(copy(details/(n+'.png'),n+'.png'),t) for n,t in [('atrium-architecture','아트리움 구조'),('escalator-landings','에스컬레이터 도착부')])+'</div></section>')
routes=json.loads((D/'ordinary-routes.json').read_text(encoding='utf-8'))
for r in routes:r['revision']='v2'
polish=json.loads((D/'ordinary-polish-routes.json').read_text(encoding='utf-8'))
for r in polish:r['revision']='최종 v3'
routes+=polish
actual=[]
for run in routes:
    s=run['summary']; picks=[p for p in s['nativeImages'] if 'end-clear' in p]
    if picks:actual.append(fig(copy(pathlib.Path(picks[-1]),f"play-{run['revision'].replace(' ','-')}-{run['scene']}-{run['choice']}.png"),f"{run['revision']} · {run['scene']} 선택 {run['choice']} · {s['seconds']:.1f}초 클리어"))
rows.append('<section><h2>실제 플레이와 최종 장식 수정 후 재검증</h2><p>v2에서 네 경로, 안내판과 에스컬레이터 외장을 고친 최종 v3에서 두 경로를 추가 완주했습니다. 1배속·기존 능력치·자동 입력이며 강제 승리/순간이동은 없습니다. 사람의 재미 평가 또는 휴대전화 성능 인증은 아닙니다.</p><div class="pair">'+''.join(actual)+'</div></section>')
body='''<!doctype html><html lang="ko"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>백화점 적용 · Unity 실제 화면</title><style>*{box-sizing:border-box}body{margin:0;background:#edeae2;color:#25383b;font:16px/1.65 system-ui,sans-serif}main{max-width:1100px;margin:auto;padding:40px 24px}h1{font-size:34px;line-height:1.3}h2{margin-top:40px}a{color:inherit}.pair{display:grid;grid-template-columns:1fr 1fr;gap:20px}figure{margin:0 0 20px}img{display:block;width:100%;height:auto;border:1px solid #aaa}figcaption{margin:8px 0;font-weight:650}.note{padding:16px 20px;background:#dde3dc;border-left:3px solid #54756c}@media(max-width:600px){main{padding:24px 12px}.pair{gap:8px}h1{font-size:26px}}</style><main><small>TRALALERO SHOOTER / 2026.10.02</small><h1>거리 끝 백화점 → 타워 상업층</h1><p>기존 잠실 거리·보행자·푸른 타워·옥상 운동화를 유지하고, 입구와 Ch5 1층에 곡선 보이드·유리 난간·교차 에스컬레이터·상점을 연결했습니다.</p><p class="note">전후 비교는 같은 거리와 렌즈의 Unity 편집 모드 캡처입니다. UI를 숨겼습니다. 실제 플레이 증거는 마지막에 구분했습니다. 이번 추가물은 Unity 건축 메시이며 신규 TRELLIS/Meshy 생성물이 아닙니다. AI 생성 경로와 실기기 성능은 미검증입니다.</p>'''
(OUT/'index.html').write_text(body+''.join(rows)+'</main></html>',encoding='utf-8')
(OUT/'sources.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps({'gallery':str(OUT),'images':len(manifest)},ensure_ascii=False))
