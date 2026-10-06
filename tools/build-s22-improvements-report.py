"""Build a source-linked Korean report with actual Unity evidence and measured charts."""
from pathlib import Path
import html,json,re,shutil
import markdown
from PIL import Image,ImageDraw,ImageFont
import matplotlib
matplotlib.use('Agg')
import matplotlib.pyplot as plt
from matplotlib import font_manager
from reportlab.pdfbase import pdfmetrics
from reportlab.pdfbase.ttfonts import TTFont
from reportlab.lib import colors
from reportlab.lib.styles import ParagraphStyle
from reportlab.lib.pagesizes import A4
from reportlab.platypus import SimpleDocTemplate,Paragraph,Spacer,Table,TableStyle,Image as PdfImage,PageBreak
ROOT=Path(__file__).resolve().parents[1];DATA=ROOT/'outputs/s22-polish-2026-10-01';PRE=ROOT/'tmp/image-previews/s22-polish-2026-10-01';FIG=PRE/'figures';SITE=DATA/'site';PDF=ROOT/'output/pdf/s22-improvements-applied-2026-10-01.pdf';DOC=ROOT/'docs/reviews/s22-improvements-applied-2026-10-01.md'
final=json.loads((DATA/'final-runtime.json').read_text(encoding='utf-8'));build=json.loads((DATA/'build-release/result.json').read_text(encoding='utf-8'));assert build['result']=='Succeeded'
for p in (FIG,SITE,PDF.parent):p.mkdir(parents=True,exist_ok=True)
REG='C:/Windows/Fonts/malgun.ttf';BOLD='C:/Windows/Fonts/malgunbd.ttf';F=lambda n,b=False:ImageFont.truetype(BOLD if b else REG,n)
navy='#172c45';teal='#168b89';cream='#f3f1ea';manifest=[]
def plate(name,title,subtitle,items,notes):
 w=1600;gap=24;pad=40;col=(w-2*pad-gap*(len(items)-1))//len(items)
 square=all(Image.open(p).height/Image.open(p).width<1.4 for p,_ in items)
 ph=(840 if square else 1050) if len(items)<=2 else 650
 out=Image.new('RGB',(w,ph+330),cream);d=ImageDraw.Draw(out);d.rectangle((0,0,w,126),fill=navy);d.text((40,20),title,font=F(40,True),fill='white');d.text((40,80),subtitle,font=F(22),fill='#bad8df')
 for i,(path,label) in enumerate(items):
  im=Image.open(path).convert('RGB');im.thumbnail((col,ph-50));x=pad+i*(col+gap);d.text((x,152),label,font=F(23,True),fill=navy);out.paste(im,(x+(col-im.width)//2,194));manifest.append(dict(figure=name,source=str(path.relative_to(ROOT)),label=label))
 for i,n in enumerate(notes):d.text((40,ph+206+i*36),n,font=F(23),fill=navy)
 out.save(FIG/name)
before=ROOT/'tmp/image-previews/s22-quality-audit-2026-10-01'
plate('01-player-anatomy.png','두 발과 꼬리 신발로 구조를 맞췄습니다.','실제 Play 모델의 같은 측면 방향. 장착 원본과 애니메이션 리그는 보존했습니다.',[(before/'player-live-left.png','변경 전 · 네 개의 팔다리'),(PRE/'player-final-left.png','적용 후 · 두 발 + 꼬리 신발')],['기본 포함 여덟 신발 스타일에 적용. 새 후방 독립 다리를 붙인 모델이 아닙니다.','후보의 열린 절단면은 실제 경계와 웨이트를 연결해 보정했습니다.'])
inside=ROOT/final['applied-inside']['folder'];ramp=min(inside.glob('f*-*.png'),key=lambda p:abs(float(p.stem.split('-')[1])-71))
old=ROOT/'tmp/image-previews/noryangjin-claude-fix-2026-09-28/run-s22-inside-1x-024549/f030-071.6.png'
plate('02-market-view.png','경사로: 바닥을 보존하고 카메라 거리를 조정','별도 1배속 실행의 비슷한 구간입니다. 위치·전투 상황이 완전히 같지는 않습니다.',[(old,'변경 전 · 71.6초'),(ramp,'최종 적용 후 · '+ramp.stem.split('-')[1]+'초')],['자신이 걷는 바닥은 불투명하게 유지. 상부 구조물은 사용자 선호대로 반투명합니다.','남는 한계: 겹친 반투명 구조물 때문에 일부 구간의 대비는 여전히 낮습니다.'])
assets=PRE/'assets-reviewed'
plate('03-lightweight-props.png','반복 소품은 형태를 다시 만들고 비용을 줄였습니다.','실제 Unity 렌더. 모바일용 공유 재질과 기존 배치·충돌체를 사용합니다.',[(assets/'MobileHole-lod0.png','구멍 · 470,390 → 444 tri'),(assets/'MobileGuardrail-lod0.png','가드레일 · 14,079 → 1,244'),(assets/'MobileVending-lod0.png','자판기 · 33,452 → 2,028')],['379개 활성 가드레일에 적용. 강하게 줄여 깨졌던 배경 LOD2는 배포에서 제거했습니다.','전체 씬의 자원 합계와 실제 한 프레임에 보이는 삼각형은 다른 지표입니다.'])
font_manager.fontManager.addfont(REG);plt.rcParams.update({'font.family':'Malgun Gothic','axes.unicode_minus':False})
fig,axs=plt.subplots(1,3,figsize=(15,5.1),layout='constrained')
r=final['render'];cost=json.loads((DATA/'optimized-final-occlusion.json').read_text(encoding='utf-8'))['meanMs'];pairs=[('가림 함수 평균 (ms)',[10.03,cost]),('시작 화면 배치',[294,r['batches']]),('설치 APK (MB)',[1526,build['bytes']/1e6])]
for ax,(title,vals) in zip(axs,pairs):
 ax.bar(['전','후'],vals,color=['#be6354',teal],width=.56);ax.set_title(title,fontweight='bold',pad=16);ax.spines[['top','right']].set_visible(False);ax.set_ylim(0,max(vals)*1.24)
 for j,v in enumerate(vals):ax.text(j,v+max(vals)*.035,f'{v:,.2f}' if v<20 else f'{v:,.0f}',ha='center',fontweight='bold')
fig.suptitle('PC에서 확인한 비용 감소 · S22 FPS 수치가 아닙니다',fontweight='bold',fontsize=16);fig.savefig(FIG/'04-measured-results.png',dpi=150);plt.close(fig)
plate('05-arrival-movies.png','도착 영상과 플레이 모델의 정체성을 연결','첫 30초의 선택된 프롤로그는 보존. 마지막 9초와 두 진입 영상을 실제로 애니메이션했습니다.',[(DATA/'cinematics-v2/Market_Arrival.png','노량진 도착 · 9초'),(DATA/'cinematics-v2/Highway_Aligned.png','고속도로 · 5.04초'),(DATA/'cinematics-v2/RestStop_Aligned.png','휴게소 · 5.04초')],['같은 얼굴·두 발·꼬리 신발. 사람보다 큰 상어와 달아나는 인물의 동작을 사용합니다.','원본 영상과 새 영상 파일은 모두 보존. 프롤로그와 게임의 화풍 차이는 일부 남습니다.'])
shutil.copy2(PRE/'cinematics-motion.png',FIG/'06-movie-motion-frames.png')
shoes=Image.new('RGB',(1600,1050),cream);draw=ImageDraw.Draw(shoes);draw.rectangle((0,0,1600,100),fill=navy);draw.text((30,20),'여덟 신발 스타일 · 실제 Play 장착 화면',font=F(36,True),fill='white')
for i,key in enumerate(('original','ruby','mint','gold','steel','spring','relic','salvage')):
 path=PRE/f'shoes_{key}-fit.png';im=Image.open(path);im.thumbnail((390,390));x=(i%4)*400;y=(i//4)*460+115;shoes.paste(im,(x+5,y+40));draw.text((x+14,y),key,font=F(24),fill=navy);manifest.append(dict(figure='07-shoes-final.png',source=str(path.relative_to(ROOT)),label=key))
shoes.save(FIG/'07-shoes-final.png')
(DATA/'applied-figure-manifest.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2),encoding='utf-8')
pdfmetrics.registerFont(TTFont('Malgun',REG));pdfmetrics.registerFont(TTFont('MalgunBold',BOLD));pdfmetrics.registerFontFamily('Malgun',normal='Malgun',bold='MalgunBold')
body=ParagraphStyle('body',fontName='Malgun',fontSize=10,leading=16,textColor=colors.HexColor(navy),spaceAfter=9,wordWrap='CJK');h1=ParagraphStyle('h1',parent=body,fontName='MalgunBold',fontSize=24,leading=33,spaceAfter=22);h2=ParagraphStyle('h2',parent=body,fontName='MalgunBold',fontSize=17,leading=25,spaceAfter=14,keepWithNext=True);small=ParagraphStyle('small',parent=body,fontSize=8.4,leading=13);head=ParagraphStyle('head',parent=small,textColor=colors.white,fontName='MalgunBold')
def rich(t):
 t=html.escape(t);t=re.sub(r'\*\*(.*?)\*\*',r'<b>\1</b>',t);t=re.sub(r'`([^`]+)`',r'<font color="#286478">\1</font>',t);return re.sub(r'\[([^]]+)\]\((https?://[^)]+)\)',r'<link href="\2" color="#168b89">\1</link>',t)
text=DOC.read_text(encoding='utf-8');assert '검증 진행 중' not in text and '추가 조정 중' not in text
flow=[];lines=text.splitlines();i=0
while i<len(lines):
 line=lines[i].strip()
 if not line:i+=1;continue
 if line.startswith('#'):
  level=len(line)-len(line.lstrip('#'));title=line[level:].strip()
  if level==2 and title=='성능 적용':
   cover=FIG/'01-player-anatomy.png';im=Image.open(cover);flow.extend([Spacer(1,14),PdfImage(str(cover),width=505,height=505*im.height/im.width)])
  if level==2 and title not in ('판단 기준','그대로 믿지 않은 지적'):flow.append(PageBreak())
  flow.append(Paragraph(rich(title),h1 if level==1 else h2))
  if title=='성능 적용':
   chart=FIG/'04-measured-results.png';im=Image.open(chart);flow.extend([PdfImage(str(chart),width=505,height=505*im.height/im.width),Spacer(1,14)])
  i+=1;continue
 if line.startswith('|'):
  rows=[]
  while i<len(lines) and lines[i].strip().startswith('|'):
   cells=[c.strip() for c in lines[i].strip().strip('|').split('|')]
   if not all(re.fullmatch(r'[-: ]+',c) for c in cells):rows.append(cells)
   i+=1
  n=len(rows[0]);widths=[90,150,265] if n==3 else [505/n]*n
  table=Table([[Paragraph(rich(c),head if j==0 else small) for c in row] for j,row in enumerate(rows)],colWidths=widths,repeatRows=1)
  table.setStyle(TableStyle([('BACKGROUND',(0,0),(-1,0),colors.HexColor(navy)),('VALIGN',(0,0),(-1,-1),'TOP'),('ROWBACKGROUNDS',(0,1),(-1,-1),[colors.HexColor('#eff4f4'),colors.white]),('LEFTPADDING',(0,0),(-1,-1),7),('RIGHTPADDING',(0,0),(-1,-1),7),('TOPPADDING',(0,0),(-1,-1),7),('BOTTOMPADDING',(0,0),(-1,-1),7)]));flow.extend([table,Spacer(1,12)]);continue
 flow.append(Paragraph(rich(line.removeprefix('> ')),body));i+=1
flow.append(PageBreak());flow.append(Paragraph('전후 그림과 실제 화면',h2))
for p in sorted(FIG.glob('*.png')):
 if p.name in ('01-player-anatomy.png','04-measured-results.png'):continue
 if p.name=='06-movie-motion-frames.png':flow.append(Paragraph('도착 영상의 동작 표본',h2))
 im=Image.open(p);scale=min(505/im.width,665/im.height);flow.append(PdfImage(str(p),width=im.width*scale,height=im.height*scale));flow.append(Spacer(1,20) if p.name=='03-lightweight-props.png' else PageBreak())
flow.pop()
def chrome(c,doc):
 c.setFont('Malgun',8);c.setFillColor(colors.HexColor('#587082'));c.drawString(45,816,'TRALALERO SHOOTER / APPLIED IMPROVEMENTS / 2026.10.01');c.drawString(45,26,'실제 Unity 증거 · PC와 실기기 측정 범위를 구분');c.drawRightString(551,26,str(doc.page))
SimpleDocTemplate(str(PDF),pagesize=A4,leftMargin=45,rightMargin=45,topMargin=54,bottomMargin=49,title='S22 성능·시각 품질 개선 적용',author='Codex').build(flow,onFirstPage=chrome,onLaterPages=chrome)
for folder in ('figures','originals','movies'):(SITE/folder).mkdir(exist_ok=True)
shutil.copy2(PDF,SITE/PDF.name)
prior=ROOT/'output/pdf/s22-performance-visual-audit-2026-10-01.pdf';shutil.copy2(prior,SITE/prior.name)
for p in FIG.glob('*.png'):shutil.copy2(p,SITE/'figures'/p.name)
for p in [*PRE.glob('player-final-*.png'),*PRE.glob('shoes_*-fit.png'),PRE/'video-market-aligned.png']:
 shutil.copy2(p,SITE/'originals'/p.name)
for name in ('Market_Arrival','Highway_Aligned','RestStop_Aligned'):shutil.copy2(DATA/'cinematics-v2'/(name+'.mp4'),SITE/'movies'/(name+'.mp4'))
cards=''.join(f'<a target="_blank" href="figures/{p.name}"><img src="figures/{p.name}" alt="{p.stem}"></a>' for p in sorted(FIG.glob('*.png')))
videos=''.join(f'<figure><video controls preload="metadata" src="movies/{name}.mp4"></video><figcaption>{label}</figcaption></figure>' for name,label in [('Market_Arrival','노량진 도착 — 9초'),('Highway_Aligned','고속도로 — 5.04초'),('RestStop_Aligned','휴게소 — 5.04초')])
css='body{margin:0;background:#eaf0f2;color:#172c45;font:17px/1.8 "Malgun Gothic",sans-serif}header{background:#172c45;color:white;padding:32px max(24px,calc((100% - 1050px)/2))}main{max-width:1050px;margin:auto;padding:32px;background:white}h1{line-height:1.4}h2{margin-top:55px;border-top:3px solid #168b89;padding-top:20px}a{color:#168b89}header a{color:white;display:inline-block;padding:8px 18px;background:#168b89}table{border-collapse:collapse;width:100%;font-size:14px}td,th{border:1px solid #d8e2e6;padding:10px;vertical-align:top;text-align:left}th{background:#172c45;color:white}img{max-width:100%;display:block;margin:25px auto}video{max-width:100%;height:520px;background:#172c45}figure{display:inline-block;margin:12px}code{font-size:13px;overflow-wrap:anywhere}@media(max-width:700px){main{padding:18px}table{font-size:12px}video{height:430px}}'
article=markdown.markdown(text.replace('(s22-performance-visual-audit-2026-10-01.md)','(s22-performance-visual-audit-2026-10-01.pdf)'),extensions=['tables'])
(SITE/'index.html').write_text(f'<!doctype html><html lang="ko"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>S22 개선 적용 보고서</title><style>{css}header a{{margin:12px 10px 0 0}}</style></head><body><header><h1>렉과 어색한 표현을<br>프로젝트에 반영했습니다.</h1><p>실제 변경 · 전후 PNG · 동작 영상 · Android 빌드</p><a target="_blank" href="{PDF.name}">전체 PDF</a><a href="#figures">전후 PNG</a><a href="#movies">새 영상</a></header><main>{article}<h2 id="figures">전후 PNG — 클릭하면 원본 크기</h2>{cards}<h2 id="movies">실제로 움직이는 새 도착 영상</h2>{videos}<p>게임 화면을 생성 이미지로 대체한 검증 자료가 아닙니다. 원본 픽셀과 출처는 기록에 보존했습니다.</p></main></body></html>',encoding='utf-8')
print(PDF);print(SITE)
