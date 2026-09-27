"""Build five proposal pages and a durable ideation record. No Unity files are written."""
import json
import re
import shutil
import subprocess
import sys
from pathlib import Path

BASE = Path(__file__).resolve().parents[1]
ROOT = BASE / 'outputs/highway-concepts-2026-09-26'
SITE = ROOT / 'site'
data = json.loads((ROOT / 'concepts.json').read_text(encoding='utf-8'))
assert len(data['concepts']) == 5
assets = BASE / 'Assets/ShooterSurvival/Prefabs/MeshyRestStop20260925'
prefabs = [p.name for p in assets.glob('*.prefab')]
for c in data['concepts']:
    assert len(c['gimmicks']) == 4 and len(c['timeline']) == 6
    previous = 0
    for span, title, text in c['timeline']:
        start, end = map(int, re.findall(r'\d+', span))
        assert start == previous and end > start, (c['id'], span)
        previous = end
    assert previous == 300
    codes = set(re.findall(r'\b(?:V|P|A|C)\d{2}\b', json.dumps(c, ensure_ascii=False)))
    for code in codes:
        assert any(p.startswith(code + '_') for p in prefabs), f'Missing prefab code {code}'

SITE.mkdir(parents=True, exist_ok=True)
(SITE / 'images').mkdir(exist_ok=True)
subprocess.run([sys.executable, str(BASE / 'tools/highway-flow-art.py')], check=True)
page = (BASE / 'tools/highway-concepts.html').read_text(encoding='utf-8')
page = page.replace('</head>', '<link rel="stylesheet" href="flow.css"></head>')
page = page.replace('<h1>같은 고속도로,<br><em>다섯 가지</em><br>플레이.</h1>', '<h1>시작부터 끝까지,<br><em>그림으로 보는</em> 고속도로.</h1>')
page = page.replace('<a href="#proposal">기획 상세</a>', '<a href="#full-flow">전체 흐름 그림</a>')
page = page.replace('<div class="quickcompare">', '<details class="comparison-fold"><summary>5안 비교표 펼치기</summary><div class="quickcompare">')
page = page.replace('</tbody></table></div></section>', '</tbody></table></div></details></section>', 1)
page = page.replace('<section id="proposal" class="proposal" aria-live="polite"></section>', '<section id="full-flow" class="full-flow" aria-label="선택한 기획안의 전체 흐름 그림"></section><details class="text-fold"><summary>기믹·보상·제작 범위의 상세 설명 펼치기</summary><section id="proposal" class="proposal" aria-live="polite"></section></details>')
page = page.replace('<script src="concept-data.js"></script>', '<script src="flow-data.js"></script><script src="flow.js"></script><script src="concept-data.js"></script>')
page = page.replace('renderProposal();history.replaceState', 'renderProposal();renderFullFlow();history.replaceState')
page = page.replace("$('proposal').scrollIntoView", "$('full-flow').scrollIntoView")
page = page.replace('const m=location.hash.match', "const openFlowAtLoad=location.hash==='#full-flow';const m=location.hash.match")
page = page.replace('selectPlan(m?+m[1]:1);', "selectPlan(m?+m[1]:1);if(openFlowAtLoad)requestAnimationFrame(()=>$('full-flow').scrollIntoView({behavior:'auto'}));")
page = page.replace('<body>', '<body><a href="index.html" style="display:block;padding:12px 4vw;background:#dfe8d2;font-size:14px">← 쉬운 그림 설명으로 돌아가기</a>')
(SITE / 'details.html').write_text(page, encoding='utf-8')
shutil.copy2(BASE / 'tools/highway-concepts-flow.css', SITE / 'flow.css')
shutil.copy2(BASE / 'tools/highway-concepts-flow.js', SITE / 'flow.js')
shutil.copy2(BASE / 'outputs/highway-analysis-2026-09-26/site/NotoSansKR.ttf', SITE / 'NotoSansKR.ttf')
images = [('coverage-left-d-0709.webp','trucks.webp'),('coverage-left-d-1460.webp','toll.webp'),('coverage-right-d-1693.webp','fork.webp'),('coverage-right-d-2657.webp','log-truck.webp')]
for source, name in images:
    shutil.copy2(BASE / 'outputs/highway-analysis-2026-09-26/site/images' / source, SITE / 'images' / name)
(SITE / 'concept-data.js').write_text('const CONCEPTS = ' + json.dumps(data, ensure_ascii=False) + ';', encoding='utf-8')

