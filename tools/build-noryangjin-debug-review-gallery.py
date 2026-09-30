"""Build a review gallery from recorded native frames, without replacing source evidence."""
from pathlib import Path
import json
import shutil
import csv

root=Path(__file__).resolve().parents[1]
report=root/'outputs/noryangjin-debug-review-2026-09-28'
site=root/'outputs/noryangjin-review-homepage-2026-09-28/site'
out=site/'debug-review';assets=out/'assets';assets.mkdir(parents=True,exist_ok=True)
runs=json.loads((report/'final-runs.json').read_text(encoding='utf-8-sig'))
before=root/'tmp/image-previews/noryangjin-revamp-fix-2026-09-28/play-debug-review-inside-050620'
after=root/runs['inside']['folder']
outside_folder=root/runs['outside']['folder']
def at(x,z,folder=after):
    frames=list(csv.DictReader((folder/'frames.csv').open()))
    return min(frames,key=lambda f:(float(f['x'])-x)**2+(float(f['z'])-z)**2)['frame'].removeprefix('frame-')
comparisons=[('물청소','033',at(124.3,-94),'불투명 원통을 움직이는 물줄기·방울로 바꾸고, 바닥 위에 분사 범위를 표시했습니다.'),('상자 벽과 코인','044',at(124.3,-267),'높이 떠 있던 큰 코인을 바닥 가까이 낮추고 크기를 줄였습니다. 체력과 피해 숫자도 작게 정리했습니다.'),('셔터 통과 공간','047',at(124.3,-313),'시간이 남아 있으면 상어가 통과할 높이를 유지합니다. 시간이 끝나면 완전히 닫혀 파괴 대상으로 바뀝니다.'),('냉동창고','054',at(-67,-302),'옛 도로 기둥과 가까운 커튼의 가림을 정리하고, 기둥을 숨겨도 바닥이 이어지도록 보완했습니다.'),('경매장 사격 정렬','082',at(380,203.5,outside_folder),'중앙 사격에 대응하는 적이 있도록4명 배치를3명으로 정렬했습니다.')]
sections=[]
for i,(title,old,new,description) in enumerate(comparisons):
    after_source=outside_folder if title=='경매장 사격 정렬' else after
    for kind,folder,frame in [('before',before,old),('after',after_source,new)]:shutil.copy2(folder/f'frame-{frame}.png',assets/f'{i}-{kind}.png')
    sections.append(f'<section class="comparison"><h2>{title}</h2><p>{description}</p><div class="pair"><figure><a href="assets/{i}-before.png" target="_blank"><img src="assets/{i}-before.png" alt="{title} 수정 전" loading="lazy"></a><figcaption>수정 전</figcaption></figure><figure><a href="assets/{i}-after.png" target="_blank"><img src="assets/{i}-after.png" alt="{title} 수정 후" loading="lazy"></a><figcaption>수정 후</figcaption></figure></div></section>')
for name in ('inside','outside','inside_clear'):
    shutil.copy2(root/runs[name]['folder']/'result.json',out/f'{name}-result.json')
