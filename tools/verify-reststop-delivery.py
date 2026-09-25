"""Verify the complete local gallery and stream its delivered ZIP for SHA-256."""
import argparse
from concurrent.futures import ThreadPoolExecutor
import hashlib
from html.parser import HTMLParser
import json
from pathlib import Path
import time
from urllib.parse import unquote,urljoin,urlparse
from urllib.request import Request,urlopen
import zipfile

ROOT=Path(__file__).resolve().parents[1];OUT=ROOT/'outputs/reststop-production-2026-09-24'
GALLERY=ROOT/'tmp/image-previews/reststop-production-2026-09-24'
parser=argparse.ArgumentParser();parser.add_argument('--revision',type=int,required=True)
args=parser.parse_args();assert args.revision>0
base='http://127.0.0.1:8772/'
receipt=json.loads((OUT/f'delivery-r{args.revision}-receipt.json').read_text(encoding='utf8'))
assert receipt['complete'] and receipt['models']==90 and receipt['rigs']==8
output=OUT/f'delivery-r{args.revision}-verification.json';assert not output.exists()
archive=Path(receipt['archive']);delivery=Path(receipt['delivery'])
manifest=json.loads((delivery/'manifest.json').read_text(encoding='utf8'))
humans=[row for row in manifest if row['kind']=='human']
assert len(manifest)==90 and len(humans)==8 and sum(row['clips'] for row in humans)==72
assert all(0<row['triangles']<=15000 for row in manifest)
with zipfile.ZipFile(archive) as package:
    assert len(package.namelist())==receipt['files']
    assert json.loads(package.read('manifest.json'))==manifest
    json_members=0
    for name in package.namelist():
        if name.endswith('.json'):
            json.loads(package.read(name).decode('utf-8-sig'));json_members+=1
with urlopen(base,timeout=10) as response:
    assert response.status==200;page=response.read().decode('utf8')
assert '모델 완료 90/90' in page and 'TRELLIS 제작 완료' in page
with urlopen(urljoin(base,'progress.json'),timeout=10) as response:progress=json.load(response)
assert progress['counts']['generated_complete']==90 and progress['counts']['rigged_complete']==8

class Links(HTMLParser):
    def __init__(self):super().__init__();self.urls=set()
    def handle_starttag(self,tag,attrs):
        for name,value in attrs:
            if name in ('href','src','poster') and value:
                target=urljoin(base,value);parsed=urlparse(target)
                if parsed.netloc==urlparse(base).netloc and Path(parsed.path).suffix.lower() in ('.glb','.fbx','.blend','.png','.mp4','.zip'):
                    self.urls.add(target)
links=Links();links.feed(page)
def check(url):
    path=(GALLERY/unquote(urlparse(url).path).lstrip('/')).resolve()
    assert path.is_relative_to(GALLERY) and path.is_file(),url
    with urlopen(Request(url,method='HEAD'),timeout=20) as response:
        assert response.status==200 and int(response.headers['Content-Length'])==path.stat().st_size,url
    return url
with ThreadPoolExecutor(max_workers=4) as workers:checked=list(workers.map(check,sorted(links.urls)))
assert len([u for u in checked if u.endswith('/model.glb')])==90
archive_url=urljoin(base,archive.name);assert archive_url in checked
with archive.open('rb') as stream:local_digest=hashlib.file_digest(stream,'sha256').hexdigest()
download=hashlib.sha256();download_bytes=0
with urlopen(archive_url,timeout=30) as response:
    assert response.status==200 and int(response.headers['Content-Length'])==archive.stat().st_size
    while chunk:=response.read(8*1024*1024):download.update(chunk);download_bytes+=len(chunk)
assert download_bytes==archive.stat().st_size and download.hexdigest()==local_digest
result={'ok':True,'models':90,'rigs':8,'animation_clips':72,'package_files':receipt['files'],
    'json_members_parsed':json_members,'http_resources_checked':len(checked),'gallery':base,'archive_url':archive_url,
    'archive_bytes':download_bytes,'archive_sha256':local_digest,'download_sha256':download.hexdigest(),
    'archive_crc_verified_by_package_builder':True,'browser_ui_checked':False,
    'browser_limit':'No connected browser was available; HTTP resources and complete downloaded bytes were checked, not browser interaction.',
    'checked_at':time.time()}
output.write_text(json.dumps(result,indent=2),encoding='utf8');print(json.dumps(result),flush=True)
