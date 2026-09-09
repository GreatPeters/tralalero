"""Four-second encounter proposal with single-file patrol and forward ambush roles."""
from collections import Counter
import json
import copy
import math
import sys
from PIL import Image, ImageDraw
import draw_sr18_three_second_cadence as cadence
import draw_sr18_single_file_examples as base

ROOT=base.ROOT
OUT=ROOT/"tmp/image-previews/sr18-four-second-moving-enemies-2026-09-06"
RECORDS=ROOT/"map-concepts/sr18-four-second-moving-enemies-2026-09-06"
ROLE_COLORS={"standard":"#bd4638","patrol":"#8054a8","ambush":"#236d87"}
ROLE_NAMES={"standard":"기본 적","patrol":"왕복 이동 적","ambush":"전방 매복 사격 적"}


def save(image,name):
    OUT.mkdir(parents=True,exist_ok=True)
    path=OUT/name
    if path.exists():raise FileExistsError(path)
    image.save(path)


def build_plan():
    sections,shops,events,length=cadence.plan(4,5)
    enemies=[e for e in events if e["kind"]=="enemy"]
    roles=("patrol","ambush","standard");cursor=0
    for i,e in enumerate(enemies):
        s=next(s for s in sections if s["id"]==e["section"])
        wanted=roles[cursor%3] if 1<i<len(enemies)-1 else "standard"
        along=e["along"]
        clear=lambda a,b: not any(not (b<lo-6 or a>hi+6) for lo,hi in s["ramps"])
        eligible=(wanted=="patrol" and 14<along<s["length"]-14 and clear(along-6,along+6)) or (
            wanted=="ambush" and s["id"] not in ("S09","S15") and 32<along<s["length"]-14 and clear(along-30,along+6))
        role=wanted if wanted=="standard" or eligible else "standard"
        if 1<i<len(enemies)-1 and (role==wanted):cursor+=1
        e["enemy_role"]=role
        if role=="patrol":
            e["patrol_half_length"]=4
            e["rule"]="One enemy patrols fore/aft on the same lane; never crosses a corner or overlaps the next station."
        elif role=="ambush":
            e["activation_lead_candidate"]=30
            e["rule"]="Player enters an upstream trigger zone; enemy waits further ahead behind existing scenery, emerges there, telegraphs, then shoots from range. No spawn beside/behind the player."
            e["implementation_gap"]="Existing move-then-attack does not chain into Shoot; visibility/movement/fire sequencing needs implementation."
    assert len(events)==74 and Counter(e["kind"] for e in events)=={"enemy":25,"bonus":25,"object":24}
    assert [e["time"] for e in events[:4]]==[5,9,13,17]
    assert all(b["time"]-a["time"]==4 for a,b in zip(events,events[1:]))
    return sections,shops,events,length


def marker(d,event,x,y,size,review=True):
    base.symbol(d,event["kind"],x,y,size)
    role=event.get("enemy_role","standard")
    if event["kind"]=="enemy" and role!="standard":
        r=size*.53
        if role=="patrol":d.ellipse((x-r,y-r,x+r,y+r),outline=ROLE_COLORS[role],width=3)
        else:d.rectangle((x-r,y-r,x+r,y+r),outline=ROLE_COLORS[role],width=3)
    if review and event["needs_review"]:
        r=size*.72
        d.ellipse((x-r,y-r,x+r,y+r),outline=cadence.AMBER,width=2)


