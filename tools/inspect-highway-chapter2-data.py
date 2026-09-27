"""Read the current workbook without changing formula caches or sheet parts."""
import importlib.util
import json
from pathlib import Path
from xml.etree import ElementTree as ET

root = Path(__file__).resolve().parents[1]
spec = importlib.util.spec_from_file_location("preserve", root / "tools/prune-reststop-placements.py")
helper = importlib.util.module_from_spec(spec)
spec.loader.exec_module(helper)
parts = helper.package(helper.WORKBOOK)
strings = helper.shared_strings(parts)
report = {}
for name, path in helper.sheet_paths(parts).items():
    data = ET.fromstring(parts[path]).find(helper.q("sheetData"))
    rows = [[helper.cell_text(c, strings) for c in row] for row in data]
    if name in ("환경 변수", "밸런스 조정", "몬스터 성장", "적 배치", "보너스 배치", "기믹 배치", "캐릭터", "강화"):
        report[name] = rows if name not in ("적 배치", "보너스 배치", "기믹 배치") else rows[:2] + [r for r in rows[2:] if "HighWay" in r][:5]
output = root / "outputs/highway-chapter2-2026-09-27/workbook-before.json"
output.write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8")
print(json.dumps({"sheets": list(helper.sheet_paths(parts)), "report": str(output)}, ensure_ascii=False))
