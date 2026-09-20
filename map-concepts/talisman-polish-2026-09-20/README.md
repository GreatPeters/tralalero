# 접힌 부적 · 시안 기준 재제작

2026-09-20. 사용자가 실제 적용본의 디자인과 약한 효과를 거절하고 시안에 맞는 제작을 요청했다. 기존 테스트 통과가 시각적 완성도를 입증하지 못했음을 인정하고, 이전 소스·자산·네이티브 프레임을 보존한 뒤 모델과 효과를 다시 만들었다.

## 결과와 확인 링크

**실제 플레이 영상 / 이전·개선 / 모델 확대:** http://127.0.0.1:6753/talisman-polished/

- [항구 영상](../../tmp/image-previews/talisman-polish-2026-09-20/video/Noryangjin_MapTool_Mode_SR18.mp4)
- [고속도로 영상](../../tmp/image-previews/talisman-polish-2026-09-20/video/HighWay.mp4)
- [휴게소 영상](../../tmp/image-previews/talisman-polish-2026-09-20/video/RestStop.mp4)
- [Blender 모델 확대](../../outputs/talisman-polish-2026-09-20/hero.png), [재질 없는 형상](../../outputs/talisman-polish-2026-09-20/neutral.png), [재수입 다각도](../../outputs/talisman-polish-2026-09-20/reimport-views/contact_sheet.png).

영상은 물리 충돌로 실제 Bonus를 획득한다. OnTriggerEnter를 영상 도구에서 직접 호출하지 않았다. 30fps 고정 타임스텝으로 120프레임(4초), 무음으로 캡처했다. 획득 프레임은 항구 41, 고속도로/휴게소 50이며 각각 30프레임에서 효과가 관측되었다. 고정 타임스텝 녹화는 실기기 FPS 측정이 아니다. Chrome에서 세 영상의 재생과 프레임 표시를 확인했다.

효과를 명확히 관찰하기 위해 녹화 중 적과 사격을 잠시 껐다. 플레이어 이동·물리 충돌·보상 적용·효과 업데이트는 실제 경로이며 이 검사용 상태는 씬에 저장하지 않았다.

## 달라진 부분

1. **모델:** 길쭉한 종이 비율, 분리된 옆면, 실제 두께·베벨·인쇄 모서리 문양, 굽은 남색 에나멜 집게와 은색 리벳. Blender에서 직접 제작했다.
2. **문양:** 공격·체력·연사·사거리·추가 탄환은 돌출된 메시 문양으로 교체했다. 두 지원군의 기존 인물 아이콘은 정체성 보존을 위해 유지한다.
3. **재질/문구:** FlatKit의 부드러운 음영과 제한된 반사를 적용했다. 수치판은 별도 무광 남색으로 분리해 특정 조명에서 하얗게 뜨지 않도록 했다. 기존 UI 참조가 없는 구형 Bonus에도 실제 계산된 값이 표시된다.
4. **효과:** 대기 시 가까운 부적에 약한 후광/점광. 획득 후 몸 옆으로 살짝 뜨며 펼쳐짐 → 밝은 중심선과 잔광을 가진 곡선 이동 → 몸통의 빛·고리·작은 광점·획득 문구 → 전체 정리. 모든 종류가 같은 몸통 앵커를 사용한다.
5. **가시성:** 짧은 흡수 궤적만 깊이 검사 Always를 사용해 캐릭터에 가려지는 문제를 해결했다. 나머지 후광과 모델은 일반 깊이 검사를 유지한다. 약 1.02초의 피드백이며 보상과 조작은 기다리지 않는다.

21개 Bonus 프리팹, 세 씬 각 50개 총 150개 배치 및 드롭·재생성 경로에 재적용했다. 공격력/체력/지원군 등 실제 보상·희귀도·좌우 선택·쿨다운은 유지한다. 이전 아트는 비활성 상태로 남아 연결을 안전하게 대체하며, 원본 백업은 `outputs/talisman-polish-2026-09-20/before/`다.

## 제작 소스와 산출물

