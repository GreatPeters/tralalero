"""Large-type linear reading version of the unchanged full-stage proposal."""
from pathlib import Path
import json

from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "map-concepts/sr18-full-enemy-bonus-flow-2026-09-06/full-flow-final.json"
OUT = ROOT / "tmp/image-previews/sr18-full-enemy-bonus-flow-2026-09-06/easy-read"
FONT = "C:/Windows/Fonts/malgun.ttf"
BOLD = "C:/Windows/Fonts/malgunbd.ttf"
NAMES = {"검": "칼 적", "그물": "그물 적", "경비": "경비원"}
GRADES = {"N": "일반", "E": "엘리트", "U": "유니크"}
COLORS = {
    "전투": ("#fff0ed", "#eac4bc", "#972f26"),
    "보너스": ("#e9f7ef", "#a5ceb5", "#216143"),
    "보스": ("#f2eafa", "#ceb8e4", "#66358a"),
    "휴식": ("#edf1f5", "#c6d0da", "#4c6174"),
    "완주": ("#e6f2fe", "#a4c9eb", "#245681"),
}


def face(size, bold=False):
    return ImageFont.truetype(BOLD if bold else FONT, size)


def label(draw, xy, words, size, color="#202b37", bold=False, anchor=None):
    draw.text(xy, words, font=face(size, bold), fill=color, anchor=anchor)


def events(data):
    result = []
    for section in data["sections"]:
        steps = [(e["fraction"], "enemy", e) for e in section["encounters"]]
        steps += [(b["fraction"], "bonus", b) for b in section["bonuses"]]
        if not steps:
            result.append(dict(kind="휴식", area=section["name"], main="잠깐 쉬어가는 길", sub="새 적 없이 다음 긴 구간을 준비", source_id=section["id"]))
        for _, kind, event in sorted(steps, key=lambda item: item[0]):
            if kind == "enemy":
                main = " + ".join(f"{NAMES.get(name, name)} {count}명" for name, count in event["team"])
                category = "보스" if any(name == "여성 보스" for name, _ in event["team"]) else "전투"
                sub = event["behavior"]
            else:
                category = "보너스"
                grades = event["grades"]
                if len(grades) == 1:
                    main = f"{GRADES[grades[0]]} 보너스 1개"
                elif grades[0] == grades[1]:
                    main = f"{GRADES[grades[0]]} 보너스 2개 비교"
                else:
                    main = f"{GRADES[grades[0]]} / {GRADES[grades[1]]} 보너스 비교"
                sub = event["purpose"] + " · 효과는 랜덤"
            result.append(dict(kind=category, area=section["name"], main=main, sub=sub, source_id=event["id"]))
    result.append(dict(kind="완주", area="마지막 출구", main="출구 도착 · 스테이지 완주", sub="여기까지가 한 스테이지 전체 흐름", source_id="finish"))
    assert len(result) == 36
    assert sum(item["kind"] in ("전투", "보스") for item in result) == 25
    assert sum(item["kind"] == "보너스" for item in result) == 9
    return result


def draw_page(items, page):
    titles = ["시작 → 첫 선택 → 큰 루프", "큰 루프의 전투와 보상", "첫 고가 → 중반 상점가", "후반 → 마지막 보스 → 완주"]
    image = Image.new("RGB", (1200, 1800), "#fafbfd")
    draw = ImageDraw.Draw(image)
    label(draw, (48, 30), "적과 보너스, 이렇게 이어집니다", 53, bold=True)
    label(draw, (50, 105), f"{page + 1} / 4     {titles[page]}", 33, "#42576b", True)
    draw.rounded_rectangle((48, 164, 1152, 216), radius=12, fill="#eaf0f5")
    label(draw, (67, 174), "적 1명 처치 = BonusWall 1개     /     초록 칸 = 별도로 놓는 보너스", 26)

    for index, item in enumerate(items):
        y = 242 + index * 151
        fill, border, color = COLORS[item["kind"]]
        draw.rounded_rectangle((182, y, 1152, y + 137), radius=17, fill=fill, outline=border, width=2)
        draw.rounded_rectangle((48, y+40, 161, y+101), radius=14, fill=color)
        label(draw, (104, y+69), item["kind"], 28, "#ffffff", True, "mm")
        label(draw, (205, y+9), item["area"], 23, "#607184")
        main_size = 42
        while draw.textlength(item["main"], font=face(main_size, True)) > 920:
            main_size -= 1
        assert main_size >= 36
        label(draw, (205, y+43), item["main"], main_size, color, True)
        assert draw.textlength(item["sub"], font=face(25)) < 920
        label(draw, (205, y+101), item["sub"], 25, "#526577")
        if index < len(items)-1:
            draw.line((105, y+113, 105, y+149), fill="#8ba1b2", width=3)
            draw.polygon([(105,y+155),(98,y+145),(112,y+145)], fill="#8ba1b2")

    footer = "다음 장으로 이어집니다 →" if page < 3 else "전체: 적 69명 · 고정 보너스 9곳에 14개"
    label(draw, (51, 1637), footer, 35, bold=True)
    label(draw, (52, 1707), "기존 기획 내용 유지 · 검토용 초안 · 실제 맵에는 미적용", 25, "#5e7080")
    label(draw, (52, 1749), "나란한 보너스의 강제 1택 기능을 구현한 그림은 아닙니다.", 23, "#6b7b89")
    target = OUT / f"0{page+1}-easy-flow.png"
    if target.exists():
        raise FileExistsError(target)
    image.save(target)
    return str(target)


def main():
    data = json.loads(SOURCE.read_text(encoding="utf-8"))
    all_events = events(data)
    OUT.mkdir(parents=True, exist_ok=True)
    paths = [draw_page(all_events[i*9:(i+1)*9], i) for i in range(4)]
    print(json.dumps(dict(pages=paths,events=len(all_events),encounters=25,fixed_bonus_points=9),ensure_ascii=False))


if __name__ == "__main__":
    main()