def overview(sections,shops,events,length,swaps=None,revision_note=None):
    canvas,p=cadence.map_canvas(sections,shops,events,length,5,render_marker=marker)
    im=Image.new("RGB",(2500,3180),"#f8fbfc")
    im.paste(canvas.crop((76,568,2370,2920)),(103,450))
    d=ImageDraw.Draw(im)
    base.text(d,(75,30),"현재 맵 · 시작 5초 비움 / 이후 4초 조우",58,bold=True)
    base.text(d,(77,120),"적 25회 · BonusWall 25회 · 기믹 24회 / 적 칸에 움직임을 섞음",35)
    if swaps:
        base.text(d,(77,164),revision_note or "교차부 2곳: 적 ↔ 기존 BonusWall 자리 교환 / 총개수 유지",27,"#397961",True)
    roles=Counter(e.get("enemy_role") for e in events if e["kind"]=="enemy")
    for x,role in [(100,"standard"),(870,"patrol"),(1620,"ambush")]:
        marker(d,dict(kind="enemy",enemy_role=role,needs_review=[]),x+25,238,48)
        base.text(d,(x+80,213),f"{ROLE_NAMES[role]} {roles[role]}",33,ROLE_COLORS[role],True)
    for x,kind in [(110,"bonus"),(890,"object")]:
        base.symbol(d,kind,x+18,350,42)
        base.text(d,(x+70,328),cadence.SHORT[kind]+" · 한 지점에 하나",32,base.COLORS[kind],True)
    base.text(d,(1680,328),"매복은 플레이어 앞쪽에서만",29,ROLE_COLORS["ambush"],True)
    # Label the quiet prefix in open water, not over the neighboring route.
    x,y=p(sections[0]["start"][0],sections[0]["start"][2]);x=x-76+103;y=y-568+450
    base.arrow(d,(x+65,y+132),(x+12,y+10),"#397961",3,13)
    base.text(d,(x+65,y+164),"0–5초 조우 없음",31,"#397961",True,"rm")
    if swaps:
        for number,change in enumerate(swaps,1):
            xx,yy=p(*change["crossing"]["world_xz"]);xx=xx-76+103;yy=yy-568+450
            d.ellipse((xx-27,yy-27,xx+27,yy+27),outline="#397961",width=5)
            base.text(d,(xx-38,yy-41),str(number),33,"#397961",True,"rm")
    base.text(d,(77,2860),"5초 적 → 9초 벽 → 13초 기믹 → 17초 적 → …",42,bold=True)
    base.text(d,(77,2935),"주황 외곽: 회전·경사 조정 필요 / 매복 대기·발사 위치는 상세 기획에서 구분",30,cadence.AMBER)
    base.text(d,(77,3000),"4초는 기본 배치 칸의 목표 간격 · 이동/등장 예고와 발사 시점은 별도 조율",29,"#667b85")
    base.text(d,(77,3060),"5분 균등 이동 가정 · 기믹/드롭과의 겹침은 적용 전 검증 · 실제 씬 미적용",29,"#667b85")
    save(im,"00-full-map-4s.png")


def dashed(d,a,b,color,width=3):
    import math
    dx,dy=b[0]-a[0],b[1]-a[1];length=math.hypot(dx,dy)
    for offset in range(0,round(length),20):
        aa=offset/length;bb=min(1,(offset+10)/length)
        d.line([(a[0]+dx*aa,a[1]+dy*aa),(a[0]+dx*bb,a[1]+dy*bb)],fill=color,width=width)


def lane(d,cx,top,bottom):
    base.road(d,(cx,top),(cx,bottom),8.5)
    for y in range(top+15,bottom-25,84):
        for sign in (-1,1):
            x=cx+sign*68
            d.rectangle((x-21,y,x+21,y+61),fill="#7899ab",outline="#536d7c",width=2)
            d.line([(x-19,y+18),(x+19,y+18)],fill="#f7faf5",width=6)


