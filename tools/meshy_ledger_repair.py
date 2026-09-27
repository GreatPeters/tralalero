"""Refresh non-terminal ledger rows from the Meshy API (status + consumed credits).
Usage: py -3.11 tools/meshy_ledger_repair.py <run_dir>"""
import json, pathlib, sys
sys.path.insert(0, str(pathlib.Path(__file__).parent))
from meshy_client import Meshy, API
m = Meshy(sys.argv[1], 10**9); m.refresh()
fixed = 0
for e in m.ledger:
    if e.get("task_id") and e.get("status") not in ("SUCCEEDED", "FAILED", "CANCELED", "FAILED_TO_CREATE") or (e.get("status") == "SUCCEEDED" and "consumed" not in e):
        r = m.session.get(f"{API}/{e['endpoint']}/{e['task_id']}", timeout=60)
        if r.status_code != 200: print("skip", e["asset"], e["kind"], r.status_code); continue
        t = r.json(); e["status"] = t.get("status", e["status"])
        if "consumed_credits" in t: e["consumed"] = t["consumed_credits"]
        m.owned.add(m._key(e)); fixed += 1
    elif e.get("status") == "CREATING" and not e.get("task_id"):
        e["status"] = "FAILED_TO_CREATE"; e["error"] = "process stopped before the API returned a task id"; m.owned.add(m._key(e)); fixed += 1
m._save()
print("rows refreshed", fixed, "estimated spend", m.spent(), "balance", m.balance(), "spent by balance", 4550 - m.balance())
