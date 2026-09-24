"""Use only the remaining shared texture budget for a visually accepted manual shape.

Requires another batch item to be held at an explicit review gate so the one GPU
stays serialized. Original automatic ledgers/rejections are never modified.
"""
import argparse,json,shutil,subprocess,sys,time
from pathlib import Path

ROOT=Path(__file__).resolve().parents[1];OUT=ROOT/'outputs/reststop-production-2026-09-24'
APP=next(p for p in (Path.home()/'Desktop').glob('AI */Trellis */*') if (p/'engine.py').is_file())
sys.path.insert(0,str(APP))
import engine
from quality_loop import texture_prompt
from quality_staged import validate,limits
from reststop_prompt_wait import wait_with_history_recheck
from reststop_texture_retry import previous_attempt_allows_retry
parser=argparse.ArgumentParser();parser.add_argument('--id',required=True);parser.add_argument('--revision',required=True)
parser.add_argument('--parent-mesh',type=int,required=True);parser.add_argument('--attempt',type=int,required=True)
parser.add_argument('--source',type=Path,required=True)
parser.add_argument('--held-stage',choices=('shape','texture_source','final_compare'),default='final_compare',help='Explicit held main-batch review stage; never release it while this GPU request is active')
args=parser.parse_args()
assert args.id in {f'B{i:02}' for i in range(1,13)}|{f'E{i:02}' for i in range(1,8)}|{f'F{i:02}' for i in range(1,12)}|{f'G{i:02}' for i in range(1,5)}|{f'P{i:02}' for i in range(1,7)}|{f'R{i:02}' for i in range(1,13)}|{f'S{i:02}' for i in range(1,16)}|{f'T{i:02}' for i in range(1,10)}|{f'V{i:02}' for i in range(1,7)}
assert args.revision.startswith('r') and args.revision[1:].isdigit()
source=args.source.resolve();assert source.is_file()
assert json.loads((source.parent/'visual-review.json').read_text(encoding='utf8'))['verdict']=='pass'
image=OUT/'inputs'/(args.id+'.png');image_hash=engine.sha_file(image);shape_hash=engine.sha_file(source)
original_path=OUT/'quality-ledgers'/(image_hash+'.json');original=engine.read_json(original_path,{})
validate(original,image_hash);assert original['terminal'] in ('review_needed','halted')
assert 1<=args.parent_mesh<=len(original['meshes']),'Invalid parent shape index'
parent=original['meshes'][args.parent_mesh-1]
if original['terminal']=='halted':
    recovery=engine.read_json(source.parent/'completed-prompt-recovery.json',{})
    assert recovery.get('original_prompt_id')==parent.get('prompt_id')
    assert recovery.get('image_hash')==image_hash and recovery.get('source_sha256')==shape_hash
    recovered_history=engine.read_json(source.parent/'trellis_history.json',{})
    assert recovered_history.get('status',{}).get('status_str')=='success'
    assert recovered_history['prompt'][1]==parent['prompt_id'], 'Require proof of this exact completed shape'
original_textures=sum(len(m['textures']) for m in original['meshes']);cap=limits(original)['textures_total']
opts=engine.read_json(ROOT/'map-concepts/reststop-production-2026-09-24/effective-settings.json',{})
assert original['settings']=={k:opts[k] for k in original['settings']}
ledger_dir=OUT/'manual-texture-ledgers';ledger_dir.mkdir(exist_ok=True);ledger_path=ledger_dir/(image_hash+'.json')
client=engine.TrellisClient(opts['trellis_url'],opts['trellis_root'])
assert client.request('/task-uv-raster-status')['implementation']=='independent-uv-math-v1'
assert client.ready()
def notify(message):
    engine.atomic_json(OUT/'manual-generation-status.json',{'asset':args.id,'message':message,'updated':time.time()})
def gpu_window():
    gate=engine.read_json(OUT/'review-pending.json',{})
    assert gate.get('state')=='pending' and gate.get('stage')==args.held_stage and not Path(gate['result']).exists(), 'Hold the explicitly named real review gate before manual GPU work'
    queue=client.request('/queue');assert not queue.get('queue_running') and not queue.get('queue_pending'),'GPU queue must be idle'
    return gate['result']
