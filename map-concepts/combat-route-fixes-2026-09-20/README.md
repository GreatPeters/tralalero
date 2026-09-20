# 전투·경로 수정 — 2026-09-20

후속 변경: [상자/얼굴 간격과 배의 고정 대기 방향](../throw-ship-refinement-2026-09-20/README.md). 아래 최초 배의 예측 조준 설명은 후속 요청으로 대체됐다.

사용자의 9개 수정 요청을 기존 Unity 프로젝트에 적용했다. 브랜치 `fix/combat-route-feedback`; 이전 부적 작업과 시작 시 저장되지 않았던 SR18 편집 상태는 보존했다. 시작 상태 사본은 `before/`에 있다.

## 적용 결과

| 항목 | 결과 |
|---|---|
| 가드 | 실제 오른손을 총 손잡이의 고정점으로 사용한다. 사격 상태에서만 조준 보정하며 총구에서 Arrow2 복제본을 발사한다. 과도하게 긴 총은 손잡이 위치를 유지하며 축소했다. |
| 뚱보 | 6개 배치를 `Shoot`로 전환했다. 제자리에서 `Fatman_ThrowShort`를 재생하고 0.62초에 발사, 2.8초 주기로 반복한다. 플레이어의 전진 속도 × 5초 안에서 시작하며 배치 정면으로 직선 발사한다. |
| 여성 보스 | E06과 E25에서 각각 중앙 1개. E06 일반 적 상대편과 E25 중복 보스를 비활성화했다. |
| 체력 | 기존 엘리트 성장값은 보존했다. E06의 교체된 여성 모델에 남아 있던 Normal 타입을 Boss로 수정하고 HP 519를 적용했다. E25 HP 2458을 유지한다. |
| 경사 화면 | 현재 발밑부터 연속으로 연결된 앞쪽 도로를 높이 추종기로 추적해 가림 처리에서 제외한다. 별개 위층 다리·간판의 가림 처리는 유지한다. |
| 퉁퉁퉁 | 이동 전후 구간을 캡슐로 검사한다. 실제 적의 Default 레이어와 풀 적의 Enemy 레이어를 모두 지원한다. 두 현재 체력을 동시에 차감하며 동률이면 둘 다 사망한다. |
| 붐바르딜로 | 무기의 실제 Owner를 탄환에 전달한다. 기존 플레이어 회전 델타를 적용하며 속도·수명·피해량을 유지한다. |
| 적 정렬 | 세 맵의 25개 행을 실제 도로 중심선/곡선 표본에 맞췄다. 쌍은 좌우 1.1m, 단일은 중앙. 이동 목표도 함께 이동했다. |
| 배 | 선체 길이 9.5m. 플레이어 방향으로 회전하고 FirePos에서 원본의 월드 크기를 유지한 CannonBall을 발사한다. 7.5초 이동 거리에서 시작하며 플레이어 이동을 예측한 직선 사격을 한다. |

5초/7.5초는 시뮬레이션 이동속도 기준이다. 현재 전진 속도 8.84m/s에서 약 44.2m/66.3m다. 뚱보는 같은 직선 구간의 플레이어에게 발사하며, 코너 너머나 다른 높이의 도로를 향해 조기 발사하지 않는다. 반복 주기·선체 길이·초반 보스의 체력 산식은 구현 선택이며 사용자 지정 수치로 기록하지 않는다.

## 검증

- `dotnet build Assembly-CSharp.csproj -nologo -v:q`: 성공. 기존 ithappy `SplineSpeed.m_LastIndex` 미할당 경고 1개.
- `dotnet build Assembly-CSharp-Editor.csproj -nologo -v:q`: 성공.
- `tools/validate-agent-harness.ps1`: 성공.
- Unity Edit Mode 70개 통과: CombatRouteFeedback 13, EnemyEventController 24, EnemyThrowDirection 9, EncounterPlacementTables 8, NoryangjinCameraOcclusion 5, NoryangjinSlope 6, ProjectilePoolLifetime 5.
- `play/report.json`: 가드 1발, Arrow2의 +X와 속도 방향 내적 1.0, 손잡이 오차 약 0.000002m. 뚱보 3회 투척, 이동 0m, 정면 방향 내적 1.0, 범위 밖 발사 0. 배 최초 발사 프레임은 범위 진입 프레임과 일치하고 FirePos 오차 0m; 포탄 직경 약 0.6~0.7m.
- `helper-contact-play.json`: 실제 프리팹의 10m 이동 구간 접촉. (적/도우미) 100/250 → 0/150, 1200/250 → 950/0, 2458/300 → 2158/0, 250/250 → 0/0. 사망한 적 콜라이더는 즉시 꺼진다.
- `camera-playmode-probe.json`: 4개 실제 경사 구간 통과. 최고 높이 12.18m, ±19.57° 경사, 경사 스팟 8개 소비, 마지막 높이 약 0.16m. 기존 게임 카메라에서 네 구간을 촬영·확인했다.
- `scene-audit.json`: 런 시작 엑셀 데이터를 적용한 후 세 맵 모두 행 정렬 오류 0. 각 100개 배치 데이터 정상 적용, 활성 적 SR18 48 / HighWay 50 / RestStop 46. HighWay와 RestStop에는 여성 보스/뚱보 모델이 없다.
- `workbook-preservation.json`: 44개 셀만 변경. 변경된 `적 배치` 시트 외 ZIP 구성요소는 동일. 보호된 런타임 데이터도 갱신했다.

근접 화면은 실제 Play Mode 동작을 별도 카메라로 촬영했고, 경사 화면은 기존 게임 카메라다. 연속 한 판 전체와 실제 휴대폰 실행은 이번 검증에 포함하지 않았다. 마지막 선행 조준 보정은 수학적 충돌 시점 테스트로 확인했으며 앞선 정지 대상 배 촬영과 구분한다.

## 확인 화면과 재현 도구

- [브라우저 갤러리](http://127.0.0.1:6753/combat-route/) — 로컬 서버가 실행 중일 때 사용. Chrome에서 실제 이미지 표시를 확인했다.
- 원본 사본: `tmp/image-previews/combat-route-2026-09-20/`.
- 장면/프리팹 적용: `tools/apply-combat-route-fixes.cs` (Edit Mode, 명시적인 저작 작업).
- 세 맵 런 시작 데이터 감사: `tools/audit-combat-scenes.cs` (감사 후 저장하지 않고 원래 장면 재열기).
- 발사/충돌 확인: `tools/verify-combat-play.cs`, `tools/verify-helper-contact-play.cs` (Play Mode).
- 경사 확인: `tools/verify-combat-camera-play.cs` (Play Mode; 검증 중 보너스·상점 발동을 분리해야 한다).

처음의 레이어 한정 합성 테스트는 실제 Default 레이어 적을 놓쳤다. 실제 프리팹 검증으로 수정한 후 두 레이어 모두를 회귀 테스트에 포함했다. 총의 -X를 앞쪽으로 가정했던 임시 보정도 포즈 측정으로 폐기했다. 최종 총구/Arrow2 진행 축은 +X이며 손잡이 고정점은 메시 min-X다.
