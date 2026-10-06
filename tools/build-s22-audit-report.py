"""Build a Korean PDF, annotated PNG evidence plates and a local HTML report.

All source pictures are captured Unity frames or decoded production-video frames.
No image generation, content inpainting or screenshot reconstruction is used.
"""
from pathlib import Path
import html
import json
import re
import shutil
import statistics
import textwrap

import markdown
import matplotlib
matplotlib.use('Agg')
import matplotlib.pyplot as plt
from matplotlib import font_manager
from PIL import Image, ImageDraw, ImageFont
from reportlab.pdfgen import canvas
from reportlab.pdfbase import pdfmetrics
from reportlab.pdfbase.ttfonts import TTFont
from reportlab.lib import colors
from reportlab.lib.styles import ParagraphStyle
from reportlab.platypus import SimpleDocTemplate, Paragraph, Spacer, Table, TableStyle, Image as PdfImage, PageBreak, KeepTogether
from reportlab.lib.pagesizes import A4

ROOT=Path(__file__).resolve().parents[1]
DATA=ROOT/'outputs/s22-quality-audit-2026-10-01'
PREVIEW=ROOT/'tmp/image-previews/s22-quality-audit-2026-10-01'
FIG=PREVIEW/'figures'
SITE=DATA/'site'
PDF=ROOT/'output/pdf/s22-performance-visual-audit-2026-10-01.pdf'
DOC=ROOT/'docs/reviews/s22-performance-visual-audit-2026-10-01.md'
INSIDE=ROOT/'tmp/image-previews/noryangjin-claude-fix-2026-09-28/run-s22-inside-023146'
INSIDE1=ROOT/'tmp/image-previews/noryangjin-claude-fix-2026-09-28/run-s22-inside-1x-024549'
OUTSIDE=ROOT/'tmp/image-previews/noryangjin-claude-fix-2026-09-28/run-s22-outside-023456'
REST=ROOT/'outputs/meshy-reststop-2026-09-25/live/audit-s22-interior-20261001'
for p in (FIG,SITE,PDF.parent):p.mkdir(parents=True,exist_ok=True)
REG='C:/Windows/Fonts/malgun.ttf'
BOLD='C:/Windows/Fonts/malgunbd.ttf'
navy='#172C45';teal='#168B89';red='#C84F40';cream='#F3F1EA'
font_manager.fontManager.addfont(REG)
plt.rcParams.update({'font.family':'Malgun Gothic','axes.unicode_minus':False,'font.size':11,'axes.spines.top':False,'axes.spines.right':False})
F=lambda size,b=False:ImageFont.truetype(BOLD if b else REG,size)
manifest=[]

def safe_save(im,path):
    # Evidence originals are immutable. Only derived report figures can be rebuilt.
    im.save(path)

def plate(name,title,subtitle,items,notes):
    width=1600; pad=40; gap=24; top=158; pic_h=1000 if len(items)<=2 else 690
    col=(width-pad*2-gap*(len(items)-1))//len(items)
    out=Image.new('RGB',(width,top+pic_h+210),cream);d=ImageDraw.Draw(out)
    d.rectangle((0,0,width,126),fill=navy);d.text((pad,22),title,font=F(40,True),fill='white');d.text((pad,82),subtitle,font=F(22),fill='#BBD9DC')
    for i,(p,label,box) in enumerate(items):
        original=Image.open(p).convert('RGB');original.thumbnail((col,pic_h-58));x=pad+i*(col+gap)+(col-original.width)//2;y=top+48
        d.text((pad+i*(col+gap),top),label,font=F(23,True),fill=navy);out.paste(original,(x,y))
        if box:
            x1,y1,x2,y2=box
            d.rectangle((x+x1*original.width,y+y1*original.height,x+x2*original.width,y+y2*original.height),outline=red,width=6)
        manifest.append({'figure':name,'source':str(p.relative_to(ROOT)),'caption':label,'crop':False,'annotationBoxNormalized':box})
    y=top+pic_h+22
    for line in notes:d.text((pad,y),line,font=F(25),fill=navy);y+=40
    safe_save(out,FIG/name)

plate('01-character-consistency.png','같은 주인공인가?','실제 영상 원본 프레임 + 실제 Play 모델. 재생성 이미지가 아닙니다.',[
    (PREVIEW/'video/opening-029.5.png','오프닝 29.5초',None),
    (PREVIEW/'video/highway-entry-000.3.png','고속도로 진입 0.3초',None),
    (PREVIEW/'player-live-left.png','실제 플레이 모델 측면',None)],
    ['회색·파란색 / 낮은 몸·직립 몸 / 얼굴·신발 표현이 서로 다릅니다.', '실제 모델은 네 개의 신발 달린 팔다리 + 별도 맨꼬리입니다. U34 요구와 불일치.'])
