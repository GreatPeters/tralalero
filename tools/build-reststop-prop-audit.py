"""Publish the evidence-backed prop review beside the existing 69-image gallery."""
import csv
import html
import json
from pathlib import Path
import shutil

ROOT = Path.cwd()
DOC = ROOT / 'map-concepts/reststop-prop-audit-2026-09-24'
GALLERY = ROOT / 'tmp/image-previews/reststop-trellis-images-2026-09-23'
OUT = GALLERY / 'audit'
OUT.mkdir(parents=True, exist_ok=True)
findings = json.loads((DOC / 'findings.json').read_text(encoding='utf8'))
assets = json.loads((GALLERY / 'atomic/manifest.json').read_text(encoding='utf8'))
esc = html.escape
for zone in range(1, 11):
    idx = f'{zone:02}'
    source = ROOT / f'outputs/reststop-blender-v4-2026-09-23/images/{idx}-reststop.png'
    if idx == '08':
        source = source.with_name('08-alt.png')
    target = OUT / f'zone-{idx}.png'
    if not target.exists():
        shutil.copy2(source, target)
source = OUT / '06-people-tools.png'
if not (OUT / '06-food-signs.png').exists() and source.exists():
    shutil.copy2(source, OUT / '06-food-signs.png')
with (DOC / 'missing-props.csv').open('w', encoding='utf-8-sig', newline='') as stream:
    writer = csv.writer(stream)
    writer.writerow(['ID', '소품', '구간', '판정', '조치', '근거'])
    for group in ['environment', 'equipment', 'optional_enrichment']:
        for row in findings[group]:
            writer.writerow([row['id'], row['title'], '/'.join(row['zones']), row['status'], row.get('action', row.get('reason')), row.get('evidence', 'Blender 원본 누락이 아닌 제안')])
with (DOC / 'coverage-by-zone.csv').open('w', encoding='utf-8-sig', newline='') as stream:
    writer = csv.writer(stream)
    writer.writerow(['구간', '기존 PNG 목록 기준 ID', '환경 추가·보완', '장비 추가 검토', '비고'])
    for zone in range(1, 11):
        idx = f'{zone:02}'
        writer.writerow([idx, ' '.join(r['id'] for r in assets if idx in r['zones']), ' '.join(r['id'] for r in findings['environment'] if idx in r['zones']), ' '.join(r['id'] for r in findings['equipment'] if idx in r['zones']), '반복 배치는 원본을 공유; PNG의 구간 표시는 재사용 가능 범위 전체가 아님'])
for name in ['missing-props.csv', 'coverage-by-zone.csv']:
    shutil.copy2(DOC / name, OUT / name)
cards = []
for row in findings['environment']:
    cards.append(f'<article><a href="{esc(row["image"])}" target="_blank"><img src="{esc(row["image"])}" alt="{esc(row["title"])} Blender 검토 렌더" loading="lazy"></a><div class="meta">{row["id"]} · {row["priority"]} · 구간 {" / ".join(row["zones"])}</div><h3>{esc(row["title"])}</h3><p>{esc(row["reason"])}</p><p class="action">{esc(row["action"])}</p><details><summary>Blender 근거</summary><p>{esc(row["evidence"])}</p></details></article>')
