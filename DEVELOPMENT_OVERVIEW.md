# 트랄랄레로 슈터 개발 문서

2026-09-10 릴리스 작업: 노량진 맵툴 편의에 공·체 9999 및 좌우 입력 4배 체크를 추가했다. 노량진 두 도구 씬에 참조 이미지 기반 업그레이드 화면과 피부/신발/모자 구매·착용·미리보기 상점을 연결했다. Data.xlsx는 성장/가격 조정 시트와 외형 12종을 포함하며 보호된 Data.bytes도 갱신됐다. 고속도로 자동 전환은 미구현이고, 빌드의 Map1 원본 씬은 이 작업에서 변경하지 않았다. [검증과 제한](map-concepts/noryangjin-release-2026-09-10/README.md).

2026-09-10 후속: SR18의 기믹 전봇대8개는 고정 피해 장애물, 뚱보3명은 중앙 진입으로 수정했다. 좌우 Normal 군집2곳을 추가하여 적27명/엑셀76행이며, Bonus Wall 공통 획득 제한은2초다. 기존 프리팹 연결과 데이터 원본을 유지했다. [적용·검증 기록](map-concepts/sr18-contact-pairs-2026-09-10/README.md).

2026-09-10: 실플레이 결함4개(탄환 중복 반납, 고가도로 시야, 기존 가로등 피해, 출구 누락)를 수정했다. 출구는 STAGE CLEAR와 현재 씬 다시하기까지 연결하며 고속도로 전환은 여전히 별도 개발 범위다. [수정·검증 기록](map-concepts/sr18-runtime-fixes-2026-09-10/README.md).

2026-09-09 전투 보완: 적 배치 행이 공격력·체력·타입을 직접 소유하며 기존 몬스터 성장으로 덮어쓰지 않는다. 지원군 보너스는 노량진 소환기를 공유하고, TungTung은 자신만의 코너 순서와 도로 높이로 전진한다. 정확한 시선 추적, 원거리 Walk 진입, 바구니별 사격 금지 소유권도 적용했다. [현재 적용·검증 기록](map-concepts/sr18-combat-polish-2026-09-09/README.md).

2026-09-09: SR18 고정 보너스 25지점을 좌우 25쌍/50개로 전환했다. `BonusWallChoicePair`는 한쪽 선택 후 다른 쪽 보상을 차단하고, `EnemyCornerClearance`는 코너 뒤 적 28유닛/발동 20유닛의 여유를 지킨다. 엑셀 25개 보너스 행이 두 제단을 함께 제어하며 처치 드롭은 유지한다. [적용 기록](map-concepts/sr18-choices-and-corner-space-2026-09-08/README.md).

2026-09-07 추가: SR18 적·고정 보너스·기믹 74개를 `Data.xlsx`의 `적 배치/보너스 배치/기믹 배치`에서 설정한다. 기존 몬스터 성장·보너스 효과 시트와 보호 데이터 경로를 재사용하며 배치 동작은 다음 Play에 적용한다. 체력 재로딩 동기화도 보정했다. [설정 방법과 검증](docs/noryangjin-encounter-workbook.md)을 따른다.

이 문서는 프로젝트의 세 기준 문서 중 `개발 문서`다. 기획을 실제 Unity 프로젝트로 구현하는 구조, 현재 상태, 제작 절차와 검증 경로를 안내한다.

## 문서 경계

- 세계관, 캐릭터의 목적, 핵심 루프, 행동 규칙과 콘텐츠 방향은 [기획서](GAME_DESIGN_OVERVIEW.md)에서 관리한다.
- 정확한 시간·수치·가격·효과와 밸런스 근거는 [밸런스 문서](BALANCE_OVERVIEW.md)에서 관리한다.
- 구체적인 경로, 구역, 전투, 기믹과 맵 제작 규칙은 [맵 기획서](MAP_DESIGN_OVERVIEW.md)에서 관리한다.
- 이 문서는 현재 구현 수량·상태, 구현 위치와 개발 흐름의 진입점이며, 상세 내용은 가까운 하위 문서에 둔다.

