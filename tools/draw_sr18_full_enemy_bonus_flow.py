"""Draw a new data-native planning chart, not an edit of a game screenshot."""
from __future__ import annotations

import json
import math
from collections import Counter
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "tmp/image-previews/sr18-full-enemy-bonus-flow-2026-09-06/final"
DATA = ROOT / "map-concepts/sr18-full-enemy-bonus-flow-2026-09-06"
BG, ROW, ROW_ALT = "#0d1727", "#142339", "#112035"
WHITE, MUTED, LINE = "#f3f6fa", "#adc0d4", "#31485f"
RED, GREEN, BLUE, PURPLE = "#ff8c8c", "#78e5b4", "#83bfff", "#dca3ff"
FONT_PATH = "C:/Windows/Fonts/malgun.ttf"
BOLD_PATH = "C:/Windows/Fonts/malgunbd.ttf"

# E = encounter; F = a manually placed reward point. Drops are derived from E counts.
SECTIONS = [
    ("입구 서행", "첫 적 확인", [(.48, [("노인", 1)], "공격 한 번")], []),
    ("시작 상점가 북행", "적 처치 → 드롭 학습", [(.62, [("그물", 2)], "앞뒤 순차 발동")], []),
    ("기존 가로 상가", "첫 선택 후 성장 압박", [(.20, [("노인", 2)], "공격 반복"), (.79, [("경비", 1), ("검", 2)], "발사 + 이동 후 공격")], [(.52, ["N", "N"], "첫 효과 비교")]),
    ("짧은 북행 통로", "가벼운 추격 압박", [(.48, [("검", 2)], "이동 후 공격")], []),
    ("상단 연결 상가", "호흡 · 다음 긴 구간 예고", [], []),
    ("큰 루프 긴 하행", "세 번의 전투와 중간 선택", [(.22, [("노인", 2), ("검", 1)], "전방 교대 압박"), (.45, [("경비", 1), ("그물", 2)], "발사 + 공격 반복"), (.80, [("뚱보", 1), ("검", 2)], "엘리트 포함 압박")], [(.62, ["N", "E"], "안전 / 욕심 비교")]),
    ("루프 바닥 가로길", "전투 사이 보상 회수", [(.20, [("검", 2), ("경비", 1)], "이동 + 발사"), (.77, [("노인", 2), ("경비", 1)], "전후 순차 발동")], [(.48, ["N"], "중간 보충")]),
    ("큰 루프 북행", "첫 큰 압박 구간", [(.17, [("그물", 2), ("경비", 1)], "전면 + 원거리"), (.42, [("뚱보", 1), ("검", 2), ("노인", 1)], "두 번으로 나눠 발동"), (.80, [("경비", 1), ("검", 2)], "회수 뒤 재압박")], [(.63, ["N", "E"], "고가 전 성장 선택")]),
    ("첫 고가 통과", "경사는 비우고 평지에서 전투", [(.34, [("노인", 2)], "상부 평지 단순 전투"), (.55, [("검", 2)], "같은 높이에서 공격")], [(.89, ["E"], "내리막 뒤 보상")]),
    ("중반 북행", "공격 순서 판단", [(.20, [("경비", 1), ("검", 2)], "발사 + 이동"), (.48, [("뚱보", 1), ("그물", 2)], "엘리트와 근접 조합")], [(.79, ["N", "E"], "빌드 보완")]),
    ("중반 가로 상가", "짧고 진한 한 편성", [(.50, [("경비", 2), ("검", 2)], "2명씩 순차 발동")], []),
    ("상단 고리 진입", "선택 후 다음 조합 시험", [(.18, [("노인", 2), ("경비", 1)], "공격 반복 + 발사"), (.78, [("뚱보", 1), ("검", 2)], "이동 후 공격")], [(.37, ["E", "E"], "후반 효과 비교")]),
    ("고리 윗변", "배운 역할 재조합", [(.48, [("경비", 1), ("검", 2), ("그물", 1)], "2명씩 교대 압박")], []),
    ("고리 짧은 하행", "전투 없이 다음 고가 준비", [], [(.50, ["E"], "준비 보상")]),
    ("두 번째 고가", "진입 전만 가볍게 압박", [(.19, [("경비", 1), ("검", 1)], "경사 전 발동·종료")], []),
    ("마지막 출구 북행", "마지막 조합 → 보스 → 보상", [(.17, [("뚱보", 1), ("검", 2)], "엘리트 포함 편성"), (.43, [("경비", 2), ("검", 2)], "두 번으로 나눠 발동"), (.70, [("여성 보스", 1)], "기존 보스 행동 사용")], [(.92, ["U"], "최종 돌파 보상")]),
]


