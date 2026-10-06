import datetime,hashlib,json,pathlib,sys,time
root=pathlib.Path(r'D:\Tralalero Shooter\Tralalero Shooter D')
out=root/'outputs/chapters45-2026-10-02/final-verification-20261001T212400Z'
mode=sys.argv[1]
assert mode in ['before-build','after-build']
dest=out/('all-product-hashes-'+mode+'.json')
assert not dest.exists(),'Preserve previous snapshot'
manifest={};last=time.monotonic();total=0
for base in ['Assets','Packages','ProjectSettings']:
    for path in (root/base).rglob('*'):
        if not path.is_file():continue
        with path.open('rb') as f:digest=hashlib.file_digest(f,'sha256').hexdigest()
        st=path.stat();total+=st.st_size
        manifest[path.relative_to(root).as_posix()]={'bytes':st.st_size,'mtime_ns':st.st_mtime_ns,'sha256':digest}
        if time.monotonic()-last>25:
            print(json.dumps({'stage':mode,'files':len(manifest),'bytes':total}),flush=True);last=time.monotonic()
dest.write_text(json.dumps(manifest,indent=2),encoding='utf-8')
record={'stage':mode,'utc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'files':len(manifest),'bytes':total}
if mode=='after-build':
    before=json.loads((out/'all-product-hashes-before-build.json').read_text(encoding='utf-8'))
    record['contentChanges']=[k for k in sorted(before.keys()|manifest.keys()) if before.get(k,{}).get('sha256')!=manifest.get(k,{}).get('sha256')]
    record['metadataChanges']=[k for k in sorted(before.keys()|manifest.keys()) if before.get(k)!=manifest.get(k)]
    (out/'all-product-build-boundary.json').write_text(json.dumps(record,indent=2),encoding='utf-8')
print(json.dumps(record),flush=True)
if mode=='after-build' and record['contentChanges']:raise SystemExit(2)
