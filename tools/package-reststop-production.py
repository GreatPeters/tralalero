"""Package only a completely accepted 90-asset / 8-rig production revision."""
import argparse
import csv
import hashlib
import json
from pathlib import Path
import shutil
import zipfile

ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'outputs/reststop-production-2026-09-24'
DOC=ROOT/'map-concepts/reststop-production-2026-09-24'
parser=argparse.ArgumentParser();parser.add_argument('--revision',type=int,default=1)
parser.add_argument('--check-only',action='store_true');args=parser.parse_args()
def read(path):return json.loads(path.read_text(encoding='utf8'))
catalog=read(DOC/'catalog.json');assert len(catalog)==90
original_jobs={Path(row['name']).stem:row for row in read(OUT/'assets/.trellis-automation/state.json')['jobs'].values()}
jobs=dict(original_jobs)
jobs.update(read(OUT/'final-overrides.json'))
rigs=read(OUT/'accepted-rigs.json')
missing=[row['id'] for row in catalog if jobs.get(row['id'],{}).get('status')!='done']
missing_rigs=[row['id'] for row in catalog if row['kind']=='human' and row['id'] not in rigs]
if missing or missing_rigs:
    print(json.dumps({'ready':False,'missing_models':missing,'missing_rigs':missing_rigs},indent=2))
    raise SystemExit(2)
for row in catalog:
    key=row['id'];folder=Path(jobs[key]['folder'])
    validation=read(folder/'validation.json');assert validation['ok'],key
    assert 0 < validation['triangles'] <= 15000,key
    assert validation['triangles']==jobs[key].get('triangles'),key
    if jobs[key].get('manual_repair'):
        assert read(folder/'visual-review.json')['verdict']=='pass',key
    else:
        assert read(folder/'quality_summary.json')['status']=='passed',key
    for name in ('model.blend','model.glb','model.fbx','preview.png'):
        assert (folder/name).is_file(),(key,name)
    if row['kind']=='human':
        rigfolder=Path(rigs[key]['folder'])
        assert read(rigfolder/'fresh-rig-validation.json')['ok'],key
        assert read(rigfolder/'visual-review.json')['verdict']=='pass',key
        assert read(rigfolder/'video-validation.json')['ok'],key
        assert read(rigfolder/'export-pose-comparison.json')['ok'],key
        assert (rigfolder/'animation-preview.mp4').is_file(),key
if args.check_only:
    print(json.dumps({'ready':True,'models':90,'rigs':8}));raise SystemExit(0)
assert args.revision>0
delivery=OUT/f'delivery-r{args.revision}';archive=OUT/f'reststop-assets-r{args.revision}.zip'
assert not delivery.exists() and not archive.exists(),'Preserve prior packages; choose a new revision'
delivery.mkdir()
manifest=[]
for row in catalog:
    key=row['id'];job=jobs[key];folder=Path(job['folder'])
    dest=delivery/('characters' if row['kind']=='human' else 'props')/key;dest.mkdir(parents=True)
    for suffix in ('blend','glb','fbx'):shutil.copy2(folder/('model.'+suffix),dest/(key+'.'+suffix))
    original=folder/'trellis_source.glb'
    if not original.exists():original=Path(original_jobs[key]['folder'])/'trellis_source.glb'
    if original.exists():shutil.copy2(original,dest/'high-detail-source.glb')
    if (folder/'original-trellis-source.glb').exists():shutil.copy2(folder/'original-trellis-source.glb',dest/'original-trellis-source.glb')
    for name in ('original-textured-body.glb','recovered-shape-before-materials.glb','original-shape.glb','reconstructed-low-before-bake.glb'):
        if (folder/name).exists():shutil.copy2(folder/name,dest/name)
    shutil.copy2(folder/'preview.png',dest/'preview.png')
    shutil.copy2(OUT/'inputs'/(key+'.png'),dest/'reference.png')
    if (folder/'textures').is_dir():shutil.copytree(folder/'textures',dest/'textures')
    if (folder/'README.md').exists():shutil.copy2(folder/'README.md',dest/'README.md')
    if (folder/'MATERIALS.md').exists():shutil.copy2(folder/'MATERIALS.md',dest/'MATERIALS.md')
    evidence=dest/'validation';evidence.mkdir()
    shutil.copy2(folder/'validation.json',evidence/'static-import.json')
    for name in ('quality_summary.json','visual-review.json','repair.json','source-repair.json','preview-selection.json','opening-validation.json','opening-depth-glb.json','opening-depth-fbx.json','opening-reference-depth.json','texture-extraction.json','transparency-validation.json','basecolor-rgb-validation.json','opacity-preservation.json','volume-reconstruction.json','completed-prompt-recovery.json','completed-prompt-history.json','manual-shape-recovery.json','manual-shape-ledger.json','interruption-receipt.json','glass-separation.json','visibility-validation.json','assembly-repair.json','body-topology-repair.json','body-texture-history.json','body-texture-request.json','cancelled-texture-attempt1.json','curve-fit.json','partition.json','fridge-layout.json','geometry-normalization-proof.json','cleanup.json'):
        if (folder/name).exists():shutil.copy2(folder/name,evidence/name)
    for name in ('main-agent-review.json','geometry-profile-proof.png','bottom.png','bottom-flat.png','bottom-neutral.png','opacity-proof-glb.png','opacity-proof-fbx.png'):
        if (folder/'quality'/name).exists():shutil.copy2(folder/'quality'/name,evidence/name)
    for contact in (folder/'quality').glob('review-contact*.png'):
        shutil.copy2(contact,evidence/contact.name)
    if (folder/'quality-fbx').is_dir():shutil.copytree(folder/'quality-fbx',evidence/'quality-fbx')
    for name in ('axle-validation.json','front-surface-validation.json','front-depth.json','front-depth-glb.json','front-depth-fbx.json','surface-distance-validation.json','topology-reconstruction.json','geometry-validation.json'):
        if (folder/name).exists():shutil.copy2(folder/name,evidence/name)
    item={'id':key,'title':row['title'],'kind':row['kind'],'category':row.get('category',''),
          'zones':row.get('zones',[]),'triangles':job.get('triangles'),
          'folder':dest.relative_to(delivery).as_posix(),'source':str(folder),'manual_repair':job.get('manual_repair',False)}
    if row['kind']=='human':
        rig=rigs[key];rigfolder=Path(rig['folder']);role=rig['role'];target=dest/'rigged';target.mkdir()
        for suffix in ('blend','glb','fbx'):shutil.copy2(rigfolder/(role+'.'+suffix),target/(role+'.'+suffix))
        for name in ('animation-preview.mp4','animation-preview.json','rig-report.json','fresh-rig-validation.json','visual-review.json','video-validation.json','export-pose-comparison.json'):
            shutil.copy2(rigfolder/name,target/name)
        item['rig_source']=str(rigfolder);item['role']=role;item['bones']=18;item['clips']=9
    manifest.append(item)
