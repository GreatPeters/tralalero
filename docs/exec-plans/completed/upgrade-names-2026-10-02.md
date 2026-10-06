# 업그레이드 이름 정리 — 2026-10-02

상태: 프로젝트 적용 완료. APK 재빌드·휴대폰 설치는 이번 작업에 포함하지 않았다.

사용자의 짧고 직관적인 이름 요청에 따라 다음 이름을 선택했다. 아래 구체적인 명칭은 구현 선택이다.

| 이전 표시 | 새 표시 |
|---|---|
| 강철 앞코 | 공격력 |
| 충격 흡수 깔창 | 체력 |
| 스프링 코일 | 공격 속도 |
| 미사일 장식 | 미사일 지속 |
| 보스 파쇄기 | 보스 피해 |
| 코인 주머니 | 코인 획득 |
| 회복 패드 | 체력 회복 |
| 퉁퉁퉁 사후르 | 동료 체력 |
| 붐바르 지원 | 동료 공격력 |
| 측면 부스터 | 좌우 속도 |

`UpgradeUI.GetDisplayName`이 workbook 이름보다 `displayNameOverride`를 우선하므로 해당 UI 표시값과 연결된 TMP 제목만 변경했다. `HarborGameUIInstaller.Upgrades.cs` 생성기도 같은 이름으로 맞췄다. id4는 기존 enum 이름과 달리 미사일 지속 시간이며, id8·9는 각 동료의 체력·공격력, id10은 좌우 이동 속도다. 가격·증가량·최대 레벨·설명·아이콘·Data.xlsx·런타임 로직은 바꾸지 않았다.

## 적용과 확인

- 공식 Unity Pipeline `run_script`로 `tools/rename-upgrades-20261002.cs`의 `Apply`를 실행해 네이티브 직렬화 API로 수정했다.
- `Noryangjin_MapTool_Mode_SR18_Revamp`, `HighWay`, `RestStop`을 다시 열어 `Verify`로 저장된 카드 60개(씬마다 현재 카드 10개와 비활성 구형 카드 10개)를 확인했다.
- 작업 직전 백업과 비교해 세 씬의 변경 줄이 `displayNameOverride`와 `m_text`뿐임을 확인했다. 증거: `outputs/upgrade-names-2026-10-02/field-diff-final.json`, `verified.txt`.
- 배포 대상이 아닌 원본 SR18은 저장 시 무관한 재직렬화 변경이 발견되어 이번 작업 직전 백업으로 정확히 복구했다. 최종 변경 없음.
- Play Mode 진입이나 PlayerPrefs 수정 없이 검증했다. 끝날 때 Editor는 컴파일 중이 아니며 정지 상태, Revamp 씬만 열리고 미저장 변경 없음.

관련 교훈: [UI 이름 변경 시 구형 씬 재직렬화 범위 확인](../../solutions/workflow-issues/scope-unity-label-edits-and-check-scene-reserialization-2026-10-02.md).
