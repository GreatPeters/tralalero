"""Stage reviewed changed sheets while preserving all unrelated workbook parts."""
from copy import deepcopy
from hashlib import sha256
from importlib.util import module_from_spec, spec_from_file_location
from pathlib import Path
from zipfile import ZipFile, ZIP_DEFLATED
import json
import xml.etree.ElementTree as ET
from io import BytesIO

work=Path('tmp/chapters-balance-20260913')
source=Path('Assets/ShooterSurvival/GameData/Editor/Data.xlsx')
plan=json.loads((work/'changes.json').read_text(encoding='utf-8'))
before=(work/'before.xlsx').read_bytes()
assert sha256(before).hexdigest()==plan['beforeHash'], 'Baseline hash changed'
allowed={plan['beforeHash']}
prior_path=Path('outputs/chapters-polish-2026-09-12/balance/verification.json')
if prior_path.exists():
    prior=json.loads(prior_path.read_text(encoding='utf-8'));allowed.update(prior.get('knownCandidateHashes',[]));allowed.add(prior['candidateSha256'])
assert sha256(source.read_bytes()).hexdigest() in allowed, 'Concurrent workbook edit'
spec=spec_from_file_location('workbook_helpers',Path(__file__).with_name('append-encounter-workbook-sheets.py'))
helper=module_from_spec(spec);spec.loader.exec_module(helper)
q=lambda name:'{'+helper.SS+'}'+name
with ZipFile(BytesIO(before)) as archive: original={name:archive.read(name) for name in archive.namelist()}
with ZipFile(work/'artifact-candidate.xlsx') as archive: candidate={name:archive.read(name) for name in archive.namelist()}
def parts(package):
    relations={r.get('Id'):r.get('Target') for r in ET.fromstring(package['xl/_rels/workbook.xml.rels'])}
    return {s.get('name'):(relations[s.get('{'+helper.REL+'}id')].lstrip('/') if relations[s.get('{'+helper.REL+'}id')].startswith('/') else 'xl/'+relations[s.get('{'+helper.REL+'}id')]) for s in ET.fromstring(package['xl/workbook.xml']).find(q('sheets'))}
old_parts,new_parts=parts(original),parts(candidate)
styles=ET.fromstring(original['xl/styles.xml']);incoming_styles=ET.fromstring(candidate['xl/styles.xml'])
mapping=helper.merge_styles(styles,incoming_styles)
updated=dict(original);updated['xl/styles.xml']=helper.xml_bytes(styles,original['xl/styles.xml'])
strings=ET.fromstring(candidate['xl/sharedStrings.xml']) if 'xl/sharedStrings.xml' in candidate else []
for name in plan['sheets']:
    raw=candidate[new_parts[name]];sheet=ET.fromstring(raw)
    for cell in sheet.iter(q('c')):
        if cell.get('t')=='s':
            value=cell.find(q('v'));inline=ET.SubElement(cell,q('is'));inline.extend(deepcopy(list(strings[int(value.text)])))
            cell.remove(value);cell.set('t','inlineStr')
        if cell.get('s') is not None:cell.set('s',str(mapping['cellXfs'][int(cell.get('s'))]))
    for row in sheet.iter(q('row')):
        if row.get('s') is not None:row.set('s',str(mapping['cellXfs'][int(row.get('s'))]))
    for column in sheet.iter(q('col')):
        if column.get('style') is not None:column.set('style',str(mapping['cellXfs'][int(column.get('style'))]))
    updated[old_parts[name]]=helper.xml_bytes(sheet,raw)
changed=[name for name in original if updated[name]!=original[name]]
assert set(changed)<={'xl/styles.xml',*(old_parts[name] for name in plan['sheets'])}
assert len(updated['xl/styles.xml'])<100000, 'Stylesheet growth needs review'
destination=Path('outputs/chapters-polish-2026-09-12/balance/Data.xlsx');destination.parent.mkdir(parents=True,exist_ok=True)
known=[]
if destination.with_name('verification.json').exists():
    prior=json.loads(destination.with_name('verification.json').read_text(encoding='utf-8'))
    known=list(dict.fromkeys(prior.get('knownCandidateHashes',[])+[prior['candidateSha256']]))
with ZipFile(destination,'w',ZIP_DEFLATED) as archive:
    for name,data in updated.items():archive.writestr(name,data)
report={'sourceSha256':plan['beforeHash'],'candidateSha256':sha256(destination.read_bytes()).hexdigest(),'knownCandidateHashes':known,
        'changedParts':changed,'unrelatedPartsPreserved':True,'stylesheetBytes':len(updated['xl/styles.xml']),
        'canonicalInstalled':False,'liveBalanceValidation':'pending','checks':plan['checks']}
destination.with_name('verification.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print(json.dumps({'candidate':str(destination),'stylesheetBytes':report['stylesheetBytes'],'unrelatedPartsPreserved':True,'installed':False}))
