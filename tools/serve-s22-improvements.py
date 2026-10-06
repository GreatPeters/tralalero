"""Serve only curated deliverables on loopback, excluding save snapshots."""
from http.server import ThreadingHTTPServer,SimpleHTTPRequestHandler
from functools import partial
from pathlib import Path
root=Path(__file__).resolve().parents[1]/'outputs/s22-polish-2026-10-01/site'
ThreadingHTTPServer(('127.0.0.1',8798),partial(SimpleHTTPRequestHandler,directory=str(root))).serve_forever()
