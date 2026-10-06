"""Loopback evidence gallery only; no editor control or external upload."""
from pathlib import Path
from http.server import ThreadingHTTPServer,SimpleHTTPRequestHandler
from functools import partial
import json,os
root=Path(__file__).resolve().parents[1]
gallery=root/'tmp/image-previews/department-store-followup-2026-10-02'
if not (gallery/'index.html').exists():raise RuntimeError('Create completed gallery first')
server=ThreadingHTTPServer(('127.0.0.1',1625),partial(SimpleHTTPRequestHandler,directory=str(gallery)))
(root/'outputs/department-store-2026-10-02/followup-v4/gallery-server.json').write_text(json.dumps({'pid':os.getpid(),'url':'http://127.0.0.1:1625/','directory':str(gallery),'scope':'loopback image gallery only'},indent=2),encoding='utf-8')
server.serve_forever()
