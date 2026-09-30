"""Resume-safe model/rig/animation production for the explicit visual replacement request."""
from concurrent.futures import ThreadPoolExecutor, as_completed
from pathlib import Path
import sys
from meshy_client import Meshy

RUN = Path('outputs/meshy-noryangjin-interior-v2-2026-09-28')
POLYS = {'N09_driven_turret':14000, 'N10_crab_aquarium':9000, 'N11_fish_counter':9000,
         'N12_foam_box':2000, 'N13_merchant_male':6500, 'N14_merchant_female':6500}
ACTIONS = {'idle':0, 'attack_once':51, 'hit':178, 'die':187}

def task(client, asset, kind, submit):
    client.refresh()
    entries = [x for x in client.ledger if x['asset']==asset and x['kind']==kind]
    entry = entries[-1] if entries else None
    if entry and entry['status'] not in ('PENDING','SUCCEEDED'):
        raise RuntimeError(f'{asset}/{kind}: inspect {entry["status"]} before another paid request')
    if entry is None:
        if client.balance()<30:
            raise RuntimeError('Meshy balance low; use the authorized TRELLIS alternative')
        entry = submit()
        print(asset,kind,'submitted',entry['task_id'],flush=True)
    result = client.wait(entry,poll=10,timeout=2400)
    if result['status']!='SUCCEEDED':
        raise RuntimeError(f'{asset}/{kind}: {result["status"]}')
    return entry,result

def produce(asset):
    client = Meshy(RUN,cap=2012)
    output = RUN/'models'/asset
    reference = RUN/'concepts'/asset/'try1.png'
    if not reference.is_file(): raise FileNotFoundError(reference)
    character = asset.startswith(('N13','N14'))
    model,result = task(client,asset,'model',lambda:client.to_3d(asset,[reference],POLYS[asset],
        pose='a-pose' if character else '', texture_prompt='Polished stylized Korean fish market game asset. Preserve clean colors, rounded forms, believable construction and readable details. No black scribbles or baked thick outline.',
        note='Explicit permission to replace unsuitable models/animations, not reuse-first; 2026-09-28'))
    def download(url,path):
        if url and not path.exists(): client.download(url,path)
    for ext in ('fbx','glb'): download(result['model_urls'].get(ext),output/f'model1.{ext}')
    download(result.get('thumbnail_url'),output/'thumb1.png')
    if character:
        rig,ret = task(client,asset,'rig',lambda:client.rig(asset,model['task_id'],1.7))
        for ext in ('fbx','glb'): download(ret['result'].get(f'rigged_character_{ext}_url'),output/f'rigged.{ext}')
        for key,url in ret['result'].get('basic_animations',{}).items():
            if key.endswith('_fbx_url') and 'armature' not in key: download(url,output/f'anim_{key.replace("_fbx_url","")}.fbx')
        for name,action in ACTIONS.items():
            _,anim = task(client,asset,f'anim{action}',lambda:client.animate(asset,rig['task_id'],action,note=name))
            res = anim['result']
            download(res.get('processed_animation_fps_fbx_url') or res.get('animation_fbx_url'),output/f'anim_{name}.fbx')
    print(asset,'READY',flush=True)

if __name__=='__main__':
    failures=[]
    with ThreadPoolExecutor(max_workers=3) as pool:
        jobs={pool.submit(produce,x):x for x in (sys.argv[1:] or list(POLYS))}
        for future in as_completed(jobs):
            try: future.result()
            except Exception as error:
                failures.append(f'{jobs[future]}: {error}')
                print(failures[-1],flush=True)
    sys.exit(bool(failures))
