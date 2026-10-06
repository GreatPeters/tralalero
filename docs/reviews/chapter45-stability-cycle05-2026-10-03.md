# Ch4·Ch5 반복 안정성 검증5차 — 2026-10-03

41분의 동일 Unity Play 세션에서 8경로를 모두 완주했다. 사망·Replay·재진입, 평지/리프트/영화관 일시정지, 첫/재완주 보상과 중복 지급 방지를 포함해 총193개 항목이 통과했다. 193회 완주라는 뜻은 아니다. 게임 코드·아트·밸런스는 유지했다. 별도로 재현된 Editor Play 재진입의 Visual Scripting 중복 구독을 Editor 전용 코드1개로 보완했다.

[검증 그래프와 원본 Unity 화면15장](http://127.0.0.1:1626/stability-cycle05-20261003/) · [PNG 그래프](../../outputs/chapter45-detailed-design-2026-10-03/stability-cycle05/editor-memory-and-object-counts.png) · [전체 증거 폴더](../../outputs/chapter45-detailed-design-2026-10-03/stability-cycle05/).

## 일반 경로 반복

Unity6000.2.6f1, 기존 Pipeline0.6.0-exp.1, 기본60HP/공격8, 일반1배속. 350ms 반응 지연과 제한된 실제 이동 입력을 사용했다. 강제 승리/순간이동/능력치 변경/적 비활성화 없음. Ch4 완료 후 영상과 자동 Ch5 진입을 사용했고, Ch5 완료 후 다음 Ch4 선택만 QA가 SceneManager로 수행했다. 강제 GC/UnloadUnusedAssets 없음.

| 순서 | 경로 | 게임 초 | 종료HP | 보석 증가 | 리프트 |
|---|---|---:|---:|---:|---:|
| 1 | 거리 왼쪽 | 302.90 | 58 | 35 | 0 |
| 2 | 백화점 B1 | 271.62 | 48 | 40 | 5 |
| 3 | 거리 오른쪽 | 299.15 | 22 | 5 | 0 |
| 4 | 백화점 영화관 | 297.20 | 27.2 | 5 | 6 |
| 5 | 거리 왼쪽 | 302.92 | 56 | 5 | 0 |
| 6 | 백화점 B1 | 283.44 | 45 | 5 | 5 |
| 7 | 거리 오른쪽 | 300.40 | 26 | 5 | 0 |
| 8 | 백화점 영화관 | 312.75 | 25.2 | 5 | 6 |

- 동일 Play 세션 2,455.18초, 8회 완주, 84/84검증, 런타임 오류/Animator 경고0. 모든 로비에서 기존 완료/리프트/생존 시계 초기화, 활성 이전 탄환0, 활성 적0.
- 평지8회, 실제 리프트4회, 영화관 생존2회에서 일시정지/재개. 관찰 구간의 시계와 플레이어·전투 객체 위치 변화0.
- 첫 Ch4 35보석, 첫 Ch5 40보석, 이후 각5보석. 실제 저장 플래그와 현재 환경표의 지급액을 비교했다. 완료 콜백2회와 승리 UI콜백을 추가 호출해도 지갑이 더 증가하지 않았다.
- [일반 경로 요약](../../outputs/chapter45-detailed-design-2026-10-03/stability-cycle05/soak-analysis.json), [개별84항목](../../outputs/chapter45-detailed-design-2026-10-03/stability-cycle05/soak-211050/suite-summary.json).

## 사망과 재시작

- 영화관4회: 초기 위치·앞선 전투 완료 상태·첫HP2·무기OFF를 지정한 재현 fixture. 실제 관객 충돌로 사망했으며 직접 피해/접촉 함수 호출 없음. 63/63통과. 미완료 생존 종료, 관객 정리, 위치 고정 해제, 카메라/FOV 복원, 무보상, 실제 Replay, 새 생존 시계·관객 접촉 상태, 일시정지·제자리 조준을 검증했다. 반복 사이에는 진행 중 생존에서 실제 Replay를3회 더 호출했다.
- 거리2회: 초기 위치와 앞선 전투 상태만 지정, 실제60HP와 보호막에서 맨홀 충돌로 사망. 16/16통과. 홀 접촉1회, 완료 보상 없음, 실제 Replay 후60HP/선택/목표/시계/탄환 초기화.
- 영화관 사망 화면 객체35,743→35,743, 재진입35,747→35,747. 버튼 런타임 구독은 각각8→8,16→16. 거리 사망 화면53,553→53,553, Replay 로비53,549→53,549; 버튼 구독8→8/4→4. 비교한 경로별 UI 구독 목록에 차이 없음.
- [재시작 세부 비교](../../outputs/chapter45-detailed-design-2026-10-03/stability-cycle05/lifecycle-analysis.json). 이 fixture들은 일반 경로 완주와 구분한다.

## 자원과 프레임 관측

| 항목 | 처음 | 마지막 같은 장면 | 해석 |
|---|---:|---:|---|
| Ch4 로비 객체 | 53,549 | 53,550 | 첫 완주 후 DOTween 관리자1개, 이후 일정 |
| Ch5 로비 객체 | 35,702 | 35,702 | 일정 |
| 관찰 대상 이벤트 | 22 | 22 | 장면 반복 내 동일, 파괴된 Unity 대상0 |
| 비자산 재질 | 7,950 | 7,953 | 초기 증가 후 마지막4로비 일정 |
| Ch4 Unity 할당 MiB | 5,055.4 | 4,713.5 | 초기 해제 후 같은 장면 범위 일정 |
| Ch5 Unity 할당 MiB | 4,729.4 | 4,729.4 | 일정 |

전체 장기 Editor 프로세스의 private 메모리18.035→17.980GiB, working set11.850→11.881GiB. Win32 읽기 전용 표본232개, Unity 표본245개. Mono Process 메모리 API가0을 반환해 해당 필드는 지원하지 않는 값으로 표시하고 사용하지 않았다. OS 계측은 첫 경로 중간에 시작했다.

영화관 fixture의 재질은 첫 fresh/death/reentry 7,967/7,968/7,969에서 이후7,969/7,970/7,970으로 유지됐다. 새 재질 이름은 TMP 글자 재질 인스턴스였고, 생존 조준 Unlit 재질은 다음 장면에서 기존 ID가 사라지고 새 ID로 교체됐다. 이 제한된 반복에서 지속 증가를 확인하지 못했으며 임의 길이의 무누수를 증명하지 않는다. 기존 Editor 폰트 캐시 등도 전체 수에 포함된다.

| 경로 | 첫 실행 p50 / p95 ms | 두 번째 p50 / p95 ms |
|---|---:|---:|
| Jamsil / 분기0 | 17.21 / 33.86 | 17.43 / 35.64 |
| ShoeTower / 분기0 | 16.80 / 24.40 | 17.21 / 25.23 |
| Jamsil / 분기1 | 17.35 / 34.98 | 17.44 / 35.67 |
| ShoeTower / 분기1 | 17.09 / 23.75 | 17.15 / 25.92 |

두 번째 p95는 같은 경로 대비 약0.69~2.17ms 높았다. 검사 도구·Editor 전체 부하가 섞인 간격 표본이며 전체 프레임 프로파일이 아니다. 폰/빌드 성능이나 원인별 회귀로 해석하지 않았다. 추측에 의한 최적화는 추가하지 않았다.

## 실제 수정: Editor 중복 구독

장기 세션 내 게임 이벤트는 일정했으나 Play를 종료·재진입하면 `Unity.VisualScripting.ReferenceCollector.<Initialize>`의 동일 구독이14→15→16개로 증가했다. 설치된Visual Scripting1.9.8의 `RuntimeVSUsageUtility`는 Play마다 `ReferenceCollector.Initialize()`를 호출하고, Initialize는 기존 해제 없이 sceneUnloaded에 추가한다. 프로젝트의 도메인 재로드 비활성화 설정에서 재현됐다.

- 추가: `Assets/ShooterSurvival/Editor/VisualScriptingPlayModeCleanup.cs`와 Unity 생성meta. `EnteredPlayMode`에서 도메인 재로드가 꺼져 있을 때만 작동한다.
- 특정 ReferenceCollector Initialize 함수와 동일한 대상의 중복 delegate만 공개 이벤트 제거 API로 해제하고 한 개를 남긴다. 다른 함수/다른 대상은 보존한다. 패키지캐시/EditorSettings/게임 코드 수정 없음.
- 보완 후 Play3회 진입, 실제 Replay6회에서 각10항목, 합30/30통과. 매 진입 콜백1개, 장면 해제마다 원래 신호1회와 Visual Scripting 전달1회, 다른 sceneUnloaded 구독 목록 동일, 정상60HP 로비 복귀.
- [재진입 증거](../../outputs/chapter45-detailed-design-2026-10-03/stability-cycle05/editor-reentry-summary.json), [변경 hash](../../outputs/chapter45-detailed-design-2026-10-03/stability-cycle05/changes.json).
- 유지보수 제약: Unity의 비공개 이벤트 backing field를 읽어 구독을 열거한다. 필드가 달라지면 변경을 하지 않고 경고한다. 패키지의 callback 형태가 바뀌면 이 좁은 매칭은 작동하지 않을 수 있으므로 Unity/Visual Scripting 업그레이드 때 해당30항목을 재검증해야 한다. Player 빌드에는 포함되지 않는다.

## 보존과 범위

기준선265개 코드/장면/controller 파일 hash 동일. 공유 자산208개, 승인된 내부 복구본210개, 보호 대상 사용자 앱 파일6개 모두 동일. 기존 산출물 누락0. 기존 추적 삭제127개 그대로, 새 삭제0. branch `qa/chapters45-full-suite-20261002`, HEAD `295a18cab0ad4156baa9106077bc10a47ac6d734` 유지. 소스 컴파일 전에 현재 장면2개를 내부 복구 경로에 별도 보관했다.

6개 검사 세션의 저장/Editor 환경 복원 불일치0. 최종 Unity는 ShoeTower, idle, dirty=false, timeScale1, captureDeltaTime0. 이번 시작의 PT_ResourcesCleanup(Editor 존재/false, Player 없음)을 그대로 유지했다. 2026-10-02 이전 역사값 미기록 한계는 바뀌지 않는다. [보존 검증](../../outputs/chapter45-detailed-design-2026-10-03/stability-cycle05/integrity.json).

이번 주기 신규 생성 모델/이미지/유료 요청/자산 재배치 없음. 이전 TRELLIS·Meshy 및 촬영 자세 개선은 유지했다. 폰·APK·전체1135검사·설치·새 인증·추가 지출·commit/push·외부 백업·기존 파일 삭제 없음. 예전1083통과/52실패를 현재 결과로 재사용하지 않았다.

실행 진입점은 `tools/chapters45-stability-cycle05.cs`, `tools/verify-cinema-replay-cycle05.cs`, `tools/verify-street-replay-cycle05.cs`, `tools/verify-editor-reentry-cycle05.cs`. 공식 Unity CLI run_script로만 호출했다. Prepare→Play→검사→Stop→Restore를 직렬 실행하며 저장 snapshot을 먼저 보존해야 한다. 실행 중 두 번째 Unity 소유자를 만들지 않는다.

이번 범위의 남은 차단 없음. 다음 유효 검증 대상은 Ch4→Ch5 전환 직전/리프트/영화관에서 저장 후 Unity 종료·재실행하여 이어하기와 보상 플래그의 영속성을 확인하는 경계다. 이번 장기 검사는 같은 프로세스의 반복이므로 그 결과까지 대체하지 않는다. 폰/APK는 계속 보류한다.
