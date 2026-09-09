"""Coordinate-native diagrams on a new canvas; never edits screenshots or Unity scenes."""
from pathlib import Path
import json
import math
from functools import lru_cache
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[1]
FLOW = ROOT / "map-concepts/sr18-full-enemy-bonus-flow-2026-09-06/full-flow-final.json"
SHOPS = ROOT / "map-concepts/sr18-roadside-market-2026-09-05/applied-layout.json"
OUT = ROOT / "tmp/image-previews/sr18-single-file-examples-2026-09-06/final"
W, H = 2000, 1640
ROAD_WIDTH = 8.36
INK = "#173342"
COLORS = {"enemy": "#bd4638", "bonus": "#cb951a", "object": "#2476aa"}
NAMES = {"enemy": "적 한 마리", "bonus": "BonusWall 한 개", "object": "오브젝트 한 개"}
EXAMPLES = [
    dict(slug="01-loop-down", title="01  큰 루프 하행", section=5, x=124.32,
         low=-295, high=-105, direction=-1,
         events=[("enemy", -130), ("bonus", -200), ("object", -270)]),
    dict(slug="02-loop-up", title="02  큰 루프 북행", section=7, x=-67,
         low=-305, high=-125, direction=1,
         events=[("object", -285), ("enemy", -215), ("bonus", -145)]),
    dict(slug="03-final-straight", title="03  마지막 직선", section=15, x=248,
         low=150, high=320, direction=1,
         events=[("bonus", 169), ("enemy", 233), ("object", 297)]),
]


@lru_cache(None)
def font(size, bold=False):
    return ImageFont.truetype("C:/Windows/Fonts/malgunbd.ttf" if bold else "C:/Windows/Fonts/malgun.ttf", size)


def text(d, point, words, size=30, color=INK, bold=False, anchor=None):
    d.text(point, words, font=font(size, bold), fill=color, anchor=anchor)


def arrow(d, a, b, color, width=3, head=12):
    d.line([a, b], fill=color, width=width)
    vx, vy = b[0] - a[0], b[1] - a[1]
    norm = math.hypot(vx, vy)
    vx, vy = vx / norm, vy / norm
    d.polygon([b, (b[0]-vx*head-vy*head*.5, b[1]-vy*head+vx*head*.5),
               (b[0]-vx*head+vy*head*.5, b[1]-vy*head-vx*head*.5)], fill=color)


def nearest_axis(x, z, sections):
    best = (float("inf"), False)
    for s in sections:
        ax, _, az = s["start"]
        bx, _, bz = s["end"]
        dx, dz = bx-ax, bz-az
        t = max(0, min(1, ((x-ax)*dx+(z-az)*dz)/(dx*dx+dz*dz)))
        dist = (x-ax-t*dx)**2+(z-az-t*dz)**2
        if dist < best[0]:
            best = (dist, abs(dz) > abs(dx))
    return best[1]


def road(d, a, b, scale, bridge=False):
    width = max(3, round(ROAD_WIDTH*scale))
    if bridge:
        d.line([a, b], fill="#617a83", width=width+6)
    d.line([a, b], fill="#775335", width=width)
    d.line([a, b], fill="#c19c6d", width=max(1, width-3))
    vx, vy = b[0]-a[0], b[1]-a[1]
    length = math.hypot(vx, vy)
    ux, uy = vx/length, vy/length
    for step in range(1, int(length/max(4, 1.8*scale))):
        distance = step*max(4, 1.8*scale)
        cx, cy = a[0]+ux*distance, a[1]+uy*distance
        d.line([(cx-uy*(width-3)/2, cy+ux*(width-3)/2),
                (cx+uy*(width-3)/2, cy-ux*(width-3)/2)], fill="#896b47", width=1)


