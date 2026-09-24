"""Publish the accepted eight-character subset while prop production continues."""
import json
from pathlib import Path
import shutil
import zipfile

ROOT=Path(__file__).resolve().parents[1];OUT=ROOT/'outputs/reststop-production-2026-09-24'
G=ROOT/'tmp/image-previews/reststop-production-2026-09-24'
rigs=json.loads((OUT/'accepted-rigs.json').read_text(encoding='utf8'))
assert set(rigs)=={f'H{i:02}' for i in range(1,9)}
archive=OUT/'reststop-humans-rigged-8-r1.zip'
assert not archive.exists(),'Preserve existing packages'
manifest=[]
with zipfile.ZipFile(archive,'x',compression=zipfile.ZIP_DEFLATED,compresslevel=3) as package:
    for key,row in sorted(rigs.items()):
        folder=Path(row['folder']);role=row['role'];prefix=f'{key}_{role}'
        for name in ('fresh-rig-validation.json','export-pose-comparison.json','video-validation.json'):
            assert json.loads((folder/name).read_text(encoding='utf8'))['ok'],key
        assert json.loads((folder/'visual-review.json').read_text(encoding='utf8'))['verdict']=='pass',key
        names=[role+'.'+ext for ext in ('blend','glb','fbx')]
        names+=['animation-preview.mp4','animation-preview.json','rig-report.json','fresh-rig-validation.json',
                'export-pose-comparison.json','video-validation.json','visual-review.json']
        for name in names:package.write(folder/name,prefix+'/'+name)
        package.write(folder/'poses/idle-001.png',prefix+'/preview.png')
        package.write(OUT/'inputs'/(key+'.png'),prefix+'/reference.png')
        manifest.append({'id':key,**row,'archive_folder':prefix})
    package.writestr('manifest.json',json.dumps(manifest,ensure_ascii=False,indent=2))
    package.writestr('README.md','''# 휴게소 인물 8종 — 본·동작 포함

주차 안내원, 요리사, 바리스타, 계산원, 청소원, 주유소 직원, 여행객, 경찰입니다.

각 폴더에는 텍스처가 포함된 Blender·FBX·GLB, 실제 동작 미리보기 MP4와 검증 기록이 있습니다. 각 인물은 18본과 9개 클립을 포함합니다. 기본 이동·공격·피격·사망·인사 외에 역할별 안내/서빙/청소/놀람/방어 동작이 들어 있습니다.

이동 클립은 제자리 동작입니다. 휴대 장비는 별도 소품이며 손 연결용 Grip.L/R 소켓을 제공합니다. 인물 높이는 3.05 작업 단위이고, 게임에서 배치할 때 원하는 스케일을 적용할 수 있습니다.

8종 모두 새 FBX·GLB 임포트, 반복 클립 끝점, 원본과 내보내기 포즈 비교, 주요 동작 시각 검토를 통과했습니다. 각 영상은 30fps, 338프레임입니다. 작은 잔여 사항은 visual-review.json에 기록했습니다.

이 파일은 완료된 인물 8종 묶음입니다. 휴게소 전체 90종 묶음의 완료를 뜻하지 않습니다. 나머지 소품은 제작 갤러리에서 진행 상태를 확인할 수 있습니다.
''')
with zipfile.ZipFile(archive) as package:assert package.testzip() is None
shutil.copy2(archive,G/archive.name)
receipt={'complete_subset':'humans','rigs':8,'clips':72,'archive':str(archive),'bytes':archive.stat().st_size}
(OUT/'humans-package-receipt.json').write_text(json.dumps(receipt,indent=2),encoding='utf8')
print(json.dumps(receipt),flush=True)