def behaviors(events):
    im=Image.new("RGB",(2400,2060),"#f8fbfc");d=ImageDraw.Draw(im)
    base.text(d,(60,32),"움직이는 적 · 한 줄 길에서의 행동 기획",57,bold=True)
    base.text(d,(62,124),"한 적을 추가 행동으로 변주 / 옆에서 생성하지 않음 / 화살표는 이동·발사 표시이지 새 길이 아님",29)
    d.line([(1200,218),(1200,1900)],fill="#d3dde1",width=2)
    base.text(d,(62,235),"① 통로 앞뒤 왕복",44,ROLE_COLORS["patrol"],True)
    base.text(d,(1250,235),"② 전방 매복 → 등장 → 원거리 사격",39,ROLE_COLORS["ambush"],True)
    for cx in (390,1560):lane(d,cx,440,1700)
    # One figure only; endpoints are small location marks, not additional enemies.
    base.symbol(d,"enemy",390,800,60)
    color=ROLE_COLORS["patrol"]
    for yy in (650,1040):d.ellipse((382,yy-8,398,yy+8),fill=color)
    base.arrow(d,(340,1000),(340,680),color,4,13)
    base.arrow(d,(438,680),(438,1000),color,4,13)
    base.text(d,(550,655),"앞 지점 ↔ 뒤 지점",35,color,True)
    base.text(d,(550,730),"같은 적 한 마리",33,color,True)
    base.text(d,(550,800),"좁은 통로 안에서 왕복",30)
    base.text(d,(550,865),"코너·다음 보너스까지 넘어가지 않음",27)
    d.rectangle((343,1420,437,1475),outline=color,width=4)
    base.text(d,(550,1430),"이 구역에 진입하면 이동 시작",30,color,True)
    base.arrow(d,(390,1630),(390,1510),"#31705d",5,16)
    base.text(d,(485,1615),"플레이어 진행",30,"#31705d")
    base.text(d,(62,1800),"기존 왕복 이동 기능을 활용",31,color,True)
    base.text(d,(62,1850),"첫 도입은 단순 적 구간에서, 한 번에 한 마리만.",27)
    # Waiting position is far ahead of the player's trigger, beside forward scenery.
    color=ROLE_COLORS["ambush"]
    d.rounded_rectangle((1770,500,2050,670),radius=8,fill="#d7e2e8",outline="#6c8799",width=3)
    base.text(d,(1810,525),"전방 대기 위치",30,color,True)
    base.text(d,(1810,577),"상점 뒤 / 시야 밖",25)
    d.ellipse((1862,622,1878,638),fill=color)
    dashed(d,(1810,700),(1620,700),color,4)
    base.arrow(d,(1650,700),(1600,700),color,4,14)
    base.symbol(d,"enemy",1560,700,60)
    base.text(d,(1780,750),"② 앞쪽 길로 나옴",32,color,True)
    base.text(d,(1780,820),"동시에 나타나는 적은 한 마리",27)
    dashed(d,(1560,760),(1560,1300),"#bd4638",4)
    base.arrow(d,(1560,1250),(1560,1315),"#bd4638",4,15)
    base.text(d,(1775,1060),"③ 발사 예고 후 원거리 사격",30,"#bd4638",True)
    base.text(d,(1775,1120),"총알을 보고 반응할 거리 확보",27)
    d.rectangle((1513,1420,1607,1475),outline=color,width=4)
    base.text(d,(1775,1430),"① 특정 구역 진입",32,color,True)
    base.arrow(d,(1560,1630),(1560,1510),"#31705d",5,16)
    base.text(d,(1660,1615),"플레이어 진행",30,"#31705d")
    base.text(d,(1250,1800),"플레이어 바로 옆·등 뒤 출현 금지",31,color,True)
    base.text(d,(1250,1850),"대기 → 등장 → 발사 연속 동작은 추가 연결 필요",27)
    base.text(d,(62,1970),"동작 설명용 확대도 · 길 폭 변경 없음 · 매복 대기 공간과 발동 거리는 실제 맵에서 추가 확인 · 씬 미적용",28,"#667b85")
    save(im,"01-moving-enemy-behaviors.png")


