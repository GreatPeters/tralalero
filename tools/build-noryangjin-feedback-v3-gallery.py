"""Publish local review pages from native evidence; never synthesize gameplay imagery."""
from pathlib import Path
import html
import json
import shutil

ROOT = Path("outputs/noryangjin-feedback-v3-2026-09-28")
DEST = Path("outputs/noryangjin-review-homepage-2026-09-28/site/feedback-v3")
DEST.mkdir(parents=True, exist_ok=True)
native = Path((ROOT / "latest-capture.txt").read_text(encoding="utf-8-sig").strip())
probe = json.loads((ROOT / "native-probes.json").read_text(encoding="utf-8-sig"))
summary_path = ROOT / "release-summary.json"
summary = json.loads(summary_path.read_text(encoding="utf-8-sig")) if summary_path.exists() else {"status": "최종 검증 진행 중"}
shots = [
    ("auction", "냉동창고 속 경매", "새 냉각기·단열 벽·참치 진열대. 손을 들고 호가를 부르는 경매인."),
    ("cold-front", "새 창고 입구", "Meshy 게이트와 연속 하역 바닥으로 이어지는 진입부."),
    ("water-on", "물방울과 젖은 바닥", "바닥 파문·물방울과 실제 미끄러짐. 주황색 예고 제거."),
    ("water-remains", "분사 뒤에 남는 물", "물줄기가 멈춰도 젖은 흔적이 남고 서서히 마릅니다."),
    ("shutter-boxes", "문 앞까지 이어지는 박스", "7개 박스 벽과 롤러 셔터. 마지막 박스는 문 앞 3.2m."),
    ("bridge-exit", "출구 시야", "천장 아래 카메라를 유지해 상어와 바닥이 보입니다."),
    ("bridge-turn", "끊김 없는 연결 다리", "469개 지점 바닥 판정 통과. 원래 도로 충돌 유지."),
    ("approach", "가리는 벽은 반투명", "바닥과 높은 장식을 나누고 시야를 가리는 부분을 흐리게 표시."),
    ("container-shadow", "검은 낙하 그림자", "주황 사각형 대신 바닥에 검은 그림자."),
    ("container-impact", "빠르게 내려오는 컨테이너", "1.05초 예고 뒤 0.22초 급강하와 착지 먼지."),
]
cards = []
for name, title, caption in shots:
    src = native / f"{name}.png"
    assert src.is_file(), src
    shutil.copy2(src, DEST / f"{name}.png")
    cards.append(f'<article><a class="shot" href="{name}.png" target="_blank"><img loading="lazy" src="{name}.png" alt="{title}"></a><h3>{title}</h3><p>{caption}</p><a class="original" href="{name}.png" target="_blank">원본 화면 열기 ↗</a></article>')
for name in ["closed-shutter", "death-coins", *[f"auction-{i}" for i in range(5)]]:
    src = Path(probe["folder"]) / f"{name}.png"
    assert src.is_file(), src
    shutil.copy2(src, DEST / f"{name}.png")
for name in ["TEST-REPORT.md", "regression-failures.md", "native-probes.json"]:
    shutil.copy2(ROOT / name, DEST / name)
claude = ROOT / "CLAUDE-REVIEW.md"
if claude.exists():
    shutil.copy2(claude, DEST / claude.name)
followup = ROOT / "CLAUDE-FOLLOWUP.md"
if followup.exists():
    shutil.copy2(followup, DEST / followup.name)
