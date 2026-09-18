"""Read-only validation of the exact authored balance cells and package scope."""
import importlib.util
import json
from pathlib import Path
import zipfile

root=Path(__file__).resolve().parents[1]
spec=importlib.util.spec_from_file_location('workbook',root/'tools/inspect-combat-workbook.py')
w=importlib.util.module_from_spec(spec);spec.loader.exec_module(w)
before=root/'tmp/combat-feedback-2026-09-14/before/Data.xlsx'
changes=json.loads((root/'map-concepts/combat-feedback-2026-09-14/balance-authored.json').read_text(encoding='utf-8'))
allowed={(c['sheet'],c['cell']):c for c in changes}
with zipfile.ZipFile(before) as old,zipfile.ZipFile(w.PATH) as new:
    old_sheets=list(w.sheets(old));new_sheets=list(w.sheets(new))
    assert [s[0] for s in old_sheets]==[s[0] for s in new_sheets]
    changed_parts=[]
    assert old.namelist()==new.namelist()
    for name in old.namelist():
        if old.read(name)!=new.read(name):changed_parts.append(name)
    assert len(changed_parts)==3,changed_parts
    checked=0
    for (name,_,old_tree,old_rows),(_,_,new_tree,new_rows) in zip(old_sheets,new_sheets):
        old_values={k:v for r in old_rows for k,v in r.items()};new_values={k:v for r in new_rows for k,v in r.items()}
        assert old_values.keys()==new_values.keys()
        old_cells={c.attrib['r']:c for c in old_tree.findall('s:sheetData/s:row/s:c',w.NS)}
        new_cells={c.attrib['r']:c for c in new_tree.findall('s:sheetData/s:row/s:c',w.NS)}
        for cell,value in new_values.items():
            if (name,cell) in allowed:
                change=allowed[name,cell]
                assert abs(float(value)-change['after'])<.0001,(name,cell,value,change)
                formula=new_cells[cell].find('s:f',w.NS)
                actual=formula.text if formula is not None else ''
                assert (actual or '')==change['formulaAfter'].lstrip('='),(name,cell,actual,change)
                checked+=1
            else:
                assert value==old_values[cell],(name,cell)
                import xml.etree.ElementTree as E
                assert E.tostring(old_cells[cell])==E.tostring(new_cells[cell]),(name,cell)
            assert new_cells[cell].attrib==old_cells[cell].attrib,(name,cell)
    assert checked==len(changes)
summary={'verified_cells':checked,'unchanged_other_package_parts':len(old.namelist())-3,'changed_parts':changed_parts,
         'cosmetic_items':len([c for c in changes if c['sheet']=='커스터마이징' and c['cell'].startswith('F')]),
         'ranged_enemies':len([c for c in changes if c['sheet']=='적 배치' and c['cell'].startswith('N')])}
(root/'map-concepts/combat-feedback-2026-09-14/workbook-verification.json').write_text(json.dumps(summary,indent=2),encoding='utf-8')
print(json.dumps(summary))
