"""Assemble approved Flow shots1/2 with the currently installed shots3/4."""
from pathlib import Path
import hashlib
import json
import shutil
import subprocess

root = Path(__file__).resolve().parent.parent
work = root / 'map-concepts/flow-opening-2026-09-12/installed'
work.mkdir(exist_ok=True)
target = root / 'Assets/JH/UI/Opening/Animated/Curse_Opening_Animated.mp4'
backup = work / 'opening-before-flow.mp4'
candidate = work / 'opening-with-flow-cfr.mp4'
if candidate.exists():
    raise RuntimeError('Installation evidence already exists; inspect before rerunning.')
if backup.exists():
    assert backup.read_bytes() == target.read_bytes(), 'Installed movie changed after backup'
else:
    shutil.copy2(target, backup)
ffmpeg = 'C:/AI/ComfyUI-Creative-AMD/venv/Lib/site-packages/imageio_ffmpeg/binaries/ffmpeg-win-x86_64-v7.1.exe'
reviewed = root / 'map-concepts/flow-opening-2026-09-12/revision-3/reviewed'
shots = [reviewed/'01_Escape_With_Shoe.mp4', reviewed/'02_Natural_Shark_Transforms.mp4']
# Keep all242 existing frames of scenes3/4. Normalize resolution for one portable
# H.264 stream. The existing game movie is silent; preserve that audio contract.
filters = (
    '[0:v]fps=24,setsar=1,setpts=PTS-STARTPTS[a];'
    '[1:v]fps=24,setsar=1,setpts=PTS-STARTPTS[b];'
    '[2:v]trim=start_frame=242:end_frame=484,setpts=PTS-STARTPTS,'
    'scale=720:1280:flags=lanczos,setsar=1[c];'
    '[a][b][c]concat=n=3:v=1:a=0,settb=1/24,setpts=N[out]'
)
subprocess.run([ffmpeg,'-hide_banner','-loglevel','error','-n',
    '-i',str(shots[0]),'-i',str(shots[1]),'-i',str(backup),
    '-filter_complex',filters,'-map','[out]','-an','-c:v','libx264',
    '-crf','18','-preset','slow','-pix_fmt','yuv420p','-r','24','-movflags','+faststart',str(candidate)],check=True)
sha = lambda path: hashlib.sha256(path.read_bytes()).hexdigest()
report = {'candidate':str(candidate),'backup':str(backup),'target':str(target),
    'sourceSha256':[sha(p) for p in shots], 'beforeSha256':sha(backup),
    'candidateSha256':sha(candidate),'expectedFrames':626,'fps':24,
    'sceneStartFrames':[0,192,384,505],'sceneStartSeconds':[0,8,16,505/24],
    'seconds':626/24,'preservedOldTailFrames':242,
    'audio':'Silent, matching the previous installed opening; Flow source audio retained in source files.'}
(work/'assembly.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print(json.dumps(report,indent=2))
