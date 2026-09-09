"""Full-route target cadence diagram. No Unity scene or screenshot edits."""
from collections import Counter
from pathlib import Path
import json
import math
import sys
from PIL import Image, ImageDraw
import draw_sr18_single_file_examples as base
from draw_sr18_all_single_file_sections import TITLES

ROOT=base.ROOT
OUT=ROOT/"tmp/image-previews/sr18-three-second-cadence-2026-09-06"
RECORDS=ROOT/"map-concepts/sr18-three-second-cadence-2026-09-06"
DURATION=300
KINDS=("enemy","bonus","object")
SHORT={"enemy":"적", "bonus":"BonusWall", "object":"기믹"}
AMBER="#bd741b"


def save(image,name):
    OUT.mkdir(parents=True,exist_ok=True)
    path=OUT/name
    if path.exists():raise FileExistsError(path)
    image.save(path)


def clock(t):
    return f"{int(t)//60}:{t%60:04.1f}"


def plan(encounter_period=1, first_time=None):
    assert encounter_period in (1,3,4)
    sections=json.loads(base.FLOW.read_text(encoding="utf-8"))["sections"]
    shops=json.loads(base.SHOPS.read_text(encoding="utf-8"))["placements"]
    slopes=json.loads((ROOT/"map-concepts/sr18-slope-spots-2026-09-05/placement.json").read_text(encoding="utf-8"))["spots"]
    distance=0
    for i,s in enumerate(sections):
        s["length"]=math.hypot(s["end"][0]-s["start"][0],s["end"][2]-s["start"][2])
        s["offset"]=distance;s["title"]=TITLES[i];s["events"]=[];s["ramps"]=[];s["upper"]=None
        distance+=s["length"]
    for i,start in [(8,0),(14,4)]:
        s=sections[i]
        a,b,c,d=[math.hypot(v["world"][0]-s["start"][0],v["world"][2]-s["start"][2]) for v in slopes[start:start+4]]
        s["ramps"]=[(a,b),(c,d)];s["upper"]=(b,c)
    first = encounter_period*.5 if first_time is None else first_time
    assert 0 <= first < DURATION
    event_count=math.ceil((DURATION-first)/encounter_period)
    events=[]
    for index in range(event_count):
        t=first+index*encounter_period
        route=t/DURATION*distance
        s=next(s for s in sections if s["offset"]<=route<s["offset"]+s["length"])
        along=route-s["offset"];fraction=along/s["length"]
        position=[s["start"][0]+(s["end"][0]-s["start"][0])*fraction,
                  s["start"][2]+(s["end"][2]-s["start"][2])*fraction]
        reasons=[]
        if min(along,s["length"]-along)<8:reasons.append("corner-or-endpoint")
        if any(a-4<=along<=b+4 for a,b in s["ramps"]):reasons.append("ramp-transition")
        event=dict(kind=KINDS[index%3],time=t,section=s["id"],along=along,route_distance=route,
                   world_xz=position,needs_review=reasons,upper_span=s["upper"] is not None)
        s["events"].append(event);events.append(event)
    expected=Counter(KINDS[i%3] for i in range(event_count))
    assert len(events)==event_count and Counter(e["kind"] for e in events)==expected
    assert len(sections)==16 and all(s["events"] or (s["offset"]+s["length"])/distance*DURATION<=first for s in sections)
    assert all(first <= e["time"] < DURATION for e in events)
    assert all(abs(b["time"]-a["time"]-encounter_period)<1e-8 for a,b in zip(events,events[1:]))
    for kind in KINDS:
        series=[e for e in events if e["kind"]==kind]
        assert all(abs(b["time"]-a["time"]-3*encounter_period)<1e-8 for a,b in zip(series,series[1:]))
    return sections,shops,events,distance


def marker(d,event,x,y,size,review=True):
    base.symbol(d,event["kind"],x,y,size)
    if review and event["needs_review"]:
        r=size*.58
        d.ellipse((x-r,y-r,x+r,y+r),outline=AMBER,width=2)


def map_canvas(sections,shops,events,length,quiet_until=0,endpoint_labels=True,render_marker=None):
    canvas=Image.new("RGB",(4000,3280),"#f8fbfc")
    d=ImageDraw.Draw(canvas)
    p=base.basemap(d,sections,shops,factor=2,show_endpoint_labels=endpoint_labels)
    quiet_distance=quiet_until/DURATION*length
    for s in sections:
        remaining=quiet_distance-s["offset"]
        if remaining<=0:break
        fraction=min(1,remaining/s["length"])
        end=[s["start"][i]+(s["end"][i]-s["start"][i])*fraction for i in range(3)]
        d.line([p(s["start"][0],s["start"][2]),p(end[0],end[2])],fill="#45947b",width=9)
    marker_size=base.ROAD_WIDTH*3.04*.72
    draw_marker=marker if render_marker is None else render_marker
    # Match the source map's bridge draw order; lower-road symbols stay beneath it.
    for e in events:
        if not e["upper_span"]:draw_marker(d,e,*p(*e["world_xz"]),marker_size)
    for index in [8,14]:
        s=sections[index]
        base.road(d,p(s["start"][0],s["start"][2]),p(s["end"][0],s["end"][2]),3.04,True)
        for e in events:
            if e["section"]==s["id"]:draw_marker(d,e,*p(*e["world_xz"]),marker_size)
    return canvas,p


