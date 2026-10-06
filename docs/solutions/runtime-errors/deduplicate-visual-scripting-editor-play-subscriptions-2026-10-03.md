---
module: Editor Play lifecycle
problem_type: runtime_error
tags: [unity, visual-scripting, domain-reload, events, editor]
---
# No-domain-reload Play에서 Visual Scripting 구독 누적

Unity6000.2.6f1 / Visual Scripting1.9.8에서 Editor Play 재진입마다 ReferenceCollector의 동일 sceneUnloaded 콜백14→15→16을 실측했다. 같은 Play 세션의 장면 전환은 늘리지 않았다. RuntimeVSUsageUtility의 BeforeSceneLoad 호출이 Initialize를 반복하며, Initialize에는 중복 방어가 없다.

프로젝트의 Editor/VisualScriptingPlayModeCleanup.cs는 도메인 재로드 비활성화 상태의 EnteredPlayMode에서 알려진 ReferenceCollector Initialize callback만 비교한다. 동일 delegate(함수와 대상)의 추가 구독만 공개 sceneUnloaded -= 연산으로 제거한다. 다른 패키지 구독·프로젝트 설정·패키지캐시·Player 코드는 변경하지 않는다.

event 목록을 얻는 공식 조회 API가 없어 Unity의 backing field를 읽는다. 필드가 없으면 아무것도 제거하지 않고 경고한다. 패키지의 컴파일된 callback 구조가 달라지면 매칭되지 않을 수 있다. Unity/패키지 업그레이드 시 tools/verify-editor-reentry-cycle05.cs로 실제 Play3회 및 Replay6회의 단일 알림 전달과 다른 구독 보존을 재검증한다.

보완 후 구독1→1→1, 실제 장면 해제 전달 각1회,30/30검증. 기존265게임/장면/controller 및208공유 자산 hash 동일. 근거: docs/reviews/chapter45-stability-cycle05-2026-10-03.md 및 outputs/chapter45-detailed-design-2026-10-03/stability-cycle05/editor-reentry-summary.json.
