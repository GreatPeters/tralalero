from pathlib import Path
import html,json,re,shutil

root=Path(__file__).resolve().parents[2]
preview=root/'tmp/image-previews/talisman-polish-2026-09-20'
gallery=root/'tmp/image-previews/bonus-gallery-2026-09-19/talisman-polished'
gallery.mkdir(parents=True,exist_ok=True)
scenes=[('Noryangjin_MapTool_Mode_SR18','항구'),('HighWay','고속도로'),('RestStop','휴게소')]
figures=[];videos=[]
for scene,label in scenes:
    record=json.loads((preview/'video'/scene/'record.json').read_text())
    assert record['claimedFrame']>=0 and record['effectFrames']>=20
    movie=scene+'.mp4';shutil.copy2(preview/'video'/movie,gallery/movie)
    videos.append(f'<article><h2>{label} · 실제 획득</h2><video controls autoplay muted loop playsinline preload="metadata" src="{movie}"></video><a href="{movie}" target="_blank">영상 크게 열기 ↗</a></article>')
    for phase,offset in [('펼침',6),('흡수',13),('몸통 발광',24),('정리',35)]:
        index=min(119,record['claimedFrame']+offset);source=preview/'video'/scene/f'frame-{index:03}.png';name=f'{scene}-{index:03}.png';shutil.copy2(source,gallery/name)
        title=label+' · '+phase
        figures.append(f'<figure data-group="after"><button class="preview" aria-label="{title} 확대"><img src="{name}" alt="{title}" loading="lazy" width="540" height="1170"></button><figcaption><strong>{title}</strong><a href="{name}" target="_blank">PNG ↗</a></figcaption></figure>')
for name,title,source in [('model.png','Blender 입체 모델',root/'outputs/talisman-polish-2026-09-20/hero.png'),('neutral.png','재질을 뺀 실제 형상',root/'outputs/talisman-polish-2026-09-20/neutral.png'),('multiview.png','GLB 재수입 다각도',root/'outputs/talisman-polish-2026-09-20/reimport-views/contact_sheet.png')]:
    shutil.copy2(source,gallery/name)
    figures.append(f'<figure data-group="model"><button class="preview" aria-label="{title} 확대"><img src="{name}" alt="{title}" loading="lazy"></button><figcaption><strong>{title}</strong><a href="{name}" target="_blank">PNG ↗</a></figcaption></figure>')
before=root/'outputs/talisman-polish-2026-09-20/before/native/Noryangjin_MapTool_Mode_SR18/01-idle.png'
after=preview/'final/Noryangjin_MapTool_Mode_SR18/01-idle.png'
for name,title,source in [('before.png','이전 적용본 · 정지 캡처',before),('after.png','개선 적용본 · 같은 캡처 방식',after)]:
    shutil.copy2(source,gallery/name)
    figures.append(f'<figure data-group="compare"><button class="preview" aria-label="{title} 확대"><img src="{name}" alt="{title}" loading="lazy"></button><figcaption><strong>{title}</strong><a href="{name}" target="_blank">PNG ↗</a></figcaption></figure>')
template=(root/'map-concepts/bonus-gallery-2026-09-19/template.html').read_text(encoding='utf-8')
template=template.replace('보너스 시안 모아보기','접힌 부적 · 모델과 이펙트 재제작')
template=template.replace('이미지를 눌러 크게 보세요. 원본 크기 보기와 PNG 저장도 가능합니다.','아래는 실제 Unity 플레이 영상입니다. 물리 충돌로 획득한 뒤 펼침·몸통 흡수·정리까지 이어집니다. 30fps 고정 타임스텝으로 녹화했으며 기기 성능 측정 영상은 아닙니다.')
nav='<nav aria-label="검토 자료"><button data-group="after" aria-pressed="true">획득 단계</button><button data-group="compare" aria-pressed="false">이전 / 개선</button><button data-group="model" aria-pressed="false">모델 확대</button><button data-group="all" aria-pressed="false">전체</button></nav>'
template=re.sub(r'<nav .*?</nav>',nav,template,flags=re.S)
template=template.replace('<main>','<section class="videos">'+''.join(videos)+'</section><main>')
template=template.replace('</style>','.videos{max-width:1500px;margin:24px auto;display:grid;grid-template-columns:repeat(3,1fr);gap:24px;padding:0 28px}.videos article{text-align:center}.videos h2{font-size:16px}.videos video{display:block;width:100%;height:58vh;background:#142231;border-radius:8px;margin-bottom:10px}.videos a{color:#006b76}figure[data-group=model] img{aspect-ratio:1/1}@media(max-width:800px){.videos{grid-template-columns:1fr}.videos video{height:65vh}}</style>')
template=template.replace('__FIGURES__','\n'.join(figures)).replace("filter('context');","filter('after');")
template=template.replace('1536 × 1024 원본','원본 크기 확대 가능').replace('장의 시안','장의 검토 자료').replace('aspect-ratio:3/2','aspect-ratio:6/13')
template=template.replace('모두 기획 시안입니다. 실제 Unity 적용 화면과는 구분해 주세요.','플레이 영상·캡처는 실제 Unity 결과, 모델 확대는 Blender 렌더입니다. 모든 Bonus의 기존 보상 효과를 유지했습니다.')
(gallery/'index.html').write_text(template,encoding='utf-8')
print(gallery/'index.html')
