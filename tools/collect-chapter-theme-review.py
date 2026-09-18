from pathlib import Path
import shutil
root=Path(__file__).resolve().parents[1]
source=root/'tmp/image-previews/chapter-ui-themes-2026-09-17'
out=root/'tmp/image-previews/harbor-ui-all-2026-09-15/7-chapter-themes-2026-09-17'
assert not out.exists(),'Preserve previous collection'
out.mkdir(parents=True)
for scene in ['HighWay','RestStop']:
    shutil.copy2(source/scene/'01-lobby.png',out/(scene+'-lobby.png'))
for name in ['HighwayPlaque','RestStopPlaque']:
    shutil.copy2(root/'Assets/ShooterSurvival/UI/ChapterThemes'/f'{name}.png',out/f'{name}.png')
body='<!doctype html><html lang="ko"><meta charset="utf-8"><title>챕터별 UI</title><style>body{background:#122c3d;color:#fff8e8;font:18px/1.6 system-ui;margin:28px}section{display:flex;gap:24px;flex-wrap:wrap}figure{width:360px;margin:0}img{width:100%}a{color:#ffe285}</style><h1>고속도로 · 휴게소 UI 적용</h1><p>고속도로는 초록색 도로 표지, 휴게소는 청록색 서비스 안내판으로 구분했습니다.<br>노량진의 닻 명판과 공통 조작·버튼 구성은 유지했습니다.</p><section>'
for scene,label in [('HighWay','고속도로'),('RestStop','휴게소')]:
    body+=f'<figure><figcaption>{label} · Unity 실제 화면</figcaption><a href="{scene}-lobby.png"><img src="{scene}-lobby.png"></a></figure>'
body+='</section><h2>명판 원본</h2><section>'
for name in ['HighwayPlaque','RestStopPlaque']:
    body+=f'<figure><a href="{name}.png"><img src="{name}.png"></a></figure>'
body+='</section></html>'; (out/'index.html').write_text(body,encoding='utf8');print(out)
