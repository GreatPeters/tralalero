"""Graft only the two Artifact Tool-edited sheets; preserve all other package parts."""
from copy import deepcopy
from hashlib import sha256
from io import BytesIO
from pathlib import Path
from zipfile import ZipFile, ZIP_DEFLATED
import importlib.util
import json
import os
import sys
import xml.etree.ElementTree as ET

spec = importlib.util.spec_from_file_location('graft_helpers', Path(__file__).with_name('append-encounter-workbook-sheets.py'))
helpers = importlib.util.module_from_spec(spec)
spec.loader.exec_module(helpers)
SS, REL = helpers.SS, helpers.REL
work = Path('tmp/sr18-polish-2026-09-09')
source = Path('Assets/ShooterSurvival/GameData/Editor/Data.xlsx')
backup_name = 'Data.revision-before.xlsx' if '--revision' in sys.argv else 'Data.before.xlsx'
before = (work / backup_name).read_bytes()
assert source.read_bytes() == before, 'Source changed after snapshot; stop.'
with ZipFile(BytesIO(before)) as z:
    entries = {n:z.read(n) for n in z.namelist()}
with ZipFile(work / 'artifact-candidate.xlsx') as z:
    added = {n:z.read(n) for n in z.namelist()}

def parts(package):
    rels = ET.fromstring(package['xl/_rels/workbook.xml.rels'])
    targets = {r.get('Id'):r.get('Target') for r in rels}
    def absolute(t): return t.lstrip('/') if t.startswith('/') else 'xl/'+t
    return {s.get('name'):absolute(targets[s.get('{'+REL+'}id')]) for s in ET.fromstring(package['xl/workbook.xml']).find('{'+SS+'}sheets')}

old_parts, new_parts = parts(entries), parts(added)
styles = ET.fromstring(entries['xl/styles.xml'])
new_styles = ET.fromstring(added['xl/styles.xml'])
maps = helpers.merge_styles(styles, new_styles)
entries['xl/styles.xml']=helpers.xml_bytes(styles,entries['xl/styles.xml'])
strings=ET.fromstring(added['xl/sharedStrings.xml']) if 'xl/sharedStrings.xml' in added else []
changed=['xl/styles.xml']
for name in ['적 배치','기믹 배치']:
    old_part, new_part = old_parts[name], new_parts[name]
    root=ET.fromstring(added[new_part])
    assert root.find('{'+SS+'}tableParts') is None, 'Unexpected native table export; do not drop relationships.'
    for node in root.iter():
        for attr in ('s','style'):
            if attr in node.attrib: node.set(attr,str(maps['cellXfs'][int(node.get(attr))]))
        if node.tag=='{'+SS+'}c' and node.get('t')=='s':
            value=node.find('{'+SS+'}v'); shared=strings[int(value.text)]; node.remove(value)
            inline=ET.SubElement(node,'{'+SS+'}is'); inline.extend(deepcopy(list(shared))); node.set('t','inlineStr')
    entries[old_part]=helpers.xml_bytes(root,added[new_part]); changed.append(old_part)
candidate=work/'Data.grafted.xlsx'
with ZipFile(candidate,'w',ZIP_DEFLATED) as z:
    for name,contents in entries.items(): z.writestr(name,contents)
with ZipFile(BytesIO(before)) as old, ZipFile(candidate) as new:
    actual=[n for n in old.namelist() if old.read(n)!=new.read(n)]
    assert set(changed) - {'xl/styles.xml'} <= set(actual) <= set(changed), actual
    for name in new.namelist():
        if name.endswith(('.xml','.rels')): ET.fromstring(new.read(name))
assert source.read_bytes()==before,'Concurrent source edit'
os.replace(candidate,source)
record={'backup':str(work/backup_name),'beforeHash':sha256(before).hexdigest(),'afterHash':sha256(source.read_bytes()).hexdigest(),'unchangedSheets':9,'changedParts':actual}
(work/('revision-preservation.json' if '--revision' in sys.argv else 'preservation.json')).write_text(json.dumps(record,indent=2),encoding='utf-8')
print(json.dumps(record,indent=2))
