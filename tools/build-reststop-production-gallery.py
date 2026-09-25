"""Build a small local gallery from actual input, generation and rig artifacts."""
from pathlib import Path
import html
import hashlib
import json
import shutil
import time

ROOT = Path.cwd()
DOC = ROOT / 'map-concepts/reststop-production-2026-09-24'
OUT = ROOT / 'outputs/reststop-production-2026-09-24'
G = ROOT / 'tmp/image-previews/reststop-production-2026-09-24'
G.mkdir(parents=True, exist_ok=True)
(G / 'inputs').mkdir(exist_ok=True)
roles = {'H01': ('주차 안내원', 'ParkingMarshal'), 'H02': ('간식 요리사', 'SnackChef'),
         'H03': ('바리스타', 'CoffeeVendor'), 'H04': ('편의점 계산원', 'Cashier'),
         'H05': ('청소원', 'Cleaner'), 'H06': ('주유소 직원', 'FuelAttendant'),
         'H07': ('여행객', 'Traveler'), 'H08': ('경찰', 'Police')}
catalog = json.loads((OUT / 'source-manifest.json').read_text(encoding='utf8'))
items = [{**r, 'kind': 'prop'} for r in catalog]
for p in sorted((DOC / 'input-records').glob('*.json')):
    row = json.loads(p.read_text(encoding='utf8'))
    key = row['id']
    items.append({'id': key, 'title': roles[key][0] if key in roles else row['title'],
                  'kind': 'human' if key in roles else 'prop', 'role': roles[key][1] if key in roles else None})
items.sort(key=lambda r: r['id'])
assert len(items) == 90 and len({r['id'] for r in items}) == 90
state_path = OUT / 'assets/.trellis-automation/state.json'
state = json.loads(state_path.read_text(encoding='utf8')) if state_path.exists() else {'jobs': {}}
source_state = json.loads((OUT / 'status.json').read_text(encoding='utf8')) if (OUT / 'status.json').exists() else {}
jobs = {Path(j['name']).stem: j for j in state['jobs'].values()}
overrides_path = OUT / 'final-overrides.json'
if overrides_path.exists():
    jobs.update(json.loads(overrides_path.read_text(encoding='utf8')))
rig_manifest_path = OUT / 'accepted-rigs.json'
rigs = json.loads(rig_manifest_path.read_text(encoding='utf8')) if rig_manifest_path.exists() else {}
def copy_current(source, destination):
    if source.exists() and (not destination.exists() or source.stat().st_size != destination.stat().st_size
                            or source.stat().st_mtime_ns != destination.stat().st_mtime_ns):
        shutil.copy2(source, destination)
cards = []
result_versions = []
counts = {'inputs': 90, 'generated_complete': 0, 'rigged_complete': 0}
esc = html.escape
for row in items:
    key = row['id']
    input_path = G / 'inputs' / (key + '.png')
    if not input_path.exists():
        shutil.copy2(OUT / 'inputs' / (key + '.png'), input_path)
    job = jobs.get(key, {})
    row['generation_status'] = job.get('status', 'pending')
    links = []
    image = f'inputs/{key}.png'
    caption = '입력 이미지 · 생성 대기'
    if job.get('status') == 'done':
        folder = Path(job['folder'])
        stamps=[(name,(folder/name).stat().st_size,(folder/name).stat().st_mtime_ns) for name in ('model.glb','preview.png')]
        version=hashlib.sha256(json.dumps([str(folder.resolve()),stamps]).encode()).hexdigest()[:10]
        result_versions.append((key,version))
        dest = G / 'models' / key / version
        dest.mkdir(parents=True, exist_ok=True)
        for name in ['model.glb', 'model.fbx', 'model.blend', 'preview.png']:
            if not (dest/name).exists():shutil.copy2(folder/name,dest/name)
        image = f'models/{key}/{version}/preview.png'
        completion_label = 'TRELLIS + 보정 완료' if job.get('manual_repair') else 'TRELLIS 완성'
        caption = f'{completion_label} · {job.get("triangles", "?"):,} tris'
        counts['generated_complete'] += 1
        links = [f'<a href="models/{key}/{version}/model.{ext}" download="{key}.{ext}">{ext.upper()} ↓</a>' for ext in ['glb', 'fbx', 'blend']]
    elif job:
        caption = {'trellis': 'TRELLIS 생성 중', 'reviewing': '다각도 검토 중', 'blender': '메시 정리·베이크 중',
                   'review_needed': '추가 수정 필요', 'failed': '오류 확인 중'}.get(job.get('status'), caption)
        if source_state.get('paused_for_reboot') and job.get('status') in ('trellis', 'reviewing', 'blender'):
            caption = '저장 완료 · 재부팅 후 검토 대기'
    animation = ''
    if key in rigs and job.get('status') == 'done':
        rig = rigs[key];folder = Path(rig['folder']);role = roles[key][1]
        technical = json.loads((folder / 'fresh-rig-validation.json').read_text(encoding='utf8'))
        visual = json.loads((folder / 'visual-review.json').read_text(encoding='utf8'))
        assert technical['ok'] and visual['verdict'] == 'pass'
        assert (folder / 'animation-preview.mp4').exists()
        dest = G / 'rigs' / key / folder.name;dest.mkdir(parents=True, exist_ok=True)
        url = f'rigs/{key}/{folder.name}'
        for name in [role+'.blend',role+'.fbx',role+'.glb','animation-preview.mp4']:
            copy_current(folder/name,dest/name)
        copy_current(folder/'poses/idle-001.png',dest/'poster.png')
        animation = f'<video controls playsinline preload="none" poster="{url}/poster.png" style="width:100%;margin-top:12px" aria-label="{esc(row["title"])} 동작 미리보기"><source src="{url}/animation-preview.mp4" type="video/mp4"></video><p>본·동작 9개 검증 완료</p><div class="downloads">'
        animation += ' '.join(f'<a href="{url}/{role}.{ext}" download>리깅 {ext.upper()} ↓</a>' for ext in ['glb','fbx','blend']) + '</div>'
        counts['rigged_complete'] += 1;row['rigging_status'] = 'done'
        result_versions.append((key,'rig',folder.name))
    cards.append(f'<article id="{key}"><a href="{image}" target="_blank"><img src="{image}" loading="lazy" alt="{esc(row["title"])}"></a><h3>{key} {esc(row["title"])}</h3><p>{caption}</p><div class="downloads">{" ".join(links)}</div>{animation}</article>')