def font(size: int, bold: bool = False):
    return ImageFont.truetype(BOLD_PATH if bold else FONT_PATH, size)


def text(draw, xy, value, size=28, fill=WHITE, bold=False, anchor=None):
    draw.text(xy, value, font=font(size, bold), fill=fill, anchor=anchor, spacing=5)


def arrow(draw, a, b, color=LINE, width=3, head=9):
    draw.line([a, b], fill=color, width=width)
    angle = math.atan2(b[1] - a[1], b[0] - a[0])
    points = [b, (b[0] - head * math.cos(angle - .5), b[1] - head * math.sin(angle - .5)),
              (b[0] - head * math.cos(angle + .5), b[1] - head * math.sin(angle + .5))]
    draw.polygon(points, fill=color)


def save(image, name):
    path = OUT / name
    if path.exists():
        raise FileExistsError(path)
    image.save(path)
    return path


def build_data():
    turns = json.loads((ROOT / "map-concepts/sr18-turn-spots-2026-09-05/placement.json").read_text(encoding="utf-8"))
    roads = json.loads((ROOT / "map-concepts/noryangjin-expansion-2026-09-02/sr18-unity-placement.json").read_text(encoding="utf-8"))
    points = [[27.09, 0, -115.11]] + [row["position"] for row in turns["spots"]] + [roads["roads"][-1]["world"]]
    lengths = [math.hypot(b[0]-a[0], b[2]-a[2]) for a, b in zip(points, points[1:])]
    total_length = sum(lengths)
    result, offset, e_id, f_id = [], 0., 0, 0
    enemy_types = Counter()
    for i, (name, intent, encounters, bonuses) in enumerate(SECTIONS):
        a, b = points[i], points[i+1]
        def locate(fraction):
            return [round(a[0]+(b[0]-a[0])*fraction, 3), round(a[2]+(b[2]-a[2])*fraction, 3)]
        row = dict(id=f"S{i+1:02}", name=name, intent=intent, start=a, end=b,
                   progress_start=round(offset/total_length*100, 1),
                   progress_end=round((offset+lengths[i])/total_length*100, 1), encounters=[], bonuses=[])
        for fraction, team, behavior in encounters:
            e_id += 1
            n = sum(count for _, count in team)
            enemy_types.update(dict(team))
            row["encounters"].append(dict(id=f"E{e_id:02}", fraction=fraction, team=team, count=n,
                                           behavior=behavior, drop_count=n, world_xz=locate(fraction)))
        for fraction, grades, purpose in bonuses:
            f_id += 1
            row["bonuses"].append(dict(id=f"F{f_id:02}", fraction=fraction, grades=grades,
                                       count=len(grades), purpose=purpose, world_xz=locate(fraction)))
        row["enemy_count"] = sum(e["count"] for e in row["encounters"])
        result.append(row)
        offset += lengths[i]
    assert len(result) == 16 and e_id == 25 and f_id == 9
    assert sum(enemy_types.values()) == 69
    assert sum(b["count"] for r in result for b in r["bonuses"]) == 14
    return dict(status="Proposal only; no Unity scene changes", roads=230, sections=result,
                enemy_groups=e_id, enemies=69, enemy_types=dict(enemy_types), fixed_bonus_locations=f_id,
                fixed_bonus_objects=14, maximum_enemy_drops=69, maximum_bonus_objects=83,
                sources=["map-concepts/sr18-turn-spots-2026-09-05/placement.json",
                         "map-concepts/noryangjin-expansion-2026-09-02/sr18-unity-placement.json",
                         "Assets/ShooterSurvival/Scripts/Game/AllStageEnemyStats.cs",
                         "Assets/ShooterSurvival/Scripts/Enemy/EnemyScript_space.cs"],
                notes=["Distance progress, not measured seconds; ignore editor 3x speed for planning.",
                       "Drops are created on enemy death, not necessarily at encounter entry.",
                       "Fixed bonus grades are proposed; effects remain random.",
                       "Side-by-side rewards do not imply an implemented exclusive-choice group.",
                       "No new enemy models or boss AI are proposed."])


