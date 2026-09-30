"""Publish native scene evidence and fresh-import model reviews into the existing local gallery."""
from pathlib import Path
import json
import shutil

ROOT=Path(__file__).resolve().parents[1]
REPORT=ROOT/'outputs/noryangjin-interior-v2-2026-09-28'
SITE=ROOT/'outputs/noryangjin-review-homepage-2026-09-28/site'
DEST=SITE/'interior-v2';ASSETS=DEST/'assets';ASSETS.mkdir(parents=True,exist_ok=True)
native=ROOT/(REPORT/'latest-capture.txt').read_text().strip()
for name in ('aisle','turret','merchants','side-stalls'):
    shutil.copy2(native/f'{name}.png',ASSETS/f'{name}.png')
shutil.copy2(SITE/'applied/assets/before-indoor.png',ASSETS/'before.png')
models=[('N09_driven_turret','운전자가 탄 터렛트',14182),('N10_crab_aquarium','게·활어 2단 수조',7500),('N11_fish_counter','얼음·생선 진열대',8702),('N12_foam_box','수산물 스티로폼 상자',1672),('N13_merchant_male','파란 앞치마 상인',6711),('N14_merchant_female','분홍 앞치마 상인',6736)]
cards=[]
for id,title,triangles in models:
    source=REPORT/'asset-review'/id
    shutil.copy2(source/'views/perspective.png',ASSETS/f'{id}.png')
    shutil.copy2(source/'views/contact_sheet.png',ASSETS/f'{id}-views.png')
    cards.append(f'<figure><a href="assets/{id}-views.png" target="_blank"><img src="assets/{id}.png" alt="{title} 3D 모델" loading="lazy"></a><figcaption>{title}<small>{triangles:,} triangles · 다각도 보기 ↗</small></figcaption></figure>')
    if id.startswith(('N13','N14')):
        for action in ('running','walking','attack_once','hit','die'):
            shutil.copy2(source/'motion'/f'{action}-phases.png',ASSETS/f'{id}-{action}.png')
