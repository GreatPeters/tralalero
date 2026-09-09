# SR18 16구간 전체 한 줄 배치 위치도

상태: **그림만 제작, Unity 씬 미수정.** 이전 3개 대표 예시 밖의 구간과 각 예시 앞뒤의 남은 길까지 포함했다. 실제 경로의 16개 구간을 빠짐없이 사용하며 길 폭과 경로 연결을 변경하지 않는다.

## 전체와 묶음

- [전체 위치도](../../tmp/image-previews/sr18-all-single-file-sections-2026-09-06/00-full-route.png)
- [01–04 입구·시작 상점가](../../tmp/image-previews/sr18-all-single-file-sections-2026-09-06/group-01.png)
- [05–08 연결길·큰 루프](../../tmp/image-previews/sr18-all-single-file-sections-2026-09-06/group-02.png)
- [09–12 첫 고가·중반](../../tmp/image-previews/sr18-all-single-file-sections-2026-09-06/group-03.png)
- [13–16 상단 고리·두 번째 고가·출구](../../tmp/image-previews/sr18-all-single-file-sections-2026-09-06/group-04.png)

확대도는 읽기 쉽게 해당 구간을 회전해 **위에서 아래**로 표시한다. 각 장의 실제 전체 맵은 원래 방향이며, 원래 진행 방향도 별도로 표기했다. 회전은 그림 표시 방식일 뿐 실제 길 변경이 아니다.

## 개별 확대도 16장

| 구간 | 그림 |
| --- | --- |
| 01 | [입구 서행](../../tmp/image-previews/sr18-all-single-file-sections-2026-09-06/sections/01-S01.png) |
| 02 | [시작 상점가 북행](../../tmp/image-previews/sr18-all-single-file-sections-2026-09-06/sections/02-S02.png) |
| 03 | [첫 가로 상가](../../tmp/image-previews/sr18-all-single-file-sections-2026-09-06/sections/03-S03.png) |
| 04 | [짧은 북행 통로](../../tmp/image-previews/sr18-all-single-file-sections-2026-09-06/sections/04-S04.png) |
| 05 | [상단 연결길](../../tmp/image-previews/sr18-all-single-file-sections-2026-09-06/sections/05-S05.png) |
| 06 | [큰 루프 하행](../../tmp/image-previews/sr18-all-single-file-sections-2026-09-06/sections/06-S06.png) |
| 07 | [큰 루프 아랫길](../../tmp/image-previews/sr18-all-single-file-sections-2026-09-06/sections/07-S07.png) |
| 08 | [큰 루프 북행](../../tmp/image-previews/sr18-all-single-file-sections-2026-09-06/sections/08-S08.png) |
| 09 | [첫 고가](../../tmp/image-previews/sr18-all-single-file-sections-2026-09-06/sections/09-S09.png) |
| 10 | [중반 북행](../../tmp/image-previews/sr18-all-single-file-sections-2026-09-06/sections/10-S10.png) |
| 11 | [중반 가로 상가](../../tmp/image-previews/sr18-all-single-file-sections-2026-09-06/sections/11-S11.png) |
| 12 | [상단 고리 진입](../../tmp/image-previews/sr18-all-single-file-sections-2026-09-06/sections/12-S12.png) |
| 13 | [고리 윗변](../../tmp/image-previews/sr18-all-single-file-sections-2026-09-06/sections/13-S13.png) |
| 14 | [고리 짧은 하행](../../tmp/image-previews/sr18-all-single-file-sections-2026-09-06/sections/14-S14.png) |
| 15 | [두 번째 고가](../../tmp/image-previews/sr18-all-single-file-sections-2026-09-06/sections/15-S15.png) |
| 16 | [마지막 출구길](../../tmp/image-previews/sr18-all-single-file-sections-2026-09-06/sections/16-S16.png) |

## 기준과 범위

- 한 지점에는 적 한 마리, BonusWall 하나 또는 오브젝트 하나만 표시한다. 옆으로 여러 개를 배열하지 않는다.
- 기존 큰 루프 하행·북행·마지막 직선의 9개 표식은 같은 경로 위치에 유지했다. 예시의 X축 반올림과 실제 구간 중심선의 미세한 차이는 0.1유닛 미만이다.
- 짧은 구간에는 세 종류를 억지로 모두 넣지 않았다. 각 지점은 구간 양끝에서 최소 12유닛 떨어지며, 연속 경로에서 다른 지점까지 최소 64유닛 간격을 둔다. 경사 시작·끝 8개 실제 좌표를 읽고 경사 영역과 5유닛 여유에는 표식을 놓지 않는다.
- 첫 고가는 상부 평지의 적 두 지점과 하부 복귀 뒤 BonusWall, 두 번째 고가는 진입 전 적 한 지점 뒤 통과 구간으로 표시했다.
- 오브젝트는 종류가 확정되지 않은 위치 기호이며, 새로운 상자 모델·파괴 기능·회피 가능성을 구현한 것이 아니다.
- 도면에는 총 32개 대표 배치 칸(적 12, BonusWall 10, 오브젝트 10)이 있다. **이 수량은 5분 전투 밀도나 종류별 5초 등장 요구를 충족한 최종 배치안이 아니다.** 이전 그림에서 사용한 넓은 간격을 전 구간에 연장한 위치 검토도다. 최종 수량과 시간 간격은 별도로 확정해야 한다.
- 현재 실제 씬의 69명/14벽 배치를 이 도면으로 대체하지 않았다. 앞선 넓은 길의 생성형 일러스트도 근거로 사용하지 않았다.

## 재현·검증

`py -3.11 -X utf8 tools/draw_sr18_all_single_file_sections.py`

원본은 `map-concepts/sr18-full-enemy-bonus-flow-2026-09-06/full-flow-final.json`의 경로, 기존 상점 배치 기록, 경사 스팟 기록이다. 공통 도면 요소는 `tools/draw_sr18_single_file_examples.py`의 기본 지도·기호 함수를 재사용한다. 스크린샷 편집이나 이미지 생성이 아니라 좌표를 새 캔버스에 그린 도식이다.

[구간·좌표·간격 검사 기록](coverage.json). 16구간 포함, 이전 예시 유지, 32개 표식의 단일 배치, 경사/코너 여유, 구간을 넘는 최소 간격을 검사했다. 전체 위치도와 네 묶음 그림의 16구간, 가로길·고가의 개별 확대도에서 레이아웃을 확인했다. 출력 PNG 21개는 기존 파일을 덮어쓰지 않는다.
