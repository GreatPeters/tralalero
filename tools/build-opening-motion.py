"""Render a motion-comic video from the supplied four-panel opening artwork.

This animates framing, titles and transitions; it does not claim synthesized
character motion. The original PNG remains unchanged.
"""
from pathlib import Path
import shutil
import subprocess

root = Path(__file__).resolve().parent.parent
work = root / 'tmp/opening-motion-20260910'
work.mkdir(parents=True, exist_ok=True)
ffmpeg = Path(r'C:\AI\ComfyUI-Creative-AMD\venv\Lib\site-packages\imageio_ffmpeg\binaries\ffmpeg-win-x86_64-v7.1.exe')
source = root / 'Assets/JH/UI/Opening/Curse_Opening.png'
font = next((root / 'Assets/JH/Font').rglob('GmarketSansTTFBold.ttf'))
shutil.copy2(font, work / 'font.ttf')
titles = ['훔친 공물', '세 짝의 저주', '신이 내건 조건', '노량진에서 시작된 여정']
captions = [
    '신단에 바칠 신발을 훔친 날,\n평범한 상어의 삶은 끝났다.',
    '가브릴렐로가 작은 신발을 발에 묶었다.\n도망쳐도, 죽어도 벗겨지지 않는 저주.',
    '더 좋은 신발을 찾아 공물로 바쳐라.\n신이 만족해야만 저주가 풀린다.',
    '쓰러지면 제단에서 다시 일어난다.\n모은 코인으로 신발을 개조하고, 더 멀리.',
]
for i in range(4):
    (work / f'title{i}.txt').write_text(titles[i], encoding='utf-8')
    (work / f'caption{i}.txt').write_text(captions[i], encoding='utf-8')
    (work / f'page{i}.txt').write_text(f'저주의 시작  {i+1} / 4', encoding='utf-8')
    # Native video filters animate a cropped view of each untouched source panel.
    graph = (
        f'[0:v]crop=iw:floor(ih/4):0:floor(ih/4)*{i},split[bg][fg];'
        '[bg]scale=720:1280:force_original_aspect_ratio=increase,crop=720:1280,gblur=sigma=30,eq=brightness=-0.24:saturation=0.5[back];'
        '[fg]scale=1440:-2,'
        "zoompan=z='min(1.08,1+on*0.00048)':x='iw/2-iw/zoom/2':y='ih/2-ih/zoom/2':d=165:s=720x406:fps=30,"
        'format=yuv420p[front];[back][front]overlay=0:350:shortest=0,'
        f"drawtext=fontfile=font.ttf:textfile=page{i}.txt:fontsize=22:fontcolor=0xBB9460:x=48:y=110,"
        f"drawtext=fontfile=font.ttf:textfile=title{i}.txt:fontsize=38:fontcolor=0xF5DCA7:x=(w-text_w)/2:y=840,"
        f"drawtext=fontfile=font.ttf:textfile=caption{i}.txt:fontsize=24:fontcolor=0xD6C6AF:line_spacing=16:x=(w-text_w)/2:y=925,"
        'fade=t=in:st=0:d=0.35,fade=t=out:st=5.15:d=0.35,format=yuv420p[v]'
    )
    subprocess.run([str(ffmpeg), '-hide_banner', '-loglevel', 'error', '-y', '-i', str(source),
                    '-filter_complex', graph, '-map', '[v]', '-frames:v', '165', '-an',
                    '-c:v', 'libx264', '-preset', 'fast', '-crf', '20', f'part{i}.mp4'], cwd=work, check=True)
(work / 'concat.txt').write_text(''.join(f"file 'part{i}.mp4'\n" for i in range(4)), encoding='utf-8')
output = root / 'Assets/JH/UI/Opening/Curse_Opening_Motion.mp4'
subprocess.run([str(ffmpeg), '-hide_banner', '-loglevel', 'error', '-y', '-f', 'concat', '-safe', '0', '-i', 'concat.txt',
                '-c', 'copy', '-movflags', '+faststart', str(output)], cwd=work, check=True)
preview = root / 'tmp/image-previews/sr18-presentation-progression-2026-09-10'
preview.mkdir(parents=True, exist_ok=True)
for i in range(4):
    subprocess.run([str(ffmpeg), '-hide_banner', '-loglevel', 'error', '-y', '-ss', str(i*5.5+2), '-i', str(output),
                    '-frames:v', '1', str(preview / f'opening-movie-{i+1}.png')], check=True)
print(f'Created 22-second 720x1280 motion comic: {output}')
