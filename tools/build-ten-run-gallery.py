"""Curate genuine Play Mode evidence; originals remain available beside the report."""
from pathlib import Path
import json,shutil,html

base=Path('tmp/image-previews/ten-run-review-2026-09-22')
gallery=Path('tmp/image-previews/bonus-gallery-2026-09-19/ten-run-review')
gallery.mkdir(parents=True,exist_ok=True)
manifest=Path('map-concepts/ten-run-review-2026-09-22/findings.json')
findings=json.loads(manifest.read_text(encoding='utf-8'))
summaries=[]
for path in sorted(base.glob('run-*/summary.json')):
    r=json.loads(path.read_text(encoding='utf-8'));r['run']=path.parent.name;summaries.append(r)
cards=[]
for i,f in enumerate(findings,1):
    images=[]
    for j,entry in enumerate(f['images']):
        source=base/entry['path'];target=gallery/f'issue-{i:02}-{j+1}-{source.parent.name}-{source.stem}.png'
        if not source.exists():raise FileNotFoundError(source)
        if not target.exists():shutil.copy2(source,target)
        images.append(f'<figure><a href="{target.name}" target="_blank"><img loading="lazy" src="{target.name}" alt="{html.escape(entry["caption"])}"></a><figcaption>{html.escape(entry["caption"])}</figcaption></figure>')
    cards.append(f'''<article id="issue-{i}" data-category="{f['category']}"><div class="copy"><div class="kicker">{i:02} · {html.escape(f['priority'])} · {html.escape(f['category'])}</div><h2>{html.escape(f['title'])}</h2><p class="evidence">{html.escape(f['evidence'])}</p><p>{html.escape(f['impact'])}</p><div class="suggestion"><b>개선 방향</b><p>{html.escape(f['suggestion'])}</p></div></div><div class="pictures">{''.join(images)}</div></article>''')
names={'left':'왼쪽 선택','attack':'공격 강화','health':'체력 강화·회피','helper':'지원군 우선','late':'늦은 회피','cautious':'% 강화·이른 회피','main':'고속도로 본선','bypass':'회복 우회로'}
chapters={'Noryangjin_MapTool_Mode_SR18':'노량진','HighWay':'고속도로','RestStop':'휴게소'}
outcomes={'death':'사망','clear':'클리어','time-limit':'관찰 시간 종료'}
rows=''.join(f'<tr><td>{r["run"][-2:]}</td><td>{chapters.get(r["scene"],r["scene"])}</td><td>{names.get(r["policy"],r["policy"])}</td><td>{r["seconds"]:.1f}초</td><td>{r["bonuses"]}</td><td>{outcomes.get(r["outcome"],r["outcome"])}</td></tr>' for r in summaries)
page='''<!doctype html><html lang="ko"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>10판 플레이 리뷰 · 실제 문제 장면</title><style>
*{box-sizing:border-box}body{margin:0;background:#f3f1ec;color:#192c36;font:16px/1.7 system-ui,sans-serif}header,main,footer{max-width:1380px;margin:auto;padding:36px}header{padding-top:55px}h1{font-size:40px;line-height:1.2;letter-spacing:-1px;max-width:900px}header p{max-width:920px;color:#52616b}.eyebrow{font-size:14px;font-weight:750;color:#1f746b}.filters{display:flex;gap:8px;flex-wrap:wrap;padding:8px 0}button{font:inherit;border:1px solid #b8c7cb;background:#fff;padding:8px 19px;border-radius:30px;cursor:pointer}button.active{background:#183d47;color:white;border-color:#183d47}article{display:grid;grid-template-columns:minmax(280px,.85fr) minmax(400px,1.4fr);gap:36px;padding:40px 0;border-top:1px solid #c9d1cf}h2{font-size:29px;line-height:1.3;margin:14px 0 22px}.kicker{font-size:13px;font-weight:750;letter-spacing:.5px;color:#9b3c28}.evidence{font-weight:650}.suggestion{background:#e6eddf;padding:20px;margin-top:25px;border-left:4px solid #5d825f}.suggestion p{margin:8px 0 0}.pictures{display:flex;gap:14px;align-items:flex-start;justify-content:center}figure{margin:0;flex:1;max-width:390px}img{width:100%;height:auto;border:1px solid #c1c7c5;border-radius:5px}figcaption{font-size:13px;color:#52616b;padding-top:8px}table{width:100%;border-collapse:collapse;background:#fff;font-size:14px}th,td{text-align:left;border-bottom:1px solid #d5dcd9;padding:9px 13px}.table-wrap{overflow:auto}details{margin:30px 0}summary{cursor:pointer;font-weight:700;padding:12px 0}footer{font-size:13px;color:#64746f}a{color:#11625a}@media(max-width:850px){header,main,footer{padding:22px}article{grid-template-columns:1fr;gap:20px}h1{font-size:30px}h2{font-size:25px}}
</style><header><div class="eyebrow">실제 Unity 플레이 화면 · 2026.09.22</div><h1>게임을 하면서 걸렸던 장면들</h1><p>사진은 실제 실행 화면입니다. 확실한 수치 오류와 시각·재미에 대한 판단을 구분했습니다. 캐릭터 체력·공격력·충돌은 그대로 두고 좌우 이동과 보너스 선택만 바꾸어 플레이했습니다.</p><div class="filters"><button class="active" data-filter="all">전체</button><button data-filter="비주얼">비주얼</button><button data-filter="밸런스">밸런스</button><button data-filter="재미·전달">재미·전달</button></div></header><main>'''+''.join(cards)+f'''
<details open><summary>플레이 기록 · 완료 {len(summaries)}회</summary><p>정상 시작 수치 HP500 / 공격력68. 1배속 자동 좌우 조작으로 실행했으며, 자동조작의 생존 시간은 사람의 난이도·재미 점수가 아닙니다. 1–2판은 조준·보너스 선택 중심, 3판부터 근접 적과 투사체 회피도 추가했습니다. 고속도로·휴게소는 해당 챕터를 직접 열어 시험했습니다.</p><div class="table-wrap"><table><tr><th>판</th><th>맵</th><th>조작 방식</th><th>관찰 시간</th><th>보너스</th><th>종료</th></tr>{rows}</table></div></details>
</main><footer>이미지를 클릭하면 원본 PNG가 열립니다. 이 검토에서는 게임 규칙이나 아트를 수정하지 않았습니다. 사진 촬영 시 부하가 생길 수 있으므로 FPS 평가는 포함하지 않았습니다.</footer><script>document.querySelectorAll('button[data-filter]').forEach(b=>b.addEventListener('click',()=>{{document.querySelectorAll('button').forEach(x=>x.classList.toggle('active',x===b));document.querySelectorAll('article').forEach(x=>x.style.display=b.dataset.filter==='all'||x.dataset.category===b.dataset.filter?'grid':'none')}}))</script></html>'''
(gallery/'index.html').write_text(page,encoding='utf-8')
(gallery/'runs.json').write_text(json.dumps(summaries,ensure_ascii=False,indent=2),encoding='utf-8')
print(f'{gallery}/index.html; {len(findings)} findings; {len(summaries)} completed runs')