equipment = ''.join(f'<tr><td>{r["id"]}</td><td>{esc(r["title"])}</td><td>{esc(r["action"])}</td></tr>' for r in findings['equipment'])
reuse = ''.join(f'<li><b>{esc(r["png"])}</b> — {esc(r["note"])}</li>' for r in findings['already_covered'])
optional = ''.join(f'<li><b>{esc(r["title"])}</b> — {esc(r["reason"])}</li>' for r in findings['optional_enrichment'])
views = ''.join(f'<a href="zone-{n:02}.png" target="_blank"><img src="zone-{n:02}.png" alt="Blender {n:02}구간" loading="lazy"><span>{n:02}구간</span></a>' for n in range(1, 11))
page = '''<!doctype html><html lang="ko"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>휴게소 · Blender 소품 누락 검토</title><style>
*{box-sizing:border-box}body{margin:0;background:#eeeee6;color:#24382c;font:16px/1.75 system-ui,"Malgun Gothic",sans-serif}main{max-width:1400px;margin:auto;padding:35px 26px}a{color:#315e42;text-underline-offset:4px}h1{font-size:clamp(30px,4vw,45px);line-height:1.3}h2{margin-top:48px;font-size:25px;border-top:1px solid #bdc9b4;padding-top:25px}h3{margin:3px 0}.lead{font-size:21px;max-width:1050px}.links{display:flex;flex-wrap:wrap;gap:24px}.notice{background:#e0e5d9;padding:16px 20px;margin:25px 0}.grid{display:grid;grid-template-columns:repeat(3,minmax(0,1fr));gap:28px 22px}article img{width:100%;aspect-ratio:3/2;object-fit:contain;background:white}.meta{font-size:12px;color:#82682f;margin-top:10px}article p{font-size:14px}.action{border-left:3px solid #9c8446;padding-left:12px}details{font-size:12px;color:#5c6b53}table{width:100%;border-collapse:collapse}td,th{text-align:left;padding:12px;border-bottom:1px solid #bdc9b4}td:first-child{width:65px}td:nth-child(2){width:180px}.views{display:grid;grid-template-columns:repeat(5,minmax(0,1fr));gap:14px}.views img{width:100%}li{margin:8px 0}footer{margin:45px 0;font-size:13px;color:#576950}@media(max-width:950px){.grid{grid-template-columns:repeat(2,minmax(0,1fr))}.views{grid-template-columns:repeat(2,minmax(0,1fr))}}@media(max-width:600px){.grid{grid-template-columns:1fr}main{padding:22px 15px}td,th{font-size:13px;padding:8px}}
</style><main><a href="../">← 개별 PNG 69종 갤러리</a><h1>Blender와 대조하니,<br>보강할 소품이 있습니다.</h1><p class="lead">환경 소품 6종 누락 · 기믹용 진열대 1종 형태 보완 · 인물 장비 6종 별도 검토</p><p>최신 v4 원본의 3,811개 오브젝트와 10개 구간, 현재 69종 PNG를 대조했습니다. 아래 이미지는 실제 Blender 원본을 촬영한 검토 자료입니다.</p><div class="links"><a href="missing-props.csv" download>보강 목록 CSV ↓</a><a href="coverage-by-zone.csv" download>10구간 대응표 CSV ↓</a><a href="../reststop-atomic-assets-69png.zip" download>기존 PNG 69종 ↓</a></div><div class="notice">이번에는 누락을 검사했습니다. 추가 단독 소품 이미지는 아직 만들지 않았습니다. 원본 Blender와 기존 69개 PNG는 보존했고, TRELLIS는 실행하지 않았습니다.</div><h2>먼저 보강할 환경 형태 7종</h2><div class="grid">'''+''.join(cards)+'''</div><h2>인물이 사용하는 장비도 분리 대상입니다</h2><p>환경·차량 69종에는 인물이 없습니다. Blender에는 8역할/35개 인물 루트가 있습니다. 주차 안내원·바리스타·요리사는 프로젝트에 기존 FBX가 있고, 컵·뒤집개·정지 표지봉은 기존 게임 설치 코드에도 연결되어 있습니다. 기존 배낭·가로등은 재사용 후보이며 형태 적합성은 별도 확인이 필요합니다.</p><table><thead><tr><th>ID</th><th>장비</th><th>판단</th></tr></thead><tbody>'''+equipment+'''</tbody></table><h2>이미 준비돼 있거나 재사용 가능한 것</h2><ul>'''+reuse+'''</ul><p>F04 나무의 현재 구간 표시는 03뿐이지만 주차장·홀 조경에도 쓸 수 있습니다. 지붕·처마·천장·조명도 필요한 구간으로 재사용 범위를 넓힐 수 있습니다. 이는 새 이미지 누락과 구분해야 합니다.</p><h2>휴게소다운 생활감을 위한 추가 제안</h2><p>아래는 Blender에 있던 물체를 빠뜨린 사례가 아닙니다. 새로 보강하면 좋은 제안입니다.</p><ul>'''+optional+'''</ul><h2>소품 이미지 외에 남는 작업</h2><ul>'''+''.join('<li>'+esc(x)+'</li>' for x in findings['non_image_work'])+'''</ul><h2>확인한 10개 구간</h2><div class="views">'''+views+'''</div><footer>2026-09-24 · Blender 5.2.1 · 원본 SHA-256 전후 일치 · 네이티브 목록/소스/렌더 대조<br>3D 변환 성공, 메시 품질, Unity 충돌·성능을 이번 검사로 검증한 것은 아닙니다.</footer></main></html>'''
(OUT / 'index.html').write_text(page, encoding='utf8')
print(json.dumps({'environment_missing': 6, 'environment_partial': 1, 'equipment_png_gaps': 6, 'optional_proposals': 6, 'audit_url': 'http://127.0.0.1:8771/audit/'}, ensure_ascii=True))