status_message = source_state.get('message', '준비 중')
if source_state.get('paused_for_reboot'):
    status_message = '재부팅 대기 · 생성 중지 확인 · S10 형태 저장, 검토부터 재개'
for source_text, display_text in {
    'Awaiting main-agent visual review: final_compare': '감량 모델 비교 검토 중',
    'Awaiting main-agent visual review: texture_source': '재질 다각도 검토 중',
    'Awaiting main-agent visual review: shape': '형태 다각도 검토 중',
    'final_compare 시각검사': '감량 모델 검토 이미지 준비 중',
    'texture_source 시각검사': '재질 검토 이미지 준비 중',
    'shape 시각검사': '형태 검토 이미지 준비 중',
}.items():
    status_message = status_message.replace(source_text, display_text)
manual_path = OUT / 'manual-generation-status.json'
if manual_path.exists():
    manual = json.loads(manual_path.read_text(encoding='utf8'))
    asset = manual.get('asset')
    if time.time()-manual.get('updated',0)<120 and jobs.get(asset,{}).get('status')!='done':
        manual_message = {
            'TRELLIS 생성 중': '재질 생성 중',
            'Texture generated; preparing actual views': '재질 검토 이미지 준비 중',
            'Awaiting main-agent visual review of manual texture': '재질 결과 확인 중',
        }.get(manual.get('message'),manual.get('message','보정 작업 중'))
        status_message = f'{asset} · {manual_message} / {status_message}'
if counts['generated_complete']==90 and counts['rigged_complete']==8:
    status_message='선택 모델 90종과 인물 8종의 본·동작 검증 완료'
