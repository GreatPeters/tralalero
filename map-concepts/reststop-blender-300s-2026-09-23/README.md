# 휴게소 · Blender 300초 / 10구간

## 요청과 범위

사용자가 지정한10구간 순서로 하나의 연결된 휴게소 환경과300초 프리비즈를 제작한다.5구간 방어전은60초, 탑뷰, 제자리 유지, 연속 조준 회전90도/초 제한이다. 이전 PPT 종합 시안에 편의점과 인물8종의 새 콘셉트 이미지를 더했다. TRELLIS 및 Seedance는 호출하지 않았고 Unity 씬·에셋은 변경하지 않았다.

완료: 연결된 마스터, 구간 영상10개와300초 통합 영상, 인물·편의점 새 시안, 정적 환경GLB를 보관했다. 영상은1,280×720·24fps다. 최종10개 영상에서 각각4개 시점을 추출해 직접 검토했으며, 통합본의7,200개 디코드 프레임 해시가10개 클립을 순서대로 이어붙인 해시와 완전히 일치한다.

## 타임라인

|영상|전체 시간|길이|내용|
|---|---|---:|---|
|01|0–25s|25s|고속도로 분기, 안내 표지, 차단기|
|02|25–50s|25s|주차 공간, 후진 차량 예고|
|03|50–75s|25s|건물 앞 휴식 공간, 도망가는 사람과 간식 카트|
|04|75–100s|25s|자동문, 본관 진입, 탑뷰 전환|
|05|100–160s|60s|위치 고정, 네 방향 경찰 접근, 연속 회전·발사|
|06|160–190s|30s|식당, 테이블, 주문대, 배식 카트|
|07|190–220s|30s|편의점, 선반, 냉장고, 계산대, 카트 우회|
|08|220–250s|30s|확대된 화장실, 남녀 구획, 세면대, 청소 구역|
|09|250–275s|25s|주유기, 주유소 차량 통과|
|10|275–300s|25s|추격 적, 출구 차단기, 출구 통과|

실제 마스터는24fps·7,200프레임이다. 영상은 이 타임라인을 정확한 경계에서 나누며 생성형 영상, 프레임 보간 또는 정지 이미지 늘이기를 사용하지 않는다. 본관 지붕은81–251초, 주유소 캐노피는255–277초에 내부 촬영을 위해 걷어낸다. 주인공을 가리는 정면 간판도84–101초에 숨긴다. 같은 장면을 유지하는 촬영용 가림 처리이며 완성 게임의 오클루전 구현은 아니다.

## 원본과 구현

- Blender5.2.1 LTS. 환경은Python으로 직접 모델링한 하나의 배치다. 본관·식당·편의점·화장실·주유소·진입/출구가 하나의 좌표계에 있다.
- 상어는 기존 프로젝트 `Assets/JH/Model/Player/Sharks/.../Original.fbx`를 가져와 기존 형상과 스킨을 유지했다. 기존 소스 파일은 수정하지 않았다.
- 사람은 새 콘셉트의 역할·색상·복장을 반영한 직접 제작 프리비즈 메시다. 인물 콘셉트와1:1로 재구성한 최종 캐릭터가 아니다. 인물은 관절별 부모 계층으로 팔·다리를 움직이며, 상어는 기존 리그를 사용한다.
- 방어전: 네 방향의8개 웨이브,32명 접근,35회 물 투사체, 실제로 기록된 발사/도착 시점에 피격과 퇴장을 연결한다. 주인공의XY는(0,20)에 고정하고 회전 목표를 속도로 제한한다. 게임의 체력/밸런스 시뮬레이션은 아니다.
- 화장실은30×20m=600㎡다. 앞 콘셉트의 정확한 치수는 없으므로12×10m=120㎡를 개념 기준으로 정한 면적5배 해석이다. PPT 실측값 또는 사용자가 지정한 치수로 주장하지 않는다.

## 검증 증거

