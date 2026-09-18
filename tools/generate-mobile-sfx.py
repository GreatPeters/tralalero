"""Generate ambience and a knife pass with the installed, bounded Stable Audio 3 CLI."""
import json
import os
from pathlib import Path
import subprocess
import time

ROOT = Path(__file__).resolve().parents[1]
ENGINE = Path("C:/AI/StableAudio3/optimized/tflite")
OUT = ROOT / "map-concepts/mobile-presentation-2026-09-14/audio/sfx-source"
JOBS = [
    ("harbor", "Quiet seaside harbor ambience, small gentle water laps against wooden dock, distant single seagull, soft natural ocean breeze, clean stereo background recording, no music, no voices"),
    ("traffic", "Soft distant highway traffic ambience, passing cars with gentle tire hiss, distant engine hum, steady restrained outdoor background, clean stereo recording, no horns, no music, no speech"),
    ("knife", "One short stylized cartoon sword swish followed by a light metallic blade ring, isolated fast airy whoosh, crisp attack and short decay, dry clean sound effect with silence between events, no music, no voices"),
]

def main():
    OUT.mkdir(parents=True, exist_ok=True)
    env = os.environ.copy()
    for key in ("OMP_NUM_THREADS", "OPENBLAS_NUM_THREADS", "MKL_NUM_THREADS", "NUMEXPR_NUM_THREADS"):
        env[key] = "8"
    records = []
    for index, (name, prompt) in enumerate(JOBS):
        target = OUT / (name + ".wav")
        if target.exists():
            raise RuntimeError("Preserve existing output: " + str(target))
        args = [str(ENGINE / ".venv/Scripts/python.exe"), str(ENGINE / "scripts/sa3_tflite.py"),
            "--dit", "sm-sfx", "--decoder", "same-s", "--precision", "fp32", "--seconds", "8", "--steps", "8",
            "--threads", "8", "--cfg", "1.5", "--seed", str(26091411 + index), "--prompt", prompt,
            "--negative-prompt", "music, vocals, speech, distortion, clipping", "--out", str(target)]
        began = time.monotonic()
        print("Generating " + name, flush=True)
        with (OUT / (name + ".log")).open("w", encoding="utf-8") as log:
            result = subprocess.run(args, cwd=ENGINE, env=env, stdout=log, stderr=subprocess.STDOUT,
                creationflags=subprocess.CREATE_NO_WINDOW | subprocess.BELOW_NORMAL_PRIORITY_CLASS)
        if result.returncode:
            raise RuntimeError("Generation failed: " + name)
        records.append(dict(name=name, prompt=prompt, seed=26091411 + index, elapsed_seconds=time.monotonic() - began,
                            bytes=target.stat().st_size, model="sm-sfx", seconds=8, steps=8, threads=8))
        (OUT / "manifest.json").write_text(json.dumps(records, indent=2), encoding="utf-8")
        print(json.dumps(records[-1]), flush=True)

if __name__ == "__main__":
    main()
