from pathlib import Path
import html
import json
import re
import shutil
import struct
import zlib

record = Path(__file__).parent
root = record.parents[1]
data = json.loads((record / 'generated-files.json').read_text(encoding='utf-8'))
preview = root / 'tmp/image-previews/talisman-pickup-flows-2026-09-20'
gallery = root / 'tmp/image-previews/bonus-gallery-2026-09-19/talisman-flows'
figures, checks = [], []
for c in data['concepts']:
    for folder in (record / 'images', preview, gallery):
        folder.mkdir(parents=True, exist_ok=True)
        target = folder / c['file']
        if not target.exists():
            shutil.copy2(c['source'], target)
    b = (preview / c['file']).read_bytes()
    assert b[:8] == b'\x89PNG\r\n\x1a\n'
    width, height = struct.unpack('>II', b[16:24])
    pos, chunks = 8, []
    while pos < len(b):
        size = struct.unpack('>I', b[pos:pos+4])[0]
        kind, payload = b[pos+4:pos+8], b[pos+8:pos+8+size]
        assert zlib.crc32(kind + payload) & 0xffffffff == struct.unpack('>I', b[pos+8+size:pos+12+size])[0]
        if kind == b'IDAT':
            chunks.append(payload)
        pos += size + 12
    zlib.decompress(b''.join(chunks))
    checks.append({'id': c['id'], 'dimensions': [width, height], 'png_validation': 'pass'})
    title = html.escape(c['id'] + ' · ' + c['name'])
    figures.append(f'<figure data-group="{c["id"]}"><button class="preview" aria-label="{title} 확대"><img src="{c["file"]}" alt="{title}" loading="lazy" width="{width}" height="{height}"></button><figcaption><div><strong>{title}</strong><small>{html.escape(c["lore"])}</small><small>제작: {html.escape(c["build"])}</small></div><a href="{c["file"]}" target="_blank" rel="noopener">원본 PNG ↗</a></figcaption></figure>')
template = (root / 'map-concepts/bonus-gallery-2026-09-19/template.html').read_text(encoding='utf-8')
template = template.replace('보너스 시안 모아보기', '접힌 부적 · 획득 흐름 4가지')
template = template.replace('이미지를 눌러 크게 보세요. 원본 크기 보기와 PNG 저장도 가능합니다.', '다가가기 → 닿는 순간 → 힘 흡수 → 계속 달리기. 같은 부적의 획득 연출을 4가지로 비교합니다. 시간은 구현 제안값입니다.')
nav = '<nav aria-label="시안 선택"><button data-group="all" aria-pressed="true">전체 4안</button>' + ''.join(f'<button data-group="{c["id"]}" aria-pressed="false">{html.escape(c["id"]+" "+c["name"])}</button>' for c in data['concepts']) + '</nav>'
template = re.sub(r'<nav .*?</nav>', nav, template, flags=re.S)
template = template.replace('__FIGURES__', '\n'.join(figures)).replace("filter('context');", "filter('all');")
template = template.replace('모두 기획 시안입니다. 실제 Unity 적용 화면과는 구분해 주세요.', '내장 ImageGen 시안입니다. 제작 방법은 검토했으며, 메시·효과 제작과 Unity 적용은 아직 진행하지 않았습니다. 수치는 비교 예시입니다.')
for folder in (preview, gallery):
    (folder / 'index.html').write_text(template, encoding='utf-8')
(record / 'verification.json').write_text(json.dumps(checks, indent=2), encoding='utf-8')
print(f'Validated {len(checks)} PNG files; gallery: {gallery}')