def overview(sections,shops,events,length,encounter_period=1,filename="00-full-map.png",quiet_until=0):
    canvas,p=map_canvas(sections,shops,events,length,quiet_until)
    image=Image.new("RGB",(2500,3030),"#f8fbfc")
    image.paste(canvas.crop((76,568,2370,2920)),(103,300))
    d=ImageDraw.Draw(image)
    title="처음 5초 비움 · 이후 3초 조우" if quiet_until else "종류별 3초 주기" if encounter_period==1 else "3초마다 한 번 조우"
    counts=Counter(e["kind"] for e in events)
    base.text(d,(78,34),"현재 노량진 맵 · "+title,63,bold=True)
    base.text(d,(80,126),f"5분 목표  |  적 {counts['enemy']}회 + BonusWall {counts['bonus']}회 + 기믹 {counts['object']}회",38)
    for x,kind in [(100,"enemy"),(830,"bonus"),(1670,"object")]:
        base.symbol(d,kind,x+25,230,48)
        base.text(d,(x+70,207),SHORT[kind]+" · 한 지점에 하나",32,base.COLORS[kind],True)
    # One small callout explicitly exposes the otherwise easy-to-miss phase interpretation.
    line="0–5초 비움 → 5초 첫 적 → 8초 BonusWall → 11초 기믹" if quiet_until else f"목표 조우 순서: 적 → {encounter_period}초 → 벽 → {encounter_period}초 → 기믹 → {encounter_period}초 → 적"
    base.text(d,(80,2710),line,39,bold=True)
    base.text(d,(80,2780),f"조우 간 중심 간격 약 {length/DURATION*encounter_period:.1f}유닛 · 가로 나란히 배치 없음",33)
    base.text(d,(80,2845),"주황 테두리: 회전·경사 주변, 실제 적용 시 위치 조정 필요",31,AMBER)
    base.text(d,(80,2907),"시간은 전체 길을 300초로 균등 환산한 목표값 · 실측/씬 적용 아님",29,"#667b85")
    base.text(d,(80,2960),"처치 드롭은 별도이며, 보너스 표식은 고정 배치 기준입니다.",27,"#667b85")
    if quiet_until:
        x,y=p(sections[0]["start"][0],sections[0]["start"][2])
        x=x-76+103;y=y-568+300
        base.arrow(d,(x+70,y+132),(x+12,y+10),"#397961",3,13)
        base.text(d,(x+70,y+162),"0–5초 조우 없음",32,"#397961",True,"rm")
    save(image,filename)


def section_groups(group,sections,length):
    image=Image.new("RGB",(2800,2660),"#f8fbfc")
    d=ImageDraw.Draw(image);start=group*4
    base.text(d,(60,36),f"종류별 3초 · {start+1:02d}–{start+4:02d} 구간 전체 조우",60,bold=True)
    base.text(d,(62,122),"각 열은 아래로 진행 / 적·벽·기믹을 1초씩 엇갈림 / 한 줄 폭 유지",35)
    for col,index in enumerate(range(start,start+4)):
        s=sections[index];left=col*700
        if col:d.line([(left,200),(left,2460)],fill="#d3dde1",width=2)
        base.text(d,(left+30,218),f"{index+1:02d} {s['title']}",36,bold=True)
        t0=s["offset"]/length*DURATION;t1=(s["offset"]+s["length"])/length*DURATION
        base.text(d,(left+32,277),f"{clock(t0)} ~ {clock(t1)}",29,"#617985")
        top,bottom=386,2350
        scale=min((bottom-top)/s["length"],9)
        cx=left+140;width=base.ROAD_WIDTH*scale
        y0=top+(bottom-top-s["length"]*scale)/2
        base.road(d,(cx,y0),(cx,y0+s["length"]*scale),scale,bool(s["ramps"]))
        for a,b in s["ramps"]:
            d.line([(cx-width/2-8,y0+a*scale),(cx-width/2-8,y0+b*scale)],fill=AMBER,width=6)
        for e in s["events"]:
            yy=y0+e["along"]*scale
            marker(d,e,cx,yy,width*.66)
            d.line([(cx+width*.62,yy),(left+235,yy)],fill=base.COLORS[e["kind"]],width=2)
            tag="  △" if e["needs_review"] else ""
            base.text(d,(left+250,yy),f"{clock(e['time'])}  {SHORT[e['kind']]}{tag}",27,base.COLORS[e["kind"]],True,"lm")
        counts=Counter(e["kind"] for e in s["events"])
        base.text(d,(left+30,2415),f"적 {counts['enemy']} · 벽 {counts['bonus']} · 기믹 {counts['object']}",29)
    base.text(d,(62,2510),"△ 회전·경사 조정 대상 / 표식은 실제 모델이 아닌 배치 칸 / 원래 길 형태는 전체 맵 참조",30,AMBER)
    base.text(d,(62,2570),"5분 균등 이동 가정의 시간표 · 실제 이동·회전·전투 시간은 별도 검증 · 씬 미적용",29,"#667b85")
    save(image,f"group-{group+1:02d}.png")


