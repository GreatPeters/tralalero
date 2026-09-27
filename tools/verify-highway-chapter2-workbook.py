"""Verify preserved workbook parts, formula caches and all non-HighWay placement rows."""
import importlib.util
import json
from pathlib import Path
from xml.etree import ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "outputs/highway-chapter2-2026-09-27"
spec = importlib.util.spec_from_file_location("edit", ROOT / "tools/apply-highway-chapter2-workbook.py")
edit = importlib.util.module_from_spec(spec)
spec.loader.exec_module(edit)
h = edit.h
before = h.package(OUT / "before/Data.xlsx")
after = h.package(h.WORKBOOK)
paths = h.sheet_paths(before)
owned = {paths[n] for n in ("환경 변수", "밸런스 조정", "적 배치", "보너스 배치", "기믹 배치")}
assert set(before) == set(after), "Package parts added/removed unexpectedly"
assert all(before[n] == after[n] for n in before if n not in owned), "Unrelated workbook part changed"
original_strings, current_strings = h.shared_strings(before), h.shared_strings(after)
preserved = 0
for name, path in paths.items():
    if path not in owned:
        continue
    old = ET.fromstring(before[path]).find(h.q("sheetData"))
    new = ET.fromstring(after[path]).find(h.q("sheetData"))
    new_rows = {r.get("r"): r for r in new}
    header = next((r for r in old if "배치ID" in edit.cells(r, original_strings).values()), None)
    columns = {} if header is None else {v: c for c, v in edit.cells(header, original_strings).items()}
    for row in old:
        if row is header:
            continue
        values = edit.cells(row, original_strings)
        if columns and values.get(columns["맵"]) == "HighWay":
            continue
        if (values.get("B") or "").startswith("hwy2_"):
            continue
        other = new_rows.get(row.get("r"))
        assert other is not None, (name, row.get("r"))
        assert ET.tostring(row) == ET.tostring(other), ("Unrelated row/formula cache changed", name, row.get("r"))
        preserved += 1
result = dict(passed=True, untouchedParts=sum(n not in owned for n in before), preservedRows=preserved)
(OUT / "workbook-preservation.json").write_text(json.dumps(result), encoding="utf-8")
print(json.dumps(result))
