from pathlib import Path
import json,shutil,html,hashlib

base=Path('tmp/image-previews/review-fixes-20-runs-2026-09-22')
gallery=Path('tmp/image-previews/bonus-gallery-2026-09-19/review-fixes')
gallery.mkdir(parents=True,exist_ok=True)
def picture(source,caption):
    source=Path(source)
    if not source.exists():raise FileNotFoundError(source)
    token=hashlib.sha256(str(source).encode()).hexdigest()[:8]
    target=gallery/f'{token}-{source.name}'
    if not target.exists():shutil.copy2(source,target)
    assert source.read_bytes()==target.read_bytes()
    return f'<figure><a target="_blank" href="{target.name}"><img loading="lazy" src="{target.name}" alt="{html.escape(caption)}"></a><figcaption>{html.escape(caption)}</figcaption></figure>'
reports=[]
for p in sorted((base/'runs').glob('run-*/summary.json')):
    r=json.loads(p.read_text(encoding='utf-8'));r['run']=int(p.parent.name[-2:]);reports.append(r)
clear=sum(r['outcome']=='clear' for r in reports)
deaths=sum(r['outcome']=='death' for r in reports)
cards=[]
def section(title,copy,images,id=''):
    cards.append(f'<section id="{id}"><h2>{title}</h2><p>{copy}</p><div class="photos">'+''.join(picture(p,c) for p,c in images)+'</div></section>')
section('심한 가림만 반투명 처리','상어 몸통과 바로 앞길을 함께 크게 가릴 때만 약 0.2초 동안 불투명도 40%로 전환합니다. 작은 가림과 구조물 위치는 유지합니다.',[
 ('tmp/image-previews/ten-run-review-2026-09-22/run-01/003-008.0-route.png','수정 전 · 회색 구조물이 상어와 길을 덮음'),
 (base/'preflight-harbor-v1/frame-031.png','수정 후 · 1배속 실제 통과 화면, 구조물 뒤의 상어와 길이 보임')])
results=list((base/'runs/run-06').glob('*result.png'))
section('접촉 위험과 마지막 피해 원인','다가오는 적의 남은 체력을 기준으로 큰 접촉 피해를 표시하고, 닿으면 죽는 경우에는 사망 경고를 표시합니다. 패배 화면에는 실제 마지막 피해 원인·체력 손실·이번 판 최종 능력치가 나옵니다.',[
 (base/'hud-v1/threat.png','경고 기능 검증 · 실제 충돌 전 사망 위험 표시. 확인을 위해 적 위치를 옮긴 별도 검사 장면.'),
 (results[-1] if results else base/'hud-v1/defeat.png','실제 재검증 종료 화면 · 원인과 손실 체력, 다음 행동 안내')])
section('여성 보스와 고속도로 적','여성 보스의 손가락 파지와 손잡이 축을 맞추고 앞치마 음영을 보강했습니다. 고속도로 적 50개 배치와 원본 6종은 약 40% 확대하고 장갑·머리 비율을 조정했습니다.',[
 (base/'characters-final/Enemy_Woman.png','여성 보스 · 손잡이 위치·파지·음영 보정'),
 (base/'characters-final/AsphaltWorker.png','고속도로 적 · 확대된 몸에 맞춘 장갑·머리 비율. 크기는 아래 실제 게임 화면에서 확인.')])
section('위험 차로는 줄무늬로 구분','초록·분홍 경로 색은 유지하고 차량 위험 표시를 넓은 주황 줄무늬로 바꿨습니다. 차량이 지나갈 때까지 위험 표시가 유지됩니다.',[
 ('tmp/image-previews/ten-run-review-2026-09-22/run-07/003-008.1-route.png','이전 고속도로 화면 · 작은 적과 단색 위험선'),
 (base/'preflight-highway-v1/frame-028.png','수정 후 1배속 확인 · 더 큰 적과 줄무늬 위험 표시. 비교 장면의 성장 상태는 서로 다름.')])
section('공격력·체력 보너스는 표시한 만큼','앞서 수정한 고정 증가량 계약을 유지했습니다. 실제 보너스 충돌 획득 검사에서 공격력 +12는 68→80, 체력 +14는 500→514로 확인했습니다.',[
 ('tmp/image-previews/bonus-amount-fix-2026-09-22/143326/Normal-att-12-before.png','실제 보너스 획득 전 · 공격력 68 / +12'),
 ('tmp/image-previews/bonus-amount-fix-2026-09-22/143326/Normal-att-12-after.png','획득 후 · 공격력 80')])
section('반복감 개선 이미지만 제안 · 게임 미적용','지원군 수·배치는 변경하지 않았습니다. 상점·간판·적 배치의 반복도 실제 게임에서는 유지했습니다. 아래는 점포 변주와 하역장 구분을 비교한 이미지 시안입니다.',[(base/'concepts/repetition-options.png','생성 시안 · 실제 게임에 적용하지 않음')],'concept')
section('반복된다고 판단했던 실제 장면','같은 파란 차양·생선 간판과 좌우 한 쌍 배치가 반복되는 모습을 지적했던 이전 검토 사진입니다. 배치는 유지했으며, 아래 여성 보스는 수정 전 모습입니다.',[
 ('tmp/image-previews/ten-run-review-2026-09-22/run-02/044-112.3-route.png','이전 검토 · 노량진 중반의 점포 모듈과 좌우 한 쌍 배치'),
 ('tmp/image-previews/ten-run-review-2026-09-22/run-04/104-296.8-route.png','이전 검토 · 노량진 후반의 차양·간판 비교용. 여성 보스 수정 전 사진')],'repetition')