def cadence_example():
    image=Image.new("RGB",(2000,920),"#f8fbfc");d=ImageDraw.Draw(image)
    base.text(d,(60,32),"같은 종류는 3초마다 · 전체 조우는 1초마다",49,bold=True)
    x0,y0,step=155,420,200
    d.line([(85,y0),(1895,y0)],fill="#c19c6d",width=62)
    for i in range(9):
        x=x0+step*i;kind=KINDS[i%3]
        base.symbol(d,kind,x,y0,48)
        base.text(d,(x,y0-85),f"{i+.5:.1f}초",31,base.COLORS[kind],True,"mm")
        base.text(d,(x,y0+86),SHORT[kind],27,base.COLORS[kind],True,"mm")
        if i<8:base.arrow(d,(x+42,y0),(x+step-42,y0),"#fff9df",3,9)
    for i,kind in enumerate(KINDS):
        x=x0+i*step;y=660+i*60
        d.line([(x,y),(x+step*3,y)],fill=base.COLORS[kind],width=3)
        for xx in [x,x+step*3]:d.line([(xx,y-9),(xx,y+9)],fill=base.COLORS[kind],width=3)
        base.text(d,(x+step*1.5,y-25),SHORT[kind]+" ↔ "+SHORT[kind]+" = 3초",26,base.COLORS[kind],True,"mm")
    base.text(d,(60,846),"각 종류가 3초마다라는 해석입니다. 전체에서 하나를 3초마다 만나는 구조와는 다릅니다.",28,"#667b85")
    save(image,"05-cadence-key.png")