page = '''<!doctype html><html lang="ko"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>노량진 · 스크린샷 피드백 적용</title>
<style>
*{box-sizing:border-box}body{margin:0;background:#f1eee5;color:#142d3c;font-family:system-ui,'Malgun Gothic',sans-serif}a{color:inherit}header,main,footer{max-width:1160px;margin:auto;padding:28px}nav{display:flex;justify-content:space-between;border-bottom:1px solid #b8c6c8;padding-bottom:20px;font-weight:700}nav a{text-decoration:none}header{padding-top:24px}small{letter-spacing:.15em;color:#436778;font-size:12px}h1{font-size:clamp(34px,5vw,62px);letter-spacing:-.06em;line-height:1.15;margin:36px 0 18px}header p{font-size:18px;max-width:690px;line-height:1.8}section{margin-bottom:52px}h2{font-size:28px;letter-spacing:-.04em;margin:0 0 14px}.note{color:#526975;line-height:1.75}.receipts{display:flex;flex-wrap:wrap;gap:10px;margin-top:24px}.receipts a,.tag{padding:10px 15px;border:1px solid #9aacb0;border-radius:3px;text-decoration:none;font-weight:650;background:#faf9f4}.status{padding:20px 24px;background:#113a51;color:#faf9ef;border-left:5px solid #e6af4a;margin:22px 0}.grid{display:grid;grid-template-columns:repeat(3,1fr);gap:38px 24px}.shot{display:block;background:#c1c9ca;border:6px solid #13384d;border-radius:12px;overflow:hidden}.shot img{width:100%;display:block;height:auto;aspect-ratio:1080/2340;object-fit:contain}article h3{font-size:20px;margin:17px 0 8px}article p{line-height:1.7;font-size:14px;color:#526975;margin:0 0 10px}.original{font-size:13px;text-underline-offset:4px}.motion{display:grid;grid-template-columns:320px 1fr;gap:35px;background:#fffdf6;padding:25px;border:1px solid #c1cdce}.motion img{width:100%;border-radius:8px}.motion p{line-height:1.8}.steps{display:flex;gap:8px;flex-wrap:wrap}button{border:1px solid #76929e;border-radius:3px;padding:12px;background:transparent;color:#173d51;font:inherit;cursor:pointer}button[aria-pressed=true]{background:#163e53;color:white}footer{border-top:1px solid #b8c6c8;line-height:1.7;color:#526975;font-size:13px}a:focus-visible,button:focus-visible{outline:3px solid #d27b18;outline-offset:4px}@media(max-width:800px){.grid{grid-template-columns:repeat(2,1fr)}.motion{grid-template-columns:240px 1fr}}@media(max-width:520px){header,main,footer{padding:20px}.grid{grid-template-columns:1fr 1fr;gap:25px 12px}.shot{border-width:3px}.motion{grid-template-columns:1fr}.motion img{max-width:280px;margin:auto}article h3{font-size:17px}article p{font-size:13px}}
</style><header><nav><a href="../">TRALALERO SHOOTER / 노량진</a><span>2026.09.28 · V3</span></nav><h1>시장을 고치고,<br>직접 달려봤습니다.</h1><p>번호 수집물과 무인 횡단 기믹을 걷어내고, 물바닥·셔터·다리·냉동창고 경매를 다시 적용한 실제 Unity 화면입니다.</p><div class="status">STATUS</div><div class="receipts"><a href="TEST-REPORT.md" target="_blank">테스트 보고서 ↗</a><a href="regression-failures.md" target="_blank">전체 회귀의 미통과 항목 ↗</a>CLAUDELINK<span class="tag">9999 · 빠른 좌우 이동 ON</span></div></header>
<main><section><small>01 / NATIVE GAME VIEW</small><h2>실제 적용 화면</h2><p class="note">같은 기능을 보기 쉽게 고정 위치에서 촬영했습니다. 아래 화면만으로 전체 완주를 주장하지 않으며, 경로 플레이 결과는 보고서에 따로 기록합니다. 이미지를 누르면 원본을 엽니다.</p><div class="grid">CARDS</div></section>
<section><small>02 / LIVE AUCTION</small><h2>서로 호가를 주고받는 경매</h2><div class="motion"><a id="motionLink" href="auction-0.png" target="_blank"><img id="motion" src="auction-0.png" alt="경매 동작의 첫 번째 실제 프레임"></a><div><h3>서 있는 사격벽에서 경매 무리로</h3><p>경매대 뒤 호가 담당과 입찰 손님이 번갈아 움직입니다. 1.7초 간격으로 촬영한 실제 프레임을 순서대로 확인할 수 있습니다.</p><div class="steps">BUTTONS</div><p class="note">비전투 인물 10명 · 인물 충돌체 0<br>중앙 통로를 지나 경매 보상을 받습니다.</p><p><a href="death-coins.png" target="_blank">상인 사망 코인 확인 ↗</a></p><p><a href="closed-shutter.png" target="_blank">닫힌 셔터 확인 ↗</a></p></div></div></section>
<section><small>03 / VALIDATION</small><h2>확인한 것과 남은 범위</h2><p class="note">기능 검사 9개와 새 기능·카메라 검사 19개 통과. 넓은 노량진 회귀는 360/386 통과이며 미통과 26건도 공개합니다. 9999는 무적이 아니므로 테스트 중 피해와 보너스는 수치에 반영됩니다. 자연 플레이 난이도·휴대전화 FPS·배포 빌드는 이번 검증 범위에 포함하지 않았습니다.</p></section></main>
<footer>이 PC의 D 프로젝트를 보여주는 로컬 검수 페이지입니다. 원본 SR18 · Data.xlsx · Build Settings 보존 여부와 저장값 복원 기록은 보고서에 포함합니다. 생성 콘셉트 그림을 게임 화면으로 사용하지 않았습니다.</footer>
<script>document.querySelectorAll('.steps button').forEach(b=>b.addEventListener('click',()=>{document.querySelectorAll('.steps button').forEach(x=>x.setAttribute('aria-pressed','false'));b.setAttribute('aria-pressed','true');const src='auction-'+b.dataset.frame+'.png';document.getElementById('motion').src=src;document.getElementById('motion').alt='경매 동작 실제 프레임 '+(Number(b.dataset.frame)+1);document.getElementById('motionLink').href=src;}));</script></html>'''
page = page.replace("STATUS", html.escape(summary["status"]))
page = page.replace("CARDS", "\n".join(cards))
page = page.replace("BUTTONS", "".join(f'<button data-frame="{i}" aria-pressed="{str(i == 0).lower()}">{i+1}</button>' for i in range(5)))
claude_links = '<a href="CLAUDE-REVIEW.md" target="_blank">Claude Code 검증 ↗</a>' if claude.exists() else ""
if followup.exists():
    claude_links += '<a href="CLAUDE-FOLLOWUP.md" target="_blank">지적 반영 재검증 ↗</a>'
page = page.replace("CLAUDELINK", claude_links)
(DEST / "index.html").write_text(page, encoding="utf-8")
(DEST / "evidence.json").write_text(json.dumps({"native": str(native), "probe": probe["folder"], "summary": summary}, ensure_ascii=False, indent=2), encoding="utf-8")
print(DEST / "index.html")
