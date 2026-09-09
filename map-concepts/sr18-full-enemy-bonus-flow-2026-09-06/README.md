# SR18 전체 적군·BonusWall 흐름 초안

상태: **아래는 적용 전 기획 기록이며, 2026-09-06 실제 SR18 씬에 적·BonusWall 배치를 적용했다.** 현재 상태와 실제 화면은 [적용 기록](../sr18-encounters-applied-2026-09-06/README.md)을 기준으로 한다. 이 기획은 시작부터 마지막 출구까지 0–100%, 16개 진행 구간을 다루며 기믹은 제외한다.

## 적용 전 실제 맵 위 배치 그림 (과거 참고)

기존 순서도 대신 실제 SR18의 Roads·Props·Water를 임시 Unity 프리뷰 씬에 복제하고, 기존 적과 BonusWall 프리팹으로 전체 배치를 렌더링했다. 길과 상점은 원본 형태를 유지한다. 적 69명과 고정 BonusWall 14개를 식별하기 쉽게 2.5배로 표시했으므로 캐릭터 크기·개별 간격은 게임 적용값이 아니다. 처치 후 생기는 드롭은 미리 배치하지 않았다.

- [전체 배치 그림](../../tmp/image-previews/sr18-full-enemy-bonus-flow-2026-09-06/actual-map/01-full-stage-actual.png)
- [시작·큰 루프 확대](../../tmp/image-previews/sr18-full-enemy-bonus-flow-2026-09-06/actual-map/02-start-and-large-loop.png)
- [고가·후반·보스 확대](../../tmp/image-previews/sr18-full-enemy-bonus-flow-2026-09-06/actual-map/03-bridges-and-finish.png)

기록: [프리뷰 배치 목록](actual-map/preview-manifest.json). 실행: `unity --format json command --project-path . eval_file tools/render-sr18-full-placement-preview.cs 60000`. 기존 출력이 있으면 중단한다. 파일은 기획 데이터의 현재 스냅샷이므로 데이터 변경 시 배치 목록도 갱신해야 한다.

검증: 83개 프리뷰 오브젝트 생성, 3개 PNG 육안 확인. 캡처 후 프리뷰 씬을 닫았고 원본 씬은 저장하지 않았다. 정리 후 지연된 Canvas 비활성 차이 1개가 발견되어, 보존한 씬 사본과 원본의 차이가 해당 활성 플래그 하나뿐임을 확인한 뒤 그 값만 복원했다. 복원 사본이 디스크 원본과 바이트 단위로 일치할 때만 dirty 표시를 해제했으며 이후 조회에서도 clean 상태를 확인했다. 재실행 시에도 정리 **이후** 별도 호출로 원본 상태를 검증해야 한다.

이미지 생성 도구의 두 후보는 경로 연결을 바꿔 위의 실제 맵 프리뷰로 대체했다. 후보와 [첫 프롬프트](illustrated/PROMPT.md), [수정 프롬프트](illustrated/PROMPT-v2.md)는 비교용으로만 보존한다. 생성 후보는 실제 길·프리팹 배치 근거가 아니다.

## 큰 글씨 순서도 (텍스트 참고)

약자와 편성 번호를 빼고 위에서 아래로 읽는 4장으로 나눴다. 전투 25회, 고정 보너스 9지점과 시작부터 완주까지의 순서는 그대로다. 빨강은 전투, 초록은 별도 배치 보너스다. 적 처치 시 발생하는 드롭은 상단 공통 설명으로 묶었다.

- [① 시작·첫 선택·큰 루프 진입](../../tmp/image-previews/sr18-full-enemy-bonus-flow-2026-09-06/easy-read/01-easy-flow.png)
- [② 큰 루프의 전투와 보상](../../tmp/image-previews/sr18-full-enemy-bonus-flow-2026-09-06/easy-read/02-easy-flow.png)
- [③ 첫 고가·중반 상점가](../../tmp/image-previews/sr18-full-enemy-bonus-flow-2026-09-06/easy-read/03-easy-flow.png)
- [④ 후반·마지막 보스·완주](../../tmp/image-previews/sr18-full-enemy-bonus-flow-2026-09-06/easy-read/04-easy-flow.png)

재현: `py -3.11 -X utf8 tools/draw_sr18_easy_flow.py`. 기존 PNG가 있으면 덮어쓰지 않고 중단한다.

## 상세 참고 그림

