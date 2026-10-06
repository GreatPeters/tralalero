"""Serve only the local, copied visual evidence, on a discovered loopback port."""
import functools
import http.server
import json
import os
from pathlib import Path

root = Path(__file__).resolve().parents[1]
gallery = root / "tmp/image-previews/chapter4-reference-2026-10-02"
handler = functools.partial(http.server.SimpleHTTPRequestHandler, directory=str(gallery))
with http.server.ThreadingHTTPServer(("127.0.0.1", 0), handler) as server:
    receipt = {"url": f"http://127.0.0.1:{server.server_port}/", "pid": os.getpid(), "root": str(gallery)}
    (root / "outputs/chapter4-reference-2026-10-02/gallery-server.json").write_text(json.dumps(receipt), encoding="utf-8")
    server.serve_forever()
