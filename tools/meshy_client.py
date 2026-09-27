"""Budget-guarded Meshy API client for chapter asset production.

The API key is read from MESHY_API_KEY or %USERPROFILE%/.meshy/api_key. It is
never written into the repository, the ledger, or task logs.

Every paid call is recorded in <run>/ledger.json before and after it runs. A call
is refused when the spent total plus the documented cost would exceed the run
cap, so a crashed script cannot silently overspend on restart.

Usage from another script:
    from meshy_client import Meshy
    m = Meshy(run_dir, cap=2300)
    task = m.image_to_image(asset_id, prompt, refs, multi_view=True)
"""
from __future__ import annotations

import base64
import json
import os
import pathlib
import time

import requests

API = "https://api.meshy.ai/openapi"
# Documented credit costs (docs.meshy.ai/en/api/pricing, checked 2026-09-25).
COST = {
    "image-to-image:nano-banana": 3,
    "image-to-image:nano-banana-2": 6,
    "image-to-image:nano-banana-pro": 9,
    "text-to-image:nano-banana-2": 6,
    "text-to-image:nano-banana-pro": 9,
    "image-to-3d": 30,
    "multi-image-to-3d": 30,
    "retexture": 10,
    "remesh": 5,
    "rigging": 5,
    "animation": 3,  # per action
}


def api_key() -> str:
    key = os.environ.get("MESHY_API_KEY")
    if not key:
        path = pathlib.Path(os.environ["USERPROFILE"]) / ".meshy" / "api_key"
        key = path.read_text(encoding="ascii").strip()
    return key


def data_uri(path: str | os.PathLike) -> str:
    p = pathlib.Path(path)
    mime = "image/png" if p.suffix.lower() == ".png" else "image/jpeg"
    return f"data:{mime};base64," + base64.b64encode(p.read_bytes()).decode()


def _no_meshy_images():
    # User rule (2026-09-25): Meshy is for 3D models and textures only; images come from Codex.
    raise RuntimeError("Meshy image generation is disabled; create concept/reference images with Codex instead.")


class BudgetError(RuntimeError):
    pass