- [전체 16구간 한 장](../../tmp/image-previews/sr18-full-enemy-bonus-flow-2026-09-06/final/01-full-stage-flow.png)
- [전반 S01–S08 확대](../../tmp/image-previews/sr18-full-enemy-bonus-flow-2026-09-06/final/02-first-half-flow.png)
- [후반 S09–S16 확대](../../tmp/image-previews/sr18-full-enemy-bonus-flow-2026-09-06/final/03-second-half-flow.png)
- [E/F 번호를 실제 경로에 대응](../../tmp/image-previews/sr18-full-enemy-bonus-flow-2026-09-06/final/04-full-route-locations.png)

각 줄은 왼쪽에서 오른쪽으로 읽고 다음 줄로 이어진다. 적 편성 E01–E25 아래의 초록 표시는 그 적들을 처치했을 때 발생하는 드롭이다. 파란 F01–F09는 별도로 배치하는 고정 BonusWall 지점이다. 보라색 고정 보너스는 유니크다.

## 제안 수량

| 항목 | 초안 |
| --- | ---: |
| 전체 진행 구간 | 16 |
| 전투 편성 | 25 |
| 적 | 69명 |
| 적별 BonusWall 드롭 | 전부 처치 시 최대 69개 |
| 고정 BonusWall | 9지점에 14개 |
| 드롭 + 고정 생성량 | 최대 83개 (획득량과 다름) |

적은 노인 12, 그물형 9, 경비 14, 검형 28, 뚱보 5, 여성 보스 1이다. 현재 프리팹의 등급 매핑을 유지하므로 일반 63, 엘리트 5, 보스 1에 해당한다. 고정 보너스는 일반 6, 엘리트 7, 유니크 1이다. 이 수량은 리듬을 검토하기 위한 1차 제안이며 밸런스 확정값이 아니다.

## 범위와 주의점

- 진행률은 기존 코너 좌표와 최종 출구에 따른 평면 경로 거리 비율이다. 30초짜리 구간만 확대한 그림이 아니며, 실측 시간표나 3배 테스트 시간표도 아니다.
- 현재 코드는 적 사망마다 BonusWall을 생성한다. 드롭의 실제 위치와 시점은 적을 어디서 언제 처치했는지에 달려 있다. 그림의 아래 화살표는 이 인과관계이며 드롭을 미리 고정 배치한다는 뜻이 아니다.
- N/E/U는 보너스 등급이며 효과는 랜덤이다. 같은 지점의 두 보너스에 강제 1택 기능이 구현되어 있다고 가정하지 않는다. 접촉 1초 제한과 실제 획득 동선을 따로 검증해야 한다.
- 모든 E/F 좌표는 편성 또는 선택 구간의 중심 후보이며 개별 적·제단·발동 스팟의 최종 Transform이 아니다. 특히 E14/E15는 첫 고가의 상부 평지, F05는 하부 복귀 뒤다.
- 평면 교차점에 적 발동 영역이 겹치지 않도록 E06을 교차 뒤로 옮겼고, F07은 두 번째 고가 교차점 직전에서 충분히 떨어뜨렸다. 세부 범위와 카메라 시야는 적용 단계에서 검사한다.
- 경비원/뚱보의 발사와 검형 등의 이동·공격은 기존 행동을 조합한다. 신규 적 모델이나 새로운 보스 패턴을 제안한 것이 아니다.
- 최종 보스 뒤 유니크는 배치 제안이다. 보스를 죽여야 보상이 잠금 해제되는 기능은 현재 그림에 포함하지 않는다.
- 위 그림을 만들던 기획 단계에서는 Unity 씬과 밸런스를 변경하지 않았다. 이후 명시적 적용 요청에 따른 씬 변경은 별도의 적용 기록에 남겼다.

## 재현

최신 구조 데이터는 [full-flow-final.json](full-flow-final.json)이다. 기존 경로/코너 기록과 실제 적 등급 카탈로그를 참조한다. 새 캔버스에 좌표와 표식을 그리는 `tools/draw_sr18_full_enemy_bonus_flow.py`로 PNG를 만들었으며, 기존 스크린샷을 수정하거나 길을 새로 생성하지 않았다.

실행 환경은 Pillow가 설치된 Python 3.11이다: `py -3.11 -X utf8 tools/draw_sr18_full_enemy_bonus_flow.py`. 출력 파일이 이미 있으면 덮어쓰지 않고 중단한다. 수정본은 별도 출력 경로를 정한다. 앞선 초안 및 v2는 비교용이며 최종 안내에는 `final/` 이미지를 사용한다.

생성 시 구간 수, 편성 수, 적 합계, 고정 보너스 합계와 편성 문자열 폭을 검사했고, 전반·후반·경로 대응 그림을 육안 확인했다.
