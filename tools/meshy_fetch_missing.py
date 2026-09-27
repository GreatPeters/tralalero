"""Download outputs of SUCCEEDED Meshy tasks whose files are missing locally (e.g. a worker stopped mid-step).
Usage: py -3.11 tools/meshy_fetch_missing.py <run_dir>"""
import pathlib, sys
sys.path.insert(0, str(pathlib.Path(__file__).parent))
from meshy_client import Meshy, API
run = pathlib.Path(sys.argv[1]); m = Meshy(run, 10**9); m.refresh()
for e in m.ledger:
    if e.get("status") != "SUCCEEDED" or not e.get("task_id"): continue
    out = run / "models" / e["asset"]
    kind = e["kind"]
    if kind == "rig" and not (out / "rigged.fbx").exists():
        t = m.session.get(f"{API}/{e['endpoint']}/{e['task_id']}", timeout=60).json(); res = t["result"]
        m.download(res["rigged_character_fbx_url"], out / "rigged.fbx")
        for key, url in (res.get("basic_animations") or {}).items():
            if url and key.endswith("_fbx_url") and "armature" not in key: m.download(url, out / f"anim_{key.replace('_fbx_url', '')}.fbx")
        print("fetched rig", e["asset"])
    if kind == "model" and not list(out.glob("model*.glb")):
        t = m.session.get(f"{API}/{e['endpoint']}/{e['task_id']}", timeout=60).json()
        m.download(t["model_urls"]["glb"], out / "model1.glb")
        if t["model_urls"].get("fbx"): m.download(t["model_urls"]["fbx"], out / "model1.fbx")
        if t.get("thumbnail_url"): m.download(t["thumbnail_url"], out / "thumb1.png")
        print("fetched model", e["asset"])
    if kind.startswith("anim") and e.get("note") and not (out / f"anim_{e['note']}.fbx").exists():
        t = m.session.get(f"{API}/{e['endpoint']}/{e['task_id']}", timeout=60).json(); res = t["result"]
        m.download(res.get("processed_animation_fps_fbx_url") or res["animation_fbx_url"], out / f"anim_{e['note']}.fbx")
        print("fetched anim", e["asset"], e["note"])
print("done")
