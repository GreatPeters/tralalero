"""Preserve all original XLSX parts except authored cells and dependent formula caches.

Artifact Tool owns authoring/calculation. This installer preserves the project's
existing compact styles, formulas, relationships and unrelated Excel caches.
"""
from copy import copy, deepcopy
from hashlib import sha256
from pathlib import Path
import importlib.util
import json
import re
import sys
from xml.etree import ElementTree as ET
from zipfile import ZipFile

sys.stdout.reconfigure(encoding='utf-8')
config=json.loads(Path(sys.argv[1]).read_text(encoding='utf-8'))
base=Path('tmp/campaign-balance-2026-09-23/workbook/before.xlsx')
out=Path('outputs/campaign-balance-2026-09-23')/config['revision']
spec=importlib.util.spec_from_file_location('preserve',Path(__file__).with_name('append-encounter-workbook-sheets.py'))
helper=importlib.util.module_from_spec(spec);spec.loader.exec_module(helper)
q=lambda x:'{'+helper.SS+'}'+x

def package(path):
    with ZipFile(path) as z:return {n:z.read(n) for n in z.namelist()}

def sheets(parts):
    rels={r.get('Id'):r.get('Target') for r in ET.fromstring(parts['xl/_rels/workbook.xml.rels'])}
    result={}
    for s in ET.fromstring(parts['xl/workbook.xml']).find(q('sheets')):
        target=rels[s.get('{'+helper.REL+'}id')]
        result[s.get('name')]=target.lstrip('/') if target.startswith('/') else 'xl/'+target
    return result

original=package(base);candidate=package(out/'artifact.xlsx');parts=sheets(original);newparts=sheets(candidate)
trees={s:ET.fromstring(original[p]) for s,p in parts.items()}
cells={(s,c.get('r')):c for s,t in trees.items() for c in t.iter(q('c'))}
newcells={(s,c.get('r')):c for s,p in newparts.items() for c in ET.fromstring(candidate[p]).iter(q('c'))}
strings=[''.join(n.itertext()) for n in ET.fromstring(candidate.get('xl/sharedStrings.xml',b'<sst/>'))]
explicit={(e['sheet'],e['cell']) for e in config['edits']}
affected=set(explicit)
reference=re.compile(r"(?:(?:'([^']+)'|([A-Za-z_가-힣][A-Za-z_가-힣 ]*))!)?(\$?[A-Z]{1,3}\$?\d+)(?::(\$?[A-Z]{1,3}\$?\d+))?")

def coordinate(cell):
    c,r=re.match(r'([A-Z]+)(\d+)',cell.replace('$','')).groups();col=0
    for letter in c:col=col*26+ord(letter)-64
    return col,int(r)

masters={(s,c.find(q('f')).get('si')):(a,c.find(q('f')).text) for (s,a),c in cells.items()
         if c.find(q('f')) is not None and c.find(q('f')).get('t')=='shared' and c.find(q('f')).text}

def formula_text(key, cell):
    f=cell.find(q('f'))
    if f is None:return ''
    if f.text:return f.text
    address,expression=masters[(key[0],f.get('si'))]
    if '"' in expression:raise ValueError('Quoted shared formulas need a token-aware translator')
    start,end=coordinate(address),coordinate(key[1]);dc,dr=end[0]-start[0],end[1]-start[1]
    def shift(match):
        absolute_col,col,absolute_row,row=match.groups()
        if not absolute_col:
            index=coordinate(col+'1')[0]+dc;col=''
            while index:index,remainder=divmod(index-1,26);col=chr(65+remainder)+col
        return absolute_col+col+absolute_row+str(int(row)+(0 if absolute_row else dr))
    return re.sub(r'(\$?)([A-Z]{1,3})(\$?)(\d+)',shift,expression)

def references(formula,sheet,changed):
    for quoted,plain,start,end in reference.findall(formula):
        source=quoted or plain or sheet
        if not end:
            if (source,start.replace('$','')) in changed:return True
        else:
            a,b=coordinate(start),coordinate(end)
            for name,cell in changed:
                if name!=source:continue
                c=coordinate(cell)
                if a[0]<=c[0]<=b[0] and a[1]<=c[1]<=b[1]:return True
    return False

while True:
    added={key for key,c in cells.items() if key not in affected and references(formula_text(key,c),key[0],affected)}
    if not added:break
    affected.update(added)

for key in sorted(affected):
    if key not in cells or key not in newcells:raise ValueError(f'Expected existing authoring cell {key}')
    old=cells[key];new=deepcopy(newcells[key]);value=new.find(q('v'))
    if new.get('t')=='e':raise ValueError(f'Formula error at {key}: {value.text}')
    if new.get('t') in ('s','str'):
        text=strings[int(value.text)] if new.get('t')=='s' else value.text
        new.remove(value);new.set('t','inlineStr');ET.SubElement(ET.SubElement(new,q('is')),q('t')).text=text
    if key not in explicit:
        # Preserve the exact native formula; only its evaluated cache may change.
        if new.find(q('f')) is not None:new.remove(new.find(q('f')))
        new.insert(0,deepcopy(old.find(q('f'))))
    if old.get('s') is None:new.attrib.pop('s',None)
    else:new.set('s',old.get('s'))
    row=next(r for r in trees[key[0]].find(q('sheetData')) if old in list(r))
    i=list(row).index(old);row.remove(old);row.insert(i,new)

updated=dict(original)
for name in {s for s,c in affected}:updated[parts[name]]=helper.xml_bytes(trees[name],original[parts[name]])
finalcells={(s,c.get('r')):c for s,p in parts.items() for c in ET.fromstring(updated[p]).iter(q('c'))}
assert finalcells.keys()==cells.keys()
assert all(ET.tostring(c)==ET.tostring(finalcells[k]) for k,c in cells.items() if k not in affected)
with ZipFile(base) as source,ZipFile(out/'Data.xlsx','w') as target:
    for info in source.infolist():target.writestr(copy(info),updated[info.filename])
report={'sourceSha256':sha256(base.read_bytes()).hexdigest(),'candidateSha256':sha256((out/'Data.xlsx').read_bytes()).hexdigest(),
        'authoredCells':[list(k) for k in sorted(explicit)],'dependentCaches':[list(k) for k in sorted(affected-explicit)],
        'changedParts':[p for p in original if original[p]!=updated[p]],'unrelatedCellsAndPartsPreserved':True}
(out/'verification.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps({k:v for k,v in report.items() if k not in ('authoredCells','dependentCaches')},ensure_ascii=False))