## 개발 진입점

- 코드와 런타임 계층: [ARCHITECTURE.md](ARCHITECTURE.md)
- 스테이지 공간 제작 기준: [맵 기획서](MAP_DESIGN_OVERVIEW.md)
- 세부 개발 문서 인덱스: [docs/README.md](docs/README.md)
- 진행 중인 실행 계획: `docs/exec-plans/active/`
- 재사용 가능한 해결 기록: `docs/solutions/`

## 기술 구성

- 엔진: Unity `6000.2.6f1`
- 렌더링: Universal Render Pipeline `17.2.0`
- 편집기 자동화: `com.unity.pipeline 0.6.0-exp.1`
- 데이터: Editor 전용 `Data.xlsx` → 서명·암호화된 `Resources/GameData/Data.bytes`
- 분석: Firebase App/Analytics `13.14.0`과 로컬 대기열·라운드 체크포인트
- 테스트: `Assets/Tests/Editor`의 EditMode 중심 검증
- 현재 Build Settings: `Noryangjin_MapTool_Mode` 한 씬만 활성화

## 런타임 시스템 지도

| 영역 | 주요 구현 | 현재 상태 |
| --- | --- | --- |
| 게임 흐름 | `GameManager`, `CanvasScript`, `TimeManager` | Tap-to-play, 죽음·리셋, 일반 스테이지 인덱스 증가는 구현. 새 2스테이지 캠페인 전환은 미연결 |
| 플레이어 | `PlayerScript`, `WeaponManager` | 자동 전진, 횡이동, 체력, route-relative 이동 구현 |
| 무기·투사체 | `WeaponScript`, `BulletScript`, `BulletPooler` | 자동 사격, 풀링, 플레이어 회전에 따른 비행 중 궤도 전환 구현 |
| 노량진 경로 | `NoryangjinTurnSpot` | 절대 yaw 회전, 회전 중 이동 정지, 다음 런 초기화 구현 |
| 적 | `EnemyScript_space`, `EnemyEventController`, `EnemyEventActivationSpot` | 여섯 Forward 적 프리팹과 이벤트 모드, 트리거 연동 구현 |
| 적 성장 | `ChapterEnemyProgression`, `ChapterEnemyStatController`, `AllStageEnemyStats` | 챕터·등급·배치 순서 보간 구현. 시간 기준 30/31초 곡선은 미구현 |
| 장애물 | `ObstacleStats`, `ObstaclePrefabs` | 8개 복합 기믹 프리팹과 풀 설정 구현 |
| 보너스 | `AuthoredBonusWall`, `WallScript`, `BonusChoiceAltarVfx` | 단일 제단 프리팹, 등급별 Data.xlsx 롤, 적 드랍 구현 |
| 기본 업그레이드 | `UpgradeUI`, `UpgradeStatManager`, `UpgradeTables` | 코인·보석 구매, PlayerPrefs 저장, 스탯 적용 구현. 연쇄 잠금은 아직 `unlocked = true` |
| 스킨 | `SkinTables`, `SkinRowParser`, `SkinBonusResolver` | 스킨 데이터 파싱·보너스 계산 구현. 상점 전체 연결은 씬에서 계속 검증 필요 |
| 게임 데이터 | `GameDataWorkbook`, Editor build preprocessor | 자동 리로드, 서명·암호화, 빌드 전 검증 구현 |
| 분석 | `GameplayRunTracker`, `FirebaseAnalyticsRuntime` | 라운드 시작·종료·코인·진행도·업그레이드 스냅샷 구현 |
| 맵 제작 | `NoryangjinMapToolWindow` | 오브젝트·적군·기믹·보너스·정보 탭, 배치 수량 집계, 복사·붙여넣기, 회전·발동 스팟, 50~1200 작업 범위 구현 |

## 현재 노량진 구현 현황

2026-09-02 열린 `Noryangjin_MapTool_Mode`를 Pipeline으로 읽기 전용 실측한 기준:

