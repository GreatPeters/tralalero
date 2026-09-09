"""Append Artifact Tool-authored sheets without re-exporting the eight existing sheets."""
from copy import deepcopy
from datetime import datetime
from hashlib import sha256
from io import BytesIO
from pathlib import Path
import json
import os
import re
import shutil
import sys
import xml.etree.ElementTree as ET
from zipfile import ZipFile, ZIP_DEFLATED

SS = 'http://schemas.openxmlformats.org/spreadsheetml/2006/main'
REL = 'http://schemas.openxmlformats.org/officeDocument/2006/relationships'
PKG = 'http://schemas.openxmlformats.org/package/2006/relationships'
CT = 'http://schemas.openxmlformats.org/package/2006/content-types'
SOURCE = Path('Assets/ShooterSurvival/GameData/Editor/Data.xlsx')
WORK = Path('tmp/sr18-data-work-2026-09-07')


def xml_bytes(root, original):
    namespaces = {p: u for _, (p, u) in ET.iterparse(BytesIO(original), events=['start-ns'])}
    for prefix, uri in namespaces.items():
        if not re.fullmatch(r'ns\d+', prefix):
            ET.register_namespace(prefix, uri)
    result = ET.tostring(root, encoding='utf-8', xml_declaration=True)
    # Preserve declarations referenced by mc:Ignorable even when no current node uses them.
    ignored = root.get('{http://schemas.openxmlformats.org/markup-compatibility/2006}Ignorable', '').split()
    for prefix in ignored:
        declaration = f'xmlns:{prefix}='.encode()
        if declaration not in result and prefix in namespaces:
            index = result.index(b'>', result.index(b'?>') + 2)
            result = result[:index] + f' xmlns:{prefix}="{namespaces[prefix]}"'.encode() + result[index:]
    return result


