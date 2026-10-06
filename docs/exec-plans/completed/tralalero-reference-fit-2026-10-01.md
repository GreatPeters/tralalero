---
title: Tralalero reference fit, outline batching, rolling log and device build
date: 2026-10-01
status: completed
---

# 트랄랄레오 레퍼런스 맞춤·외곽선 묶음·통나무·실기기 빌드

사용자 요청과 해석 구분: `docs/design/USER_STATED_REQUIREMENTS.md` 2026-10-01 (Claude Code) 항목. 선행 작업은 `s22-performance-visual-polish-2026-10-01.md`(Codex)이며 그 결과를 검토한 후속 작업이다. 브랜치 `fix/s22-performance-visual-polish-20261001`, 커밋하지 않았다.

## 검토에서 찾은 부족한 점

| 항목 | S22 적용본 상태 | 근거 |
|---|---|---|
| 주인공 실루엣 | 꼬리 끝을 잘라 신발로 바꿔 꼬리지느러미가 없다. 앞다리가 몸 폭(±0.6)보다 넓게(±1.5) 벌어져 뒤 시점에서 덩어리처럼 보인다. | 사용자 레퍼런스, `player-final-*.png`, 정점 분석 |
| 표정 | 입선 끝이 아래로 처져 찡그린 얼굴. 레퍼런스는 이빨을 드러내고 웃는다. | 텍스처 입선 3D 역추적(y −2.7→−1.9, 끝 z 하강) |
| 고속도로 SetPass | 혼잡 구간 747배치에 SetPass 663. `FlatKit/Stylized Surface With Outline`(그리기 286개)이 SRP Batcher 비호환이고, 외곽선이 태그 없는 2번째 패스라 물체마다 본체와 번갈아 그려진다. | `highway-dense-07s.json` |
| 통나무 | 모델 피벗이 원통 바닥 모서리인데 피벗 기준 440°/s 회전 → 반 바퀴마다 최대 약 1.8m 도로에 박힘. ±18° 앞뒤 기울기, 진행 반대 방향 회전. | 사용자 제보, 실측 치수(지름 2.2m, 길이 5.1m) |
| 도착 영상 | 게임 모델로 촬영한 시장 도착·고속도로·휴게소 영상이 이전 주인공 형상. | 렌더 도구가 카탈로그 모델 사용 |

## 적용

1. **주인공 형상** — `tools/build-tralalero-reference-player.py`(Blender). S22 v5 빌더의 좌표·UV·웨이트·꼬리 신발 오프셋을 그대로 쓰되, 꼬리지느러미 아래 날개만 (1.88,0.93)→(2.42,1.21) 평면으로 잘라 그 단면을 꼬리 다리로 이어 세 번째 신발에 연결한다. 위 날개는 원본 그대로. 앞다리는 본 체인 가중치에 비례해 안쪽으로 최대 0.38 이동(신발 메시도 같은 함수). 16개 메시(기본·몸통·유료 신발 7종×2). `tools/import-tralalero-reference-player.cs`가 같은 GUID 자산에 덮어쓰고 이전본을 백업한다.
2. **웃는 입** — `tools/repaint-tralalero-grin.py`. 몸통 UV를 3D 위치로 래스터화(`tools/tralalero-texel-map.py`)해 기존 빨간 입선을 확산 채움으로 지우고, 측면 (y,z) 윤곽으로 입꼬리가 올라간 열린 입·위아래 이빨·외곽선을 8개 스킨 알베도에 그린다.
3. **외곽선 묶음** — `StylizedSurfaceOutline.shader`의 모든 패스를 FlatKit `StylizedInput.hlsl` CBUFFER로 통일(UsePass), 외곽선 패스를 같은 계산식의 HLSL `LightMode=OutlineLegacy`로 바꾸고 누락 CBUFFER 속성(`_DetailMap*`, 영향 0)을 선언. `tools/install-legacy-outline-feature.cs`가 Mobile/PC 렌더러에 RenderObjects(불투명, AfterRenderingOpaques)를 추가. `TemporarySceneryFade`도 이 패스를 끈다. 머티리얼 162개는 수정하지 않았다.
4. **통나무** — `HighwayChapter2Rules.RollingLogPose`: 원통 중심 기준 배치, 진행 방향으로 v/r 속도 회전, 기울기 ±4°와 그만큼의 끝단 여유, 도로면(+0.12) 기준 높이. `HighwayRollingLogTests` 추가.
5. **영상 재촬영** — 기존 S22 도구로 도착 3장면 재촬영(`S22Cinematics.RecordEntries`), `encode-s22-cinematics.py`, `S22InstallCinematics.Main`. 오프닝 앞 30초 원본 유지, 936/121/121프레임 계약 유지.

