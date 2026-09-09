"""Merge Artifact Tool-authored cells/new sheets; preserve unrelated workbook content."""
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

work = Path('tmp/noryangjin-release-20260910')
source = Path('Assets/ShooterSurvival/GameData/Editor/Data.xlsx')
plan = json.loads((work / 'changes.json').read_text(encoding='utf-8'))
before = (work / 'Data.before.xlsx').read_bytes()
assert sha256(before).hexdigest() == plan['beforeHash']
assert source.read_bytes() == before, 'Concurrent source workbook change'
spec = importlib.util.spec_from_file_location('xml_helpers', Path(__file__).with_name('append-encounter-workbook-sheets.py'))
helper = importlib.util.module_from_spec(spec)
spec.loader.exec_module(helper)
SS, REL, PKG, CT = helper.SS, helper.REL, helper.PKG, helper.CT
q = lambda n: '{' + SS + '}' + n
with ZipFile(BytesIO(before)) as archive:
    old = {n: archive.read(n) for n in archive.namelist()}
with ZipFile(work / 'candidate.xlsx') as archive:
    candidate = {n: archive.read(n) for n in archive.namelist()}

def parts(package):
    targets = {r.get('Id'): r.get('Target') for r in ET.fromstring(package['xl/_rels/workbook.xml.rels'])}
    return {s.get('name'): (targets[s.get('{' + REL + '}id')].lstrip('/') if targets[s.get('{' + REL + '}id')].startswith('/') else 'xl/' + targets[s.get('{' + REL + '}id')])
            for s in ET.fromstring(package['xl/workbook.xml']).find(q('sheets'))}

old_parts, new_parts = parts(old), parts(candidate)
updated = dict(old)
styles, new_styles = ET.fromstring(old['xl/styles.xml']), ET.fromstring(candidate['xl/styles.xml'])
maps = {}
for collection in ['numFmts', 'fonts', 'fills', 'borders', 'cellStyleXfs', 'cellXfs']:
    base, extra = styles.find(q(collection)), new_styles.find(q(collection))
    maps[collection] = {}
    if extra is None: continue
    if base is None: base = ET.Element(q(collection)); styles.insert(0, base)
    offset = len(base)
    next_num = max([163] + [int(e.get('numFmtId', 0)) for e in base]) + 1
    for index, entry in enumerate(extra):
        clone = deepcopy(entry)
        if collection == 'numFmts':
            maps[collection][int(entry.get('numFmtId'))] = next_num
            clone.set('numFmtId', str(next_num)); next_num += 1
        else:
            maps[collection][index] = offset + index
            for attr, mapping in [('fontId','fonts'),('fillId','fills'),('borderId','borders'),('numFmtId','numFmts'),('xfId','cellStyleXfs')]:
                if attr in clone.attrib:
                    value = int(clone.get(attr)); clone.set(attr, str(maps[mapping].get(value, value)))
        base.append(clone)
    base.set('count', str(len(base)))
updated['xl/styles.xml'] = helper.xml_bytes(styles, old['xl/styles.xml'])
strings = ET.fromstring(candidate['xl/sharedStrings.xml']) if 'xl/sharedStrings.xml' in candidate else []

def convert_cell(cell, style=None, preserve_style=False):
    cell = deepcopy(cell)
    if cell.get('t') == 's':
        value = cell.find(q('v')); inline = ET.SubElement(cell, q('is'))
        inline.extend(deepcopy(list(strings[int(value.text)]))); cell.remove(value); cell.set('t', 'inlineStr')
    if preserve_style:
        if style is None: cell.attrib.pop('s', None)
        else: cell.set('s', style)
    elif cell.get('s') is not None: cell.set('s', str(maps['cellXfs'][int(cell.get('s'))]))
    return cell

def column(address):
    value = 0
    for letter in re.sub(r'\d', '', address): value = value * 26 + ord(letter) - 64
    return value

