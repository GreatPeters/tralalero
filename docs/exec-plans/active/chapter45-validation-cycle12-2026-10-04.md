# TV 경계·최종 저장본 경로 검증 — cycle12

최신 위임에 따라 TV의 실제 피격 경계 안/밖, 착지와 경고 종료 및 회피를 native 실행으로 확인한다. 재현된 표시 문제만 수정하며 골프채/카트의 미재현 후보는 바꾸지 않는다. 이후 cycle11 잡기·차량·경고 높이를 포함한 최종 저장본으로 Ch4 좌우와 Ch5 B1/영화관 네 경로, 관련 사망/Replay/일시정지/보상을 검증한다.

기준306제품파일, native 게임 저장78관측키와 허용6키의 존재/타입/값, Editor 시험설정9키, native registry 읽기 기준을 새로 기록했다. 제품과 공유자산 변경 전 최소 PC 내부 복구 사본을 만든다. 시험으로 바뀐 허용 게임키만 native PlayerPrefs로 복원하고 전체 관측값을 비교한다. 엔진session은 읽기 비교만 하며 복원 쓰기하지 않는다.

TV 경계 위치 지정·주변 조우 분리 등 fixture는 일반 경로와 구분한다. 실제 Damage/Hit/Contact/Tick을 수동 호출하지 않는다. 네 일반 경로는 기존350ms지연·제한PlayerMove 입력,60HP/8ATK/1배속을 유지한다. 새모델/유료호출/폰/APK/인증/설치/commit/push/외부백업/기존삭제/전체테스트 금지. 이전 cycle의 수치는 이번 통과로 재사용하지 않는다.


## 완료 — 2026-10-04 cycle12

TV 실제 접촉 경계의 기존 타원 누락과 차량 피해 안내를 수정했다. 제품 변경은 ShoeTower 씬/PlayerDamageCause 2파일이며 피해·시간·충돌체는 보존했다. TV27창, 최종4경로42검사, TV 사망·Replay12검사, 영화관 사망·Replay15검사를 완료했다. 최종 원본 패배 UI를 별도 캡처하고 독립 검토했다.

관측78저장값·공유208자산 동일, 엔진 키 복원 쓰기0, QA callback/경고 mesh0, 동일 Editor 복원. 골프채·카트 미재현 후보 그대로. 새 모델/유료/폰/APK/commit/push/기존파일삭제0. 이전 요구사항60행과 모든 필드를 보존하고 관련 행에 cycle12 근거만 추가했다.

상세: [cycle12 보고서](../../reviews/chapter45-validation-cycle12-2026-10-04.md), [최종 분석](../../../outputs/chapter45-detailed-design-2026-10-03/validation-cycle12/tv-final-verification.json), [전후 원본 화면](http://127.0.0.1:1626/validation-cycle12-20261004/).