section('후반 챕터를 성장 상태로 다시 확인','고속도로 강화 후 체력 위주 선택은 클리어했습니다. 휴게소도 식당 포위전을 버티고 출구로 나가는 구간까지 확인했습니다. 휴게소 전체 클리어는 이번 기록에 없습니다.',[
 (sorted((base/'runs/run-14').glob('*result.png'))[-1],'14판 · 고속도로 클리어 후 휴게소 전환 화면'),
 (base/'runs/run-19/045-121.8-route.png','19판 · 휴게소 식당 포위전 진행. 이후 탈출했으나 189.6초에 차량 충돌로 사망')],'growth')
chapters={'Noryangjin_MapTool_Mode_SR18':'노량진','HighWay':'고속도로','RestStop':'휴게소'}
profiles={'current':'현재 저장 상태','harbor-growth':'노량진 성장 후','highway-entry':'고속도로 진입','highway-ready':'고속도로 강화 후','reststop-entry':'휴게소 진입','reststop-ready':'휴게소 강화 후'}
causes={'Crate':'뚱보 상자','EnemyContact':'적과 충돌','Traffic':'차량','Roadblock':'도로 장애물','TollGate':'닫힌 차단기','Hole':'구멍','Seagull':'갈매기','Pole':'가로등','GuardShot':'경비원 탄환','EnemyProjectile':'적의 투척물'}
rows=''
for r in reports:
    start=next(e for e in r['events'] if e['kind']=='start');reason=causes.get(r.get('fatalCause'),r.get('fatalCause')) if r['outcome']=='death' else '—'
    rows+=f'<tr><td>{r["run"]:02}</td><td>{chapters[r["scene"]]}</td><td>{profiles.get(r["profile"],r["profile"])}</td><td>{start["health"]:,.0f} / {start["attack"]:,.0f}</td><td>{r["seconds"]:.1f}초</td><td>{"클리어" if r["outcome"]=="clear" else "사망" if r["outcome"]=="death" else r["outcome"]}</td><td>{reason}</td></tr>'
page='''<!doctype html><html lang="ko"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>수정 확인 · 20판 재검증</title><style>
body{margin:0;background:#101923;color:#eef4f6;font:16px/1.7 system-ui,sans-serif}main{max-width:1300px;margin:auto;padding:36px}h1{font-size:37px;line-height:1.3}h2{font-size:25px;line-height:1.4}p{color:#bbcbd6;max-width:1020px}a{color:#85dcff}.summary{padding:20px 25px;background:#203948;border-left:4px solid #ecc46f;border-radius:4px}section{padding:28px 0 38px;border-bottom:1px solid #344450}.photos{display:flex;gap:20px;align-items:flex-start}figure{margin:0;flex:1;max-width:560px}img{width:100%;height:auto;border-radius:8px}figcaption{font-size:13px;color:#a9bdca;padding-top:8px}#concept figure{max-width:none}.table{overflow:auto}table{border-collapse:collapse;width:100%;font-size:14px}th,td{padding:10px;border-bottom:1px solid #334650;text-align:left;white-space:nowrap}.note{font-size:14px;color:#adbcbf}@media(max-width:720px){main{padding:20px}h1{font-size:28px}.photos{gap:10px;flex-wrap:wrap}figure{min-width:46%}}</style><main>
'''+f'<h1>요청한 수정 적용과 20판 재검증</h1><div class="summary">현재 기록 {len(reports)}/20판 · 클리어 {clear}판 · 사망 {deaths}판</div><p>게임 수정 화면은 실제 Unity 캡처입니다. 반복감 개선 이미지는 별도의 미적용 시안으로 표시했습니다.</p>'+''.join(cards)+f'''
<section><h2>20판 기록</h2><p>각 챕터의 성장 상태는 실제 강화표에 존재하는 구매 레벨로 재현했습니다. 임의의 무적·체력 유지·공격력 덮어쓰기는 사용하지 않았습니다. 반복 구간은 3배속 자동 좌우 조작, 연출 확인은 별도 1배속입니다.</p><div class="table"><table><tr><th>판</th><th>맵</th><th>성장 상태</th><th>시작 HP / 공격력</th><th>게임 시간</th><th>결과</th><th>마지막 원인</th></tr>{rows}</table></div><p class="note">성장 프로필은 재현 가능한 테스트 조건이며 자연 육성에 필요한 판 수나 재화 수급을 검증했다는 뜻은 아닙니다. 첫 후반 12판은 자동 회피의 방향 선택 문제를 찾아 별도로 보관하고 다시 실행했습니다. 위 표는 보정한 후반 12판과 노량진 8판이며, 전체로는 종료된 시도 32판과 일시정지된 부분 기록 1개가 남아 있습니다. 자동조작의 결과를 사람의 승률로 해석하지 않습니다.</p></section></main></html>'''
(gallery/'index.html').write_text(page,encoding='utf-8')
(gallery/'run-summary.json').write_text(json.dumps(reports,ensure_ascii=False,indent=2),encoding='utf-8')
print(f'{gallery}/index.html; completed={len(reports)} clear={clear} death={deaths}')
