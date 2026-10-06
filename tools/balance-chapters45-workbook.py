from pathlib import Path
from zipfile import ZipFile, ZIP_DEFLATED
from io import BytesIO
from hashlib import sha256
import json,re,xml.etree.ElementTree as ET
ROOT=Path(r'D:\Tralalero Shooter\Tralalero Shooter D')
SOURCE=ROOT/'Assets/ShooterSurvival/GameData/Editor/Data.xlsx'
OUT=ROOT/'outputs/chapters45-2026-10-02/workbook'
VALUES={'c45_cityHealth':28,'c45_towerHealth':32,'c45_archiveHealth':40,'c45_targetHealth':40,'c45_captainHealth':224}
raw=SOURCE.read_bytes(); before=sha256(raw).hexdigest()
with ZipFile(BytesIO(raw)) as z: parts={n:z.read(n) for n in z.namelist()}
member='xl/worksheets/sheet7.xml';xml=parts[member].decode();ns={'s':'http://schemas.openxmlformats.org/spreadsheetml/2006/main'}
strings=[''.join(e.itertext()) for e in ET.fromstring(parts['xl/sharedStrings.xml'])]
changes=[]
for row in ET.fromstring(xml).find('s:sheetData',ns):
    cells={re.sub(r'\d','',c.get('r')):c for c in row}
    if 'B' not in cells:continue
    cell=cells['B'];v=cell.find('s:v',ns)
    key=strings[int(v.text)] if cell.get('t')=='s' else ''.join(cell.find('s:is',ns).itertext()) if cell.get('t')=='inlineStr' else v.text if v is not None else ''
    if key not in VALUES:continue
    ref=cells['D'].get('r'); old=cells['D'].find('s:v',ns).text
    xml,count=re.subn(r'(<c\b[^>]*\br="'+ref+r'"[^>]*>\s*<v>)[^<]+(</v>)',lambda m:m[1]+str(VALUES[key])+m[2],xml)
    assert count==1,(key,count)
    changes.append({'key':key,'before':float(old),'after':VALUES[key]})
assert len(changes)==len(VALUES)
parts[member]=xml.encode(); backup=OUT/f'before-balance-{before[:12]}.xlsx'
if not backup.exists():backup.write_bytes(raw)
receipt_path=OUT/'verification.json'
if receipt_path.exists():(OUT/'verification-before-balance.json').write_bytes(receipt_path.read_bytes())
with ZipFile(OUT/'Data.xlsx','w',ZIP_DEFLATED) as z:
    for n,data in parts.items():z.writestr(n,data)
receipt={'sourceSha256':before,'candidateSha256':sha256((OUT/'Data.xlsx').read_bytes()).hexdigest(),'changedMembers':[member],'changes':changes,'evidence':'ordinary HP60 attack8 fire1.6 run died at10s to HP180 contact; preserve existing remaining-health contact contract, tune chapter content to attainable shot counts'}
receipt_path.write_text(json.dumps(receipt,indent=2),encoding='utf-8');print(json.dumps(receipt))
