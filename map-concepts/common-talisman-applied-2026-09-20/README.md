# 접힌 부적 · 모든 Bonus 몸통 흡수 적용

**시각 품질 거절 및 대체:** 사용자가 이 첫 적용본의 외형과 약한 효과를 거절했다. 기능 검증 기록은 유지하지만, 시각적 완성도의 근거로 삼지 않는다. 최신 모델·효과·실제 플레이 영상은 [재제작 기록](../talisman-polish-2026-09-20/README.md)을 따른다.

2026-09-20. 사용자 요청에 따라 시안 `03. 접힌 부적 · 모든 강화는 몸으로`를 실제 Unity 프로젝트에 적용했다. 작업 브랜치: `feat/common-bonus-talisman`.

## 적용 결과

- 보너스 프리팹 21개와 노량진·고속도로·휴게소의 배치 보너스 각 50개, 총 150개에 공통 부적을 저장했다. 이전 제단·별/기둥 효과·겹치는 UI는 숨겼다. 적 사망 드롭과 이후 맵툴 재생성도 공통 경로를 사용한다.
- 얇은 입체 종이 3판, 남색 인쇄 테두리·집게, 작은 금속 리벳으로 부적 모델을 만들었다. 프로젝트의 모든 BuffType 아이콘 및 실제 표시명/수치를 연결했다.
- 획득 시 보상을 즉시 한 번 적용하고, 별도 시각 루트에서 약 0.68초 동안 펼침 → 아이콘의 몸통 이동 → 작은 금빛 발광 → 정리를 재생한다. 모든 종류가 동일한 몸통 앵커를 사용한다.
- 공격/체력/연사/사거리/추가 탄환/지원군의 실제 효과, 등급·랜덤 선택·좌우 택일·2초 획득 간격은 유지했다. NerfWall은 기존 표시를 유지한다.
- 외부 서비스나 패키지는 추가하지 않았다. Unity CLI/Pipeline, 직접 생성한 메시와 기존 FlatKit·폰트·아이콘을 사용했다. 모델 생성용 ImageGen/로컬 LLM/Higgsfield/GitHub 다운로드는 이번 구현에 필요하지 않았다.

## 소유 파일

- `Assets/ShooterSurvival/Scripts/Walls/BonusTalismanPresentation.cs`: 종류별 콘텐츠 연결, 기존 표시 억제, 재사용 및 중복 획득 연출 가드.
- `BonusTalismanVisual.cs`: 카메라 방향·세계 크기 보정, 대기 흔들림, 판 펼침.
- `BonusTalismanPickup.cs`: 원본 비활성화 이후에도 유지되는 분리 효과, 몸통 추적, 종료/사망/재시작 정리. 보상 코드 없음.
- `Assets/ShooterSurvival/Editor/BonusTalismanInstaller.cs`: 공유 메시/재질/스프라이트/프리팹 생성 및 모든 적용 대상 저장.
- `Assets/ShooterSurvival/Resources/BonusTalisman/`: 재생성 가능한 모델·재질·SoftGlow·프리팹.
- WallScript는 표시 갱신/획득 훅/잘못된 롤의 표시 차단만 확장했다. FeastOfFortuneWallSetup은 재생성 후 새 표현을 적용한다.
- 공유 Gmarket SDF에는 새 표시 문구에 필요한 동적 글리프가 추가되었다. 관련 재질은 기존 FlatKit outline 렌더러에 등록된다.

## 검증

- `dotnet build Assembly-CSharp.csproj -nologo -v:q`: 성공. 기존 타사 SplineSpeed 경고 외 새 런타임 경고 없음.
- `dotnet build Assembly-CSharp-Editor.csproj -nologo -v:q`: 최종 성공, 0 오류.
- `powershell -ExecutionPolicy Bypass -File tools/validate-agent-harness.ps1`: 통과.
- Unity EditMode **37/37 통과**: BonusTalismanTests 7, BonusWallChoicePairTests 6, BonusWallCooldownTests 6, BonusAltarRulesTests 18. 각 JSON 결과가 이 폴더에 있다.
- Play Mode 실제 OnTriggerEnter 경로: 일반 6계열·추가 탄환·두 지원군, 정확한 단일 보상, 원본 숨김, 분리 효과 지속/종료, 실제 EnemyScript_space.SpawnBonusAltar 드롭, 재활성화 및 같은 프레임의 Player.ResetState 정리를 통과했다. [기록](runtime-Noryangjin_MapTool_Mode_SR18.txt).
- 모든 Wall 프리팹 **23개**(Nerf 포함)의 기존 보상 필드를 HEAD와 대조했다. Unity가 새로 직렬화한 기존 C# 기본값을 정규화했으며 실질 값 변경은 없다. [기록](preservation.json).
- 세 씬을 다시 열어 각 50개 부적 연결·옛 제단 숨김·clean 상태를 확인했다. [씬 감사](scene-audit.txt).
- [코드 검토](review.md): 순차 검토 및 발견 사항 수정. 해결되지 않은 작업 항목 없음.

## 실제 화면

**http://127.0.0.1:6753/talisman-applied/** — 세 맵 × 대기/펼침/흡수/몸통 발광/완료, 총 15장.

Chrome에서 갤러리의 세 씬 목록과 고속도로 원본 확대 표시를 확인하고 열람 탭을 유지했다.

원본: `tmp/image-previews/common-talisman-applied-2026-09-20/final/<Scene>/`.
화면은 실제 Unity Editor 렌더다. 시각 단계를 비교하려고 시간을 정지한 뒤 효과 시간을 지정하여 찍었다. 실제 획득·보상·시간에 따른 종료는 별도 Play Mode 검증 기록으로 확인한다. 추가로 배치한 검사용 부적은 Play Mode 종료와 함께 사라지며 저장된 맵의 좌표는 바꾸지 않았다.

## 재생성·확인

```powershell
unity pipeline list --format json
unity command --project-path . list_open_scenes --format json
unity command --project-path . run_script --file tools/apply-common-bonus-talisman.cs --timeout_ms 120000 --format json
powershell -ExecutionPolicy Bypass -File tools/test-common-talisman.ps1
python tools/verify-talisman-preservation.py
python map-concepts/common-talisman-applied-2026-09-20/build-gallery.py
```

적용 도구는 Play Mode나 dirty 씬에서 실행을 거부한다. 생성기는 기존 GUID를 유지하며 파일을 갱신한다. Unity 인증 endpoint는 CLI가 탐색하며 포트를 고정하지 않는다.

## 범위와 남은 검증

Unity Editor에서 구현·검증했다. 새 Android 빌드/실기기 프레임 시간은 이번에 측정하지 않았다. 네이티브 셰이더·기존 아이콘을 사용한 실제 모델이므로 생성 이미지의 광택이나 캐릭터 외형을 픽셀 단위로 복제한 것은 아니다. 기존 사용자의 미커밋 작업은 보존했고 최종 Editor는 원래 SR18 씬의 clean Edit Mode로 돌려놓았다.