def timeline(events):
    im=Image.new("RGB",(2200,820),"#f8fbfc");d=ImageDraw.Draw(im)
    base.text(d,(60,32),"시작 5초 비움 · 이후 4초마다 한 번",55,bold=True)
    base.text(d,(62,120),"기본 적 → 보너스 → 기믹을 유지하면서, 일부 적 칸을 왕복·전방 매복으로 교체",30)
    x0,step,y=125,238,420
    examples=events[6:14]
    d.line([(65,y),(2135,y)],fill="#c19c6d",width=68)
    for i,e in enumerate(examples):
        x=x0+i*step
        marker(d,e,x,y,50,False)
        kind=e["kind"];role=e.get("enemy_role","standard")
        label=ROLE_NAMES[role] if kind=="enemy" else cadence.SHORT[kind]
        color=ROLE_COLORS[role] if kind=="enemy" else base.COLORS[kind]
        base.text(d,(x,y-100),f"{int(e['time'])}초",34,color,True,"mm")
        base.text(d,(x,y+95),label,25,color,True,"mm")
        if i<7:
            base.arrow(d,(x+42,y),(x+step-42,y),"#fff9de",3,12)
            base.text(d,(x+step/2,y+174),"4초",27,"#667b85",anchor="mm")
    base.text(d,(60,723),"4초 = 전체 조우 간격 / 같은 종류끼리는 12초 / 움직이는 적은 적 칸의 변주이며 별도 추가 조우가 아님",29,"#667b85")
    save(im,"02-four-second-rhythm.png")


