"""Append chapter-specific settings without rewriting unrelated XLSX package parts."""
from pathlib import Path
from zipfile import ZipFile, ZIP_DEFLATED
from io import BytesIO
from hashlib import sha256
import json
import re
import xml.etree.ElementTree as ET

ROOT = Path(r'D:\Tralalero Shooter\Tralalero Shooter D')
SOURCE = ROOT / 'Assets/ShooterSurvival/GameData/Editor/Data.xlsx'
OUT = ROOT / 'outputs/chapters45-2026-10-02/workbook'
SETTINGS = {
    'playerSpeed_Jamsil': 7.8, 'playerSpeed_ShoeTower': 7,
    'progressCoinPerCheckpoint_Jamsil': 380, 'progressCoinPerCheckpoint_ShoeTower': 420,
    'firstClearJewels_Jamsil': 35, 'replayClearJewels_Jamsil': 5,
    'firstClearJewels_ShoeTower': 40, 'replayClearJewels_ShoeTower': 5,
    'coinPickupRadius_Jamsil': 3.5, 'coinPickupRadius_ShoeTower': 3.5,
    'c45_cityHealth': 145, 'c45_cityDamage': 25,
    'c45_towerHealth': 180, 'c45_towerDamage': 30,
    'c45_archiveHealth': 210, 'c45_archiveDamage': 30,
    'c45_targetHealth': 180, 'c45_captainHealth': 1250,
    'c45_warningSeconds': 1.6, 'c45_sweepSeconds': 1.3,
    'c45_hazardDamageFraction': .08, 'c45_liftSeconds': 6,
    'c45_shopCoins': 80, 'towerArchiveCoinReward': 80,
}

def main():
    OUT.mkdir(parents=True, exist_ok=True)
    original = SOURCE.read_bytes()
    before_hash = sha256(original).hexdigest()
    with ZipFile(BytesIO(original)) as z:
        parts = {name: z.read(name) for name in z.namelist()}
    member = 'xl/worksheets/sheet7.xml'
    text = parts[member].decode('utf-8')
    ns = {'s': 'http://schemas.openxmlformats.org/spreadsheetml/2006/main'}
    tree = ET.fromstring(text)
    strings = [''.join(e.itertext()) for e in ET.fromstring(parts['xl/sharedStrings.xml'])]
    def value(cell):
        if cell is None: return ''
        v=cell.find('s:v',ns)
        return strings[int(v.text)] if cell.get('t')=='s' else ''.join(cell.find('s:is',ns).itertext()) if cell.get('t')=='inlineStr' else v.text if v is not None else ''
    rows=list(tree.find('s:sheetData',ns))
    existing={value(next((c for c in row if c.get('r','').startswith('B')),None)):row for row in rows}
    new_rows=[]
    last=max(int(row.get('r')) for row in rows)
    for key,number in SETTINGS.items():
        if key in existing:
            row=existing[key];cell=next(c for c in row if c.get('r').startswith('D'))
            if float(value(cell))!=number: raise RuntimeError('Existing setting differs; review before changing '+key)
            continue
        last+=1
        new_rows.append(f'<row r="{last}" spans="2:4"><c r="B{last}" t="inlineStr"><is><t>{key}</t></is></c><c r="C{last}" t="inlineStr"><is><t>float</t></is></c><c r="D{last}"><v>{number}</v></c></row>')
    candidate=text.replace('</sheetData>',''.join(new_rows)+'</sheetData>')
    candidate=re.sub(r'<dimension ref="([A-Z]+\d+):([A-Z]+)\d+"',lambda m:f'<dimension ref="{m[1]}:{m[2]}{last}"',candidate,count=1)
    updated=dict(parts);updated[member]=candidate.encode('utf-8')
    changed=[n for n in parts if parts[n]!=updated[n]]
    assert set(changed)<={member}
    with ZipFile(OUT/'Data.xlsx','w',ZIP_DEFLATED) as z:
        for name,data in updated.items(): z.writestr(name,data)
    backup=OUT/f'before-{before_hash[:12]}.xlsx'
    if not backup.exists(): backup.write_bytes(original)
    receipt={'sourceSha256':before_hash,'candidateSha256':sha256((OUT/'Data.xlsx').read_bytes()).hexdigest(),'changedMembers':changed,'appendedRows':len(new_rows),'settings':SETTINGS}
    (OUT/'verification.json').write_text(json.dumps(receipt,indent=2),encoding='utf-8')
    print(json.dumps(receipt))

if __name__=='__main__': main()
