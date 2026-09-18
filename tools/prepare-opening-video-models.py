"""Download the official ComfyOrg Wan2.2 5B files to the existing local ComfyUI.

No model is overwritten. Downloads are revision-pinned and SHA256-verified.
"""
from concurrent.futures import ThreadPoolExecutor
import hashlib
import json
from pathlib import Path
import urllib.request

manifest = json.loads(Path('tmp/wan22-model-manifest.json').read_text(encoding='utf-8'))
destination = Path('C:/AI/ComfyUI-Creative-AMD/ComfyUI/models')

def download(row):
    target = destination / Path(row['rfilename']).relative_to('split_files')
    target.parent.mkdir(parents=True, exist_ok=True)
    if target.exists():
        with target.open('rb') as stream:
            digest = hashlib.file_digest(stream, 'sha256').hexdigest()
        if digest != row['lfs']['sha256']:
            raise RuntimeError(f'Existing model differs; preserving {target}')
        return str(target)
    partial = target.with_suffix('.download')
    url = f"https://huggingface.co/Comfy-Org/Wan_2.2_ComfyUI_Repackaged/resolve/{manifest['revision']}/{row['rfilename']}?download=true"
    digest = hashlib.sha256()
    total = 0
    with urllib.request.urlopen(url, timeout=120) as response, partial.open('wb') as output:
        while chunk := response.read(8 * 1024 * 1024):
            output.write(chunk); digest.update(chunk); total += len(chunk)
    if total != row['size'] or digest.hexdigest() != row['lfs']['sha256']:
        raise RuntimeError(f'Download verification failed: {partial}')
    partial.rename(target)
    print(f'Verified {target.name}: {total} bytes', flush=True)
    return str(target)

with ThreadPoolExecutor(max_workers=3) as pool:
    paths = list(pool.map(download, manifest['files']))
Path('tmp/wan22-models-ready.json').write_text(json.dumps(paths, indent=2), encoding='utf-8')