def symbol(d, kind, x, y, size):
    """Every glyph is narrower than the same single road; no side-by-side glyphs."""
    c = COLORS[kind]
    if kind == "enemy":
        r = size*.15
        d.ellipse((x-r,y-size*.46,x+r,y-size*.16), fill="#edc49f", outline="#78342d", width=2)
        d.rounded_rectangle((x-size*.26,y-size*.13,x+size*.26,y+size*.2), radius=4, fill=c)
        d.line([(x-size*.27,y-size*.04),(x-size*.41,y+size*.17)], fill=c, width=max(2,round(size*.13)))
        d.line([(x+size*.27,y-size*.04),(x+size*.41,y+size*.17)], fill=c, width=max(2,round(size*.13)))
        for sign in [-1,1]:
            d.line([(x+sign*size*.13,y+size*.17),(x+sign*size*.22,y+size*.46)], fill="#34485c", width=max(2,round(size*.15)))
    elif kind == "bonus":
        r = size*.44
        d.ellipse((x-r,y-r,x+r,y+r), fill="#fff0aa", outline=c, width=max(2,round(size*.08)))
        d.ellipse((x-r*.67,y-r*.67,x+r*.67,y+r*.67), outline=c, width=2)
        d.polygon([(x,y-r*.6),(x+r*.4,y),(x,y+r*.6),(x-r*.4,y)], fill=c)
    else:
        d.rounded_rectangle((x-size*.37,y-size*.38,x+size*.37,y+size*.38), radius=4, fill=c, outline="#15445f", width=2)
        d.line([(x-size*.27,y-size*.22),(x+size*.27,y+size*.22)], fill="#dbf0ff", width=3)
        d.line([(x+size*.27,y-size*.22),(x-size*.27,y+size*.22)], fill="#dbf0ff", width=3)


def basemap(d, sections, placements, factor=1, show_endpoint_labels=True):
    d.rectangle((38*factor,284*factor,1185*factor,1460*factor), fill="#deeff2")
    scale = 1.52*factor
    def p(x,z):
        return (145*factor+(x+75)*scale, 321*factor+(350-z)*scale)
    for grid in range(-400,501,50):
        xx=p(grid,0)[0]
        if 38*factor<xx<1185*factor: d.line([(xx,284*factor),(xx,1460*factor)], fill="#d0e6ec")
        yy=p(0,grid)[1]
        if 284*factor<yy<1460*factor: d.line([(38*factor,yy),(1185*factor,yy)], fill="#d0e6ec")
    for item in sorted(placements, key=lambda item: item["kind"] == "Shop"):
        x,_,z=item["position"]
        cx,cy=p(x,z)
        vertical=nearest_axis(x,z,sections)
        across,along=(9,12.2) if item["kind"]=="Ground" else (5.8,9)
        dx,dz=(across,along) if vertical else (along,across)
        rect=(cx-dx*scale/2,cy-dz*scale/2,cx+dx*scale/2,cy+dz*scale/2)
        d.rectangle(rect, fill="#bfc2bd" if item["kind"]=="Ground" else "#668ba6")
        if item["kind"]=="Shop":
            d.line([(rect[0]+2*factor,cy),(rect[2]-2*factor,cy)], fill="#f8f8ec", width=max(1,round(2*factor)))
    for i in list(range(len(sections)))+[8,14]:
        s=sections[i]
        road(d,p(s["start"][0],s["start"][2]),p(s["end"][0],s["end"][2]),scale,i in [8,14])
    for point,name,offset in [(sections[0]["start"],"시작",(12,8)),(sections[-1]["end"],"출구",(-22,-36))]:
        x,y=p(point[0],point[2]);d.ellipse((x-5*factor,y-5*factor,x+5*factor,y+5*factor),fill="#287460")
        if show_endpoint_labels:
            text(d,(x+offset[0]*factor,y+offset[1]*factor),name,round(24*factor),bold=True)
    return p


