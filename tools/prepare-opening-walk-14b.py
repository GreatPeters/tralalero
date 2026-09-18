"""Fetch only the two pinned official FP8 experts needed for the walk comparison."""
from concurrent.futures import ThreadPoolExecutor
import hashlib
import json
from pathlib import Path
import time
import urllib.request

ROOT = Path(__file__).resolve().parent.parent
OUT = ROOT / 'map-concepts/opening-walk-14b-2026-09-12'
MODELS = Path('C:/AI/ComfyUI-Creative-AMD/ComfyUI/models')
REPO = 'Comfy-Org/Wan_2.2_ComfyUI_Repackaged'
NAMES = {
    'split_files/diffusion_models/wan2.2_i2v_high_noise_14B_fp8_scaled.safetensors',
    'split_files/diffusion_models/wan2.2_i2v_low_noise_14B_fp8_scaled.safetensors',
    'split_files/vae/wan_2.1_vae.safetensors',
}
OUT.mkdir(parents=True, exist_ok=True)
manifest_path = OUT / 'models-manifest.json'
if manifest_path.exists():
    manifest = json.loads(manifest_path.read_text(encoding='utf-8'))
else:
    with urllib.request.urlopen(f'https://huggingface.co/api/models/{REPO}?blobs=true', timeout=60) as response:
        metadata = json.load(response)
    files = [row for row in metadata['siblings'] if row['rfilename'] in NAMES]
    if len(files) != len(NAMES) or any('lfs' not in row for row in files):
        raise RuntimeError('Incomplete model manifest')
    manifest = {'repository': REPO, 'revision': metadata['sha'], 'files': files}
    manifest_path.write_text(json.dumps(manifest, indent=2), encoding='utf-8')

def sha256(path):
    digest = hashlib.sha256()
    with path.open('rb') as stream:
        for chunk in iter(lambda: stream.read(8 * 1024 * 1024), b''):
            digest.update(chunk)
    return digest.hexdigest()

def download(row):
    target = MODELS / Path(row['rfilename']).relative_to('split_files')
    target.parent.mkdir(parents=True, exist_ok=True)
    expected = row['lfs']['sha256']
    if target.exists():
        if target.stat().st_size != row['size'] or sha256(target) != expected:
            raise RuntimeError(f'Existing model does not match; refusing overwrite: {target}')
        print(f'Verified existing {target.name}', flush=True)
        return {'path': str(target), 'sha256': expected, 'downloaded': False}
    partial = target.with_suffix(target.suffix + '.walk14b-download')
    began = time.monotonic()
    url = f'https://huggingface.co/{REPO}/resolve/{manifest["revision"]}/{row["rfilename"]}'
    for attempt in range(4):
        offset = partial.stat().st_size if partial.exists() else 0
        if offset == row['size']:
            break
        headers = {'Range': f'bytes={offset}-'} if offset else {}
        try:
            with urllib.request.urlopen(urllib.request.Request(url, headers=headers), timeout=90) as response:
                append = offset > 0 and response.status == 206
                if not append:
                    offset = 0
                downloaded = offset
                last_report = time.monotonic()
                with partial.open('ab' if append else 'wb') as stream:
                    while chunk := response.read(8 * 1024 * 1024):
                        stream.write(chunk)
                        downloaded += len(chunk)
                        if time.monotonic() - last_report >= 20:
                            print(f'{target.name}: {downloaded / row["size"]:.1%} ({downloaded / 1e9:.2f} GB)', flush=True)
                            last_report = time.monotonic()
            break
        except (OSError, TimeoutError) as error:
            print(f'Download retry {attempt + 1}: {target.name}: {error}', flush=True)
            if attempt == 3:
                raise
    if partial.stat().st_size != row['size'] or sha256(partial) != expected:
        raise RuntimeError(f'Checksum or length mismatch: {partial}')
    partial.rename(target)
    elapsed = time.monotonic() - began
    print(f'Installed {target.name}: SHA256 verified, {elapsed:.1f}s', flush=True)
    return {'path': str(target), 'sha256': expected, 'downloaded': True, 'seconds': elapsed}

with ThreadPoolExecutor(max_workers=2) as executor:
    results = list(executor.map(download, manifest['files']))
(OUT / 'models-ready.json').write_text(json.dumps(results, indent=2), encoding='utf-8')
print('All required models verified', flush=True)
