from pathlib import Path
import shutil,html
root=Path(__file__).resolve().parents[1];source=root/'tmp/image-previews/ui-text-alignment-2026-09-17';out=root/'tmp/image-previews/harbor-ui-all-2026-09-15/8-text-alignment-2026-09-17'
assert not out.exists(),'Preserve previous collection';out.mkdir(parents=True)
for scene,label in [('Noryangjin_MapTool_Mode_SR18','1-노량진'),('HighWay','2-고속도로'),('RestStop','3-휴게소')]:
    folder=out/label;folder.mkdir()
    for file in sorted((source/scene/'1080x2340').glob('*.png')):shutil.copy2(file,folder/file.name)
    assert (folder/'01-story-scene-3.png').is_file()
short=source/'Noryangjin_MapTool_Mode_SR18/1080x1920/01-story-scene-3.png';shutil.copy2(short,out/'story-16x9.png')
body=['<!doctype html><html lang="ko"><meta charset="utf-8"><title>글자 정렬 수정</title><style>body{background:#102c45;color:#fff7df;font:18px/1.6 system-ui;margin:28px}section{display:flex;gap:24px;flex-wrap:wrap}figure{margin:0;width:340px;max-width:90vw}img{width:100%}a{color:#ffe080}</style><h1>글자 정렬 수정 · Unity 실제 화면</h1><p>버튼·숫자·자막의 글자 중심을 맞추고, 이전 장면의 화살표와 문구를 한 묶음으로 정렬했습니다.</p>']
for folder in sorted(out.iterdir()):
    if folder.is_dir():
        body.append(f'<h2>{folder.name}</h2><section>')
        for file in sorted(folder.glob('*.png')):
            path=html.escape(file.relative_to(out).as_posix(),quote=True);body.append(f'<figure><a href="{path}"><img src="{path}"></a><figcaption>{file.stem}</figcaption></figure>')
        body.append('</section>')
body.append('<h2>16:9 화면 확인</h2><figure><a href="story-16x9.png"><img src="story-16x9.png"></a></figure></html>');(out/'index.html').write_text(''.join(body),encoding='utf8');print(out)
