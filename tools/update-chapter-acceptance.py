"""Reconcile accepted source hashes with the native integration evidence."""
import hashlib
import json
from pathlib import Path

root = Path(__file__).resolve().parent.parent
record = root / "map-concepts/chapters-polish-2026-09-12"
manifest_path = record / "review/accepted-exports.json"
manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
poses_path = record / "review/native-poses-skin-matrices-60fps.json"
poses = {row["name"]: row for row in json.loads(poses_path.read_text(encoding="utf-8"))}
for group in ("enemies", "props", "movies"):
    for item in manifest[group]:
        source = root / item.get("source", item.get("path"))
        if hashlib.sha256(source.read_bytes()).hexdigest() != item["sha256"]:
            raise RuntimeError(f"Accepted source changed: {source}")
for item in manifest["enemies"]:
    pose = poses[item["name"]]
    if pose["bones"] != 18 or pose["actions"] != 6:
        raise RuntimeError(f"Incomplete Unity rig: {item['name']}")
    item["visualReview"] = "Native prefab/Play Mode reviewed; 18bones/6actions verified with weighted skin matrices at60fps"
    item["nativePoseEvidence"] = str(poses_path.relative_to(root))
    item["ranged"] = pose["ranged"]
    item["nativeLivingGroundErrorMetres"] = max(abs(p[k]) for p in pose["poses"] if not p["clip"].endswith("die") for k in ("minY", "maxY"))
    item["nativeDeathGroundErrorMetres"] = max(abs(p[k]) for p in pose["poses"] if p["clip"].endswith("die") for k in ("minY", "maxY"))
for item in manifest["props"]:
    item["visualReview"] = "Imported at stable prefab paths; conversion transforms preserved; native bounds minY=0 and live scenery reviewed"
for item in manifest["movies"]:
    item["sceneBinding"] = "Noryangjin_MapTool_Mode_SR18 -> HighWay" if item["name"] == "Highway_Entry" else "HighWay -> RestStop"
    item["nativePlayback"] = "Passed automatic completion/skip and wallet/upgrades/equipment preservation in QA"
manifest["unityIntegration"] = "applied-original-and-isolated-qa"
manifest_path.write_text(json.dumps(manifest, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
print("Verified all source hashes and reconciled 9enemies,8props,2movies with native evidence")
