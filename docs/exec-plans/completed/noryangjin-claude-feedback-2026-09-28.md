# 노량진 개편 씬: 입체 교차·큰 파도·젖은 바닥 (Claude Code, 2026-09-28)

- 상태: 적용·검증 완료.
- 대상: `Assets/ShooterSurvival/Scenes/Tools/Noryangjin_MapTool_Mode_SR18_Revamp.unity` 안전본만. 원본 SR18·Data.xlsx·Build Settings는 git 상태 변화 없음을 확인했다.
- 사용자 원문: `docs/design/USER_STATED_REQUIREMENTS.md`의 "개편 씬 스크린샷 5장 피드백".
- 재현: `unity command --project-path . run_script --file tools/install-noryangjin-revamp.cs --entry InstallNoryangjinRevamp.Main` (재컴파일이 끝난 뒤 실행한다. 재컴파일 중이면 연결 오류로 실패한다).

## 원인

| 스샷 | 원인 |
| --- | --- |
| 1 | 60초 분기의 바깥 부두 데크(x≈99.3, 남북)가 초반 동쪽 부두 S3(z≈-7.25, 동서)를 같은 높이로 가로질렀다. 높이 12m SR18 고가 도로도 카메라(높이 12.7m)와 같은 높이라 바닥의 띠처럼 보였다. |
| 2·3 | 실내 시장동(x=124.3, 남북 378m)이 S3를 같은 높이로 가로질러, S3에서 건물 유리벽을 뚫고 지나갔다. |
| 4 | 파도가 2번뿐이었다. 한쪽 차로만 2.2초 전에 예고하고, 직접 만든 곡면 메시가 천천히 솟았다. |
| 5 | 젖은 자국 색이 청록 반투명(불투명도 최대 0.46)이었고, 밟으면 옆으로 미끄러지기만 했다. |

## 적용

- 입체 교차: `NoryangjinMarketBranch`에 높이 곡선을 추가했다(`liftHeight` 9m, 분기 거리 2→40 오르막, 40–64 수평, 64→100 내리막, 양 끝 완화 6m, 최대 경사 약 16.7°). `Point/Sample`에 높이가 들어가서 바깥 부두 데크가 다리가 된다. 난간·보·기둥은 시장 벽 밖으로 나간 뒤에만 세우고, 기둥은 S3 도로를 피한다.
- 실내 시장동: `Editor/NoryangjinCrossingLiftBuilder.cs`가 설치 마지막에 교차 구간을 들어 올린다. 바닥 충돌체(`IndoorTileSurface`)와 벽·천장·바닥 큐브는 1m 간격으로 나눠 곡선을 따라 휜다. 소품·글자·조명·이벤트 기준점은 통째로 올린다. 반사 프로브는 옮기지 않고 위로 늘린다. 밑면 슬래브와 기둥 16개를 추가하고, 교차 구간 아래 옛 S6 도로 표시 8개를 숨긴다. 변형 메시는 `Models/Generated/NoryangjinRevamp/CrossingLiftMeshes.asset` 하나에 저장한다.
- 경사 위 전투: 상어와 총알은 발밑 경사 대신 "앞 30m 안의 가장 높은 바닥"을 바라본다(`NoryangjinMarketBranch.AimForward`). 오르막에서는 꼭대기의 상인을 겨누고, 평지·내리막에서는 수평을 유지한다. 러시 상인은 기준점 바닥 기준 발 높이로 경사를 따라 달린다(`NoryangjinRushEvent.FollowMarketFloor`).
- 실내 판정(`NoryangjinInteriorDetailVisibility`)은 올라간 바닥 기준으로 층을 본다.
- 카메라(`NoryangjinMarketAtmosphere`):
  - 아래층을 지날 때 같은 지점에 위층 도로와 플레이어 높이의 바닥이 함께 있으면, 실내와 같은 낮은 카메라(높이 5.8m, 8°)로 내린다. S3의 새 입체 교차와 높이 12m 고가 4곳에 적용된다.
  - 실내 오르막에서는 카메라 위치의 바닥 높이 기준으로 카메라를 두고 시선 각도를 보정한다.
  - 실내 카메라 시작 지점을 z<35에서 z<43으로 앞당겼고, 바깥 부두를 선택하면 쓰지 않는다.
  - 바깥 부두에서는 시장 방송을 내보내지 않는다.
