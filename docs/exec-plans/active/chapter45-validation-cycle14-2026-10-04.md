# 실제 UI 입력·화면비 검증 — cycle14

2026-10-04 최신 위임: 기존 Unity Editor의 1080×2340 및 1080×1920에서 실제 시작 제스처와 표시/클릭 영역을 비교한다. 시작·분기·일시정지·재시작·결과 복귀의 빠른 중복 입력과 드래그 취소를 검사하고, 재현 결함만 수정한다. 기존 callback 검증을 실제 입력 검증으로 세지 않는다.

## 보존 및 범위

- 기존 PID 83488, branch `qa/chapters45-full-suite-20261002`, HEAD `295a18cab0ad4156baa9106077bc10a47ac6d734`를 유지한다.
- cycle13 완료 시점의 제품 306파일 해시 및 저장 78항목, QA Editor 9항목, Game View 설정·배율·포커스·커서를 fresh 캡처했다.
- Windows 공식 SendInput을 사용하며 전경 Unity Game View와 커서 소유권을 확인한다. 입력은 실제 UnityEngine.Input 및 EventSystem으로 들어간다. callback 직접 호출은 시험 중인 UI/시작/분기에서 금지한다.
- 분기와 골 인근 위치·전제 조건 seed는 명시된 directed fixture이다. 골 전제 조건을 위한 다른 선택의 Commit은 일반 플레이나 실제 분기 입력 검증에 포함하지 않는다. 전체 네 경로를 반복하지 않는다.
- 폰·기기·새 모델·유료 호출·APK·commit/push·외부 백업·기존 파일 삭제는 하지 않는다. 전체1135 검사/기존52 실패 재조사를 하지 않는다. 엔진 세션 키는 쓰거나 되감지 않는다.

## 진행

- probe-01: QA helper 타입 오류로 실제 입력 전 중단. 게임/Editor 복원 완료. 화면 복원 helper의 읽기 전용 scale setter 오류는 shownArea API로 수정했다.
- probe-02: 실제 누름·이동 관측. 커서 이탈 guard로 중단. 시험 소유 mouse-up을 별도로 해제하고 원래 화면·커서를 복구했다. 제품 결함으로 판정하지 않았다.
- probe-03: 7/7, 24/24 단계 통과. 독립 좌표검증 오차 약2.97 game px. 탭·수직·영역 밖 거절, 영역 안 수평 시작 확인. 모든 복원 receipt 정상.
- actual-ui-two-aspects-01: 제품 결함 재현. 수직 드래그 취소 후 armed 상태가 남고 FixedUpdate가 다음 down을 놓치면서 영역 밖 누름만으로 시작했다. observed-input frames224–235에 근거를 보존했다.
- PlayerScript.cs 한 파일 최소 수정: 시작 입력 처리를 Update로 옮기고, 누름 종료/취소 시 armed 상태를 해제한다. 수정 전 원본은 outputs/.../validation-cycle14/recovery에 fresh 해시와 함께 보존했다. Unity 컴파일 오류0.
- actual-ui-two-aspects-02: 수정된 시작 및 메뉴 17검사 통과 후 장면 로드 중 일시 해상도 전환 guard로 중단. 제품 실패0. QA 도구가 해당 프레임에 입력을 보류하도록 수정했다.
- actual-ui-two-aspects-03 진행 중. 완료 후 독립 코드·시각 검토, 정확한 화면/저장 및 기존 자산 보존 검증을 수행한다.

증거 루트: `outputs/chapter45-detailed-design-2026-10-03/validation-cycle14/`. 이전 실패·중단 기록을 덮어쓰지 않는다. 물리 기기 및 모바일 touch 경로는 검증 범위 밖이다.


## 완료 — 실제 OS 입력과 복원

제품 입력 문제4건을 두 파일에서 최소 수정했다. 최종 두 화면비103/103·느린 드래그7/7·로딩과 독립적인60ms OS 버스트45/45 통과, 실행 오류0. 일반 경로 전체는 반복하지 않았다. 도구/fixture 중단 이력은 삭제하지 않고 QA 이슈에 분리했다.

동일 PID clean Edit, 원래 화면·배율·포커스·커서와78설정 복원. 승인 코드2개 외304해시·208공유·210복구·6보호파일 동일. 새 삭제/결제/모델/폰/APK/commit/push0. 독립 코드·시각 검토 및 원본20장 갤러리 완료.

상세: [cycle14 결과](../../reviews/chapter45-validation-cycle14-2026-10-04.md).
