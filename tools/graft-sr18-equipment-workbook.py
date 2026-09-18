"""Install only the Artifact Tool-authored equipment sheet, preserving other tabs."""
from copy import deepcopy
from hashlib import sha256
from importlib.util import module_from_spec, spec_from_file_location
from pathlib import Path
from zipfile import ZipFile, ZIP_DEFLATED
import json
import argparse
import xml.etree.ElementTree as ET

parser = argparse.ArgumentParser()
parser.add_argument('--work', default='tmp/sr18-presentation-data-20260910')
parser.add_argument('--prefix', default='equipment')
args = parser.parse_args()
work = Path(args.work)
prefix = args.prefix
source = Path('Assets/ShooterSurvival/GameData/Editor/Data.xlsx')
before = (work / f'{prefix}-before.xlsx').read_bytes()
plan = json.loads((work / f'{prefix}-changes.json').read_text())
assert sha256(before).hexdigest() == plan['beforeHash']
assert source.read_bytes() == before, 'Concurrent workbook modification'
spec = spec_from_file_location('helpers', Path(__file__).with_name('append-encounter-workbook-sheets.py'))
helper = module_from_spec(spec)
spec.loader.exec_module(helper)
q = lambda name: '{' + helper.SS + '}' + name
with ZipFile(source) as archive:
    old = {name: archive.read(name) for name in archive.namelist()}
with ZipFile(work / f'{prefix}-candidate.xlsx') as archive:
    candidate = {name: archive.read(name) for name in archive.namelist()}

def sheet_parts(package):
    targets = {r.get('Id'): r.get('Target') for r in ET.fromstring(package['xl/_rels/workbook.xml.rels'])}
    return {s.get('name'): targets[s.get('{' + helper.REL + '}id')].lstrip('/') if targets[s.get('{' + helper.REL + '}id')].startswith('/') else 'xl/' + targets[s.get('{' + helper.REL + '}id')]
            for s in ET.fromstring(package['xl/workbook.xml']).find(q('sheets'))}

updated = dict(old)
styles = ET.fromstring(old['xl/styles.xml'])
new_styles = ET.fromstring(candidate['xl/styles.xml'])
maps = helper.merge_styles(styles, new_styles)
updated['xl/styles.xml'] = helper.xml_bytes(styles, old['xl/styles.xml'])
strings = ET.fromstring(candidate['xl/sharedStrings.xml']) if 'xl/sharedStrings.xml' in candidate else []
old_parts, new_parts = sheet_parts(old), sheet_parts(candidate)
for name in plan['sheets']:
    original = candidate[new_parts[name]]
    root = ET.fromstring(original)
    for cell in root.iter(q('c')):
        if cell.get('t') == 's':
            value = cell.find(q('v'))
            inline = ET.SubElement(cell, q('is'))
            inline.extend(deepcopy(list(strings[int(value.text)])))
            cell.remove(value)
            cell.set('t', 'inlineStr')
        if cell.get('s') is not None:
            cell.set('s', str(maps['cellXfs'][int(cell.get('s'))]))
    for row in root.iter(q('row')):
        if row.get('s') is not None: row.set('s', str(maps['cellXfs'][int(row.get('s'))]))
    for column in root.iter(q('col')):
        if column.get('style') is not None: column.set('style', str(maps['cellXfs'][int(column.get('style'))]))
    updated[old_parts[name]] = helper.xml_bytes(root, original)
changed = [name for name in old if old[name] != updated[name]]
assert set(changed) <= {'xl/styles.xml', *(old_parts[name] for name in plan['sheets'])}
temporary = work / f'{prefix}-final.xlsx'
with ZipFile(temporary, 'w', ZIP_DEFLATED) as archive:
    for name, data in updated.items(): archive.writestr(name, data)
source.write_bytes(temporary.read_bytes())
(work / f'{prefix}-preservation.json').write_text(json.dumps({'changed': changed, 'unchanged': len(old) - len(changed)}, indent=2))
print(json.dumps({'changed': changed, 'unrelatedPartsPreserved': True}))
