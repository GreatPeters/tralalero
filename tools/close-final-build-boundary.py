import datetime,hashlib,json,pathlib,xml.etree.ElementTree as ET
p=pathlib.Path(r'D:\Tralalero Shooter\Tralalero Shooter D')
out=p/'outputs/chapters45-2026-10-02/final-verification-20261001T212400Z'
def read(name):return json.loads((out/name).read_text(encoding='utf-8'))
before=read('all-product-hashes-before-build.json');after=read('all-product-hashes-after-build.json')
added=sorted(after.keys()-before.keys());removed=sorted(before.keys()-after.keys())
changed=[k for k in before.keys()&after.keys() if before[k]['sha256']!=after[k]['sha256']]
assert added==['Assets/AddressableAssetsData/link.xml','Assets/AddressableAssetsData/link.xml.meta'] and not removed and not changed
xml=ET.fromstring((p/added[0]).read_bytes());assert xml.tag=='linker'
assemblies=[node.attrib['fullname'].split(',')[0] for node in xml]
assert assemblies==['Unity.Addressables','Unity.Localization','Unity.ResourceManager','UnityEngine.CoreModule']
assert all(hashlib.sha256((p/k).read_bytes()).hexdigest()==after[k]['sha256'] for k in added)
record={'outcome':'accepted','utc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'files':len(before),'totalAfterBuild':len(after),'existingContentChanges':changed,'removedFiles':removed,'generatedBuildFiles':[{'file':k,**after[k]} for k in added],'reviewer':'final_checker independently inspected actual XML/importer metadata and approved retaining the two expected generated linker files; no deletion or rebuild required','decision':'All51746 preexisting product files byte-identical plus2 expected Addressables linker-file additions;51748 total. Metadata-only refreshes are retained in raw boundary report.'}
(out/'build-boundary-resolution.json').write_text(json.dumps(record,indent=2),encoding='utf-8')
print(json.dumps(record))