ledger=json.loads((ROOT/'outputs/meshy-noryangjin-interior-v2-2026-09-28/ledger.json').read_text())
spent=sum(x.get('consumed',0) for x in ledger)
html='''<!doctype html><html lang="ko"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>노량진 실내 · 새 모델 적용</title><link rel="stylesheet" href="../style.css"><style>
.hero{display:grid;grid-template-columns:1fr 340px;gap:70px;align-items:center;padding:55px 0}.hero h1{font-size:48px;line-height:1.25}.hero p{max-width:580px;color:var(--muted)}.hero img{height:640px;width:100%;object-fit:contain}.facts{display:flex;gap:32px;margin-top:32px}.facts b{display:block;font-family:Gmarket;font-size:25px;color:var(--blue)}.facts span{font-size:12px}.section{padding:45px 0;border-top:1px solid var(--line)}.section h2{font-size:32px}.shots{display:grid;grid-template-columns:1fr 1fr 1fr;gap:24px}.shots img{height:590px;width:100%;object-fit:contain;background:#e9edec}.shots figcaption{margin-top:12px;font-size:14px;font-weight:bold}.shots small,.models small{display:block;font-weight:normal;color:var(--muted);font-size:12px}.models{display:grid;grid-template-columns:repeat(3,1fr);gap:30px;margin-top:30px}.models img{width:100%;aspect-ratio:1;object-fit:cover}.models figcaption{font-weight:bold;margin-top:12px}.motion{display:grid;grid-template-columns:1fr 1fr;gap:20px}.motion img{width:100%}.note{padding:18px 22px;background:var(--soft);font-size:13px;border-left:3px solid var(--blue)}.links{display:flex;gap:18px;flex-wrap:wrap}.cta{display:inline-block;background:var(--blue);color:white;padding:12px 24px;text-decoration:none}.ref{display:grid;grid-template-columns:1fr 1fr;gap:20px;max-width:800px;margin:25px auto}.ref img{height:510px;width:100%;object-fit:contain;background:#e9edec}.ref figcaption{font-size:13px;margin-top:10px}.models a img,.shots a img{transition:filter .18s}.models a:hover img,.shots a:hover img{filter:brightness(1.07)}details{margin-top:24px}summary{cursor:pointer;color:var(--blue)}@media(max-width:800px){.hero{grid-template-columns:1fr;gap:24px}.hero h1{font-size:34px}.hero img{height:580px}.shots{grid-template-columns:1fr}.shots img{height:640px}.models{grid-template-columns:1fr 1fr;gap:18px}.motion{grid-template-columns:1fr}.ref img{height:360px}.facts{gap:25px}.section h2{font-size:26px}}
</style></head><body><header class="topbar"><a class="brand" href="./"><span class="brand-mark">N</span><span>노량진 실내<span class="brand-sub">UNITY SCENE / REVISION 02</span></span></a><nav><a href="#scene">게임 화면</a><a href="#models">새 모델</a><a href="#motion">동작</a></nav><span class="status">씬 적용 완료</span></header><main class="wrap">
<section class="hero"><div><p class="eyebrow">REFERENCE → NEW MODELS → NATIVE SCENE</p><h1>수조가 이어지는 시장.<br>새 상인과 터렛트.</h1><p>기존 갈색 기둥과 작은 금속 상자 수조를 정리하고, 활어 수조·생선 진열대·흰 상자로 실내를 다시 구성했습니다. 흰 천장과 매달린 조명, 따뜻한 바닥까지 실제 씬에 적용했습니다.</p><a class="cta" href="#scene">실제 화면 보기 ↓</a><div class="facts"><div><b>6종</b><span>새 Meshy 모델</span></div><div><b>2명</b><span>새 리깅 상인</span></div><div><b>SPENT</b><span>이번 Meshy 크레딧</span></div></div></div><a href="assets/turret.png" target="_blank"><img src="assets/turret.png" alt="교체된 터렛트와 시장의 Unity 게임 화면"></a></section>
<section class="section" id="scene"><p class="eyebrow">ACTUAL GAME CAMERA</p><h2>실내·터렛트·상인 돌진.</h2><p class="note">아래는 실제 Unity Play 화면입니다. 구성을 비교하기 위해 플레이어를 정지시키고 사격을 끈 시각 검사 장면입니다. 이미지 생성 예시나 전체 코스 완주 증거와 구분합니다. 이미지를 누르면 원본 PNG가 새 탭으로 열립니다.</p><div class="shots">
<figure><a href="assets/aisle.png" target="_blank"><img src="assets/aisle.png" alt="새 수조와 생선 진열대가 놓인 실내 통로"></a><figcaption>연속된 수조와 진열대<small>기둥 겹침 제거 · 번호 간판 · 흰 천장 · 매달린 조명</small></figcaption></figure>
<figure><a href="assets/turret.png" target="_blank"><img src="assets/turret.png" alt="핸들을 잡고 앞을 보는 새 터렛트 운전자"></a><figcaption>실제로 앉아 운전하는 캐릭터<small>삽 제거 · 적재함 상자 · 체력바와 피해 숫자 분리</small></figcaption></figure>
<figure><a href="assets/merchants.png" target="_blank"><img src="assets/merchants.png" alt="파란색과 분홍색 앞치마를 입은 새 상인들"></a><figcaption>새 남녀 상인<small>걷기·달리기·외침·피격·넘어지는 동작 연결</small></figcaption></figure></div>
<details><summary>이전 실내와 비교</summary><div class="ref"><figure><img src="assets/before.png" alt="이전 적용 화면"><figcaption>이전 적용 화면 · 촬영 위치는 다릅니다.</figcaption></figure><figure><img src="assets/aisle.png" alt="새 실내 화면"><figcaption>이번 실내 교체 후</figcaption></figure></div></details></section>
<section class="section" id="models"><p class="eyebrow">GENERATED ASSETS / FRESH IMPORT</p><h2>새로 만든 모델 6종.</h2><p>실내에 맞지 않았던 소품과 인물을 교체했습니다. 아래 이미지는 다운로드한 3D 모델을 새로 불러와 렌더한 검사 이미지입니다.</p><div class="models">CARDS</div><p class="note">모델 180 + 리깅 10 + 추가 동작 24 = 214크레딧. Meshy 잔액이 충분해 TRELLIS로 전환하지 않았습니다. 운전자는 차량과 함께 만든 정적 모델이며, 터렛트 이동과 상자 탈락은 게임 코드가 제어합니다.</p></section>
<section class="section" id="motion"><p class="eyebrow">ANIMATION REVIEW</p><h2>달리기와 넘어짐도 확인했습니다.</h2><div class="motion"><figure><a href="assets/N13_merchant_male-running.png" target="_blank"><img src="assets/N13_merchant_male-running.png" alt="남자 상인의 달리기 다섯 시점" loading="lazy"></a><figcaption>남자 상인 · 달리기</figcaption></figure><figure><a href="assets/N14_merchant_female-die.png" target="_blank"><img src="assets/N14_merchant_female-die.png" alt="여자 상인의 넘어짐 다섯 시점" loading="lazy"></a><figcaption>여자 상인 · 넘어짐</figcaption></figure></div><p>Meshy FBX 원본의 주요 시점 검사입니다. Unity에서는 새 모델에 연결한 클립을 사용하고, 사망 동작의 재생 길이에 맞춰 퇴장 시간을 조정했습니다.</p></section>
<section class="section"><p class="eyebrow">VERIFICATION</p><h2>적용 위치와 확인 범위.</h2><div id="verification">검증 결과를 정리하고 있습니다.</div><p class="note">맵툴 편의 탭 → 시작 스테이지 1(노량진) → Play. 적용 씬: Noryangjin_MapTool_Mode_SR18_Revamp. 모바일 실기기 성능과 자연 플레이 승률은 별도 확인이 필요합니다.</p><div class="links"><a href="../">기획 미리보기</a><a href="../applied/">앞선 개편 적용 기록</a></div></section></main><footer><div class="wrap">NORYANGJIN · REFERENCE INTERIOR V2 · 2026.09.28</div></footer><script src="verification.js"></script></body></html>'''.replace('SPENT',str(spent)).replace('CARDS',''.join(cards))
(DEST/'index.html').write_text(html,encoding='utf-8')
if not (DEST/'verification.js').exists():
    (DEST/'verification.js').write_text('',encoding='utf-8')
old=SITE/'applied/index.html'
content=old.read_text(encoding='utf-8')
banner='<p class="note"><a href="../interior-v2/">새 모델·실내 재작업 결과 보기 →</a></p>'
if '../interior-v2/' not in content:old.write_text(content.replace('<main class="wrap">','<main class="wrap">'+banner),encoding='utf-8')
print(json.dumps({'gallery':str(DEST),'native':str(native),'models':len(models),'spent':spent}))
