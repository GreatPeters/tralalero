"""Graft the twelve artifact-authored bonus cells while preserving other data caches."""
from copy import copy,deepcopy
from pathlib import Path
from zipfile import ZipFile
import importlib.util,json,xml.etree.ElementTree as ET

root=Path(__file__).resolve().parents[1]
work=root/'tmp/bonus-amount-fix-2026-09-22'
source=root/'Assets/ShooterSurvival/GameData/Editor/Data.xlsx'
output=root/'outputs/bonus-amount-fix-2026-09-22/Data.xlsx'
assert source.read_bytes()==(work/'before.xlsx').read_bytes(),'Source changed since snapshot'
spec=importlib.util.spec_from_file_location('preserve',root/'tools/append-encounter-workbook-sheets.py')
helper=importlib.util.module_from_spec(spec);spec.loader.exec_module(helper)
q=lambda name:'{'+helper.SS+'}'+name
def package(path):
    with ZipFile(path) as archive:return {n:archive.read(n) for n in archive.namelist()}
def sheets(parts):
    rels={r.get('Id'):r.get('Target') for r in ET.fromstring(parts['xl/_rels/workbook.xml.rels'])}
    return {s.get('name'):('xl/'+rels[s.get('{'+helper.REL+'}id')]).replace('xl//xl/','xl/') for s in ET.fromstring(parts['xl/workbook.xml']).find(q('sheets'))}
original=package(source);candidate=package(output);updated=dict(original)
edits=json.loads((work/'changes.json').read_text(encoding='utf-8'));assert len(edits)==12
name=edits[0]['sheet'];target_path=sheets(original)[name]
tree=ET.fromstring(original[target_path]);old_cells={c.get('r'):c for c in tree.iter(q('c'))}
new_cells={c.get('r'):c for c in ET.fromstring(candidate[sheets(candidate)[name]]).iter(q('c'))}
strings=[''.join(si.itertext()) for si in ET.fromstring(candidate.get('xl/sharedStrings.xml',b'<sst/>'))]
for e in edits:
    old=old_cells[e['cell']];new=deepcopy(new_cells[e['cell']])
    if new.get('t') in ('s','str'):
        text=strings[int(new.find(q('v')).text)] if new.get('t')=='s' else new.find(q('v')).text
        new.remove(new.find(q('v')));new.set('t','inlineStr');ET.SubElement(ET.SubElement(new,q('is')),q('t')).text=text
    value=''.join(new.find(q('is')).itertext()) if new.get('t')=='inlineStr' else float(new.find(q('v')).text)
    assert value==e['after'],e
    if old.get('s') is not None:new.set('s',old.get('s'))
    else:new.attrib.pop('s',None)
    row=next(r for r in tree.find(q('sheetData')) if old in list(r));i=list(row).index(old);row.remove(old);row.insert(i,new)
updated[target_path]=helper.xml_bytes(tree,original[target_path])
targets={e['cell'] for e in edits};final={c.get('r'):c for c in ET.fromstring(updated[target_path]).iter(q('c'))}
assert final.keys()==old_cells.keys()
assert all(ET.tostring(c)==ET.tostring(final[address]) for address,c in old_cells.items() if address not in targets)
changed=[p for p in original if original[p]!=updated[p]];assert changed==[target_path]
temporary=source.with_suffix('.bonus.tmp.xlsx')
with ZipFile(source) as old,ZipFile(temporary,'w') as result:
    for info in old.infolist():result.writestr(copy(info),updated[info.filename])
temporary.replace(source);output.write_bytes(source.read_bytes())
(work/'preservation.json').write_text(json.dumps({'changedCells':sorted(targets),'changedParts':changed,'unrelatedCellsAndPartsPreserved':True},indent=2),encoding='utf-8')
print(json.dumps({'changedCells':sorted(targets),'changedParts':changed}))
