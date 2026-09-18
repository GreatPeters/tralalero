"""Collect reviewed native frames without overwriting the user's concept folders."""
from pathlib import Path
import shutil,html,json

root=Path(__file__).resolve().parents[1]
source=root/'tmp/image-previews/harbor-opening-refinement-2026-09-16'
destination=root/'tmp/image-previews/harbor-ui-all-2026-09-15/4-refined-2026-09-16'
if destination.exists():
    raise SystemExit('Preserve existing collection; choose a new folder for another revision.')
destination.mkdir(parents=True)
groups=[]
for scene,label in [('Noryangjin_MapTool_Mode_SR18','1-노량진'),('HighWay','2-고속도로'),('RestStop','3-휴게소')]:
    folder=destination/label;folder.mkdir();files=[]
    for image in sorted((source/'release'/scene).glob('*.png')):
        if image.name=='07-start-greeting.png' and scene!='Noryangjin_MapTool_Mode_SR18':
            image=source/'final-gameplay'/f'{scene}.png';name='07-gameplay-final.png'
        else:name=image.name
        if not image.is_file():raise FileNotFoundError(image)
        shutil.copy2(image,folder/name);files.append((folder/name).relative_to(destination))
    if scene=='Noryangjin_MapTool_Mode_SR18':
        for image in sorted((source/'opening-runtime-final').glob('*.png')):
            target=folder/('opening-'+image.name);shutil.copy2(image,target);files.append(target.relative_to(destination))
    groups.append((label,files))
folder=destination/'4-상인-모델';folder.mkdir()
for pose in ['merchant-seated-v4','merchant-greeting-v4','workshop-fresh']:
    shutil.copy2(source/pose/'perspective.png',folder/f'{pose}.png')
groups.append(('4-상인-모델',[p.relative_to(destination) for p in sorted(folder.glob('*.png'))]))
parts=['<!doctype html><html lang="ko"><meta charset="utf-8"><title>실제 적용 화면 · 2026-09-16</title><style>body{margin:32px;background:#102c45;color:#fff9e9;font:17px/1.6 system-ui}h1{font-size:28px}section{display:grid;grid-template-columns:repeat(auto-fill,minmax(220px,1fr));gap:20px}figure{margin:0;background:#173951;padding:10px;border-radius:8px}img{width:100%;height:auto}a{color:#ffe180}figcaption{overflow-wrap:anywhere}</style><h1>실제 적용 화면</h1><p>노량진 · 고속도로 · 휴게소 / 2026-09-16<br>이미지를 누르면 원본 PNG를 엽니다. 1~3은 Unity 실제 화면, 4는 새 모델의 Blender 검증 렌더입니다.</p>']
for title,files in groups:
    parts.append(f'<h2>{html.escape(title)}</h2><section>')
    for file in files:
        href=html.escape(file.as_posix(),quote=True);parts.append(f'<figure><a href="{href}"><img loading="lazy" src="{href}" alt="{html.escape(file.stem)}"></a><figcaption>{html.escape(file.stem)}</figcaption></figure>')
    parts.append('</section>')
parts.append('</html>');(destination/'index.html').write_text(''.join(parts),encoding='utf8')
(destination/'README.md').write_text('# 실제 적용 화면\n\n[index.html](index.html)에서 모든 화면을 볼 수 있습니다.\n\n원래 1·2·3 시안 폴더는 그대로 보존했습니다. 이번 폴더는 실제 Unity 적용 화면과 새 모델 검증 렌더입니다.\n',encoding='utf8')
print(json.dumps({'folder':str(destination),'pngs':len(list(destination.rglob('*.png')))},ensure_ascii=False))
