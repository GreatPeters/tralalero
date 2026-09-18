# 보너스 효과 시안 10종

2026-09-19. 사용자가 작은 별 위주의 보너스 표현을 다시 거절하고, 실제 프로젝트에서 구현 가능한 서로 다른 이미지 시안 10장을 요청했다. 각 이미지의 왼쪽은 대기, 오른쪽은 획득 연출이다. built-in `image_gen`으로 제작한 기획 이미지이며 실제 Unity 캡처나 설치 완료 결과가 아니다. 정확한 프롬프트와 원본 경로는 `prompts.json`, 비교한 실제 화면은 `reference-game.png`에 있다.

현재 저장된 게임 상태를 기본 브랜치에 머지하라는 요청은 별도로 수행한다. 이 10종 중 특정 안을 게임에 설치하라는 선택은 아직 없다.

| 번호 | 이미지 | 제작 경로 | 특징 |
|---|---|---|---|
| 01 | [황금 메달](images/01-gold-medallion.png) | Unity 원판/화살표 메시, 금속 재질, 얇은 링·궤적 | 멀리서도 보상으로 읽히는 큰 메달 |
| 02 | [항구의 진주](images/02-harbor-pearl.png) | 프로젝트의 `Sea_Shell_Scallop_Open_Pearl_A` + 구체/Fresnel 재질 + 소수 파티클 | 항구와 가장 자연스럽게 연결되는 조개·진주 |
| 03 | [보물상자](images/03-treasure-chest.png) | 프로젝트의 `Chest_Wooden_Round_Small_Fantasy` + 별도 뚜껑 애니메이션 + 코인 메시 | 열리고 내용물이 들어오는 명확한 획득감 |
| 04 | [버프 홀로그램](images/04-buff-hologram.png) | Unity 화살표/링 메시 + unlit additive 셰이더 + 짧은 궤적 | 성장·능력 상승의 의미가 명확함 |
| 05 | [선물 보급품](images/05-gift-supply.png) | 프로젝트의 `Gift_Christmas_Cube` 계열 + 재질 변경 + 뚜껑/컨페티 | 귀엽고 경쾌한 선물형 보상 |
| 06 | [축복의 등대](images/06-lighthouse-beacon.png) | Blender 원통/원뿔·창틀 + 반투명 빛 메시 + 링 | 항구의 안전한 안내 불빛을 보상으로 사용 |
| 07 | [행운의 나침반](images/07-lucky-compass.png) | 프로젝트의 `Survival/Compass` + 황동/남색 재질 + 평면 문양 | 회전하는 보물과 바닥으로 퍼지는 방향 문양 |
| 08 | [바다 수정](images/08-ocean-crystal.png) | 절차적 결정 메시 + emissive 셰이더 + 메시 파티클 | 작아도 잘 읽히는 선명한 보석 실루엣 |
| 09 | [날개의 축복](images/09-winged-blessing.png) | Blender 좌우 날개 메시 또는 알파 스프라이트 + 짧은 리본 | 획득 순간이 가장 화려한 축복형 연출 |
| 10 | [행운의 종](images/10-lucky-bell.png) | Blender 종/아치 + 회전 애니메이션 + 확장 링 | 소리와 동작을 연결하기 쉬운 항구 소품 |

## 실제 재사용 후보

- `Assets/polyperfect/Poly Universal Pack/Prefabs/Nature/Seawater/Sea_Shell_Scallop_Open_Pearl_A.prefab`
- `Assets/polyperfect/Poly Universal Pack/Prefabs/Fantasy/Furniture Fantasy/Chest_Wooden_Round_Small_Fantasy.prefab`
- `Assets/polyperfect/Poly Universal Pack/Prefabs/Survival/Compass.prefab`
- `Assets/polyperfect/Poly Universal Pack/Prefabs/Christmas/Gift_Christmas_Cube_Open_Base.prefab` 및 `Gift_Christmas_Cube_Open_Lid.prefab`
- `Assets/JMO Assets/Cartoon FX Remaster/CFXR Assets/Graphics/`의 기존 글로우/광선 재질과 적합한 파티클 소재

기존 모델은 출발점이며 생성 이미지의 정확한 모델 형태와 이미 동일하다는 뜻은 아니다. 쉘·상자·나침반은 리스케일/재질 조정, 일부 모델은 뚜껑·손잡이 분리 또는 간단한 리모델링이 필요하다. 현재 범위는 설치된 에셋과 직접 제작 가능한 메시/셰이더로 충분해 새 MCP나 TRELLIS 설치를 수행하지 않았다.

## 구현 시 지킬 사항

- 기존 `WallScript` 보상 종류·수치와 `BonusWallChoicePair`의 1택·중복 방지 동작을 유지한다.
- 이미지의 `+14% 공격력`은 비교용 예시다. 메달/화살표 안을 선택하면 실제 보상 종류에 따라 검·하트·탄환 등의 표시를 연결한다.
- 대기 효과는 거리 제한 및 공유 재질을 사용한다. 획득 연출은 약 0.5–0.9초를 출발점으로 조정하며 화면 전체를 가리는 연기·섬광을 피한다.
- 발판 크기와 충돌 범위는 먼저 유지하고, 실제 휴대폰 화면에서 오브젝트·글자의 겹침을 검증한다.
- 수치는 구현 목표이지 성능 측정 결과가 아니다. 시안 확정 후 실제 Unity 씬에서 크기·발광·지속 시간을 다시 맞춘다.

전체 이미지의 로컬 열람 사본: `tmp/image-previews/bonus-concepts-2026-09-19/`. 원본 생성 파일을 덮어쓰거나 삭제하지 않았다.
