"""Generate precise, illustrated SVG storyboards from the five approved-for-review concepts.
These are proposal diagrams, not generated gameplay screenshots or Unity assets.
"""
from pathlib import Path
import html
import json
import zipfile

BASE = Path(__file__).resolve().parents[1]
ROOT = BASE / 'outputs/highway-concepts-2026-09-26'
OUT = ROOT / 'site/flow'
OUT.mkdir(parents=True, exist_ok=True)
DATA = json.loads((ROOT / 'concepts.json').read_text(encoding='utf-8'))

CAPTIONS = {
    1: [('차 한 대의 이동을 읽기','빈 곳으로 이동 → 약한 적 처치'),('버스가 비운 구역에 들어가기','3초 사격 창 → 코인'),('차 뒤 / 넓은 차로 중 선택','서로 다른 이동 패턴 → 합류'),('왼쪽으로 통나무 피하기','낙하 종료 → 가운데서 반격'),('공사 화살표와 요금소 통과','도전 / 여유 통로 → 보너스'),('배운 사건을 차례대로 돌파','마지막 교전 → 휴게소 출구')],
    2: [('보급차 뒤에 합류','트럭을 노리는 적부터 읽기'),('좌우의 투척수를 먼저 요격','내구도 보존 → 보급 받기'),('보급 / 비상 정비 중 선택','수리 또는 지원군 → 합류'),('방패 적과 투척수의 순서 판단','상어와 트럭을 번갈아 보호'),('콘을 치우며 호송차 통과','길이 열리면 정상 속도 복귀'),('마지막 습격을 막고 진입','트럭 생존: 추가 보급 / 손실: 기본 완주')],
    3: [('교전하며 현재 상태 만들기','체력·화력·지원군 확인'),('지원군 / 회복 중 선택','짧은 사건 15초 → 합류'),('획득한 보상으로 공통 교전','예: 지원군을 골랐다면 함께 공격'),('코인 / 마지막 통로 해제','현재 선택을 마지막 장면에 연결'),('공격 보급 / 응급 회복','부족한 것을 채우고 합류'),('모든 경로가 같은 검문소로','그림 예시: 지원군 + 통로 해제 선택')],
    4: [('1번 장치와 1번 문 연결','조준 유지 → 문 열기'),('작업차를 옮겨 사격선 확보','열린 공간으로 이동 → 적 처치'),('장치 작업로 / 직접 교전로','서로 다른 해결 방법 → 합류'),('화살표와 가동 장치를 함께 읽기','보이는 연결대로 통로 개방'),('방호판 작동 / 넓은 회피로','통나무를 보내고 공격 위치 확보'),('1번 → 2번 장치 순서 해결','놓쳐도 기본 통로 유지 → 완주')],
    5: [('보급을 싣고 도망가는 레커','목표 확인 → 추격 시작'),('타이어를 보내고 약점 공격','후면 잠금장치 1단계 해제'),('가까운 추격 / 회복 쉼터','다음 대결 시각은 동일'),('레커 암을 반대쪽으로 회피','암 복귀 중 후면 약점 사격'),('화력 / 수리 보급 중 선택','마지막 대결의 준비를 바꾸기'),('타이어 → 암 → 약점 두 차례','레커 고장·갓길 정차 → 보급 회수')]
}
BRANCHES = {
    1: [('① 75–120초','큰 차 뒤','이동 1회 · 짧은 사격','bus','넓은 차로','이동 2회 · 긴 사격','gun','120초 합류'),('② 170–230초','연속 무피격','성공하면 코인 추가','coin','여유 통로','사건 적게 · 기본 보상','shield','230초 합류')],
    2: [('① 75–120초','보급차 합류','지원군 · 추가 습격','helper','비상 정비','방어 성공 → 내구도 +1','wrench','120초 합류'),('② 170–230초','공격 보급','화력으로 먼저 요격','gun','보호 대열','트럭 1회 보호막','shield','230초 합류')],
    3: [('① 60–75초','물류 통로','지원군 · 화물 위험','helper','졸음쉼터','회복 15% 초안','heal','75초 합류'),('② 150–170초','물류차 구간','교전 → 코인','coin','공사 관리로','마지막 통로 해제','switch','170초 합류'),('③ 210–230초','공격 보급','마지막 구간 화력','gun','응급 보급','현재 체력 회복','heal','230초 합류')],
    4: [('① 75–120초','장치 작업로','장치 2개 · 긴 사격 창','switch','직접 교전로','장치 없이 적 추가','enemy','120초 합류'),('② 170–230초','방호 장치','통나무 막고 사격','shield','넓은 회피로','크게 이동 · 기본 보상','arrow','230초 합류')],
    5: [('① 75–120초','가까운 추격','추가 교전 · 코인','coin','회복 쉼터','회복 후 같은 대결','heal','120초 재회'),('② 170–230초','화력 보급','약점 창 1회 보조 사격','gun','수리 보급','회복 · 첫 사고 완화','wrench','230초 재회')]
}

