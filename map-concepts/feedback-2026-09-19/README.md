# 설정·상인·삽·보너스 수정 — 2026-09-19

사용자가 첨부한 네 가지 실제 화면 문제를 현재 Unity 프로젝트에 적용했다.

## 적용

- **설정**: 승인된 `tmp/image-previews/harbor-ui-all-2026-09-15/1/02-combat-settings-results.png`를 비교했다. Jua 제목/본문, 큰 아이콘, 두꺼운 파랑·은색 테두리, 노란 토글과 큰 슬라이더 손잡이로 교정했다. 기존 소리·진동·음량·감도·닫기·계속하기·재도전 바인딩을 유지한다. 세 챕터 공통으로 저장했다. 법적 URL이 없는 버튼은 여전히 실제로 비활성이다.
- **상인 팔**: 소매 안쪽을 몸통에 고정하던 넓은 가중치 마스크가 인사 중 긴 삼각형을 만들었다. 앞치마/몸통 보호 영역을 좁히고, UV 이음새를 용접 좌표 기준으로 묶어 가중치를 완화했다. 최대 네 영향을 정규화하며 인사 시 상완 회전을 95°에서 55°로 줄였다. 기존 FBX GUID, 머터리얼, 앉기/인사 컨트롤러를 유지한다.
- **삽**: 손을 중심으로 삽의 회전과 오프셋을 함께 수정하고 10% 줄였다. 대기 자세에서 삽날이 다리 바깥에 오며 손과 연결된 상태를 유지한다. 원본 `Enemy_OldMan.prefab`와 실제 삽 모델을 가진 씬 오브젝트에 적용했다. 총 20개 원본/인스턴스에 적용했으며 이름만 OldMan이고 다른 모델을 쓰는 적은 대상에서 제외했다.
- **보너스**: 초록 마법 구체/연기 대신 Cartoon FX의 별과 작은 빛 머터리얼로 새 파티클 구성을 만들었다. 대기 효과는 작게 반짝이고, 성공적인 획득 때 상어 몸 위로 금빛 별이 퍼진다. 반복 호출은 중복 효과를 만들지 않으며 선택하지 않은 제단에도 획득 효과를 만들지 않는다. 각 시스템은 최대 32개, 총 두 시스템이다.

## 실제 확인 이미지

- [설정](../../tmp/image-previews/feedback-2026-09-19/accepted/01-settings.png), [일시정지](../../tmp/image-previews/feedback-2026-09-19/accepted/02-pause.png)
- [상인 인사](../../tmp/image-previews/feedback-2026-09-19/source-poses/merchant-StandGreet-1.2.png)
- [삽 대기](../../tmp/image-previews/feedback-2026-09-19/source-poses/shovel-idle-0.0.png), [걷기](../../tmp/image-previews/feedback-2026-09-19/source-poses/shovel-walk-0.6.png), [공격](../../tmp/image-previews/feedback-2026-09-19/source-poses/shovel-attack_loop-0.6.png)
- [보너스 접근](../../tmp/image-previews/feedback-2026-09-19/accepted/05-bonus-approach.png), [획득](../../tmp/image-previews/feedback-2026-09-19/accepted/06-bonus-collected.png)

`source-poses`는 손과 소품 연결을 점검하기 위해 멀리 이동한 플레이어를 향하는 머리 IK만 임시로 끈 Play Mode 포즈 캡처다. IK 변경은 저장하지 않았다. 캐릭터 클로즈업은 검사용 카메라이며, 설정과 보너스 이미지는 실제 1080×2340 Game View다. `final`의 삽/보너스 이미지는 검사용 카메라를 잘못 복원했던 폐기 증거다. 최종 이미지는 위 링크와 `accepted`, `source-poses`를 사용한다.

## 검증

- Native Edit Mode **27/27**: 신규 presentation 5, Harbor refinement 10, bonus choice 6, bonus cooldown 6. 결과 JSON은 이 폴더의 `tests-*.json`.
- 실제 버튼/슬라이더 상태 전환, 일시정지 복귀, 플레이어 충돌을 통한 보너스 선택, 미선택 제단 제거, 중복 burst 방지, 2.5초 후 정리: `runtime-checks.txt`, **10개 확인 통과**. 최종 획득에서 체력 500 → 508.
- `dotnet build Assembly-CSharp.csproj -nologo`, `dotnet build Assembly-CSharp-Editor.csproj -nologo`, `tools/validate-agent-harness.ps1` 통과. 상세 로그는 이 폴더에 보관.
- 상인 Blender 4.4 작성본 및 GLB 신선한 재임포트: 둘 다 기술 gate 통과. 23,480 triangles, 18 bones, 두 action을 유지했다. 실제 Unity FBX의 앉기/인사와 앞·뒤·옆 Blender 렌더를 확인했다.
- `ChapterPlaytestPreferences`로 추적한 66개 키 복원, 코인 241/보석 0. 설정 토글/슬라이더 값도 테스트의 finally에서 원래 값으로 복원했다. 최종 SR18 저장 씬으로 돌아오고 Play Mode를 종료했다.
- 에디터 검증 범위이며 APK/실기기 배포는 수행하지 않았다.

## 재현과 소스

편집 상태에서 씬을 저장한 뒤 공식 `unity command --project-path . eval_file --file <script>`로 아래 public entry를 호출한다.

```csharp
return HarborGameUIInstaller.ApplyFaithfulSettingsAll();
// 별도 호출
return HarborCharacterPresentationRepair.ApplyShovelGrip();
// 별도 호출
return HarborBonusPresentationInstaller.RebuildRewardEffects();
```

상인은 `tools/rig-harbor-merchant.py`를 Blender 4.4 CLI로 실행하고 생성한 `Merchant.fbx`를 기존 `Resources/HarborRefinement/Merchant/Merchant.fbx`에 복사/재임포트한다. 메타파일을 교체하지 않는다.

- [작성 Blender](../../outputs/feedback-2026-09-19/merchant-rigged/Merchant.blend), [인사 포즈 Blender](../../outputs/feedback-2026-09-19/merchant-rigged/Merchant-Greeting.blend)
- [FBX](../../outputs/feedback-2026-09-19/merchant-rigged/Merchant.fbx), [GLB](../../outputs/feedback-2026-09-19/merchant-rigged/Merchant.glb)
- [작성본 metrics](../../outputs/feedback-2026-09-19/merchant-authored-metrics.json), [재임포트 metrics](../../outputs/feedback-2026-09-19/merchant-glb-metrics.json)
- [멀티뷰](../../tmp/image-previews/feedback-2026-09-19/blender-authored/contact_sheet.png), [동작 단계](../../tmp/image-previews/feedback-2026-09-19/blender-authored/animation_contact_sheet.png)

`tools/verify-feedback-runtime.cs`는 새 Play Mode에서 실행한다. 기존 증거 폴더가 있으면 이름을 바꾸고 실행한다. `tools/capture-feedback-poses.cs`는 실제 애니메이터를 샘플링하므로 검증용 Play Mode에서만 실행하고 끝나면 Play Mode를 종료한다. 네이티브 테스트 전에는 임시 오브젝트로 dirty가 된 씬을 저장본으로 다시 열어 저장 확인 대화상자가 테스트를 막지 않게 한다.

`ce-compound mode:headless`로 기존 관련 학습 문서에 후속 원인과 예방법을 추가했다: [native frame validation](../../docs/solutions/ui-bugs/verify-mobile-sdf-and-world-presentation-in-native-frames-2026-09-16.md).
