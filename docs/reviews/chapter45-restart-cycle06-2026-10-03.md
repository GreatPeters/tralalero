# Ch4·Ch5 정상 종료·재실행과 저장 경계 검증 — cycle06

검증 완료 UTC: 2026-10-03T23:18:31.106345+00:00. [원본 화면 갤러리](http://127.0.0.1:1626/restart-cycle06-20261003-verified/), [기계 판독 결과](../../outputs/chapter45-detailed-design-2026-10-03/restart-cycle06/summary.json), [복원·무결성](../../outputs/chapter45-detailed-design-2026-10-03/restart-cycle06/integrity.json).

10개 경계에서 135개 검사를 통과했고 최종 검사 실패 0개, 경계 수집 구간에 포착된 게임 오류 0개다. 초기 정상 재실행1회, 경계별10회, 저장 복원 확인2회를 합쳐 정상 종료 영수증 13개를 남겼다. 각 경계의 전후 native Unity PID가 다르다. 새로 재현된 게임 버그가 없어 게임 코드·씬·아트·밸런스를 수정하지 않았다. 검사 스크립트와 결과 기록만 추가했다.

## 저장 계약과 중요한 한계

지갑, 챕터 해금/완료 보상 플래그, 영구 강화, 코스메틱, 설정을 포함한 관찰 대상78항목은 매번 존재 여부와 정의된 타입의 PlayerPrefs getter로 읽은 값이 일치했다. 각 키의 저장소 타입을 매번 별도로 추론한 검사는 아니다. 실제 게임의 MoneyScript/ChapterProgression 저장 경로를 사용했으며 경계 도달→종료 구간에 수동 PlayerPrefs.Save를 호출하지 않았다. 미완료 시점에는 완료 보상을 기록하지 않았고, 완료 직후 중복 콜백을 보내도 추가 지급되지 않았다. 최초 Ch5 완료와 새 프로세스에서 재도전한 완료의 서로 다른 보상 금액도 확인했다.

현재 중간 체크포인트 기능은 없다. Chapter45Director의 위치/층/리프트, 선택값, RestStopHoldout의 시간/카운터는 런타임 상태이고, CanvasScript.LoadGame은 현재 씬을 새로 로드한다. 새 프로세스에서 QA가 같은 챕터를 명시적으로 선택했으며 새 시도의 거리0·선택 초기화·생존 타이머0·정상 체력·발사체0을 확인했다. 이를 위치 기반 ‘이어하기 통과’로 표현하지 않는다. 흰 운동화는 이야기상 물리 목표와 챕터5 완료로 이어지며 별도 코스메틱 인벤토리 지급으로 해석하지 않았다.

| 경계 | 통과 / 실패 | native PID 전후 | 코인 / 보석 변화 |
|---|---:|---|---:|
| Ch4 왼쪽 분기 보상 | 14 / 0 | 59008 → 92076 | 0 / 0 |
| Ch4 오른쪽 분기 보상 | 14 / 0 | 92076 → 69052 | 120 / 0 |
| Ch4 백화점 입구 완료 | 14 / 0 | 69052 → 34872 | 0 / 35 |
| Ch5 에스컬레이터 이동 중 | 13 / 0 | 34872 → 68468 | 0 / 0 |
| Ch5 B1 도착 | 13 / 0 | 68468 → 21360 | 0 / 0 |
| Ch5 영화관 생존 중 | 13 / 0 | 21360 → 19780 | 0 / 0 |
| Ch5 영화관 생존 완료 | 13 / 0 | 19780 → 70816 | 0 / 0 |
| 옥상 흰 운동화 접촉 전 | 13 / 0 | 70816 → 36952 | 0 / 0 |
| 흰 운동화 최초 완료 보상 | 14 / 0 | 36952 → 69020 | 0 / 40 |
| 흰 운동화 재도전 완료 보상 | 14 / 0 | 69020 → 84524 | 0 / 5 |

## 검사 방식

앞선 전투 완료와 초기 위치를 주입한 directed fixture다. 선택·리프트 이동·30초 생존 완료·실제 목표 접촉은 native game logic으로 진행했다. 무기를 끄고 영화관 완료 사례만 체력300을 설정했다. 강제 승리/목표 수집/수동 플레이어 피해는 없다. 경계 확인 후 timeScale0으로 잠시 고정하고 File/Exit로 정상 종료했다. 이 검사는 일반 동선·밸런스·군중 밀도·시각 개선 평가를 대체하지 않는다. 해당 평가는 이전 cycle01–05 기록에 분리돼 있다. 휴대폰 성능이나 Player 빌드 검증도 아니다.

원래 사용자의 입력이 없고 씬이 깨끗한지 확인한 뒤 진행했다. 종료 직전과 재실행 구성 시 새 입력 여부를 다시 검사했다. 미저장 씬이나 프리팹을 저장/폐기하지 않았고 Unity 강제 종료도 사용하지 않았다. 기존 서비스와 다른 프로세스는 그대로 뒀다.

## 검사 도구 문제와 보존된 실패

- 첫 실행 래퍼는 Windows 자식 프로세스의 상속 PIPE 때문에 이미 성공한 Editor 실행 후에도 대기했다. 공식 Pipeline과 새 native PID로 기존 인스턴스를 확인했고 중복 실행하지 않았다. 다음 실행부터 출력 파일과 준비 상태 확인을 사용했다. 이전 래퍼는 해당 Editor의 다음 정상 종료 후 TimeoutExpired로 반환했다.
- 재실행 초기에 Pipeline이 도달 가능해도 main thread가 아직 초기화 중이면503 Server Busy가 반환됐다. 이 정확한 일시 응답만 재시도한다. 새 Editor를 다시 띄우지 않는다.
- 리프트 첫 fixture가 afterSegment0을 가정해 시작 단계에서 실패했다. 실제 첫 에스컬레이터는59번 구간 뒤였다. 실패 산출물을 유지하고 실제 authored lift를 조회하도록 도구만 수정했다. `lift-mid-attempt02`에서 통과했다. 이 실패를 게임 버그로 계산하거나 지우지 않았다.
- 운동화 접촉 전 첫 fixture는 마지막 짧은 구간 안에서만 목표를 검색해 실제 목표 내부에 배치됐고, 접촉 전 조건이 실패했다. 해당 층 전체 경로를 검색하도록 도구를 고쳤다. 실패 산출물을 유지하고, 그 검사에서 native 코드가 지급한 `jewel`/`chapter_rewarded_5` 두 항목만 직전 통과 경계의 값으로 되돌렸다. 현재78항목이 실패 당시와 일치하는지 먼저 확인하고 복원 후78항목을 다시 확인했다. 이 준비 복원에는 명시적 PlayerPrefs.Save를 사용했으며, 재실행 보존을 주장하는 실제 경계 도달→정상 종료 구간에는 수동 Save가 없다. `shoe-before-attempt02`와 이후 최초/재도전 보상 검사에 이 구분을 적용한다.
- Unity AI Toolkit의 기존 계정 서비스 요청 오류(PointsBalanceResult/SettingsResult, generators-beta.ai.unity.com)가 Editor 시작 시 관찰됐다. 게임 오류와 분리했고 인증·패키지·설정을 바꾸지 않았다. 전체 Editor 콘솔 오류가0이라는 주장은 하지 않는다.

## 원상 복원과 변경 범위

**복원 검사의 저장소 가정 오류를 발견해 보완했다.** 셸의 게임 전용 HKCU48항목은 복원·재실행 후 hash가 원본과 같았지만, native Unity PlayerPrefs에는 테스트 값5개가 남았다. Unity 내부 Microsoft.Win32.Registry 읽기와 외부 셸 읽기는 동일한 경로 문자열에 서로 다른 값을 반환했다. 셸 스냅샷은 코인1781/보석0, 최초 native API 관찰은 코인33031/보석30이었다. 따라서 셸의48개 일치만으로 게임 저장이 복원됐다고 판단한 초기 가정을 폐기했다. 공식 문서는 Windows Editor의 기본 경로를 설명하지만, 이 환경에서 두 실행 문맥이 같다는 증거는 아니다. [Unity6.2 PlayerPrefs 문서](https://docs.unity3d.com/6000.2/Documentation/ScriptReference/PlayerPrefs.html).

실제 native PlayerPrefs API에서 현재값이 진단 당시와 동일함을 확인한 뒤 테스트로 바뀐 `coin`, `jewel`, `chapter_unlocked`, `chapter_rewarded_4`, `chapter_rewarded_5`만 처음 기록한 값/존재 여부로 복원했다. 정상 종료·재실행을 한 번 더 수행했고 새로운 PID에서78항목이 원본과 일치했다. 원래 코인33031·보석30·해금2·Ch4/5 보상키 없음이다. 실제 native registry는 수정 전47개→수정 후45개이며, 새 보상키2개 제거·기존 값3개 복원 외 나머지42항목은 복원 전후와 추가 재실행에서 정확히 같았다. [native 복원 범위](../../outputs/chapter45-detailed-design-2026-10-03/restart-cycle06/native-registry-restoration-scope.json).

한계: 전체 native registry의 테스트 전 사본은 없고, 테스트 전 native PlayerPrefs 관찰78항목만 있다. 따라서 미관찰 native 키가 모든 플레이 이전과 같은지는 증명하지 못한다. 셸48항목의 정확한 사본과 hash는 별도 실행 문맥의 보존 증거로만 남긴다. 처음부터 native API 또는 동일 프로세스 registry를 기준으로 캡처해야 한다.

Editor SessionState9개, 원래 PlayModeStartScene, 활성 ShoeTower 씬, timeScale1/captureDelta0도 복원했다. 최종 Editor는 idle·dirty=false다. 현재 PT_ResourcesCleanup은 Editor 존재/false, Player 없음 그대로이며 2026-10-02 이전 미기록 값은 복구했다고 주장하지 않는다.

기준 파일272개·공유 자산208개·이전 복구 사본210개·이번 최소 복구 사본7개·보호된 사용자 앱 파일6개·persistentData의 기존 파일1개를 검증했다. 기존 tracked 삭제127개, 새 삭제0개이며 branch/HEAD가 같다. 새 모델/이미지 생성·아트 재배치는 없었다. 기존 TRELLIS 환경/ Meshy 캐릭터 적용은 그대로 보존했다. APK·폰 검사·전체1135검사·설치·새 인증·추가 결제·commit/push·외부 백업·기존 파일 삭제를 하지 않았다. 과거1083/52는 이번 통과 수치에 포함하지 않는다.

재현 도구는 `tools/restart-preflight-cycle06.cs`, `tools/restart-state-cycle06.cs`, `tools/restart-boundary-cycle06.cs`이며 공식 Unity CLI로 직렬 실행했다. 최초 캡처 파일은 덮어쓰지 않는다. 실패 후에는 이미 생성된 native Editor와 원래 snapshot을 먼저 확인한다.

보고서 생성기의 최초 불완전 초안은 `restart-cycle06/publish-attempt01/`에 보존했다. 최종 산출물은10개 사례·20개 원본 이미지·135개 검사를 확인한 뒤 발행했다. Native 복원과 저장소 문맥 진단은 `tools/restore-native-prefs-cycle06.cs`, `tools/native-registry-cycle06.cs`에 기록돼 있다.
