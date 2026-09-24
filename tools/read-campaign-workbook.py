"""Read canonical workbook cells and cached formulas without modifying the archive."""
import json
from pathlib import Path
from zipfile import ZipFile
from xml.etree import ElementTree as ET

SOURCE = Path('Assets/ShooterSurvival/GameData/Editor/Data.xlsx')
NS = {'s': 'http://schemas.openxmlformats.org/spreadsheetml/2006/main'}

def read(source=SOURCE):
    result = {}
    with ZipFile(source) as z:
        strings = [''.join(n.itertext()) for n in ET.fromstring(z.read('xl/sharedStrings.xml'))] if 'xl/sharedStrings.xml' in z.namelist() else []
        rels = {n.get('Id'): n.get('Target') for n in ET.fromstring(z.read('xl/_rels/workbook.xml.rels'))}
        for sheet in ET.fromstring(z.read('xl/workbook.xml')).findall('s:sheets/s:sheet', NS):
            path = rels[sheet.get('{http://schemas.openxmlformats.org/officeDocument/2006/relationships}id')]
            path = path.lstrip('/') if path.startswith('/') else 'xl/' + path
            cells = {}
            for c in ET.fromstring(z.read(path)).findall('.//s:sheetData/s:row/s:c', NS):
                v, f = c.find('s:v', NS), c.find('s:f', NS)
                value = v.text if v is not None else None
                if c.get('t') == 's': value = strings[int(value)]
                elif c.get('t') == 'inlineStr': value = ''.join(c.find('s:is', NS).itertext())
                elif value is not None:
                    try: value = float(value)
                    except ValueError: pass
                cells[c.get('r')] = {'value': value, 'formula': f.text if f is not None else None}
            result[sheet.get('name')] = cells
    return result

if __name__ == '__main__':
    import sys
    sys.stdout.reconfigure(encoding='utf-8')
    data = read()
    output = Path(sys.argv[1])
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text(json.dumps(data, ensure_ascii=False, indent=2), encoding='utf-8')
    for name in ['밸런스 조정', '환경 변수', '캐릭터', '업그레이드', '특별 상점', '커스터마이징', '적 배치', '기믹 배치']:
        cells = data.get(name, {})
        rows = sorted({int(''.join(filter(str.isdigit, c))) for c in cells})
        if name not in ['밸런스 조정', '환경 변수']: rows = rows[:5]
        print(name)
        for r in rows:
            print(r, {c: v['value'] for c, v in cells.items() if int(''.join(filter(str.isdigit,c))) == r and v['value'] is not None})
