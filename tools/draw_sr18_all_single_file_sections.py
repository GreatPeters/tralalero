"""Complete the coordinate-native, single-file diagrams for all 16 SR18 sections."""
from pathlib import Path
from collections import Counter
import json
import math
from PIL import Image, ImageDraw
import draw_sr18_single_file_examples as base

ROOT = base.ROOT
OUT = ROOT / "tmp/image-previews/sr18-all-single-file-sections-2026-09-06"
RECORDS = ROOT / "map-concepts/sr18-all-single-file-sections-2026-09-06"
TITLES = ["입구 서행", "시작 상점가 북행", "첫 가로 상가", "짧은 북행 통로",
          "상단 연결길", "큰 루프 하행", "큰 루프 아랫길", "큰 루프 북행",
          "첫 고가", "중반 북행", "중반 가로 상가", "상단 고리 진입",
          "고리 윗변", "고리 짧은 하행", "두 번째 고가", "마지막 출구길"]
# A station is a single placement, never a crowd, paired wall, or extra lane.
# Distances follow the same authored route. Previous three examples remain anchored.
STATIONS = {
    0: [("enemy", 20)],
    1: [("bonus", 62)],
    2: [("object", 35), ("enemy", 105), ("bonus", 175)],
    3: [("object", 38)],
    4: [("enemy", 42)],
    5: [("bonus", 40), ("object", 105)],
    6: [("enemy", 25), ("object", 95), ("bonus", 165)],
    7: [],
    8: [("enemy", 90), ("enemy", 160), ("bonus", 270)],
    9: [("enemy", 65)],
    10: [("object", 48)],
    11: [("bonus", 35), ("enemy", 105)],
    12: [("object", 35), ("bonus", 99)],
    13: [("object", 51)],
    14: [("enemy", 48)],
    15: [],
}
NOTES = ["입구에는 하나만", "긴 직선 안의 단일 벽", "세 종류를 앞뒤로 분리", "짧은 길에는 하나만",
         "회전 사이에 하나만", "앞선 예시 + 앞뒤 남은 길", "가로길도 나란히 배치하지 않음", "앞선 예시 + 앞뒤 남은 길",
         "상부는 적 / 내려온 뒤 보너스", "코너를 지난 평지 한 곳", "짧은 가로길, 한 곳만", "교차점 전후를 피해서 배치",
         "오브젝트와 벽 사이 간격", "회전 전까지 여유 확보", "진입 전 적 / 고가 구간은 비움", "앞선 예시 + 출구까지 연결"]


def load_plan():
    sections = json.loads(base.FLOW.read_text(encoding="utf-8"))["sections"]
    shops = json.loads(base.SHOPS.read_text(encoding="utf-8"))["placements"]
    slopes = json.loads((ROOT / "map-concepts/sr18-slope-spots-2026-09-05/placement.json").read_text(encoding="utf-8"))["spots"]
    cumulative = 0
    for index, section in enumerate(sections):
        ax, _, az = section["start"]
        bx, _, bz = section["end"]
        section["length"] = math.hypot(bx-ax, bz-az)
        section["distance_start"] = cumulative
        section["title"] = TITLES[index]
        section["note"] = NOTES[index]
        section["stations"] = list(STATIONS[index])
        section["slope_zones"] = []
        section["direction"] = ("東 →" if bx>ax else "西 ←") if abs(bx-ax)>abs(bz-az) else ("북 ↑" if bz>az else "남 ↓")
        section["direction"] = section["direction"].replace("東", "동").replace("西", "서")
        cumulative += section["length"]
    for old in base.EXAMPLES:
        section = sections[old["section"]]
        for kind, z in old["events"]:
            section["stations"].append((kind, abs(z-section["start"][2])))
    for index, offset in [(8, 0), (14, 4)]:
        section = sections[index]
        distances = [math.hypot(s["world"][0]-section["start"][0], s["world"][2]-section["start"][2])
                     for s in slopes[offset:offset+4]]
        section["slope_zones"] = [(distances[0], distances[1], "오르막 · 비움"),
                                  (distances[1], distances[2], "상부 평지"),
                                  (distances[2], distances[3], "내리막 · 비움")]
    all_events = []
    for index, section in enumerate(sections):
        section["stations"].sort(key=lambda item: item[1])
        events = []
        for kind, distance in section["stations"]:
            assert 12 <= distance <= section["length"]-12, (section["id"], distance)
            for begin, end, name in section["slope_zones"]:
                if "비움" in name:
                    assert not begin-5 <= distance <= end+5, (section["id"], "ramp", distance)
            t = distance/section["length"]
            x = section["start"][0] + (section["end"][0]-section["start"][0])*t
            z = section["start"][2] + (section["end"][2]-section["start"][2])*t
            event = dict(kind=kind, distance=distance, world_xz=[x,z], section=section["id"],
                         route_distance=section["distance_start"]+distance)
            events.append(event)
            all_events.append(event)
        section["events"] = events
    for first, second in zip(all_events, all_events[1:]):
        assert second["route_distance"]-first["route_distance"]>=64, (first,second)
    assert len(sections)==16 and len(all_events)==32
    return sections, shops, all_events


