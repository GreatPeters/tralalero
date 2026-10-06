import datetime, hashlib, json, pathlib, subprocess
root=pathlib.Path(r'D:\Tralalero Shooter\Tralalero Shooter D')
out=root/'outputs/chapters45-2026-10-02/final-verification-20261001T212400Z'
backup=out/'shared-side-effects-preserved'
backup.mkdir(exist_ok=False)
record=json.loads((out/'product-boundary-after-shared.json').read_text(encoding='utf-8'))
hashes={}
for rel in record['metadataChanges']:
    data=(root/rel).read_bytes(); dest=backup/rel
    dest.parent.mkdir(parents=True,exist_ok=True);dest.write_bytes(data)
    hashes[rel]={'bytes':len(data),'sha256':hashlib.sha256(data).hexdigest()}
(backup/'manifest.json').write_text(json.dumps(hashes,indent=2),encoding='utf-8')
rel='Assets/ShooterSurvival/Fonts/FONT Menu.asset'
assert hashes[rel]['sha256']=='4c0a30bb6fe2ccab4fd329e4bb82620e17fbfe143e3d2fc803543707e8282a5a'
prior=root/'outputs/chapters45-2026-10-02/git-status-after.txt'
assert prior.is_file() and rel not in prior.read_text(encoding='utf-8-sig')
before=json.loads((out/'product-file-metadata-before.json').read_text(encoding='utf-8'))
baseline=subprocess.run(['git','-c','safe.directory='+root.as_posix(),'-C',str(root),'show','HEAD:'+rel],capture_output=True,check=True).stdout
assert len(baseline)==before[rel][0]==266722
(backup/'font-clean-baseline.asset').write_bytes(baseline)
(root/rel).write_bytes(baseline)
receipt={'utc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'action':'Exact restoration of proven clean baseline font after preserving test-generated cache','file':rel,'before':hashes[rel],'restoredBytes':len(baseline),'restoredSHA256':hashlib.sha256(baseline).hexdigest(),'proof':'Clean pre-task and20:51 status;266722-byte pre-suite metadata; reviewer verified diff contains only transient dynamic atlas glyph/packing/cache data','otherTouchedAssetsRestored':False}
(out/'font-cache-restoration.json').write_text(json.dumps(receipt,indent=2),encoding='utf-8')
print(json.dumps(receipt))
