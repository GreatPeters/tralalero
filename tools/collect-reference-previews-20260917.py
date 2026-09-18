from pathlib import Path
import shutil,html
root=Path(__file__).resolve().parents[1]
source=root/'tmp/image-previews/harbor-reference-fidelity-2026-09-17'
target=root/'tmp/image-previews/harbor-ui-all-2026-09-15/5-reference-fix-2026-09-17'
assert not target.exists(),'Preserve prior collection'
target.mkdir(parents=True)
body=['<!doctype html><html lang="ko"><meta charset="utf-8"><title>UI 재수정 적용본</title><style>body{background:#102c45;color:#fff8e5;font:18px system-ui;margin:32px}section{display:flex;flex-wrap:wrap;gap:24px}img{width:300px;max-width:90vw}a{color:#fff8e5}figure{margin:0}</style><h1>2026-09-17 실제 적용 화면</h1><p>글자·외곽선·패널 비율 / 상인 크기·위치 / 위쪽 화살표와 아래쪽 손</p>']
for scene,label in [('Noryangjin_MapTool_Mode_SR18','1-노량진'),('HighWay','2-고속도로'),('RestStop','3-휴게소')]:
    folder=target/label;folder.mkdir();body.append(f'<h2>{label}</h2><section>')
    for image in sorted((source/'accepted'/scene).glob('*.png')):
        if scene!='Noryangjin_MapTool_Mode_SR18' and image.name.startswith(('03','04')):continue
        shutil.copy2(image,folder/image.name);path=html.escape((folder/image.name).relative_to(target).as_posix(),quote=True)
        body.append(f'<figure><a href="{path}"><img src="{path}"></a><figcaption>{image.stem}</figcaption></figure>')
    body.append('</section>')
shutil.copy2(source/'start-hand-motion.gif',target/'start-hand-motion.gif');body.append('<h2>손 움직임 · 실제 화면 녹화</h2><a href="start-hand-motion.gif"><img src="start-hand-motion.gif"></a></html>')
(target/'index.html').write_text(''.join(body),encoding='utf8');print(target)
