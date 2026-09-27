"""Generate Meshy multi-view concept sheets for chapter characters/props.

Usage: py -3.11 tools/meshy_reststop_concepts.py <run_dir> <cap> <asset_id> [<asset_id> ...]
Prompts live in <run_dir>/prompts.json as {asset_id: {"kind", "prompt", "refs"}}.
Concept images are cheap gates: 3D conversion only runs after a visual review.
"""
import json
import pathlib
import sys

sys.path.insert(0, str(pathlib.Path(__file__).parent))
from meshy_client import Meshy  # noqa: E402

STYLE = ("Match the exact 3D art style of the reference characters: stylized mobile-game 3D cartoon, "
         "chunky stubby proportions about three heads tall with a noticeably large head, big hands and feet, caricature face with small "
         "eyes, matte soft painted colors, simple readable shapes, no glossy plastic look. ")
POSE = "Full body, A-pose with arms angled 30 degrees away from the body, legs apart, facing the viewer, plain white background, no ground shadow, no lettering anywhere. "


def main():
    run, cap, ids = pathlib.Path(sys.argv[1]), int(sys.argv[2]), sys.argv[3:]
    prompts = json.loads((run / "prompts.json").read_text(encoding="utf-8"))
    m = Meshy(run, cap)
    for asset in ids:
        spec = prompts[asset]
        out = run / "concepts" / asset
        n = m.attempts(asset, "concept")
        refs = [run / r for r in spec.get("refs", [])]
        if spec["kind"] == "person":
            prompt = STYLE + spec["prompt"] + " " + POSE
            entry = m.image_to_image(asset, prompt, refs, model=spec.get("model", "nano-banana-pro"), multi_view=True)
        else:
            prompt = spec["prompt"]
            if refs:
                entry = m.image_to_image(asset, prompt, refs, model=spec.get("model", "nano-banana-2"), multi_view=spec.get("multi_view", False))
            else:
                entry = m.text_to_image(asset, prompt, model=spec.get("model", "nano-banana-2"), multi_view=spec.get("multi_view", False))
        task = m.wait(entry)
        if task["status"] != "SUCCEEDED":
            print(asset, "FAILED", entry.get("error"))
            continue
        for i, url in enumerate(task.get("image_urls", [])):
            m.download(url, out / f"try{n + 1}_{i}.png")
        print(asset, "ok", len(task.get("image_urls", [])), "images; spent", m.spent())


if __name__ == "__main__":
    main()