def text(x, y, value, size=20, color='#234735', weight=400, anchor='start'):
    return f'<text x="{x}" y="{y}" font-size="{size}" fill="{color}" font-weight="{weight}" text-anchor="{anchor}">{html.escape(value)}</text>'

def group(x,y,content,scale=1):
    return f'<g transform="translate({x} {y}) scale({scale})">{content}</g>'

def arrow(x1,y1,x2,y2,color='#326849',width=5):
    return f'<path d="M{x1} {y1}L{x2} {y2}" stroke="{color}" stroke-width="{width}" fill="none" marker-end="url(#arrow)"/>'

def vehicle(x,y,kind='truck',scale=1):
    color = {'bus':'#357a85','truck':'#4d87a5','escort':'#326585','tow':'#b77637','log':'#976a3c','arrowtruck':'#e1a638'}.get(kind,'#6a8b79')
    body = '<ellipse cx="1" cy="8" rx="31" ry="53" fill="#27493b" opacity=".15"/>'
    body += f'<rect x="-27" y="-43" width="54" height="88" rx="9" fill="{color}" stroke="#f5f0d9" stroke-width="3"/>'
    body += '<rect x="-19" y="-32" width="38" height="17" rx="4" fill="#c8e4e3"/><path d="M-27 -25h-6m60 0h6" stroke="#304e43" stroke-width="4"/>'
    if kind=='bus':
        body += ''.join(f'<rect x="{a}" y="{b}" width="7" height="12" fill="#b9d9d7"/>' for a in [-21,14] for b in [-6,11,28])
    if kind in ['truck','escort']:
        body += '<rect x="-21" y="-5" width="42" height="44" rx="3" fill="#c4d0c8"/>'
        if kind=='escort': body += '<path d="M-10 17h20m-10-10v20" stroke="#326b74" stroke-width="6"/>'
    if kind=='log':
        body += ''.join(f'<rect x="{a}" y="-4" width="9" height="44" rx="4" fill="#c99456" stroke="#7a522c" stroke-width="2"/>' for a in [-20,-6,8])
    if kind=='tow': body += '<path d="M0 8L36 41" stroke="#e0b862" stroke-width="10"/><path d="M36 41v12q0 11-12 8" fill="none" stroke="#4c5a4c" stroke-width="5"/>'
    if kind=='arrowtruck': body += '<rect x="-24" y="2" width="48" height="30" rx="3" fill="#2a4540"/><path d="M-14 17h28m-5-7 8 7-8 7" fill="none" stroke="#f4d879" stroke-width="4"/>'
    return group(x,y,body,scale)

def shark(x,y,scale=1):
    s='<ellipse cx="1" cy="16" rx="28" ry="14" fill="#264b3e" opacity=".14"/><path d="M-10 20l-17 19 27-9 27 9-17-19" fill="#94b4b8" stroke="#35585a" stroke-width="2"/><ellipse rx="18" ry="29" fill="#a5c0c0" stroke="#35585a" stroke-width="3"/><path d="M-14-1l-22 18 24-5M14-1l22 18-24-5" fill="#a5c0c0" stroke="#35585a" stroke-width="2"/><ellipse cx="-15" cy="25" rx="9" ry="5" fill="#3badd0" stroke="#f2f5ec" stroke-width="3"/><ellipse cx="15" cy="25" rx="9" ry="5" fill="#3badd0" stroke="#f2f5ec" stroke-width="3"/><circle cx="-6" cy="-14" r="2.6" fill="#264340"/><circle cx="6" cy="-14" r="2.6" fill="#264340"/>'
    return group(x,y,s,scale)

