from pathlib import Path
import zipfile
import xml.etree.ElementTree as E
import sys
sys.stdout.reconfigure(encoding='utf-8')

PATH = Path(__file__).resolve().parents[1] / 'Assets/ShooterSurvival/GameData/Editor/Data.xlsx'
NS = {'s': 'http://schemas.openxmlformats.org/spreadsheetml/2006/main'}

def sheets(archive):
    strings = [''.join(node.itertext()) for node in E.fromstring(archive.read('xl/sharedStrings.xml'))] if 'xl/sharedStrings.xml' in archive.namelist() else []
    rels = {r.attrib['Id']: r.attrib['Target'].lstrip('/') for r in E.fromstring(archive.read('xl/_rels/workbook.xml.rels'))}
    for sheet in E.fromstring(archive.read('xl/workbook.xml')).find('s:sheets', NS):
        target = rels[sheet.attrib['{http://schemas.openxmlformats.org/officeDocument/2006/relationships}id']]
        if not target.startswith('xl/'): target = 'xl/' + target
        tree = E.fromstring(archive.read(target))
        rows = []
        for row in tree.findall('s:sheetData/s:row', NS):
            cells = {}
            for cell in row:
                value = cell.find('s:v', NS)
                text = value.text if value is not None else ''.join(cell.find('s:is', NS).itertext()) if cell.find('s:is', NS) is not None else ''
                if cell.attrib.get('t') == 's': text = strings[int(text)]
                cells[cell.attrib['r']] = text
            rows.append(cells)
        yield sheet.attrib['name'], target, tree, rows

if __name__ == '__main__':
    with zipfile.ZipFile(PATH) as archive:
        for name, _, _, rows in sheets(archive):
            if name == '적 배치':
                print(name, rows[1]); print(rows[2:12]); print(rows[52:55]); print(rows[102:106])
            elif name == '환경 변수': print(name, rows)
