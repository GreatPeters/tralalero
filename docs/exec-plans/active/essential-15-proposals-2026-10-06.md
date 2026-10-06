# 필수 제안 문서 1–15 적용 — 2026-10-06

## 근거와 정정

- 사용자 원문(2026-10-06): "다운로드\트랄랄레오_슈터_필수_제안_문서.docx 이게 지금 잘 적용됐는지 확인해보고, 적용이 안 되었으면 적용해줘. meshyai나 trellis 2.0 을 써도 좋으니까. 아니면 다른 SE 제작하는 내 컴퓨터 D드라이버에 깔려있는 LLM을 써도 좋아."
- 현재 `Downloads/트랄랄레오_슈터_필수_제안_문서.docx`(10:03, 237문단, 사진 3장 포함)는 게임플레이 15개 요구 문서다. 앞선 [essential-proposals-and-play-account](essential-proposals-and-play-account-2026-10-06.md) 실행은 `outputs/essential-proposals-2026-10-06/source-text.txt`에 보존된 **10월 4일 툰 스타일 보고서** 내용을 적용했으며, 이 15개 항목은 점검 시점에 모두 미적용이었다.
- D드라이브 확인: `D:\AI\Models`에 TRELLIS2, ComfyUI(Qwen Image Edit, Wan2.2 영상), Qwen3.8-27B LLM, 배경 제거 모델이 있다. 효과음(SE) 생성 모델은 없다. 새 SE는 만들지 않았고 Meshy/TRELLIS 크레딧도 쓰지 않았다.
- 결정이 필요한 항목은 문서의 "권장 출발점"을 적용했고 아래에 명시했다.

## 항목별 결과

