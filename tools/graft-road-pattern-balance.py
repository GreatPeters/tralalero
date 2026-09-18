"""Copy only reviewed Artifact Tool cells into the preserved native workbook."""
from copy import deepcopy
from hashlib import sha256
from importlib.util import module_from_spec, spec_from_file_location
from pathlib import Path
from zipfile import ZipFile, ZIP_DEFLATED
import json
import re
import xml.etree.ElementTree as ET

work = Path('tmp/road-pattern-balance-20260913')
plan = json.loads((work / 'changes.json').read_text(encoding='utf-8'))
source = Path('Assets/ShooterSurvival/GameData/Editor/Data.xlsx')
assert sha256(source.read_bytes()).hexdigest() == plan['installFromHash']
spec = spec_from_file_location('helpers', Path(__file__).with_name('append-encounter-workbook-sheets.py'))
helper = module_from_spec(spec); spec.loader.exec_module(helper)
q = lambda name: '{' + helper.SS + '}' + name
def package(path):
    with ZipFile(path) as archive: return {n: archive.read(n) for n in archive.namelist()}
old, new = package(work / 'before.xlsx'), package(work / 'artifact-candidate.xlsx')
def sheets(data):
    rels = {r.get('Id'): r.get('Target') for r in ET.fromstring(data['xl/_rels/workbook.xml.rels'])}
    return {s.get('name'): ('xl/' + rels[s.get('{'+helper.REL+'}id')]).replace('xl//xl/', 'xl/') for s in ET.fromstring(data['xl/workbook.xml']).find(q('sheets'))}
old_paths, new_paths = sheets(old), sheets(new)
styles = ET.fromstring(old['xl/styles.xml'])
mapping = helper.merge_styles(styles, ET.fromstring(new['xl/styles.xml']))
strings = ET.fromstring(new['xl/sharedStrings.xml']) if 'xl/sharedStrings.xml' in new else []
edits = {'밸런스 조정': ['C88', 'D88'] + [f'{c}{r}' for r in range(109,119) for c in 'BCD'],
         '환경 변수': ['D12'] + [f'{c}{r}' for r in range(26,35) for c in 'BCD']}
for row in plan['checks']['disabled']: edits.setdefault(row['sheet'], []).append(f"E{row['row']}")
updated = dict(old)
for name, addresses in edits.items():
    target = ET.fromstring(old[old_paths[name]]); incoming = ET.fromstring(new[new_paths[name]])
    incoming_cells = {c.get('r'): c for c in incoming.iter(q('c'))}
    data = target.find(q('sheetData')); rows = {r.get('r'): r for r in data}
    for address in addresses:
        cell = deepcopy(incoming_cells[address]); number = re.search(r'\d+', address)[0]
        if cell.get('t') == 's':
            value = cell.find(q('v')); inline = ET.SubElement(cell, q('is'))
            inline.extend(deepcopy(list(strings[int(value.text)]))); cell.remove(value); cell.set('t', 'inlineStr')
        if cell.get('s') is not None: cell.set('s', str(mapping['cellXfs'][int(cell.get('s'))]))
        if number not in rows: rows[number] = ET.SubElement(data, q('row'), {'r': number})
        row = rows[number]
        for previous in list(row):
            if previous.get('r') == address: row.remove(previous)
        row.append(cell)
        def col(c):
            result = 0
            for letter in re.match('[A-Z]+', c.get('r'))[0]: result = result * 26 + ord(letter) - 64
            return result
        row[:] = sorted(row, key=col)
        if name == '밸런스 조정' and int(number) >= 110: row.set('ht', '44'); row.set('customHeight', '1')
    data[:] = sorted(data, key=lambda r: int(r.get('r')))
    dimension = target.find(q('dimension'))
    if dimension is not None:
        ref = dimension.get('ref'); highest = max(int(r.get('r')) for r in data)
        dimension.set('ref', re.sub(r'\d+$', str(max(highest, int(re.search(r'\d+$', ref)[0]))), ref))
    updated[old_paths[name]] = helper.xml_bytes(target, old[old_paths[name]])
updated['xl/styles.xml'] = helper.xml_bytes(styles, old['xl/styles.xml'])
assert len(updated['xl/styles.xml']) < 100000
output = Path('outputs/road-patterns-2026-09-13/balance/Data.xlsx'); output.parent.mkdir(parents=True, exist_ok=True)
with ZipFile(output, 'w', ZIP_DEFLATED) as archive:
    for name, data in updated.items(): archive.writestr(name, data)
report = {'sourceSha256':plan['beforeHash'], 'installFromHash':plan['installFromHash'], 'candidateSha256':sha256(output.read_bytes()).hexdigest(),
          'changedCells':edits, 'stylesheetBytes':len(updated['xl/styles.xml']), 'checks':plan['checks'],
          'unrelatedPartsPreserved':all(updated[n] == old[n] for n in old if n not in [old_paths[s] for s in edits] + ['xl/styles.xml'])}
output.with_name('verification.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps({k:report[k] for k in ['candidateSha256','stylesheetBytes','unrelatedPartsPreserved']}))
