---
title: 노량진 9999 플레이 검수와 후속 수정
date: 2026-09-28
status: completed
origin: docs/design/USER_STATED_REQUIREMENTS.md
---

# 노량진 9999 플레이 검수와 후속 수정

## 목적과 범위

공격력/체력9999(테스트)와 빠른 좌우 이동을 켜고 실제 노량진 Play를 검수한다. 캡처와 런타임 값으로 확인된 문제를 아래 작업 단위로 수정하고 재검증한다. 사용자 요청에 따라 마지막에도 두 옵션을 켜 둔다. 원본 SR18, Data.xlsx, Build Settings와 실제 구매/재화 저장값은 보존한다. 테스트 옵션은 무적이 아니며 일반 보너스 적용 후 수치는9999보다 커질 수 있다.

원본 요청은 `docs/design/USER_STATED_REQUIREMENTS.md`의 2026-09-28 테스트 항목. 구현 기준은 `Noryangjin_MapTool_Mode_SR18_Revamp` 안전본과 기존 이벤트·카메라·재화 계약이다. 신규 모델 결제나 정식 성장 밸런스 재설계는 이번 범위에 포함하지 않는다.

## 실제 발견 근거

기준 실행: `tmp/image-previews/noryangjin-revamp-fix-2026-09-28/play-debug-review-inside-050620/`. 실제1배속, 시작HP/공격9999, 이동 감도125→31.25. 자동 좌우 조작을 이용한 실제 물리/사격 실행이며 승률 검증으로 간주하지 않는다.

| 항목 | 관찰 | 원인과 결정 |
|---|---|---|
| P1 경매 인간벽 | 261.26초 적 충돌 사망. 중앙 탄 y≈1.0, 적4명 체력20047이 그대로 유지 | 4명 배치의 중앙±0.6m, 반경0.44m 사이로 좁은 탄은 지나가고 넓은 플레이어는 충돌한다. 중앙 적이 있는3명 배치로 정렬한다. 기존 적 전체의 체력 교환 규칙은 유지한다. |
| P2 보상/숫자 가림 | frame-044/047: 코인 여러 개가 상어와 다음 벽을 덮고5자리 HP가 겹침 | 상자 상단 높이에서 코인을 만들고 추가로1.1m 상승해 자동 수집 높이2.5m를 벗어날 수 있다. 새 기물의 보상은 바닥 기준 높이로 내리고 작은 기존 코인 그림을 사용한다. 벽 표시도 근거리의 작은 숫자/합산 피해로 제한한다. 금액은 보존한다. |
| P3 TV/물줄기 오독 | frame-019: 앵커가 거꾸로 보임. frame-033: 물이 불투명 원통처럼 보이고 바닥 위험 영역이 안 보임 | Cube 뒷면 UV와 새 바닥 아래의 WetBand. TV는 명시적 정방향 UV 화면을 사용한다. 물은 움직이는 가는 줄기/방울로 표시하고, 새 바닥 위에 사전 경고와 활성 범위를 표시한다. 피해 범위/초당10%는 유지한다. |
| P4 시야 가림 | frame-008/012/078: 상부 도로·상점이 진행 시야를 막음. frame-054: 냉동창고 내부에 옛 갈색 도로 기둥 | 상부 도로 메시가 아래로 긴 기둥을 포함해 bounds.min 기반 검출을 피한다. 실제 도로 collider에 대한 시선 교차로 확인하되 현재/진행 중 지면은 제외한다. 심하게 가리는 상점은 안전본 전용으로 숨김 처리하고 복원한다. 냉동창고도 충돌을 유지한 채 옛 도로 표시만 제어한다. |
| P5 실내 판정 | 방향·층 높이·실제 분기 없이 XZ 범위만으로 실내 가시성을 적용 | 반대 방향 접근과 위층을 실내로 취급하지 않는다. 높이와 진행 방향, 분기를 판정에 포함한다. |
| P6 셔터 표현 | frame-047:13초 남았는데 문 밑 공간이 상어보다 낮음 | 남은 시간 비율을 곧바로 완전 폐쇄 높이에 매핑한다. 남은 시간이 있는 동안 통과 높이를 확보하고0초에 짧게 완전히 닫는다. 열린 동안 통과/닫힌 후 파괴 규칙은 유지한다. |

## 작업 단위

### U1 테스트 조건/경매 중앙 정렬

- 파일: `tools/noryangjin-debug-review.cs`, `tools/verify-noryangjin-revamp-fix.cs`, `tools/install-noryangjin-revamp.cs`.
- 기존 `NoryangjinMapToolTestOverrides.Select`를 사용한다. 테스트 도구가 두 옵션을 다시 끄지 않게 선택을 분리한다. 최종 저장값 복원 후 옵션ON/스테이지1을 다시 적용한다.
- 경매 인간벽을 중앙 한 명과 양쪽 한 명으로 구성한다. 신규 적의 몸집·HP 교환·보상 계약은 바꾸지 않는다.
- 검증: 시작/종료 옵션, 실제9999 시작값/31.25 감도 기록. 중앙 미사일이 경매 적을 실제로 맞히고 동일 경로를 통과하는지 확인한다.

