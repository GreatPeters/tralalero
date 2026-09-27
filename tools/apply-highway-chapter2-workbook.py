"""Seed chapter2 values and vehicle placements without re-exporting Data.xlsx.

Existing non-HighWay rows retain cell addresses and cached formula values.
After initial seeding, edit the workbook: reruns preserve owned setting values.
"""
import argparse
import importlib.util
import json
import math
from copy import deepcopy
from pathlib import Path
from xml.etree import ElementTree as ET
from zipfile import ZipFile

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "outputs/highway-chapter2-2026-09-27"
spec = importlib.util.spec_from_file_location("preserve", ROOT / "tools/prune-reststop-placements.py")
h = importlib.util.module_from_spec(spec)
spec.loader.exec_module(h)

# Migration seeds only. Runtime/installer read Data.xlsx, never this dictionary.
SEEDS = {
    "length": 2340, "runSpeed": 7.8, "laneWidth": 4.4, "sharkScale": .8, "cameraScale": 1.15,
    "medianWidth": .84, "medianHeight": 1, "medianShoulder": .6, "outerShoulder": 2,
    "dashPaint": 8, "dashGap": 12, "paintWidth": .15, "meshStep": 2, "meshSegment": 40,
    "forkAt": 468, "mergeAt": 1170, "branchOffset": 36, "tollAt": 1950, "tollMerge": 2320,
    "tollBranchOffset": 18, "popupAhead": 60, "popupSeconds": 5, "popupSlow": .2,
    "popupQuiet": 40, "separation": 23, "accidentQuietSeconds": 8,
    "hpSedan": 1, "hpTaxi": 1, "hpOneTon": 1.3, "hpBox": 1.6, "hpTanker": 2,
    "hpBus": 3, "hpPolice": 1.2, "hpTow": 1.2, "hpAccident": 2.5,
    "targetDps": 64, "healthSeconds": 3.8, "coinBase": 62, "jamCoin": 1.5, "openCoin": .8,
    "jamMinSpeed": 3, "jamMaxSpeed": 5, "jamGapMin": 20, "jamGapMax": 25,
    "openMinSpeed": 22, "openMaxSpeed": 26, "openMinSeconds": 3, "openMaxSeconds": 5,
    "normalSpeed": 12, "swarmSpeed": 18, "swarmCount": 8, "swarmSpacing": 14,
    "vehicleWidth": 3.4, "truckWidth": 3.7, "busWidth": 7.6, "followGap": 3,
    "spawnAhead": 82, "arrivalWindow": 1.5, "playerRadius": .7, "fastDamage": .25,
    "trafficVisibleAhead": 150, "trafficRecycleBehind": 24, "oppositeSpeed": 19,
    "oppositeGap": 28, "oppositeCount": 18,
    "crash1": 234, "crash2": 610, "crash3": 945, "crash4": 1248,
    "crashLead": 40, "crashTail": 60, "crashQueueGap": 18,
    "swarmStart": 1345, "swarmEnd": 1410,
    "logStart": 1470, "logEnd": 1545, "logTruckSpeed": 21, "logReleaseSeconds": 2,
    "logSeconds": 6, "logLength": 3.4, "logSpeed": 12, "logBounceHeight": 3.8, "logRadius": 1.6, "logDamage": .25,
    "deerAt": 1610, "deerLead": 30, "deerTail": 20, "deerDamage": .2,
    "deerMinSpeed": 3.2, "deerMaxSpeed": 5.5, "deerRadius": 1.5, "deerSpin": .8,
    "holeAt": 1690, "holeLead": 20, "holeTail": 20, "holeLane": 2,
    "workAt": 1790, "workLead": 30, "workTail": 50, "workLane": 2,
    "coneInterval": 2.4, "coneDamage": .08, "coneFlight": 1.1,
    "mystery1": 1440, "mystery2": 1735, "mysteryCount": 2,
    "largeChance": 20, "normalChance": 55, "coinChance": 20, "missChance": 5,
    "largePercent": 30, "normalPercent": 12, "mysteryCoins": 248, "missDamage": .05,
    "tankerRadius": 28, "chainRadius": 10, "chainDamage": .75, "chainBounces": 2,
    "shatterMultiplier": 2, "shieldThreshold": .15, "magnetRadius": 12, "magnetSpeed": 24,
    "uniqueCarry": 0, "fxMaximum": 3, "smokeSeconds": .8, "clearViewAhead": 12,
    "debrisSeconds": .8, "debrisSideSpeed": 16, "debrisUpSpeed": 12,
    "nearMissDistance": 3, "windSeconds": .35, "newsSeconds": 3,
    "entryAttLevel": 10, "entryHpLevel": 12, "entrySpeedLevel": 1,
    "clearAttLevel": 20, "clearHpLevel": 24, "clearSpeedLevel": 10,
    "normalBonusCount": 12, "normalBonusLane": 2.4,
    "normalBonus1": 85, "normalBonus2": 145, "normalBonus3": 330, "normalBonus4": 365,
    "normalBonus5": 520, "normalBonus6": 765, "normalBonus7": 835, "normalBonus8": 1110,
    "normalBonus9": 1328, "normalBonus10": 1865, "normalBonus11": 2130, "normalBonus12": 2260,
    "sideCarWidth": 3,
    "revealSeconds": 1.5, "vehicleGroundClearance": .08,
    "entryExtraCars": 8, "entryExtraInterval": 14,
    "guideWidth": .65, "guideAhead": 60, "guideAfter": 30,
    "branchTransition": 220,
    "trafficTarget": 351, "entryWaves": 6, "jamWaves": 61, "openWaves": 28, "tollWaves": 8,
    "openPlayerMultiplier": 1.5, "openVehicleSpeed": 5, "speedLineCount": 22,
    "healthLabelDistance": 115, "healthLabelPixels": 14,
    "tollPickupLead": 14, "tollRoofHide": 14,
    "waveHealthSeconds": 1.6, "swarmHealthSeconds": 1.2, "waveWeakCycle": 3,
    "rushHealthSeconds": 1.1,
}


