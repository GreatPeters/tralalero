"""A three-picture explanation of each concept, with the full plan retained separately."""
import html
import json
import shutil
from pathlib import Path

BASE=Path(__file__).resolve().parents[1]
ROOT=BASE/'outputs/highway-concepts-2026-09-26'
SITE=ROOT/'site'
OUT=SITE/'simple'
OUT.mkdir(exist_ok=True)
data=json.loads((ROOT/'simple.json').read_text(encoding='utf-8'))
assert [c['id'] for c in data['concepts']]==[1,2,3,4,5]
assert all(len(c['steps'])==3 and len(c['choices'])==2 and len(c['flow'])==5 for c in data['concepts'])

def group(x,y,s,k=1):return f'<g transform="translate({x} {y}) scale({k})">{s}</g>'
def text(x,y,s,size=22,color='#203e3b'):
    return f'<text x="{x}" y="{y}" text-anchor="middle" font-size="{size}" fill="{color}" font-weight="700">{html.escape(s)}</text>'
def label(x,y,s,color='#244b3a',w=100):
    return f'<rect x="{x-w/2}" y="{y-25}" width="{w}" height="34" rx="8" fill="#fffef4" stroke="{color}" stroke-width="2"/>'+text(x,y,s,20,color)
def arrow(x1,y1,x2,y2):return f'<path d="M{x1} {y1}L{x2} {y2}" stroke="#16805a" stroke-width="10" fill="none" marker-end="url(#tip)"/>'
def shark(x,y):
    return group(x,y,'<ellipse cx="0" cy="30" rx="34" ry="16" fill="#294e49" opacity=".13"/><path d="M-13 24l-18 22 31-10 31 10-18-22" fill="#91b6c1" stroke="#254d59" stroke-width="3"/><ellipse rx="24" ry="39" fill="#9fc2cb" stroke="#254d59" stroke-width="4"/><path d="M-18-1l-27 21 31-6M18-1l27 21-31-6" fill="#9fc2cb" stroke="#254d59" stroke-width="3"/><circle cx="-8" cy="-19" r="3.8" fill="#203f4b"/><circle cx="8" cy="-19" r="3.8" fill="#203f4b"/><ellipse cx="-18" cy="33" rx="13" ry="7" fill="#35aee0" stroke="white" stroke-width="4"/><ellipse cx="18" cy="33" rx="13" ry="7" fill="#35aee0" stroke="white" stroke-width="4"/>')
def enemy(x,y):return group(x,y,'<ellipse cy="38" rx="22" ry="9" fill="#33463d" opacity=".13"/><path d="M-9 18l-6 23m24-23 6 23" stroke="#4b5964" stroke-width="10"/><rect x="-18" y="-11" width="36" height="36" rx="7" fill="#db7146"/><circle cy="-26" r="15" fill="#f0c6a3"/><path d="M-20-25q20-24 40 0" fill="#3d5260"/><path d="M18 0l13 18" stroke="#f0c6a3" stroke-width="9"/>')
def car(x,y,kind='bus'):
    color={'bus':'#287b95','truck':'#4d83bd','tow':'#cd8031'}[kind]
    s=f'<rect x="-42" y="-75" width="84" height="150" rx="16" fill="{color}" stroke="#f5eedb" stroke-width="4"/><rect x="-31" y="-59" width="62" height="28" rx="5" fill="#d2ebef"/>'
    if kind=='bus':s+=''.join(f'<rect x="{a}" y="{b}" width="13" height="22" rx="3" fill="#c4e0e7"/>' for a in [-33,20] for b in [-17,16,49])
    else:s+='<rect x="-31" y="-18" width="62" height="78" rx="5" fill="#e0e2d2"/>'
    if kind=='truck':s+='<path d="M-18 21h36m-18-18v36" stroke="#4781a7" stroke-width="9"/>'
    return group(x,y,s)
