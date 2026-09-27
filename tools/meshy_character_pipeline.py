"""Run model -> rig -> animations for approved Meshy character concepts, sequentially.

Usage: py -3.11 tools/meshy_character_pipeline.py <run_dir> <cap> <plan.json> [asset ...]
plan.json: {asset: {"order": [2,0,1], "poly": 4000, "anims": {"idle": 0, "attack_once": 421, ...}}}
Steps already present on disk are skipped, so a restart never pays twice.
"""
import json, pathlib, sys
sys.path.insert(0, str(pathlib.Path(__file__).parent))
from meshy_client import Meshy

run, cap = pathlib.Path(sys.argv[1]), int(sys.argv[2])
plan = json.loads(pathlib.Path(sys.argv[3]).read_text(encoding="utf-8"))
only = sys.argv[4:] or list(plan)
m = Meshy(run, cap)

def ok(asset, kind):
    return [e for e in m.ledger if e["asset"] == asset and e["kind"] == kind and e.get("status") == "SUCCEEDED"]

def pending(asset, kind):
    # A task submitted by an interrupted run is resumed instead of paid for again.
    m.refresh()
    live = [e for e in m.ledger if e["asset"] == asset and e["kind"] == kind and e.get("status") == "PENDING" and e.get("task_id")]
    return live[-1] if live else None

for asset in only:
    spec = plan[asset]; out = run / "models" / asset; out.mkdir(parents=True, exist_ok=True)
    tag = spec.get("tag", "try1")
    if not ok(asset, "model"):
        views = sorted((run / "concepts" / asset).glob(f"{tag}_*.png"))
        if not views:
            print(asset, "concept not ready; skipped", flush=True); continue
        order = spec.get("order", list(range(len(views))))
        views = [str(views[i]) for i in order]
        e = pending(asset, "model") or m.to_3d(asset, views, spec.get("poly", 4000), pose=spec.get("pose", "a-pose"), note=f"views={tag} order={order}")
        t = m.wait(e)
        if t["status"] != "SUCCEEDED": print(asset, "model failed", e.get("error"), flush=True); continue
        n = len(ok(asset, "model"))
        m.download(t["model_urls"]["glb"], out / f"model{n}.glb")
        if t["model_urls"].get("fbx"): m.download(t["model_urls"]["fbx"], out / f"model{n}.fbx")
        if t.get("thumbnail_url"): m.download(t["thumbnail_url"], out / f"thumb{n}.png")
        print(asset, "model ok; spent", m.spent(), flush=True)
    if spec.get("rig", True) and not ok(asset, "rig"):
        e = pending(asset, "rig") or m.rig(asset, ok(asset, "model")[-1]["task_id"], spec.get("height", 1.7))
        t = m.wait(e)
        if t["status"] != "SUCCEEDED": print(asset, "rig failed", e.get("error"), flush=True); continue
        res = t["result"]
        m.download(res["rigged_character_fbx_url"], out / "rigged.fbx")
        for key, url in (res.get("basic_animations") or {}).items():
            if url and key.endswith("_fbx_url") and "armature" not in key:
                m.download(url, out / f"anim_{key.replace('_fbx_url', '')}.fbx")
        print(asset, "rig ok; spent", m.spent(), flush=True)
    for clip, action in spec.get("anims", {}).items():
        if (out / f"anim_{clip}.fbx").exists(): continue
        e = pending(asset, f"anim{action}") or m.animate(asset, ok(asset, "rig")[-1]["task_id"], action, note=clip)
        t = m.wait(e)
        if t["status"] != "SUCCEEDED": print(asset, clip, "anim failed", e.get("error"), flush=True); continue
        res = t["result"]
        m.download(res.get("processed_animation_fps_fbx_url") or res["animation_fbx_url"], out / f"anim_{clip}.fbx")
    print(asset, "done; spent", m.spent(), flush=True)
