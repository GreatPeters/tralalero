---
title: 노량진 경로 시간은 Excel 적용 런타임 속도로 산정한다
date: 2026-09-02
category: logic-errors
module: Noryangjin route planning
problem_type: logic_error
component: documentation
symptoms:
  - 145개 도로 모듈 경로를 약 4분 30초로 문서화했지만 실제 기본 속도에서는 약 3분 27초가 됨
  - 현재 564-unit 경로의 94초 추정값이 Excel 적용 런타임 속도와 맞지 않음
root_cause: inadequate_documentation
resolution_type: documentation_update
severity: high
related_components: [tooling, development_workflow]
tags: [unity, noryangjin, route-timing, data-xlsx, player-speed, excel-precedence, map-planning]
---

# 노량진 경로 시간은 Excel 적용 런타임 속도로 산정한다

## Problem

노량진 연속 경로의 완주 시간을 `6 units/s`로 계산해 기존 51개 모듈에 94개를 더한 총 145개 모듈 경로를 약 4분 30초로 판단했다. 실제 런타임은 `Data.xlsx`의 `playerSpeed` 값1인 `8 units/s`를 사용하므로, 이 경로안은 4~5분 목표에 미달했다.

## Symptoms

- 신규 94개·총 145개 모듈 기준 경로가 실제 속도에서는 약 3분 27초에 끝났다.
- 경로 설계 문서의 시간 추정과 플레이어의 실제 전진 속도가 달랐다.
- 맵 길이뿐 아니라 구역 수, 전투 비트와 성장 벽을 배치할 공간까지 과소 산정될 수 있었다.

## What Didn't Work

처음 계산은 Inspector fallback과 오래된 문서에 남아 있던 `6 units/s`를 사용했다.

```text
기존 거리 = 564 units
신규 거리 = 94 modules × 11.25 units = 1,057.5 units
6 기준 = (564 + 1,057.5) ÷ 6 + 9 turns × 0.5 ≈ 274.8초
8 기준 = (564 + 1,057.5) ÷ 8 + 9 turns × 0.5 ≈ 207.2초
```

식 자체가 아니라 입력값의 권위가 잘못됐다. `PlayerScript`는 `useExcelCharacterDefaults`가 켜져 있으면 Inspector 기본값을 Excel 환경 변수로 덮어쓰며, 노량진 씬은 이 경로를 사용한다.

## Solution

경로 시간을 아래 세 입력으로 다시 계산했다.

1. 현재 씬에서 측정한 실제 경로 거리
2. `Data.xlsx`에서 런타임이 해석한 `playerSpeed` 값1
3. 실제 회전 스팟 수와 회전당 정지시간

수정한 기준안은 현재 거리 564 units 뒤에 도로 138개를 추가하고, 전체 회전을 10회로 구성한다.

```text
기존 거리 = 564 units
신규 거리 = 138 modules × 11.25 units = 1,552.5 units
총 거리 = 2,116.5 units

이동 시간 = 2,116.5 ÷ 8 = 264.5625초
회전 시간 = 10 turns × 0.5초 = 5초
총 시간 = 269.5625초 = 약 4분 29.6초
```

동일한 입력으로 4~5분 목표 범위를 역산하면 총 168~211개 모듈, 현재 51개 뒤에 추가할 도로는 약 117~160개다. 선택한 신규 138개·총 189개 모듈은 이 범위 중앙에 들어온다.

관련 숫자는 [개발 문서](../../../DEVELOPMENT_OVERVIEW.md)와 [맵 기획서](../../../MAP_DESIGN_OVERVIEW.md)에 반영했다. 오래된 현재값을 담고 있던 [신뢰성 문서](../../RELIABILITY.md)와 [맵 2 참고 문서](../../noryangjin-map2-authored-scene.md)도 현재 속도 8 기준으로 맞췄다.

## Why This Works

경로 완주시간은 지오메트리만의 속성이 아니다.

```text
routeSeconds = measuredRouteUnits ÷ resolvedRuntimeSpeed
             + turnCount × turnDelay
```

이 프로젝트에서 `resolvedRuntimeSpeed`는 Inspector에 보이는 값이 아니라 다음 우선순위로 결정된다.

1. 대상 플레이어가 `useExcelCharacterDefaults`를 사용하는지 확인
2. 사용한다면 `Data.xlsx > 환경 변수 > playerSpeed > 값1` 적용
3. 키가 없거나 opt-in이 꺼진 경우에만 Inspector fallback 사용

따라서 데이터 원본, 코드의 해석 규칙과 씬의 opt-in 상태를 함께 확인해야 맵 계획과 실제 게임플레이가 같은 속도를 사용한다.

## Prevention

- 경로·시간 계획 전 [플레이어 기본값 문서](../../player-character-defaults.md)와 [노량진 맵툴 문서](../../noryangjin-gameplay-maptool.md)에서 현재 속도 권위를 확인한다.
- `PlayerScript`의 데이터 우선순위와 대상 씬의 `useExcelCharacterDefaults` 값을 함께 확인한다.
- 모듈 수만 곱하지 말고 측정된 기존 거리, 신규 피치, 회전 수와 정지시간을 계산식에 남긴다.
- 계산 결과는 자동 전진 또는 Play Mode 측정으로 다시 확인하고 실제 결과는 밸런스 문서에 기록한다.
- 속도값이 바뀌면 경로 시간, 필요 모듈 범위와 구역별 전투 예산을 함께 다시 계산한다.

## Related Issues

- [독립 오라클로 설정 열 검증](../best-practices/use-independent-oracles-for-configuration-column-tests-2026-08-01.md)
- [도메인 리로드 없이 Excel 기본값 갱신](../integration-issues/refresh-excel-character-defaults-without-domain-reload-2026-08-01.md)
- [실제 맵툴 오브젝트를 경로 계획으로 확장](../design-patterns/scale-live-map-tool-object-matches-into-route-plans-2026-07-19.md)
