"""Surgical OOXML edits: retain unrelated entries, formulas, styling and drawings."""
import importlib.util
import json
from pathlib import Path
import re
import zipfile
import sys

root = Path(__file__).resolve().parents[1]
spec = importlib.util.spec_from_file_location('workbook', root / 'tools/inspect-combat-workbook.py')
workbook = importlib.util.module_from_spec(spec)
spec.loader.exec_module(workbook)
report_path = root / 'map-concepts/combat-feedback-2026-09-14/balance-changes.json'
report = []

def main():
    if report_path.exists(): raise RuntimeError('Already applied; retain prior balance evidence.')
    authored = None
    if '--proposal' not in sys.argv:
        authored = json.loads((report_path.parent / 'balance-authored.json').read_text(encoding='utf-8'))
    with zipfile.ZipFile(workbook.PATH) as source:
        updates = {}
        for name, path, _, rows in workbook.sheets(source):
            edits = {}
            def change(ref, value, multiplier=None):
                old = next(row[ref] for row in rows if ref in row)
                edits[ref] = (value, multiplier)
                report.append(dict(sheet=name, cell=ref, before=old, after=value, multiplier=multiplier))
            for row in rows:
                number = re.search(r'\d+', next(iter(row))).group()
                def get(column): return row.get(column + number, '')
                if name == '커스터마이징' and get('H') == '0':
                    change('F' + number, round(float(get('F')) * 10), 10)
                    change('K' + number, round(float(get('K')) * 4, 4), 4)
                if name == '적 배치' and '사격' in get('F'):
                    change('N' + number, round(float(get('N')) * .45, 2), .45)
                    change('K' + number, round(float(get('K')) * .85, 2), .85)
                    if get('D').endswith('_Right'):
                        change('I' + number, max(6, float(get('I')) - 7))
                if name == '환경 변수' and get('B').startswith('coinPickupRadius_'):
                    change('D' + number, 3.5)
            if not edits: continue
            if '--proposal' in sys.argv: continue
            xml = source.read(path)
            for ref, (value, multiplier) in edits.items():
                pattern = rb'(<(?:\w+:)?c\b[^>]*\br="' + ref.encode() + rb'"[^>]*>)(.*?)(</(?:\w+:)?c>)'
                def replace(match):
                    body = match[2]
                    change = next(c for c in authored if c['sheet'] == name and c['cell'] == ref)
                    formula = change['formulaAfter'].lstrip('=')
                    if change['formulaBefore']:
                        from xml.sax.saxutils import escape
                        body = re.sub(rb'(<(?:\w+:)?f\b[^>]*>).*?(</(?:\w+:)?f>)', lambda f: f[1] + escape(formula).encode() + f[2] if formula else b'', body)
                    body, count = re.subn(rb'(<(?:\w+:)?v>).*?(</(?:\w+:)?v>)', lambda v: v[1] + str(value).encode() + v[2], body)
                    if count != 1: raise RuntimeError('Missing numeric value: ' + ref)
                    return match[1] + body + match[3]
                xml, count = re.subn(pattern, replace, xml)
                if count != 1: raise RuntimeError('Missing cell: ' + ref)
            updates[path] = xml
        if '--proposal' in sys.argv:
            report_path.parent.mkdir(parents=True, exist_ok=True)
            (report_path.parent / 'balance-proposal.json').write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding='utf-8')
            print('Proposed cells: ' + str(len(report)))
            return
        target = workbook.PATH.with_suffix('.revised.xlsx')
        with zipfile.ZipFile(target, 'w', zipfile.ZIP_DEFLATED) as output:
            for entry in source.infolist(): output.writestr(entry, updates.get(entry.filename, source.read(entry)))
    target.replace(workbook.PATH)
    report_path.parent.mkdir(parents=True, exist_ok=True)
    report_path.write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding='utf-8')
    print(json.dumps({'changed_cells': len(report), 'sheets': sorted(set(item['sheet'] for item in report))}, ensure_ascii=False))

if __name__ == '__main__': main()
