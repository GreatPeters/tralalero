"""Serve only the curated reference-fit gallery on loopback (port 8799)."""
from http.server import ThreadingHTTPServer,SimpleHTTPRequestHandler
from functools import partial
from pathlib import Path
root=Path(__file__).resolve().parents[1]/'outputs/tralalero-reference-2026-10-01/site'
ThreadingHTTPServer(('127.0.0.1',8799),partial(SimpleHTTPRequestHandler,directory=str(root))).serve_forever()
