"""Create level-matched listening copies and loop masters without changing game audio."""
import hashlib
import html
import importlib.util
import json
import math
from pathlib import Path
import subprocess

import numpy as np

ROOT = Path(__file__).resolve().parents[1]
FOLDER = ROOT / "map-concepts/emotional-horror-bgm-2026-09-15"
OUT = FOLDER / "listen"
spec = importlib.util.spec_from_file_location("audio_tools", ROOT / "tools/prepare-mobile-audio.py")
audio = importlib.util.module_from_spec(spec)
spec.loader.exec_module(audio)


def run_ffmpeg(args):
    result = subprocess.run(
        [str(audio.FFMPEG), "-hide_banner", "-nostdin", *args],
        capture_output=True, text=True, encoding="utf-8", errors="replace",
        creationflags=subprocess.CREATE_NO_WINDOW, check=True,
    )
    return result.stderr


def levels(path):
    log = run_ffmpeg(["-i", str(path), "-af", "loudnorm=I=-18:TP=-2:LRA=11:print_format=json", "-f", "null", "-"])
    return json.loads(log[log.rfind("{"):log.rfind("}") + 1])


def trim_outer_silence(data, rate):
    """Remove only near-silent outer padding, retaining 100ms around musical content."""
    window = rate // 100
    frames = data[:len(data) // window * window].reshape(-1, window, 2)
    rms = np.sqrt(np.mean(frames * frames, axis=(1, 2)))
    active = np.flatnonzero(rms > 10 ** (-60 / 20))
    if not len(active):
        raise ValueError("Silent recording.")
    start = max(0, int(active[0]) * window - rate // 10)
    end = min(len(data), (int(active[-1]) + 1) * window + rate // 10)
    return data[start:end], start / rate, (len(data) - end) / rate


def main():
    jobs = json.loads((FOLDER / "raw/bgm-requests.json").read_text(encoding="utf-8"))["jobs"]
    if len(jobs) != 10:
        raise ValueError("Expected ten distinct candidate jobs.")
    OUT.mkdir(parents=True, exist_ok=True)
    if any(OUT.iterdir()):
        raise RuntimeError("Preserve previous listening copies; use a fresh output directory.")
    measurements = []
    playlist = ["#EXTM3U"]
    cards = []
    for job in jobs:
        source_folder = FOLDER / ("raw-retry-08" if job["name"] == "08_black_tide_requiem" else "raw")
        source = source_folder / (job["name"] + ".wav")
        data, rate = audio.read(source)
        if data.shape[1] != 2 or not np.isfinite(data).all() or len(data) / rate < 60:
            raise ValueError("Invalid generated recording: " + str(source))
        raw_saturated = int(np.sum(np.abs(data) >= 32766 / 32768))
        data, trimmed_start, trimmed_end = trim_outer_silence(data, rate)
        data = audio.loop(data - data.mean(axis=0), rate, 2)
        if np.sqrt(np.mean(data * data)) < .001:
            raise ValueError("Near-silent generated recording: " + str(source))
        data = audio.finish(data, .8)
        work = FOLDER / "raw" / (job["name"] + "-crossfade.wav")
        audio.write(work, data, rate)
        before = levels(work)
        # Constant gain preserves the crossfade; the true-peak ceiling takes precedence.
        gain_db = min(-18 - float(before["input_i"]), -2 - float(before["input_tp"]))
        master = data * 10 ** (gain_db / 20)
        wav = OUT / (job["name"] + ".wav")
        audio.write(wav, master, rate)
        for extension, codec in (("mp3", ["-c:a", "libmp3lame", "-b:a", "192k"]), ("ogg", ["-c:a", "libvorbis", "-q:a", "5"])):
            run_ffmpeg(["-v", "error", "-n", "-i", str(wav), *codec,
                        "-metadata", "title=" + job["title"],
                        "-metadata", "album=Emotional Horror BGM - 2026-09-15",
                        "-metadata", "artist=Original local AI-generated candidate",
                        str(wav.with_suffix("." + extension))])
        measured = levels(wav)
        # Inspect decoded MP3 as well, so export failures are detected independently.
        mp3_levels = levels(wav.with_suffix(".mp3"))
        seconds = len(master) / rate
        window = rate // 10
        frames = master[:len(master) // window * window].reshape(-1, window, 2)
        frame_rms = np.sqrt(np.mean(frames * frames, axis=(1, 2)))
        silent_frames = frame_rms < 10 ** (-55 / 20)
        longest = current = 0
        for silent in silent_frames:
            current = current + 1 if silent else 0
            longest = max(longest, current)
        row = dict(
            name=job["name"], title=job["title"], direction=job["direction"],
            seconds=seconds, sample_rate=rate, channels=2,
            source=str(source.relative_to(FOLDER)), raw_saturated_samples=raw_saturated,
            trimmed_start_seconds=trimmed_start, trimmed_end_seconds=trimmed_end,
            integrated_lufs=float(measured["input_i"]), true_peak_db=float(measured["input_tp"]),
            mp3_integrated_lufs=float(mp3_levels["input_i"]), mp3_true_peak_db=float(mp3_levels["input_tp"]),
            gain_db=gain_db, peak=float(np.max(np.abs(master))),
            seam_delta=float(np.max(np.abs(master[-1] - master[0]))),
            longest_near_silence_seconds=longest / 10,
            sha256=hashlib.sha256(wav.read_bytes()).hexdigest(),
        )
        if not math.isfinite(row["integrated_lufs"]) or row["true_peak_db"] > -.9 or row["mp3_true_peak_db"] > 0:
            raise ValueError("Invalid output level: " + str(row))
        if row["longest_near_silence_seconds"] > 3:
            raise ValueError("Long silent passage requires review: " + str(row))
        measurements.append(row)
        playlist.extend([f'#EXTINF:{int(seconds)},{job["title"]}', job["name"] + ".mp3"])
        name = html.escape(job["name"])
        cards.append(
            f'<article><h2><span>{name[:2]}</span> {html.escape(job["title"])}</h2><p>{html.escape(job["direction"])}</p>'
            f'<audio aria-label="{html.escape(job["title"])} 미리듣기" controls preload="none" src="{name}.mp3"></audio>'
            f'<p class="files"><a href="{name}.mp3" download>MP3</a> '
            f'<a href="{name}.wav" download>WAV</a> <a href="{name}.ogg" download>OGG</a> · {seconds:.0f}초</p></article>'
        )
        print(json.dumps(row, ensure_ascii=False), flush=True)
    if len({row["sha256"] for row in measurements}) != 10:
        raise ValueError("Duplicate candidate audio.")
    (FOLDER / "signal-report.json").write_text(json.dumps(measurements, indent=2, ensure_ascii=False), encoding="utf-8")
    (OUT / "all-10.m3u8").write_text("\n".join(playlist) + "\n", encoding="utf-8")
    (OUT / "index.html").write_text('''<!doctype html><html lang="ko"><meta charset="utf-8">
<meta name="viewport" content="width=device-width,initial-scale=1"><title>감성과 공포 · BGM 10곡</title>
<style>body{margin:0;background:#191a20;color:#eee8df;font:16px/1.6 "Malgun Gothic",system-ui,sans-serif}main{max-width:850px;margin:50px auto;padding:0 24px}h1{font-size:32px}header p{color:#bbb3ac}section{display:grid;grid-template-columns:repeat(auto-fit,minmax(270px,1fr));gap:18px}article{background:#25252e;border:1px solid #44414a;border-radius:12px;padding:20px}h2{font-size:19px;margin:0}h2 span{color:#d5b983;font-size:14px;margin-right:8px}article p{color:#c2b9b1}audio{width:100%;margin:8px 0}a{color:#edc893;margin-right:12px}a:focus-visible,audio:focus-visible{outline:2px solid #edc893;outline-offset:4px}.files{font-size:14px}</style>
<main><header><h1>감성과 공포 · BGM 10곡</h1><p>아련한 멜로디와 으스스한 분위기를 섞은 후보입니다. 곡별 설명은 제작 방향이며, 직접 들어 비교해 주세요.</p>
<p>각 약 1분 · MP3 미리듣기 · WAV / OGG 포함 · <a href="all-10.m3u8">전체 재생목록</a></p></header><section>'''
        + "\n".join(cards) + '''</section></main><script>document.querySelectorAll('audio').forEach(a=>a.addEventListener('play',()=>document.querySelectorAll('audio').forEach(b=>{if(b!==a)b.pause()})));</script></html>''', encoding="utf-8")
    print("COMPLETE: ten candidates, thirty audio exports, playlist and listening page", flush=True)


if __name__ == "__main__":
    main()
