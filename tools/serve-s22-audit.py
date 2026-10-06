"""Serve only the curated report, on loopback, for the user's external browser."""
from functools import partial
from http.server import ThreadingHTTPServer, SimpleHTTPRequestHandler
from pathlib import Path
root=Path(__file__).resolve().parents[1]/'outputs/s22-quality-audit-2026-10-01/site'
ThreadingHTTPServer(('127.0.0.1',8797),partial(SimpleHTTPRequestHandler,directory=str(root))).serve_forever()