- 도로 모듈 51개
- 회전 스팟 5개
- 경로 약 564 units
- 평면 중심선 기준 현재 `동18` 구간과 마지막 `남8` 구간이 기존 작성분 안에서 한 번 교차
- 씬은 Excel 캐릭터 기본값을 사용하며, 현재 `Data.xlsx`의 `playerSpeed`는 8 units/s
- 현재 경로는 회전 정지 전 약 70.5초, 0.5초 회전 5회를 포함하면 약 73초
- 최근 실제 이동 관찰값은 13.5모듈에 약 17.5초, 모듈당 약 1.296초
- 최신 제작 기준은 현재 51개 뒤에 179개를 추가한 총 230개 모듈
- 최근 관찰값을 선형 환산한 230개 이동시간은 약 298.1초이며 회전 정지는 별도
- [맵 기획서](MAP_DESIGN_OVERVIEW.md)의 기준안과 SUPER RADICAL 입체 교차 30안은 모두 총 230개로 갱신
- 직접 배치된 배경·시장 소품 530개
- 적 루트 3개, 적 발동 스팟 1개
- 회전 스팟 5개, `ObstacleStats` 장애물 4개
- 보너스 루트 2개, `AuthoredBonusWall` 컴포넌트 4개

실측 당시 씬은 저장되지 않은 변경이 있는 dirty 상태였다. 분석은 저장·수정 없이 수행했다. 위 값은 현재 구현 스냅샷이며 게임 경험 목표를 정의하지 않는다. 목표시간과 성장곡선은 [밸런스 문서](BALANCE_OVERVIEW.md), 길의 역할과 진행 규칙은 [기획서](GAME_DESIGN_OVERVIEW.md), 구체적인 배치는 [맵 기획서](MAP_DESIGN_OVERVIEW.md)를 따른다.

## 현재 구현 갭

| 기획 요구 | 코드 분석 결과 | 필요한 개발 |
| --- | --- | --- |
| 스테이지 시작 신성한 제단 | 전용 부활 제단·스테이지 앵커 컴포넌트를 찾지 못함 | `StageId`와 부활 앵커 저장·복귀 구현 |
| 제단 옆 불법 신발 개조업자 | 전용 NPC·상호작용 코드 없음 | 기본 업그레이드 UI를 여는 NPC·배치 계약 구현 |
| 같은 스테이지 시작점 부활 | `GameManager`는 단일 `playerSpawnPoint`로 리셋 | 스테이지별 스폰 앵커와 현재 스테이지 영구 저장 |
| 10레벨 연쇄 해금 | `UpgradeUI`의 잠금 값이 항상 `true` | Data.xlsx 기반 선행 항목·레벨 조건과 잠금 UI 구현 |
| 스테이지 클리어 A/B 특별 상점 | A/B 해금·가격 층 코드 없음 | 스테이지 클리어 보상 상태와 특별 상점 UI·저장 구현 |
| 노량진 → 고속도로 이동 | Build Settings에는 노량진만 활성화 | 고속도로 런타임 씬 확정, Build 등록, 씬 전환 구현 |
| 첫 구간 이후 급격한 적 성장 | 현재는 챕터·등급·배치 순서 보간 | 시간 또는 경로 진행도 기반 성장곡선과 Data.xlsx 스키마 연결 |
| 스테이지 의미의 분석 로그 | TurnSpot 수가 세부 stage 진행도로도 사용됨 | 제품 스테이지와 경로 체크포인트를 다른 분석 필드로 분리 |

## 씬·콘텐츠 상태

