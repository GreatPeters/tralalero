"""Preserve all 936 selected opening frames; normalize only codec/timestamps."""
from pathlib import Path
import hashlib
import json
import shutil
import subprocess
import imageio_ffmpeg

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'outputs/startup-errors-2026-10-02'
SOURCE = ROOT / 'Assets/JH/UI/Opening/Animated/Curse_Opening_Animated.mp4'
BACKUP = OUT / 'before/Curse_Opening_Animated.mp4'
TARGET = OUT / 'Curse_Opening_Animated-baseline.mp4'
FF = imageio_ffmpeg.get_ffmpeg_exe()

OUT.mkdir(parents=True, exist_ok=True)
BACKUP.parent.mkdir(exist_ok=True)
if not BACKUP.exists():
    shutil.copy2(SOURCE, BACKUP)
allowed = {hashlib.sha256(BACKUP.read_bytes()).hexdigest()}
receipt = OUT / 'opening-encoding.json'
if receipt.exists():
    allowed.add(json.loads(receipt.read_text(encoding='utf-8'))['outputSha256'])
if hashlib.sha256(SOURCE.read_bytes()).hexdigest() not in allowed:
    raise RuntimeError('Opening content changed since this snapshot; use a fresh backup/output directory.')
assert imageio_ffmpeg.count_frames_and_secs(str(BACKUP)) == (936, 39.0)
subprocess.run([
    FF, '-hide_banner', '-loglevel', 'error', '-y', '-i', str(BACKUP),
    '-map', '0:v:0', '-map', '0:a?', '-vf', 'setpts=N/(24*TB)',
    '-r', '24', '-fps_mode', 'cfr', '-frames:v', '936',
    '-c:v', 'libx264', '-preset', 'slow', '-threads', '4', '-crf', '17',
    '-profile:v', 'baseline', '-level:v', '3.1', '-pix_fmt', 'yuv420p',
    '-bf', '0', '-refs', '1', '-g', '48', '-keyint_min', '48',
    '-sc_threshold', '0', '-video_track_timescale', '24000',
    '-avoid_negative_ts', 'make_zero', '-c:a', 'copy', '-movflags', '+faststart', str(TARGET)
], check=True)
count, duration = imageio_ffmpeg.count_frames_and_secs(str(TARGET))
assert (count, duration) == (936, 39.0)
probe = subprocess.run([FF, '-hide_banner', '-i', str(TARGET)], capture_output=True).stderr.decode('utf-8', errors='replace')
assert 'Constrained Baseline' in probe and '24 fps' in probe
quality = subprocess.run([FF, '-hide_banner', '-i', str(BACKUP), '-i', str(TARGET),
    '-lavfi', '[0:v]settb=AVTB,setpts=PTS-STARTPTS[a];[1:v]settb=AVTB,setpts=PTS-STARTPTS[b];[a][b]psnr',
    '-f', 'null', '-'], capture_output=True, check=True).stderr.decode('utf-8', errors='replace')
(OUT / 'opening-codec.txt').write_text(probe, encoding='utf-8')
(OUT / 'opening-psnr.txt').write_text(next(line for line in quality.splitlines() if 'PSNR y:' in line), encoding='utf-8')
report = dict(frames=count, duration=duration, profile='Constrained Baseline', bFrames=0,
    sourceSha256=hashlib.sha256(BACKUP.read_bytes()).hexdigest(),
    outputSha256=hashlib.sha256(TARGET.read_bytes()).hexdigest(),
    sourceBytes=BACKUP.stat().st_size, outputBytes=TARGET.stat().st_size)
(OUT / 'opening-encoding.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
print(json.dumps(report))