- `outputs/reststop-blender-300s-2026-09-23/scene-validation.json`: 새 Blender 프로세스로 마스터를 열고1,441개 방어전 프레임의 고정 위치·탑뷰·회전 속도 검사. 네이티브 부동소수점 값의 최댓값90.003752도/초는0.01도/초 수치 허용차 이내다. 초 단위 원본 시뮬레이션은90도/초,0.1초당9도 이내다.
- 최종`scene-validation.json`:880,366삼각형의 전체 프리비즈 씬. 건물뿐 아니라 인물·차량·가구·식재·애니메이션 개체를 포함한다. 초기생성수치인`build-metrics.json`과 구분한다. 모바일 최종 자산 예산 검증은 아니다.
- `fresh-glb-validation.json`: 환경GLB를 새 프로세스에 재임포트.260메시,388,430삼각형,29재질,10개 구간의 실제 명명된 환경이 확인된다. 정적 환경만 내보내며 전체 애니메이션은`.blend`에 있다.
- `evidence/`: 각 구간의 실제 EEVEE 정지 렌더. `fresh-glb-overview.png`: 재임포트한 환경의 전체 배치 렌더.
- 각 MP4는 전체 디코드 오류, 실제 프레임 수, 길이, SHA-256을 검사한다. 완료 영수증은`videos/*.json`, 종합은`video-manifest.json`에 남긴다.
- `final-verification.json`:10개 클립과 통합본의 디코드 프레임 순서까지 일치. 전체300.00초·7,200프레임.
- `timeline-tests.txt`:시간 구간, 제자리 유지, 속도 제한,0.1초 회전량, 사방 접근, 실제 비행시간,각도랩,반프레임 가시성,공용 출입구 경로의10개 테스트 통과.
- `restroom-camera-visibility.json`, `indoor-aisle-check.json`:수정된 카메라6개 시점에서 몸통 가시성 확인,190–253초의 정적 중심동선505표본에 막는 면 없음. 플레이어 전체 충돌체를 쓸어 검사한 결과나 실제 게임 물리 검증은 아니다.
- `actor-ownership-validation.json`:정적인 직원6명의 다리를 각각의 관절로 복구.3,696삼각형 수가 같고 정점 오차는0.000018m 미만이므로 화면 형상을 유지한다. 이후 환경 내보내기에서 모든 인물 부품을 제외했다.
- `asset-evidence/contact-sheet.jpg`, `people-native-blender.png`:재임포트 환경의 여러 방향과 실제 인물8종을 직접 열어 확인했다. 넓은 환경 측면 뷰는 배경 지형이 포함되며, 플레이 시야 평가는 각 영상의 프레임으로 수행했다.

## 전달 파일

- `outputs/reststop-blender-300s-2026-09-23/videos/01-reststop.mp4`~`10-reststop.mp4`.
- `outputs/reststop-blender-300s-2026-09-23/videos/reststop-300s.mp4`.
- `outputs/reststop-blender-300s-2026-09-23/reststop-master.blend`:Blender5.2.1 LTS, 텍스처·폰트 포함. 시작 프레임의 카메라 뷰로 열리고 별도 스크립트 허용 없이 네이티브 키로 재생한다.55개 가림 객체는 뷰포트와 렌더의 숨김 타이밍을 같이 보관했다.
- `outputs/reststop-blender-300s-2026-09-23/reststop-environment.glb`:정적 환경만 포함.
- `tmp/image-previews/reststop-blender-300s-2026-09-23/index.html`:오프라인에서도 열 수 있는 갤러리. 원본PNG·각MP4·통합MP4·Blender·GLB 사본을 같은 폴더에 둔다.

정적 환경 일부는 구간·재질별로 합쳐져 있다. 개별 시설의 재배치는`tools/reststop-previs/`의 제작 파라미터로 재생성하는 방식이 기준이며, 인물과 움직이는 장치는 독립된 계층을 유지한다. 최종 게임 아트·모바일 성능·물리·밸런스를 완료했다고 주장하지 않는다.

## 재현

`tools/reststop-previs/`에`timeline.py`, `geometry.py`, `people.py`, `build.py`, `polish.py`, `render.py`, `validate.py`, `verify_export.py`, `encode.py`, `gallery.py`가 있다. 생성된 모델을 손으로 덮어 고치는 대신 제작 코드의 변경을 반영했다.

```powershell
python tools/reststop-previs/timeline.py
& 'C:/Users/ljh/AppData/Local/Programs/Blender Foundation/Blender 5.2/blender.exe' -b -t 4 --python tools/run-limited-generation.py -- --script tools/reststop-previs/build.py --record outputs/reststop-blender-300s-2026-09-23/build-resource.json --threads 4
& 'C:/Users/ljh/AppData/Local/Programs/Blender Foundation/Blender 5.2/blender.exe' -b -t 4 --python tools/run-limited-generation.py -- --script tools/reststop-previs/render.py --record outputs/reststop-blender-300s-2026-09-23/animation-resource.json --threads 4 -- --mode animation --width 1280 --samples 8 --start 1 --end 7200
python tools/reststop-previs/encode.py --watch
python tools/reststop-previs/gallery.py
```

렌더 재시작은 같은 씬의 이미 완성된JPEG만 건너뛴다. 모델이나 애니메이션을 바꾸면 기존 프레임을 섞지 말고 새 출력 리비전을 사용해야 한다. 기존 Unity/사용자 프로세스는 건드리지 않는다. 작업 소유 프로세스는 스레드4개·낮은 우선순위·한 번에 하나의 Blender GPU 작업으로 제한했다.

갤러리: <http://127.0.0.1:8767/>. 전용 폴더만 제공하며 서버가 꺼져도 해당 폴더의`index.html`로 볼 수 있다. 사람·편의점 PNG는`tmp/image-previews/reststop-blender-300s-2026-09-23/`, 생성 프롬프트는`concept-prompts.json`에 있다.