source_state['message'] = status_message
page = '''<!doctype html><html lang="ko"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>휴게소 · TRELLIS 제작 진행</title><style>*{box-sizing:border-box}body{margin:0;background:#eeeee6;color:#24382c;font:16px/1.65 system-ui,"Malgun Gothic",sans-serif}main{max-width:1450px;margin:auto;padding:35px 25px}h1{font-size:39px;line-height:1.3}.status{background:#dce5d6;padding:16px 22px;margin:24px 0}.grid{display:grid;grid-template-columns:repeat(4,minmax(0,1fr));gap:24px}article img{width:100%;aspect-ratio:1;object-fit:contain;background:white}article h3{font-size:15px;margin:8px 0}article p{font-size:13px;margin:5px 0;color:#667455}a{color:#315e42}.downloads{display:flex;gap:14px;font-size:13px}footer{margin:35px 0;border-top:1px solid #bdc9b4;padding-top:18px;font-size:13px}@media(max-width:1000px){.grid{grid-template-columns:repeat(3,minmax(0,1fr))}}@media(max-width:700px){.grid{grid-template-columns:repeat(2,minmax(0,1fr))}main{padding:20px 12px}}@media(max-width:420px){.grid{grid-template-columns:1fr}}</style><main><a href="http://127.0.0.1:8771/">← 원래 소품 갤러리</a><h1>휴게소 소품과 사람,<br>TRELLIS로 제작 중</h1><p>소품·구조·차량 82종 + 인물 8종 · 총 90종</p><p>기존 저장값: 형태 1536 · 텍스처 2048 · 최종 15,000삼각형 · 시드 12345 · 형태 12 / 텍스처 25스텝</p><div class="status">'''+f'입력 이미지 90/90 · 모델 완료 {counts["generated_complete"]}/90 · 인물 리깅·애니메이션 완료 {counts["rigged_complete"]}/8<br>'+esc(source_state.get('message', '준비 중'))+'''</div><p>입력 그림과 완성 모델 미리보기를 구분해 표시합니다. 완료된 항목에만 모델 다운로드가 표시됩니다. 인물은 모델 생성 이후 본·애니메이션을 별도 검증합니다.</p><div class="grid">'''+''.join(cards)+'''</div><footer>기존 Blender v4와 게임 씬은 보존합니다. 이동 카트는 제외했습니다. 현재 페이지는 제작 진행 중이며 전체 완료를 뜻하지 않습니다.</footer></main></html>'''
if counts['generated_complete'] == 90 and counts['rigged_complete'] == 8:
    page = page.replace('휴게소 · TRELLIS 제작 진행', '휴게소 · TRELLIS 제작 완료')
    page = page.replace('TRELLIS로 제작 중', 'TRELLIS 제작 완료')
    page = page.replace('현재 페이지는 제작 진행 중이며 전체 완료를 뜻하지 않습니다.',
                        '선택된 모델 90종과 인물 8종의 본·동작 검증을 마쳤습니다.')
archives = sorted(G.glob('reststop-assets-r*.zip'),key=lambda p:p.stat().st_mtime)
if source_state.get('paused_for_reboot'):
    page = page.replace('TRELLIS로 제작 중', '재부팅을 위해 제작 일시 중지')
humans_archive=G/'reststop-humans-rigged-8-r1.zip'
if humans_archive.exists() and counts['rigged_complete']==8:
    page=page.replace('<div class="grid">',f'<p><a href="{humans_archive.name}" download>인물 8종 본·동작 ZIP 다운로드</a></p><div class="grid">',1)
if archives:
    filename=archives[-1].name
    page=page.replace('<div class="grid">',f'<p><a href="{esc(filename)}" download>전체 에셋 ZIP 다운로드</a></p><div class="grid">',1)
page=page.replace('<a href="http://127.0.0.1:8771/">← 원래 소품 갤러리</a>', '<a href="inputs/">원본 입력 PNG 90종</a>')
page=page.replace('시드 12345','기본 시드 12345')
page=page.replace('<div class="status">','<div class="status" aria-live="polite">',1)
page=page.replace('<div class="grid">','<button id="new-results" type="button" hidden style="margin:0 0 18px;padding:8px 14px;font:inherit;color:#24382c;background:#dce5d6;border:1px solid #829879;cursor:pointer">새 모델 보기 · 새로고침</button><div class="grid">',1)
result_revision=hashlib.sha256(json.dumps(result_versions).encode()).hexdigest()[:12]
live_script='''<script>
const initialRevision=INITIAL_REVISION;
const resultsButton=document.getElementById('new-results');
resultsButton.addEventListener('click',()=>location.reload());
async function refreshProgress(){
  try {
    const response=await fetch('progress.json',{cache:'no-store'});
    if(!response.ok)return;
    const update=await response.json();const c=update.counts;
    const label=`입력 이미지 ${c.inputs}/90 · 모델 완료 ${c.generated_complete}/90 · 인물 리깅·애니메이션 완료 ${c.rigged_complete}/8`;
    document.querySelector('.status').replaceChildren(document.createTextNode(label),document.createElement('br'),document.createTextNode(update.message));
    resultsButton.hidden=update.revision===initialRevision;
  } catch(error) { /* Keep the last known status if a write or connection is interrupted. */ }
}
setInterval(refreshProgress,15000);
</script>'''.replace('INITIAL_REVISION',json.dumps(result_revision))
page=page.replace('</main></html>',live_script+'</main></html>')
(G/'progress.json').write_text(json.dumps({'counts':counts,'message':status_message,'revision':result_revision},ensure_ascii=False),encoding='utf8')
(G / 'index.html').write_text(page, encoding='utf8')
(DOC / 'catalog.json').write_text(json.dumps(items, ensure_ascii=False, indent=2), encoding='utf8')
(DOC / 'progress.json').write_text(json.dumps(counts, indent=2), encoding='utf8')
print(json.dumps(counts))
