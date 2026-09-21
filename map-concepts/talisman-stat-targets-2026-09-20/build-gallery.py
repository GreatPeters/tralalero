from pathlib import Path
import json
import re
import shutil

record = Path(__file__).parent
root = record.parents[1]
data = json.loads((record / 'generated-files.json').read_text(encoding='utf-8'))
preview = root / 'tmp/image-previews/talisman-stat-targets-2026-09-20'
gallery = root / 'tmp/image-previews/bonus-gallery-2026-09-19/talisman-targets'
name = data['file']
template = (root / 'map-concepts/bonus-gallery-2026-09-19/template.html').read_text(encoding='utf-8')
template = template.replace('보너스 시안 모아보기', '접힌 부적 · 능력에 맞는 흡수')
template = template.replace('이미지를 눌러 크게 보세요. 원본 크기 보기와 PNG 저장도 가능합니다.', '공격력은 입 앞 발사부로, 체력은 몸으로. 신발 강화로 잘못 읽히던 이전 시안을 수정했습니다.')
template = re.sub(r'<nav .*?</nav>', '<nav aria-label="시안 선택"><button data-group="all" aria-pressed="true">공격력 · 체력 비교</button></nav>', template, flags=re.S)
figure = f'<figure data-group="targets"><button class="preview" aria-label="공격력과 체력 흡수 흐름 확대"><img src="{name}" alt="공격력과 체력 흡수 흐름" width="1536" height="1024"></button><figcaption><strong>공격력 → 입 앞 발사부 / 체력 → 몸</strong><a href="{name}" target="_blank" rel="noopener">원본 PNG 열기 ↗</a></figcaption></figure>'
template = template.replace('__FIGURES__', figure).replace("filter('context');", "filter('all');")
template = template.replace('grid-template-columns:repeat(2,minmax(0,1fr))', 'grid-template-columns:1fr')
for folder in (record / 'images', preview, gallery):
    folder.mkdir(parents=True, exist_ok=True)
    if not (folder / name).exists():
        shutil.copy2(data['source'], folder / name)
for folder in (preview, gallery):
    (folder / 'index.html').write_text(template, encoding='utf-8')
print(gallery / 'index.html')
