# Bonus Wall 신성한 제단 — 실제 제작 범위

이 제작 범위를 반영해 다시 그린 [프리팹 활용 시안 10종](../bonus-practical-altars-2026-09-19/README.md).

2026-09-19 사용자 후속: “현실적으로 프리팹 활용하거나 에셋 활용하거나 트렐리스 사용하거나 등등 가능한 선에서”.

## 확인 범위

파일 목록 및 기존 제단 제작 코드를 확인한 구성 제안이다. 후보 프리팹의 Unity 렌더, 메시 분리 가능 여부, 실제 기기 성능은 아직 확인하지 않았다. PNG 시안과 동일한 모델이 이미 설치돼 있다는 의미는 아니다. 이번 작업은 구현 가능성 정리이며 게임 적용이나 TRELLIS 실행은 하지 않았다.

## 실제 확인한 재사용 후보

아래 Poly Universal Pack 경로의 공통 접두어는 `Assets/polyperfect/Poly Universal Pack/Prefabs/`다.

| 용도 | 존재를 확인한 파일 |
|---|---|
| 돼지머리 공물 | `Fantasy/Butcher Fantasy/Pig_Head.prefab` |
| 접시 | `Survival/Plate_Enamel.prefab` |
| 그릇 | `Fantasy/Bowl_A_Fantasy.prefab` |
| 사과 | `Farm/Crops Farm/Apple_Red.prefab` |
| 촛대 | `Fantasy/Light Fantasy/Candle_Table_A_Lit_Fantasy.prefab` |
| 목제 테이블 후보 | `Fantasy/Furniture Fantasy/Table_Round_A_Fantasy.prefab` |
| 조개 | `Nature/Seawater/Sea_Shell_Scallop_Open_A.prefab` |
| 등불 | `Fantasy/Light Fantasy/Lantern_A_Fantasy.prefab` |

기존 게임 자산:

- `Assets/ShooterSurvival/Prefabs/Walls/New/Box_left.prefab`: 현재 보너스 프리팹 기반. 획득·수치·충돌 동작을 보존하고 시각 자식만 변형하는 출발점.
- `Assets/ShooterSurvival/Models/MeshyAI/_Gameplay_Walls/FeastOfFortune/FeastOfFortune_Left.fbx`, `FeastOfFortune_Right.fbx`: 과거 공물 모델 후보. 현재 그림과 일치하거나 부품을 바로 분리할 수 있다고 단정하지 않는다.
- `Assets/ShooterSurvival/Editor/FeastOfFortuneWallSetup.cs`: 기존 베벨 받침·물결·바닥 문양·숫자/배지 생성 경로.
- `Assets/JMO Assets/Cartoon FX Remaster/CFXR Prefabs/Fire/CFXR Flame Candle.prefab`
- `Assets/JMO Assets/Cartoon FX Remaster/CFXR Prefabs/Fire/Variants/CFXR Flame Plain (calm).prefab`
- `Assets/JMO Assets/Cartoon FX Remaster/CFXR Assets/Graphics/cfxr proc glow soft add.mat`: 기존 보너스 설치 코드에서 참조하는 재질.

Cartoon FX 전체를 그대로 얹는 방식이 아니라 필요한 불꽃·글로우 소재를 골라 색·크기·방출량을 조정한다. 신발로 향하는 이동은 별도 타깃 추적 궤적이 필요하다.

## 10개 시안의 제작 방식

모든 안은 공물·받침·효과를 분리한다. 공물은 위 Pig_Head와 접시·과일 후보를 우선 검토한다.

| 안 | 재사용 및 직접 제작 | 시안에서 현실화할 부분 |
|---|---|---|
| 01 백옥 | 기존 받침 구조 + 단순 팔각 석단 메시 + 공물·촛대 | 금색 인레이를 얇은 메시/텍스처로, 후광 1개와 흡수 궤적 2~3개 |
| 02 청자 | Bowl_A 후보 + 짧은 받침 + 기존 물결 소재 | 복잡한 청자 부조를 텍스처화, 유체 대신 회전 UV·리본 |
| 03 고사상 | 기존 FeastOfFortune 모델 검토 또는 낮은 상 직접 제작 + 공물·촛대 | 장식 다리·술 장식을 줄이고 고정 공물을 축소/페이드로 소멸 |
| 04 월륜 | 원호로 직접 만든 초승달 메시 + 원형 받침 | 작은 원호 1~2개, 달 표면은 간단한 발광 재질 |
| 05 금줄 | 기존 받침 + 커브 기반 로프 + 종이 평면 | 로프는 물리 없이 고정, 복잡한 매듭은 단순 실루엣 |
| 06 성화 | Bowl_A를 확인 후 청동화하거나 단순 회전체 화로 제작 + CFX 불꽃 | 정교한 손잡이 생략, 연기 없는 작은 불꽃과 흡수 궤적 |
| 07 조개 | Sea_Shell_Scallop_Open_A + 공물 + 작은 받침 | 생성 이미지와 다른 조개 비율은 수작업 조정, 발광은 얇은 테두리/후광 |
| 08 석판 | 베벨 큐브 석판 + 신발 문양 텍스처 + 종이 평면 | 문양은 실제 조각 대신 표면 이미지, 공통 보너스 UI 위치 유지 |
| 09 일륜 | 원판·방사형 막대·원형 받침을 직접 제작 | 광선 2~3개와 바닥 링 1개로 제한, 세밀한 금속 세공 생략 |
| 10 등불 | Lantern_A 두 개 + 낮은 받침 + 공물 | 등불 형태는 기존 에셋에 맞춤, 실시간 조명 대신 발광과 빛 궤적 |

01·03·10을 우선 후보로 제안한다. 기존 모델 외형을 실제로 확인한 후 최종 재사용 비중을 결정한다.

## TRELLIS 사용 기준

현재 10안의 기본 실루엣은 기존 프리팹과 간단한 메시로 제작할 수 있는 구성을 목표로 한다. TRELLIS는 필요하면 독특한 정적 석단·장식의 초안 후보로 사용한다. 이번 세션에서 TRELLIS 가동 상태나 생성 품질은 검증하지 않았으므로 필수 의존성으로 두지 않는다.

반투명 후광·움직이는 빛·촛불·공물 소멸은 Unity에서 별도로 구현한다. 생성 모델을 쓸 경우 배경/빛을 제외한 불투명 부품만 생성하고, 스케일·피벗·재질·메시·실루엣을 정리한 뒤 가져온다. 선택 부분만 수정하거나 효과를 끌 수 있도록 제단 전체를 하나의 생성 메시로 합치지 않는다.

## 런타임 기준

대기는 작고 절제된 후광/불꽃, 획득은 공물 위치에서 신발로 향하는 짧은 궤적과 발밑 링을 기본으로 한다. 기존 보너스 수치, 1택·중복 방지·재시작 초기화는 유지한다. 낮은 받침은 통과 가능한 보상으로 읽혀야 하며 충돌 장애물처럼 보이지 않도록 실제 Game View에서 확인한다.

공물과 신발을 연결할 때 캐릭터의 실제 발·꼬리 신발 위치에 연결점을 두고, 이동 중에도 효과가 따라가도록 구현한다. 이는 현재 코드에 이미 구현됐다는 주장이 아니라 필요한 신규 작업이다.