def icon(x,y,kind,k=1):
    if kind=='heal':s='<circle r="30" fill="#63a474"/><path d="M-16 0h32M0-16v32" stroke="white" stroke-width="11"/>'
    elif kind=='coin':s='<circle r="28" fill="#efc354" stroke="#a87a28" stroke-width="4"/><circle r="20" fill="none" stroke="#ffe7a2" stroke-width="3"/><path d="M0-13v26" stroke="#aa7e2a" stroke-width="6"/>'
    elif kind=='switch':s='<rect x="-30" y="-33" width="60" height="66" rx="8" fill="#dfaf4e" stroke="#fff9e2" stroke-width="4"/>'+text(0,11,'1',35,'#fff')
    elif kind=='helper':return group(x,y,shark(-22,0)+shark(27,8),k*.55)
    elif kind=='wrench':s='<circle r="30" fill="#e4cf9b"/><path d="M-15 18L12-12M4-22q19-3 18 14" stroke="#687d8c" stroke-width="11" fill="none"/>'
    elif kind=='bus':return group(x,y,car(0,0),k*.48)
    elif kind=='gun':s='<path d="M-34-9h53v23H-5l-9 22h-19l9-25h-10z" fill="#347b9e" stroke="white" stroke-width="3"/><path d="M26-17l16-6m-11 22h17m-23 12 16 8" stroke="#d5a137" stroke-width="4"/>'
    else:s=''
    return group(x,y,s,k)
def bullets(x,y1,y2):return ''.join(f'<ellipse cx="{x}" cy="{y}" rx="5" ry="10" fill="#78dfe5" stroke="#e9ffff" stroke-width="2"/>' for y in range(y2,y1,25))
def start(title,desc,w=500,h=410):
    return f'<svg xmlns="http://www.w3.org/2000/svg" width="{w}" height="{h}" viewBox="0 0 {w} {h}" role="img"><title>{html.escape(title)}</title><desc>{html.escape(desc)}</desc><defs><marker id="tip" markerWidth="6" markerHeight="6" refX="4.8" refY="3" orient="auto"><path d="M0 0L5 3L0 6z" fill="#16805a"/></marker><clipPath id="clip"><rect width="500" height="410" rx="18"/></clipPath></defs><style>text{{font-family:"Malgun Gothic","Noto Sans KR",sans-serif}}</style><g clip-path="url(#clip)"><rect width="500" height="410" fill="#e9efdf"/>'
def road(fork=False):
    if fork:return '<path d="M250 440V255Q250 190 130-35M250 255Q250 190 370-35" fill="none" stroke="#7d8f86" stroke-width="150"/><path d="M236 430V260Q236 186 118-30" fill="none" stroke="#e0a1b2" stroke-width="5"/><path d="M264 430V260Q264 186 382-30" fill="none" stroke="#bddd94" stroke-width="5"/>'
    return '<rect x="47" y="-10" width="406" height="440" fill="#7d8f86"/><path d="M70 0V410M430 0V410" stroke="#e7ede0" stroke-width="4"/><path d="M188 0V410M312 0V410" stroke="#e7ede0" stroke-width="3" stroke-dasharray="19 17"/>'