- `Noryangjin_MapTool_Mode`: 현재 유일한 활성 Build 씬이며 맵툴과 런타임을 함께 담당한다.
- `Noryangjin_MapTool_Mode_2`: 150개 도로·511개 소품의 정적 확장 참고 씬이다. Build Settings에는 없다.
- `Noryangjin_MapTool_Mode_SR18`: 맵1의 51개 도로를 보존하고 등대 인피니티 179개를 추가한 총 230개 도로·15개 회전 스팟의 정적 제작 씬이다. 기존 나무 길 양옆에 점포 306개·안벽 306개를 배치하고 기존 장식과 물 1개를 유지한다. 이전 417점포·내부 시장 블록은 길을 가려 철회했다. 2026-09-07 정상 크기의 적 25명(기본 15·왕복 5·매복 5)·고정 BonusWall 25개·기믹 24개·적 발동 스팟 25개·독립 이동 목표 10개로 교체했다. Props 총수는 715개다. 기존 길·상점은 보존했고 Build Settings에는 없다. 상세 검증은 `map-concepts/sr18-latest-elements-applied-2026-09-07/README.md`를 따른다.
- `Stage02_Highway_AutoDraft`: 고속도로 자동 초안 씬은 존재하지만 런타임 스테이지로 연결되지 않았다.
- `Forward March Mode`: 소스 프로젝트에는 존재하지만 현재 Build Settings에는 등록되지 않았다.
- 예전 문서의 Build Settings 목록보다 실제 `EditorBuildSettings.asset`을 우선한다.

## 데이터 원본 우선순위

1. `Assets/ShooterSurvival/GameData/Editor/Data.xlsx`: 밸런스·스킨의 편집 가능한 단일 수치 원본
2. [밸런스 문서](BALANCE_OVERVIEW.md): 수치의 목표, 조정 근거와 검증 결과
3. 런타임 구현: 위 원본을 반영한 결과이며 독립적인 수치 원본이 아님

세부 보호·빌드·런타임 반영 절차는 [게임 데이터 워크북 문서](docs/game-data-workbook.md)를 따른다.

## 기술 부채와 위험

- 런 상태가 `GameManager`, `CanvasScript`, `TimeManager`에 나뉘어 있어 스테이지 전환·부활 로직 추가 시 중복 상태가 생길 수 있다.
- `GameManager`는 기본적으로 10 stage × 10 chapter 인덱스 순환을 사용해 현재의 `노량진 → 고속도로` 제품 용어와 맞지 않는다.
- 노량진 맵툴 씬이 제작 도구와 실제 부팅 씬을 겸하므로 dirty 씬 보호가 중요하다.
- Map 2, SR18 제작 씬과 Highway 초안은 존재하지만 모두 현재 제품의 스테이지 2 런타임 계약을 만족한다고 볼 수 없다. SR18은 경사 전환 스팟 8개와 전용 도로 높이 추종을 추가해 두 고가 구간의 실제 주행을 검증했으며, 전체 스테이지 전투·전환·성능 검증은 남아 있다.
- 스킨·업그레이드·몬스터 수치는 `Data.xlsx`가 원본이므로 PlayerPrefs와 Inspector가 별도 권위가 되지 않게 해야 한다.
- Firebase 이벤트의 `stage`가 제품 스테이지와 TurnSpot 진행 단계를 혼용하지 않도록 계약을 정리해야 한다.

## 권장 개발 순서

1. 제품 스테이지 식별자와 스테이지별 부활 앵커 모델
2. 신성한 제단 등록·저장·부활 흐름
3. 제단 옆 불법 신발 개조업자와 기본 업그레이드 UI 연결
4. 10레벨 연쇄 해금과 잠금 표시
5. 노량진 클리어 A 업그레이드·특별 상점
6. 진행도 기반 적 성장곡선과 밸런스 데이터 연결
7. 고속도로 런타임 씬 확정·전환·B 업그레이드
8. 제품 스테이지와 경로 진행도를 분리한 분석 이벤트

## 기본 검증

```powershell
dotnet build Assembly-CSharp.csproj -nologo
dotnet build Assembly-CSharp-Editor.csproj -nologo
powershell -ExecutionPolicy Bypass -File tools/validate-agent-harness.ps1
```

기능별 추가 검증과 Unity Editor 자동화 절차는 해당 개발 문서와 `AGENTS.md`를 따른다.
