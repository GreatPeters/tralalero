# Ch4/Ch5 cycle17 — final cycle16 impact regression

기존 cycle16 장면·모델을 보존하고 최종 소스에서 Ch4 좌우 및 연결되는 Ch5 두 경로를 완료했다. 재현된 근거리 패널 탄환 누락만 수정했다. 신규 모델 생성·유료 호출·크레딧 사용0. 전화기/기기 검사·APK·commit/push·외부 백업·기존 파일 삭제는 하지 않았다.

[실제 Unity 화면 갤러리](http://127.0.0.1:1626/validation-cycle17-20261004/) · [검증 자료](../../outputs/chapter45-detailed-design-2026-10-03/validation-cycle17/) · [23종 장부](../../outputs/chapter45-detailed-design-2026-10-03/validation-cycle17/model-ledger.md)

## 전체 경로와 별도 조건 검사

| 경로 | 실제 게임 시간 | 종료 HP | 리프트 | 오류 |
| --- | ---: | ---: | ---: | ---: |
| Jamsil / 0 | 302.27s | 56 | 0 | 0 |
| ShoeTower / 0 | 286.57s | 53 | 5 | 0 |
| Jamsil / 1 | 299.78s | 52 | 0 | 0 |
| ShoeTower / 1 | 307.23s | 35.2 | 6 | 0 |

네 경로 suite의 독립 assertion 42개 통과, 실패0, Error/Exception/Assert0. 실제 Start 흐름으로 시작해 기존 보행·전투·선택·리프트·보스·목표·보상 처리를 실행했다. Ch4 완료가 Ch5로 연결됐다. ordinary stats/1x clock이며 teleport/health pin/force win/manual damage는 사용하지 않았다. 입력은 시야에 들어온 목표에 지연·횡이동 상한을 적용하는 QA driver이다. 이 검사는 OS 손가락 입력이나 인간의 재미 평가를 뜻하지 않는다. 일반/리프트/영화관 일시정지와 중복 완료 보상 방지 등은 suite-summary의 개별 근거를 참조한다.

별도 directed fixtures: queue 10검사, popcorn-lifecycle 26검사, popcorn-transfer 13검사, 실패·오류0. 시작 위치 지정, 다른 조우 제외, 발사 비활성화, 명시적 사망/조우 ResetForRun/리프트 진입 조건 우회를 사용했으므로 전체 자연 플레이와 분리한다. 실제 발사·비행·경고·줄 동작·접촉은 native Update/FixedUpdate가 처리한다. 별도 Bullet guard fixture는 실제 Water/Bomb 풀과 임시 타깃을 사용한 동기 메서드 호출30검사이며 전체 플레이 근거에 섞지 않는다.

## 실제 수정한 결함

최초 Ch4-left 시도는1677.96582m에서 닫힌 왼쪽 패널을 향한 탄이 패널 내부에서 생성된 뒤 첫0.64m 이동으로 얇은 trigger를 지나쳐 멈췄다. 이미 열린 오른쪽 경로는 있었으므로 절대 진행 불가라고 주장하지 않는다. 동일 시작 조건5초에서 수정 전9발에도 HP40 유지, 수정 후5발로 HP0/개방·전진을 확인했다. 각20개는 캡처 표본 수이다.

제품 변경은 `Assets/ShooterSurvival/Scripts/Weapon/BulletScript.cs` 하나다. 양수 비행 이동 전에 현재 겹치는 활성·enabled·alive·eligible Chapter45 타깃을 검사하고 기존 CanHitTarget/ReceiveProjectile/ReturnToPool을 사용한다. 피해량·속도·모델·씬·충돌체 크기는 변경하지 않았다. shield/zero damage는 기존대로 탄만 소비하며, 다른 층·미입장·비활성·정지·이동 중 대상은 우회하지 않는다. Water/Bomb 각각15조건과 중복 콜백 후 풀 개수·체력 보존을 확인했다. 일반 고속 관통 전체를 해결했다는 주장은 아니다.

첫 panel-after는 Play 직후 지연 컴파일/도메인 재로드로 QA callback이 소실되어 무효였다. Edit 컴파일 완료 뒤 panel-after2가 통과했다. 복원 중 너무 일찍 시작한 routes-fixed는 preference guard가 gameplay 전에 거부했다. 첫 queue 준비는 데스크톱 입력 변화 guard가 Play 전에 거부했다. 준비까지 finally 복원 범위에 넣고 동일 guard를 유지한 새 queue-final 실행이 통과했다. 최초 팝콘 lifecycle은 pause 중 탄 하나의 렌더 위치0.00000033m 차이를 exact JSON 비교가 거부했다. 원본 수치와 실패를 보존하고 구조 동일성+절대1e-5 수치 오차 기준으로 재검사했다. 실제 clock/lifetime/HP/velocity 변화나 제품 결함으로 분류하지 않았다. 이 검사 하네스 실패와 최초 실제 결함 실행은 보존하며 성공 집계에서 제외했다.

## 상가·중년 대기줄·팝콘 영향

새 상가21개는 기존 Hero 상가 재사용이며 cycle16 저장본 그대로다. 좌우 경로 화면 안 타워 기준점/위험 예고점 2876개 표본에서 신규 상가 Renderer AABB 교차0. 이는 해당 표본점의 새 상가 차폐를 배제하는 한정 근거이며 모든 픽셀·모든 시점이나 기존 HUD 차폐가 없다는 증명은 아니다. 독립 PNG 검토는 별도 review artifact에 남겼다.

B1 CH05 중년 남성은 기존 Meshy 캐릭터의 자기 rig/Animator를 사용한다. 3인 줄의2.2m 공통 이동, 호출 중·이동 중 pause, 선두 사망 후 후속 호출, native 조우 재활성화 후 원위치·체력·단일 Animator·호출 초기화를 검사했다. 과거 CH09 몸체는 보관 상태로 유지했다.

팝콘은 발사 입장 거리23m,1.1초 active-time 예고, 좌/중/우12도 간격3발,11m/s,4초 active-time 수명이다. 조건 없는 이론상 최대 비행은44m이며23m와 다른 값이다. 표시는 잠근 목표점과 3갈래 문구이고 전체 부채꼴 바닥 면적을 칠하지 않는다. 실제 손 발사점·탄 mesh/collider·pause·사망 후 이미 발사된 탄의 잔존과 정리·재활성화·거리 밖/안·리프트/StopRun 정리를 확인했다. 이 경로는 Instantiate/Destroy이며 객체 풀이라고 주장하지 않는다. 관찰별 위치·남은 수명·HP와 소실 시점은 projectile-lifecycle-summary/frames.jsonl에 기록했다. 장시간 누수 없음이나 폰 성능 검증으로 확장하지 않는다.

## 모델 장부와 보존

기존 accepted model23종 = local TRELLIS.2 환경/소품12 + Meshy 캐릭터10 + Meshy 수건1. cycle17 신규 생성/재생성/유료 요청0. cycle16의 팝콘 carton은 실제 local TRELLIS 생성1종이고, native 커널·인쇄 panel은 후처리이다. 이번 회귀에서는 같은 자산을 재사용했다. 원본·영수증·결과·현재 씬 참조를 hash로 검증했다. B02 tote46참조는 현재 배경30 + 등록된 손 소품6 + 보관10이며, 비활성 actor 자식6개를 과거 장부처럼 보관으로 잘못 계산하지 않는다. 캐릭터는 현재305 + 보관 CH09 1 =306참조/10종이다. 지정23종의 실제 기획 누락은 없고 반복 상가·군중의 외형 다양화는 선택적 개선이다.

기준 파일376개 중 BulletScript만 변경, 나머지375개·공유 자산208개·이전 복구 사본210개 보존. 관찰 PlayerPrefs78개, native 비세션 값41개, userdata·보호된 앱 파일 보존. Unity 엔진 세션 키를 쓰지 않았다. 기존 삭제127개와 branch/HEAD를 보존하고 신규 삭제0. 동일 Editor PID83488, 원래 scene/start selector/locale/idle 상태 복원. 화면13필드 중9개는 동일하며 GameView 창 위치·전면 창·전면 PID·커서4개가 달라졌다. 다른 프로세스가 전면에 있으므로 현재 데스크톱을 보존하고 창·커서를 강제 복원하지 않았다. 이를13필드 완전 복원으로 주장하지 않는다. 현재 PT_ResourcesCleanup 값은 전후 확인했으며, 오래전 전체 suite 이전 값이 기록되지 않았던 역사적 한계는 해소된 것으로 주장하지 않는다.

## 남은 범위

넓고 평평한 거리·매장 표면, 반복 외형, 근접 미술 완성도는 남는다. 전체 레퍼런스 미술 완성, 인간의 재미, 모든 짧은 입력 경계, 모바일 성능을 새로 통과했다고 하지 않는다. 과거1135개 전체 검사와52개 기존 실패는 이번에 재실행·해결하지 않았다. 이번 결과는 최종 cycle16 콘텐츠 영향 회귀와 재현된 패널 결함 수정이다.
