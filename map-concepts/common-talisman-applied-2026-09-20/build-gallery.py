from pathlib import Path
import html
import re
import shutil

root=Path(__file__).resolve().parents[2]
preview=root/'tmp/image-previews/common-talisman-applied-2026-09-20/final'
gallery=root/'tmp/image-previews/bonus-gallery-2026-09-19/talisman-applied'
gallery.mkdir(parents=True,exist_ok=True)
scenes=[('Noryangjin_MapTool_Mode_SR18','항구'),('HighWay','고속도로'),('RestStop','휴게소')]
phases=[('01-idle','대기'),('02-unfold','펼침'),('03-absorb','흡수'),('04-body-pulse','몸통 발광'),('05-complete','완료')]
figures=[]
for phase,label in phases:
    for scene,place in scenes:
        name=f'{scene}-{phase}.png'
        shutil.copy2(preview/scene/(phase+'.png'),gallery/name)
        title=html.escape(place+' · '+label)
        figures.append(f'<figure data-group="{phase}"><button class="preview" aria-label="{title} 확대"><img src="{name}" alt="{title}" loading="lazy" width="1080" height="2340"></button><figcaption><strong>{title}</strong><a href="{name}" target="_blank" rel="noopener">원본 PNG ↗</a></figcaption></figure>')
template=(root/'map-concepts/bonus-gallery-2026-09-19/template.html').read_text(encoding='utf-8')
template=template.replace('보너스 시안 모아보기','접힌 부적 · 실제 Unity 적용 화면')
template=template.replace('이미지를 눌러 크게 보세요. 원본 크기 보기와 PNG 저장도 가능합니다.','세 맵의 대기·펼침·흡수·몸통 발광·정리 상태입니다. 단계 캡처는 검사를 위해 플레이를 정지했으며 실제 획득·보상·효과 종료는 별도 Play Mode 검증을 통과했습니다.')
nav='<nav aria-label="연출 단계">'+''.join(f'<button data-group="{phase}" aria-pressed="false">{label}</button>' for phase,label in phases)+'<button data-group="all" aria-pressed="false">전체 15장</button></nav>'
template=re.sub(r'<nav .*?</nav>',nav,template,flags=re.S)
template=template.replace('__FIGURES__','\n'.join(figures)).replace("filter('context');","filter('01-idle');")
template=template.replace('1536 × 1024 원본','1080 × 2340 원본').replace('장의 시안','장의 실제 화면')
template=template.replace('repeat(2,minmax(0,1fr))','repeat(3,minmax(0,1fr))').replace('aspect-ratio:3/2','aspect-ratio:6/13')
template=template.replace('모두 기획 시안입니다. 실제 Unity 적용 화면과는 구분해 주세요.','실제 Unity Editor 렌더입니다. 공통 부적·흡수 연출이 적용되었으며 Android 기기 성능 검증은 이번 범위에 포함되지 않습니다.')
(gallery/'index.html').write_text(template,encoding='utf-8')
print(gallery/'index.html')