def main():
    current = SOURCE.read_bytes()
    before = current
    if '--refresh-generated' in sys.argv:
        record = json.loads((WORK / 'preservation.json').read_text(encoding='utf-8'))
        if sha256(current).hexdigest() != record['afterHash']:
            raise RuntimeError('Generated workbook has subsequent edits; refusing refresh.')
        before = Path(record['backup']).read_bytes()
    with ZipFile(BytesIO(before)) as old, ZipFile(WORK / 'additions.xlsx') as new:
        entries = {n: old.read(n) for n in old.namelist()}
        additions = {n: new.read(n) for n in new.namelist()}
    workbook = ET.fromstring(entries['xl/workbook.xml'])
    sheets = workbook.find(f'{{{SS}}}sheets')
    new_workbook = ET.fromstring(additions['xl/workbook.xml'])
    new_sheets = list(new_workbook.find(f'{{{SS}}}sheets'))
    existing_names = {s.get('name') for s in sheets}
    if existing_names.intersection(s.get('name') for s in new_sheets):
        raise ValueError('Encounter sheets already exist; refusing replacement.')
    assert [s.get('name') for s in new_sheets] == ['적 배치', '보너스 배치', '기믹 배치']

    styles = ET.fromstring(entries['xl/styles.xml'])
    source_styles = ET.fromstring(additions['xl/styles.xml'])
    maps = {}
    for collection in ['numFmts', 'fonts', 'fills', 'borders', 'cellStyleXfs', 'cellXfs']:
        base = styles.find(f'{{{SS}}}{collection}')
        added = source_styles.find(f'{{{SS}}}{collection}')
        if added is None:
            maps[collection] = {}
            continue
        if base is None:
            base = ET.Element(f'{{{SS}}}{collection}')
            styles.insert(0, base)
        offset = len(base)
        num_id = max([163] + [int(e.get('numFmtId', 0)) for e in base]) + 1
        maps[collection] = {}
        for index, item in enumerate(added):
            clone = deepcopy(item)
            if collection == 'numFmts':
                maps[collection][int(item.get('numFmtId'))] = num_id
                clone.set('numFmtId', str(num_id))
                num_id += 1
            else:
                maps[collection][index] = offset + index
                for attr, mapping in [('fontId', 'fonts'), ('fillId', 'fills'), ('borderId', 'borders'), ('numFmtId', 'numFmts'), ('xfId', 'cellStyleXfs')]:
                    if attr in clone.attrib:
                        value = int(clone.get(attr))
                        clone.set(attr, str(maps.get(mapping, {}).get(value, value)))
            base.append(clone)
        base.set('count', str(len(base)))
    entries['xl/styles.xml'] = xml_bytes(styles, entries['xl/styles.xml'])

    # Convert only NEW string cells to inline strings, so the existing shared-string part is untouched.
    strings = ET.fromstring(additions['xl/sharedStrings.xml']) if 'xl/sharedStrings.xml' in additions else []
    new_rels = ET.fromstring(additions['xl/_rels/workbook.xml.rels'])
    targets = {r.get('Id'): r.get('Target') for r in new_rels}
    rels = ET.fromstring(entries['xl/_rels/workbook.xml.rels'])
    content_types = ET.fromstring(entries['[Content_Types].xml'])
    next_id = max(int(s.get('sheetId')) for s in sheets) + 1
    for index, sheet in enumerate(new_sheets, 1):
        target = targets[sheet.get(f'{{{REL}}}id')]
        source_part = target.lstrip('/') if target.startswith('/') else 'xl/' + target
        assert 'xl/worksheets/_rels/' + Path(source_part).name + '.rels' not in additions, 'Unexpected related objects'
        root = ET.fromstring(additions[source_part])
        for node in root.iter():
            for attr in ('s', 'style'):
                if attr in node.attrib:
                    node.set(attr, str(maps['cellXfs'][int(node.get(attr))]))
            if node.tag == f'{{{SS}}}c' and node.get('t') == 's':
                value = node.find(f'{{{SS}}}v')
                shared = strings[int(value.text)]
                node.remove(value)
                inline = ET.SubElement(node, f'{{{SS}}}is')
                inline.extend(deepcopy(list(shared)))
                node.set('t', 'inlineStr')
        part = f'xl/worksheets/encounter-settings-{index}.xml'
        relationship = f'rIdEncounterSettings{index}'
        assert part not in entries and all(r.get('Id') != relationship for r in rels)
        entries[part] = xml_bytes(root, additions[source_part])
        ET.SubElement(sheets, f'{{{SS}}}sheet', {'name': sheet.get('name'), 'sheetId': str(next_id), f'{{{REL}}}id': relationship})
        ET.SubElement(rels, f'{{{PKG}}}Relationship', {'Id': relationship, 'Type': REL + '/worksheet', 'Target': 'worksheets/' + Path(part).name})
        ET.SubElement(content_types, f'{{{CT}}}Override', {'PartName': '/' + part, 'ContentType': 'application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml'})
        next_id += 1
    for part, root in [('xl/workbook.xml', workbook), ('xl/_rels/workbook.xml.rels', rels), ('[Content_Types].xml', content_types)]:
        entries[part] = xml_bytes(root, entries[part])
    candidate = WORK / 'Data.candidate.xlsx'
    with ZipFile(candidate, 'w', ZIP_DEFLATED) as output:
        for name, contents in entries.items():
            output.writestr(name, contents)
    with ZipFile(BytesIO(before)) as old, ZipFile(candidate) as after:
        changed = [n for n in old.namelist() if old.read(n) != after.read(n)]
        assert set(changed) == {'xl/styles.xml', 'xl/workbook.xml', 'xl/_rels/workbook.xml.rels', '[Content_Types].xml'}, changed
        for name in after.namelist():
            if name.endswith('.xml') or name.endswith('.rels'):
                ET.fromstring(after.read(name))
    if SOURCE.read_bytes() != current:
        raise RuntimeError('Data.xlsx changed during authoring; source left untouched.')
    backup = WORK / f'Data.before-{datetime.now():%H%M%S}.xlsx'
    backup.write_bytes(before)
    os.replace(candidate, SOURCE)
    (WORK / 'preservation.json').write_text(json.dumps({'backup': str(backup), 'beforeHash': sha256(before).hexdigest(), 'afterHash': sha256(SOURCE.read_bytes()).hexdigest(), 'existingSheetsUnchanged': 8, 'newRows': [25, 25, 24], 'changedPackageParts': changed}, indent=2), encoding='utf-8')
    print('Added 25 enemy / 25 bonus / 24 gimmick settings; all 8 existing worksheet parts preserved byte-for-byte.')


if __name__ == '__main__':
    main()