def flow_chart(data, rows, filename, subtitle):
    width, row_height, top = 2600, 212, 350
    height = top + row_height*len(rows) + 235
    image = Image.new("RGB", (width, height), BG)
    d = ImageDraw.Draw(image)
    text(d, (64, 38), "SR18 전체 적군 · BonusWall 흐름", 61, bold=True)
    text(d, (67, 120), subtitle, 32, MUTED)
    text(d, (67, 176), "16구간  /  25전투 편성  /  적 69명  /  고정 보너스 9지점·14개", 34, bold=True)
    text(d, (67, 230), "위: 적 편성 →   아래: 처치 드롭 + 고정 보너스   |   각 줄을 읽고 다음 줄로 계속", 29, MUTED)
    for x, color, label in [(68, RED, "적 편성"), (345, GREEN, "처치 드롭"), (630, BLUE, "고정 BonusWall")]:
        d.rounded_rectangle((x, 291, x+24, 315), radius=5, fill=color)
        text(d, (x+39, 285), label, 27)
    text(d, (1030, 285), "N 일반   E 엘리트   U 유니크   ·   등급만 지정 / 효과는 랜덤", 27, MUTED)
    x0, x1 = 490, 2418
    for index, row in enumerate(rows):
        y = top + index*row_height
        d.rounded_rectangle((46, y, width-46, y+row_height-12), radius=15, fill=ROW if index%2==0 else ROW_ALT)
        text(d, (66, y+15), f"{row['id']}   {row['progress_start']:g}–{row['progress_end']:g}%", 32, bold=True)
        text(d, (66, y+66), row["name"], 28)
        text(d, (66, y+112), f"적 {row['enemy_count']}명 · 드롭 최대 {row['enemy_count']}", 25, GREEN)
        text(d, (66, y+156), row["intent"], 23, MUTED)
        d.line((423, y+16, 423, y+row_height-30), fill=LINE, width=2)
        arrow(d, (x0-38, y+61), (x1+55, y+61), LINE, 3)
        arrow(d, (x0-38, y+159), (x1+55, y+159), LINE, 2)
        for event in row["encounters"]:
            x = x0+(x1-x0)*event["fraction"]
            boss = any(name == "여성 보스" for name, _ in event["team"])
            elite = any(name == "뚱보" for name, _ in event["team"])
            edge = PURPLE if boss else "#ffc278" if elite else RED
            d.rounded_rectangle((x-170, y+10, x+170, y+114), radius=10, fill="#382836", outline=edge, width=2)
            text(d, (x-152, y+17), f"{event['id']} · {event['count']}명", 27, edge, True)
            team = [f"{name} {count}" for name, count in event["team"]]
            composition = " · ".join(team)
            size = 27 if d.textlength(composition, font=font(27)) <= 304 else 24
            assert d.textlength(composition, font=font(size)) <= 304, composition
            text(d, (x-152, y+56), composition, size)
            text(d, (x-152, y+89), event["behavior"], 18, MUTED)
            arrow(d, (x, y+116), (x, y+134), GREEN, 2, 6)
            d.rounded_rectangle((x-83, y+137, x+83, y+181), radius=9, fill="#1d423c", outline=GREEN, width=1)
            text(d, (x, y+159), f"드롭 ×{event['count']}", 25, GREEN, anchor="mm")
        for bonus in row["bonuses"]:
            x=x0+(x1-x0)*bonus["fraction"]
            color=PURPLE if "U" in bonus["grades"] else BLUE
            d.rounded_rectangle((x-166, y+123, x+166, y+190), radius=9, fill="#233752", outline=color, width=3)
            text(d, (x, y+139), f"{bonus['id']}  {' / '.join(bonus['grades'])}  ({bonus['count']}개)", 27, color, True, "mm")
            text(d, (x, y+172), bonus["purpose"], 22, WHITE, anchor="mm")
        if not row["encounters"]:
            text(d, ((x0+x1)//2, y+60), "새 적 편성 없음 · 호흡 구간", 30, MUTED, anchor="mm")
        if not row["bonuses"]:
            text(d, (x1+20, y+153), "고정 보너스 없음", 21, MUTED, anchor="rm")
    footer = top + row_height*len(rows) + 18
    text(d, (66, footer), "적을 전부 처치하면 드롭 69개 + 고정 14개 = 최대 83개 발생  ·  생성량과 실제 획득량은 다름", 30, GREEN)
    text(d, (66, footer+57), "기획 초안 · 씬 미적용 · 적 HP/피해량 미확정 · 0–100%는 경로 거리 기준이며 실측 시간표가 아님", 26, MUTED)
    text(d, (66, footer+108), "코너·경사 경계는 비우기 / 고정 보너스는 앞선 처치 드롭과 간격 확보 / 보스 처치 잠금 기능은 별도", 25, MUTED)
    save(image, filename)


def route_chart(data):
    width, height = 1800, 2380
    im = Image.new("RGB", (width, height), BG)
    d = ImageDraw.Draw(im)
    text(d, (60, 34), "전체 흐름을 현재 길에 대응", 53, bold=True)
    text(d, (60, 110), "E01–E25 전투 편성  ·  F01–F09 고정 BonusWall  ·  전체 흐름표와 같은 번호", 25, MUTED)
    def p(xz):
        return (290+(xz[0]+85)*2.48, 2120-(xz[1]+365)*2.48)
    for section in data["sections"]:
        a=p([section["start"][0],section["start"][2]])
        b=p([section["end"][0],section["end"][2]])
        d.line([a,b], fill="#718aa4", width=20)
        arrow(d, (a[0]+(b[0]-a[0])*.07,a[1]+(b[1]-a[1])*.07),
              (a[0]+(b[0]-a[0])*.12,a[1]+(b[1]-a[1])*.12), WHITE, 4, 11)
    for a,b in [((-55.75,-77.7511),(169.25,-77.7511)),((371.75,135.9989),(281.75,135.9989))]:
        d.line([p(a),p(b)], fill=BG, width=28)
        d.line([p(a),p(b)], fill="#64cbd0", width=17)
    for section in data["sections"]:
        vertical=abs(section["end"][2]-section["start"][2])>abs(section["end"][0]-section["start"][0])
        for event in section["encounters"]:
            x,y=p(event["world_xz"])
            d.ellipse((x-9,y-9,x+9,y+9),fill=RED)
            text(d,(x-20,y) if vertical else (x,y-20),event["id"],23,RED,True,"rm" if vertical else "mb")
        for bonus in section["bonuses"]:
            x,y=p(bonus["world_xz"])
            d.polygon([(x,y-12),(x+12,y),(x,y+12),(x-12,y)],fill=GREEN)
            text(d,(x+22,y) if vertical else (x,y+22),bonus["id"],25,GREEN,True,"lm" if vertical else "mt")
    start=data["sections"][0]["start"]
    end=data["sections"][-1]["end"]
    text(d,p([start[0]+8,start[2]-14]),"START",29,WHITE,True,"lm")
    text(d,p([end[0],end[2]+20]),"FINISH",29,WHITE,True,"mm")
    text(d,(60,2190),"회색 = 기존 길 중심선   청록 = 경사·고가 영향 구간",28,MUTED)
    text(d,(60,2240),"모든 전투·보너스 번호가 시작부터 마지막 출구까지 이어집니다.",29)
    text(d,(60,2290),"실제 좌표를 단순화한 기획 도식 / 기존 길 변경 없음 / 배치·보상 수치는 제안",25,MUTED)
    save(im,"04-full-route-locations.png")


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    DATA.mkdir(parents=True, exist_ok=True)
    data=build_data()
    flow_chart(data,data["sections"],"01-full-stage-flow.png","시작부터 마지막 출구까지 · 230개 길 전체 · 적군과 BonusWall만 표시")
    flow_chart(data,data["sections"][:8],"02-first-half-flow.png","크게 보기 1/2 · S01–S08 · 시작부터 큰 루프 북행까지")
    flow_chart(data,data["sections"][8:],"03-second-half-flow.png","크게 보기 2/2 · S09–S16 · 첫 고가부터 마지막 출구까지")
    route_chart(data)
    manifest=DATA/"full-flow-final.json"
    if manifest.exists():
        raise FileExistsError(manifest)
    manifest.write_text(json.dumps(data,ensure_ascii=False,indent=2),encoding="utf-8")
    print(json.dumps({k:data[k] for k in ["enemy_groups","enemies","enemy_types","fixed_bonus_locations","fixed_bonus_objects","maximum_enemy_drops","maximum_bonus_objects"]},ensure_ascii=False))
    print(OUT)


if __name__ == "__main__":
    main()
