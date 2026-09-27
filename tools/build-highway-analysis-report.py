"""Assemble an offline-capable report from native Unity evidence; keep source captures intact."""
import csv
import json
import shutil
from pathlib import Path
from PIL import Image

BASE = Path(__file__).resolve().parents[1]
ROOT = BASE / 'outputs/highway-analysis-2026-09-26'
SITE = ROOT / 'site'
SITE.mkdir(exist_ok=True)
(SITE / 'images').mkdir(exist_ok=True)
(SITE / 'evidence').mkdir(exist_ok=True)

def load(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))

def picture(path):
    name = '-'.join(path.relative_to(ROOT).parts).replace('.png', '')
    dest = SITE / 'images' / (name + '.webp')
    with Image.open(path) as image:
        image.thumbnail((540, 1170))
        image.save(dest, quality=84)
    return 'images/' + dest.name

runs = []
for file in sorted((ROOT / 'runs').glob('*/result.json')):
    r = load(file)
    r['images'] = [{'url': picture(p), 'name': p.stem} for p in sorted(file.parent.glob('*.png'))]
    timeline = list(csv.DictReader((file.parent / 'timeline.csv').open(encoding='utf-8-sig')))
    r['timeline'] = [{k: float(t[k]) for k in ('time', 'distance', 'hp', 'maxHp', 'lane')} for t in timeline]
    target = SITE / 'evidence' / f'run-{r["attempt"]:02d}.json'
    shutil.copy2(file, target)
    r['evidence'] = 'evidence/' + target.name
    runs.append(r)

coverage = []
for label in ['left', 'right']:
    directory = ROOT / 'coverage' / label
    if directory.exists():
        coverage.append({'label': label, 'result': load(directory / 'result.json') if (directory / 'result.json').exists() else None,
                         'images': [{'url': picture(p), 'distance': int(p.stem.split('-')[1])} for p in sorted(directory.glob('d-*.png'))]})

scene = load(ROOT / 'scene.json')
for name in ['scene.json', 'conditions.json']:
    shutil.copy2(ROOT / name, SITE / 'evidence' / name)
data = {'runs': runs, 'coverage': coverage, 'scene': scene, 'conditions': load(ROOT / 'conditions.json')}
(SITE / 'report-data.js').write_text('const REPORT = ' + json.dumps(data, ensure_ascii=False) + ';', encoding='utf-8')
shutil.copy2(BASE / 'tools/highway-analysis-report.html', SITE / 'index.html')
font = BASE / 'Assets/JH/Font/NotoSansKR-VariableFont_wght.ttf'
if font.exists():
    shutil.copy2(font, SITE / 'NotoSansKR.ttf')
print(json.dumps({'site': str(SITE), 'runs': len(runs), 'coverage': [(c['label'], c['result'] is not None, len(c['images'])) for c in coverage]}, ensure_ascii=False))
