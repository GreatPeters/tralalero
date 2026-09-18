"""Install only artifact-authored cell edits; preserve every unrelated workbook part."""
from copy import deepcopy
from io import BytesIO
from pathlib import Path
from zipfile import ZipFile,ZIP_DEFLATED
import importlib.util,json,re,sys,xml.etree.ElementTree as ET

ROOT=Path(__file__).resolve().parents[1]
source=ROOT/'Assets/ShooterSurvival/GameData/Editor/Data.xlsx'
backup=ROOT/'tmp/backups/harbor-opening-refinement-2026-09-16/files/Assets/ShooterSurvival/GameData/Editor/Data.xlsx'
corner='--corner-fix' in sys.argv
if corner:backup=ROOT/'tmp/harbor-refinement-workbook/before-corner-fix.xlsx'
assert source.read_bytes()==backup.read_bytes(),'Workbook changed since task snapshot'
spec=importlib.util.spec_from_file_location('preserve',ROOT/'tools/append-encounter-workbook-sheets.py');helper=importlib.util.module_from_spec(spec);spec.loader.exec_module(helper)
q=lambda name:'{'+helper.SS+'}'+name
def package(path):
 with ZipFile(path) as archive:return {n:archive.read(n) for n in archive.namelist()}
original=package(source);candidate=package(ROOT/'tmp/harbor-refinement-workbook'/('corner-candidate.xlsx' if corner else 'candidate.xlsx'))
def sheets(parts):
 rels={r.get('Id'):r.get('Target') for r in ET.fromstring(parts['xl/_rels/workbook.xml.rels'])}
 return {s.get('name'):('xl/'+rels[s.get('{'+helper.REL+'}id')]).replace('xl//xl/','xl/') for s in ET.fromstring(parts['xl/workbook.xml']).find(q('sheets'))}
old_paths=sheets(original);new_paths=sheets(candidate)
strings=ET.fromstring(candidate['xl/sharedStrings.xml']) if 'xl/sharedStrings.xml' in candidate else []
changes=json.loads((ROOT/'tmp/harbor-refinement-workbook'/('corner-changes.json' if corner else 'changes.json')).read_text(encoding='utf8'))
updated=dict(original)
for name in {c['sheet'] for c in changes}:
 path=old_paths[name];tree=ET.fromstring(original[path]);data=tree.find(q('sheetData'))
 cells={c.get('r'):c for c in tree.iter(q('c'))};new_cells={c.get('r'):c for c in ET.fromstring(candidate[new_paths[name]]).iter(q('c'))}
 for change in (c for c in changes if c['sheet']==name):
  address=change['cell'];new=deepcopy(new_cells[address]);old=cells.get(address)
  if old is not None and old.get('s') is not None:new.set('s',old.get('s'))
  else:new.attrib.pop('s',None)
  if new.get('t')=='s':
   value=new.find(q('v'));inline=ET.SubElement(new,q('is'));inline.extend(deepcopy(list(strings[int(value.text)])));new.remove(value);new.set('t','inlineStr')
  row_number=re.search(r'\d+',address)[0];row=next(r for r in data if r.get('r')==row_number)
  if old is not None:index=list(row).index(old);row.remove(old);row.insert(index,new)
  else:row.append(new);row[:]=sorted(row,key=lambda c:(len(re.sub(r'\d','',c.get('r'))),re.sub(r'\d','',c.get('r'))))
  cells[address]=new
 updated[path]=helper.xml_bytes(tree,original[path])
assert all(updated[k]==original[k] for k in original if k not in {old_paths[c['sheet']] for c in changes})
temporary=source.with_suffix('.refinement.tmp.xlsx')
with ZipFile(temporary,'w',ZIP_DEFLATED) as archive:
 for name,data in updated.items():archive.writestr(name,data)
temporary.replace(source)
report={'changed_cells':len(changes),'changed_parts':sorted(k for k in original if updated[k]!=original[k]),'other_parts_identical':True}
(ROOT/'map-concepts/harbor-opening-refinement-2026-09-16'/('workbook-corner-preservation.json' if corner else 'workbook-preservation.json')).write_text(json.dumps(report,indent=2),encoding='utf8')
print(json.dumps(report))
