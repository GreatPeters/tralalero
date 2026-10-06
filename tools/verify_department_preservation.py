import hashlib,json,pathlib,sys
ROOT=pathlib.Path(r'D:\Tralalero Shooter\Tralalero Shooter D')
OUT=ROOT/'outputs/department-store-2026-10-02'
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
manifest=json.loads((OUT/'recovery-manifest.json').read_text(encoding='utf-8'))
before=json.loads((OUT/'product-metadata-before.json').read_text(encoding='utf-8'))
after={p.relative_to(ROOT).as_posix():[p.stat().st_size,p.stat().st_mtime_ns] for base in ['Assets','Packages','ProjectSettings'] for p in (ROOT/base).rglob('*') if p.is_file()}
intentional=['Assets/ShooterSurvival/Scenes/Tools/Jamsil.unity','Assets/ShooterSurvival/Scenes/Tools/ShoeTower.unity']
result={'preservedRecoveryCopies':len(manifest),'invalidRecoveryCopies':[r['path'] for r in manifest if sha(pathlib.Path(r['recoveryPath']))!=r['sha256']],
 'sharedAssetHashMismatches':[r['path'] for r in manifest if r['path'] not in intentional and sha(ROOT/r['path'])!=r['sha256']],
 'intentionalSceneHashes':{p:sha(ROOT/p) for p in intentional},
 'changedPreexistingProductMetadata':[p for p in before if p in after and before[p]!=after[p]],
 'missingPreexistingProducts':[p for p in before if p not in after],
 'newProducts':[p for p in after if p not in before],
 'limits':'All 208 known shared recovery assets hash checked. All product paths/size/mtime checked; metadata is not a hash of every preexisting file.'}
dest=OUT/(sys.argv[1] if len(sys.argv)>1 else 'preservation-final.json');dest.write_text(json.dumps(result,indent=2),encoding='utf-8')
print(json.dumps({k:(len(v) if k=='newProducts' else v) for k,v in result.items()},indent=2))
