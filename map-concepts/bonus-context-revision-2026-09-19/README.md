# Bonus Wall 발생 맥락 수정 제안

2026-09-19 사용자 후속: “근데 갑자기 저게 있는 게 말이 되나...? 고사상이 있는 게?”, 이어 “맥락이 이해되게 그리고 현실적인 걸 고려해서”.

## 제안과 현재 상태 구분

실제 EnemyScript_space는 허용된 적의 사망 시 SpawnBonusAltar로 bonusWall을 생성한다. 이 출현 맥락을 검토하지 않은 고사상 외형은 적 처치 드롭에 특히 어색했다.

새 제안은 **적이 놓친 장비·현장에 남은 보급품 → 저주 신발의 문양으로 인식 → 흡수하여 이번 판 능력 강화**다. 시작 부활 제단은 유지하고 런 중 작은 보상과 구분한다.

이는 GAME_DESIGN_OVERVIEW.md §12.5의 고사상 공물 설정을 대체하는 **에이전트 제안**이다. 사용자 요구는 맥락과 현실적인 제작 범위이며, 구체적인 공구·구급함 설정을 승인한 것으로 기록하지 않는다. 기존 기획서·게임 코드는 변경하지 않았다.

## 세 장의 그림

1. [적이 놓친 물품을 신발이 흡수](images/01-enemy-drop-flow.png): 소지품이 떨어지는 출처, 신발의 반응, 흡수와 임시 능력 증가.
2. [현장에 남은 보급품](images/02-fixed-choice-context.png): 공구함 주변에서 발견하는 고정 선택과 흡수 완료 전후.
3. [장소가 바뀌어도 같은 규칙](images/03-chapter-context-and-kit.png): 노량진 정비 소품, 고속도로 정비 차량, 휴게소 짐 주변에 공통 소형 보너스를 배치하는 예시.

## 파일 존재를 확인한 재사용 후보

공통 접두어: `Assets/polyperfect/Poly Universal Pack/Prefabs/`.

- `Tools/Wrench.prefab`
- `Farm/Toolbox_Wood_Open.prefab`
- `Survival/First_Aid_Kit.prefab`
- `Survival/Bag_Duffle_Plain.prefab`
- `Fantasy/Alchemy Fantasy/Bag_Pouch.prefab`
- `Racetrack/Props Racetrack/Toolbox_Repair.prefab`
- `Steampunk/Parts Steampunk/Cog_5_Teeth.prefab`

파일 존재 확인은 실제 그림과 동일한 외형·가방 내용물 분리 가능성을 보증하지 않는다. 물품을 넣고 꺼내는 시뮬레이션 대신 비활성 자식 오브젝트를 짧은 이동/스케일 애니메이션으로 보이는 방식이 제작 출발점이다.

신발 문양은 평면 이미지/메시, 흡수는 짧은 타깃 이동 궤적을 제안한다. 수치·1택·중복 방지·런 초기화 규칙은 기존 동작을 기준으로 구현한다. 공격력/체력은 예시 두 종류이며 나머지 실제 보너스 종류의 아이템 매핑은 별도 설계해야 한다. 모든 능력에 구급함이나 톱니를 일괄 쓰지 않는다.

## 확인과 한계

내장 ImageGen으로 생성한 맥락 설명 시안이며 Unity 적용 이미지가 아니다. 세 PNG 모두 1536×1024로 다시 열어 확인했다. 원본은 유지하며 `tmp/image-previews/bonus-context-revision-2026-09-19/`에 열람용 사본을 저장했다.

두 번째 장의 원안은 화살표가 물품 방향으로 향해 의미가 반대였다. 두 번의 부분 수정으로도 명확하게 해결되지 않아, 발견/흡수 완료 두 상태를 새로 그린 최종본을 채택했다. 세 번째 장의 “체력 회복”을 “체력 증가”로 수정했다. 화살표·입자 방향은 런타임에서 물품에서 신발로 향하도록 검증해야 한다.

[원 프롬프트](prompts.json), [수정 프롬프트](revisions.json), [선택된 생성 경로](generated-files.json).