| # | 적용 | 핵심 파일 | 검증 |
|---|---|---|---|
| 1 AI 음성 제거 | 노량진 시장 음성(방송·경매·상인·셔터·갈매기 안내 11클립)의 유일한 재생 경로 `NoryangjinRevampDirector.Speak`와 경매 AudioSource를 차단. 셔터 시작에는 기존 비음성 `warning` SE. 다른 챕터에는 음성 클립 참조 없음(씬/프리팹 GUID 검색). 오프닝 영상에는 오디오 트랙이 없음. | NoryangjinRevampDirector, NoryangjinAuctionActivity, NoryangjinShutterEvent | `MarketVoicesAreDisabled` |
| 2 적 사이 통과 차단 | 같은 발동 스팟의 근접전 적 2명 이상을 한 줄로 보고, 아무도 처치하지 않은 채 줄을 지나치면 레인이 가장 가까운 적과 기존 접촉 HP 교환을 강제. 한 명 처치 시 열림. 사격·매복 적 제외. | EnemyRowGate(신규), EnemyEventActivationSpot, EnemyScript_space.ForceRowContact | `EnemyRowCrossingRules` (실제 우회 플레이는 미검증) |
| 3 민감도 삭제 | 5개 씬 설정 창의 감도 행 비활성, 저장값 무시·삭제, 기본값 1 고정. | SettingsManager, HarborSettingsPanel, CanvasScript, PlayerScript, HarborGameUIInstaller.EssentialSettings | 씬 테스트 5/5, 플레이 캡처 |
| 4 하단 버튼 간격 | 법적 고지 행 간격 0.007→0.018, 로그인/탈퇴 가로 간격 0.025→0.06, 계속하기/다시 도전 간격 0.03→0.08(패널 비율). 터치 영역=표시 영역이라 겹침 없음. | 동일 설치기 | 씬 테스트 5/5, 1080×2340 캡처 |
| 5 앱 전환 | 런 도중 포커스/일시정지 상실 시 설정 창(일시정지)을 열고, 복귀 후 창 유지(권장안). 복귀 직후 눌린 터치는 놓을 때까지 무시. 에디터 포커스 변화는 제외. OS가 프로세스를 종료한 경우의 복구는 미구현. | CanvasScript | `InterruptionPausesOnlyALiveRun` (실기기 미검증) |
| 6 오프닝 앞 정지 이미지 | 영상 전·대체용 `Story_01–04.png`(9/11 구판)를 현재 영상(10/2) 프레임 0 / 20.2 / 24.5 / 30초로 교체(941×1672 유지, GUID 유지). 1장은 영상 첫 프레임과 동일. 구 `Curse_Opening.png`는 pages가 있는 한 표시되지 않음. | Assets/JH/UI/Opening/Animated/Story_0*.png | 시각 확인 |
| 7 로딩·팁 | 지속 오버레이(회전 표시, 가짜 % 없음, 전체 입력 차단, 새 씬이 한 프레임 그려질 때까지 유지). 다시 도전·다음 챕터·계정 복원 로드에 적용. 팁 5개는 코드로 확인되는 규칙만 사용("강한 적 = 코인 더"는 배치 데이터상 보장되지 않아 제외). | LoadingOverlay(신규), ChapterProgression, CanvasScript, PlayAccountService | `LoadingTipsAreVerifiedRules`, 플레이 중 상태 확인 |
| 8 상어 미리보기 | 사진 1 오류 문구 1024×768 대 768×576 = Mobile RP renderScale 0.75. URP 소스 확인 결과 renderScale은 `BeginCameraRendering` 이후 읽히므로 미리보기 카메라만 1로 렌더 후 즉시 복원. | CosmeticPreview | 에디터 렌더·복원 0.75 확인. **Vulkan 실기기 노이즈 해소는 미검증** |
| 9 오토바이 보스 HP 교환 | 충돌 전 스냅샷으로 P′=max(0,P−E), E′=max(0,E−P) 한 번 적용. P=E는 양쪽 0(권장안, 플레이어 사망이므로 클리어 보상 없음). 다른 보스 미적용. | NoryangjinTurretEvent | `TurretCollisionExchangesRemainingHp` 4케이스 |
| 10 X1/X2 | `GameSpeed`가 Time.timeScale 단일 소유: 맵툴 테스트 배율 × 사용자 X1/X2 × 이벤트 감속. 고속도로 1.4는 전진 속도 배율이라 X2에서 2.8. 일시정지는 timeFactor 0. 씬 전환 시 이벤트 감속만 해제, X2 선택은 유지(권장안). 우하단 소형 버튼. | GameSpeed, GameSpeedToggle(신규), HighwayChapter2Controller, NoryangjinMapToolTestSpeed | `GameSpeedComposesMultipliers`, 플레이 중 1→2, 팝업 0.4, 일시정지 후 2 유지 |
| 11 얼음박스 20초 | 셔터 카운트다운 고정 20초(게임 시간 기준, X2면 실제 10초). 박스 벽 HP ×1.5(수치 미정 — 실기기 튜닝 필요). 박스는 높이 순으로 위에서부터 떨어짐. | NoryangjinShutterEvent, NoryangjinBreakable | `ShutterCountdownIsTwentySeconds`, `BoxesShedFromTheTop` |
| 12 오프닝 4단계 | 자막을 ①신성한 신발을 훔쳤다 ②신이 분노했다 ③저주를 받았다 ④더 좋은 신발을 얻어야 한다로 교체, 장면 경계를 영상 실제 컷 0/11/20.5/27초에 맞춤(1fps·4fps 프레임 검토). 영상 편집·신규 자산 없음. | OpeningStoryUI | `OpeningMovieTimingTests` 13/13, 폰트 글리프 확인 |
| 13 진행도 그래픽 | "새벽 시장 · 4:00" 화면 시계 제거. 1–5챕터 노드 + 현재 챕터 진행 바. 진행도는 시간 아님: 노량진 회전 체크포인트, 고속도로 거리/길이, 휴게소 경로 85%+버티기 15%, 4·5챕터 경로 거리. 후퇴 없음, 클리어 전 최대 99%. 최고 기록 = 최고 도달 위치(권장안), 설정 창에 표시, PlayerPrefs 보존(클라우드 동기 키에는 미포함). | ChapterRunProgress, ChapterProgressHud(신규), NoryangjinMarketAtmosphere, HarborSettingsPanel | `ChapterProgressIsMonotonicAndCapped`, 플레이 캡처 |
| 14 인간 텍스처 | **원인 확정**: `NoryangjinClaudeFeedbackBuilder.OutlineCharacters`가 외곽선 머티리얼을 텍스처 *이름*으로 캐시. Meshy/TRELLIS 아틀라스가 모두 `texture_0`이라 N14 여성 상인 몸·Enemy_Woman이 N13 남성 상인 아틀라스를 사용 → 사진 3의 노랑·남색 위장무늬. 노량진 5개 슬롯(러시 템플릿 YellowMan_Sword, Woman 템플릿·보스, 호스 작업자 2)을 자기 폴더 아틀라스 머티리얼로 교체, 빌더를 GUID 키로 수정. 나머지 4개 씬은 불일치 0. | CharacterTextureMismatchRepair(신규), NoryangjinClaudeFeedbackBuilder, 노량진 씬, Models/Generated/CharacterTextureRepair | 같은 조건 전후 렌더, 씬 테스트 5/5 |
| 15 캐릭터 변주 | 씬·계층 경로 시드로 고정 재현되는 속도(0.88–1.12, 대기·이동만), 보행 위상, 크기(±4%), 의상 톤(밝기·온도) 변주. 공격·사망 클립은 원속도 유지, 풀 재사용 시 재적용. 1–3챕터 EnemyEventController 적 대상. 4·5챕터는 기존 phaseOffset만. | ActorVariation(신규), EnemyEventController | `ActorVariationIsStableAndBounded` |

