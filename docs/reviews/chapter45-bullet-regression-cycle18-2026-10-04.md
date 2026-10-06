# Ch4·Ch5 근거리 탄환 영향 확인 — cycle18

상태: **영향 범위·검사 준비·보존 확인 완료, 새 Unity Play 검사는 시작 전 차단**. 합의된 콘텐츠 적용과 cycle17 결과를 유지하며, 이번 추가 검사를 완료/통과로 표시하지 않는다. 새 제품 수정과 신규 모델·유료 호출은 없다.

## 현재 실제 단계와 시각

- 마지막 네이티브 실행 시도: 2026-10-04 11:50:16 UTC. 최근 입력 후 0.015초로 실행 보호 가드가 Play와 게임 설정 준비 전에 중단했다.
- 보존 검사 완료: 2026-10-04 11:58:00 UTC. 제품 파일376개·복구 사본211개·관찰 저장값78개·native registry 전체 관찰 값·userdata·branch/HEAD 동일.
- 마지막 읽기 전용 관찰: 2026-10-04T12:01:24.7411548Z. 입력 후 0.000초, Unity 최소화/깨끗한 Edit 상태, 전면은 다른 프로세스. 이번 native Play0, 새 native 통과0.
- 15초 무입력 기준은 동시 작업을 보호하기 위한 검사 가드다. Windows 최근 입력 신호만으로 사람이 입력했는지, 자동 재입력이 발생했는지는 확정할 수 없다. 원인 미확정 신호를 무시하거나 창/포인터를 강제로 복원하지 않았다.

## 확인한 영향 범위와 준비한 검사

현재 BulletScript의 새 경로는 Ch4의 `SecurityPartition_1580`, `LeftSecurityPanel`, `RightSecurityPanel` 세 패널에 적용된다. 저장 씬과 현재 Ch5의 `targets` 배열은 비어 있다. Ch5 일반 적은 기존 trigger 경로를 사용하므로 그 경로의 간섭 여부를 확인하는 검사로 구분했다.

실제 Water/Bomb prefab 풀을 사용하는 검사 코드를 기존 프로젝트 참조로 오프라인 컴파일했다. 컴파일 성공은 실행 통과 근거가 아니다. 각 타입에서 내부 중앙, 앞/뒤 경계 안쪽, 앞/뒤 경계 바깥, 동시3발, 정지 후 재개를 준비했다. 기존 Retry 버튼으로 새 씬을 연 뒤 같은 조건을 반복하도록 했다. 계획된 발사 조건은 Ch4 84 + Ch5 28 =112개이며 **아직 실행하지 않았다**.

검사는 위치·발사점·상대 조우를 통제하는 directed fixture다. 실제 Update/FixedUpdate/물리 접촉을 기다리며 피해·ReceiveProjectile·trigger·FixedUpdate를 수동 호출하지 않는다. 전체 자연 플레이나 사람의 입력 체감 평가가 아니다. 재현되지 않은 일반 적의 내부 생성 관통 가능성은 결함으로 확정하거나 수정하지 않았다.

[검사 준비와 상태](../../outputs/chapter45-detailed-design-2026-10-03/validation-cycle18/readiness.json) · [원본 스크립트의 검토용 사본](../../outputs/chapter45-detailed-design-2026-10-03/validation-cycle18/prepared-scripts/) · [Play 이전 차단 증거](../../outputs/chapter45-detailed-design-2026-10-03/validation-cycle18/ch4-attempt1-blocked.json)

## 이미 완료된 적용·검증과 남은 조건

기존 [cycle17 보고서](chapter45-validation-cycle17-2026-10-04.md)의 최종 소스는 그대로다. Ch4 좌우와 Ch5 두 경로의 실제 Start·전투·선택·연결·리프트·보스·보상42검사가 통과했다. 별도 배우/팝콘 조건49개와 동기 탄환 guard30개는 서로 다른 범위의 기존 증거다. 근거리 패널은 수정 전9발에도 HP40, 수정 후5발로 HP0/개방·전진을 확인했다. 이 통과 수를 이번 새 검사로 다시 계산하지 않는다.

60행 요구 추적 기록을 보존했다. 최신 콘텐츠 적용·23종 모델 원장 범위에서 누락된 생성 모델 유형은 없으며, 이번 읽기 전용 확인으로 새 미적용 콘텐츠나 재현 결함이 발견되지 않았다. 전체 레퍼런스 수준의 미술 완성, 거리/매장 표면의 반복감, 사람의 재미와 모든 빠른 마우스 재입력 경계까지 완료했다고 확대하지 않는다. 아직 남은 승인 범위의 작업은 위 근거리·경계·다중 탄·정지/재개·Retry의 새 native 검사다. 기존 통과 경로의 무한 재검사, 기존52개 실패 수리, 폰/APK 검증은 이번 완료 조건에 넣지 않는다.

## 모델·비용·복원

원장23종 = local TRELLIS.2 환경/소품12 + Meshy 캐릭터10 + Meshy 수건1. cycle18에서 새 생성/재생성/유료 요청/계정 조회0. 과거 확인 비용636크레딧(캐릭터606 + 수건30)은 이력이며 현재 잔액을 새로 조회하지 않았다. 원장의 근거 파일 244개를 새로 SHA-256 대조했다. 기존 모델과 씬 참조를 유지했고 네이티브 후처리를 새 AI 모델로 세지 않았다.

게임 설정 준비와 Play에 들어가지 않았으므로 게임 설정을 바꾸거나 복원하는 쓰기도 하지 않았다. 읽기 전용 보존 확인에서 원래 scene/start selector/시간/관찰 SessionState/PT_ResourcesCleanup 값을 유지했다. 캡처용 QA checksum SessionState 한 개만 준비 단계에서 작성했다. 네 Unity 엔진 세션 키는 쓰지 않았다. 과거 전체 suite 이전 PT_ResourcesCleanup 값이 없다는 역사적 한계는 그대로다.

화면 관찰 13필드 중 10개 동일; 바뀐 필드는 foreground, foregroundPid, cursor. 현재 외부 데스크톱을 보존했고 화면·포인터 복원 호출0이다. 강제 전면화·기존 파일 삭제·commit/push·외부 백업·설치는 하지 않았다.

최초 보존 검사 Python은 결과에 없는 `playing` 필드를 읽어 중단했다. 기존 증거를 보존하고 실제 ownership 결과의 필드를 사용해 수정했으며 위 11:58 검사가 통과했다. 이는 검사 코드 오류이며 게임 실패가 아니다.

[보존 결과](../../outputs/chapter45-detailed-design-2026-10-03/validation-cycle18/readonly-integrity.json) · [화면 설정 대조](../../outputs/chapter45-detailed-design-2026-10-03/validation-cycle18/display-readonly-comparison.json) · [모델·비용 재확인](../../outputs/chapter45-detailed-design-2026-10-03/validation-cycle18/model-cost-recheck.json)
