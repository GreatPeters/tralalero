from pathlib import Path
import json
import re
import shutil

root = Path(__file__).resolve().parents[2]
record = Path(__file__).parent
data = json.loads((record / 'generated-files.json').read_text(encoding='utf-8'))
preview = root / 'tmp/image-previews/slim-ring-story-2026-09-19'
gallery = root / 'tmp/image-previews/bonus-gallery-2026-09-19/slim-ring-story'
name = data['file']
template = (root / 'map-concepts/bonus-gallery-2026-09-19/template.html').read_text(encoding='utf-8')
template = template.replace('보너스 시안 모아보기', '슬림 링 · 공물에서 신발로')
template = template.replace('이미지를 눌러 크게 보세요. 원본 크기 보기와 PNG 저장도 가능합니다.', '작은 공물 → 저주 신발의 반응 → 링의 힘 흡수. 선택한 1번 링을 게임 설정에 연결한 4컷 시안입니다.')
template = re.sub(r'<nav .*?</nav>', '<nav aria-label="시안 선택"><button data-group="all" aria-pressed="true">설정 연결 시안</button></nav>', template, flags=re.S)
figure = f'<figure data-group="story"><button class="preview" aria-label="공물에서 신발로 확대"><img src="{name}" alt="슬림 링 · 공물에서 신발로" width="1536" height="1024"></button><figcaption><strong>공물 발견 · 링 등장 · 흡수 · 신발 강화</strong><a href="{name}" target="_blank" rel="noopener">원본 PNG 열기 ↗</a></figcaption></figure>'
template = template.replace('__FIGURES__', figure).replace("filter('context');", "filter('all');")
template = template.replace('grid-template-columns:repeat(2,minmax(0,1fr))', 'grid-template-columns:1fr')
for folder in (record / 'images', preview, gallery):
    folder.mkdir(parents=True, exist_ok=True)
    if not (folder / name).exists():
        shutil.copy2(data['source'], folder / name)
for folder in (preview, gallery):
    (folder / 'index.html').write_text(template, encoding='utf-8')
print(gallery / 'index.html')
