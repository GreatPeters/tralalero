"""Remesh finished Meshy models to a lower triangle budget (5 credits each), keeping their textures.
Usage: py -3.11 tools/meshy_remesh.py <run_dir> <cap> <polycount> <asset> [...]
Output: <run_dir>/models/<asset>/remesh<poly>.fbx (+ .glb)."""
import pathlib, sys
sys.path.insert(0, str(pathlib.Path(__file__).parent))
from meshy_client import Meshy, COST
run, cap, poly = pathlib.Path(sys.argv[1]), int(sys.argv[2]), int(sys.argv[3])
m = Meshy(run, cap)
for asset in sys.argv[4:]:
    src = [e for e in m.ledger if e["asset"] == asset and e["kind"] == "model" and e.get("status") == "SUCCEEDED"][-1]
    body = {"input_task_id": src["task_id"], "topology": "triangle", "target_polycount": poly, "target_formats": ["glb", "fbx"]}
    e = m._create(asset, f"remesh{poly}", "v1/remesh", body, COST["remesh"])
    t = m.wait(e)
    if t["status"] != "SUCCEEDED": print(asset, "remesh failed", e.get("error"), flush=True); continue
    out = run / "models" / asset
    for fmt in ("fbx", "glb"):
        if t["model_urls"].get(fmt): m.download(t["model_urls"][fmt], out / f"remesh{poly}.{fmt}")
    print(asset, "remesh ok; texture urls:", bool(t.get("texture_urls")), "spent", m.spent(), flush=True)