def draw(example, sections, placements):
    image = Image.new("RGB", (W,H), "#f8fbfc")
    d = ImageDraw.Draw(image)
    text(d, (54,35), example["title"], 54, bold=True)
    text(d, (56,110), "한 마리 폭 유지  ·  가로로 나란히 놓지 않음  ·  앞뒤로 충분히 떨어뜨림", 30)
    text(d, (55,187), "현재 전체 맵", 33, bold=True)
    text(d, (55,233), "실제 경로·상점 좌표로 그린 도식", 24, "#617985")
    p = basemap(d, sections, placements)
    xa,ya=p(example["x"]-15,example["high"])
    xb,yb=p(example["x"]+15,example["low"])
    d.rounded_rectangle((xa,ya,xb,yb),radius=8,outline="#8a477a",width=4)
    for number,(kind,z) in enumerate(example["events"],1):
        x,y=p(example["x"],z)
        symbol(d,kind,x,y,ROAD_WIDTH*scale*.88)
        d.line([(x+9,y),(x+43,y)],fill=COLORS[kind],width=2)
        text(d,(x+48,y),str(number),28,COLORS[kind],True,"lm")
    text(d,(55,1480),"보라색 테두리 구간을 오른쪽에 확대했습니다.",25,"#61516b")
    d.line([(1222,186),(1222,1485)], fill="#d3dde1",width=2)
    text(d,(1270,187),"해당 구간 확대",33,bold=True)
    text(d,(1270,233),"진행 방향  "+("↓" if example["direction"]<0 else "↑"),27)
    cx,top,bottom=1434,325,1400
    detail_scale=(bottom-top)/(example["high"]-example["low"])
    width=ROAD_WIDTH*detail_scale
    assert width>30
    road(d,(cx,top),(cx,bottom),detail_scale)
    # Stall edges are scenery, never an extra playable lane.
    for yy in range(top+8,bottom-40,54):
        for sign in [-1,1]:
            xx=cx+sign*(width/2+31)
            d.rectangle((xx-13,yy,xx+13,yy+43),fill="#7899ab",outline="#b5c5cc")
            d.line([(xx-12,yy+12),(xx+12,yy+12)],fill="#f7faf5",width=5)
    detail_points=[]
    for number,(kind,z) in enumerate(example["events"],1):
        y=top+(example["high"]-z)*detail_scale
        symbol(d,kind,cx,y,width*.9)
        detail_points.append(y)
        d.line([(cx+width*.55,y),(1580,y)],fill=COLORS[kind],width=2)
        text(d,(1600,y-20),f"{number}. {NAMES[kind]}",30,COLORS[kind],True)
    for y1,y2 in zip(detail_points,detail_points[1:]):
        midpoint=(y1+y2)/2
        head_y=midpoint+25 if example["direction"]<0 else midpoint-25
        tail_y=midpoint-25 if example["direction"]<0 else midpoint+25
        arrow(d,(cx,tail_y),(cx,head_y),"#fff9dd",4,11)
        bracket_x=1548
        lo,hi=sorted((y1,y2))
        d.line([(bracket_x,lo+44),(bracket_x,hi-44)],fill="#8998a0",width=2)
        for yy in [lo+44,hi-44]:d.line([(bracket_x-9,yy),(bracket_x+9,yy)],fill="#8998a0",width=2)
        text(d,(1580,midpoint-17),"사이에 빈 길 확보",28,"#556b76")
    text(d,(1290,1460),"폭은 그대로 확대  /  한 지점에 하나씩",24,"#556b76")
    text(d,(55,1568),"대표 구간 배치 예시  ·  전체 수량·시간표 아님  ·  그림의 표식은 실제 모델이 아님  ·  씬 미적용",26,"#566a75")
    OUT.mkdir(parents=True,exist_ok=True)
    target=OUT/(example["slug"]+".png")
    if target.exists():raise FileExistsError(target)
    image.save(target)
    return str(target)


def main():
    sections=json.loads(FLOW.read_text(encoding="utf-8"))["sections"]
    placements=json.loads(SHOPS.read_text(encoding="utf-8"))["placements"]
    assert len(sections)==16
    for example in EXAMPLES:
        assert len(example["events"])==3
        assert {kind for kind,_ in example["events"]}=={"enemy","bonus","object"}
        assert min(abs(a[1]-b[1]) for a,b in zip(example["events"],example["events"][1:]))>=64
        print(draw(example,sections,placements))


if __name__=="__main__":
    main()