plan=root/'docs/exec-plans/completed/noryangjin-debug-playtest-fixes-2026-09-28.md'
if not plan.exists():plan=root/'docs/exec-plans/active/noryangjin-debug-playtest-fixes-2026-09-28.md'
shutil.copy2(plan,out/'plan.md')
inside=runs['inside_clear']['elapsed'];outside=runs['outside']['elapsed']
html=f'''<!doctype html><html lang="ko"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>노량진 · 플레이 검수와 수정</title><link rel="stylesheet" href="../style.css"><style>
.review-hero{{padding:65px 0 40px}}.review-hero h1{{font-size:43px}}.stats{{display:flex;gap:35px;flex-wrap:wrap;padding:25px 0;border-block:1px solid var(--line)}}.stats b{{display:block;font-size:25px;color:var(--blue);font-family:Gmarket}}.stats small{{font-size:12px;color:var(--muted)}}.comparison{{padding:40px 0;border-bottom:1px solid var(--line)}}.comparison h2{{font-size:30px}}.pair{{display:grid;grid-template-columns:1fr 1fr;gap:24px;max-width:780px;margin:24px auto}}.pair img{{height:650px;width:100%;object-fit:contain;background:#e7ecec}}.pair figcaption{{text-align:center;font-weight:bold;margin-top:10px}}.note{{padding:18px 22px;background:var(--soft);font-size:13px}}.links{{display:flex;gap:22px;flex-wrap:wrap}}.proof{{padding:40px 0}}@media(max-width:700px){{.review-hero h1{{font-size:31px}}.stats{{gap:20px}}.pair{{gap:10px}}.pair img{{height:390px}}.comparison h2{{font-size:24px}}}}
</style></head><body><header class="topbar"><a class="brand" href="./"><span class="brand-mark">N</span><span>노량진 플레이 검수<span class="brand-sub">PLAYTEST / FIXES / VERIFICATION</span></span></a><nav><a href="#changes">수정 전후</a><a href="plan.md">수정 기획서</a><a href="../interior-v2/">모델 적용 기록</a></nav><span class="status">테스트 옵션 ON</span></header><main class="wrap"><section class="review-hero"><p class="eyebrow">2026.09.28 / ACTUAL UNITY PLAY</p><h1>플레이하며 찾은 문제를<br>씬에 반영했습니다.</h1><p>공격력·체력9999와 빠른 좌우 이동을 켜고 검수했습니다. 경매장 중앙 사격의 틈, 코인·숫자 가림, TV·물줄기·셔터 표현과 냉동창고 시야를 수정했습니다.</p><div class="stats"><div><b>두 옵션 ON</b><small>마지막 Editor 설정도 유지</small></div><div><b>{inside:.1f}초</b><small>안쪽1배속 완주 · 최종 표시 조정 전</small></div><div><b>{outside:.1f}초</b><small>최종 바깥 코스 완주</small></div><div><b>38 / 38</b><small>관련 EditMode 검사</small></div></div></section><section id="changes"><p class="note">모두 실제 Play 화면입니다. 자동 좌우 조작과 테스트 옵션으로 기능·표현을 확인한 결과이며 자연 플레이 승률이나 정식 난이도 검증은 아닙니다. 전후 이미지는 같은 구간의 별도 실행이라 정확한 촬영 위치·수치가 다릅니다. 원본은 이미지를 눌러 열 수 있습니다.</p>{''.join(sections)}</section><section class="proof"><h2>경매장 중앙 사격도 확인했습니다.</h2><p>수정 전에는4명 사이의 중앙 틈으로 미사일이 통과했지만 상어는 적과 부딪혔습니다. 중앙 적이 있는3명 배치로 바꾼 뒤, 실제 사격으로 중앙 적 체력이0이 되고 코스가 이어지는 것을 확인했습니다.</p><p>최종 바깥 분기는3배속302.3초에 완주했습니다. 안쪽은 핵심 수정 후1배속306.7초에 완주했고, 마지막 표시 조정 후3배속 반복에서는243.2초 기존 즉사홀에서 중단됐습니다. 이 실패 기록도 보존했습니다. 현재 설정은1배속·9999·빠른 좌우 이동ON으로 정리했습니다.</p><div class="links"><a href="plan.md">수정 기획서</a><a href="inside_clear-result.json">안쪽1배속 완주 기록</a><a href="inside-result.json">안쪽 추가 실패 기록</a><a href="outside-result.json">바깥 실행 기록</a></div><p class="note">원본 SR18·Data.xlsx·Build Settings와 실제 재화/성장 저장값을 보존합니다. 씬 재생성 시 발생하던 Editor URP 예외는 별도 운영 기록에 남겨 두었습니다.</p></section></main><footer><div class="wrap">NORYANGJIN / PLAYTEST REVIEW · <a href="../interior-v2/">새 모델과 실내 적용 기록</a></div></footer></body></html>'''
(out/'index.html').write_text(html,encoding='utf-8')
previous=site/'interior-v2/index.html';content=previous.read_text(encoding='utf-8')
if '../debug-review/' not in content:previous.write_text(content.replace('<main class="wrap">','<main class="wrap"><p class="note"><a href="../debug-review/">최신 플레이 검수와 후속 수정 보기 →</a></p>'),encoding='utf-8')
print(out)
