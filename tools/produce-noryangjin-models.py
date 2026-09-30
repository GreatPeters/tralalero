"""Resume-safe Meshy production for the approved Noryangjin review (2026-09-28)."""
import concurrent.futures
import json
from pathlib import Path
import sys

from meshy_client import Meshy

RUN = Path('outputs/meshy-noryangjin-fix-2026-09-28')
ASSETS = {'N03_forklift': 9000, 'N04_livefish_truck': 11000, 'N05_harbor_crane': 12000,
          'N06_red_lighthouse': 6500, 'N07_market_cat': 3500, 'N08_frozen_tuna': 4500}

def produce(asset):
    client = Meshy(RUN, cap=2192)
    entries = [e for e in client.ledger if e['asset'] == asset and e['kind'] == 'model']
    entry = entries[-1] if entries else None
    if entry and entry['status'] == 'CREATING':
        raise RuntimeError(f'{asset}: ambiguous submission; inspect remote tasks before retrying')
    if entry and entry['status'] in ('FAILED', 'CANCELED', 'FAILED_TO_CREATE'):
        raise RuntimeError(f'{asset}: review failed task before paying for another attempt')
    if not entry:
        reference = RUN / 'concepts' / asset / 'try1.png'
        if not reference.is_file():
            raise FileNotFoundError(reference)
        if client.balance() < 30:
            raise RuntimeError(f'{asset}: insufficient Meshy balance; use authorized TRELLIS fallback')
        entry = client.to_3d(asset, [reference], ASSETS[asset],
            texture_prompt='Clean colorful Korean cartoon mobile game prop, flat painted color, no photoreal dirt, no text, preserve distinct mechanical parts.',
            note='User approved homepage then explicitly authorized models and scene application, 2026-09-28')
        print(f'{asset}: submitted {entry["task_id"]}', flush=True)
    task = client.wait(entry, poll=10, timeout=2400)
    if task['status'] != 'SUCCEEDED':
        raise RuntimeError(f'{asset}: {task["status"]}')
    target = RUN / 'models' / asset
    for ext in ('glb', 'fbx'):
        output = target / f'model1.{ext}'
        if not output.exists():
            client.download(task['model_urls'][ext], output)
    if task.get('thumbnail_url') and not (target / 'thumb1.png').exists():
        client.download(task['thumbnail_url'], target / 'thumb1.png')
    print(f'{asset}: downloaded; credits={entry.get("consumed",entry["estimate"])}', flush=True)
    return asset

if __name__ == '__main__':
    chosen = sys.argv[1:] or list(ASSETS)
    with concurrent.futures.ThreadPoolExecutor(max_workers=3) as pool:
        futures = {pool.submit(produce, asset): asset for asset in chosen}
        failures = []
        for future in concurrent.futures.as_completed(futures):
            try:
                future.result()
            except Exception as error:
                failures.append(f'{futures[future]}: {type(error).__name__}: {error}')
                print(failures[-1], flush=True)
    if failures:
        sys.exit(1)
