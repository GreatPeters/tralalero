"""Splice two Artifact Tool-authored rows while retaining every other workbook part."""
from copy import deepcopy
from hashlib import sha256
from io import BytesIO
from pathlib import Path
from zipfile import ZipFile, ZIP_DEFLATED
import importlib.util
import json
import os
import re
import xml.etree.ElementTree as ET

work = Path('tmp/sr18-contact-pairs-20260910')
source = Path('Assets/ShooterSurvival/GameData/Editor/Data.xlsx')
backup = Path('tmp/backups/sr18-contact-pairs-20260910-022738/Data.xlsx')
before = backup.read_bytes()
assert source.read_bytes() == before, 'Workbook changed after snapshot'
spec = importlib.util.spec_from_file_location('xml_preservation', Path(__file__).with_name('append-encounter-workbook-sheets.py'))
helper = importlib.util.module_from_spec(spec)
spec.loader.exec_module(helper)
ns = helper.SS
q = lambda name: '{' + ns + '}' + name

def sheet_path(package):
    targets = {r.get('Id'): r.get('Target') for r in ET.fromstring(package['xl/_rels/workbook.xml.rels'])}
    sheets = ET.fromstring(package['xl/workbook.xml']).find(q('sheets'))
    target = targets[next(s.get('{' + helper.REL + '}id') for s in sheets if s.get('name') == '적 배치')]
    return target.lstrip('/') if target.startswith('/') else 'xl/' + target

with ZipFile(BytesIO(before)) as archive:
    original = {n: archive.read(n) for n in archive.namelist()}
with ZipFile(work / 'candidate.xlsx') as archive:
    candidate = {n: archive.read(n) for n in archive.namelist()}
part = sheet_path(original)
root = ET.fromstring(original[part])
data = root.find(q('sheetData'))
old_rows = {int(r.get('r')): r for r in data}
new_rows = {int(r.get('r')): r for r in ET.fromstring(candidate[sheet_path(candidate)]).find(q('sheetData'))}
strings = ET.fromstring(candidate['xl/sharedStrings.xml']) if 'xl/sharedStrings.xml' in candidate else []
mappings = json.loads((work / 'row-mappings.json').read_text(encoding='utf-8'))
assert not any(row in old_rows for row in (28, 29)), 'Destination rows already exist'
for mapping in mappings:
    row = deepcopy(new_rows[mapping['destination']])
    template = old_rows[mapping['sourceRow']]
    row.attrib = dict(template.attrib, r=str(mapping['destination']))
    styles = {re.sub(r'\d', '', cell.get('r')): cell.get('s') for cell in template}
    for cell in row:
        column = re.sub(r'\d', '', cell.get('r'))
        if styles.get(column) is None:
            cell.attrib.pop('s', None)
        else:
            cell.set('s', styles[column])
        if cell.get('t') == 's':
            value = cell.find(q('v'))
            inline = ET.SubElement(cell, q('is'))
            inline.extend(deepcopy(list(strings[int(value.text)])))
            cell.remove(value)
            cell.set('t', 'inlineStr')
    data.append(row)
for node in root.iter():
    if node.tag in (q('dimension'), q('autoFilter')) and node.get('ref'):
        node.set('ref', re.sub(r'27$', '29', node.get('ref')))
    if node.tag == q('dataValidation') and node.get('sqref'):
        node.set('sqref', re.sub(r'27(?=\s|$)', '29', node.get('sqref')))
updated = dict(original)
updated[part] = helper.xml_bytes(root, original[part])
assert all(updated[p] == original[p] for p in original if p != part)
for row_number, expected in ((28, (43, 72)), (29, (124, 207))):
    row = next(r for r in data if int(r.get('r')) == row_number)
    cells = {re.sub(r'\d', '', c.get('r')): c for c in row}
    assert tuple(float(cells[c].find(q('v')).text) for c in ('N', 'O')) == expected
temporary = work / 'preserved.xlsx'
with ZipFile(temporary, 'w', ZIP_DEFLATED) as archive:
    for name, contents in updated.items():
        archive.writestr(name, contents)
assert source.read_bytes() == before, 'Concurrent workbook edit'
os.replace(temporary, source)
(work / 'preservation.json').write_text(json.dumps({'backup': str(backup), 'changedParts': [part],
    'unchangedParts': len(original) - 1, 'beforeHash': sha256(before).hexdigest(),
    'afterHash': sha256(source.read_bytes()).hexdigest(), 'addedRows': mappings}, indent=2), encoding='utf-8')
print(json.dumps({'changedParts': [part], 'rows': [28, 29], 'cachedStats': [[43, 72], [124, 207]]}))
