"""Convert an approved Meshy concept into a textured 3D model, then optionally rig/animate.

Usage:
  py -3.11 tools/meshy_reststop_models.py <run_dir> <cap> model <asset_id> <polycount> [try_tag] [pose]
  py -3.11 tools/meshy_reststop_models.py <run_dir> <cap> rig <asset_id> [height_m]
  py -3.11 tools/meshy_reststop_models.py <run_dir> <cap> anim <asset_id> <action_id> <clip_name>

Downloads land under <run_dir>/models/<asset_id>/.
"""
import json
import pathlib
import sys

sys.path.insert(0, str(pathlib.Path(__file__).parent))
from meshy_client import Meshy  # noqa: E402


def latest(m, asset, kind):
    done = [e for e in m.ledger if e["asset"] == asset and e["kind"] == kind and e.get("status") == "SUCCEEDED"]
    if not done:
        raise SystemExit(f"no succeeded {kind} for {asset}")
    return done[-1]


def main():
    run, cap, op, asset = pathlib.Path(sys.argv[1]), int(sys.argv[2]), sys.argv[3], sys.argv[4]
    m = Meshy(run, cap)
    out = run / "models" / asset
    if op == "model":
        poly = int(sys.argv[5])
        tag = sys.argv[6] if len(sys.argv) > 6 else "try1"
        pose = sys.argv[7] if len(sys.argv) > 7 else ""
        views = sorted((run / "concepts" / asset).glob(f"{tag}_*.png"))
        if not views:
            views = sorted((run / "concepts" / asset).glob(f"{tag}.png"))
        n = m.attempts(asset, "model") + 1
        entry = m.to_3d(asset, [str(v) for v in views], poly, pose=pose, note=f"views={tag}")
        task = m.wait(entry)
        if task["status"] != "SUCCEEDED":
            raise SystemExit(f"{asset} model FAILED {entry.get('error')}")
        urls = task["model_urls"]
        m.download(urls["glb"], out / f"model{n}.glb")
        if urls.get("fbx"):
            m.download(urls["fbx"], out / f"model{n}.fbx")
        for i, (k, u) in enumerate(sorted((task.get("thumbnail_urls") or {}).items())):
            m.download(u, out / f"thumb{n}_{k}.png")
        if task.get("thumbnail_url"):
            m.download(task["thumbnail_url"], out / f"thumb{n}.png")
    elif op == "rig":
        height = float(sys.argv[5]) if len(sys.argv) > 5 else 1.7
        model = latest(m, asset, "model")
        entry = m.rig(asset, model["task_id"], height)
        task = m.wait(entry)
        if task["status"] != "SUCCEEDED":
            raise SystemExit(f"{asset} rig FAILED {entry.get('error')}")
        res = task.get("result", {})
        for key in ("rigged_character_fbx_url", "rigged_character_glb_url"):
            if res.get(key):
                m.download(res[key], out / ("rigged." + key.split("_")[-2]))
        basic = res.get("basic_animations", {})
        for key, url in basic.items():
            if url and key.endswith("_fbx_url") and "armature" not in key:
                m.download(url, out / f"anim_{key.replace('_fbx_url', '')}.fbx")
    elif op == "anim":
        action, clip = int(sys.argv[5]), sys.argv[6]
        rig = latest(m, asset, "rig")
        entry = m.animate(asset, rig["task_id"], action, note=clip)
        task = m.wait(entry)
        if task["status"] != "SUCCEEDED":
            raise SystemExit(f"{asset} anim FAILED {entry.get('error')}")
        res = task.get("result", {})
        url = res.get("processed_animation_fps_fbx_url") or res.get("animation_fbx_url")
        m.download(url, out / f"anim_{clip}.fbx")
    print(asset, op, "done; spent", m.spent())


if __name__ == "__main__":
    main()