## 검증 기록

- `dotnet build Assembly-CSharp.csproj -nologo`: 오류 0, 경고 2(기존 SplineSpeed, NoryangjinRushEvent).
- `dotnet build Assembly-CSharp-Editor.csproj -nologo`: 오류 0, 경고 0.
- `tools/validate-agent-harness.ps1`: 통과.
- EditMode: EssentialProposalsTests 23/23, OpeningMovieTimingTests 13/13, HarborFeedbackPresentationTests 5/5, PlayAccountServiceTests 13/13, GameProgressSnapshotTests 15/15, EnemyEventControllerTests 24/24. NoryangjinRuntimeCleanupContractTests 14/15 — `canShoot`가 이번 작업 이전의 미커밋 변경에서 프로퍼티가 되어 실패(이번 변경과 무관).
- 플레이 모드(노량진, 1080×2340): 새 HUD·X1/X2·설정 창·꾸미기 미리보기 캡처, X2/팝업/일시정지 값 확인, 다시 도전 로딩 오버레이 표시→해제 확인. 캡처: `tmp/image-previews/essential-15-2026-10-06/`.
- PC 검증과 폰 검증을 분리: 휴대폰이 연결되어 있지 않아(`adb devices` 비어 있음) **실기기 검증 0건**, APK 빌드·설치 없음, 커밋·푸시 없음.

## 남은 일 (사용자/실기기 필요)

1. S22에서 설정→앱 전환→복귀, X1/X2·고속도로 2.8, 보스 충돌, 20초 얼음박스 난이도(HP ×1.5 튜닝), 상어 미리보기 노이즈, 적 줄 우회 시도, 위장무늬 해소를 확인.
2. 사용자 SE 파일 수령 시 음성 자리에 연결(현재는 무음 + 셔터 경고 SE).
3. OS가 백그라운드 프로세스를 종료한 경우의 런 상태 복구는 별도 저장 흐름이 필요(미구현).
4. 5번 앱 전환으로 "게임이 초기화"된 원인은 코드상 재현 경로를 찾지 못했다. 빌드 매니페스트(configChanges, singleTask)는 정상. 가장 유력한 후보는 OS 메모리 회수지만 미확정.
5. 추가 제안 16–24는 이 작업 범위가 아니다.

백업: 수정 전 파일 22개는 `outputs/essential-15-2026-10-06/backup/`.

## 재검증 (2026-10-06 23시대, APK 미적용)

사용자 요청: "전반적으로 다시 한 번 검증해봐. apk는 아직 적용하지말고".

코드 재검토와 플레이 모드 시나리오(리플렉션으로 이벤트 직접 발동, 플레이 종료 시 폐기)에서 발견해 고친 것:

| 발견 | 수정 | 확인 |
|---|---|---|
| 11 박스가 **아래부터** 부서짐(런타임 루프가 i를 Length-1부터 내려가며 방문하는 것을 반대로 해석, 단위 테스트도 같은 오해를 반영) | `shedPieces[shedOrder[i]]`, 테스트를 실제 방문 순서로 수정 | 노량진 BoxWall: 1.83→0.36 순서 |
| 2 노량진 발동 스팟 50개가 **모두 적 1명**이라 스팟 기준 줄 판정이 한 번도 작동하지 않음 | 위치 기반 판정으로 재작성(진행 방향 3.5 m 이내·도로 폭 5.5 m 내 근접 적 2명 이상). 씬당 1개(Canvas), 런 시작 시 초기화 | 실제 적으로 5케이스: 줄 사이 통과→1회 충돌(적 130→0, 상어 −130), 이미 1명 처치→통과, 단독 적·세로 열·도로 밖→충돌 없음 |
| 7 비동기 로딩 중 이전 씬이 HP 0을 다시 감지해 패배 연출·분석 이벤트를 중복 실행 | 로딩 중 `CanvasScript.Update` 중단 | 패배→다시 도전: 로딩 중 gameOver=false 유지 |
| 10 X2가 로비·설정·전환 화면에도 적용 | `GameSpeed.SetRunning` — 런 진행 중에만 사용자 배율 | `GameSpeedOnlyAppliesDuringRuns` |
| 15 색조 PropertyBlock이 무기·경고 링(MeshRenderer)에도 적용, 고속도로 투척 적의 애니메이터 재타이밍과 충돌 가능 | SkinnedMeshRenderer만 색조, HighwayEnemyAnimation 보유 적은 속도 변주 제외 | 코드 검토 |
| 13 휴게소: 경로선이 출발 375 m 뒤에서 시작·결승(1,430 m)보다 김, 버티기는 경로 중간(약 900 m) → 출발 2초에 15% | 모든 경로형 챕터를 "런 시작 지점→GameEndTrigger 투영 거리" 비율로 계산 | 휴게소 2%, 고속도로 1%(결승 2,328 m), 4·5챕터 경로 |
| 13 HUD가 4·5챕터 목표 패널(상단 −240)과 30단위 겹침 | 높이 46의 한 줄 레이아웃 | 잠실 캡처 |

확인 결과(수정 후): 오토바이 HP 교환 P>E(178/128→50/0)·E>P(88/128→0/40) 정확. 셔터 20초, 박스 HP 60→90. 5개 챕터 플레이 시작·HUD·X1/X2·줄 판정기 생성·콘솔 오류 0. 빌드 오류 0, 하네스 통과, EssentialProposalsTests 24/24.

전체 EditMode 1,192건: 1,138 통과 / 54 실패. 실패 스택 어디에도 이번 변경 파일이 없음. NoryangjinGameplayIntegrationTests의 NRE는 예외 스택을 켜서 확인한 결과 구 씬 `Noryangjin_MapTool_Mode` 픽스처의 `PlayerScript.HandleAnimation`(playerAnimator null)으로, 이번 변경과 무관. 과거 기록의 1,135건 중 52 실패 기준선과 구분해 새로 통과/해결로 표시하지 않음.

남은 결정 사항:
- **밸런스**: 셔터 앞 박스벽 7개 = 기본 연사 1.6발/초로 약 49초 분량(×1.0이어도 33초). 20초 안에는 대부분 셔터가 닫히고, 닫힌 셔터(HP×5)를 부숴야 함. 진행 불능은 아님.
- **"고속도로 기존 1.4배 구간"**: 데이터의 고속도로 가속은 전진 속도 `openPlayerMultiplier` **1.5**(X2에서 3.0). 10/05 사용자 요청 "이 모드에서 TimeScale 1.4, 끝나면 1.0"은 대상 미확정으로 아직 어디에도 적용되지 않았다.
- 노량진 진행도는 회전 체크포인트 15개 기준이라 약 6%씩 단계적으로 오른다.
- 동시에 다른 세션이 계정 파일(PlayAccountService, GameProgressSnapshot, HarborAccountPanel)을 수정 중. 이 작업의 로딩 오버레이 변경은 보존됨을 확인.
