from pathlib import Path
import json, shutil, html, re
root=Path(__file__).resolve().parents[2]
topic="bonus-diverse-pickups-2026-09-19"
data=json.loads(Path(__file__).with_name("generated-files.json").read_text(encoding="utf-8"))
preview=root/"tmp/image-previews"/topic
images=Path(__file__).with_name("images")
gallery=root/"tmp/image-previews/bonus-gallery-2026-09-19/diverse"
for folder in (preview,images,gallery):folder.mkdir(parents=True,exist_ok=True)
figures=[]
for c in data["concepts"]:
    for folder in (preview,images,gallery):
        dest=folder/c["file"]
        if not dest.exists():shutil.copy2(c["source"],dest)
    name=html.escape(c["id"]+" · "+c["name"]); src=c["file"]
    figures.append(f'<figure data-group="{c["id"]}"><button class="preview" aria-label="{name} 확대"><img src="{src}" alt="{name}" loading="lazy" width="1536" height="1024"></button><figcaption><div><strong>{name}</strong><small>{html.escape(c["build"])}</small></div><a href="{src}" target="_blank" rel="noopener">원본 PNG 열기 ↗</a></figcaption></figure>')
template=(root/"map-concepts/bonus-gallery-2026-09-19/template.html").read_text(encoding="utf-8")
template=template.replace("보너스 시안 모아보기","입체 보너스 · 새로운 10가지")
template=template.replace("이미지를 눌러 크게 보세요. 원본 크기 보기와 PNG 저장도 가능합니다.","좋다고 하신 재질과 입체감을 유지한 10가지 형태입니다. 이미지를 눌러 게임 안 모습·형태·획득 연출을 크게 비교하세요.")
nav='<nav aria-label="시안 선택"><button data-group="all" aria-pressed="true">전체 10안</button>'+''.join(f'<button data-group="{c["id"]}" aria-pressed="false">{html.escape(c["id"]+" "+c["name"])}</button>' for c in data["concepts"])+'</nav>'
template=re.sub(r'<nav .*?</nav>',nav,template,flags=re.S)
template=template.replace("__FIGURES__","\n".join(figures)).replace("filter('context');","filter('all');")
template=template.replace("모두 기획 시안입니다. 실제 Unity 적용 화면과는 구분해 주세요.","직접 제작할 메시와 기존 아이콘·UI 소재를 조합하는 ImageGen 시안입니다. Unity 적용본이 아니며 표시 수치는 비교 예시입니다.")
for folder in (preview,gallery):(folder/"index.html").write_text(template,encoding="utf-8")
print("Saved 10 selected images; gallery:",gallery/"index.html")

