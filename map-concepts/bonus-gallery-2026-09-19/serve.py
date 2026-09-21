from pathlib import Path
from http.server import ThreadingHTTPServer, SimpleHTTPRequestHandler
from functools import partial
import json

root = Path(__file__).resolve().parents[2]
gallery = root / "tmp/image-previews/bonus-gallery-2026-09-19"
handler = partial(SimpleHTTPRequestHandler, directory=str(gallery))
server = ThreadingHTTPServer(("127.0.0.1", 0), handler)
url = f"http://127.0.0.1:{server.server_port}/"
(gallery / "server.json").write_text(json.dumps({"url": url}), encoding="utf-8")
print(url, flush=True)
server.serve_forever()