- 큰 파도(`NoryangjinWaveEvent` 재작성):
  - 패턴 3종으로 16번 나온다. 한쪽 덮침(2~3연속 포함), 초록 틈 1.5m만 남는 양쪽 파도, 차로를 따라오다 덮치기 0.45초 전에 고정되는 추격 파도(폭 2.2m).
  - 예고 0.95–1.4초, 덮침 0.9초, 닿으면 즉사다.
  - 파도 몸체는 SrRubfish `FX_PF_WaveAttack_Projectile`을 바다 16m 밖에서 끌어와 도로를 덮치게 한다. 덮칠 때 `Waterfall_MainFoam`·`Water_SimpleSplash_01`·`Waterfall_UnderwaterFoam`과 짧은 카메라 흔들림을 쓴다. 거품과 물결은 구역 앞쪽에서 터져 상어를 가리지 않는다.
  - "위험/안전" 글씨는 구역 앞끝에 띄운다. 시작점은 다리에서 내려온 뒤(분기 거리 100)다.
- 젖은 바닥(`NoryangjinWetPatch`):
  - 색을 거의 검정(0.012, 0.014, 0.02, 불투명도 0.84)으로 바꿨다.
  - 밟으면 1.1초 회전하며 초당 최대 체력 6%가 준다(`PlayerDamageCause.WetFloor`). 회전 직후 1.2초 무적 동안에는 기존 미끄러짐만 적용된다.
- 플레이 중 발견해서 고친 것:
  - 재컴파일 뒤 첫 Play에서 모든 보너스 패드가 매 프레임 NullReferenceException을 냈다(`BonusPadVisual.Tint`, `MaterialPropertyBlock` 미복원). 이 예외로 Error Pause가 걸렸다. 한 줄 null 보완으로 고쳤다.
  - 시작 6초의 SR18 신호 게이트(`Harbor_lane_signal_gantry`) 표지판이 카메라 높이라 화면을 막았다. 기존 가림 반투명 대상에 넣었다.
  - 컨테이너 구간 크레인 두 대를 길에서 약 6m 더 떨어뜨렸다.
  - 회전 안내 문구를 한 줄 길이로 줄였다.

## 검증

- `dotnet build`: Assembly-CSharp 성공(기존 경고 2), Assembly-CSharp-Editor 성공(경고 0). Unity 재컴파일 오류 0.
- EditMode 테스트(모두 통과): `NoryangjinGradeSeparationTests` 19/19(신규), `NoryangjinRevampMechanicsTests` 13/13, `NoryangjinFeedbackV3Tests` 10/10, `NoryangjinInteriorV2Tests` 2/2, `NoryangjinCameraOcclusionTests` 9/9, `NoryangjinDebugReviewTests` 14/14, `NoryangjinMapToolTestStartStageTests` 8/8, `NoryangjinMapToolTestSpeedTests` 8/8.
- 고정 촬영(`tools/claude-noryangjin-shots.cs`), `tmp/image-previews/noryangjin-claude-fix-2026-09-28/`:
  - `shots-223908`: S3 아래 통과, 바깥 부두 다리.
  - `shots-225131`: 실내 오르막 카메라, 검은 젖은 자국.
  - `shots-233430`: 신호 게이트 반투명, 크레인, S3 재확인.
- 실제 플레이 봇(`tools/claude-noryangjin-run.cs`, 9999·빠른 좌우 ON, 1배속, 파도는 `SafeLateral`로 회피):
  - 바깥 부두 `run-outside-1x-232844`: 파도 16번과 보스를 지나 297.3초에 완주했다.
  - 실내 `run-inside-1x-231950`: 상인 15명 처치, 오르막·내리막·물청소·터렛트·셔터·냉동창고 합류까지 정상이었다. 243.9초에 갈매기 구간에서 봇이 구멍을 피하지 못해 죽었다(봇 한계).
  - 들어 올리기 없이 같은 봇으로 비교한 실행 `run-inside-1x-230815`은 완주했다. 이 비교로 러시 실패 원인이 경사 조준임을 찾았고, `AimForward`로 고친 뒤 15명을 모두 처치했다.
- 봇은 파도 위치를 미리 아는 조건이라 사람의 난이도 근거가 아니다. 휴대폰 실기기 조작감과 FPS는 확인하지 않았다.
- 저장값: 시작 전 `outputs/noryangjin-claude-fix-2026-09-28/before/playerprefs.tsv` 66키를 백업했고, 끝에 불일치 0으로 복원했다. 맵툴 설정은 9999 ON, 빠른 좌우 ON, 시작 스테이지 1, 3배속으로 되돌렸다. 씬은 저장된 상태다.

## 남은 것

- 시장동 출구 "시장동 출구" 간판이 실내 카메라 높이라 약 0.5초 화면 윗부분을 가린다.
- 회전할 때 빈 바다만 보이는 기존 구간(예: 21초, 167초)이 남아 있다.
- 파도 난이도는 사람 플레이로 조정해야 한다. 조정할 값은 `install-noryangjin-revamp.cs`의 `plan` 배열(예고 시간, 간격)과 `gapHalfWidth`/`hunterHalfWidth`다.