plate('02-market-camera.png','경사로에서 천장과 빈 간판이 시야를 차지','동일 개편 씬. 배속 차이도 증거에 표시했습니다.',[
    (INSIDE/'f017-071.1.png','2배속 관찰 · 71.1초',(0.03,.57,.98,.99)),
    (INSIDE1/'f030-071.6.png','1배속 재확인 · 71.6초',(0.04,.15,.96,.61))],
    ['2배속에서는 바닥 너머 바다와 아래층 글자가 보입니다.', '1배속에서도 천장/간판 중심의 낮은 구도는 재현됐습니다. 투명 강도는 동일하지 않습니다.'])
wave=next(OUTSIDE.glob('f020-*.png'))
plate('03-feedback-overlap.png','위험과 보상이 같은 영역에서 겹침','테스트 능력치 9999. 정상 성장의 숫자 밀도와 동일하다고 주장하지 않습니다.',[
    (wave,'바깥 부두 파도',(0,.42,1,.87)),
    (INSIDE/'f070-263.0.png','상인 돌격 중 피해·보상',(0.1,.58,.98,.97))],
    ['물 효과가 차선과 주인공을 덮고, 적 체력·피해·코인이 한곳에 겹칩니다.', '위험 판독 우선 / 피해 강조 / 보상 합산 표시로 정보 우선순위를 정리할 필요가 있습니다.'])
plate('04-reststop-sightline.png','휴게소: 큰 표지판과 낮은 대비','거리 945로 이동한 실내 관찰 fixture. 자연 진입·방어전 완료 증거가 아닙니다.',[
    (REST/'frame-007.png','편의점 표지판 통과',(0,.58,1,.75)),
    (REST/'frame-019.png','출구의 창틀/벽면',(0,.49,1,.79))],
    ['큰 간판이 플레이어와 앞길을 가립니다. 흰 타일 위 물 탄환과 숫자도 약합니다.', '왼쪽 상단 Combat Harness 표시는 에디터 도구이며 출시 UI 결함으로 세지 않았습니다.'])
plate('05-highway-density.png','고속도로: 차량과 숫자의 밀집','100초 관찰 구간 중 원본 프레임. 표본 시간은 관찰 시작 기준입니다.',[
    (PREVIEW/'highway-route/frame-048.0.png','밀집 차량 구간 · +48초',None),
    (PREVIEW/'highway-route/frame-064.0.png','곡선/차량 가림 · +64초',None)],
    ['26개 표본: 187~1,088배치 / 127~717 SetPass / 15.7~55.0만 삼각형.', '삼각형 수만으로 가볍다고 판단할 수 없습니다. 반복 재질과 제출 호출을 함께 봐야 합니다.'])
plate('06-auction-and-props.png','경매장: 역할은 보이지만 배치가 기계적','실제 경매장 화면과 현재 프리팹 정적 보기. 자연스러운 손 동작의 증거는 아닙니다.',[
    (INSIDE/'f043-171.4.png','실제 경매장 입구',None),
    (PREVIEW/'models/N19_live_fish_tub-hero.png','현재 활어 대야 프리팹',None),
    (PREVIEW/'models/N09_driven_turret-hero.png','현재 삼륜차 프리팹',None)],
    ['소품의 역할은 식별됩니다. 전면 재생성보다 배열·시선·행동의 관계부터 조정하는 편이 낫습니다.', '반복 대야와 동일한 사람 행렬, 넓은 빈 바닥이 전시용 배치처럼 보이는 것이 핵심입니다.'])
shutil.copy2(PREVIEW/'ui-contact.png',FIG/'07-ui-overview.png')
shutil.copy2(PREVIEW/'video/opening-contact.png',FIG/'08-opening-timeline.png')