- [Blender 제작 스크립트](../../tools/build-polished-talisman.py)
- [편집용 Blender 장면](../../outputs/talisman-polish-2026-09-20/Talisman.blend)
- [모델만 담은 Blender 파일](../../outputs/talisman-polish-2026-09-20/TalismanAsset.blend)
- [GLB](../../outputs/talisman-polish-2026-09-20/Talisman.glb)
- [Unity용 FBX](../../outputs/talisman-polish-2026-09-20/TalismanBody.fbx), 같은 폴더의 `WallBonus_*.fbx/.glb` 5종.
- [원본 상세 지표](../../outputs/talisman-polish-2026-09-20/source-metrics-full.json), [GLB 재수입 지표](../../outputs/talisman-polish-2026-09-20/reimport-metrics.json).
- [반복 검토 기록](iteration_review.json), [초기 요구·실패 판정](contract.md).

Blender 4.4.0 사용. 기본 공격 문양을 포함한 모델은 **3,756 triangles / 8 meshes**이며 원본·재수입의 치수 **1.475888 × 0.715456 × 2.458720**이 일치한다. 무효 좌표·퇴화 면 0, UV 메시 8. UV/노멀 분리 및 여러 부품 때문에 재수입 topology의 경계 에지는 존재하며 3D 프린팅용 수밀 모델로 주장하지 않는다. [같은 스튜디오 카메라·조명의 재수입 렌더](../../outputs/talisman-polish-2026-09-20/reimport-matched.png)도 확인했다.

Trellis는 허용된 선택지였으나 이번에는 분리된 접힘 피벗과 문양을 직접 제어하는 로컬 Blender 제작을 사용했다. Unity 제어는 공식 CLI/Pipeline이다. 영상 인코딩에 로컬 `imageio-ffmpeg`를 사용했으며 Unity 런타임 패키지를 추가하지 않았다.

## 코드와 검증

- `PolishedTalismanAssets`가 FBX 부품·재질·문양·수치판·VFX 재질을 묶어 공통 프리팹을 생성한다. 기존 `BonusTalismanInstaller`가 모든 적용 경로에 배포한다.
- `BonusTalismanVisual`은 실제 메시 문양 또는 기존 지원군 초상을 선택한다. `BonusTalismanPickup`은 별도 효과 루트에서 움직임을 재생하며 기존 종료/재시작 가드를 유지한다.
- **39/39 EditMode 통과:** BonusTalismanTests 9, ChoicePair 6, Cooldown 6, AltarRules 18. 이 폴더의 `tests-*.json` 참조.
- [Play Mode 보상·드롭·지원군·재시작 검사](runtime-Noryangjin_MapTool_Mode_SR18.txt) 통과.
- [23개 프리팹의 보상 필드 보존](preservation.json), [150개 저장 배치 감사](scene-audit.txt) 통과.
- 런타임/에디터 dotnet build와 `tools/validate-agent-harness.ps1` 통과. 마지막 확인에서 Unity 오류 로그 0. SR18 clean Edit Mode로 복원했다.

코드/리소스 검토는 AGENTS.md에 따라 메인 스레드에서 순차 실행했다. 레거시 수치 참조, 반사에 의한 수치판 소실, 캐릭터에 가려지는 궤적, 이동 중 부적/캐릭터 겹침을 수정하고 최종 영상에서 재확인했다. 해결되지 않은 구현 차단 항목은 없으며, 시각 판단은 작성자 검토임을 `iteration_review.json`에 명시했다.

재생성 순서: Blender 제작 스크립트 실행 → 생성한 FBX들을 `Assets/ShooterSurvival/Resources/BonusTalisman/Polished/`에 복사 → Unity에서 `tools/apply-common-bonus-talisman.cs` 실행. 네이티브 영상은 Play Mode에서 `tools/record-talisman-play.cs`, 인코딩은 `python tools/encode-talisman-video.py`, 갤러리는 `python map-concepts/talisman-polish-2026-09-20/build-gallery.py`.

## 검토 범위

형상·재질·움직이는 게임 화면을 각각 검토했다. 시안의 모든 조명/질감을 픽셀 단위로 복제했다고 주장하지 않는다. 뒷면은 시안에 없으므로 단순 종이로 작성했다. Android 실기기 성능 검증은 아직 하지 않았다. 기존 다른 작업과 게임 밸런스는 보존했다.