def save(image, path):
    path.parent.mkdir(parents=True, exist_ok=True)
    if path.exists():
        raise FileExistsError(path)
    image.save(path)


def detail(d, section, rect, compact=False):
    left, top, right, bottom = rect
    width = right-left
    cx = left+width*.25
    length = section["length"]
    scale = min((bottom-top-100)/length, 8.0)
    road_width = base.ROAD_WIDTH*scale
    y0 = top + (bottom-top-length*scale)/2
    y1 = y0+length*scale
    base.road(d, (cx,y0), (cx,y1), scale, bool(section["slope_zones"]))
    label_x = left+width*.48
    size = 25 if compact else 29
    for distance in range(6, int(length)-5, 12):
        yy = y0+distance*scale
        for sign in [-1,1]:
            xx = cx+sign*(road_width/2+20)
            d.rectangle((xx-8, yy-12, xx+8, yy+12), fill="#7899ab")
            d.line([(xx-7,yy-5),(xx+7,yy-5)],fill="#f7faf5",width=3)
    for begin, end, label in section["slope_zones"]:
        if "비움" not in label:
            continue
        aa,bb=y0+begin*scale,y0+end*scale
        d.line([(cx-road_width/2-4,aa),(cx-road_width/2-4,bb)], fill="#c78436", width=6)
        base.text(d,(label_x,(aa+bb)/2),label,size-3,"#996224",anchor="lm")
    ys=[]
    for number,event in enumerate(section["events"],1):
        yy=y0+event["distance"]*scale
        base.symbol(d,event["kind"],cx,yy,road_width*.9)
        color=base.COLORS[event["kind"]]
        d.line([(cx+road_width*.6,yy),(label_x-12,yy)],fill=color,width=2)
        base.text(d,(label_x,yy),f"{number}. {base.NAMES[event['kind']]}",size,color,True,"lm")
        ys.append(yy)
    for first,second in zip(ys,ys[1:]):
        yy=(first+second)/2
        base.arrow(d,(cx,yy-18),(cx,yy+18),"#fff9de",3,10)
        # The bridge profile already labels the empty descent interval.
        if not section["slope_zones"]:
            base.text(d,(label_x,yy),"빈 길 확보",size-3,"#71838c",anchor="lm")
    if len(ys)==1:
        arrow_y=(ys[0]+y1)/2
        if y1-ys[0]>50:
            base.arrow(d,(cx,arrow_y-15),(cx,arrow_y+15),"#fff9de",3,10)
        base.text(d,(label_x,min(y1+55,bottom-15)),"한 곳만 배치",size-2,"#71838c",anchor="lm")
    base.text(d,(cx,y0-35),"진입",22,"#647982",anchor="mm")
    base.text(d,(cx,y1+35),"출구" if section["id"]=="S16" else "다음 구간",22,"#647982",anchor="mm")


def individual(index, sections, shops):
    section=sections[index]
    im=Image.new("RGB",(2000,1640),"#f8fbfc")
    d=ImageDraw.Draw(im)
    base.text(d,(54,35),f"{index+1:02d}  {section['title']}",52,bold=True)
    base.text(d,(56,110),"한 마리 폭 · 한 지점에 하나 · 앞뒤 간격 유지 · 실제 씬 미적용",30)
    base.text(d,(55,187),"현재 전체 맵",33,bold=True)
    base.text(d,(55,233),section["note"],25,"#617985")
    project=base.basemap(d,sections,shops)
    a=project(section["start"][0],section["start"][2])
    b=project(section["end"][0],section["end"][2])
    rect=(min(a[0],b[0])-17,min(a[1],b[1])-17,max(a[0],b[0])+17,max(a[1],b[1])+17)
    d.rounded_rectangle(rect,radius=7,outline="#8a477a",width=4)
    for number,event in enumerate(section["events"],1):
        x,y=project(*event["world_xz"])
        base.symbol(d,event["kind"],x,y,base.ROAD_WIDTH*1.52*.9)
        base.text(d,(x+25,y),str(number),24,base.COLORS[event["kind"]],True,"lm")
    d.line([(1222,186),(1222,1485)],fill="#d3dde1",width=2)
    base.text(d,(1270,187),"해당 구간 전체 확대",33,bold=True)
    base.text(d,(1270,236),f"실제 진행 {section['direction']}  /  아래 방향으로 돌려 표시",23)
    detail(d,section,(1270,320,1970,1405))
    base.text(d,(1270,1470),"코너와 경사에는 추가 배치를 몰지 않음",24,"#556b76")
    base.text(d,(55,1548),"입구부터 출구까지 구간 누락 없음 · 이전 3개 예시 유지 · 이 그림은 수량·5분 시간표 확정안이 아님",25,"#566a75")
    save(im,OUT/"sections"/f"{index+1:02d}-{section['id']}.png")


