---
title: Unity UI 이름 변경 시 구형 씬 재직렬화 범위 확인
date: 2026-10-02
category: workflow-issues
module: Upgrade UI scene maintenance
problem_type: workflow_issue
component: development_workflow
severity: medium
applies_when:
  - Unity 씬에 직렬화된 UI 문구를 일괄 수정할 때
  - 사용 중인 씬과 오래된 원본 씬이 함께 존재할 때
tags: [unity, scene, serialization, upgrades, labels, scope]
---

# Unity UI 이름 변경 시 구형 씬 재직렬화 범위 확인

## Context

업그레이드 제목 10종을 변경하면서 사용하지 않는 원본 SR18 씬까지 열고 저장했다. Unity 네이티브 API를 사용했지만 원본 씬에서는 문구 외에도 `clearViewGroups`, 피드백 설정, stripped transform 관련 줄이 바뀌었다. 사용 중인 세 씬은 제목 필드만 바뀌었다.

## Guidance

1. 실제 표시값의 소유자를 먼저 찾는다. 이 프로젝트에서는 workbook보다 `UpgradeUI.displayNameOverride`가 우선하므로 workbook을 수정할 필요가 없다.
2. 사용 중인 씬을 명시적으로 열거하고, 작업 직전 파일 백업을 남긴다. 기존 사용자 변경이 있으므로 HEAD를 복원 기준으로 사용하지 않는다.
3. Unity `SerializedObject`와 TMP API로 제목을 수정한다. 씬 생성기에도 같은 문구를 반영한다.
4. 저장 후 백업과 비교해 허용한 필드만 바뀌었는지 확인한다. 네이티브 저장도 오래된 직렬화 형식을 갱신할 수 있다.
5. 범위 밖 씬의 변경을 발견하면 그 씬을 닫고 해당 작업의 새 백업으로 복원·재임포트한다. 이번처럼 정확한 원본 파일 복원과 YAML 수동 편집은 구별한다.
6. 다시 연 씬에서 저장값을 확인하고 원래 Editor 씬 구성을 복구한다.

## Why This Matters

단순한 이름 교체에 자동 마이그레이션까지 섞이면 리뷰와 회귀 원인 추적이 어려워진다. UI 표시만 바꾸는 작업에서는 이름 필드 이외의 변경이 없는지가 중요한 검증 기준이다.

## Example and evidence

`tools/rename-upgrades-20261002.cs`는 최종적으로 Revamp·HighWay·RestStop 세 씬만 대상으로 한다. 카드 60개를 검증했고, `outputs/upgrade-names-2026-10-02/field-diff-final.json`에는 세 씬의 제목 필드만 변경되고 원본 SR18은 작업 전과 같다는 결과를 남겼다. PlayerPrefs와 플레이 상태는 변경하지 않았다.

## Related guidance

[Humanoid 프리팹 저장 시 재임포트 pose override 방지](prevent-humanoid-reimport-pose-overrides-when-saving-unity-prefabs-2026-07-27.md)는 네이티브 저장의 부수 변경이라는 점에서 관련되지만, 대상·원인·해결은 다르다. 이번 기록은 UI 표시 소유권과 씬 범위 검증을 다룬다. 외부 이슈 검색은 이 로컬 작업의 검증 근거에 사용하지 않았다.