ab=json.loads((DATA/'occlusion-ab.json').read_text(encoding='utf-8'))
med=[statistics.median(r['ms'] for r in c['frames']) for c in ab['cases']]
fig,ax=plt.subplots(figsize=(12,5.3),layout='constrained')
ax.bar(range(6),med,color=[red if c['enabled'] else teal for c in ab['cases']],width=.63)
ax.set_xticks(range(6),['ON 1','OFF 1','ON 2','OFF 2','ON 3','OFF 3']);ax.set_ylabel('중앙 프레임 시간 (ms)');ax.set_ylim(0,24)
for i,v in enumerate(med):ax.text(i,v+.45,f'{v:.2f}',ha='center',fontweight='bold')
ax.set_title('노량진 카메라 가림 검사 ON/OFF 반복',loc='left',fontweight='bold',pad=18)
ax.text(.01,.94,'Windows Editor / 정지 시뮬레이션 / 조건별 240프레임\n3~6번: 동일 242배치, 1,335,308삼각형 / S22 FPS가 아님',transform=ax.transAxes,va='top',fontsize=10)
ax.grid(axis='y',alpha=.2);ax.set_axisbelow(True);fig.savefig(FIG/'09-occlusion-ab.png',dpi=180);plt.close(fig)

build=json.loads((DATA/'build-assets.json').read_text(encoding='utf-8'))
total=sum(x['bytes'] for x in build)
groups=[('PNG 출처 텍스처',sum(x['bytes'] for x in build if x['path'].endswith('.png'))),('FBX',sum(x['bytes'] for x in build if x['path'].endswith('.fbx'))),('Shader',sum(x['bytes'] for x in build if x['path'].endswith('.shader')))]
groups.append(('나머지',total-sum(v for _,v in groups)))
fig,ax=plt.subplots(figsize=(12,5.4),layout='constrained');ys=list(range(len(groups)))
ax.barh(ys,[v/1e6 for _,v in groups],color=[red,navy,teal,'#8B98A8']);ax.invert_yaxis();ax.set_yticks(ys,[x for x,_ in groups]);ax.set_xlim(0,1930);ax.set_xlabel('BuildReport packed asset 합계 (MB, decimal)')
for i,(_,v) in enumerate(groups):ax.text(v/1e6+25,i,f'{v/1e6:,.0f} MB  ({v/total:.1%})',va='center')
ax.set_title('빌드 자원에서는 영상보다 텍스처가 큰 비중',loc='left',fontweight='bold',pad=18)
fig.text(.3,.01,'압축 APK 1,526 MB / packed asset 2,223 MB / 런타임 RAM은 별도 지표',fontsize=10)
fig.savefig(FIG/'10-build-composition.png',dpi=180);plt.close(fig)

