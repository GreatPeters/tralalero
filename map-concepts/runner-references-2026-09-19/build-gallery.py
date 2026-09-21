from pathlib import Path
import json, html, shutil, re
root=Path(__file__).resolve().parents[2]
meta=json.loads(Path(__file__).with_name("sources.json").read_text(encoding="utf-8"))
more_path=Path(__file__).with_name("sources-more.json")
if more_path.exists():
    meta=json.loads(more_path.read_text(encoding="utf-8"))+meta
selected=[x for x in meta if int(Path(x["file"]).stem.rsplit("-",1)[1]) <= (3 if x["slug"]=="arrow-a-row" else 2)]
out=root/"tmp/image-previews/bonus-gallery-2026-09-19/references"
out.mkdir(parents=True,exist_ok=True)
notes={
"arrow-a-row":["두 개의 배너로 런 중 능력 선택","획득 수치와 달라진 공격 모습","장비·동료는 별도 보상 선택 화면"],
"weapon-craft-run":["총기와 탄창 소재로 강화 대상을 통일","변경된 총기와 발사 형태를 화면에 표시"],
"gun-head-run":["무기·숫자·강화 게이트의 단순한 실루엣","연사·피해량을 색과 아이콘으로 구분"],
"mob-control":["배율을 통과한 결과를 군중 크기로 보여줌","보너스 수치와 증가 결과를 가까이 배치"],
"gun-clone":["카드 안에 아이콘·수치·등급을 담는 방식","총 개수·탄 크기·연사 효과의 시각화"],
"reload-rush":["실제 탄환을 모아 탄창 수량을 채우는 표현","장전 구역과 탄창·무기의 연결"],
"weapon-master-gun-shooter-run":["사격 대상과 통과 지점의 색 구분","증가·감소 수치를 큰 숫자로 비교"],
"merge-grabber":["동료가 합류해 화력이 늘어나는 구성","캐릭터 수치와 능력 선택을 함께 표시"],
"last-war-survival":["좌우 목표를 컨테이너와 장비로 보여주는 홍보 화면","획득 결과를 부대 증가로 표현한 홍보 화면"],
"into-the-dead-2-zombie-killer":["현장 배경·무기·동료가 같은 세계관을 공유","장비 수집과 무기 외형의 연결 · 보조 참고"],
}
figures=[]
for item in selected:
    filename=item["file"]; dest=out/filename
    if not dest.exists():shutil.copy2(root/"tmp/image-previews/runner-references-2026-09-19"/filename,dest)
    idx=int(Path(filename).stem.rsplit("-",1)[1])-1
    caption=notes[item["slug"]][idx]
    title=html.escape(item["game"]+" · "+caption)
    figures.append(f'<figure data-group="{item["slug"]}"><button class="preview" aria-label="{title} 확대"><img src="{filename}" alt="{title}" loading="lazy"></button><figcaption><div><strong>{html.escape(item["game"])}</strong><small>{html.escape(caption)}</small></div><a href="{item["source"]}" target="_blank" rel="noopener">공식 출처 ↗</a></figcaption><p class="source-note">공식 스토어 공개 이미지 · 클릭하면 확대</p></figure>')
template=(root/"map-concepts/bonus-gallery-2026-09-19/template.html").read_text(encoding="utf-8")
template=template.replace("<title>Bonus Wall · 시안 갤러리</title>","<title>러닝 슈터 · 실제 게임 레퍼런스</title>")
template=template.replace("TRALALERO SHOOTER / BONUS WALL","RUNNER SHOOTER / REFERENCE STUDY")
template=template.replace("보너스 시안 모아보기","러닝 슈터 레퍼런스")
template=template.replace("이미지를 눌러 크게 보세요. 원본 크기 보기와 PNG 저장도 가능합니다.","새 레퍼런스 6개를 추가했습니다. 카드·탄약 수집·동료 합류·세계관 연결을 비교해 보세요. 아래 코멘트는 디자인 관점의 해석입니다.")
games=list(dict.fromkeys((x["slug"],x["game"]) for x in selected))
nav='<nav aria-label="레퍼런스 게임">'+''.join(f'<button data-group="{key}" aria-pressed="false">{html.escape(label)}</button>' for key,label in [("all",f"전체 {len(selected)}장")]+games)+'</nav>'
template=re.sub(r'<nav .*?</nav>',nav,template,flags=re.S)
template=template.replace("__FIGURES__","\n".join(figures))
template=template.replace("active.length+'장의 시안 · 1536 × 1024 원본'","active.length+'장의 공식 스토어 자료 · 확대 화면에서 ← → 로 이동'")
template=template.replace("filter('context');","filter('all');")
template=template.replace("PNG 저장", "이미지 저장")
template=template.replace("모두 기획 시안입니다. 실제 Unity 적용 화면과는 구분해 주세요.","Steam 및 App Store의 공식 공개 자료입니다. 모바일 자료에는 홍보 문구가 포함되며, 최신 버전을 직접 플레이해 검증한 자료는 아닙니다. 이미지 권리는 각 게임 권리자에게 있습니다.")
template=template.replace("</style>",".preview img{height:420px;aspect-ratio:auto;object-fit:contain;background:#e4e7e5}.source-note{font-size:12px;margin-top:7px}#full{max-height:calc(100vh - 90px)}@media(max-width:800px){.preview img{height:auto;max-height:600px}}\n</style>")
(out/"index.html").write_text(template,encoding="utf-8")
(root/"tmp/image-previews/runner-references-2026-09-19/index.html").write_text(template,encoding="utf-8")
print(out/"index.html")
print(len(selected),"images")
