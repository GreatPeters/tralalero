"""Final decoded-frame integrity and self-contained offline review package."""
import json,hashlib,shutil,zipfile,sys
from pathlib import Path
ROOT=Path.cwd();OUT=ROOT/'outputs/reststop-blender-v2-2026-09-23';G=ROOT/'tmp/image-previews/reststop-blender-v2-2026-09-23';DOC=ROOT/'map-concepts/reststop-blender-v2-2026-09-23'
v=json.loads((OUT/'video-manifest.json').read_text(encoding='utf8'));native=json.loads((OUT/'validation.json').read_text());fresh=json.loads((OUT/'fresh-glb-validation.json').read_text());review=json.loads((DOC/'iteration_review.json').read_text(encoding='utf8'))
assert len(v['clips'])==10 and v['full']['frames']==7200 and v['full']['duration_seconds']==300
assert review['decision']=='retain_repair';assert fresh['fresh_import'] and all(fresh['required_zones'].values());assert native['defense_max_yaw_degrees_s']<=90.001
def frames(file):return [s.rsplit(',',1)[1].strip() for s in file.read_text().splitlines() if s and not s.startswith('#')]
clips=[]
for r in v['clips']:
 p=OUT/'videos'/r['file'];assert hashlib.sha256(p.read_bytes()).hexdigest()==r['sha256'];clips+=frames(p.with_suffix('.framemd5'))
full=frames((OUT/'videos'/v['full']['file']).with_suffix('.framemd5'));assert clips==full and len(full)==7200
shutil.copy2(DOC/'README.md',G/'README.md')
checks={'ordered_clips_equal_full_decoded_frames':True,'total_frames':len(full),'total_seconds':300,'native_validation':native,'fresh_glb_validation':fresh,'source_files':{p.name:hashlib.sha256(p.read_bytes()).hexdigest() for p in sorted((ROOT/'tools/reststop-previs-v2').glob('*.py'))},'master_sha256':hashlib.sha256((OUT/'reststop-v2.blend').read_bytes()).hexdigest()}
(OUT/'delivery-validation.json').write_text(json.dumps(checks,ensure_ascii=False,indent=2),encoding='utf8');shutil.copy2(OUT/'delivery-validation.json',G/'delivery-validation.json')
archive=OUT/'reststop-v2-review.zip'
with zipfile.ZipFile(archive,'x',compression=zipfile.ZIP_DEFLATED,compresslevel=1) as z:
 for p in sorted(G.iterdir()):
  if p.is_file() and p.suffix in ('.html','.mp4','.png','.jpg','.blend','.glb','.md','.json'):z.write(p,p.name)
 for p in sorted((ROOT/'tools/reststop-previs-v2').glob('*.py')):z.write(p,'source/'+p.name)
 for p in [DOC/'iteration_review.json',OUT/'video-manifest.json',OUT/'comparison/manifest.json']:z.write(p,'evidence/'+p.name)
with zipfile.ZipFile(archive) as z:assert z.testzip() is None
shutil.copy2(archive,G/archive.name)
(OUT/'package.json').write_text(json.dumps({'file':archive.name,'bytes':archive.stat().st_size,'sha256':hashlib.sha256(archive.read_bytes()).hexdigest(),'archive_test':True},indent=2));print('PACKAGE_COMPLETE',archive,archive.stat().st_size,flush=True)
