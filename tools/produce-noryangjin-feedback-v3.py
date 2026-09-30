"""Explicit screenshot-feedback asset production; resume paid tasks, never duplicate unknown requests."""
from concurrent.futures import ThreadPoolExecutor, as_completed
from pathlib import Path
import json
import shutil
import sys
from meshy_client import Meshy

RUN=Path('outputs/meshy-noryangjin-feedback-v3-2026-09-28')
OLD=Path('outputs/meshy-noryangjin-interior-v2-2026-09-28')
MODELS={'N15_refrigeration_unit':11000,'N16_auction_counter':14000,'N17_tuna_ice_pallet':9000,'N18_coldstore_gateway':16000}
ACTIONS={'N13_merchant_male':{'bid':28,'call':29},'N14_merchant_female':{'bid':28}}

def request(client,asset,kind,submit):
    client.refresh();previous=[e for e in client.ledger if e['asset']==asset and e['kind']==kind]
    entry=previous[-1] if previous else None
    if entry and entry['status'] not in ('PENDING','SUCCEEDED'):
        raise RuntimeError(f'Inspect existing paid task before retrying: {asset}/{kind}/{entry["status"]}')
    if entry is None:entry=submit();print(asset,kind,'submitted',entry['task_id'],flush=True)
    result=client.wait(entry,poll=10,timeout=2400)
    if result['status']!='SUCCEEDED':raise RuntimeError(f'{asset}/{kind}: {result["status"]}')
    return result

def produce(asset):
    client=Meshy(RUN,cap=150);folder=RUN/'models'/asset;folder.mkdir(parents=True,exist_ok=True)
    def download(url,path):
        if url and not path.exists():client.download(url,path)
    if asset in MODELS:
        reference=RUN/'concepts'/asset/'try1.png'
        if not reference.is_file():raise FileNotFoundError(reference)
        result=request(client,asset,'model',lambda:client.to_3d(asset,[reference],MODELS[asset],texture_prompt='Professional Korean seafood cold warehouse. Clean readable industrial construction, brushed metal, white insulated panels, blue trim. Preserve all open spaces and apertures. No text or black doodles.',note='User screenshot feedback: create credible cold warehouse and live auction props; 2026-09-28'))
        for ext in ('glb','fbx'):download(result['model_urls'].get(ext),folder/f'model1.{ext}')
        download(result.get('thumbnail_url'),folder/'thumb1.png')
    else:
        # Preserve previous raw evidence and stage a complete new import source family.
        for path in (OLD/'models'/asset).glob('*.fbx'):
            dest=folder/path.name
            if not dest.exists():shutil.copy2(path,dest)
        ledger=json.loads((OLD/'ledger.json').read_text(encoding='utf-8'))
        rig=next(e['task_id'] for e in ledger if e['asset']==asset and e['kind']=='rig' and e['status']=='SUCCEEDED')
        for name,action in ACTIONS[asset].items():
            result=request(client,asset,f'anim{action}',lambda:client.animate(asset,rig,action,note=f'Cold auction {name} gesture'))
            data=result['result'];download(data.get('processed_animation_fps_fbx_url') or data.get('animation_fbx_url'),folder/f'anim_{name}.fbx')
    print(asset,'READY',flush=True)

if __name__=='__main__':
    assets=sys.argv[1:] or list(MODELS)+list(ACTIONS)
    unknown=set(assets)-set(MODELS)-set(ACTIONS)
    if unknown:raise SystemExit(f'Unknown asset IDs: {sorted(unknown)}')
    failed=[]
    with ThreadPoolExecutor(max_workers=3) as pool:
        jobs={pool.submit(produce,asset):asset for asset in assets}
        for task in as_completed(jobs):
            try:task.result()
            except Exception as error:failed.append(jobs[task]);print(jobs[task],str(error),flush=True)
    raise SystemExit(bool(failed))