def cells(row, strings):
    return {h.column_of(c.get("r")): h.cell_text(c, strings) for c in row}


def cell(ref, value, formula=None):
    c = ET.Element(h.q("c"), {"r": ref})
    if isinstance(value, (int, float)):
        if formula is not None:
            ET.SubElement(c, h.q("f")).text = formula
        ET.SubElement(c, h.q("v")).text = format(value, ".12g")
    else:
        c.set("t", "inlineStr")
        ET.SubElement(ET.SubElement(c, h.q("is")), h.q("t")).text = str(value)
    return c


def column(index):
    result = ""
    while index:
        index, digit = divmod(index - 1, 26)
        result = chr(65 + digit) + result
    return result


def append_row(data, values, row_number=None, formulas=None):
    number = row_number or max([0] + [int(r.get("r")) for r in data]) + 1
    row = ET.SubElement(data, h.q("row"), {"r": str(number)})
    for i, value in enumerate(values, 1):
        if value is not None:
            row.append(cell(column(i + 1) + str(number), value, (formulas or {}).get(i)))
    return row


def update_dimension(tree):
    data = tree.find(h.q("sheetData"))
    extent = max([1] + [int(r.get("r")) for r in data])
    dim = tree.find(h.q("dimension"))
    if dim is not None:
        dim.set("ref", "B1:Y" + str(extent))


