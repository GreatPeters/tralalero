"""Reproduce the installed P04 image preprocessing without invoking TRELLIS."""
from pathlib import Path
import json

import numpy as np
from PIL import Image, ImageOps
from rembg import remove, new_session

root = Path(__file__).resolve().parents[1]
folder = root / 'outputs/reststop-production-2026-09-24/reviews/P04-preprocess'
folder.mkdir(parents=True, exist_ok=False)
source = Image.open(root / 'outputs/reststop-production-2026-09-24/inputs/P04.png')
assert (Path.home() / '.u2net/u2net.onnx').is_file()
session = new_session('u2net', providers=['CPUExecutionProvider'])
masked = remove(source, session=session)
masked.save(folder / 'after-rembg.png')
scale = min(1, 1024 / max(masked.size))
masked = masked.resize((int(masked.width * scale), int(masked.height * scale)), Image.Resampling.LANCZOS)
alpha = np.array(masked)[:, :, 3]
coords = np.argwhere(alpha > .8 * 255)
bbox = (int(coords[:, 1].min()), int(coords[:, 0].min()), int(coords[:, 1].max()), int(coords[:, 0].max()))
cx, cy = (bbox[0] + bbox[2]) / 2, (bbox[1] + bbox[3]) / 2
size = max(bbox[2] - bbox[0], bbox[3] - bbox[1])
cropped = masked.crop((cx - size // 2, cy - size // 2, cx + size // 2, cy + size // 2))
pixels = np.array(cropped).astype(np.float32) / 255
pixels = pixels[:, :, :3] * pixels[:, :, 3:4]
condition = Image.fromarray((pixels * 255).astype(np.uint8))
condition = ImageOps.expand(condition, border=10, fill=(0, 0, 0))
condition.save(folder / 'condition-image.png')
(folder / 'report.json').write_text(json.dumps({'source': str(source.filename), 'provider': 'CPUExecutionProvider',
    'method': 'Existing remove_background=True, max_size=1024, alpha threshold .8, square crop and padding=10',
    'alpha_bbox_after_resize': bbox, 'condition_size': condition.size, 'extra_trellis_requests': 0}, indent=2), encoding='utf8')
print(str(folder / 'condition-image.png'), flush=True)
