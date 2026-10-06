# 실제 OS 입력과 두 세로 화면비 검증 — cycle14

[PC 로컬 원본 갤러리](http://127.0.0.1:1626/validation-cycle14-20261004/) · [짧은 화면 재시작 후](../../tmp/image-previews/chapter45-model-production-2026-10-02/validation-cycle14-20261004/short-04-retry-lobby.png) · [수정 전 오진입](../../tmp/image-previews/chapter45-model-production-2026-10-02/validation-cycle14-20261004/before-short-retry.png)

2026-10-04. **실제 입력에서 발견한 네 문제를 제품 두 파일에서 수정했다. 두 화면비 흐름 103/103, 느린 시작 7/7, OS 60ms 버스트 45/45 검사가 통과했다.** 각각 282/282, 35/35, 102/102 단계이며 최종 실행 오류0이다. 서로 겹치는 검사이므로 고유 항목155개라고 합산하지 않는다.

이번 변경은 기존 게임의 입력 경계 수정이다. 기존 씬·미술·모델·전투 수치·스토리·저장 구조를 재설계하지 않았다. 새 모델/생성/유료 호출/미술 자산 변경0이다.

## 재현과 실제 적용

| 문제 | 변경 전 실제 증거 | 적용 및 변경 후 |
|---|---|---|
| 취소한 시작 드래그가 영역 밖 누름으로 이어짐 | two-aspects-01 frames224–235. 수직 드래그 종료 후 armed가 남고, FixedUpdate가 새 Down 프레임을 놓쳐 영역 밖 누름만으로 시작 | PlayerScript 시작 판정을 Update로 이동. mouse release/touch 종료·취소 시 armed 해제. 양 화면비에서 탭·수직·영역 밖 입력 거절, 영역 안 수평 입력 시작 |
| 느린 수평 이동은 시작되지 않음 | slow-start-reproduction-01: 실제4px씩12회 이동 후 시작 실패 | 시작 press 원점에서 누적8px을 판정. slow-start-regression-01 동일 입력7/7 통과 |
| pause 버튼 누름만으로 레인이 옆으로 튐 | two-aspects-02 frames502–504: 정지한 커서인데 lane0→1.75m | Update에서 press 기준점을 기록하고 실제 이동은 기존 FixedUpdate 유지. 양 화면비 첫/두번째 pause 클릭 모두 lane0→0 |
| 재시작 다음 클릭이 새 로비의 스토리를 누름 | two-aspects-06 short-04-retry-lobby.png. Retry 좌표가 새 로비 Story 영역과 겹쳐 오진입 | CanvasScript가 새 로비에서0.3초 및 누름 해제까지 CanvasGroup 상호작용/시작을 차단. 기존 그룹 값 복원, 새 그룹은 런타임에서 제거. 후속 클릭 및 실제60ms OS 버스트에서 로비 유지·장면 로드1회 |

제품 변경은 `Assets/ShooterSurvival/Scripts/Player/PlayerScript.cs`, `Assets/ShooterSurvival/Scripts/UI and VFX/CanvasScript.cs`이다. 각각 수정 전 정확한 바이트/해시를 이 cycle의 PC 내부 recovery에 보존했다. Unity 컴파일 오류0. 첫 입력 수정 중간본도 별도로 보존했다.

## 실제 입력과 확인 범위

설치된 Pipeline에는 마우스 주입 명령이 없어 [Microsoft SendInput](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-sendinput)을 사용했다. Game View의 화면 위치·배율을 읽고 실제 Unity Input 좌표로 보정한 뒤 다른 지점에서 독립 검증했다. 표시된 버튼의 전체 사각형이 화면 안에 있고 중앙 raycast가 해당 Button에 닿는지 확인하고, 그 화면 좌표로 OS mouse move/down/up을 보냈다. 시험 중인 시작·버튼·분기 callback을 직접 호출하지 않았다.

기존 고정 프리셋1080×2340(index23),1080×1920(index22)만 사용했다. 설정·pause·계속하기·retry·결과 돌아가기의 실제 중앙 입력과 드래그 밖 취소를 확인했다. 클릭 가능한 모든 픽셀이나 모든 frame rate를 검사했다는 주장은 하지 않는다. 분기는 클릭 버튼이 아니라 **공간 레인 선택**이며 실제 드래그로 좌/우를 선택했다. 양 화면비에서 Ch4와 Ch5의 반대 선택을 각각 시험하고, 선택 후 반대 드래그로 다시 선택되지 않음을 확인했다.

최종103검사에서 시작/설정 차단/닫기/계속하기/재시작/분기/물리 신발 획득/결과 복귀가 통과했다. pause 중 시간·거리 유지, resume 때 같은 장면에서 진행, retry/return 때 새 장면1회와 wallet 유지, 로비에 스토리 overlay가 남지 않음을 확인했다. 앱 재실행 후 중간 위치 이어하기 기능은 추가하거나 주장하지 않았다.

Editor 스레드에서 발송하는 클릭은 장면 로드 중 간격이 늘어날 수 있어, **별도 OS 배경 스레드 버스트**를 추가했다. 양 화면비 Retry/결과 Return 네 경우에서 첫 mouse-up→다음 down→up의 실제 간격은 60.007~61.177ms다.12개 이벤트 모두 SendInput 반환1이며45검사 통과다. 배경 스레드는 Unity API를 호출하지 않고 전경 HWND·대상 HWND·커서 소유권을 확인한 후 Win32 입력만 보냈다. 이는 OS 이벤트 주입과 최종 게임 반응의 증거이며 Unity가 각 이벤트 경계를 모두 프레임별 로그로 남겼다는 주장은 아니다.

분기 인근과 골 인근은 기존 directed fixture를 사용했다. 전제 전투 완료·목표 상태·위치를 seed한 후 시험 분기는 실제 드래그로, 신발은 실제 물리 접촉으로 진행했다. 골 전제 조건의 다른 선택 Commit은 fixture로 명시했으며 실제 플레이 통과로 세지 않았다. 골 검사는 새 Ch5 실행으로 분리하고 진행 중 리프트가 없음을 확인했다. 전체 일반 네 경로·전투·리프트 완주를 이번에 반복하지 않았으며 해당 근거는 cycle12에 보존돼 있다.

## 중단 기록과 독립 검토

초기 helper 타입/복원 API 오류, 좌표·커서 소유권 중단, 로드 중 일시 해상도, 비동기 로드 대기, Ch5 선택 층, 골 fixture 상태 문제를 각각 보존했다. 이들은 제품 실패로 억지 수정하지 않았다. 상세 이력은 [QA 도구 이슈](../../outputs/chapter45-detailed-design-2026-10-03/validation-cycle14/qa-issues.json)에 있다. 기존 receipt를 덮어쓰지 않으며 Prepare가 성공하면 이후 단계 실패에도 저장 복원을 시도한다. 외부 포커스/커서 소유권을 잃으면 입력을 중단한다. 외부 앱으로 포커스가 넘어간 모든 경우의 mouse-up 해제를 보장했다는 주장은 하지 않는다.

독립 코드 검토가 느린 드래그와 pause 레인 이동을 발견했고, 수정 및 실제 회귀를 확인했다. 최종 Canvas 변경의 새 P1/P2는 발견되지 않았다. 원본 최종18장과 지정된 수정 전2장을 독립 시각 검토했다. 시각 검토는 버튼 문구·화면 내 배치, 선택 후 안내를 평가하며 실제 클릭/선택 성립은 기능 로그와 구분한다. Editor의 Combat Harness overlay는 원본 화면에 그대로 있으며 출시 UI로 새로 추가한 것이 아니다.

## 저장·화면·기존 작업 보존

최종 기준306파일 중 승인된 코드2개만 변경,304개 동일. 공유208자산·기존210복구본·보호 앱6파일·persistentData1파일 동일. 기존 제품53,461경로 모두 존재한다. inherited tracked 삭제127개 유지, 새 삭제0. branch `qa/chapters45-full-suite-20261002`, HEAD `295a18cab0ad4156baa9106077bc10a47ac6d734` 유지.

fresh 캡처한 게임7키의 존재/int값과 locale을 복원했고 전체 관측78설정이 일치한다. coin33031/jewel30/chapter_unlocked2로 복원했고 chapter_rewarded3/4/5와 TutorialDone의 원래 부재를 복원했다. native 비세션41값이 동일하다. 자연 증가한 엔진 세션 식별/횟수는 그대로 유지하며 엔진 키 쓰기·직접 registry 쓰기0이다.

동일 Unity PID83488의 clean Edit로 종료했다. 원래 씬·play 시작 씬·QA9설정·시간값과 Game View 인스턴스·위치·크기·해상도index·배율·이동·low-resolution 설정·포커스·전경 HWND·커서가 fresh 원본과 정확히 일치한다. 최종 native audit는 버스트 실행 **뒤**에 수행했다. PT_ResourcesCleanup은 이번 캡처의 존재/false를 보존했으며, 과거 미기록 값까지 복원했다고 주장하지 않는다.

휴대폰/touch·새 플랫폼·APK·모델 생성·결제·인증/설치·commit/push·외부 백업·기존 파일 삭제0. 전체1135 검사와 기존52 실패 조사도 수행하지 않았다. 남은 실행 차단은 없다. 휴대폰 터치, 다른 FPS/장치, 인간의 조작감·난이도·지루함 평가는 이번 증거 밖이다.

## 증거와 명령

- [최종 두 화면비103검사](../../outputs/chapter45-detailed-design-2026-10-03/validation-cycle14/actual-ui-two-aspects-07/summary.json)
- [느린 시작7검사](../../outputs/chapter45-detailed-design-2026-10-03/validation-cycle14/slow-start-regression-01/summary.json)
- [실제 OS 버스트45검사](../../outputs/chapter45-detailed-design-2026-10-03/validation-cycle14/os-burst-two-aspects-01/summary.json) · [이벤트 UTC](../../outputs/chapter45-detailed-design-2026-10-03/validation-cycle14/os-burst-two-aspects-01/background-os-bursts.json)
- [최종 저장·자산 감사](../../outputs/chapter45-detailed-design-2026-10-03/validation-cycle14/integrity-after-burst.json) · [정확한 화면 복원](../../outputs/chapter45-detailed-design-2026-10-03/validation-cycle14/display-final-confirmed.json)
- [독립 코드](../../outputs/chapter45-detailed-design-2026-10-03/validation-cycle14/independent-reviews/code-review.json) · [OS 버스트 후속](../../outputs/chapter45-detailed-design-2026-10-03/validation-cycle14/independent-reviews/code-review-burst-followup.json) · [독립 시각](../../outputs/chapter45-detailed-design-2026-10-03/validation-cycle14/independent-reviews/visual-review.json)
- [입력 범위](../../outputs/chapter45-detailed-design-2026-10-03/validation-cycle14/coverage.json) · [원본20장 HTTP/해시](../../outputs/chapter45-detailed-design-2026-10-03/validation-cycle14/gallery.json)

실행: `run-input14.py actual-ui-two-aspects-07`, `run-input14.py slow-start-regression-01 BeginSlow`, `run-input14.py os-burst-two-aspects-01 BeginBurst`, `audit-validation14-after-burst.py`. 모두 workspace `trellis-recovery` 아래 스크립트를 Python UTF-8로 실행했다. 이력 ID 재실행은 거절된다. 향후 검사는 새 cycle의 fresh 원본과 새 출력 ID가 필요하다.