def vehicle_rows(s, growth):
    result = []
    def add(kind, route, station, lane, spawn, speed, group=0, front=False):
        index = len(result) + 1
        # Reveal earlier at a proportionately farther station: the original arrival time stays unchanged.
        lead = min(s["revealSeconds"], max(0, spawn) / s["runSpeed"])
        station += speed * lead
        spawn -= s["runSpeed"] * lead
        station = round(station, 4)
        progress = max(0, min(1, station / s["length"]))
        factor = 1 if group == 1 else 1 + (growth[1] / growth[0] - 1) * progress ** growth[2]
        health_key = ("rushHealthSeconds" if route == "Open" else "waveHealthSeconds") if group >= 100 else "swarmHealthSeconds" if group == 6 else "healthSeconds"
        result.append(dict(id=f"HWY3_{index:03}_{kind}", kind=kind, route=route,
            station=round(station, 4), lane=lane, spawn=round(max(0, spawn), 4), speed=round(speed, 4),
            group=group, front=front, health_key=health_key, hp=s["targetDps"] * s[health_key] * s["hp" + kind] * factor,
            coins=round(s["coinBase"] * (2 if kind in ("Tanker", "Bus") else 1)), factor=factor))
    kinds = ["Sedan", "Taxi", "OneTon", "Box", "Sedan", "Tanker"]
    wave_id = 100
    def wave(route, start, speed, index):
        nonlocal wave_id
        wave_id += 1
        if route == "Jam" and index % 19 == 13:
            add("Taxi", route, start+s["spawnAhead"], 0, start, speed, wave_id)
            add("Bus", route, start+s["spawnAhead"], 1.5, start, speed, wave_id)
            add("OneTon", route, start+s["spawnAhead"]+s["swarmSpacing"], 2, start, speed, wave_id)
            return
        weak = (index // round(s["waveWeakCycle"])) % 3
        for lane in range(3):
            kind = ("Sedan" if index % 2 == 0 else "Taxi") if lane == weak else ["OneTon", "Box", "Tanker"][(index + lane) % 3]
            add(kind, route, start + s["spawnAhead"], lane, start, speed, wave_id)
    for i in range(round(s["entryWaves"])):
        wave("Common", i * 25, s["normalSpeed"], i)
    for i, start in enumerate((310, 335)):
        wave("Common", start, s["normalSpeed"], i + 6)
    for group, route in ((1, "Common"), (2, "Jam"), (3, "Jam"), (4, "Common")):
        d = s[f"crash{group}"]
        add("Accident", route, d, 1.5, d - s["trafficVisibleAhead"], 0, group, True)
        add("Taxi", route, d + 1, 0, d - s["trafficVisibleAhead"], 0, group, True)
        # The weak-side lane stays clear behind its front car: one kill really opens a passage.
        for j in range(4):
            kind = "Tanker" if group != 1 and j == 0 else kinds[j % len(kinds)]
            add(kind, route, d + (j // 2 + 1) * s["crashQueueGap"], 1 + j % 2,
                d - s["trafficVisibleAhead"], 0, group)
    for route in ("Jam", "Open"):
        count = round(s["jamWaves" if route == "Jam" else "openWaves"])
        for i in range(count):
            start = s["forkAt"] + 50 + i / max(1, count - 1) * (s["mergeAt"] - s["forkAt"] - 120)
            speed = (s["jamMinSpeed"] + (i % 3) / 2 * (s["jamMaxSpeed"] - s["jamMinSpeed"])) if route == "Jam" else s["openVehicleSpeed"]
            wave(route, start, speed, i)
    for i in range(round(s["swarmCount"])):
        lane = i % 3
        add("Tow" if i in (1, 2, 5) else "Police", "Common",
            s["swarmStart"] + s["spawnAhead"] + (i // 3) * s["swarmSpacing"] + (0 if lane == 1 else 3), lane,
            s["swarmStart"] - 18, s["swarmSpeed"], 6)
    for i in range(round(s["tollWaves"])):
        wave("Common", s["tollAt"] + 60 + i * 32, s["normalSpeed"], i)
    assert len(result) == round(s["trafficTarget"]), len(result)
    return result


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--apply", action="store_true")
    parser.add_argument("--set", action="append", default=[])
    args = parser.parse_args()
    overrides = dict(item.split("=", 1) for item in args.set)
    if set(overrides) - set(SEEDS):
        raise ValueError("Unknown setting override")
    parts = h.package(h.WORKBOOK)
    paths = h.sheet_paths(parts)
    strings = h.shared_strings(parts)
    changed = dict(parts)
    balance = ET.fromstring(parts[paths["밸런스 조정"]])
    balance_data = balance.find(h.q("sheetData"))
    settings, references = {}, {}
    existing = {cells(r, strings).get("B"): r for r in balance_data}
    for key, seed in SEEDS.items():
        full_key = "hwy2_" + key
        row = existing.get(full_key)
        if row is None:
            row = append_row(balance_data, [full_key, seed, "2챕터 전용 · 요청/검증 조정 값"])
        if key in overrides:
            reference = "C" + row.get("r")
            old = next(c for c in row if c.get("r") == reference)
            at = list(row).index(old); row.remove(old); row.insert(at, cell(reference, float(overrides[key])))
        value = float(cells(row, strings)["C"])
        if not math.isfinite(value) or value < 0:
            raise ValueError(f"Invalid chapter2 setting: {full_key}")
        settings[key] = value
        references[key] = "'밸런스 조정'!C" + row.get("r")
    update_dimension(balance)
    changed[paths["밸런스 조정"]] = h.helper.xml_bytes(balance, parts[paths["밸런스 조정"]])

    environment = ET.fromstring(parts[paths["환경 변수"]])
    env_data = environment.find(h.q("sheetData"))
    for row in list(env_data):
        if (cells(row, strings).get("B") or "").startswith("hwy2_"):
            env_data.remove(row)
    for key, value in settings.items():
        append_row(env_data, ["hwy2_" + key, "float", value, 0, 0], formulas={3: references[key]})
    update_dimension(environment)
    changed[paths["환경 변수"]] = h.helper.xml_bytes(environment, parts[paths["환경 변수"]])

    growth = ET.fromstring(parts[paths["몬스터 성장"]]).find(h.q("sheetData"))
    growth_row = next(r for r in growth if cells(r, strings).get("B") == "2" and cells(r, strings).get("C") == "Normal")
    g = cells(growth_row, strings)
    vehicles = vehicle_rows(settings, (float(g["F"]), float(g["G"]), float(g["H"])))
    enemy = ET.fromstring(parts[paths["적 배치"]])
    data = enemy.find(h.q("sheetData"))
    header = next(r for r in data if "배치ID" in cells(r, strings).values())
    columns = {value: col for col, value in cells(header, strings).items()}
    for row in list(data):
        if cells(row, strings).get(columns["맵"]) == "HighWay":
            data.remove(row)
    for i, title in enumerate(("차종", "차량경로", "차량거리", "차로번호", "등장거리", "사건ID", "사고앞줄"), 19):
        ref = column(i) + header.get("r")
        for old in list(header):
            if old.get("r") == ref:
                header.remove(old)
        header.append(cell(ref, title))
    sequence_base = max([0] + [int(cells(r, strings).get("B", "0") or "0") for r in list(data)[2:]])
    for index, v in enumerate(vehicles, 1):
        row_number = max(int(r.get("r")) for r in data) + 1
        multiplier = references["hp" + v["kind"]]
        growth_formula = "1" if v["group"] == 1 else f"(1+('몬스터 성장'!G{growth_row.get('r')}/'몬스터 성장'!F{growth_row.get('r')}-1)*(U{row_number}/{references['length']})^'몬스터 성장'!H{growth_row.get('r')})"
        health_formula = f"{references['targetDps']}*{references[v['health_key']]}*{multiplier}*{growth_formula}"
        append_row(data, [sequence_base + index, "HighWay", v["id"], 1, "공격 반복", v["speed"], 0, settings["trafficVisibleAhead"], 0, 0,
            "차량 적 · 원본 전투/성장/코인 시스템", "Normal", 0, v["hp"], 0, 0, v["coins"], v["kind"], v["route"],
            v["station"], v["lane"], v["spawn"], v["group"], int(v["front"])], row_number, {14: health_formula})
    update_dimension(enemy)
    changed[paths["적 배치"]] = h.helper.xml_bytes(enemy, parts[paths["적 배치"]])
    snapshot = json.loads((OUT / "before/scene-inspection.json").read_text(encoding="utf-8"))
    selected_bonuses = sorted(b["left"] for b in snapshot["bonuses"])[:round(settings["normalBonusCount"])]
    for sheet in ("보너스 배치", "기믹 배치"):
        tree = ET.fromstring(parts[paths[sheet]])
        sheet_data = tree.find(h.q("sheetData"))
        sheet_header = next(r for r in sheet_data if "배치ID" in cells(r, strings).values())
        columns = {value: col for col, value in cells(sheet_header, strings).items()}
        for row in sheet_data:
            values = cells(row, strings)
            if values.get(columns["맵"]) != "HighWay":
                continue
            enabled = sheet == "보너스 배치" and values.get(columns["배치ID"]) in selected_bonuses
            for title, value in (("사용", int(enabled)), ("등급", "Normal")):
                if title not in columns:
                    continue
                ref = columns[title] + row.get("r")
                old = next((c for c in row if c.get("r") == ref), None)
                if old is not None:
                    position = list(row).index(old)
                    row.remove(old)
                    row.insert(position, cell(ref, value))
        changed[paths[sheet]] = h.helper.xml_bytes(tree, parts[paths[sheet]])
    owned = {paths[name] for name in ("환경 변수", "밸런스 조정", "적 배치", "보너스 배치", "기믹 배치")}
    modified = [name for name in parts if changed[name] != parts[name]]
    assert set(modified) <= owned
    OUT.mkdir(parents=True, exist_ok=True)
    (OUT / "vehicle-plan.json").write_text(json.dumps(dict(settings=settings, vehicles=vehicles, modified=modified), ensure_ascii=False, indent=2), encoding="utf-8")
    if args.apply:
        if not (OUT / "before/Data.xlsx").exists():
            raise RuntimeError("Working-copy backup required")
        temp = h.WORKBOOK.with_suffix(".highway2.tmp.xlsx")
        with ZipFile(h.WORKBOOK) as old, ZipFile(temp, "w") as new:
            for entry in old.infolist():
                new.writestr(entry, changed[entry.filename])
        temp.replace(h.WORKBOOK)
        (OUT / "workbook-parts-verified.json").write_text(json.dumps({"changed": modified, "untouchedParts": len(parts) - len(modified), "vehicles": len(vehicles)}))
    print(json.dumps({"apply": args.apply, "vehicles": len(vehicles), "changed": modified}))


if __name__ == "__main__":
    main()