def main():
    sections,shops,events,length=plan()
    overview(sections,shops,events,length)
    for group in range(4):section_groups(group,sections,length)
    cadence_example()
    RECORDS.mkdir(parents=True,exist_ok=True)
    path=RECORDS/"schedule.json"
    if path.exists():raise FileExistsError(path)
    payload=dict(status="Target-time drawing only; not installed",interpretation="Each category every 3 seconds; phases offset by 1 second",
                 duration=300,route_length=length,assumed_speed=length/300,per_kind_period=3,encounter_period=1,
                 counts=dict(Counter(e["kind"] for e in events)),review_count=sum(bool(e["needs_review"]) for e in events),
                 events=events,sections=[dict(id=s["id"],title=s["title"],start=s["start"],end=s["end"],length=s["length"],offset=s["offset"]) for s in sections])
    path.write_text(json.dumps(payload,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
    print(json.dumps({"images":6,"counts":payload["counts"],"review_count":payload["review_count"],"section_count":len(sections),"spacing_units":length/300,"output":str(OUT)},ensure_ascii=False))


def comparison():
    sections,shops,events,length=plan(3)
    overview(sections,shops,events,length,3,"01-one-encounter-every-3s.png")
    path=RECORDS/"schedule-alternating.json"
    if path.exists():raise FileExistsError(path)
    payload=dict(status="Alternative interpretation drawing; not installed",interpretation="One encounter every 3 seconds; alternate enemy, wall, gimmick; each kind every 9 seconds",
                 duration=300,encounter_period=3,per_kind_period=9,counts=dict(Counter(e["kind"] for e in events)),events=events)
    path.write_text(json.dumps(payload,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
    print(json.dumps({"comparison_counts":payload["counts"],"review_count":sum(bool(e["needs_review"]) for e in events),"file":str(OUT/"01-one-encounter-every-3s.png")}))


def quiet_start_detail(sections,shops,events,length):
    canvas,p=map_canvas(sections,shops,[e for e in events if e["time"]<=11],length,5,endpoint_labels=False)
    x0,y0=p(-32,-40);x1,y1=p(70,-136)
    crop=(round(x0),round(y0),round(x1),round(y1))
    im=Image.new("RGB",(2000,1680),"#f8fbfc")
    target_width=1300
    factor=target_width/(crop[2]-crop[0])
    target_height=round((crop[3]-crop[1])*factor)
    im.paste(canvas.crop(crop).resize((target_width,target_height),Image.Resampling.LANCZOS),(70,245))
    d=ImageDraw.Draw(im)
    base.text(d,(58,32),"시작 구간 확대 · 첫 5초는 이동만",53,bold=True)
    base.text(d,(60,112),"현재 맵의 시작 짧은 길과 첫 코너 유지 / 가로로 나란히 배치하지 않음",28)
    for e in events[:3]:
        x,y=p(*e["world_xz"]);x=70+(x-crop[0])*factor;y=245+(y-crop[1])*factor
        d.line([(x+33,y),(1450,y)],fill=base.COLORS[e["kind"]],width=2)
        words={"enemy":"5초 · 첫 적 한 마리", "bonus":"8초 · BonusWall 한 개", "object":"11초 · 기믹 한 개"}[e["kind"]]
        base.text(d,(1470,y-20),words,31,base.COLORS[e["kind"]],True)
    x,y=p(sections[0]["start"][0],sections[0]["start"][2])
    x=70+(x-crop[0])*factor;y=245+(y-crop[1])*factor
    base.text(d,(x+45,y+15),"시작",32,"#397961",True)
    base.text(d,(x+45,y+64),"0–5초 이동만",32,"#397961",True)
    base.text(d,(60,1535),"첫 적은 첫 코너를 지난 뒤 / 5초는 목표 시각이며 실제 회전 종료와 맞춰 조정",28,"#667b85")
    base.text(d,(60,1593),"초록 길에는 적·BonusWall·기믹을 놓지 않음 · 상점과 배경은 유지 · 씬 미적용",28,"#667b85")
    save(im,"01-start-closeup.png")


def quiet_start_timeline():
    im=Image.new("RGB",(2000,760),"#f8fbfc");d=ImageDraw.Draw(im)
    base.text(d,(60,35),"처음은 비우고 · 5초부터 3초 간격",51,bold=True)
    base.text(d,(62,123),"0–5초 출발 → 5초 적 → 8초 벽 → 11초 기믹 → 14초 적 → 17초 벽",29)
    start,y,pixels=80,360,96
    d.line([(start,y),(1900,y)],fill="#c19c6d",width=66)
    d.line([(start,y),(start+5*pixels-38,y)],fill="#68a58e",width=66)
    base.text(d,(start+210,y-105),"0–5초",34,"#397961",True,"mm")
    base.text(d,(start+210,y+92),"조우 없이 이동",31,"#397961",True,"mm")
    for i,t in enumerate([5,8,11,14,17]):
        kind=KINDS[i%3];x=start+t*pixels
        base.symbol(d,kind,x,y,49)
        base.text(d,(x,y-95),f"{t}초",34,base.COLORS[kind],True,"mm")
        base.text(d,(x,y+95),SHORT[kind],30,base.COLORS[kind],True,"mm")
        if i<4:
            base.arrow(d,(x+45,y),(x+3*pixels-45,y),"#fff9de",3,12)
            base.text(d,(x+1.5*pixels,y+170),"3초",29,"#667b85",anchor="mm")
    base.text(d,(60,667),"300초 동안 적 33회 · BonusWall 33회 · 기믹 33회 / 같은 종류끼리는 9초 간격",30,"#667b85")
    save(im,"02-start-rhythm.png")


def quiet_start():
    global OUT
    OUT=OUT/"quiet-start-5s/final"
    sections,shops,events,length=plan(3,5)
    assert [e["time"] for e in events[:4]]==[5,8,11,14]
    assert len(events)==99 and Counter(e["kind"] for e in events)==dict.fromkeys(KINDS,33)
    assert not sections[0]["events"]
    overview(sections,shops,events,length,3,"00-full-map.png",quiet_until=5)
    quiet_start_detail(sections,shops,events,length)
    quiet_start_timeline()
    path=RECORDS/"schedule-quiet-start-5s-final.json"
    if path.exists():raise FileExistsError(path)
    payload=dict(status="Drawing only; scene not changed",duration=300,quiet_until=5,first_encounter=5,
                 encounter_period=3,per_kind_period=9,counts=dict(Counter(e["kind"] for e in events)),
                 review_count=sum(bool(e["needs_review"]) for e in events),events=events)
    path.write_text(json.dumps(payload,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
    print(json.dumps({"images":3,"first_times":[e["time"] for e in events[:4]],"counts":payload["counts"],"review_count":payload["review_count"],"output":str(OUT)}))


if __name__=="__main__":
    quiet_start() if "--quiet-start" in sys.argv else comparison() if "--compare" in sys.argv else main()
