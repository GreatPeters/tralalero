from pathlib import Path
import subprocess
import imageio_ffmpeg

base=Path('tmp/image-previews/talisman-polish-2026-09-20/video')
for folder in base.iterdir():
    if not folder.is_dir() or not (folder/'record.json').exists():
        continue
    subprocess.run([imageio_ffmpeg.get_ffmpeg_exe(),'-y','-framerate','30','-i',str(folder/'frame-%03d.png'),'-c:v','libx264','-crf','18','-pix_fmt','yuv420p','-movflags','+faststart',str(base/(folder.name+'.mp4'))],check=True,capture_output=True)
    print(base/(folder.name+'.mp4'))