def person(x,y,scale=1,shield=False):
    s='<ellipse cy="21" rx="16" ry="7" fill="#294b40" opacity=".16"/><path d="M-5 10l-5 13m15-13 5 13" stroke="#344f4b" stroke-width="6"/><rect x="-11" y="-9" width="22" height="24" rx="5" fill="#b16843"/><circle cy="-18" r="10" fill="#e6c6a0"/><path d="M-13-18q13-17 26 0" fill="#314e47"/>'
    if shield: s+='<path d="M2-5h21v21L12 27 2 16z" fill="#91b7bc" stroke="#315958" stroke-width="2"/>'
    return group(x,y,s,scale)

def icon(x,y,kind,scale=1):
    if kind in ['truck','bus','tow','log','escort','arrowtruck']: return vehicle(x,y,kind,scale*.64)
    if kind=='enemy': return person(x,y,scale)
    if kind=='helper': return shark(x-11,y,scale*.47)+shark(x+13,y+9,scale*.47)
    s=''
    if kind=='heal': s='<circle r="22" fill="#85a862"/><path d="M-12 0h24M0-12v24" stroke="#f8f3d9" stroke-width="8"/>'
    elif kind=='coin': s='<circle r="21" fill="#edc764" stroke="#af8739" stroke-width="3"/><circle r="14" fill="none" stroke="#fff2b9" stroke-width="2"/><path d="M0-9v18m-5-14h10m-10 10h10" stroke="#a77b2b" stroke-width="3"/>'
    elif kind=='shield': s='<path d="M-20-22h40v25q-3 16-20 25Q-17 19-20 3z" fill="#6f9fa8" stroke="#eaf0dd" stroke-width="3"/><path d="M-9 0l7 8 13-19" stroke="#edf5e6" stroke-width="4" fill="none"/>'
    elif kind=='gun': s='<path d="M-21-6h34v14h-17l-5 13h-11l5-16h-6z" fill="#45738b" stroke="#edf1db" stroke-width="2"/><path d="M18-11l11-5m-9 16h13m-15 10 11 5" stroke="#d0a44d" stroke-width="3"/>'
    elif kind=='wrench': s='<circle r="23" fill="#d9c698"/><path d="M-9 16L12-12M5-19q14-2 13 12" fill="none" stroke="#5c705e" stroke-width="9"/>'
    elif kind=='switch': s='<rect x="-21" y="-25" width="42" height="50" rx="6" fill="#d19d48" stroke="#f4edd3" stroke-width="3"/>'+text(0,9,'1',28,'#fbf9e9',700,'middle')
    elif kind=='arrow': s='<circle r="23" fill="#9aaf82"/>'+arrow(-14,8,12,-9,'#fff6df',4)
    elif kind=='flag': s='<path d="M-17 26v-54" stroke="#526d57" stroke-width="4"/><path d="M-15-26h40v27h-40z" fill="#f3ebd5"/>'+''.join(f'<rect x="{a}" y="{b}" width="10" height="9" fill="#4b7750"/>' for a,b in [(-15,-26),(5,-26),(-5,-17),(15,-17),(-15,-8),(5,-8)])
    elif kind=='cone': s='<path d="M0-24L15 18h-30z" fill="#d88846" stroke="#f9efd5" stroke-width="2"/><path d="M-7-1h14m-18 12h22" stroke="#f2ead4" stroke-width="5"/><rect x="-20" y="17" width="40" height="7" rx="3" fill="#4e6552"/>'
    return group(x,y,s,scale)

def trees():
    return ''.join(group(x,y,'<rect x="-3" y="10" width="6" height="14" fill="#987748"/><path d="M0-29L-22 10h44z" fill="#759167"/><path d="M0-15L-25 19h50z" fill="#83a272"/>',s) for x,y,s in [(30,72,.9),(410,143,1),(60,200,.7),(392,42,.8)])