for c in data['concepts']:
    pid=c['id']
    for i,(title,desc) in enumerate(c['steps']):
        s=start(title,desc)+road(pid==3 and i<2)
        if pid==1:
            s+=f'<rect x="87" y="90" width="86" height="295" rx="18" fill="#9be0b7" opacity=".4"/>'
            s+=car(250 if i==0 else 370,130 if i==0 else 135 if i==1 else 56)
            s+=shark(250 if i==0 else 130,332)
            if i==0:s+=arrow(283,231,382,231)+label(252,41,'버스')+label(249,397,'나',w=55)
            if i==1:s+=arrow(260,316,164,316)+label(246,286,'왼쪽으로',w=122)
            if i==2:s+=enemy(130,107)+bullets(130,270,165)+label(130,51,'적',w=55)+label(290,262,'공격!',w=90)
        elif pid==2:
            s+=car(250 if i!=1 else 232,136,'truck')+shark(370 if i==1 else 250,333)
            s+=label(244,40,'지킬 트럭',w=136)
            if i<2:s+=enemy(372,120)+label(373,62,'적',w=55)
            if i==0:s+='<path d="M347 118L296 118" stroke="#d9654e" stroke-width="7" stroke-dasharray="10 8"/>'+label(128,258,'트럭이 위험!',w=156)
            if i==1:s+=bullets(370,273,174)+arrow(251,325,326,325)
            if i==2:s+=icon(250,252,'heal',.84)+label(370,258,'보급',w=74)
        elif pid==3:
            if i<2:
                s+=icon(134,91,'helper',1.2)+label(131,157,'동료',w=83)+icon(369,91,'heal')+label(369,157,'회복',w=83)
                s+=shark(250 if i==0 else 347,334 if i==0 else 246)
                if i==1:s+=arrow(249,322,317,280)+label(140,296,'체력이 부족해요',w=180)
            else:
                s+=shark(250,296)+icon(250,162,'heal',1.2)+label(250,98,'체력 회복',w=132)
                s+='<rect x="192" y="230" width="116" height="12" rx="6" fill="#d6e0d0"/><rect x="192" y="230" width="92" height="12" rx="6" fill="#54a77d"/>'
        elif pid==4:
            s+='<rect x="195" y="108" width="15" height="44" rx="4" fill="#d9ae57"/><rect x="294" y="108" width="15" height="44" rx="4" fill="#d9ae57"/>'
            s+=f'<path d="M204 115L{210 if i==2 else 294} {52 if i==2 else 115}" stroke="#efb65b" stroke-width="13"/>'
            s+=icon(130,205,'switch',.95)+shark(130 if i==1 else 250,334 if i<2 else 229)
            if i==0:s+=label(250,70,'닫힌 문',w=110)+label(124,269,'쏘는 장치',w=134)+text(250,135,'1',24,'#fff')
            if i==1:s+=bullets(130,279,246)+label(281,202,'장치를 쏴요',w=156)+arrow(242,330,172,330)
            if i==2:s+=label(290,73,'열렸어요',w=120)+arrow(250,174,250,105)+icon(358,140,'coin',.7)
        else:
            s+=car(250,112,'tow')+shark(130 if i==1 else 250,333)
            if i<2:
                s+='<rect x="318" y="179" width="107" height="195" rx="15" fill="#ec9078" opacity=".46"/><path d="M271 135L369 216" stroke="#e4ad51" stroke-width="20"/><path d="M367 218Q421 260 382 324" stroke="#d96846" stroke-width="8" fill="none" stroke-dasharray="11 9"/>'
                if i==0:s+=label(363,167,'공격할 곳',w=129)
                else:s+=arrow(255,320,173,320)+label(184,278,'왼쪽으로',w=125)
            else:
                s+='<rect x="226" y="176" width="48" height="27" rx="6" fill="#ffe282" stroke="#fffdf0" stroke-width="4"/>'+bullets(250,279,216)+label(362,206,'약점!',w=85)
            s+=label(250,35,'레커차',w=100)
        s+='</g></svg>'
        (OUT/f'idea-{pid}-{i+1}.svg').write_text(s,encoding='utf-8')
    for j,choice in enumerate(c['choices']):
        s=start(choice['name'],choice['text'],260,150)+'<path d="M106 160L127 0M154 160L133 0" stroke="#c9d8ba" stroke-width="46"/>'+icon(130,73,choice['icon'],1.15)+'</g></svg>'
        (OUT/f'idea-{pid}-choice-{j+1}.svg').write_text(s,encoding='utf-8')

for name in ['html','css','js']:
    shutil.copy2(BASE/f'tools/highway-concepts-simple.{name}',SITE/('index.html' if name=='html' else f'simple.{name}'))
(SITE/'simple-data.js').write_text('const SIMPLE_PLANS = '+json.dumps(data['concepts'],ensure_ascii=False)+';',encoding='utf-8')

doc=BASE/'docs/ideation/2026-09-26-highway-five-concepts.md'
md=['# 고속도로 개편 5안 — 쉬운 그림 설명','','기획 그림입니다. 게임에는 적용하지 않았습니다.','','[홈페이지에서 보기](http://127.0.0.1:8776/)','']
for c in data['concepts']:
    md += [f'## {c["id"]}. {c["name"]}','',c['headline'],'']
    for i,(title,desc) in enumerate(c['steps']):
        md += [f'### {i+1}. {title}','',f'![{title}](../../outputs/highway-concepts-2026-09-26/site/simple/idea-{c["id"]}-{i+1}.svg)','',desc,'']
    md += ['**재미있는 점:** '+c['fun'],'','**한 판의 흐름:** '+' → '.join(c['flow']),'','**갈림길의 한 가지 예**','']
    md += ['- '+x['side']+' · '+x['name']+': '+x['text'] for x in c['choices']]
    md += ['','**실수하면:** '+c['mistake'],'','**재사용:** '+c['reuse'],'','**새 작업:** '+c['new'],'']
md += ['[원래의 상세 기획](2026-09-26-highway-five-concepts-detailed.md)','']
doc.write_text('\n'.join(md),encoding='utf-8')
(SITE/'고속도로-개편-5안.md').write_text('\n'.join(md).replace('../../outputs/highway-concepts-2026-09-26/site/','').replace('2026-09-26-highway-five-concepts-detailed.md','고속도로-개편-5안-상세.md'),encoding='utf-8')
print(json.dumps({'simplePlans':5,'mainIllustrations':15,'choiceIllustrations':10,'gameApplied':False}))
