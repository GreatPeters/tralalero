"""Retain the reviewed six-second animation and retime it to eight seconds."""
from pathlib import Path
import json
import subprocess

root = Path(__file__).resolve().parent.parent
folder = root / 'map-concepts/flow-opening-2026-09-12/revision-3'
source = folder / '02_Natural_Shark_Transforms.mp4'
destination = folder / 'reviewed/02_Natural_Shark_Transforms.mp4'
destination.parent.mkdir(parents=True, exist_ok=True)
ffmpeg = Path('C:/AI/ComfyUI-Creative-AMD/venv/Lib/site-packages/imageio_ffmpeg/binaries/ffmpeg-win-x86_64-v7.1.exe')

# The source develops a disappearing third foot around seven seconds. Keep
# frames0..143, which include the natural shark and complete transformation.
# Slow that moving footage to75%; preserve the original watermark and framing.
command = [
    str(ffmpeg), '-hide_banner', '-loglevel', 'error', '-n', '-i', str(source),
    '-filter_complex',
    '[0:v]trim=end_frame=144,setpts=(PTS-STARTPTS)*4/3,fps=24[v];'
    '[0:a]atrim=end=6,asetpts=PTS-STARTPTS,atempo=0.75,apad,atrim=end=8[a]',
    '-map', '[v]', '-map', '[a]', '-c:v', 'libx264', '-crf', '18',
    '-preset', 'slow', '-pix_fmt', 'yuv420p', '-c:a', 'aac', '-b:a', '192k',
    '-movflags', '+faststart', '-t', '8', str(destination),
]
subprocess.run(command, check=True)
record = {
    'source': source.relative_to(root).as_posix(),
    'output': destination.relative_to(root).as_posix(),
    'source_frames_kept': [0, 143],
    'source_seconds_kept': [0, 6],
    'playback_speed': 0.75,
    'output_target_seconds': 8,
    'reason': 'Exclude the late disappearing-foot defect; retain continuous generated motion.',
    'watermark': 'Preserved unchanged within the full original frame.',
    'command': command,
}
(destination.parent / '02-edit.json').write_text(json.dumps(record, indent=2), encoding='utf-8')
print(destination)
