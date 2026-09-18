"""Read cached workbook values/formulas without rewriting its compact styles."""
import hashlib
import json
from pathlib import Path
import xml.etree.ElementTree as ET
from zipfile import ZipFile

root = Path(__file__).resolve().parent.parent
source = root/'Assets/ShooterSurvival/GameData/Editor/Data.xlsx'
ns = {'s':'http://schemas.openxmlformats.org/spreadsheetml/2006/main'}
rel = '{http://schemas.openxmlformats.org/officeDocument/2006/relationships}id'
result = {'sourceSha256': hashlib.sha256(source.read_bytes()).hexdigest(), 'sheets': {}}
with ZipFile(source) as archive:
    strings = []
    if 'xl/sharedStrings.xml' in archive.namelist():
        strings = [''.join(node.itertext()) for node in ET.fromstring(archive.read('xl/sharedStrings.xml'))]
    relationships = {node.attrib['Id']: node.attrib['Target'] for node in ET.fromstring(archive.read('xl/_rels/workbook.xml.rels'))}
    for sheet in ET.fromstring(archive.read('xl/workbook.xml')).findall('s:sheets/s:sheet', ns):
        target = relationships[sheet.attrib[rel]]
        path = target.lstrip('/') if target.startswith('/') else 'xl/'+target
        cells = {}
        for cell in ET.fromstring(archive.read(path)).findall('.//s:sheetData/s:row/s:c', ns):
            value = cell.find('s:v',ns)
            text = value.text if value is not None else None
            if cell.get('t') == 's': text = strings[int(text)]
            elif cell.get('t') == 'inlineStr': text = ''.join(cell.find('s:is',ns).itertext())
            elif text is not None:
                try: text = float(text); text = int(text) if text.is_integer() else text
                except ValueError: pass
            formula = cell.find('s:f',ns)
            cells[cell.attrib['r']] = {'value':text, **({'formula':formula.text} if formula is not None else {})}
        result['sheets'][sheet.attrib['name']] = cells
destination = root/'map-concepts/chapters-polish-2026-09-12/balance-source.json'
destination.write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf-8')
for name in ['환경 변수','밸런스 조정','업그레이드','특별 상점','커스터마이징']:
    cells=result['sheets'].get(name,{})
    rows=sorted({int(''.join(filter(str.isdigit,key))) for key in cells})
    chosen=rows if name in ['환경 변수','밸런스 조정'] else rows[:6]+rows[-3:]
    print(name,json.dumps({r:{k:v['value'] for k,v in cells.items() if int(''.join(filter(str.isdigit,k)))==r and v['value'] is not None} for r in chosen},ensure_ascii=False))
