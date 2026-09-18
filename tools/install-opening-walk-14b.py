"""Replace only the last opening shot, verifying decoded frames before installation."""
from pathlib import Path
import hashlib,json,shutil,subprocess

root=Path(__file__).resolve().parent.parent
ffmpeg=Path('C:/AI/ComfyUI-Creative-AMD/venv/Lib/site-packages/imageio_ffmpeg/binaries/ffmpeg-win-x86_64-v7.1.exe')
work=root/'map-concepts/opening-walk-14b-2026-09-12/installed'
work.mkdir(exist_ok=True)
target=root/'Assets/JH/UI/Opening/Animated/Curse_Opening_Animated.mp4'
backup=work/'opening-before-14b.mp4'
candidate=work/'opening-with-14b.mp4'
if backup.exists() or candidate.exists():raise RuntimeError('Preserve existing installation evidence; inspect before rerunning.')
shots=[root/f'map-concepts/opening-animation-2026-09-11/shot-{i:02d}.mp4' for i in range(1,4)]
last=root/'map-concepts/opening-walk-14b-2026-09-12/shot-04-14b-fp8.mp4'
shots.append(last)
listing=work/'concat.txt';listing.write_text(''.join(f"file '{p.as_posix()}'\n" for p in shots),encoding='utf-8')
subprocess.run([str(ffmpeg),'-hide_banner','-loglevel','error','-f','concat','-safe','0','-i',str(listing),'-c','copy','-movflags','+faststart',str(candidate)],check=True)
def frames(path):
    result=subprocess.run([str(ffmpeg),'-hide_banner','-loglevel','error','-i',str(path),'-map','0:v:0','-f','framemd5','-'],check=True,capture_output=True,text=True)
    return [line.split(',')[-1].strip() for line in result.stdout.splitlines() if line and not line.startswith('#')]
old,new,replacement=frames(target),frames(candidate),frames(last)
assert len(old)==len(new)==484 and len(replacement)==121
assert old[:363]==new[:363], 'Earlier three shots changed'
assert new[363:]==replacement, 'Installed last shot differs from14B source'
assert old[363:]!=new[363:], 'No actual replacement'
shutil.copy2(target,backup);shutil.copy2(candidate,target)
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
report={'frames':484,'fps':24,'seconds':484/24,'preservedFirstFrames':363,'replacementFrames':121,'beforeSha256':sha(backup),'installedSha256':sha(target),'lastShotSha256':sha(last),'generationSeconds':3244.136,'installedAsset':str(target)}
(work/'installation.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print(json.dumps(report,indent=2))
