# 노량진 개편 씬 9항목 피드백 (Claude Code, 2026-09-29)

- 상태: 적용·검증 완료.
- 대상: `Noryangjin_MapTool_Mode_SR18_Revamp.unity` 안전본만. 원본 SR18·Data.xlsx·Build Settings는 git 변화 없음.
- 사용자 원문: `docs/design/USER_STATED_REQUIREMENTS.md`의 "개편 씬 스크린샷 9항목 피드백".
- 앞선 작업: `noryangjin-claude-feedback-2026-09-28.md`(입체 교차·파도·젖은 바닥).
- 사용자용 보고서(Claude Docs, 확인 화면 9장): <https://claude.ai/code/artifact/724464e5-3449-4ba2-b337-19eb72324ee1>.
- 재현: `unity command --project-path . run_script --file tools/install-noryangjin-revamp.cs --entry InstallNoryangjinRevamp.Main` (재컴파일이 끝난 뒤).

## 항목별 원인과 적용

| # | 원인 | 적용 |
| --- | --- | --- |
| 1·2 | 다리 밑 카메라 낮추기(09-28 추가)가 초반 동쪽 부두에서 작동 | `NoryangjinMarketAtmosphere.lowerUnderDecks` 기본 꺼짐(판정 코드는 남김). 높은 카메라가 다리와 시장동을 지나도록 두 가지를 보완했다. 올라간 부두 다리·시장 바닥·밑면 슬래브 147개를 기존 가림 반투명 대상에 넣었다. 상어가 동쪽 부두(아래층)에 있을 때는 카메라가 들어가 있는 올라간 시장 구획과 소품 97개를 잠시 숨긴다(`NoryangjinInteriorDetailVisibility.liftedDetails`) |
| 3 | 젖은 자국 크기 | `NoryangjinFeedbackV3Builder`의 `halfSize` ×0.8(시각·판정 같이) |
| 4 | 시장 방송 순환 4번째 문구("활어 경매가 시작됩니다")가 아무 때나 나옴 | 순환에서 제외. 셔터 시작 안내를 "문이 곧 닫힙니다. 주의해주세요!"로 바꾸고 새 음성 `door-closing` |
| 5 | 서쪽 부두 연결 데크 3개가 꺾이는 모서리 2곳(124.3,-348 / -67,-348)을 덮지 못해 옛 SR18 회전 도로 판자가 보임 | 같은 재질 모서리 덮개 2개, 옛 회전 도로 2개 표시 숨김(충돌 유지) |
| 6 | 경매 인물이 통로 양옆, 공중에 뜬 가격판, 참치 팔레트뿐. Codex가 Meshy 캐릭터 재질을 일반 Lit으로 바꿔 아웃라인이 빠짐 | 경매장 재구성: 통로 가운데 입찰자 12명(번호 모자)이 경매사를 보며 서 있다가 상어가 16m 안에 오면 양옆으로 흩어짐(`NoryangjinAuctionScatter`). 활어 대야(Meshy N19) 40개, 천장 줄에 매단 "활어 경매장" 간판, 다리 달린 가격판, 광어 호가·낙찰 음성. 캐릭터 스킨 메시 8개를 FlatKit 아웃라인 재질로 되돌림 |
| 7 | 컨테이너 낙하 | 컨테이너 사건 삭제. SR18 갈매기 기믹(`SR18_L_G16_T193_Seagull`)을 3곳에 복제: 반경 28→44m, 그림자 1.0→1.4초, 14m 높이에서 0.55초 급강하, 경고 문구·음성 |
| 8 | 옛 야외 경매장(x 355–432, z 203.5)은 V3가 지웠는데 그 구간의 점포 12개와 SR18 적·보너스 숨김이 남아 빈 길이 됨 | 숨김 해제(점포 복원, 적·보너스는 Data.xlsx 배치대로 실행 중 활성화). "노량진 상인 연합 출동!" 상인 6명 돌격 추가(x=425) |
| 9 | 기존 여자 적을 가로 2.75×세로 1.72로 늘림 | Codex가 만든 참고 이미지 → Meshy N20(리깅, idle·walk·run·attack·hit·die). 균일 비율 키 3.8m, 상어 쪽을 봄. 길 전체를 막는 것은 충돌체가 담당 |

플레이 중 추가로 고친 것:
- 흩어짐이 먼 시장동 셔터 구간에서 먼저 발동했다. 판정에 좌우 위치와 진행 방향을 넣었다.
- 끝부분 돌격이 되살아난 SR18 적과 겹쳐 너무 빽빽했다. 9명에서 6명으로 줄이고 뒤로 옮겼다.
- 보스를 재설치할 때마다 몸체가 쌓였다. 이전 몸체를 지우고 새로 붙인다.

## 제작

- 참고 이미지: `outputs/meshy-noryangjin-claude-2026-09-29/concepts/`. Codex CLI(`codex exec -m gpt-5.5`)가 내장 이미지 생성으로 만들었다. 기본 모델은 새 CLI를 요구해서 실패했다.
- Meshy: `tools/produce-noryangjin-claude.py`, 원장 `outputs/meshy-noryangjin-claude-2026-09-29/ledger.json`, 77크레딧(N19 30, N20 47). 잔액 1669에서 시작했다.
- 가져오기: `ImportMeshyObjects.NoryangjinClaude0929`(N19), `ImportMeshyCharacters.NoryangjinClaudeBoss0929`(N20).
- 음성: `tools/create-noryangjin-claude-voice.ps1`(Windows 한국어 TTS) → `outputs/noryangjin-claude-fix-2026-09-28/audio/`.
- 씬 빌더: `Assets/ShooterSurvival/Editor/NoryangjinClaudeFeedbackBuilder.cs`. V2/V3 빌더 뒤, 입체 교차 들어 올리기 앞에 실행한다.

## 검증

- 빌드: Assembly-CSharp 성공(기존 경고 2), Editor 성공(경고 0). harness 검사 통과.
- EditMode: 83/83(GradeSeparation 19, RevampMechanics 13, FeedbackV3 10, InteriorV2 2, CameraOcclusion 9, DebugReview 14, StartStage 8, TestSpeed 8).
- 실제 플레이 봇(9999·빠른 좌우 ON, 1배속): 실내 경로 `tmp/image-previews/noryangjin-claude-fix-2026-09-28/run-inside-1x-090510`, 305.3초 완주. 경매 흩어짐 173.2초(경매장 입장 직후), 갈매기 경고 219.2초, 상인 돌격 260.2초, 보스 297초.
- 고정 촬영: `shots-085201`(경매장·모서리·갈매기 그림자), `shots-090439`(새 보스 근접).
- 봇은 파도와 위험 위치를 미리 아는 조건이라 사람의 난이도 근거가 아니다. 실기기는 확인하지 않았다.

## 남은 것과 주의

- 저장값: 이번 작업 시작 전 새 백업을 하지 않았다. 어제 백업과 비교하면 코인 38,161→40,218, 보석 20→30만 다르다. 사용자가 직접 플레이한 몫이 섞였을 수 있어 되돌리지 않았다. 맵툴 설정(9999 ON, 빠른 좌우 ON, 스테이지 1, 3배속)은 그대로다.
- 활어 대야 번호표에 Codex 이미지의 "N19" 글자가 그대로 텍스처로 들어갔다(작게 보임).
- 새 보스는 공격 중 몸을 숙이는 동작(Meshy action 51)이라 가까이서는 웅크린 듯 보인다.
- 시장동 출구 간판이 실내 카메라 높이라 잠깐 화면 위를 가리는 문제는 남아 있다.
