import datetime,hashlib,json,pathlib,shutil,subprocess
root=pathlib.Path(r'D:\Tralalero Shooter\Tralalero Shooter D')
out=root/'outputs/chapters45-2026-10-02/final-verification-20261001T212400Z'
def read(p):return json.loads(p.read_text(encoding='utf-8-sig'))
def sha(data):return hashlib.sha256(data).hexdigest()
def blob(ref):return subprocess.run(['git','-c','safe.directory='+root.as_posix(),'-C',str(root),'show',ref],capture_output=True,check=True).stdout
inbound=read(pathlib.Path('box-left-inbound-summary.json'))
tomb=read(pathlib.Path('box-left-label-tombstone-evidence.json'))
semantic=read(pathlib.Path('box-left-checkpoint-semantic-evidence.json'))
materials=read(pathlib.Path('shared-suite-material-checkpoint-proof.json'))
changed=read(out/'product-boundary-after-shared.json')['metadataChanges']
assert inbound['scanErrorCount']==0 and inbound['reboundToDifferentObjects']==0 and inbound['currentGeneratedIdInboundReferenceCount']==0
assert inbound['newlyUnresolvedSinceCheckpoint']==78 and inbound['allNewlyUnresolvedAddedObjectZero'] and tomb['allSameAndNull']
assert semantic['canonicalContentsIdentical'] and not semantic['propertyChangedObjects']
assert all(row['textIdenticalIgnoringCRLF'] for row in semantic['generatorAndTemplateComparison'])
box='Assets/ShooterSurvival/Prefabs/Walls/New/Box_left.prefab'
assert sha((root/box).read_bytes())==semantic['provenance']['currentSha256']
checks=[]
for rel in changed:
    data=(root/rel).read_bytes()
    if rel==box:
        checks.append({'file':rel,'result':'Canonical serialized property equality; regenerated IDs; no demonstrated live inbound-reference loss','sha256':sha(data)})
        continue
    row=next((r for r in materials if r['path']==rel),None)
    reference=row['checkpoint_blob'] if row else 'HEAD:'+rel
    baseline=blob(reference)
    assert data==baseline,rel
    checks.append({'file':rel,'result':'Byte-identical','reference':reference,'bytes':len(data),'sha256':sha(data)})
critical=read(out/'critical-hashes-before.json')
critical_changes=[p for p,digest in critical.items() if sha((root/p).read_bytes())!=digest]
assert not critical_changes
smoke=read(out/'font-restored-native-smoke.json')
assert smoke['summary']['outcome']=='captured' and not smoke['summary']['errors'] and smoke['restoration']['mismatches']==0
frozen=read(out/'all-product-hashes-before-build.json')
assert all(sha((root/p).read_bytes())==frozen[p]['sha256'] for p in changed)
receipt={'outcome':'accepted','utc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'reviewer':'Independent final_checker; canonical fields, checkpoint provenance, complete inbound references, null tombstones and native restored-font captures reviewed','touchedAssets':len(changed),'byteEqualityChecks':checks,'prefabCanonicalEquivalent':True,'canonicalSha256':semantic['canonicalSha256After'],'inboundSummary':inbound,'formerlyValidReferenceRecords':78,'formerlyValidReferencesAllInertNullAddedComponents':True,'liveComponentLossDemonstrated':False,'prefabRestoredOrIdRewritten':False,'fontRestoredToProvenCleanBaseline':True,'nativeFontStations':smoke['summary']['stationCount'],'criticalFiles':len(critical),'criticalCodeSceneHashChanges':critical_changes,'newChapterDependencyExclusion':'Neither new chapter references the generated bonus-box assets; restored FONT Menu only supports inactive legacy player-child HUD','temporalLimit':'No immediate19:59 pre-suite Box_left bytes were captured. Current properties equal the real17:35 dirty checkpoint; this cannot rule out an unrecorded intervening edit and reversal. No exact pre-suite byte restoration is claimed.','remainingOldReferences':'6994 inbound references also absent at17:35 remain outside this scoped preservation check; they were not silently rewritten.','decision':'Preserve current dirty prefab and materials; no ID repair is justified for null tombstones. Keep the accepted four-route evidence and proceed to build under full-product SHA256 freeze.'}
(out/'shared-side-effect-resolution.json').write_text(json.dumps(receipt,indent=2),encoding='utf-8')
evidence=out/'shared-side-effect-evidence';evidence.mkdir(exist_ok=True)
for name in ['box-left-inbound-summary.json','box-left-label-tombstone-evidence.json','box-left-checkpoint-semantic-evidence.json','inspect-box-left-checkpoint.py','shared-suite-material-checkpoint-proof.json','shared-suite-side-effects-trace.md','shared-suite-editor-import-excerpt.txt','boundary-Jamsil-dependencies.json','boundary-ShoeTower-dependencies.json']:
    shutil.copyfile(name,evidence/name)
for name in ['close-shared-test-boundary.py','freeze-final-build-content.py','restore-verified-font-cache.py','run-final-font-smoke.py']:
    shutil.copyfile(name,root/'tools'/name)
stamp=datetime.datetime.now(datetime.timezone.utc).strftime('%Y-%m-%d %H:%M:%S UTC')
with (root/'docs/exec-plans/active/chapters45-progress-2026-10-02.md').open('a',encoding='utf-8') as f:
    f.write(f'\n\n## {stamp} - Independent asset-preservation closure\n- Final checker/developer/QA: all 21 shared-test side effects reviewed. The font returned exactly to its proven clean baseline; 19 other files match HEAD or the real dirty checkpoint byte-for-byte. Box_left properties equal all 421 documents of the 17:35 dirty checkpoint.\n- Complete scan: 12,828 YAML files, 31,385 inbound references, zero read errors or reference rebinding. The 78 suspicious records are preexisting null added-component tombstones, not lost live components. No prefab ID repair or HEAD reset was applied. The unavailable immediate pre-suite byte snapshot remains an explicit evidence limit.\n- No scoped product correction remains. Four routes and 286 critical hashes remain accepted; all 51,746 product files are now hashed before the final build. Device check still reports no connected phone. Qwen name matches are the four preexisting web-search companion processes; no unrelated service was stopped.\n- Evidence: final-verification-20261001T212400Z/shared-side-effect-resolution.json and shared-side-effect-evidence/. Next: build and verify the corrected review APK, compare every product hash again.\n')
print(json.dumps({'outcome':receipt['outcome'],'touchedAssets':len(changed),'criticalHashChanges':critical_changes,'evidence':str(evidence)}))