def road(fork=False,city=False):
    s='<rect width="440" height="235" rx="10" fill="#dfe8ce"/>'
    if city:
        for x,y in [(25,10),(355,8),(45,125),(365,148)]:
            s+=f'<rect x="{x}" y="{y}" width="44" height="68" rx="3" fill="#b9c9bd"/>'
            s+=''.join(f'<rect x="{x+8+a*15}" y="{y+9+b*16}" width="9" height="10" fill="#e8eee0"/>' for a in range(2) for b in range(3))
    else:s+=trees()
    if fork:
        s+='<path d="M220 265V166Q220 111 122-25M220 166Q220 111 318-25" stroke="#64796b" stroke-width="116" fill="none"/><path d="M205 250V165Q205 105 115-25" stroke="#ce8fac" stroke-width="4" fill="none"/><path d="M235 250V165Q235 105 325-25" stroke="#c0d388" stroke-width="4" fill="none"/>'
    else:
        s+='<path d="M117-15L97 250H340L322-15z" fill="#65796b"/><path d="M127 0L110 235M312 0L328 235" stroke="#e5e8d6" stroke-width="2"/><path d="M188 0L181 235M256 0L266 235" stroke="#e5e8d6" stroke-width="3" stroke-dasharray="12 12"/>'
    return s

def bullets(x,y1,y2):
    return ''.join(f'<ellipse cx="{x}" cy="{y}" rx="3.5" ry="7" fill="#82d6d9"/>' for y in range(int(y2),int(y1),18))

def gate(y,open=False):
    return f'<rect x="111" y="{y-7}" width="14" height="27" rx="3" fill="#d9c583"/><rect x="316" y="{y-7}" width="14" height="27" rx="3" fill="#d9c583"/><path d="M120 {y}L{135 if open else 211} {y-63 if open else y}" stroke="#eee3bb" stroke-width="8"/><path d="M322 {y}L{308 if open else 231} {y-63 if open else y}" stroke="#d99a59" stroke-width="8"/>'

def scene(pid,i):
    isfork=(pid==3 and i in [1,3,4]) or (pid!=3 and i in [2,4] and not(pid==2 and i==4))
    s=road(isfork, i==0)
    if isfork:
        idx=([1,3,4].index(i) if pid==3 else [2,4].index(i))
        branch=BRANCHES[pid][idx]
        s+=icon(145,54,branch[3],1.05)+icon(299,54,branch[6],1.05)+shark(220,191,.7)
        s+='<rect x="104" y="92" width="77" height="25" rx="6" fill="#f4efd8"/><rect x="265" y="92" width="77" height="25" rx="6" fill="#f4efd8"/>'
        s+=text(142,110,'왼쪽',13,'#385c42',700,'middle')+text(303,110,'오른쪽',13,'#385c42',700,'middle')
        return '<g clip-path="url(#sceneClip)">'+s+'</g>'
    s+=shark(175 if pid==1 and i==3 else 220,188,.75)
    if pid==1:
        if i==0:s+=vehicle(270,72,'truck',.67)+arrow(212,158,153,146)+person(216,30,.65)
        elif i==1:s+=vehicle(278,104,'bus',.84)+person(150,46,.8)+bullets(151,164,77)+arrow(215,170,151,170)
        elif i==3:
            s+=vehicle(250,44,'log',.72)
            for x,y in [(230,118),(280,156),(247,200)]:s+=group(x,y,'<rect x="-21" y="-7" width="42" height="14" rx="6" fill="#ba8448" stroke="#6f5837" stroke-width="2"/>')
            s+=arrow(159,168,156,76)
        elif i==5:s+=gate(87,True)+icon(220,49,'flag',1.2)+icon(151,128,'coin',.6)+icon(289,128,'heal',.6)
    elif pid==2:
        s+=vehicle(215 if i!=1 else 185,72,'escort',.86)
        if i==0:s+=icon(298,134,'shield',.65)+arrow(210,150,210,124)
        elif i==1:s+=person(288,64,.9)+bullets(280,181,95)+arrow(218,173,279,162)
        elif i==3:s+=person(141,120,.8,True)+person(289,110,.8)+icon(315,165,'shield',.6)
        elif i==4:s+=icon(182,119,'cone',.64)+icon(239,122,'cone',.64)+person(296,93,.78)+bullets(278,168,117)
        elif i==5:s+=icon(295,65,'flag',1)+icon(279,153,'heal',.73)+icon(140,121,'coin',.65)
        s+=text(217,12,'● ● ●',12,'#365f79',700,'middle')
    elif pid==3:
        if i==0:s+=person(168,65,.8)+person(279,95,.8)+icon(277,145,'coin',.6)+bullets(218,155,60)
        elif i==2:s+=person(215,38,.85)+shark(166,163,.43)+shark(277,163,.43)+bullets(216,143,80)+icon(330,85,'shield',.6)
        elif i==5:s+=gate(90,True)+icon(220,36,'flag',1)+shark(165,156,.43)+shark(278,156,.43)+icon(280,110,'switch',.65)
    elif pid==4:
        if i==0:s+=gate(59)+icon(220,114,'switch',.66)+bullets(220,156,144)
        elif i==1:s+=vehicle(280,76,'arrowtruck',.77)+icon(215,111,'switch',.57)+person(163,35,.65)+arrow(259,78,314,80)
        elif i==3:s+=vehicle(281,53,'arrowtruck',.67)+icon(175,112,'switch',.64)+gate(52,True)+bullets(176,161,141)
        elif i==5:s+=gate(50,True)+icon(160,93,'switch',.6)+icon(280,132,'switch',.6)+icon(222,26,'flag',.68)+arrow(186,136,253,149)
    elif pid==5:
        s+=vehicle(220,57,'tow',1.03)
        if i==0:s+=icon(290,87,'coin',.65)+arrow(222,146,222,109)
        elif i==1:
            for x,y in [(171,116),(278,141)]:s+=f'<circle cx="{x}" cy="{y}" r="14" fill="#384e42" stroke="#bcc2a9" stroke-width="6"/>'
            s+=bullets(221,151,110)
        elif i==3:s+='<path d="M246 85L299 136" stroke="#d8b159" stroke-width="10"/><path d="M286 122Q328 155 299 194" fill="none" stroke="#d98c52" stroke-width="6" stroke-dasharray="7 6"/>'+arrow(211,165,159,153)
        elif i==5:s+=icon(295,60,'flag',1)+icon(176,124,'coin',.7)+icon(267,145,'heal',.65)+'<path d="M240 20l10-13m2 32 15-1" stroke="#916750" stroke-width="4"/>'
    return '<g clip-path="url(#sceneClip)">'+s+'</g>'

