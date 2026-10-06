---
title: Own FlatKit passes to avoid startup keyword-space assertions
date: 2026-10-02
category: runtime-errors
module: FlatKit shader binding and opening playback
problem_type: runtime_error
component: development_workflow
severity: high
symptoms:
  - "Entering Play reports incompatible keyword spaces and 71 versus 75 keyword-state sizes."
  - "The original opening reports unexpected H.264 timestamps."
root_cause: scope_issue
resolution_type: code_fix
tags: [unity, flatkit, usepass, local-keywords, srp-batcher, h264, startup]
---

# 시작 시 FlatKit 오류와 영상 타임스탬프 경고 수정

## 문제와 원인

첨부 로그에는 키워드 공간 오류 5건과 크기 불일치 5건, H.264 타임스탬프 경고 1건이 있었다. Firebase, CombatHarness, 표 로딩 메시지는 일반 정보 로그였다.

외곽선 묶음 최적화에서 `FlatKit/Stylized Surface With Outline`이 여섯 패스를 `UsePass`로 가져왔다. 이 셰이더의 키워드 공간은 71개, 원본 표면 셰이더는 자체 Outline 패스까지 있어 75개였다. Unity 6000.2.6f1에서 공유 패스의 상태를 다른 공간에 적용하면서 `Material::BuildProperties` → `ComputePassValuesHash` → `LocalKeywordState::Remove`가 어긋났다. 시작 시 텍스처 스트리밍이 머티리얼 정보를 읽으며 같은 오류가 나타났다.

기존 머티리얼 교체나 저장된 키워드와 무관하게 **새 머티리얼 생성/패스 바인딩만으로 재현**했다. 일반 표면 셰이더는 오류 0건, 외곽선 셰이더는 8건이었다. SRP Batcher 호환성 검사는 이때도 0(호환)이었다. 따라서 호환성 검사만으로 이 런타임 오류를 찾을 수 없었다. 로컬 키워드는 셰이더별 공간에 저장된다는 [Unity 설명](https://docs.unity.cn/6000.0/Documentation/Manual/shader-keywords-scope-fundamentals.html)과 현장의 재현 결과를 함께 사용했다. 다른 Unity 버전의 모든 `UsePass`가 잘못됐다고 일반화하지 않는다.

영상은 원래 선택한 `Curse_Opening_Animated.mp4`의 High-profile H.264 스트림이었다. Windows 재생 시 타임스탬프 교정 경고가 났다. 최근 사용자가 요청한 네 번째 장면 원복은 유지해야 하므로 내용이나 컷 순서는 바꾸지 않았다.

## 수정

- 여섯 패스를 외곽선 셰이더 안에 선언해 해당 셰이더가 키워드 상태를 소유하게 했다. `StylizedInput.hlsl` 공용 레이아웃, 모든 머티리얼 프로퍼티, `OutlineLegacy`의 전체 계산 코드와 RenderObjects 설정은 유지했다.
- `tools/sync-legacy-outline-passes.py`로 원본 패스 본문을 동기화한다. `--check`는 여섯 본문이 일치하는지 검증한다. 셰이더의 키워드 개수는 여전히 71/75지만 공유 상태의 잘못된 소유 관계가 사라져 오류가 없어졌다.
- 영상은 Constrained Baseline, B-frame 0, 24fps, 일정한 타임스탬프로 다시 인코딩했다. **936프레임·39초·720×1280**, 동일 GUID, 0/8/20/30초 장면 경계와 원래 네 번째 장면을 보존했다. 전체 프레임 비교 PSNR 평균 47.638dB, 최저 44.726dB. 원본 17,255,290바이트와 새 파일 28,229,382바이트 및 해시는 출력 기록에 남겼다.
- Analytics는 고장 난 기능이 아니므로 수정하지 않았다. 에디터에서는 플랫폼 조건문으로 전송 초기화를 건너뛰고 일반 `Debug.Log`를 출력한다.

## 검증

- `LegacyOutlineShaderTests`: 수정 전 1개 실패/1개 통과, 수정 후 2개 통과. `Material.SetPass`의 bool만 검사하지 않고 예상하지 않은 네이티브 Assert까지 검사한다.
- 독립 새 머티리얼 재검사: 일반·외곽선 셰이더 모두 오류 0건. SRP Batcher 코드 0 유지.
- 실제 Play 진입 3회(Revamp 두 번, HighWay 한 번): 각각 **Warning/Error/Exception/Assert 0건**. 일반 정보 로그는 유지했다.
- 영상 네 장면 이동: frame 0/192/480/720. 이전 장면은 frame480으로 복귀, 건너뛰기 시 타깃 텍스처 해제, 다시 열기 시 첫 장면 재생 확인. 두 Revamp 실행에서는 39초 자동 종료도 확인했다.
- 기존 영상 시간 테스트 12개와 에디터 자동 재생 옵션 테스트 5개 통과. 신규 2개 포함 **총 19개 집중 검사 통과**.
- runtime/editor dotnet build 성공, harness 검사 및 패스 본문 동기화 검사 통과. runtime 빌드의 기존 경고와 실제 시작 경고는 구분한다.
- 모델·텍스처·외곽선 머티리얼 프로퍼티·외곽선 계산 코드가 이번 수정에서 바뀌지 않았음을 대조했다. HighWay 로비에서 730배치/83 SetPass를 관찰했으며 동일 순간의 전후 성능 비교로 주장하지 않는다.

## 실패한 검증 시도와 예방

새 테스트를 추가한 첫 재컴파일은 `triggered` / `isCompiling=true`에 머물고 타임아웃이 났다. 씬이 clean이고 Play가 중지된 것을 확인하고, 당일 저장값과 SessionState를 기록한 뒤 에디터를 재시작했다. 이후 컴파일과 회귀 검사가 정상 수행됐다. Library 삭제나 테스트 결과 재사용으로 통과 처리하지 않았다.

향후에는 SRP 호환성, 셰이더 컴파일 성공, 실제 새 머티리얼 바인딩을 별도 검사한다. 영상 인코딩 변경도 frame count만 확인하지 말고 실제 재생/seek 로그를 본다. MP4 원본을 바꾼 뒤 이전 작업 폴더의 백업으로 덮어쓰지 않는다.

## 기록

원시 로그·수정 전 파일·테스트 결과·동영상 인코딩/PSNR·시작별 로그: `outputs/startup-errors-2026-10-02/`. 당일 66개 저장 항목을 복원한 뒤 스냅샷 파일의 완전 일치를 확인했다(코인 40,228/보석 30). 세션 옵션과 원래 clean Revamp 씬/Edit Mode도 복원했다. 이 작업은 Unity 프로젝트의 시작 문제 수정이며 Android APK 재빌드나 실기기 테스트 결과를 포함하지 않는다.

기존 [외곽선 묶음 최적화 기록](../performance-issues/batch-legacy-flatkit-outline-with-render-objects-2026-10-01.md)의 `UsePass` 지침도 이번 수정에 맞게 갱신했다. 해당 문서는 성능 원인을, 이 문서는 키워드 상태와 검증 누락을 다룬다. AGENTS.md에 이미 지식 검색 규칙이 있어 추가 규칙은 필요하지 않았다.