def main():
    sections,shops,events,length=build_plan()
    overview(sections,shops,events,length)
    behaviors(events)
    timeline(events)
    RECORDS.mkdir(parents=True,exist_ok=True)
    path=RECORDS/"image-plan.json"
    if path.exists():raise FileExistsError(path)
    roles=dict(Counter(e["enemy_role"] for e in events if e["kind"]=="enemy"))
    payload=dict(status="Image planning only; Unity scene unchanged",duration=300,quiet_until=5,
                 encounter_period=4,same_kind_period=12,counts=dict(Counter(e["kind"] for e in events)),enemy_roles=roles,
                 review_count=sum(bool(e["needs_review"]) for e in events),events=events)
    path.write_text(json.dumps(payload,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
    print(json.dumps({"images":3,"counts":payload["counts"],"roles":roles,"review_count":payload["review_count"],"output":str(OUT)},ensure_ascii=False))


def swap_crossing_bonuses(sections,events):
    """Swap roles only. Keep every time, position, deck tag and review flag unchanged."""
    before=copy.deepcopy(events)
    changes=[]
    for section_id,point in [("S09",(-10.8,-77.7511)),("S15",(326.75,135.9989))]:
        target=min((e for e in events if e["section"]==section_id and e["kind"]=="enemy"),
                   key=lambda e:math.dist(e["world_xz"],point))
        donor=min((e for e in events if e["section"]==section_id and e["kind"]=="bonus"),
                  key=lambda e:abs(e["route_distance"]-target["route_distance"]))
        assert target["enemy_role"]=="standard"
        change=dict(crossing=copy.deepcopy(target),donor=copy.deepcopy(donor))
        target["kind"]="bonus";target.pop("enemy_role")
        donor["kind"]="enemy";donor["enemy_role"]="standard"
        changes.append(change)
    assert [(c["crossing"]["time"],c["donor"]["time"]) for c in changes]==[(161,165),(269,273)]
    assert Counter(e["kind"] for e in before)==Counter(e["kind"] for e in events)=={"enemy":25,"bonus":25,"object":24}
    assert Counter(e.get("enemy_role") for e in before)==Counter(e.get("enemy_role") for e in events)
    changed={161,165,269,273}
    for old,new in zip(before,events):
        assert all(old[k]==new[k] for k in ("time","world_xz","along","route_distance","section","upper_span","needs_review"))
        if old["time"] not in changed:assert old==new
    return changes


def swap_detail(sections,shops,events,length,swaps):
    canvas,p=cadence.map_canvas(sections,shops,events,length,5,endpoint_labels=False,render_marker=marker)
    im=Image.new("RGB",(2600,1990),"#f8fbfc");d=ImageDraw.Draw(im)
    base.text(d,(65,34),"교차 지점은 BonusWall · 원래 벽 자리에는 적",54,bold=True)
    base.text(d,(67,118),"보너스를 추가하지 않고 두 쌍의 자리를 맞바꿈 / 적 25 · BonusWall 25 · 기믹 24 유지",32)
    regions=[(-42,42,-120,-35),(258,367,86,188)]
    titles=["① 첫 고가 교차 지점", "② 두 번째 고가 교차 지점"]
    for i,(region,change) in enumerate(zip(regions,swaps)):
        left=65+i*1300
        xmin,xmax,zmin,zmax=region
        x0,y0=p(xmin,zmax);x1,y1=p(xmax,zmin)
        crop=(round(x0),round(y0),round(x1),round(y1))
        factor=min(1170/(crop[2]-crop[0]),1120/(crop[3]-crop[1]))
        width,height=round((crop[2]-crop[0])*factor),round((crop[3]-crop[1])*factor)
        im.paste(canvas.crop(crop).resize((width,height),Image.Resampling.LANCZOS),(left,360))
        base.text(d,(left,225),titles[i],41,bold=True)
        base.text(d,(left,290),f"{cadence.clock(change['crossing']['time'])} 적 → 벽    /    {cadence.clock(change['donor']['time'])} 벽 → 적",28)
        for label,key,color in [("벽으로 교체","crossing",base.COLORS["bonus"]),("적을 옮김","donor",base.COLORS["enemy"])]:
            x,y=p(*change[key]["world_xz"])
            x=left+(x-crop[0])*factor;y=360+(y-crop[1])*factor
            radius=13*factor
            d.ellipse((x-radius,y-radius,x+radius,y+radius),outline=color,width=5)
            # Labels are in the water above the horizontal bridge, not on its deck.
            yy=y-radius-50
            base.text(d,(x,yy),label,30,color,True,"mm")
        base.text(d,(left,1560),"교차부의 적을 빼고, 기존 벽을 이 자리로 이동",28,"#397961",True)
        base.text(d,(left,1610),"벽이 있던 자리에는 기본 적 한 마리 배치",28,"#617985")
        if i==1:
            base.text(d,(left,1680),"경사 경계 인근: 실제 적용 시 윗길 높이 확인 필요",27,cadence.AMBER)
        else:
            base.text(d,(left,1680),"아랫길이 아닌 기존 상부 진행선의 배치를 교환",27,cadence.AMBER)
    base.text(d,(65,1845),"길·시간·총수량·왕복/매복 적 수는 유지 · 유형 순서만 두 곳에서 교환 · 4초는 전체 조우 간격",29,"#667b85")
    base.text(d,(65,1902),"이미지 기획 수정만 반영 · 실제 Unity 씬은 변경하지 않음",29,"#667b85")
    save(im,"01-crossing-swaps-detail.png")


def crossing_revision():
    global OUT
    OUT=OUT/"crossing-bonus-swap"
    sections,shops,events,length=build_plan()
    swaps=swap_crossing_bonuses(sections,events)
    overview(sections,shops,events,length,swaps)
    swap_detail(sections,shops,events,length,swaps)
    path=RECORDS/"image-plan-crossing-bonuses.json"
    if path.exists():raise FileExistsError(path)
    payload=dict(status="Image planning revision only; Unity scene unchanged",duration=300,quiet_until=5,encounter_period=4,
                 base_same_kind_period=12,same_kind_period_note="Local type-order exceptions at swapped stations",
                 counts=dict(Counter(e["kind"] for e in events)),enemy_roles=dict(Counter(e["enemy_role"] for e in events if e["kind"]=="enemy")),
                 swaps=swaps,events=events)
    path.write_text(json.dumps(payload,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
    print(json.dumps({"images":2,"counts":payload["counts"],"roles":payload["enemy_roles"],"swap_times":[[s["crossing"]["time"],s["donor"]["time"]] for s in swaps],"output":str(OUT)},ensure_ascii=False))


def nudge_lower_bridge_bonuses(sections,events,length):
    before=copy.deepcopy(events);moves=[];shift=11.25/4
    section=next(s for s in sections if s["id"]=="S09")
    for event in events:
        if event["time"] not in (161,177):continue
        assert event["section"]=="S09" and event["kind"]=="bonus"
        previous=copy.deepcopy(event)
        event["world_xz"][0]-=shift
        event["along"]-=shift;event["route_distance"]-=shift
        event["geometry_time"]=event["route_distance"]/length*300
        event["nominal_time_note"]="Time is the original planning slot; X-only authoring offset applied."
        event["needs_review"]=[]
        if min(event["along"],section["length"]-event["along"])<8:event["needs_review"].append("corner-or-endpoint")
        if any(a-4<=event["along"]<=b+4 for a,b in section["ramps"]):event["needs_review"].append("ramp-transition")
        assert section["upper"][0]<event["along"]<section["upper"][1]
        moves.append(dict(time=event["time"],before=previous["world_xz"],after=event["world_xz"].copy(),delta_x=-shift,layer="first upper bridge"))
    assert len(moves)==2
    for old,new in zip(before,events):
        if old["time"] not in (161,177):assert old==new
        else:assert old["kind"]==new["kind"] and old["world_xz"][1]==new["world_xz"][1]
    assert Counter(e["kind"] for e in events)=={"enemy":25,"bonus":25,"object":24}
    return moves


def nudge_detail(sections,shops,events,length,moves):
    canvas,p=cadence.map_canvas(sections,shops,events,length,5,endpoint_labels=False,render_marker=marker)
    a,b=p(-40,-53),p(145,-100)
    crop=(round(a[0]),round(a[1]),round(b[0]),round(b[1]));factor=2140/(crop[2]-crop[0])
    im=Image.new("RGB",(2300,1120),"#f8fbfc")
    height=round((crop[3]-crop[1])*factor)
    im.paste(canvas.crop(crop).resize((2140,height),Image.Resampling.LANCZOS),(80,270))
    d=ImageDraw.Draw(im)
    single=len(moves)==1
    base.text(d,(60,32),"가운데 교차부의 BonusWall 하나만 조금 더 왼쪽으로" if single else "표시한 아래쪽 다리의 벽 두 개만 왼쪽으로",53,bold=True)
    base.text(d,(62,121),"직전 위치에서 ¼칸 더 이동 / 왼쪽 첫 벽 · 위쪽 ②번 벽 · 나머지 배치는 그대로" if single else "각각 약 ¼칸 이동 / 위쪽 ②번 벽 · 적 · 기믹 · 총수량은 그대로",32)
    for move in moves:
        oldx,oldy=p(*move["before"]);newx,newy=p(*move["after"])
        oldx=80+(oldx-crop[0])*factor;newx=80+(newx-crop[0])*factor
        yy=270+(newy-crop[1])*factor
        radius=11*factor
        d.ellipse((newx-radius,yy-radius,newx+radius,yy+radius),outline="#397961",width=5)
        arrow_y=yy-radius-35
        d.line([(oldx,arrow_y-14),(oldx,arrow_y+14)],fill="#70828c",width=3)
        base.arrow(d,(oldx,arrow_y),(newx,arrow_y),"#397961",4,10)
        base.text(d,(newx,arrow_y-36),"← ¼칸",31,"#397961",True,"mm")
    base.text(d,(62,908),"회색 눈금 = 이전 위치 / 초록 테두리 = 옮긴 위치",30,"#397961",True)
    base.text(d,(62,970),"길의 폭·높이·연결은 유지 · 도면상 위치만 미세 조정",30,"#617985")
    base.text(d,(62,1034),"같은 상부 다리 위에서 X 위치만 변경 · 이미지 기획 수정이며 Unity 씬은 그대로" if single else "첫 벽은 경사 끝 가까이에 있어 실제 적용 시 여유 확인 필요 · Unity 씬 미수정",28,cadence.AMBER)
    save(im,"01-left-nudge-detail.png")


def nudge_revision():
    global OUT
    OUT=OUT/"crossing-bonus-swap/left-quarter"
    sections,shops,events,length=build_plan()
    swaps=swap_crossing_bonuses(sections,events)
    moves=nudge_lower_bridge_bonuses(sections,events,length)
    swaps[0]["crossing"]["world_xz"]=moves[0]["after"].copy()
    overview(sections,shops,events,length,swaps,"아래쪽 다리의 BonusWall 2개만 왼쪽 ¼칸 이동 / 나머지 배치 유지")
    nudge_detail(sections,shops,events,length,moves)
    path=RECORDS/"image-plan-left-nudge.json"
    if path.exists():raise FileExistsError(path)
    payload=dict(status="Image plan only; Unity scene unchanged",duration=300,quiet_until=5,base_encounter_period=4,
                 counts=dict(Counter(e["kind"] for e in events)),enemy_roles=dict(Counter(e["enemy_role"] for e in events if e["kind"]=="enemy")),moves=moves,events=events)
    path.write_text(json.dumps(payload,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
    print(json.dumps({"images":2,"moves":moves,"counts":payload["counts"],"output":str(OUT)},ensure_ascii=False))


def center_left_revision():
    global OUT
    OUT=OUT/"crossing-bonus-swap/center-left-2026-09-07"
    sections,shops,events,length=build_plan()
    swaps=swap_crossing_bonuses(sections,events)
    nudge_lower_bridge_bonuses(sections,events,length)
    before=copy.deepcopy(events)
    target=next(e for e in events if e["time"]==177)
    assert target["kind"]=="bonus" and target["section"]=="S09"
    previous=target["world_xz"].copy();shift=11.25/4
    target["world_xz"][0]-=shift;target["along"]-=shift;target["route_distance"]-=shift
    target["geometry_time"]=target["route_distance"]/length*300
    assert sections[8]["upper"][0]<target["along"]<sections[8]["upper"][1]
    for old,new in zip(before,events):
        if old["time"]!=177:assert old==new
        else:assert old["world_xz"][1]==new["world_xz"][1] and old["kind"]==new["kind"]
    moves=[dict(time=177,before=previous,after=target["world_xz"].copy(),delta_x=-shift,layer="first upper bridge")]
    swaps[0]["crossing"]["world_xz"]=next(e for e in events if e["time"]==161)["world_xz"].copy()
    overview(sections,shops,events,length,swaps,"가운데 교차부 BonusWall 하나만 왼쪽으로 ¼칸 더 이동 / 나머지 배치 유지")
    nudge_detail(sections,shops,events,length,moves)
    path=RECORDS/"image-plan-center-left-2026-09-07.json"
    if path.exists():raise FileExistsError(path)
    payload=dict(status="Image plan only; Unity scene unchanged",previous_plan="image-plan-left-nudge.json",duration=300,quiet_until=5,base_encounter_period=4,
                 counts=dict(Counter(e["kind"] for e in events)),enemy_roles=dict(Counter(e["enemy_role"] for e in events if e["kind"]=="enemy")),moves=moves,events=events)
    assert payload["counts"]=={"enemy":25,"bonus":25,"object":24}
    path.write_text(json.dumps(payload,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
    print(json.dumps({"moves":moves,"counts":payload["counts"],"output":str(OUT)},ensure_ascii=False))


if __name__=="__main__":
    if "--nudge-center-left" in sys.argv:center_left_revision()
    elif "--nudge-left" in sys.argv:nudge_revision()
    elif "--crossing-bonuses" in sys.argv:crossing_revision()
    else:main()
