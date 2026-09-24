# 휴게소 v4 — 차량 1.8배 확대

버스와 자동차가 전반적으로 작다는 피드백을 반영했다. v3를 보존하고 차량 6종·91대의 가로·세로·높이를 각각 1.8배로 확대했다. 배율은 이번 제작 선택이다.

- 갤러리: http://127.0.0.1:8770/
- 이미지: `tmp/image-previews/reststop-blender-v4-2026-09-23/01-reststop.png`부터 `10-reststop.png`
- 같은 카메라 비교: 위 폴더의 `vehicle-scale-comparison.png`. 상어·사람과 카메라 배율은 동일하다.
- 원본: `outputs/reststop-blender-v4-2026-09-23/reststop-v4.blend`
- 정적 환경: 같은 폴더의 `reststop-v4-environment.glb`
- 소스: `tools/reststop-previs-v4/`

## 수정

승용차 높이는 약 3.11~3.98, 배송 트럭 5.67, 박스 트럭 6.84, 버스 6.66 게임 유닛이다. 상어 높이 약 3.26, 사람 약 3.05를 크기 비교 기준으로 사용했다. 실제 차량 실측치나 미터 단위 건축 수치가 아니다.

주차장 76대는 커진 차체 바닥 범위를 기준으로 재배치했다. 주차면 길이·폭도 조정했다. 진행 중심선과 차체 가장자리의 최소 간격은 약 5.43 유닛이며 검토용 통로 반폭 4.1보다 크다. 차량 사이에는 최소 0.7 유닛의 여유를 확보하는 배치 조건을 적용했다. 주요 나무·조명·주차 안내원과 후진차 이동 범위도 배치에서 제외했다.

후진차의 최대 진입 위치를 경로 중심에서 5.2 유닛으로 옮기고 브레이크등을 차체에 맞췄다. 주유소·출구 차량도 동일하게 1.8배 확대했다. 카트 0개, 자동 전진+좌우 전용 입력, 쿼터뷰 60초 방어, 24fps·7,200프레임을 보존했다.

## 확인

`scale-metrics.json`은 최종 치수와 주차 위치를, `vehicle-validation.json`은 네이티브 차체 크기·지면 접촉과 이동 차량 검토를 기록한다. 후진차·버스·출구 차량의 작동 구간을 0.2초 간격의 보수적 2D 시각 바운딩 박스로 비교했으며, 작성된 플레이어 동작과 겹치는 샘플은 없었다. Unity 물리나 모든 가능한 플레이어 입력에 대한 충돌 검증은 아니다.

`validation.json`에서 경로 및 좌우 제약·방어 회전 속도를 재확인했다. `fresh-glb-validation.json`과 전체 렌더로 GLB 새 임포트를 확인했다. 10장 PNG와 같은 카메라의 차량 전후 비교를 실제로 열어 검토했다. 최종 선택은 `retain_repair`다.

## 재현

저장소 루트에서 Blender CLI와 `tools/run-limited-generation.py`로 `tools/reststop-previs-v4/resize.py`를 실행한 다음 `python tools/reststop-previs-v4/run.py`를 실행한다. 마지막으로 `python tools/reststop-previs-v4/gallery.py`를 실행한다. GPU 작업은 순차 처리한다.

이번 결과는 Blender 프리비즈 수정이다. Unity 씬 설치, TRELLIS·Seedance 실행, 새 동영상 인코딩은 하지 않았다.