class Meshy:
    def __init__(self, run_dir: str | os.PathLike, cap: int):
        self.run = pathlib.Path(run_dir)
        self.run.mkdir(parents=True, exist_ok=True)
        self.ledger_path = self.run / "ledger.json"
        self.cap = cap
        self.ledger = json.loads(self.ledger_path.read_text(encoding="utf-8")) if self.ledger_path.exists() else []
        self.owned: set[str] = set()  # entries this process created or advanced
        self.session = requests.Session()
        self.session.headers["Authorization"] = f"Bearer {api_key()}"

    # ---- bookkeeping -------------------------------------------------
    def spent(self) -> int:
        return sum(e.get("consumed", e.get("estimate", 0)) for e in self.ledger if e.get("status") != "FAILED_TO_CREATE")

    def attempts(self, asset: str, kind: str) -> int:
        return sum(1 for e in self.ledger if e["asset"] == asset and e["kind"] == kind and e.get("status") != "FAILED_TO_CREATE")

    @staticmethod
    def _key(e: dict) -> str:
        return e.get("uid") or f"{e['asset']}|{e['kind']}|{e['created']}|{e.get('task_id', '')}"

    def _lock(self):
        lock = self.ledger_path.with_suffix(".lock")
        for _ in range(600):
            try:
                return os.open(lock, os.O_CREAT | os.O_EXCL | os.O_WRONLY), lock
            except FileExistsError:
                time.sleep(0.1)
        raise TimeoutError("ledger lock held too long: " + str(lock))

    def _merge_locked(self):
        disk = json.loads(self.ledger_path.read_text(encoding="utf-8")) if self.ledger_path.exists() else []
        merged = {self._key(e): e for e in disk}
        for e in self.ledger:
            if self._key(e) in self.owned:
                merged[self._key(e)] = e  # only this process's own rows override the disk copy
        self.ledger = sorted(merged.values(), key=lambda e: e["created"])

    def _save(self):
        # Several generation processes may share one run; merge under a lock so none drops another's rows.
        fd, lock = self._lock()
        try:
            self._merge_locked()
            tmp = self.ledger_path.with_suffix(f".{os.getpid()}.tmp")
            tmp.write_text(json.dumps(self.ledger, ensure_ascii=False, indent=1), encoding="utf-8")
            tmp.replace(self.ledger_path)
        finally:
            os.close(fd)
            os.remove(lock)

    def refresh(self):
        fd, lock = self._lock()
        try:
            self._merge_locked()
        finally:
            os.close(fd)
            os.remove(lock)

    def balance(self) -> int:
        r = self.session.get(f"{API}/v1/balance", timeout=30)
        r.raise_for_status()
        return r.json()["balance"]

    # ---- task plumbing -----------------------------------------------
    def _create(self, asset: str, kind: str, endpoint: str, body: dict, estimate: int, note: str = "") -> dict:
        self.refresh()
        if self.spent() + estimate > self.cap:
            raise BudgetError(f"{asset}/{kind}: {self.spent()}+{estimate} exceeds cap {self.cap}")
        entry = {"uid": f"{os.getpid()}-{time.time_ns()}", "asset": asset, "kind": kind, "endpoint": endpoint, "estimate": estimate, "note": note,
                 "created": time.strftime("%Y-%m-%dT%H:%M:%S"), "status": "CREATING"}
        self.ledger.append(entry)
        self.owned.add(self._key(entry))
        self._save()
        r = self.session.post(f"{API}/{endpoint}", json=body, timeout=120)
        if r.status_code >= 400:
            entry["status"] = "FAILED_TO_CREATE"
            entry["error"] = r.text[:500]
            self._save()
            raise RuntimeError(f"{endpoint} {r.status_code}: {r.text[:300]}")
        entry["task_id"] = r.json()["result"]
        entry["status"] = "PENDING"
        self.owned.add(self._key(entry))
        self._save()
        return entry

    def wait(self, entry: dict, poll: float = 6, timeout: float = 1800) -> dict:
        endpoint = entry["endpoint"]
        self.owned.add(self._key(entry))
        start = time.time()
        while True:
            r = self.session.get(f"{API}/{endpoint}/{entry['task_id']}", timeout=60)
            r.raise_for_status()
            task = r.json()
            status = task.get("status")
            if status in ("SUCCEEDED", "FAILED", "CANCELED"):
                entry["status"] = status
                if "consumed_credits" in task:
                    entry["consumed"] = task["consumed_credits"]
                if status != "SUCCEEDED":
                    entry["error"] = json.dumps(task.get("task_error"), ensure_ascii=False)[:500]
                self._save()
                (self.run / "tasks").mkdir(exist_ok=True)
                (self.run / "tasks" / f"{entry['asset']}.{entry['kind']}.{entry['task_id']}.json").write_text(
                    json.dumps(task, ensure_ascii=False, indent=1), encoding="utf-8")
                return task
            if time.time() - start > timeout:
                raise TimeoutError(f"{entry['asset']} {endpoint} {entry['task_id']}")
            time.sleep(poll)

    def download(self, url: str, dest: str | os.PathLike) -> pathlib.Path:
        dest = pathlib.Path(dest)
        dest.parent.mkdir(parents=True, exist_ok=True)
        with requests.get(url, stream=True, timeout=300) as r:
            r.raise_for_status()
            with open(dest, "wb") as f:
                for chunk in r.iter_content(1 << 16):
                    f.write(chunk)
        return dest

    # ---- endpoints ---------------------------------------------------
    def image_to_image(self, asset, prompt, refs, model="nano-banana-pro", multi_view=False, note=""):
        _no_meshy_images()
        body = {"ai_model": model, "prompt": prompt, "reference_image_urls": [data_uri(p) for p in refs]}
        if multi_view:
            body["generate_multi_view"] = True
        return self._create(asset, "concept", "v1/image-to-image", body, COST[f"image-to-image:{model}"], note)

    def text_to_image(self, asset, prompt, model="nano-banana-2", multi_view=False, aspect="1:1", note=""):
        _no_meshy_images()
        body = {"ai_model": model, "prompt": prompt}
        if multi_view:
            body["generate_multi_view"] = True
        else:
            body["aspect_ratio"] = aspect
        return self._create(asset, "concept", "v1/text-to-image", body, COST[f"text-to-image:{model}"], note)

    def to_3d(self, asset, images, polycount, pose="", texture_prompt="", note="", model_type="standard"):
        body = {"ai_model": "meshy-7.1", "should_texture": True, "enable_pbr": False,
                "texture_resolution": "2k", "should_remesh": True, "topology": "triangle",
                "target_polycount": polycount, "pose_mode": pose, "target_formats": ["glb", "fbx"],
                "model_type": model_type}
        if texture_prompt:
            body["texture_prompt"] = texture_prompt[:800]
        if len(images) == 1:
            body["image_url"] = images[0] if str(images[0]).startswith(("http", "data:")) else data_uri(images[0])
            return self._create(asset, "model", "v1/image-to-3d", body, COST["image-to-3d"], note)
        body["image_urls"] = [u if str(u).startswith(("http", "data:")) else data_uri(u) for u in images]
        return self._create(asset, "model", "v1/multi-image-to-3d", body, COST["multi-image-to-3d"], note)

    def retexture(self, asset, input_task_id, text_prompt="", image=None, note=""):
        body = {"input_task_id": input_task_id, "ai_model": "latest", "enable_original_uv": True, "enable_pbr": False}
        if text_prompt:
            body["text_style_prompt"] = text_prompt[:600]
        if image:
            body["image_style_url"] = data_uri(image)
        return self._create(asset, "retexture", "v1/retexture", body, COST["retexture"], note)

    def rig(self, asset, input_task_id, height=1.7, note=""):
        body = {"input_task_id": input_task_id, "height_meters": height}
        return self._create(asset, "rig", "v1/rigging", body, COST["rigging"], note)

    def animate(self, asset, rig_task_id, action_id, note=""):
        body = {"rig_task_id": rig_task_id, "action_id": action_id, "post_process": {"operation_type": "change_fps", "fps": 30}}
        return self._create(asset, f"anim{action_id}", "v1/animations", body, COST["animation"], note)
