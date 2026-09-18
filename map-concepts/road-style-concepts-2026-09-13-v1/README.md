# 노량진과 연결되는 고속도로·휴게소 컨셉 6안

최신 정정은 [v2 컨셉](../road-style-concepts-2026-09-13-v2/README.md)에 반영했다. v1의 동물형/2등신 적,차선 구성,높은 R3 식당 시점은 최신 사용자 요구로 승인된 것이 아니다.

사용자의 요청에 따라 현재 구성을 개선했을 때의 외형을 그림으로 비교했다. **Unity 씬 캡처가 아닌 built-in image_gen 컨셉 이미지**이며 이번 작업에서 씬·프리팹·밸런스·저장값을 변경하지 않았다. 실제 적용에서는 메시·재질·카메라·배치를 별도로 검증해야 한다. 간판 문구와 적의 세부 외형은 그림의 표현이며 새 게임 요구사항이나 확정된 에셋 사양이 아니다.

## 공통 방향

노량진 플레이 캡처의 따뜻한 갈색/크림색, 선명한 파랑과 흰 줄무늬 차양, 가까이 겹치는 소품과 굵직한 형태를 공통 기준으로 삼았다. 고속도로 아스팔트·초록/분홍 분기, 휴게소 주유소/매점, 식당 중앙 방어 공간은 현재 기능을 바탕으로 한다.

## 비교

| 코드 | 컨셉 | 방향 |
|---|---|---|
| H1 | [항만에서 이어지는 도심도로](../../tmp/image-previews/road-style-concepts-2026-09-13-v1/H1-harbor-city-highway.png) | 노량진의 파랑·갈색과 가까운 소품 구성을 이어받은 도심 진입 고속도로. |
| H2 | [녹지가 감싸는 곡선도로](../../tmp/image-previews/road-style-concepts-2026-09-13-v1/H2-green-curve-highway.png) | 수목·방음벽이 가까이 감싸고 초록 우회로가 읽히는 곡선형 고속도로. |
| H3 | [요금소와 회복 분기](../../tmp/image-previews/road-style-concepts-2026-09-13-v1/H3-toll-recovery-highway.png) | 톨게이트·작업 소품·회복 쉼터를 중심으로 구역의 표정을 만든 고속도로. |
| R1 | [노량진에서 이어지는 먹거리 골목](../../tmp/image-previews/road-style-concepts-2026-09-13-v1/R1-market-food-reststop.png) | 줄무늬 차양과 먹거리 매대가 길 가까이 모여 있는 시장형 휴게소. |
| R2 | [초록 주유소와 매점 마당](../../tmp/image-previews/road-style-concepts-2026-09-13-v1/R2-green-service-reststop.png) | 원본 휴게소의 좌측 주유소·우측 매점을 노량진의 표현 방식으로 통일한 안. |
| R3 | [중앙 식당과 안뜰](../../tmp/image-previews/road-style-concepts-2026-09-13-v1/R3-foodhall-courtyard-reststop.png) | 먹거리 매대가 가장자리를 감싸는 중앙 식당과 열린 안뜰형 휴게소. |

노량진과의 시각적 연결감을 우선하면 **H1+R1**, 고속도로·휴게소의 원래 장소성을 우선하면 **H2+R2**가 비교의 출발점이다. H3는 요금소/분기 구간의 장면 구성, R3는 기존 방어전 구간의 내부 방향으로 조합할 수 있다. 이 조합은 에이전트 제안이며 사용자 확정 요구가 아니다.

## 입력과 검토

- 원본 기준: `output/meshy_images/stage_02_1_highway_concept_batch_v1.png`, `stage_03_3_rest_stop_concept_batch_v1.png`.
- 노량진 미술 기준: `tmp/image-previews/sr18-live-playtest-2026-09-10/005227/guided-005230/frame-002.png`.
- 현재 배치 기준: `tmp/image-previews/road-reference-visuals-2026-09-13/`의 고속도로·휴게소·식당 최종 화면.
- H1/H3 최초 결과의 추가 다리/신발은 두 다리+꼬리 신발 형태로 수정했다. R3는 첫 공통 프롬프트의 보너스 제단을 제거하고 높은 카메라·사방 출입구·중앙 고정 전투 공간으로 수정했다.
- 최초 후보는 preview 폴더의 `drafts/`에 보존했다. 최종6개 파일이 이 README의 링크 대상이다.
- [전체 프롬프트 및 수정 지시](prompts.md), [생성 원본·최종 파일 대응](manifest.json).