def header(width,height,title,desc):
    return f'<svg xmlns="http://www.w3.org/2000/svg" width="{width}" height="{height}" viewBox="0 0 {width} {height}" role="img"><title>{html.escape(title)}</title><desc>{html.escape(desc)}</desc><defs><clipPath id="sceneClip"><rect width="440" height="235" rx="10"/></clipPath><marker id="arrow" markerWidth="8" markerHeight="8" refX="6" refY="3" orient="auto"><path d="M0 0L6 3L0 6z" fill="#527957"/></marker></defs><style>text{{font-family:"Malgun Gothic","Noto Sans KR",sans-serif}}</style>'

def build(c):
    pid=c['id'];color=c['color']
    svg=header(1500,1640,c['title']+' · 전체 흐름', '시작부터 300초 종료까지 6장면과 좌우 분기, 보상, 합류를 그린 미적용 기획도')
    svg+='<rect width="1500" height="1640" fill="#f5f3e9"/>'
    svg+=text(45,45,f'0{pid} / 300초 전체 흐름',20,color,700)+text(45,98,c['title'],43,color,700)+text(45,134,c['subtitle'],21,'#5c725b')
    svg+=text(1455,45,'기획 그림 · 게임 미적용',17,'#667860',400,'end')
    svg+=text(1455,96,'읽는 순서  ① → ② → ③ ↴',18,'#667860',400,'end')+text(1455,126,'④ → ⑤ → ⑥',18,'#667860',400,'end')
    coords=[(45,184),(535,184),(1025,184),(45,658),(535,658),(1025,658)]
    for a,b in [(0,1),(1,2),(3,4),(4,5)]:
        x,y=coords[a];xx,yy=coords[b];svg+=arrow(x+447,y+183,xx-10,yy+183,width=4)
    svg+='<path d="M1245 599V622Q1245 633 1225 633H285Q265 633 265 647" fill="none" stroke="#78906d" stroke-width="4" marker-end="url(#arrow)"/>'
    svg+=text(750,623,'앞에서 익힌 행동을 후반에서 바꿔 쓰기',17,'#647a5b',400,'middle')
    for i,(x,y) in enumerate(coords):
        short=c['timeline'][i][1].split(' / ')[0]
        p=f'<rect width="430" height="414" rx="13" fill="#fffef7" stroke="#c0ceb5" stroke-width="2"/>'
        p+=f'<circle cx="31" cy="30" r="17" fill="{color}"/>'+text(31,37,str(i+1),20,'#fff',700,'middle')
        p+=text(62,37,c['timeline'][i][0],19,color,700)+text(410,37,short,17,'#526b50',400,'end')
        p+=group(10,56,scene(pid,i),.93)
        p+=text(21,306,CAPTIONS[pid][i][0],21,color,700)
        p+=text(21,340,CAPTIONS[pid][i][1],16,'#5d7258')
        branch_index=([1,3,4].index(i)+1 if pid==3 and i in [1,3,4] else [2,4].index(i)+1 if pid!=3 and i in [2,4] else None)
        if branch_index:
            p+=f'<rect x="20" y="363" width="390" height="31" rx="6" fill="#e8ecd9"/>'+text(215,385,f'분기 {branch_index} · 아래의 두 경로 중 선택',16,color,600,'middle')
        else:p+=text(21,385,'↑ 전진 방향   ·   그림은 대표 사건을 요약',13,'#849078')
        svg+=group(x,y,p)
        single=header(440,235,f'{c["title"]} · {c["timeline"][i][0]}',CAPTIONS[pid][i][0])+scene(pid,i)+'</svg>'
        (OUT/f'plan-{pid}-scene-{i+1}.svg').write_text(single,encoding='utf-8')
    svg+=text(45,1129,'갈라져도 같은 한 판으로 다시 합류',27,color,700)+text(45,1162,'좌우의 보상·부담을 보고 선택 · 모든 경로에 교전은 남음',17,'#6f8064')
    branches=BRANCHES[pid];w=1410/len(branches)
    for j,b in enumerate(branches):
        x=45+j*w;mid=w/2;left=mid-106;right=mid+106
        p=f'<rect x="5" width="{w-15}" height="349" rx="10" fill="#edf0e1" stroke="#c4d0b8"/>'
        p+=text(mid,33,b[0],19,color,700,'middle')
        p+=f'<path d="M{mid} 48V63Q{mid} 80 {left} 98M{mid} 63Q{mid} 80 {right} 98M{left} 237Q{left} 274 {mid} 288M{right} 237Q{right} 274 {mid} 288" fill="none" stroke="#9aae88" stroke-width="4"/>'
        p+=icon(left,122,b[3],.78)+icon(right,122,b[6],.78)
        p+=text(left,169,'왼쪽 · '+b[1],17,color,700,'middle')+text(right,169,'오른쪽 · '+b[4],17,color,700,'middle')
        p+=text(left,199,b[2],14,'#64765a',400,'middle')+text(right,199,b[5],14,'#64765a',400,'middle')
        p+=f'<rect x="{mid-90}" y="286" width="180" height="40" rx="20" fill="{color}"/>'+text(mid,313,b[7],18,'#fff',700,'middle')
        svg+=group(x,1190,p)
    svg+=text(45,1593,'장면·분기·보상 연결을 설명하는 기획 그림입니다. 실제 맵 배치, 거리 비율, 플레이 화면이 아닙니다.',17,'#6c7e61')
    svg+='</svg>'
    path=OUT/f'plan-{pid}.svg';path.write_text(svg,encoding='utf-8')
    branch_svg=svg.replace('width="1500" height="1640" viewBox="0 0 1500 1640"','width="1500" height="550" viewBox="0 1090 1500 550"',1)
    (OUT/f'plan-{pid}-branches.svg').write_text(branch_svg,encoding='utf-8')
    return {'id':pid,'src':f'flow/plan-{pid}.svg','branches':f'flow/plan-{pid}-branches.svg','captions':CAPTIONS[pid],'scenes':[f'flow/plan-{pid}-scene-{i+1}.svg' for i in range(6)]}

manifest=[build(c) for c in DATA['concepts']]
(ROOT/'site/flow-data.js').write_text('const FLOW_ART = '+json.dumps(manifest,ensure_ascii=False)+';',encoding='utf-8')
with zipfile.ZipFile(ROOT/'site/고속도로-전체흐름-그림5장.zip','w',zipfile.ZIP_DEFLATED) as z:
    for c in DATA['concepts']:z.write(OUT/f'plan-{c["id"]}.svg',f'{c["id"]}-{c["title"]}.svg')
print(json.dumps({'fullFlowSheets':5,'sceneIllustrations':30,'directory':str(OUT)},ensure_ascii=False))
