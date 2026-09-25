# 휴게소 → 고속도로 모델 적용 완료

2026-09-25 사용자가 선택한 1번(휴게소부터 고속도로까지 적용)을 완료했다. 독립 제작물의 최종 선택 90종을 Unity에 가져오고 기존 두 씬에 배치했다.

- 적용 사진 12장·동작 영상 4개: [결과 갤러리](http://127.0.0.1:8772/applied-scenes/)
- 휴게소: `Assets/ShooterSurvival/Scenes/Tools/RestStop.unity`
- 고속도로: `Assets/ShooterSurvival/Scenes/Tools/HighWay.unity`
- 재사용 프리팹: `Assets/ShooterSurvival/Prefabs/RestStopProduction20260925/`
- FBX·텍스처·재질·애니메이터: `Assets/ShooterSurvival/Models/RestStopProduction20260925/`
- 검사 기록·원본 씬 백업: `outputs/reststop-scene-integration-2026-09-25/`
- 실제 Unity 이미지 원본: `tmp/image-previews/reststop-scene-integration-2026-09-25/`

## 적용 내용

| 항목 | 휴게소 | 고속도로 |
| --- | ---: | ---: |
| 씬에 연결된 신규 모델 종류 | 90 | 83 |
| 교체한 기존 소품 인스턴스 | 770 | 131 |
| 교체한 기존 인물 외형 | 103 | 0 |
| 기존 전투 개체 수 | 66 | 50 |
| 기존 투사체 연결 개체 수 | 46 | 34 |

휴게소 인물 교체 103개는 전투 개체 46개와 배경 인물 57개다. 건물 조립물에는 계산원·청소원·주유원·여행객을 추가했다. 경찰 방패 P01은 방어전 경찰 풀에 연결되며 Edit Mode에서는 해당 개체들이 비활성 상태다. 고속도로의 83종은 도로변·차량과 기존 부속 휴게소 시설에 쓰였다. 고속도로 전투 인물은 9월 23일 검증한 기존 6역할을 유지했다.

식당, 편의점, 화장실, 휴식 차양, 주유 시설과 두 종류의 판매대를 개별 소품으로 조립했다. 차량 기본 프리팹은 승인된 v4 치수를 적용했고, 주차 간격을 조정했다. 기존 움직이는 교통 위험물은 원래 충돌체와 보이는 크기의 대응을 유지했다. 차단기 팔은 기존 회전축에 새 모델을 연결했다.

이번 작업은 기존 Unity 레벨에 모델을 설치한 것이다. Blender v4의 10구간 경로를 새로 이식하거나 60초 방어·회전 속도·성장 밸런스를 변경한 작업은 아니다. 현재 게임의 경로와 전투 설정을 보존했다. 독립 배포 ZIP과 Blender v4 원본은 기존 제작 기록으로 남아 있다.

## 검증

- Unity에 복사한 FBX 90개 모두 선택된 배포 원본과 SHA-256이 일치한다.
- 네이티브 검사: 모델 90개, 재질 영역 99개, 인물 8개/18본/9클립(총 72개). 삼각형 예산 15,000 이하, 모든 클립의 대상 본과 실제 포즈 변화 확인.
- 유리/금속/천의 재질 분리, S10 투명도, T06 외부 평면의 노멀 미연결 보존. 기존 프로젝트의 FlatKit 표현을 사용하며 원본 PBR 렌더와 완전히 같은 광택을 주장하지 않는다.
- 두 씬을 저장 후 다시 열어 적·충돌체·카메라·보너스 계약이 적용 전과 동일함을 확인. 누락 스크립트/재질 0, 적마다 Animator 1개, 투사체 연결 유지.
- 기존 `RestStop` 검색 테스트 26개: 20개 통과, 6개 실패. 실패 조건은 적용 전 백업에서도 동일했다: 활성 워크북 적 46개 대 과거 기대값 50개, 두 씬의 없어진 pageProgress, UI 외곽선 0.2, 튜토리얼 높이 0.14, 교체된 강화 아이콘 경로. `preexisting-test-failures.json`에 원본/현재 값을 비교했다. 테스트를 통과시키기 위해 기존 UI나 밸런스를 되돌리지 않았다.
- `HighwayRebuildContractTests`: 3/3 통과.
- 일반 Play Mode: 휴게소 약 81.28, 고속도로 약 63.65 게임 단위 이동 후 현재 체력 500인 프로필이 첫 교전에서 패배. 각 검증 동안 오류 0. 전체 스테이지 클리어 검증은 아니다.
- 통제된 사격 검사: 새 H03·H08 씬 인물을 복제해 공격력 100/체력 10,000으로 검사. 각각 발사 1회, 플레이어 500→400, 손 이동 0.66/0.81 단위, 오류 0. 이는 밸런스 수치 변경이 아닌 저장하지 않은 런타임 검사다.
- 코인·강화·해금·장비 등 66개 저장값을 복원한 TSV가 원본과 바이트 단위로 동일하다. 코인 1,781, 보석 0 유지.
- 갤러리 사진 12개와 영상 4개 HTTP 200 및 파일 길이 일치. 네이티브 렌더는 직접 확인했다. 브라우저 UI 조작과 모바일 실기기 FPS/메모리/빌드는 미검증이다.

영상은 네이티브 카메라 캡처다. 일반 이동 영상은 1초 간격, 사격 영상은 0.1초 간격의 프레임으로 만들었으므로 성능 측정 자료가 아니다. 오버레이 UI 전체의 시각 검증도 포함하지 않는다.

## 재현과 유지보수

공식 `unity command --project-path . run_script`로 아래 파일의 진입점을 실행한다. 완료된 씬에 `Main` 설치를 다시 실행하면 중복 방지 검사가 거부한다. 현재 결과의 확인에는 `Verify`를 사용한다.

1. `tools/prepare-production-unity.py`: 최종 배포 FBX와 GLB 재질을 준비한다. TRELLIS Python 환경 사용.
2. `tools/import-production-unity.cs`, `ImportProductionUnity.Main`: 네이티브 모델/재질/동작/프리팹 생성. `Fit/Orientation` 부모에서 치수와 용도별 수평 방향을 보정하고 원래 FBX 축 변환을 유지한다.
3. `tools/build-production-assemblies.cs`: 8개 시설/판매대 조립 프리팹.
4. `tools/install-production-scenes.cs`: `Main("RestStop")`, 이어 `Main("HighWay")`; `Refine`, `RefineLayout`은 각각 두 씬에, `PositionHighwayService`는 고속도로에 적용한 후처리다.
5. 같은 파일의 `Verify("RestStop")`, `Verify("HighWay")`로 저장 후 재개방 검사. `tools/validate-production-assets.cs`는 90종과 72개 동작을 검사한다.
6. `tools/capture-production-scenes.cs`, `tools/probe-production-gameplay.cs`가 시각/실행 증거를 생성한다. Play Mode 검사 전후 저장값 스냅샷/복원을 수행한다.

옛 `RestStopChapterBuilder.Create`나 과거 소품 가져오기 명령은 현재 씬을 다시 구축하거나 여러 재질을 하나로 합칠 수 있다. 이번 모델을 유지보수할 때는 위 경로와 최종 선택 매니페스트를 사용한다. 원래 소품 렌더러는 비활성화해 기존 충돌/참조를 보존했으며, 교체한 전투 인스턴스는 오래된 시각 프리팹을 해제하고 새 Body와 장비를 연결했다. 다른 씬에서 사용하는 예전 전투 원본 프리팹은 자동 갱신 대상이 아니다.

PC·모바일 렌더러에 새 불투명 재질 95개의 FlatKit 외곽선 등록도 저장했다. 생성/import 중 독립 원본/배포 ZIP은 수정하지 않았다. Git 커밋·푸시는 수행하지 않았다.