def group_page(group, sections):
    im=Image.new("RGB",(2400,2200),"#f8fbfc")
    d=ImageDraw.Draw(im)
    start=group*4
    base.text(d,(50,35),f"노량진 전체 배치 원칙  |  {start+1:02d}–{start+4:02d} 구간",56,bold=True)
    base.text(d,(52,116),"각 열은 아래로 진행 · 한 마리 폭 유지 · 적 / 벽 / 오브젝트는 떨어진 별도 지점",31)
    for col,index in enumerate(range(start,start+4)):
        s=sections[index]
        x=col*600
        if col:d.line([(x,198),(x,2040)],fill="#d3dde1",width=2)
        base.text(d,(x+32,210),f"{index+1:02d}  {s['title']}",34,bold=True)
        base.text(d,(x+32,263),f"실제 진행 {s['direction']}",26,"#617985")
        detail(d,s,(x+28,335,x+580,1930),True)
        # Wrap notes deliberately at a word boundary if they exceed a single line.
        words=s["note"].split()
        line=""; yy=1990
        for word in words:
            test=(line+" "+word).strip()
            if d.textlength(test,font=base.font(24))>535:
                base.text(d,(x+32,yy),line,24,"#617985");yy+=35;line=word
            else:line=test
        if line:base.text(d,(x+32,yy),line,24,"#617985")
    base.text(d,(52,2130),"주황 표시: 경사 비움 · 표시들은 모델이 아닌 위치 기호 · 간격 우선 검토도 · 씬 미적용",28,"#566a75")
    save(im,OUT/f"group-{group+1:02d}.png")


def overview(sections, shops, events):
    # Render the same code-native base into a new layout, not an edit of a screenshot.
    plot=Image.new("RGB",(2000,1640),"#f8fbfc")
    d=ImageDraw.Draw(plot)
    project=base.basemap(d,sections,shops)
    for event in events:
        x,y=project(*event["world_xz"])
        base.symbol(d,event["kind"],x,y,base.ROAD_WIDTH*1.52*.95)
    for s in sections:
        fraction=.52
        x=s["start"][0]+(s["end"][0]-s["start"][0])*fraction
        z=s["start"][2]+(s["end"][2]-s["start"][2])*fraction
        if any(math.hypot(x-e["world_xz"][0],z-e["world_xz"][1])<17 for e in s["events"]):continue
        a=project(x,z)
        dx=s["end"][0]-s["start"][0];dz=s["end"][2]-s["start"][2]
        b=(a[0]+dx/s["length"]*13,a[1]-dz/s["length"]*13)
        base.arrow(d,a,b,"#fff8df",2,5)
    im=Image.new("RGB",(2000,2260),"#f8fbfc")
    im.paste(plot.crop((38,284,1185,1460)).resize((1840,1887),Image.Resampling.LANCZOS),(80,245))
    d=ImageDraw.Draw(im)
    base.text(d,(65,35),"현재 맵 · 16구간 전체 위치도",53,bold=True)
    for x,kind in [(90,"enemy"),(660,"bonus"),(1250,"object")]:
        base.symbol(d,kind,x+15,158,42)
        base.text(d,(x+55,137),base.NAMES[kind],34,base.COLORS[kind],True)
    base.text(d,(65,2177),"한 줄 · 앞뒤 간격 우선 위치도 / 확정 수량·시간표 아님 / 실제 씬 미적용",30,"#566a75")
    save(im,OUT/"00-full-route.png")


def main():
    sections,shops,events=load_plan()
    for index in range(16):individual(index,sections,shops)
    for group in range(4):group_page(group,sections)
    overview(sections,shops,events)
    RECORDS.mkdir(parents=True,exist_ok=True)
    manifest=RECORDS/"coverage.json"
    if manifest.exists():raise FileExistsError(manifest)
    payload=dict(status="Diagram proposal only; Unity scene unchanged",section_count=16,
                 prior_example_sections=["S06","S08","S16"],all_previous_examples_retained=True,
                 station_count=len(events),counts=dict(Counter(e["kind"] for e in events)),
                 min_route_gap=min(b["route_distance"]-a["route_distance"] for a,b in zip(events,events[1:])),
                 sections=[{k:s[k] for k in ["id","title","length","events","slope_zones"]} for s in sections])
    manifest.write_text(json.dumps(payload,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
    print(json.dumps({"images":21,"sections":16,"stations":len(events),"counts":payload["counts"],"min_gap":payload["min_route_gap"],"output":str(OUT)},ensure_ascii=False))


if __name__=="__main__":main()
