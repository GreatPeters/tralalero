> Resolved later on 2026-10-04: the user provided the downloaded target on Desktop. Actual local pixels were opened and SHA-256 recorded in outputs/ch5-toon-2026-10-04/validation/reference-confirmed.json. This report preserves the earlier failed Library attempt, not current status.

# Ch5 툰 적용 — 참조 픽셀 접근 차단

최신 사용자는 `CH5-Mall-implementation-target.png`를 Ch5에 실제 적용하도록 승인했다. **미술 적용은 아직 시작하지 않았다.** 필수 이미지의 실제 픽셀 접근이 공식 경로 두 곳에서 실패했으며, 사용자 지시에 따라 우회하지 않았다.

## 확인한 장애

- Library 항목: `libfile_526a742767f8819191278fa911cf012c`, 파일명 `CH5-Mall-implementation-target.png`, image/png,1,747,450 bytes. 생성 콘셉트이며 실제 게임 화면이 아니다.
- 현재 Library 스킬의 지원된 read에서 `Native image pixels were unavailable`이 반환됐다. 명시적 image_file/include_images 읽기도 이미지0개를 반환했다.
- 현재 스킬의 공식 materialize 도우미가 Windows에서 `AttributeError: module 'os' has no attribute 'setxattr'`로 실패했다. 지정한 로컬 이미지 파일은 설치되지 않았다. 헬퍼를 수정하거나 직접 URL 다운로드, 다른 서버 전송, 메타데이터 우회, 실패 중간 파일 열기를 하지 않았다.
- 현재 제공된 도구에는 지원되는 로컬 브라우저 첨부 보기 도구가 없다. 부모 클라우드 경로를 이 PC 경로로 가정하지 않았다.

필요한 것은 추가 실행 승인이 아니라 **이 로컬 작업에서 실제로 볼 수 있는 동일 PNG 원본**이다. 일반 첨부로 전달하거나 실제 존재하는 접근 가능한 로컬 경로를 제공하면 현재 승인 범위를 그대로 이어갈 수 있다. 이미지를 보지 않고 설명만으로 참조 일치 작업을 완료했다고 하지 않는다.

## 독립적으로 완료한 준비

- Unity PID83488, ShoeTower의 깨끗한 Edit 상태. 미저장 씬·Prefab 편집·다른 검사 callback 없음. cycle18 최종376개 제품 기준과 비교해 추가 변경 없음.
- Ch5 씬과 meta의 내부 복구 사본2개를 만들고 해시 일치를 확인했다. 외부 백업이나 기존 파일 삭제는 하지 않았다.
- Ch5 의존 파일1592개와 Tools 씬8개의 해시, 저장값78개를 기록하고 준비 후 동일함을 확인했다.
- 현재 렌더러6600개/활성5032개, 재질157개를 읽기 전용으로 조사했다. 이는 객체 현황이며 실제 프레임 성능이나 폰 성능의 통과 수치가 아니다.
- 현재 카메라·충돌체·director/route·전투·리프트·분기·목표·보상 관련 컴포넌트와 재질/메시 참조를 기록했다. 첫 조회의 MeshFilter 없는 렌더러 처리 오류는 조사 코드에서 고쳐 재조회했고, 게임 소스는 변경하지 않았다.
- 제품 수정0, 씬/재질 저장0, Play0, 새 생성/결제/설치0. 설정과 엔진 세션 키를 복원하는 쓰기도 하지 않았다.

[준비 요약](../../outputs/ch5-toon-2026-10-04/preflight/preflight-summary.json) · [현재 구조](../../outputs/ch5-toon-2026-10-04/preflight/scene-inspection.json) · [복구 사본](../../outputs/ch5-toon-2026-10-04/preflight/recovery-manifest.json) · [보존 대조](../../outputs/ch5-toon-2026-10-04/preflight/preflight-integrity.json) · [장애 기록](../../outputs/ch5-toon-2026-10-04/preflight/reference-block.json)

## 픽셀 확인 후 실행할 승인 범위

Ch5 전용으로 복제한 FlatKit 재질과 새 시각 메시를 사용하고 공유 원본 재질/셰이더 및 Ch1~4는 유지한다. 크림 벽면·코랄 테두리/간판·청록 유리, 둥근 입구/기둥·깊은 쇼윈도·간결한 진열·식재가 사용자 설명이며, 정확한 형태·색·구도 판단은 실제 이미지 확인 후 한다. 색만 바꾼 상자로 끝내지 않는다.

1F 명품→2F 의류→3F 스포츠→4F 리빙→B1 또는5/6F 영화관→7F 신발 획득을 유지한다. 이미지에 두 층이 보인다는 이유로 전체 층을 줄이거나 실제 주인공을 임시 파란 캐릭터로 교체하지 않는다. 카메라·동선·충돌·전투·분기·보상은 보존한다.

대표 구간의 실제 게임 카메라 전후와 독립 시각 검토를 먼저 수행하고, 같은 스타일 규칙을 전체 층으로 확장한다. 이후 관련 두 경로·층 이동·전투·생존·Retry·보상을 검증하고 설정·자산 보존과 Editor 지표를 확인한다. 최종 전달은 생성 콘셉트가 아닌 실제 Unity 캡처다. 추가 현금 결제·충전·권한/보안 변경·공개·삭제·APK·폰 검증·commit/push는 승인 범위에 없다.
