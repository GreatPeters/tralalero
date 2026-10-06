# 챕터 4·5 언어와 UI 가독성 검증 — cycle07

2026-10-03 시작, 2026-10-04 완료. [Unity 원본 비교 갤러리](http://127.0.0.1:1626/ui-cycle07-20261004/) · 원본과 수치: `outputs/chapter45-detailed-design-2026-10-03/ui-cycle07/`.

## 결과

기존 `en-US`를 선택해도 챕터 안내·선택 설명·운동화 획득 화면이 한국어로 남던 문제를 수정했다. 기본 영어 15개 사례의 누락은 15→0이며, 한국어 15개 사례의 텍스트와 측정값은 전후 일치한다. 추가로 회복 보상 문장 `HP 20% Heal`을 `Restored 20% HP`로 다듬었다. 보상 수치·지급 로직·층 이동·저장 동작은 변경하지 않았다.

| 검사 | 결과와 범위 |
|---|---|
| 기본 동일 표시 상태 전후 | 30개 상태 × 전후 = 60장. Jamsil 선택·보상·위험, ShoeTower 선택·학생·위험·8개 층·운동화 획득, 각 한·영 |
| 회복 보상 문장 전후 | 한·영 4장. 한국어 보존, 영어 문장 수정 후 재검증 |
| 추가 대기줄·영화관 | 각 한·영 4장. 영화관 카메라 전환을 검증용 0.8초로 맞추고 생성 인원 0 확인 |
| 글자 영역·폰트 | 선택한 68개 원본의 대상 HUD/완료 제목·본문에서 glyph/rect overflow, TMP overflow, missing glyph 0 |
| 옛 층명 잔존 | 캡처 시 활성 TMP의 58F/118F 표기 0. 비활성 보존 루트는 삭제하지 않음 |
| 작성된 경고·층 안내 | Jamsil 11개 + ShoeTower 17개 = 28개, 영어 변환 후 한국어 잔존 0 |
| 컴파일 | Unity `recompile_status`: completed, failed=false, errors=[] |

제목/본문과 Return 버튼 대표 화면을 직접 확인했고 레이아웃 변경이 필요한 재현 결함은 없었다. 자동 측정은 명시한 텍스트 영역에 한하며, 모든 UI·해상도·장식 표지의 겹침을 전수 판정했다는 뜻은 아니다. Return 버튼은 시각 확인이며 본문 자동 측정 대상과 구분한다.

## 실제 적용

기존 소스 4개를 수정하고 챕터 전용 표시 클래스 1개와 Unity 생성 `.meta`를 추가했다.

- `Chapter45Director.cs`: 안내를 기존 선택 언어로 변환한 뒤 기존 표시/크기 계산에 전달.
- `Chapter45Choice.cs`: 경로 설명과 미선택 프리뷰의 언어 변경 반영. 선택/보상 계산 보존.
- `Chapter45CompletionPresentation.cs`: 기존 ShoeTower 공물 획득 조건 안에서 제목·본문·Return만 변환; 닫을 때 원문 복원.
- `RestStopHoldout.cs`: Ch5 영화관 생존·처치·조작 문구 변환. Ch3 분기 보존.
- `Chapter45PresentationText.cs`: 이미 등록된 `ko-KR`/`en-US`에 맞춘 챕터 문구와 동적 숫자 조각 변환.

기존 공유 Localization 자산·폰트·씬·프리팹·모델·배치·전투·밸런스는 그대로다. 이번 주기에 새 환경/캐릭터 생성, 자산 재적용, 재사용 모델 추가는 없다. 씬이나 레이아웃도 바꾸지 않았다. baseline 대비 실제 코드 차이는 `source-diff.patch`, 추가 파일과 해시는 `changes.json`/`integrity.json`에 있다.

## 저장과 작업 상태 보존

테스트 전 Unity 프로세스 안에서 실제 HKCU native registry 45개를 타입 포함 저장했다. 관측 PlayerPrefs 78개는 최종값이 전부 동일하고, native registry의 엔진 세션 키 4개를 제외한 41개도 타입/값이 일치한다. 별도 Standalone 키는 계속 존재하지 않는다. 코인 33031, 보석 30도 유지됐다.

초기 QA 정리에서 엔진 세션 메타데이터를 두 번 과거값으로 되감은 실수가 있었다. 두 receipt(`preprobe-session-metadata-restoration.json`, `before-v3-Jamsil-session-metadata-restoration.json`)는 보존했다. 이후 `docs/solutions/workflow-issues/verify-unity-cosmetic-previews-and-test-prefs-2026-09-10.md`의 “Do not rewind those engine identities merely to force a zero-difference registry report” 지침을 재확인하고 중단했다. 후속 Play가 만든 현재 세션 ID/카운터는 유지했다. 최종적으로 달라진 엔진 키는 `unity.player_session_count`, `unity.player_sessionid`, `unity_connect.session_id`이며, 비교 제외 키 4개 중 `unity_connect.mega_session_id`는 원본과 같다. **전체 45개가 무변경이었다고 주장하지 않는다.** 게임 저장값 보존과 엔진 세션 메타데이터를 별도로 보고한다.

임시 SelectedLocale은 ko-KR, SessionState 9개·시작 씬·실제 활성 씬·timeScale/captureDeltaTime은 원래 상태로 복원했다. `PT_ResourcesCleanup`은 이번 주기 전 캡처한 현재값으로 확인했으며 과거 미기록 값은 추정하지 않았다. 같은 Editor PID 83488, clean idle ShoeTower, timeScale=1/captureDeltaTime=0이다. 이번 주기에 Editor 재시작은 없었다.

원본 비교 304개 중 승인된 소스 4개만 수정했고 나머지 300개는 일치한다. 공유 자산 208개, 이전 복구 사본 210개, 이번 최소 복구 사본 277개, 보호된 사용자 앱 파일 6개의 해시가 일치한다. persistent-data `TestResults.xml`은 cycle06 기준과 일치하며 cycle07에서 새 파일 기준을 잡았다는 뜻은 아니다. 기존 tracked deletion 127개가 유지됐고 추가 삭제 0, branch/HEAD도 그대로다.

## 검증의 한계와 남은 범위

이 주기는 실제 Unity 렌더링/컴포넌트의 **UI 표시 검증**이다. 타이머·위치·공물 표시 latch를 제어했으며 실제 보상 지급/챕터 완료/저장 API는 호출하지 않았다. 영화관 선택은 보상이 없는 기존 FloorRoute만 설정했다. 새게임·이어하기·보스·완주 통과 수에 합산하지 않는다. cycle06의 135개 결과를 중간 저장/재개 구현으로 해석하지 않으며, 새 checkpoint 기능도 추가하지 않았다.

프로젝트에는 한국어/영어 Locale이 등록되어 있지만 공용 체력·공격력 HUD와 일부 메뉴는 여전히 한국어 고정이다. 기존 영어 AllTexts는 9개 능력치 항목 수준이며 이번에 게임 전체 영어 지원을 완료한 것은 아니다. 장식용 한·영 병기 상점/층 표지도 유지했다. 언어 선택 UI나 저장형 locale selector를 새로 추가하지 않았다.

추가 검증의 첫 coverage는 Ch4 내부 구간 메타데이터 4개까지 포함해 실패했다. 코드 사용처를 확인해 실제 표시 경고/리프트/Ch5 층명만 검사하도록 바로잡았고 최초 결과를 남겼다. 영화관 첫 추가 PNG는 정지 fixture가 전환 시작점을 유지해 하늘만 보였으므로 UI 측정만 유효하다. 해당 원본은 `extra-v2/*/cinema.png`에 남기고 갤러리는 카메라 전환을 맞춘 `targeted-before/*/cinema.png`를 사용했다. 게임 카메라 코드를 수정한 것이 아니다.

전체 1135 테스트·폰/APK·새 결제/설치/인증·commit/push·외부 백업은 실행하지 않았다. 과거 52개 실패 수치를 현재 통과 결과로 사용하지 않았다. 현재 주기 완료를 막는 도구 차단은 없다.

## 재현 기록

공식 Unity CLI의 연결 발견 후 `run_script`로 `UiStateCycle07.Configure` → Play → `UiProbeCycle07.Begin/SelectLocale/Show/Capture` → locale 복원 → Stop → `UiStateCycle07.RestoreEditor` → `UiNativeRegistryCycle07.VerifyGamePreferences` 순서로 실행했다. 엔드포트 포트는 하드코딩하지 않았다. 최종 증거는 `compile-final.json`, `summary.json`, `integrity.json`, `observed-playerprefs-final.json`, `native-registry-final.json`, `editor-restoration-final.json`, `gallery.json`이다. 원본 registry 내용은 공개 갤러리에 복사하지 않았다.

갤러리는 기존 로컬 서버에서 68개 PNG와 HTML을 HTTP로 다시 읽어 원본 SHA-256 일치까지 확인했다. UI PNG를 생성 이미지로 대체하거나 편집하지 않았다.

마지막 읽기 전용 확인: EditorPrefs `PT_ResourcesCleanup`의 존재/값이 이번 시작 시점과 일치하고 PlayerPrefs에는 해당 키가 없다. 저장한 두 씬의 활성 TMP 인벤토리에서 `(58|118)\s*(F|층)`도0건이다. 최종 갤러리34행/68개 그림, 승인된 소스4개 외 baseline 변화 없음까지 재확인했다 (`post-publish-audit.json`).