md = ['---','title: 고속도로 전면 개편 기획 5안','date: 2026-09-26','status: proposed-not-implemented','---','','# 고속도로 전면 개편 기획 5안','','사용자 요청: 재미 중심으로 상세 기획 5개와 웹페이지. **개발 적용 금지.** 아래 수치·자산 추가·동작은 제안이며 구현·재미·경제 검증 결과가 아니다.','','## 근거와 범위','','- 현재 자동 전진·좌우 드래그·자동 사격, 곡선 분기, 차량·고라니·통나무·공사·요금소 자산을 확인했다.','- 기존 일반 자동 전투 10회는 초반 접촉 종료. 별도 보정 관찰 2회와 혼합하지 않으며 사람의 승률 근거로 사용하지 않는다.','- 첫 우회로 대형 산 모델 가림과 단순 반복을 출발점으로 삼는다.','- 기획 검토 축: 순간 주행 판단 / 전투 목표 / 경로 선택 / 장치 상호작용 / 기억에 남는 대결.','- 아이디어 생성·비판은 저장소 지침에 따라 메인 스레드에서 순차 수행했다.','','## 공통 전제']
for rule in data['common']:
    md += ['', '### ' + rule['title'], '', rule['text']]
md += ['', '## 추천 순위', '', '1. 교통 틈새 돌파', '2. 갈림길 원정', '3. 레커 대장 추격전', '4. 호송 작전', '5. 공사 구간 돌파', '', '순위·확신도는 에이전트의 기획 판단이며 성공 확률이나 사용자 조사 결과가 아니다.']
for c in data['concepts']:
    md += ['', f'## {c["id"]}. {c["title"]}', '', c['hook'], '', f'**핵심 재미:** {c["category"]}  ', f'**판단 질문:** {c["question"]}  ', f'**제작 부담:** {c["complexity"]}  ', f'**기획 확신도:** {c["confidence"]}% (주관적·미검증)  ', '**상태:** Unexplored / 미적용', '', '**Basis — direct/reasoned:** ' + c['basis'], '', '**플레이 루프:** ' + ' → '.join(c['loop']), '', '**조작:** ' + c['player'], '', '**현재와의 차이:** ' + c['changes'], '', '### 300초 진행']
    md += ['', f'![{c["title"]} 전체 흐름도](../../outputs/highway-concepts-2026-09-26/site/flow/plan-{c["id"]}.svg)', '']
    for span, title, body in c['timeline']:
        md += ['', f'**{span} · {title}**', '', body]
    md += ['', '### 기믹 상세']
    for g in c['gimmicks']:
        md += ['', '#### ' + g['name']]
        for key, label in [('telegraph','예고'),('action','대응'),('payoff','성공'),('failure','실패'),('variation','변형'),('asset','재료'),('newWork','신규 동작')]:
            md += ['', f'- **{label}:** {g[key]}']
    md += ['', '### 분기']
    for name, left, ld, right, rd in c['branches']:
        md += ['', f'- **{name}:** {left} — {ld} / {right} — {rd}']
    md += ['', '### 마지막 장면', '', c['finale'], '', '### 기존 모델 재사용']
    for status, name, detail in c['assets']:
        md += ['', f'- **{status} · {name}:** {detail}']
    md += ['', '### 신규 제작 후보']
    for name, detail in c['newAssets']:
        md += ['', f'- **{name}:** {detail}']
    for title, values in [('필요한 시스템',c['systems']),('주의할 점',c['risks']),('선택 후 확인할 기준',c['acceptance'])]:
        md += ['', '### ' + title, ''] + ['- ' + value for value in values]
    md += ['', '**작은 시제품 제안:** ' + c['prototype']]
md += ['', '## 조합과 우선순위', '', data['recommendation']['why'], '', data['recommendation']['alternative'], '', data['recommendation']['combination'], '', '## 제외한 방향', '', '| 후보 | 제외 이유 |', '|---|---|']
for idea, reason in data['rejected']:
    md += [f'| {idea} | {reason} |']
md += ['', '## 참고', '']
for s in data['sources']:
    md += [f'- [{s["title"]}]({s["url"]}): {s["note"]}']
text = '\n'.join(md) + '\n'
doc = BASE / 'docs/ideation/2026-09-26-highway-five-concepts-detailed.md'
doc.parent.mkdir(exist_ok=True)
doc.write_text(text, encoding='utf-8')
(SITE / '고속도로-개편-5안-상세.md').write_text(text.replace('../../outputs/highway-concepts-2026-09-26/site/', ''), encoding='utf-8')
subprocess.run([sys.executable, str(BASE / 'tools/build-highway-simple.py')], check=True)
print(json.dumps({'concepts':5,'gimmicks':20,'timelines':'all 300 seconds','prefabCodesVerified':True,'site':str(SITE),'document':str(doc)}, ensure_ascii=False))
