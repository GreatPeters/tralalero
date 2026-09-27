# 고속도로 한국형 보정 · Meshy 재사용 (2026-09-25)

휴게소 작업([기록](../meshy-reststop-2026-09-25/README.md)) 다음 단계. 기획: [계획서 §7](../../docs/plans/2026-09-25-reststop-korean-entry-meshy-plan.md). 사용자 조건: 기존 것은 재활용하고 부족한 것만 새로 뽑는다. 작업 중 사용자 정정에 따라 이후 이미지 생성은 Codex, Meshy는 모델·텍스처·리깅·애니메이션에만 쓴다(고속도로 컨셉 8장은 정정 전에 Meshy로 생성).

## 적용 결과 (HighWay 씬)

경로(`HighwayRoute` 2,340), 적 50명·보너스·기믹 31개의 위치와 충돌체는 그대로다. 설치 전후 계약이 같아야 저장한다(`tools/install-highway-korean.cs` Main, Refine1–2).

- **배경:** 유리 고층 빌딩 101개(skyscraper/business_center, City backdrop, horizon) 비활성화 → 출발 구간 아파트 단지(P13), 중반 논·논둑·비닐하우스(P14), 전 구간 산 능선(P09, 바닥 높이 -7).
- **적 교체(원거리/근접 유지):** 원뿔 정비공→도로공사 직원(라바콘), 교통경찰→순찰대(투척 예고), 톨게이트 반장→**요금소 직원(신규)**, 타이어 거한→**레커 기사(신규)**, 아스팔트 작업자→화물차 기사(돌진), 배달 라이더→기동대(방패). 이륜차 배달원은 한국 고속도로에 맞지 않아 제외.
- **차량:** 보이는 차량 40대(이전 휴게소판·HWY 차량)를 Meshy 차량으로 교체. 움직이는 기믹 차량은 기존 충돌 길이에 맞춘다. 인물 14명은 휴게소 배경 인물로 교체.
- **공사 구간 4곳:** 바리케이드 뒤 LED 화살표 작업차(V10), 라바콘 테이퍼, 깃발 흔드는 신호수(C08).
- **신규 기믹:** 레커차 뒤 추월 2곳(d 870, 1700), 굴러오는 타이어 2곳(d 540, 1250). 닿으면 최대 체력 15–30% 손실.
- **반대편 차도 교통:** `AmbientTrafficPath` 16대(탱크로리·레커차 포함), 충돌 없음, 플레이어 170 이내만 표시.
- **공통 수정:** 도로공사 직원이 놓는 라바콘이 즉사 바리케이드를 복제하던 문제를 고쳐, 이제 닿으면 12%만 잃고 쓰러진다(`RestStopConeContact`). 휴게소에도 적용된다.

## Meshy

| 항목 | 수 | 크레딧 |
| --- | ---: | ---: |
| 컨셉(Meshy, 정정 전) | 8 | 57 |
| 3D 모델 | 8 | 240 |
| 리깅 | 3 | 15 |
| 동작 | 11 | 33 |
| 차량 리메시 | 3 | 15 |
| **합계** | | **360** (잔액 2,842 → 2,482) |

중복 결제 없음. V10 화살표 작업차는 2,500 리메시가 화살표판을 뭉개서 원본 모델(7,734 삼각형)을 쓴다(리메시 결과는 `models/V10_arrow_truck/rejected/`).

## 검증

- 설치·보정 전후 계약 동일. 원본 씬 `outputs/meshy-highway-2026-09-25/install/HighWay.before.unity`.
- 경로 확인 실행(체력 보정, 즉사 기믹·정면 차량 충돌만 끔): 298.6초에 2,328.8 도달 — 이전 기준 298.56초/2,328.85와 동일. 오류 0. 인증용 난이도 결과가 아니다.
- 정면 차량 기믹의 즉사 규칙은 기존 설계 그대로다(검사 1·2회차 117m 사망 원인).
- EditMode: HighwayRebuildContract 3, HighwayProjectilePath 5, MapToolChapterControls 5, RoadChapterPattern 27, RestStopEncounterLane 11, NoryangjinCameraOcclusion 6, HighwayHazardRules 1, RestStopEnemyBehavior 2, RestStopConeContact 1 통과. `dotnet build` 두 어셈블리 오류 0. 저장값 66개 바이트 단위 복원.
- 미검증: 실기기 성능, 사람이 직접 한 고속도로 전 구간 난이도.

이미지: `tmp/image-previews/meshy-highway-2026-09-25/` (before/after 12컷, 탑뷰, 컨셉·모델 검수, `highway-full-3.jpg`).