(DATA/'figure-manifest.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2),encoding='utf-8')
text=DOC.read_text(encoding='utf-8')
insertions={
    '## 결론':['09-occlusion-ab.png'],
    '### P1. 텍스처와 비활성 자산의 빌드 의존성':['10-build-composition.png'],
    '### V1. 주인공이 장면마다 다른 캐릭터처럼 보인다 · 우선순위 높음':['01-character-consistency.png'],
    '### V2. 노량진 경사로: 바닥·간판·카메라의 공간 관계가 읽히지 않는다 · 우선순위 높음':['02-market-camera.png'],
    '### V4. 경매장에는 내용이 있지만 군중·소품이 격자로 보인다 · 우선순위 중간':['06-auction-and-props.png'],
    '### V5. 물·피격·코인·체력 숫자가 한곳에서 겹친다 · 우선순위 높음':['03-feedback-overlap.png'],
    '### V6. 휴게소 간판과 과노출에 가까운 밝은 바닥 · 우선순위 높음':['04-reststop-sightline.png'],
    '### V8. UI는 전체 중 상대적으로 일관적이다 · 유지하면서 정리':['07-ui-overview.png'],
    '### P1. 고속도로: 적은 삼각형도 많은 호출 때문에 비쌀 수 있다':['05-highway-density.png'],
}

pdfmetrics.registerFont(TTFont('Malgun',REG));pdfmetrics.registerFont(TTFont('MalgunBold',BOLD));pdfmetrics.registerFontFamily('Malgun',normal='Malgun',bold='MalgunBold')
body=ParagraphStyle('body',fontName='Malgun',fontSize=10,leading=16.1,textColor=colors.HexColor(navy),spaceAfter=8,wordWrap='CJK')
h1=ParagraphStyle('h1',parent=body,fontName='MalgunBold',fontSize=24,leading=33,spaceAfter=20)
h2=ParagraphStyle('h2',parent=body,fontName='MalgunBold',fontSize=17,leading=24,spaceBefore=15,spaceAfter=12,keepWithNext=True)
h3=ParagraphStyle('h3',parent=body,fontName='MalgunBold',fontSize=12.5,leading=19,spaceBefore=12,spaceAfter=8,keepWithNext=True)
small=ParagraphStyle('small',parent=body,fontSize=8.3,leading=12.7,spaceAfter=2)
header=ParagraphStyle('header',parent=small,fontName='MalgunBold',textColor=colors.white)

def rich(s):
    s=html.escape(s)
    s=re.sub(r'`([^`]+)`',lambda m:'<font color="#276778">'+m.group(1)+'</font>',s)
    s=re.sub(r'\*\*(.*?)\*\*',r'<b>\1</b>',s)
    s=re.sub(r'\[([^]]+)\]\((https?://[^)]+)\)',r'<link href="\2" color="#168B89">\1</link>',s)
    return s

flow=[];lines=text.splitlines();i=0;heading='';pending=[];first=True
def flush_figures():
    global pending
    for name in pending:
        image=Image.open(FIG/name);w,h=image.size;scale=min(505/w,500/h)
        flow.extend([Spacer(1,5),PdfImage(str(FIG/name),width=w*scale,height=h*scale),Spacer(1,12)])
    pending=[]
while i<len(lines):
    line=lines[i].strip()
    if not line:i+=1;continue
    if line.startswith('#'):
        flush_figures();level=len(line)-len(line.lstrip('#'));title=line[level:].strip()
        if level==1:flow.append(Paragraph(rich(title),h1))
        else:
            if level==2 and not first and not title.startswith(('3.','5.','6.')):flow.append(PageBreak())
            flow.append(Paragraph(rich(title),h2 if level==2 else h3));first=False
        pending=insertions.get(line,[]).copy();i+=1;continue
    if line.startswith('|'):
        rows=[]
        while i<len(lines) and lines[i].strip().startswith('|'):
            cells=[c.strip() for c in lines[i].strip().strip('|').split('|')]
            if not all(re.fullmatch(r'[-: ]+',c) for c in cells):rows.append(cells)
            i+=1
        n=len(rows[0]); widths=([76,210,219] if n==3 else [505/n]*n)
        if rows[0][0]=='파일':widths=[215,180,110]
        if rows[0][0]=='순서' and n==3:widths=[38,172,295]
        paras=[[Paragraph(rich(cell),header if r==0 else small) for cell in row] for r,row in enumerate(rows)]
        table=Table(paras,colWidths=widths,repeatRows=1,hAlign='LEFT')
        table.setStyle(TableStyle([('BACKGROUND',(0,0),(-1,0),colors.HexColor(navy)),('VALIGN',(0,0),(-1,-1),'TOP'),('ROWBACKGROUNDS',(0,1),(-1,-1),[colors.HexColor('#F4F6F6'),colors.white]),('LEFTPADDING',(0,0),(-1,-1),7),('RIGHTPADDING',(0,0),(-1,-1),7),('TOPPADDING',(0,0),(-1,-1),7),('BOTTOMPADDING',(0,0),(-1,-1),7),('LINEBELOW',(0,0),(-1,-1),.3,colors.HexColor('#D8E0E6'))]))
        flow.extend([table,Spacer(1,10)]);continue
    para=[]
    while i<len(lines) and lines[i].strip() and not lines[i].startswith(('#','|')):
        para.append(lines[i].strip());i+=1
    if para[0].startswith('- '):
        for item in para:flow.append(Paragraph('• '+rich(item.removeprefix('- ')),body))
    else:flow.append(Paragraph(rich(' '.join(para)),body))
flush_figures()
def page_chrome(c,doc):
    c.setStrokeColor(colors.HexColor('#C6D2DC'));c.line(44,805,551,805)
    c.setFont('Malgun',8);c.setFillColor(colors.HexColor('#596D7F'));c.drawString(44,816,'TRALALERO SHOOTER  /  PERFORMANCE + VISUAL AUDIT');c.drawString(44,26,'2026.10.01 · 실기기 FPS 확정 아님 · 원본 증거와 제한을 함께 기록');c.drawRightString(551,26,str(doc.page))
doc=SimpleDocTemplate(str(PDF),pagesize=A4,rightMargin=45,leftMargin=45,topMargin=54,bottomMargin=49,title='Galaxy S22 성능·시각 품질 심층 검토',author='Codex')
doc.build(flow,onFirstPage=page_chrome,onLaterPages=page_chrome)

# The local site contains only report deliverables and curated evidence, no save backups.
shutil.copy2(PDF,SITE/PDF.name)
(SITE/'figures').mkdir(exist_ok=True)
for p in FIG.glob('*.png'):shutil.copy2(p,SITE/'figures'/p.name)
raw=SITE/'originals';raw.mkdir(exist_ok=True)
gallery=[]
for p in sorted(PREVIEW.glob('ui-*.png'))+sorted(PREVIEW.glob('player-live-*.png')):
    shutil.copy2(p,raw/p.name);gallery.append(p.name)
md=text
for key,names in insertions.items():
    md=md.replace(key,key+'\n\n'+''.join(f'<figure><a href="figures/{name}" target="_blank"><img loading="lazy" src="figures/{name}" alt="검토 근거 그림"></a><figcaption>클릭하면 원본 크기의 PNG가 새 탭으로 열립니다.</figcaption></figure>\n\n' for name in names))
article=markdown.markdown(md,extensions=['tables','toc','fenced_code'])
css='''body{margin:0;background:#edf1f3;color:#172c45;font:17px/1.8 "Malgun Gothic",sans-serif}main{max-width:1050px;margin:0 auto;padding:34px 42px;background:#fff}header{background:#172c45;color:#fff;padding:28px 42px;position:relative}header p{color:#c4d9e3;margin:4px 0}header a{color:#fff;background:#168b89;padding:9px 15px;border-radius:5px;text-decoration:none;display:inline-block;margin:12px 10px 0 0}h1{font-size:35px;line-height:1.35}h2{font-size:27px;border-top:3px solid #168b89;padding-top:23px;margin-top:65px}h3{font-size:21px;margin-top:36px}strong{color:#9e392d}table{border-collapse:collapse;width:100%;font-size:14px;margin:22px 0;overflow-wrap:anywhere}th{background:#172c45;color:white}td,th{border:1px solid #dce3e8;padding:10px;text-align:left;vertical-align:top}tr:nth-child(even){background:#f1f5f5}figure{margin:25px 0;background:#f3f1ea;padding:12px}figure img{display:block;max-width:100%;max-height:880px;margin:auto}figcaption{font-size:13px;color:#526978;text-align:center}code{font-size:13px;overflow-wrap:anywhere;color:#23636d}a{color:#117c89}.grid{display:grid;grid-template-columns:repeat(3,1fr);gap:15px}.grid img{width:100%;max-height:430px;object-fit:contain;background:#172c45}.grid a{text-align:center;font-size:13px}.note{background:#e5f3f1;padding:18px;border-left:5px solid #168b89}footer{margin-top:60px;color:#647686;font-size:14px}@media(max-width:700px){main{padding:18px}header{padding:25px 18px}h1{font-size:27px}table{font-size:12px}.grid{grid-template-columns:repeat(2,1fr)}}'''
cards=''.join(f'<a href="originals/{x}" target="_blank"><img loading="lazy" src="originals/{x}" alt="{x}">{x}</a>' for x in gallery)
page=f'''<!doctype html><html lang="ko"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>S22 성능·시각 품질 심층 보고서</title><style>{css}</style></head><body><header><h1>S22 렉과 시각 품질, 근거로 살펴봤습니다.</h1><p>2026.10.01 · 실제 Unity 화면 · CPU 비교 측정 · 빌드 자원 분석</p><a href="{PDF.name}" target="_blank">전체 PDF 열기</a><a href="#originals">원본 PNG 보기</a></header><main><p class="note">핵심: 영상보다 카메라 가림 CPU 비용과 맵 자원의 부담이 더 강한 원인 후보입니다. PC 결과와 S22 확정 결과를 구분했습니다. 게임 수정·재배포는 하지 않았습니다.</p>{article}<h2 id="originals">원본 PNG: UI와 실제 주인공 다면 보기</h2><div class="grid">{cards}</div><h2>영상 장면 전체 비교</h2><figure><a href="figures/08-opening-timeline.png" target="_blank"><img src="figures/08-opening-timeline.png" alt="실제 오프닝 장면 비교"></a></figure><footer>그림은 실제 캡처/영상 프레임과 주석입니다. 생성 이미지로 게임 화면을 대체하지 않았습니다. 저장값·씬을 복원했습니다.</footer></main></body></html>'''
(SITE/'index.html').write_text(page,encoding='utf-8')
(SITE/'report.md').write_text(text,encoding='utf-8')
print(json.dumps({'pdf':str(PDF),'site':str(SITE/'index.html'),'figures':len(list(FIG.glob('*.png')))},ensure_ascii=True))
