import copy
import importlib.util
from pathlib import Path
import tempfile
import unittest
import xml.etree.ElementTree as ET
from zipfile import ZipFile


def load(name):
    spec = importlib.util.spec_from_file_location(name, Path(__file__).with_name(name + '.py'))
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


compact = load('compact-workbook-styles')
graft = load('append-encounter-workbook-sheets')
STYLE = f'''<styleSheet xmlns="{compact.NS}">
<numFmts count="2"><numFmt numFmtId="164" formatCode="0.00"/><numFmt numFmtId="199" formatCode="0.00"/></numFmts>
<fonts count="2"><font><name val="Arial"/></font><font><name val="Arial"/></font></fonts>
<fills count="2"><fill><patternFill patternType="none"/></fill><fill><patternFill patternType="none"/></fill></fills>
<borders count="2"><border/><border/></borders>
<cellStyleXfs count="2"><xf fontId="0" fillId="0" borderId="0" numFmtId="164"/><xf fontId="1" fillId="1" borderId="1" numFmtId="199"/></cellStyleXfs>
<cellXfs count="2"><xf fontId="0" fillId="0" borderId="0" numFmtId="164" xfId="0"/><xf fontId="1" fillId="1" borderId="1" numFmtId="199" xfId="1"/></cellXfs>
<cellStyles count="1"><cellStyle name="Normal" xfId="1" builtinId="0"/></cellStyles>
</styleSheet>'''.encode()
SHEET = f'''<worksheet xmlns="{compact.NS}"><cols><col min="1" max="2" style="1" width="14"/></cols><sheetData><row r="1" s="1" customFormat="1"><c r="A1" s="1"><f>2+2</f><v>4</v></c></row></sheetData><mergeCells count="1"><mergeCell ref="A2:B2"/></mergeCells></worksheet>'''.encode()


class StyleCompactionTests(unittest.TestCase):
    def test_package_preserves_formula_layout_and_effective_formats(self):
        with tempfile.TemporaryDirectory() as directory:
            source, result = Path(directory) / 'before.xlsx', Path(directory) / 'after.xlsx'
            with ZipFile(source, 'w') as archive:
                archive.writestr('xl/styles.xml', STYLE)
                archive.writestr('xl/worksheets/sheet1.xml', SHEET)
                archive.writestr('xl/workbook.xml', b'<keep-this-part/>')
            report = compact.compact(source, result)
            self.assertEqual(report['afterCounts']['cellXfs'], 1)
            with ZipFile(result) as archive:
                sheet = archive.read('xl/worksheets/sheet1.xml')
                self.assertIn(b's="0"', sheet)
                self.assertIn(b'style="0"', sheet)
                self.assertEqual(compact.without_style_ids(sheet), compact.without_style_ids(SHEET))
                self.assertEqual(archive.read('xl/workbook.xml'), b'<keep-this-part/>')
            second = Path(directory) / 'again.xlsx'
            compact.compact(result, second)
            with ZipFile(result) as a, ZipFile(second) as b:
                self.assertEqual({n:a.read(n) for n in a.namelist()}, {n:b.read(n) for n in b.namelist()})

    def test_repeated_grafts_do_not_multiply_existing_style_records(self):
        base = ET.fromstring(STYLE)
        compact.compact_styles(base)
        before = ET.tostring(base)
        for _ in range(20):
            maps = graft.merge_styles(base, ET.fromstring(STYLE))
            self.assertEqual(maps['cellXfs'], {0:0, 1:0})
        self.assertEqual(ET.tostring(base), before)

    def test_new_format_is_added_once_without_changing_old_indices(self):
        base = ET.fromstring(STYLE)
        compact.compact_styles(base)
        old = compact.effective_xfs(base)[0]
        incoming = copy.deepcopy(base)
        incoming.find(f'{{{compact.NS}}}numFmts')[0].set('formatCode', '0.0000')
        first = graft.merge_styles(base, incoming)
        second = graft.merge_styles(base, incoming)
        self.assertEqual(first, second)
        self.assertEqual(compact.effective_xfs(base)[0][:len(old)], old)
        self.assertNotEqual(first['cellXfs'][0], 0)


if __name__ == '__main__':
    unittest.main()