with engine.OutputLock(ledger_path.with_suffix('.lock')):
    ledger=engine.read_json(ledger_path,{'image_hash':image_hash,'settings':original['settings'],'original_ledger':str(original_path),'attempts':[]})
    assert ledger['image_hash']==image_hash and ledger['settings']==original['settings']
    index=args.attempt-1;assert 0<=index<=len(ledger['attempts'])
    if index==len(ledger['attempts']):
        assert original_textures+len(ledger['attempts'])<cap,'No texture budget remains; counters were not reset'
        if ledger['attempts']:
            previous=ledger['attempts'][-1]
            assert previous_attempt_allows_retry(previous),'Require a reviewed rejection or confirmed cancellation before another bounded texture attempt'
        held_gate=gpu_window()
        histories=list((OUT/'assets').glob('*/stages/*/trellis_history.json'))+list((OUT/'manual').glob('*/texture*/trellis_history.json'))
        latest=max((p.stat().st_mtime for p in histories),default=0)
        while time.time()<latest+60:
            notify('Waiting for preserved 60-second generation cooldown');time.sleep(min(2,latest+60-time.time()))
        assert gpu_window()==held_gate
        uploaded=client.upload(image,args.id+'_'+args.revision+'_manual_t'+str(args.attempt))
        local_index=len(parent['textures'])+sum(a['parent_mesh']==args.parent_mesh for a in ledger['attempts'])
        seed=(parent['seed']+local_index)&0x7fffffff
        folder=OUT/'manual'/(args.id+'-'+args.revision)/('texture'+str(args.attempt));folder.mkdir(parents=True,exist_ok=True)
        record={'number':args.attempt,'parent_mesh':args.parent_mesh,'seed':seed,'source':str(source),'shape_hash':shape_hash,'folder':str(folder),'state':'reserved','held_review_result':held_gate,'held_review_stage':args.held_stage}
        ledger['attempts'].append(record);engine.atomic_json(ledger_path,ledger)
        graph=texture_prompt(engine.read_json(APP/'workflow_api.json',{}),opts,uploaded,source,'trellis_automation/manual/'+args.id+'/'+args.revision+'/texture'+str(args.attempt)+'/asset',seed)
        graph['194']['inputs']['remove_background']=True
        engine.atomic_json(folder/'submitted_prompt.json',graph)
        assert gpu_window()==held_gate
        record['prompt_id']=client.submit(graph,args.id+'-'+args.revision+'-manual-texture'+str(args.attempt));record['state']='submitted';engine.atomic_json(ledger_path,ledger)
    else:
        record=ledger['attempts'][index];folder=Path(record['folder'])
        assert record['shape_hash']==shape_hash and record['source']==str(source)
    if record['state']=='reserved':raise RuntimeError('Submission state is ambiguous; do not blindly resubmit')
    if record['state']=='submitted':
        history=wait_with_history_recheck(client,client.wait,record['prompt_id'],opts['timeout_minutes']*60,notify)
        shutil.copy2(client.output_path(history),folder/'model.glb');engine.atomic_json(folder/'trellis_history.json',history)
        record['state']='generated';record['finished']=time.time();engine.atomic_json(ledger_path,ledger)
    assert record['state']=='generated' and engine.valid_glb(folder/'model.glb')
    notify('Texture generated; preparing actual views')
    if not record.get('renders_ready') or not all((folder/'quality'/(n+'.png')).exists() for n in ('hero','top','front','opposite','neutral')):
        command=[opts['blender'],'--background','--factory-startup','--python-exit-code','2','--python',str(ROOT/'tools/run-limited-generation.py'),'--','--script',str(ROOT/'tools/reststop-production-review-render.py'),'--record',str(folder/'render-resource.json'),'--',str(folder)]
        with (folder/'render.log').open('wb') as log:result=subprocess.run(command,stdout=log,stderr=subprocess.STDOUT,creationflags=getattr(subprocess,'CREATE_NO_WINDOW',0))
        assert result.returncode==0,'Manual texture render failed'
    record['renders_ready']=True;engine.atomic_json(ledger_path,ledger)
    notify('Awaiting main-agent visual review of manual texture')
    print(json.dumps({'asset':args.id,'folder':str(folder),'original_mesh_attempts':len(original['meshes']),'original_texture_attempts':original_textures,'manual_texture_attempts':len(ledger['attempts']),'combined_texture_limit':cap,'seed':record['seed']}),flush=True)
