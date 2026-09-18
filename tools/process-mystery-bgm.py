"""Prepare five playable loop masters without replacing the installed soundtrack."""
import importlib.util
import json
from pathlib import Path
import subprocess
import numpy as np

root = Path(__file__).resolve().parents[1]
spec = importlib.util.spec_from_file_location('audio_tools', root / 'tools/prepare-mobile-audio.py')
audio = importlib.util.module_from_spec(spec); spec.loader.exec_module(audio)
folder = root / 'map-concepts/combat-feedback-2026-09-14/audio'
output = folder / 'previews'; output.mkdir(parents=True, exist_ok=True)
report = []
for source in sorted((folder / 'candidates').glob('*.wav')):
    data, rate = audio.read(source)
    if not np.isfinite(data).all(): raise ValueError(source)
    master = audio.finish(audio.loop(data, rate, 2), .68)
    wav = output / (source.stem + '-loop.wav')
    if wav.exists(): raise RuntimeError('Preserve prior master: ' + str(wav))
    audio.write(wav, master, rate)
    audio.encode(wav, wav.with_suffix('.ogg'))
    subprocess.run([str(audio.FFMPEG), '-hide_banner', '-loglevel', 'error', '-i', str(wav),
        '-c:a', 'libmp3lame', '-b:a', '160k', str(wav.with_suffix('.mp3'))], check=True, creationflags=subprocess.CREATE_NO_WINDOW)
    report.append(dict(name=source.stem, seconds=len(master)/rate, sample_rate=rate,
        peak=float(np.max(np.abs(master))), rms_db=float(20*np.log10(np.sqrt(np.mean(master*master))+1e-10)),
        seam_delta=float(np.max(np.abs(master[-1]-master[0])))))
(folder / 'signal-report.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print(json.dumps(report,indent=2))
