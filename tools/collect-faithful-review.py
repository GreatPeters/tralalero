from pathlib import Path
import shutil,html
root=Path(__file__).resolve().parents[1];source=root/'tmp/image-previews/harbor-faithful-art-2026-09-17';out=root/'tmp/image-previews/harbor-ui-all-2026-09-15/6-faithful-art-2026-09-17'
assert not out.exists(),'Preserve previous review collection'
out.mkdir(parents=True)
for file in ['01-lobby.png','05-story-video.png']:shutil.copy2(root/'tmp/image-previews/harbor-ui-all-2026-09-15/1'/file,out/('reference-'+file))
for scene,label in [('Noryangjin_MapTool_Mode_SR18','1-노량진'),('HighWay','2-고속도로'),('RestStop','3-휴게소')]:
 target=out/label;target.mkdir()
 for file in sorted((source/'accepted'/scene).glob('*.png')):shutil.copy2(file,target/file.name)
 assert (target/'01-story-first.png').exists() and (target/'03-lobby.png').exists(),scene
transitions=out/'4-챕터영상';transitions.mkdir()
for file in sorted((source/'transitions').glob('*.png')):shutil.copy2(file,transitions/file.name)
body=['<!doctype html><html lang="ko"><meta charset="utf-8"><title>시안과 실제 적용 비교</title><style>body{background:#102d47;color:#fff7df;font:18px/1.6 system-ui;margin:28px}section{display:flex;gap:20px;flex-wrap:wrap}figure{margin:0;width:360px;max-width:45vw}img{width:100%}a{color:#ffe080}h2{margin-top:40px}</style><h1>시안과 새 적용본</h1><p>왼쪽은 선택 시안, 오른쪽은 Unity 실제 화면입니다. 같은 너비로 비교합니다.<br>실제 게임 배경과 재생 영상은 프로젝트 콘텐츠를 사용합니다. 이전 장면과 독립된 손 동작을 유지했습니다.</p>']
for name,ref,actual in [('로비','reference-01-lobby.png','1-노량진/03-lobby.png'),('이야기 영상','reference-05-story-video.png','1-노량진/01-story-first.png')]:
 body.append(f'<h2>{name}</h2><section>')
 for label,file in [('선택 시안',ref),('새 적용본',actual)]:body.append(f'<figure><figcaption>{label}</figcaption><a href="{file}"><img src="{file}" alt="{label}"></a></figure>')
 body.append('</section>')
for folder in sorted(out.iterdir()):
 if folder.is_dir():
  body.append(f'<h2>{folder.name}</h2><section>')
  for file in sorted(folder.glob('*.png')):
   path=html.escape(file.relative_to(out).as_posix(),quote=True);body.append(f'<figure><a href="{path}"><img src="{path}"></a><figcaption>{html.escape(file.stem)}</figcaption></figure>')
  body.append('</section>')
body.append('</html>');(out/'index.html').write_text(''.join(body),encoding='utf8');print(out)