## 재현 명령

```
blender -b --factory-startup --python tools/build-tralalero-reference-player.py
unity command --project-path . run_script --file tools/import-tralalero-reference-player.cs --entry TralaleroReferencePlayerImport.Main
python tools/repaint-tralalero-grin.py install
unity command --project-path . run_script --file tools/install-legacy-outline-feature.cs --entry LegacyOutlineFeatureInstall.Main
python tools/run-tralalero-reference-tests.py TralaleroReferenceAnatomyTests HighwayRollingLogTests
```

Play 검증은 `tools/tralalero-reference-verification.cs`(Snapshot→Prepare→Play→Subject/Shoes/Stride/LogWatch→Restore). 렌더 검사는 `tools/probe-tralalero-highway-batches.cs`, 통나무 치수는 `tools/probe-tralalero-log.cs`, Blender 미리보기는 `tools/render-s22-player-views.py`.

## 되돌리기

- 메시: `TralaleroReferencePlayerImport.Restore`(백업 `outputs/tralalero-reference-2026-10-01/before/Player`).
- 입: `python tools/repaint-tralalero-grin.py restore`.
- 셰이더/렌더러: `before/FlatKit`, `before/Settings`의 원본으로 복사.
- 영상: `before/cinematics-v2`, `before/S22Polish-UI`.

## 검증 결과

- 렌더(같은 고속도로 시작+7초): 747배치·281k 삼각형 동일, **SetPass 663→100**. 셰이더 SRP Batcher 코드 10/19→0. 외곽선은 차량·나무·주인공에 그대로 보인다. 노량진 로비 120배치/SetPass 53→50, 달리기 중 최대 348배치/SetPass 104.
- 통나무: 활성 823~858프레임 4회 실행 모두 최저점이 도로면 위(최악 −0.012~−0.018m, 음수=도로 위). 측면·게임 화면 캡처 확인. 첫 시험은 거리만 순간이동해 플레이어 차로가 980m 튀었으므로(테스트 부작용) 플레이어도 경로로 옮기도록 수정했다.
- 주인공: Unity 실제 렌더 6면, 신발 8종 장착, 보행 14프레임에서 꼬리 다리 늘어짐·관통 없음. 메시 +1,196 삼각형.
- 테스트: TralaleroReferenceAnatomy 4, SharkTailFootRig 1, CosmeticShopIntegration 4, MobileRenderingQuality 2, HighwayRollingLog 2, HighwayChapter2Rules 16, HighwayHazardRules 1, GeneratedStylizedSurface 3, HighwayCombatRevision 10, NoryangjinCameraOcclusion 11, HighwayChapterIntegration 1, HighwayRebuildContract 3, HighwayThreeLaneTraffic 22 통과. MonsterGrowthAndMapToolEnemy 41개 중 2개 실패(`Enemy_Guard` 스케일 0.45≠0.8, 맵툴 탭 5≠4) — 이번 변경과 무관한 기존 실패(`Enemy_Guard.prefab`은 9/22 커밋 이후 미수정).
- `dotnet build Assembly-CSharp.csproj` 성공. 저장 레지스트리는 Play마다 바이트 일치 복원(코인 40,228 / 보석 30).

## 빌드와 실기기 설치

