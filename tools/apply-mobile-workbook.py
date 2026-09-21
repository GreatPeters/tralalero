"""Install requested artifact-authored stats, preserving unrelated formula caches."""
from copy import deepcopy
from pathlib import Path
from zipfile import ZipFile
import importlib.util
import json
import re
import sys
import xml.etree.ElementTree as ET

root=Path(__file__).resolve().parents[1]
work=root/'tmp/mobile-feedback-2026-09-20/workbook'
source=root/'Assets/ShooterSurvival/GameData/Editor/Data.xlsx'
fatman='--fatman' in sys.argv
assert source.read_bytes()==(work/('before-damage.xlsx' if fatman else 'before.xlsx')).read_bytes(), 'Source changed since the snapshot'
spec=importlib.util.spec_from_file_location('preserve',root/'tools/append-encounter-workbook-sheets.py')
helper=importlib.util.module_from_spec(spec);spec.loader.exec_module(helper)
q=lambda name:'{'+helper.SS+'}'+name
def package(path):
 with ZipFile(path) as archive:return {n:archive.read(n) for n in archive.namelist()}
def sheets(parts):
 rels={r.get('Id'):r.get('Target') for r in ET.fromstring(parts['xl/_rels/workbook.xml.rels'])}
 return {s.get('name'):('xl/'+rels[s.get('{'+helper.REL+'}id')]).replace('xl//xl/','xl/') for s in ET.fromstring(parts['xl/workbook.xml']).find(q('sheets'))}
original=package(source);candidate=package(root/'outputs/mobile-feedback-2026-09-20/Data.xlsx')
old_paths=sheets(original);new_paths=sheets(candidate);updated=dict(original)
edits=json.loads((work/('fatman-changes.json' if fatman else 'changes.json')).read_text(encoding='utf-8'))
assert len(edits)>0 and (fatman or len(edits)==2)
for name in {e['sheet'] for e in edits}:
 path=old_paths[name];tree=ET.fromstring(original[path]);rows=tree.find(q('sheetData'))
 cells={c.get('r'):c for c in tree.iter(q('c'))};new_cells={c.get('r'):c for c in ET.fromstring(candidate[new_paths[name]]).iter(q('c'))}
 for edit in edits:
  if edit['sheet']!=name:continue
  address=edit['cell'];old=cells[address];new=deepcopy(new_cells[address]);assert new.get('t')!='s'
  if old.get('s') is not None:new.set('s',old.get('s'))
  else:new.attrib.pop('s',None)
  row=next(r for r in rows if r.get('r')==re.search(r'\d+',address)[0]);index=list(row).index(old);row.remove(old);row.insert(index,new)
 updated[path]=helper.xml_bytes(tree,original[path])
 target_addresses={e['cell'] for e in edits if e['sheet']==name}
 final_cells={c.get('r'):c for c in ET.fromstring(updated[path]).iter(q('c'))}
 original_cells={c.get('r'):c for c in ET.fromstring(original[path]).iter(q('c'))}
 assert final_cells.keys()==original_cells.keys()
 for address,old in original_cells.items():
  if address not in target_addresses:
   assert ET.tostring(old)==ET.tostring(final_cells[address]), 'Unrequested cell changed: '+address
  elif fatman:
   assert float(final_cells[address].find(q('v')).text)==100
changed=[p for p in original if original[p]!=updated[p]]
assert changed==[old_paths[edits[0]['sheet']]]
temporary=source.with_suffix('.mobile.tmp.xlsx')
with ZipFile(source) as old,ZipFile(temporary,'w') as output:
 for info in old.infolist():output.writestr(info,updated[info.filename])
temporary.replace(source)
(work/('fatman-preservation.json' if fatman else 'preservation.json')).write_text(json.dumps({'edits':edits,'changedParts':changed,'otherPartsIdentical':True},indent=2),encoding='utf-8')
print(json.dumps({'changedCells':[e['cell'] for e in edits],'changedParts':changed}))
