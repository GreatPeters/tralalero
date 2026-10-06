# 독립 시각 검토 후 좁은 수정 — 2026-10-05

[실제 전후 갤러리](http://127.0.0.1:1626/independent-visual-review-20261005/index.html)

독립 아트 검토가 발견한 4F·5F 도착·7F의 P1 가림과 오른쪽 보너스 밑줄 누락을 수정했다. 전체 아트 또는 모바일 성능 합격으로 확대하지 않는다. 새 한국 일상 인물·농구선수 보강은 Pass27에서 진행 중이다.

## 실제 변경
- ShoeTower의 기존 카메라 가림 처리에 문제 코너 매장 4곳의 천장·소핏·둥근 전면 12개를 추가했다(121→133). 구조·충돌·카메라·적 수치·공격은 유지한다. 카메라를 막는 동안 렌더러만 숨기고 복원한다.
- HighwayBonusCardPresentation의 선택 밑줄을 실제 픽업의 좌우 판정(RouteFrame.Lane > 0)에 맞췄다. 획득 트리거·보상 로직은 그대로다.
- 새 생성 모델 0, 새 생성 비용 0. 이번 수정은 기존 구조물의 가림 등록과 UI 표시 코드다.

## 검증 근거
- 실제 영화관 분기 일반 플레이: 13검사 통과, 실패0·오류0. 보통 HP60/공격8, 에스컬레이터3·리프트3·6F 30초 생존·7F 목표·일시정지·첫/반복/중복 보상. `outputs/camera-restore-2026-10-05/validation/route-independent26-final/suite-summary.json`.
- 실제 하이패스/현금 2경로 통과. 기존 성장 fixture HP3300/공격88에서 시작해 1950m 보너스와 이후35m 확인이며 Ch2 전체 완주 증거는 아니다. 좌우 각5개 선택 근접 표본 모두 선택 쪽만 밑줄 활성.
- 독립 reviewer는 같은 포즈 전후8쌍, 수정 후 실제 경로18장, 좌우 선택2장을 직접 보았다. P1 가림3곳 및 P2 밑줄 수정 범위 시각 합격. 연속 동영상 전체 프레임 판정은 아니다.
- 5F 매점/대기 공간의 정면 가독성은 현재 표본만으로 미확인. Ch4 가까운 시점에서 타워 꼭대기 운동화가 화면 밖으로 나가는 구도 한계가 있다. 가림 처리 후 일부 매장 글자가 공중에 남아 보이는 경미한 현상은 유지 기록한다.

## 보존과 설정 소유권
- `outputs/independent-visual-review-2026-10-05/checkpoint/`가 최신 보존 근거다. 원본 Pass24 스냅샷을 재바인딩/덮어쓰지 않았다. Pass27 검증을 위해 같은 guard를 유지한다.
- 게임 PlayerPrefs 78개, 프로젝트 소유 SessionState 9개를 타입·존재 여부·값까지 복원 검증했다. 현재 PT_ResourcesCleanup도 기록/복원했고 과거 전체검사 전 미기록값을 안다고 주장하지 않는다.
- SessionState 9개는 TestStartStage/Active/DefaultPath/AppliedPath, OpeningVideoEnabled, TestPower9999, TestFastLateral, TestTimeScale, SR18.Progression.Active다. 각각 NoryangjinMapToolTestStartStage.cs, OpeningStoryUI.cs, NoryangjinMapToolTestOverrides.cs, NoryangjinMapToolTestSpeed.cs, Sr18ProgressionPlaytest.cs에서 소유하는 프로젝트 검사 설정이다. 엔진 레지스트리 메타데이터가 아니다.
- 엔진 레지스트리는 읽기 비교만 했으며 Unity 자체의 session id/count 변화 외 수정하지 않았다. Native 창 이동/크기/포커스 조작 없음.
- Pass26 전후 5씬의 모든 감사 대상 Transform 및 Camera/StableGameplayCamera 직렬화가 같다. Ch1·2·3의 카메라는 Pass24 복원 기준과 동일하다. Ch5 상대 카메라(0,5.8,-12), pitch19,FOV68도 동일하다. **Pass25 경로 시작 방향 변경으로 Ch5 저장 world Transform은 Pass24와 다르다**. 상대 구도 보존을 절대 위치 보존으로 바꾸어 표현하지 않는다.
- 최소 공유 복구9파일을 확인했고 발생한 폰트/표면 변경은 변경본을 PC 안에 남긴 후 원본 해시로 복원했다. branch/head 및 기존 삭제 보존; commit/push/APK/외부백업 없음.