- `tools/build-tralalero-reference-android.cs:TralaleroReferenceAndroidBuild.Main false` (S22 빌드 도구와 같은 흐름, 새 출력 경로). ARM64 일반 APK **성공**, 오류 0·경고 58, 152.8초.
- `Builds/Android/TralaleroShooter-20261001-Reference-release.apk`, 752,905,082 bytes, SHA-256 `2be301d9ac4c25a46230fa6131f4c6839e85fecaabf52f5e621952e4b4ae87ab`. 씬: Revamp·HighWay·RestStop.
- 빌드 후 `ProjectSettings.asset`이 빌드 전 사본과 바이트 일치(서명/아키텍처/번들 설정 복원).
- SM-S901N(R5CT60Y9RSM)에 `adb install -r` 성공. 설치된 base.apk SHA-256 일치. firstInstallTime(2026-10-01 00:18:44) 유지 = 제거·데이터 삭제 없음. 이전 설치본은 9/30 아이콘 빌드였고 S22 APK는 설치된 적이 없었다.
- `am start -W`: Status ok, COLD, 581ms, 프로세스 유지. logcat에 앱 FATAL/Unity 오류 없음. 휴대폰이 잠금(keyguard) 상태라 실제 게임 화면·FPS·발열은 확인하지 못했다.

## 채택하지 않은 시도

- 유료 신발 7종(각 약 4.1만 삼각형) 22% 감량: 상점 근접에서 밑창 각짐·텍스처 번짐이 보여 폐기. 증거 `outputs/tralalero-reference-2026-10-01/rejected/`. 플레이어 신발은 S22 GPU 기준 전체 프레임의 일부라 품질 위험이 더 컸다.

## 남는 한계와 주의점

- 뒤 시점에서 꼬리지느러미는 세로선으로 보인다(실제 상어의 정후면 실루엣). 게임 카메라 구도는 바꾸지 않았다.
- 입은 텍스처 그림이라 입을 실제로 벌리는 애니메이션은 없다.
- `OutlineLegacy`는 렌더러 기능에 의존한다. 새 렌더러/품질 단계를 추가하면 `install-legacy-outline-feature.cs`를 다시 실행해야 외곽선이 보인다.
- 오프닝 앞 30초(원본 영상)의 주인공은 여전히 예전 화풍이다.
- `encode-s22-cinematics.py`는 S22 미리보기 시트를 덮어쓴다. 이번에는 백업 프레임으로 원본 시트를 복원했다.

## 후속 2026-10-02: 오프닝 네 번째 장면 원복

사용자가 오프닝 네 번째 장면이 이상하다며 기존 장면으로 되돌려 달라고 했다. `tools/restore-original-opening-scene4.cs`로 세 씬(Revamp·HighWay·RestStop)의 `OpeningStoryUI.movie`를 `Assets/JH/UI/Opening/Animated/Curse_Opening_Animated.mp4`, `pages[3]`을 `Story_04.png`로 되돌렸다(HEAD 커밋의 원래 바인딩과 같음). 원본도 936프레임/39초라 0·8·20·30초 장면 규칙은 그대로다. 고속도로·휴게소 진입 영상(`Highway_Aligned`/`RestStop_Aligned`)은 유지했다. `Assets/JH/UI/S22Polish/Opening_Aligned.mp4`와 `Market_Arrival.png`는 참조 없이 파일만 남아 빌드에 들어가지 않는다.

- 검증: MapToolOpeningVideoTests 5, OpeningMovieTimingTests 12 통과. Play에서 4번째 페이지로 넘겨 원본 클립 779·894프레임이 재생되는 화면 확인(`opening-scene4-restored-a/b.png`).
- 재빌드: `TralaleroReferenceAndroidBuild.Main false opening4` → `Builds/Android/TralaleroShooter-20261001-Reference-release-opening4.apk`, 754,384,909 bytes, 오류 0·경고 58, SHA-256 `3dcc269467728c679d8aa00317fbda17a810ad40c87738f8951481ebabafc907`. ProjectSettings 바이트 일치 복원.
- SM-S901N `adb install -r` 성공(2026-10-02 00:29:13), base.apk 해시 일치, firstInstallTime 유지. `am start -W` Status ok·COLD·792ms, 앱 크래시 로그 없음. 휴대폰 잠금 상태로 화면은 미확인.