for name, addresses in plan['changed'].items():
    original = old[old_parts[name]]; root = ET.fromstring(original); data = root.find(q('sheetData'))
    rows = {int(r.get('r')): r for r in data}
    cells = {c.get('r'): c for r in data for c in r}
    exported = ET.fromstring(candidate[new_parts[name]])
    replacement = {c.get('r'): c for r in exported.find(q('sheetData')) for c in r}
    copy_style = set(plan['styleCells'].get(name, []))
    for address in set(addresses):
        row_number = int(re.search(r'\d+', address).group())
        if row_number not in rows:
            attrs = dict(rows.get(3, next(iter(rows.values()))).attrib, r=str(row_number))
            rows[row_number] = ET.Element(q('row'), attrs); data.append(rows[row_number])
        previous = cells.get(address)
        replacement_cell = replacement.get(address, ET.Element(q('c'), {'r': address}))
        converted = convert_cell(replacement_cell, previous.get('s') if previous is not None else None,
                                 preserve_style=previous is not None and address not in copy_style)
        if previous is not None: rows[row_number].remove(previous)
        rows[row_number].append(converted)
        rows[row_number][:] = sorted(rows[row_number], key=lambda c: column(c.get('r')))
    data[:] = sorted(data, key=lambda r: int(r.get('r')))
    dimension = root.find(q('dimension'))
    if dimension is not None:
        dimension.set('ref', re.sub(r'\d+$', str(max(rows)), dimension.get('ref')))
    updated[old_parts[name]] = helper.xml_bytes(root, original)

workbook = ET.fromstring(old['xl/workbook.xml']); sheets = workbook.find(q('sheets'))
rels = ET.fromstring(old['xl/_rels/workbook.xml.rels']); types = ET.fromstring(old['[Content_Types].xml'])
for index, name in enumerate(plan['newSheets']):
    assert name not in old_parts, 'New sheet already exists'
    target = 'xl/worksheets/release-' + str(index + 1) + '.xml'
    root = ET.fromstring(candidate[new_parts[name]])
    for row in root.find(q('sheetData')):
        for i, cell in enumerate(list(row)): row[i] = convert_cell(cell)
        if row.get('s') is not None: row.set('s', str(maps['cellXfs'][int(row.get('s'))]))
    for col in root.findall(q('cols') + '/' + q('col')):
        if col.get('style') is not None: col.set('style', str(maps['cellXfs'][int(col.get('style'))]))
    updated[target] = helper.xml_bytes(root, candidate[new_parts[name]])
    relationship = 'rIdRelease' + str(index + 1)
    ET.SubElement(rels, '{' + PKG + '}Relationship', {'Id': relationship, 'Type': REL + '/worksheet', 'Target': target[3:]})
    ET.SubElement(sheets, q('sheet'), {'name': name, 'sheetId': str(max(int(s.get('sheetId')) for s in sheets) + 1), '{' + REL + '}id': relationship})
    ET.SubElement(types, '{' + CT + '}Override', {'PartName': '/' + target, 'ContentType': 'application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml'})
updated['xl/workbook.xml'] = helper.xml_bytes(workbook, old['xl/workbook.xml'])
updated['xl/_rels/workbook.xml.rels'] = helper.xml_bytes(rels, old['xl/_rels/workbook.xml.rels'])
updated['[Content_Types].xml'] = helper.xml_bytes(types, old['[Content_Types].xml'])
changed_parts = [name for name in old if old[name] != updated[name]]
expected = {old_parts[name] for name in plan['changed']} | {'xl/styles.xml','xl/workbook.xml','xl/_rels/workbook.xml.rels','[Content_Types].xml'}
assert set(changed_parts) == expected, changed_parts
temporary = work / 'merged.xlsx'
with ZipFile(temporary, 'w', ZIP_DEFLATED) as archive:
    for name, value in updated.items(): archive.writestr(name, value)
assert source.read_bytes() == before, 'Concurrent source workbook change'
os.replace(temporary, source)
(work / 'preservation.json').write_text(json.dumps({'beforeHash': sha256(before).hexdigest(), 'afterHash': sha256(source.read_bytes()).hexdigest(),
    'changedParts': changed_parts, 'newSheets': plan['newSheets'], 'unchangedParts': len(old) - len(changed_parts)}, indent=2), encoding='utf-8')
print(json.dumps({'changedParts': len(changed_parts), 'newSheets': plan['newSheets'], 'unchangedParts': len(old)-len(changed_parts)}))
