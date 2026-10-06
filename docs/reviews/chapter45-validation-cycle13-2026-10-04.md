# 메뉴·챕터 진입과 종료 연결 — cycle13

[PC 원본 화면 갤러리](http://127.0.0.1:1626/validation-cycle13-20261004/) · [공물 결과 원본](../../outputs/chapter45-detailed-design-2026-10-03/validation-cycle13/entry-exit-01/07-shoe-result.png) · [돌아온 로비](../../outputs/chapter45-detailed-design-2026-10-03/validation-cycle13/entry-exit-01/08-result-return-lobby.png)

2026-10-04. cycle12 최종 저장본에서 메뉴 시작·지원 재개·Ch3→4·Ch4→5·공물 결과→로비 연결을 검사했다. **73/73 native 검사 통과, 수집 게임 오류0, 제품 변경0**이다. 기존 일반 네 경로는 반복하지 않았다. 독립 검토에서 확인한 QA 실행기의 복원 경계만 수정하고 별도 실패 경로를 검증했다.

## 실제 지원되는 메뉴와 확인한 동작

현재 로비는 `좌우로 움직여 게임 시작` 안내를 사용한다. 별도 `새 게임/이어하기` 저장 슬롯 메뉴는 없다. 로비 시작은 새 판이며 지갑·해금·영구 강화/장비 저장을 지우지 않는다. 설정의 `계속하기`는 **현재 Play 세션에서 일시정지한 판을 재개**한다. `다시 도전`과 결과의 `돌아가기`는 같은 챕터 로비를 다시 연다. 중간위치 저장 또는 앱 재실행 후 중간위치 복구 기능은 만들지 않았다.

| 연결 | 이번 실제 확인 |
|---|---|
| Ch4 로비→시작 | HP60·공격8·거리0에서 기존 시작 callback이 동작하고 시간이 흐름 |
| 로비 설정 | 계속하기/다시 도전은 숨김. 설정 위에서 시작 callback은 차단되고 닫아도 자동 시작하지 않음 |
| 일시정지→계속하기 | 시간·거리·위치가 정지한 뒤 같은 씬 handle과 누적 시간에서 재개. 시작 callback으로 판이 초기화되지 않음 |
| 일시정지→다시 도전 | 실제 버튼 클릭으로 씬 재로드. 완료·목표·분기·리프트 상태0인 살아 있는 로비와 동일 지갑 |
| Ch3→Ch4 | 기존 native 승리 callback 이후 실제 Jamsil 씬. 저장 보석60/해금4 유지, 카메라 정상·이전 인트로 차단 없음 |
| Ch4→Ch5 | 같은 경로로 실제 ShoeTower 씬. 저장 보석95/해금5 유지, 시작 가능·카메라 정상 |
| 실제 신발 트리거→결과 | 정지 중 획득/보상 없음. 재개 뒤 실제 물리 trigger가 완료. 2초 결과 지연 뒤 공물 전용 문구·보석+40·돌아가기 표시 |
| 결과→돌아가기 | 실제 버튼 클릭으로 ShoeTower 새 로비. 보석135·코인33031·해금5 유지, 목표 시각/claim latch 복원 |
| 돌아온 로비→설정→새 판 | 기존 계속하기가 남지 않으며 다시 시작해 HP60·공격8·거리0부터 진행. 중복 보상 없음 |

두 전환은 scene-authored `nextChapterMovie=null`이었다. 화면/건너뛰기 표본이 없으며 **로딩을 통한 장면 연결만** 확인했다. 연결 영상·Skip 버튼을 검사했다고 주장하지 않는다. 없는 영상이나 Ch6를 새로 만들지 않았다. 결과의 `돌아가기` 목적지는 별도 전역 메뉴가 아니라 ShoeTower 로비다.

## 입력과 조건 설정의 범위

메뉴는 실제 활성·상호작용 가능 Button을 확인하고 화면 중앙 EventSystem raycast의 첫 대상이 해당 버튼인지 검사한 뒤 pointer click callback을 실행했다. 하드웨어 마우스/터치 입력을 발생시키지는 않았다. 로비 시작은 기존 `CanvasScript.PlayerPressedStartButton` callback을 직접 호출했으므로 8px 시작 스와이프 판정의 입력 장치 전체 경로 증거는 아니다.

Ch3/4는 기존 캠페인 전환 fixture 방식대로 시작한 판에서 `Canvas.YouWin`을 호출해 연결만 검사했다. 일반 전투 클리어로 집계하지 않았다. Ch5는 기존 fixture의 완료 전제·선택 상태·최종 접근 위치 설정을 재사용한 뒤 실제 물리 `OnTriggerEnter/Stay`로 신발을 획득했다. `TryClaim`이나 `ClaimGoal`을 직접 호출하지 않았다. 실제 일반 HP/공격력 네 경로의 전투·리프트·신발 획득 증거는 [cycle12](chapter45-validation-cycle12-2026-10-04.md)에 별도로 있다.

## 도구로 해결한 문제와 사람 평가가 필요한 부분

이번 범위에서 미해결 상태로 재현된 제품 결함은 없다. 독립 코드 검토가 발견한 **QA 실행기 문제**는 해결했다. `Prepare`가 TutorialDone을 바꾼 후 `Configure`가 거절되면 `try/finally` 밖이어서 복원을 건너뛸 수 있었다. 둘을 try 안으로 옮기고 준비 성공·Play 요청을 추적했다. 정상 연결 검사 전체를 반복하지 않고, Edit 상태에서 의도적으로 잘못된 씬 이름을 전달해 Configure 사전조건을 거절시켰다. 준비 중 변경은 TutorialDone1개, 복원 후 관측78개 모두 일치, Play 요청0·엔진 키 쓰기0·원래 clean Editor 상태였다. 수정 전 도구는 실행 금지 증거로 보존했다.

추가로 도구로 좁혀 검사할 수 있는 범위는 하드웨어 시작 스와이프 입력과 여러 화면비에서의 버튼 위치/클릭 영역, 아직 미재현인 골프채·카트 접촉 후보다. 이번 연결 검사에서 이 항목을 결함으로 확정하거나 완료로 표시하지 않았다. 이미 통과한 네 전체 경로를 무작정 반복할 이유는 발견하지 못했다.

사람에게 필요한 평가는 로비의 좌우 시작 제스처를 처음 보고 이해하는지, `계속하기/다시 도전`의 차이를 바로 이해하는지, 위험 예고가 실제 반응 속도에 맞는지, 전투·이동의 지루함과 난이도, 보상 획득의 만족감이다. 독립 원본9장 검토에서는 다음 행동과 글자 가독성에 막힘이 없었다. 다만 최종 결과의 큰 보석 그림과 신발 획득 문구가 함께 있어 구체적으로 어떤 신발을 얻었는지 전달하는 힘은 약할 수 있다. 돌아가기 연결 오류가 아니며 사람의 해석·만족도 평가 항목으로 남겼다. 새 그림/모델로 임의 변경하지 않았다.

## 저장·작업 보존

검사 전에 coin/jewel/chapter_unlocked/chapter_rewarded_3/4/5/TutorialDone **7항목의 존재·int 타입·값**과 locale, QA Editor9설정, native registry 읽기 snapshot을 fresh 캡처했다. 게임 실행과 거절 검사 각각 native 복원 영수증이 있다. 최종 코인33031·보석30·해금2이며 원래 없던3/4/5보상 플래그와TutorialDone도 원래 부재 상태다. 관측78저장값, native 비세션41값, 공유208자산, 기존 복구210사본, 보호 앱6파일·persistentData1파일이 같다. 엔진45값 중 세션 식별/횟수의 자연 변경은 유지했으며 복원 쓰기0·직접 registry 쓰기0이다.

제품306파일 해시가 전부 cycle12 최종본과 같다. 기존 제품53,461파일 존재, inherited tracked 삭제127개 동일·이번 추가 삭제0. branch `qa/chapters45-full-suite-20261002`, HEAD `295a18cab0ad4156baa9106077bc10a47ac6d734` 그대로다. 같은 Unity Editor PID83488은 정지된 clean Edit로 돌아왔고 QA callback0·경고 runtime mesh0·원래 씬/시작씬/locale/QA설정을 확인했다. PT_ResourcesCleanup은 이번 시작의 존재/false와 같으며 미기록된 과거 값을 복원했다고 주장하지 않는다.

새 생성·유료 호출·폰·APK·commit/push·외부백업·기존파일삭제0. 전체1135검사/기존52실패 재조사·기기 성능·장시간 안정성 검사를 하지 않았다. 남은 실행 차단은 없다.

## 증거와 재현

- [73개 실제 검사](../../outputs/chapter45-detailed-design-2026-10-03/validation-cycle13/entry-exit-01/summary.json)
- [준비 후 Configure 거절/복원](../../outputs/chapter45-detailed-design-2026-10-03/validation-cycle13/configure-rejection-01-summary.json)
- [최종 저장·자산 보존](../../outputs/chapter45-detailed-design-2026-10-03/validation-cycle13/integrity.json)
- [실제 메뉴 callback 목록](../../outputs/chapter45-detailed-design-2026-10-03/validation-cycle13/menu-edit-inventory.json)
- [독립 코드 검토](../../outputs/chapter45-detailed-design-2026-10-03/validation-cycle13/independent-reviews/code-review.json) · [수정 후 검토](../../outputs/chapter45-detailed-design-2026-10-03/validation-cycle13/independent-reviews/code-review-followup.json) · [원본9장 시각 검토](../../outputs/chapter45-detailed-design-2026-10-03/validation-cycle13/independent-reviews/visual-review.json)
- [검사 도구 이슈](../../outputs/chapter45-detailed-design-2026-10-03/validation-cycle13/qa-issues.json) · [원본 갤러리 해시/HTTP](../../outputs/chapter45-detailed-design-2026-10-03/validation-cycle13/gallery.json) · [도구 보관 안내](../../outputs/chapter45-detailed-design-2026-10-03/validation-cycle13/source-tools/README.txt)

실행: `python -X utf8 trellis-recovery/run-flow13.py`(당시 entry-exit-01), 수정 후 `python -X utf8 trellis-recovery/run-flow13.py --verify-configure-rejection`, `python -X utf8 trellis-recovery/audit-validation13.py`. 기존 출력 덮어쓰기를 거부한다. 새 실행에는 새 baseline과 출력 ID가 필요하다.
