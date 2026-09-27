"""Remove RestStop placement rows whose 배치ID no longer exists in the scene (2026-09-26 restructure).

EncounterPlacementController aborts every data-driven placement for a scene when one row names a missing
object, so rows for deleted enemies/bonuses/props must leave the workbook with them.

Edits only the three placement sheet parts at XML level (rows removed, later rows renumbered); every other
part of Data.xlsx, including formula caches, stays byte-identical (same approach as apply-mobile-workbook.py).

Usage: py -3.11 tools/prune-reststop-placements.py <ids.tsv> [--apply]
ids.tsv: "<group>\t<object name>" lines exported from the saved RestStop scene.
"""
import importlib.util
import json
import pathlib
import re
import shutil
import sys
import xml.etree.ElementTree as ET
from zipfile import ZipFile

ROOT = pathlib.Path(__file__).resolve().parents[1]
WORKBOOK = ROOT / "Assets/ShooterSurvival/GameData/Editor/Data.xlsx"
SHEETS = {"적 배치": "Enemies", "보너스 배치": "Bonuses", "기믹 배치": "Props"}

spec = importlib.util.spec_from_file_location("preserve", ROOT / "tools/append-encounter-workbook-sheets.py")
helper = importlib.util.module_from_spec(spec)
spec.loader.exec_module(helper)
q = lambda name: "{" + helper.SS + "}" + name


def package(path):
    with ZipFile(path) as archive:
        return {n: archive.read(n) for n in archive.namelist()}


def sheet_paths(parts):
    rels = {r.get("Id"): r.get("Target") for r in ET.fromstring(parts["xl/_rels/workbook.xml.rels"])}
    return {s.get("name"): ("xl/" + rels[s.get("{" + helper.REL + "}id")]).replace("xl//xl/", "xl/")
            for s in ET.fromstring(parts["xl/workbook.xml"]).find(q("sheets"))}


def shared_strings(parts):
    root = ET.fromstring(parts["xl/sharedStrings.xml"])
    return ["".join(t.text or "" for t in si.iter(q("t"))) for si in root.findall(q("si"))]


def cell_text(cell, strings):
    v = cell.find(q("v"))
    if cell.get("t") == "s" and v is not None:
        return strings[int(v.text)]
    if cell.get("t") == "inlineStr":
        return "".join(t.text or "" for t in cell.iter(q("t")))
    return v.text if v is not None else None


def column_of(ref):
    return re.match(r"[A-Z]+", ref).group(0)


def main():
    ids_path = pathlib.Path(sys.argv[1])
    apply = "--apply" in sys.argv
    present = {}
    for line in ids_path.read_text(encoding="utf-8").splitlines():
        if "\t" in line:
            group, name = line.split("\t", 1)
            present.setdefault(group, set()).add(name.strip())
    parts = package(WORKBOOK)
    strings = shared_strings(parts)
    paths = sheet_paths(parts)
    updated = dict(parts)
    report = {}
    for sheet, group in SHEETS.items():
        tree = ET.fromstring(parts[paths[sheet]])
        data = tree.find(q("sheetData"))
        rows = list(data)
        header = {cell_text(c, strings): column_of(c.get("r")) for c in rows[1]}  # row 1 is the sheet note
        scene_col, id_col = header["맵"], header["배치ID"]
        doomed = []
        for row in rows[2:]:
            cells = {column_of(c.get("r")): c for c in row}
            scene = cell_text(cells[scene_col], strings) if scene_col in cells else None
            ident = (cell_text(cells[id_col], strings) or "").strip() if id_col in cells else ""
            if scene == "RestStop" and ident not in present.get(group, set()):
                doomed.append((row, ident))
        report[sheet] = [i for _, i in doomed]
        if not apply or not doomed:
            continue
        for row, _ in doomed:
            data.remove(row)
        for index, row in enumerate(list(data), start=1):
            old = int(row.get("r"))
            if old == index:
                continue
            row.set("r", str(index))
            for c in row:
                c.set("r", column_of(c.get("r")) + str(index))
            if row.get("spans") is None:
                pass
        dim = tree.find(q("dimension"))
        if dim is not None:
            ref = dim.get("ref")
            if ":" in ref:
                a, b = ref.split(":")
                dim.set("ref", a + ":" + column_of(b) + str(len(list(data))))
        updated[paths[sheet]] = helper.xml_bytes(tree, parts[paths[sheet]])
    print(json.dumps({k: [len(v), v[:6]] for k, v in report.items()}, ensure_ascii=False))
    if apply:
        backup = ids_path.parent / "Data.before-prune.xlsx"
        if not backup.exists():
            shutil.copy2(WORKBOOK, backup)
        changed = [p for p in parts if parts[p] != updated[p]]
        assert set(changed) <= {paths[s] for s in SHEETS}, changed
        temporary = WORKBOOK.with_suffix(".prune.tmp.xlsx")
        with ZipFile(WORKBOOK) as old, ZipFile(temporary, "w") as output:
            for info in old.infolist():
                output.writestr(info, updated[info.filename])
        temporary.replace(WORKBOOK)
        print("saved; changed parts", changed, "backup", backup)


if __name__ == "__main__":
    main()
