"""Generate two original music candidates with the installed Stable Audio 3 CPU CLI."""
import json
import os
from pathlib import Path
import subprocess
import time
import wave

ROOT = Path(__file__).resolve().parents[1]
ENGINE = Path("C:/AI/StableAudio3/optimized/tflite")
PYTHON = ENGINE / ".venv/Scripts/python.exe"
SCRIPT = ENGINE / "scripts/sa3_tflite.py"
OUT = ROOT / "map-concepts/mobile-presentation-2026-09-14/audio"
JOBS = [
    {
        "name": "a_midnight_tide",
        "seed": 26091401,
        "prompt": (
            "Instrumental soundtrack for a whimsical spooky cartoon arcade game, 75 BPM, "
            "gentle eerie music box and soft celesta, slightly detuned felt piano, a memorable "
            "original minor-key melody, quiet rounded plucked bass, subtle brushed percussion, "
            "mysterious and mischievous rather than frightening, nostalgic toy theatre, "
            "warm low end, spacious but clear mix, steady looping groove, no big finale"
        ),
    },
    {
        "name": "b_ghost_arcade",
        "seed": 26091402,
        "prompt": (
            "Instrumental playful dark arcade soundtrack, 90 BPM, quirky pizzicato strings "
            "and delicate glass bells, muted electric piano, gentle analog bass, a catchy "
            "original chromatic minor-key motif, restrained dry rim clicks and soft shakers, "
            "curious haunted seaside carnival atmosphere, cute and sneaky, understated tension, "
            "clean game background mix, consistent rhythm suitable for looping, no big finale"
        ),
    },
]


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    manifest = OUT / "bgm-requests.json"
    if manifest.exists():
        raise RuntimeError("Preserve earlier candidate requests and results.")
    manifest.write_text(json.dumps({
        "engine": str(ENGINE), "model": "sm-music", "decoder": "same-s",
        "precision": "fp32", "seconds": 64, "steps": 8, "cfg": 1.5,
        "threads": 8, "reference_recording_used": False, "jobs": JOBS,
    }, indent=2), encoding="utf-8")
    env = os.environ.copy()
    for key in ("OMP_NUM_THREADS", "OPENBLAS_NUM_THREADS", "MKL_NUM_THREADS", "NUMEXPR_NUM_THREADS"):
        env[key] = "8"
    for job in JOBS:
        target = OUT / (job["name"] + ".wav")
        if target.exists():
            raise RuntimeError("Preserve existing audio: " + str(target))
        command = [
            str(PYTHON), str(SCRIPT), "--dit", "sm-music", "--decoder", "same-s",
            "--precision", "fp32", "--seconds", "64", "--steps", "8", "--threads", "8",
            "--cfg", "1.5", "--negative-prompt",
            "vocals, singing, speech, lyrics, harsh distortion, loud impacts, abrupt ending",
            "--seed", str(job["seed"]), "--prompt", job["prompt"], "--out", str(target),
        ]
        began = time.monotonic()
        print("Generating " + job["name"], flush=True)
        with (OUT / (job["name"] + ".log")).open("w", encoding="utf-8") as log:
            result = subprocess.run(command, cwd=ENGINE, env=env, stdout=log, stderr=subprocess.STDOUT,
                creationflags=subprocess.CREATE_NO_WINDOW | subprocess.BELOW_NORMAL_PRIORITY_CLASS)
        if result.returncode != 0:
            raise RuntimeError("Generation failed; inspect " + job["name"] + ".log")
        with wave.open(str(target), "rb") as audio:
            stats = {"channels": audio.getnchannels(), "sample_rate": audio.getframerate(),
                     "frames": audio.getnframes(), "seconds": audio.getnframes() / audio.getframerate()}
        stats.update(name=job["name"], elapsed_seconds=time.monotonic() - began, bytes=target.stat().st_size)
        (OUT / (job["name"] + ".json")).write_text(json.dumps(stats, indent=2), encoding="utf-8")
        print(json.dumps(stats), flush=True)


if __name__ == "__main__":
    main()
