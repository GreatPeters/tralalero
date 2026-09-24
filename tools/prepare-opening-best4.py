"""Assemble the user's four v5 opening shots without re-encoding; verify every frame."""
import argparse
import hashlib
import json
from pathlib import Path
import shutil
import subprocess

ROOT = Path(__file__).resolve().parents[1]
NAMES = ["01_Stolen_Shoe.mp4", "02_The_Curse.mp4", "03_The_Condition.mp4", "04_To_Noryangjin.mp4"]


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("source", type=Path)
    parser.add_argument("--ffmpeg", required=True)
    args = parser.parse_args()
    folder = ROOT / "map-concepts/opening-best4-v5-2026-09-23"
    folder.mkdir(exist_ok=True)
    target = ROOT / "Assets/JH/UI/Opening/Animated/Curse_Opening_Animated.mp4"
    candidate = folder / "opening-best4-v5.mp4"
    if candidate.exists():
        raise RuntimeError("Candidate already exists; preserve the previous evidence.")
    sources = [args.source / name for name in NAMES]
    for source in sources:
        if not source.is_file():
            raise FileNotFoundError(source)
    backup = folder / "opening-before-best4-v5.mp4"
    if not backup.exists():
        shutil.copy2(target, backup)
    local = folder / "sources"
    local.mkdir(exist_ok=True)
    for source in sources:
        shutil.copy2(source, local / source.name)
    listing = folder / "concat.txt"
    listing.write_text("".join(f"file 'sources/{name}'\n" for name in NAMES), encoding="utf-8")
    subprocess.run([args.ffmpeg, "-hide_banner", "-loglevel", "error", "-n", "-f", "concat",
                    "-safe", "0", "-i", str(listing), "-map", "0:v:0", "-c", "copy",
                    "-movflags", "+faststart", str(candidate)], check=True)

    def hashes(path):
        output = subprocess.run([args.ffmpeg, "-v", "error", "-i", str(path), "-map", "0:v:0",
                                 "-f", "framemd5", "-"], check=True, capture_output=True, text=True).stdout
        return [line.split(",")[-1].strip() for line in output.splitlines() if line and not line.startswith("#")]

    per_shot = [hashes(source) for source in sources]
    assert [len(frames) for frames in per_shot] == [192, 288, 240, 216]
    assert hashes(candidate) == [frame for frames in per_shot for frame in frames], "Decoded frames changed"
    sha = lambda p: hashlib.sha256(p.read_bytes()).hexdigest()
    report = {"sources": [{"name": p.name, "sha256": sha(p), "frames": len(frames)}
                          for p, frames in zip(sources, per_shot)],
              "sourceFolder": str(args.source), "candidateSha256": sha(candidate),
              "beforeSha256": sha(backup), "frames": 936, "fps": 24, "seconds": 39,
              "width": 720, "height": 1280, "sceneStartFrames": [0, 192, 480, 720],
              "sceneStartSeconds": [0, 8, 20, 30], "decodedFramesIdentical": True,
              "audio": "All four supplied sources contain video only."}
    (folder / "assembly.json").write_text(json.dumps(report, indent=2), encoding="utf-8")
    print(json.dumps(report, indent=2))


if __name__ == "__main__":
    main()