(delivery/'manifest.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2),encoding='utf8')
with (delivery/'asset-list.csv').open('w',encoding='utf-8-sig',newline='') as f:
    writer=csv.DictWriter(f,fieldnames=['id','title','kind','category','zones','triangles','folder','role','bones','clips'],extrasaction='ignore')
    writer.writeheader();writer.writerows({**item,'zones':','.join(map(str,item['zones']))} for item in manifest)
shutil.copy2(DOC/'effective-settings.json',delivery/'generation-settings.json')
shutil.copy2(DOC/'vehicle-game-scale-reference.json',delivery/'vehicle-game-scale-reference.json')
(delivery/'README.md').write_text('''# 휴게소 소품·인물

소품·구조·차량 82종과 인물 8종입니다. 각 폴더의 번호는 `asset-list.csv` 및 기존 이미지 번호와 대응합니다.

- `props/`: 정적 소품의 Blender, GLB, FBX, 2K 재질, 기준 이미지와 검증 기록.
- `characters/`: 정적 몸체와 `rigged/`의 본·동작 포함 모델, 실제 동작 미리보기.
- 각 인물은 18본, 9개 동작 클립을 포함합니다. 이동 클립은 제자리 동작이며 게임의 경로 이동과 조합할 수 있습니다.
- `manifest.json`: 원본 선택 경로와 직접 보정 여부. 작은 잔여 결함은 각 시각 검토 기록에 남겼습니다.
- `high-detail-source.glb`: 감량에 사용한 고밀도 소스이며 직접 보정이 포함될 수 있습니다. 별도의 `original-trellis-source.glb`가 있으면 수정 전 AI 결과입니다.
- 자동 감량 `.blend`에는 `Detailed_Source`라는 숨긴 고밀도 원본이 함께 있을 수 있습니다. 게임용 최종본은 동봉된 FBX·GLB 또는 `Asset_Optimized` 메시를 사용하세요.
- `MATERIALS.md`가 있는 모델은 그 재질 구성과 투명도 설정을 유지하세요. 투명 부품의 알파 및 일부 평면의 노멀 연결을 직접 보정한 경우가 있습니다.

생성 설정은 형태1536, 텍스처2048, 목표15000삼각형, 중간300000면, 기본시드12345, 형태12/텍스처25스텝입니다. 재시도에는 기존 자동화의 파생 시드 정책을 적용했습니다. 상세 설정은 `generation-settings.json`을 확인하세요.

Blender에서 새 FBX·GLB 임포트와 실제 동작을 확인한 독립 에셋 묶음입니다. 기존 Unity 씬과 Blender v4는 이 묶음 제작 과정에서 변경하지 않았습니다. 차량 크기와 구역 배치는 기존 v4를 기준으로 후속 조립할 수 있습니다.
''',encoding='utf8')
files=sorted(p for p in delivery.rglob('*') if p.is_file())
checksums={}
for path in files:
    with path.open('rb') as stream:
        checksums[path.relative_to(delivery).as_posix()]=hashlib.file_digest(stream,'sha256').hexdigest()
(delivery/'sha256.json').write_text(json.dumps(checksums,ensure_ascii=False,indent=2),encoding='utf8')
with zipfile.ZipFile(archive,'x',compression=zipfile.ZIP_DEFLATED,compresslevel=3) as package:
    for path in sorted(p for p in delivery.rglob('*') if p.is_file()):package.write(path,path.relative_to(delivery))
with zipfile.ZipFile(archive) as package:assert package.testzip() is None
receipt={'complete':True,'models':90,'rigs':8,'delivery':str(delivery),'archive':str(archive),'archive_bytes':archive.stat().st_size,'files':len(files)+1}
(OUT/f'delivery-r{args.revision}-receipt.json').write_text(json.dumps(receipt,indent=2),encoding='utf8')
print(json.dumps(receipt),flush=True)