### U2 기물 보상과 전투 표시

- 파일: `Assets/ShooterSurvival/Scripts/Game/NoryangjinRevamp/NoryangjinBreakable.cs`, `Assets/ShooterSurvival/Editor/NoryangjinInteriorV2Builder.cs`.
- 기물 코인은 기존 `CoinPickup`을 사용하되 바닥 기준 높이/작은 시각 크기로 생성한다. 체력 숫자와 합산 피해 표시는 가까운 기물에만 보이며 금액·피해량·파괴 판정은 그대로다.
- 테스트: `Assets/Tests/Editor/NoryangjinDebugReviewTests.cs`. 높은 상자에서도 생성 높이는 자동 수집 범위 이내, 지급 금액/반복 수집 계약 보존, 근거리 숫자 표시/재시작 초기화.
- 실행 자세: 보상 높이 회귀를 먼저 실패시키고 수정.

### U3 TV/물청소 표현

- 파일: `NoryangjinInteriorV2Builder.cs`, `NoryangjinHoseEvent.cs`, 신규 `NoryangjinHoseSprayVisual.cs`(모두 위 Editor/Game 경로 기준).
- TV 화면에는 한 방향 평면/정상 UV 사용. 물줄기는 가는 복수 곡선과 움직이는 방울로 시각화하고 경고 띠는 타일 위에 배치한다. 기존 주기 앞에 짧은 경고만 추가하며 활성 시점과 피해는 동일하다.
- 테스트: `NoryangjinDebugReviewTests.cs`의 경고/분사/안전 시간 경계. `NoryangjinRevampMechanicsTests`의 안전 반쪽/다른 도로 판정 유지. 네이티브 TV/경고/분사 캡처.

### U4 도로·상점 시야와 실내 범위

- 파일: `Assets/ShooterSurvival/Scripts/Player/NoryangjinCameraOcclusion.cs`, `NoryangjinInteriorDetailVisibility.cs`, `NoryangjinInteriorV2Builder.cs`, main installer.
- 기존 추가 장식40% fade를 기본값으로 유지한다. 안전본에서 지정한 심한 상점 가림은 완전히 숨기고 종료 시 원상 복구한다. 긴 기둥을 포함한 상부 도로는 opt-in 실제 collider 시선 검사로 처리한다. 지면 투영 경로/충돌은 유지한다.
- 실내와 냉동창고는 방향·높이 조건을 포함한다. 옛 도로 렌더러 상태를 캡처하여 구간 종료/컴포넌트 비활성화 때 복원한다.
- 테스트: `Assets/Tests/Editor/NoryangjinCameraOcclusionTests.cs`, `NoryangjinInteriorV2Tests.cs`, `NoryangjinDebugReviewTests.cs`. 상부 메시/진행 지면/경사로/부분 가림/비활성 복원/반대 방향/위층/바깥 분기.

### U5 셔터 통과 높이

- 파일: `Assets/ShooterSurvival/Scripts/Game/NoryangjinRevamp/NoryangjinShutterEvent.cs`, installer.
- 카운트다운 중에는 상어가 통과할 수 있는 최저 여유 높이를 유지한다.0초 이후 실제 닫힘과 기물 활성화를 맞추고 파괴 탈출·HP 소모는 유지한다.
- 테스트: `NoryangjinDebugReviewTests.cs`: 남은 시간 양수/0 경계, 리셋/타이머 종료. 실제 접근/통과/강제 시간초과 fixture.

## 순서와 완료 조건

기준 Play 증거 보존 → 원인/수정 결정 기록 → 회귀 테스트 → 개별 수정 → 공식 Unity CLI로 안전본 재생성 → 두 분기 실제 Play와 문제 지점 재촬영 → 저장값 복원/두 옵션ON 유지. 실행 경과는 `outputs/noryangjin-debug-review-2026-09-28/WORKLOG.md`에 적는다.

완료 조건은 코드/씬 저장, 관련 빌드·테스트 성공, 수정 전후 증거, 원본3파일 해시 일치, 저장값 복원 및 마지막 두 옵션ON 확인이다. 기존 Editor 재생성 URP 예외는 별도 운영 이슈로 기록하며 실제 Play 오류와 혼동하지 않는다. 추가 발견은 코드 적용 전 이 계획의 범위를 재평가하되 현재 사용자 승인 안의 명백한 결함은 연속 처리한다.

## 가정과 연구 범위

이번 수정은 사용자가 승인한 자율 플레이 검수의 일부다. 외부 정보가 필요한 API/서비스 변경이 없어 저장소의 구현과 native 증거를 기준으로 결정했다. 경매 중앙 정렬·시각 표시·임계값은 실행자의 설계 선택이며 사용자 원문으로 기록하지 않는다. 단순한 봇 이동 실수만으로 정식 난이도를 낮추지 않는다.
