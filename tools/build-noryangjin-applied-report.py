"""Publish selected native captures, model previews and sanitized verification summaries."""
from pathlib import Path
import hashlib
import json
import shutil

ROOT = Path(__file__).resolve().parents[1]
SITE = ROOT / 'outputs/noryangjin-review-homepage-2026-09-28/site/applied'
PREVIEW = ROOT / 'tmp/image-previews/noryangjin-revamp-fix-2026-09-28'
SITE.mkdir(parents=True, exist_ok=True)
(SITE / 'assets').mkdir(exist_ok=True)

def copy(source, name):
    source = Path(source)
    target = SITE / 'assets' / name
    if target.exists() and source.read_bytes() != target.read_bytes():
        raise RuntimeError(f'Refusing to overwrite a different capture: {name}')
    if not target.exists():
        shutil.copy2(source, target)
    return 'assets/' + name

inside = 'play-inside-030758'
outside = 'play-outside-031200'
captures = [
    ('play-capture-final-inside-033154', '010', '실내 시장동', '낮은 천장·벽·수조·젖은 타일을 적용한 실제 구간 진입 테스트. 촬영을 위해 시작 위치를 옮긴 화면입니다.'),
    ('play-capture-final-inside-033154', '029', '터렛트', '기존 적과의 중첩을 정리하고 운전자·적재함·체력 표시를 분리한 구간 테스트 화면입니다.'),
    ('play-capture-final-inside-033154', '007', '호객 러시', '약15명 상인들이 정면에서 몰려옵니다. 구간 진입 테스트라 튜토리얼 안내가 함께 보입니다.'),
    (inside, '021', '물청소', '통로의 물줄기와 회피 공간. 접촉하면 회전 중 초당 최대 체력 10% 감소.'),
    (inside, '028', '셔터 탈출', '상자 벽을 부수고 닫히는 셔터 아래로 통과합니다.'),
    (outside, '019', '바깥 부두의 파도', '경고 구역을 피하는 별도 물리 경로. 접촉 즉사는 별도 실제 컴포넌트 검증으로 확인.'),
    ('play-capture-final-outside-033318', '025', '바깥길 겹침 정리', '기존 점포와 길에 붙은 난간 기둥을 정리한 뒤의 실제 구간 진입 테스트입니다.'),
]
gallery = []
for folder, frame, title, caption in captures:
    source = PREVIEW / folder / f'frame-{frame}.png'
    file = copy(source, f'{folder}-{frame}.png')
    gallery.append({'title': title, 'caption': caption, 'image': file,
                    'source': str(source.relative_to(ROOT)).replace('\\', '/'),
                    'sha256': hashlib.sha256(source.read_bytes()).hexdigest()})

copy(PREVIEW / 'user-shot-1.png', 'before-indoor.png')
copy(PREVIEW / 'meshy-native-six.png', 'models-native.png')
models = []
for asset, title, tris in [('N03_forklift','지게차',8749),('N04_livefish_truck','활어 운반차',10752),
                          ('N05_harbor_crane','항구 크레인',12032),('N06_red_lighthouse','빨간 등대',6301),
                          ('N07_market_cat','가게 고양이',3631),('N08_frozen_tuna','냉동 참치',4591)]:
    run = ROOT / 'outputs/meshy-noryangjin-fix-2026-09-28'
    image = copy(run / 'models' / asset / 'thumb1.png', asset + '.png')
    models.append({'name': title, 'id': asset, 'triangles': tris, 'image': image})
    immutable = PREVIEW / (asset + '-reference.png')
    if not immutable.exists(): shutil.copy2(run / 'concepts' / asset / 'try1.png', immutable)

runs = []
for folder in [inside, outside, 'play-outside-030433']:
    data = json.loads((PREVIEW / folder / 'result.json').read_text(encoding='utf-8-sig'))
    runs.append({key: data.get(key) for key in ['route','elapsed','health','maximum','completed','cause','debugObserved','branchComplete']})
data = {'gallery': gallery, 'models': models, 'runs': runs, 'credits': 180,
        'limits': ['원본 씬·빌드 목록·공유 엑셀 보존', '폰 실기기 성능·사람 승률은 측정하지 않음',
                   '양쪽 완주 기록은 공격37·체력46·공속30의 실제 성장 수치. 9999/체력 고정 미사용.',
                   '공격20·체력12·공속10의 바깥길은 203.5초 후반 적 접촉으로 패배.']}
(SITE / 'data.js').write_text('const report = ' + json.dumps(data, ensure_ascii=False, indent=2) + ';\n', encoding='utf-8')
(SITE / 'capture-manifest.json').write_text(json.dumps(data, ensure_ascii=False, indent=2), encoding='utf-8')
print(json.dumps({'site': str(SITE), 'captures': len(gallery), 'models': len(models)}, ensure_ascii=False))
