# 휴게소 TRELLIS 소품·인물 제작 — 90종 완료

2026-09-25: **모델 90/90종, 인물 8종/72동작 완료**. 남은 21종의 생성·보정·실제 시각 검토와 새 FBX/GLB 검증을 마쳤다. [완료 안내](COMPLETED-2026-09-25.md)에 최종 폴더, ZIP과 검증 영수증을 연결했다. 작업 전용 생성 실행기와 8189 백엔드는 종료했고 8772 갤러리는 유지한다. 이전 실패 원장과 후보는 보존하며, 이미 완료된 항목을 다시 생성하지 않는다. **[재부팅 체크포인트](REBOOT-RESUME.md)**는 이전 S10 중지 당시의 역사적 기록이다.

사용자가 기존 자동화의 저장값으로 소품 제작과 인물 본·애니메이션 추가를 요청했다. [제작 계약](contract.md)의 전체 범위를 완료했다. 이번 에셋을 기존 Unity 씬에 적용하는 작업은 포함하지 않았다.

- 작업 갤러리: http://127.0.0.1:8772/
- 최종 전체 ZIP: [reststop-assets-r1.zip](http://127.0.0.1:8772/reststop-assets-r1.zip), 6,319,598,606 bytes.
- 최종 폴더: `outputs/reststop-production-2026-09-24/delivery-r1/` — 1,444개 파일, 목록/재질/원본/검증 기록 포함.
- ZIP CRC 검사, JSON 377개 파싱, 갤러리 자원 402개 및 원본 PNG 90개 HTTP 검사, 전체 ZIP 다운로드 SHA-256 일치 확인. 연결된 브라우저가 없어 화면 조작 검증은 수행하지 않았다.
- 입력: `outputs/reststop-production-2026-09-24/inputs/` — 90종 준비/디코딩 확인 완료.
- 작업 상태: 같은 폴더의 `status.json`, `assets/.trellis-automation/state.json`.
- 최종 소품 선택본: 자동화 통과 결과 + `final-overrides.json`의 직접 수정 선택본. 자동화에서 거절된 후보를 최종본으로 사용하지 않는다.
- 최종 인물 리그 선택본: `accepted-rigs.json`. 개별 `fresh-rig-validation.json`, `visual-review.json`, `video-validation.json`과 실제 동작 영상을 함께 보관한다.
- 인물 본체 H01~H08: 주차 안내원·요리사·바리스타·계산원·청소원·주유 직원·여행객·경찰.
- 원래 69종에 환경 E01~E07/장비 P01~P06을 추가했다. 선택적 장식 제안은 추가하지 않았다.
- [차량 게임 크기 기준](vehicle-game-scale-reference.json)은 기존 v4에서 1.8배 확대한 뒤 실제 측정한 값이다. 새 TRELLIS 원본은 저장값 `longest_side=0`을 유지하며, 이 기준은 후속 조립에서 작은 차량 문제를 반복하지 않도록 함께 제공한다.
- 인물 8종/72클립 묶음: [갤러리 ZIP](http://127.0.0.1:8772/reststop-humans-rigged-8-r1.zip). 전체 소품 완료와는 별개이며, 인물 8종의 검증만 완료된 묶음이다.

## 실행

아래는 완료 전 실행 방식의 기록이다. 현재 90종이 선택 완료되어 새 생성이나 재개는 필요하지 않다. 예전 자동 후보의 `review_needed`/`halted`는 실패 이력을 보존한 상태이며 최종 선택은 `final-overrides.json`과 함께 판단한다. 재부팅 뒤 홈페이지가 닫혔다면 [완료 안내](COMPLETED-2026-09-25.md)의 읽기 전용 갤러리만 재시작한다.

`tools/run-reststop-trellis-production.py`가 설치된 자동화 엔진을 불러온다. 저장값은 `saved-settings.json`, 실제 작업값은 `effective-settings.json`이다. 입력/출력/작업 전용 포트만 변경하고 주요 생성·품질·재시도·60초 휴식 설정은 유지한다.

시각 검토는 주 에이전트가 수행한다. `review-pending.json`의 실제 다각도 이미지를 열고 `result` 경로에 원래 스키마의 JSON 판정을 작성하면 자동화가 이어진다. 현재 실행 프로세스가 종료되어도 시도 원장으로 재개한다. 출력 폴더의 잠금을 무시해 중복 배치를 실행하지 않는다.

검토 도구에도 전용 Python 환경과 `-X utf8`을 사용한다. [Windows 실행 환경·한글 기록 주의점](../../docs/solutions/workflow-issues/preserve-python-runtime-and-unicode-through-powershell-2026-09-25.md)을 참고한다.

`tools/watch-reststop-review.py`는 최대 45초 동안 실제 검토 대기 상태를 확인하고 비교 시트 경로를 반환한다. 판정을 자동 승인하지 않는다. 판정 JSON은 같은 폴더의 임시 파일에 완성한 뒤 원자적으로 교체해 실행기가 부분 기록을 읽지 않도록 한다. 천장처럼 아래가 실제 사용 면인 부품은 `reststop-production-review-render.py --underside-only`로 하부 렌더도 확인한다. 현재 범위와 완료 조건은 [실행 계획](../../docs/exec-plans/active/reststop-trellis-production.md)에 연결했다.

```powershell
$reststopResumeStamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$reststopResumeRecord = "outputs/reststop-production-2026-09-24/runner-resume-$reststopResumeStamp-resource.json"
& 'C:/AI/TRELLIS2-AMD/venv/Scripts/python.exe' -X utf8 tools/run-limited-generation.py --script tools/run-reststop-trellis-production.py --record $reststopResumeRecord -- --only V05,V06 --defer F03,F04,R11,V04 --resume
```

F03·F04 자동 감량 중단, R11 완료 상태 조회 오류와 V04 비정상 재부팅 중단 기록을 보존한다. 현재 재개 인자는 `--only V05,V06 --defer F03,F04,R11,V04 --resume`이다. 먼저 `runner.json`의 실제 실행 PID가 종료됐는지 확인하고 새 리비전의 로그/자원 기록을 사용한다. `--defer`는 명시한 기존 중단 항목만 별도 처리 대상으로 남기며, 실패 판정이나 원장을 통과로 바꾸지 않는다. V04 첫 시도는 소비된 상태를 유지하며 `tools/retry-reststop-interrupted-shape.py`가 별도 원장으로 남은 최대 2회만 허용한다. 설치된 자동화 파일과 작업 ID를 바꾸지 않는다. [후처리 복구](../../docs/solutions/workflow-issues/preserve-uv-seams-and-corner-normals-for-trellis-foliage-2026-09-24.md)와 [완료 기록 재확인](../../docs/solutions/integration-issues/recheck-comfy-history-before-replaying-lost-prompts-2026-09-24.md)을 참고한다.

진행 갤러리는 상태 변경 시 자동 갱신하며, 브라우저에서 새로고침해 확인할 수 있다. 모델 다운로드는 실제 완료된 항목에만 나타난다.

## 현재 확인된 결과와 보정

- F08 첫 의자: 형태 417.87초·텍스처 229.72초. 감량은 별도 시간이다. 이 시간은 이 PC의 이 입력/첫 설정에서 관찰한 값이며 전체 작업 완료 시간을 보장하지 않는다.
- 자동 감량 두 방식에서 좌판 아래 프레임의 노멀·표면 문제가 남아 검토 필요로 기록했다. 허용 자동 시도 횟수를 초기화하거나 강제로 통과시키지 않았다.
- 직접 보정 F08-r3에서 하부 프레임의 가중 노멀과 손상된 베이크 노멀 채널만 수정했다. 쿠션 PBR·UV·형상과 14,365삼각형을 유지했다. 앞뒤·위·중립 시각 검토 및 새로운 FBX/GLB 임포트 검증을 통과해 최종 선택했다. 원본의 작은 다리 패임과 봉제선/광택 차이는 기록했다.
- 기본 검토 렌더의 프레이밍이 긴 부품을 잘라 작업용 렌더러에서 여유를 늘렸다. 모자에 가려진 인물 얼굴 확인을 위해 낮은 정면 뷰를 추가했다. 이미지가 잘린 것을 메시 누락으로 오판하지 않는다.
- Blender 경로는 임포트 전 절대 경로로 해석하고 `--factory-startup`으로 실행한다. 상대 경로 시험에서 일부 검토 이미지가 `C:/outputs/...`에 저장된 이력이 있어, 사용하지 않고 프로젝트 안에 다시 렌더했다.

인물 리깅은 `tools/rig-reststop-proxy.py`에서 실제 몸체에 뼈를 맞추고 닫힌 임시 프록시의 bone heat 가중치를 원본 UV 메시로 옮긴다. `tools/reststop-rig-actions.py`가 9개 동작을 작성하며 `tools/verify-reststop-rig.py`와 `tools/render-reststop-rig-preview.py`가 새 임포트 및 실제 동작 영상을 확인한다. 이전 `rig-reststop-production.py`, `rig-reststop-from-donor.py` 방식의 실패 후보는 보존하되 배포하지 않는다. 검증 완료 인물과 선택 리비전은 `accepted-rigs.json`을 기준으로 갤러리에 표시한다. 기존 게임 씬이나 프리팹은 수정하지 않았다.

TRELLIS 검토 대기 구간에서 후보 하나를 제작하는 명령은 다음과 같다. 완료 명단에 자동 추가하지 않으며, 실제 동작 렌더를 확인한 뒤 시각 검토 기록을 별도로 작성해야 한다.

```powershell
& 'C:/AI/TRELLIS2-AMD/venv/Scripts/python.exe' -X utf8 tools/build-reststop-rig-candidate.py --id H05 --revision r2 --source "outputs/reststop-production-2026-09-24/assets/H05_102aff5a49/stages/m1_t1/low1/model.glb"
```

예시 명령은 새 수정 후보가 필요한 경우에만 사용한다. 기존 승인본을 다시 만들 필요는 없다. 동일 리비전의 `.blend`가 있으면 덮어쓰지 않고 중단한다.

리깅 교훈: [뼈 위치를 맞춘 뒤 가중치를 전사하기](../../docs/solutions/workflow-issues/fit-bones-before-transferring-trellis-skin-weights-2026-09-24.md). 현재 진행 수치는 `progress.json`, 상세 판정은 [실행 기록](execution-log.md)을 확인한다.
