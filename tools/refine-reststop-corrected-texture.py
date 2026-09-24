"""Run/resume bounded Blender passes for a manual texture or reviewed correction.

Invoke through run-limited-generation.py so child Blender processes inherit limits.
"""
import argparse,json,re,shutil,subprocess,sys,time
from pathlib import Path

ROOT=Path(__file__).resolve().parents[1];OUT=ROOT/'outputs/reststop-production-2026-09-24'
APP=next(p for p in (Path.home()/'Desktop').glob('AI */Trellis */*') if (p/'engine.py').is_file())
sys.path.insert(0,str(APP));import engine
parser=argparse.ArgumentParser();parser.add_argument('--id',required=True)
mode=parser.add_mutually_exclusive_group(required=True)
mode.add_argument('--texture-attempt',type=int)
mode.add_argument('--reviewed-source',type=Path,help='Folder of a visually approved local correction; no new AI texture attempt')
parser.add_argument('--low-index',type=int,required=True,choices=(1,2))
parser.add_argument('--fixed-inserts',choices=('S08',),help='Preserve verified analytic fridge inserts and allocate only the remaining budget to its body')
args=parser.parse_args()
assert not args.fixed_inserts or (args.id==args.fixed_inserts and args.reviewed_source)
assert re.fullmatch(r'[A-Z]\d{2}',args.id)
key=engine.sha_file(OUT/'inputs'/(args.id+'.png'))
lp=OUT/('manual-refinement-ledgers' if args.reviewed_source else 'manual-texture-ledgers')/(key+'.json')
lp.parent.mkdir(exist_ok=True)
opts=engine.read_json(ROOT/'map-concepts/reststop-production-2026-09-24/effective-settings.json',{})
def notify(message):engine.atomic_json(OUT/'manual-generation-status.json',{'asset':args.id,'message':message,'updated':time.time()})
with engine.OutputLock(lp.with_suffix('.lock')):
    if args.reviewed_source:
        folder=args.reviewed_source.resolve()
        assert folder.is_relative_to(OUT/'manual')
        original=engine.read_json(OUT/'quality-ledgers'/(key+'.json'),{})
        assert original.get('terminal') in ('review_needed','halted')
        original_count=sum(len(t['lows']) for m in original['meshes'] for t in m['textures'])
        original_count+=len(engine.read_json(OUT/'reduction-recovery'/(key+'.json'),{}).get('runs',[]))
        source_hash=engine.sha_file(folder/'model.glb')
        ledger=engine.read_json(lp,{'settings':original['settings'],'source_folder':str(folder),'source_sha256':source_hash,'original_low_count':original_count,'lows':[]})
        assert ledger['source_folder']==str(folder) and ledger['source_sha256']==source_hash
        assert ledger['original_low_count']==original_count and original_count+args.low_index<=2
        attempt=ledger
    else:
        ledger=engine.read_json(lp,{})
        assert 1<=args.texture_attempt<=len(ledger['attempts'])
        attempt=ledger['attempts'][args.texture_attempt-1];folder=Path(attempt['folder'])
        assert attempt['state']=='generated'
        original_count=0
    assert engine.read_json(folder/'visual-review.json',{}).get('verdict')=='pass'
    assert ledger['settings']=={k:opts[k] for k in ledger['settings']}
    lows=attempt.setdefault('lows',[]);i=args.low_index-1;assert i<=len(lows)
    cached=i<len(lows)
    if cached:
        low=lows[i];dest=Path(low['folder'])
        assert low['state']=='generated' and engine.output_valid(dest,opts['target_triangles']),'Audit an interrupted pass instead of blindly rerunning it'
    else:
        if lows:
            assert engine.read_json(Path(lows[-1]['folder'])/'visual-review.json',{}).get('verdict') in ('mesh','texture'),'Review the earlier pass before another reduction'
        dest=folder/('low'+str(original_count+args.low_index));assert not dest.exists(),'Preserve existing candidates'
        dest.mkdir();profile='parts' if original_count+i==0 else 'preserve_details'
        low={'folder':str(dest),'profile':profile,'state':'started'};lows.append(low);engine.atomic_json(lp,ledger)
        shutil.copy2(folder/'model.glb',dest/'trellis_source.glb')
        if (folder/'repair.json').exists():shutil.copy2(folder/'repair.json',dest/'source-repair.json')
        if args.fixed_inserts:
            low['fixed_inserts']=args.fixed_inserts;engine.atomic_json(lp,ledger)
            notify('Blender · 냉장고 본체 감량 및 내부 부품 보존')
            command=[opts['blender'],'--background','--factory-startup','--python-exit-code','2','--python',str(ROOT/'tools/run-limited-generation.py'),'--',
                '--script',str(ROOT/'tools/refine-reststop-fridge-mesh.py'),'--record',str(dest/'refine-resource.json'),'--',
                '--source',str(folder/'model.glb'),'--output',str(dest),'--target',str(opts['target_triangles']),
                '--texture-size',str(opts['texture_size']),'--profile',profile]
            with (dest/'blender.log').open('wb') as log:
                result=subprocess.run(command,stdout=log,stderr=subprocess.STDOUT,timeout=opts['timeout_minutes']*60,creationflags=getattr(subprocess,'CREATE_NO_WINDOW',0))
            assert result.returncode==0,'Fixed-insert refinement failed; preserve and audit this consumed pass'
            request=dest/'verify-request.json';engine.atomic_json(request,{'output':str(dest),'target_triangles':opts['target_triangles']})
            command=[opts['blender'],'--background','--factory-startup','--python-exit-code','2','--python',str(APP/'blender_refine.py'),'--','--verify',str(request)]
            with (dest/'verify.log').open('wb') as log:
                result=subprocess.run(command,stdout=log,stderr=subprocess.STDOUT,timeout=300,creationflags=getattr(subprocess,'CREATE_NO_WINDOW',0))
            assert result.returncode==0,'Fresh fixed-insert export checks failed'
        else:
            engine.run_blender(folder/'model.glb',dest,{**opts,'refine_profile':profile},notify)
        assert engine.output_valid(dest,opts['target_triangles']);low['state']='generated';engine.atomic_json(lp,ledger)
    if not all((dest/'quality'/(n+'.png')).is_file() for n in ('hero','top','opposite','front','neutral')):
        command=[opts['blender'],'--background','--factory-startup','--python-exit-code','2','--python',str(ROOT/'tools/run-limited-generation.py'),'--','--script',str(ROOT/'tools/reststop-production-review-render.py'),'--record',str(dest/'review-resource.json'),'--',str(dest)]
        with (dest/'review-render.log').open('wb') as log:result=subprocess.run(command,stdout=log,stderr=subprocess.STDOUT,creationflags=getattr(subprocess,'CREATE_NO_WINDOW',0))
        assert result.returncode==0
    print(json.dumps({'folder':str(dest),'cached':cached,'triangles':engine.read_json(dest/'validation.json',{})['triangles'],'visual_review_required':engine.read_json(dest/'visual-review.json',{}).get('verdict')!='pass'}),flush=True)
