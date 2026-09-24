"""Create an offline review bundle after final visual review."""
from pathlib import Path
import json, zipfile, shutil, hashlib, sys
ROOT=Path.cwd();OUT=ROOT/'outputs/reststop-blender-300s-2026-09-23';G=ROOT/'tmp/image-previews/reststop-blender-300s-2026-09-23'
manifest=json.loads((OUT/'video-manifest.json').read_text(encoding='utf8'))
assert len(manifest['clips'])==10 and manifest['full']['frames']==7200 and manifest['full']['duration_seconds']==300
assert json.loads((G/'gallery-status.json').read_text())['full_video']
for name in ('people-native-blender.png',):
    if (OUT/name).exists():shutil.copy2(OUT/name,G/name)
files=[p for p in G.iterdir() if p.is_file() and p.suffix in ('.mp4','.png','.jpg','.blend','.glb','.html')]
target=OUT/'reststop-review-pack.zip'
with zipfile.ZipFile(target,'x',compression=zipfile.ZIP_DEFLATED,compresslevel=1) as z:
    for p in files:z.write(p,p.name)
    z.write(ROOT/'map-concepts/reststop-blender-300s-2026-09-23/README.md','README.md')
    z.write(OUT/'video-manifest.json','video-manifest.json')
    z.write(ROOT/'map-concepts/reststop-blender-300s-2026-09-23/concept-prompts.json','concept-prompts.json')
    for p in (ROOT/'tools/reststop-previs').glob('*.py'):z.write(p,'source/'+p.name)
with zipfile.ZipFile(target) as z:
    assert z.testzip() is None
    count=len(z.namelist())
receipt={'file':str(target),'bytes':target.stat().st_size,'members':count,'sha256':hashlib.sha256(target.read_bytes()).hexdigest()}
(OUT/'package-manifest.json').write_text(json.dumps(receipt,indent=2));print(json.dumps(receipt))
