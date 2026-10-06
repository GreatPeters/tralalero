# 자연 주행 입력 검증 — cycle15

[PC 로컬 원본 갤러리](http://127.0.0.1:1626/validation-cycle15-20261004/) · [Ch5 첫 착지 원본 PNG](../../tmp/image-previews/chapter45-model-production-2026-10-02/validation-cycle15-20261004/natural-ch5-01-08-natural-route-milestone.png)

2026-10-04. **최종 Ch4 27/27, Ch5 37/37 검사가 통과했다. 이번 제품 코드·자산 변경은0개다.** 각102/102,121/121 단계가 완료됐고 최종 검사 오류는0개다. 이전 실패 기록은 별도로 보존했다. 사람의 빠른 손 떼기/재배치는 아직 확인이 필요하다.

## 실제 주행 범위

| 항목 | Ch4 | Ch5 |
|---|---|---|
| 출발 | 기존 QA 선택으로 Jamsil 새 로비 | 기존 QA 선택으로 ShoeTower 새 로비 |
| 시작·이동 | 실제 OS 느린 수평 드래그와 마우스 이동 | 동일 |
| 도달 | 첫 분기560m에서 왼쪽 선택, 반대 드래그 후 단일 선택 유지 | 1층 활성30초 조건, 첫 에스컬레이터5초 이동, 2층 착지 후 조작·전투 |
| 최종 거리 / 게임 시간 | 592.06m / 92.60초 | 199.60m / 41.75초 |
| 체력 / 공격력 | 60→60 / 8 | 60→58(착지)→56(최종) / 8 |
| 실제 발사체 | 149 | 60 |
| 관측 최대 프레임간 차선 변화 | 0.305m | 0.313m |

위치·체력·공격력·적/목표 상태 fixture를 쓰지 않았고 시작·이동·분기 callback도 직접 호출하지 않았다. 자동 사격과 충돌 피해는 제품 코드가 처리했다. 각 씬 handle이 유지됐고 관측된 시간·진행 거리는 단조 증가했으며 `run begin`은 각1회였다. 임의의 순간이동·승리 처리는 없다. 두 챕터를 따로 선택했으므로 Ch4 끝→Ch5 전체 연결이나 처음부터의 캠페인 연속 완주 증거로 확대하지 않는다. 기존 QA 준비 과정의 TutorialDone 임시 설정과 원복은 저장 영수증에 남아 있다.

cycle14의 분기/신발 검사는 위치와 선행 조건 fixture를 사용했다. 이번 첫 분기와 첫 층 연결 검사는 로비부터 연속 이동한 별도 증거다. 이전 cycle12는 이동 함수를 직접 호출했으며 이번 입력 증거를 대신하지 않는다.

## 입력 경계와 초점

로비에서 누르고 Game View→Inspector로 초점을 옮긴 뒤 해제·복귀해도 시작되지 않았다. 느린4px 누적 이동은 실제 시작을 만들었다. 주행 중10px씩 느리게 오른쪽으로 움직이다 누른 채 반대로 움직이면 좌우 이동이 반전됐다.

누른 채 일시정지는 기존 Pause 버튼을 실제 mouse-down으로 선택하고 OS Return을 보내 기존 EventSystem Submit을 사용했다. 직접 PauseGame 호출이나 새 키 기능은 없다. 누른 상태의 일시정지, 메뉴 위 이동, 해제 동안 시간·거리·차선이 멈췄고 Resume 버튼 클릭 후 같은 실행을 계속했다. Ch4 출발 구간과 Ch5 출발/착지 뒤에서 확인했다.

포커스 이동은 같은 Unity Editor의 기존 Inspector와 Game View 사이에만 이뤄졌다. `Application.isFocused=false`를 실제 관측했고 복귀 후 기존 차선과 실행을 유지했다. 다른 앱을 활성화하거나 입력하지 않았다. Unity도 Editor Play 중 Game View의 초점 변화에 따른 콜백을 문서화한다. [공식 문서](https://docs.unity3d.com/ScriptReference/MonoBehaviour.OnApplicationFocus.html). 외부 앱 전환/휴대폰 Home 경계의 검증은 아니다.

자동 운전은 화면에 투영된 렌더러 관측을350ms 지연해 판단하고100ms 간격으로 OS 입력을 보냈다. 실제 가림 판정이나 사람의 반응 실험은 아니다. 첫 분기 방향은 내부 경로 메타데이터를 읽어 정했으며, 사용자가 시각 안내만 보고 길을 고를 수 있다는 증거는 아니다. 최종 원본은1080×2340 하나의 기존 Editor 프리셋이다.

## 중단 기록과 남은 사람 확인

`natural-ch4-01`은 Editor가 임시933×784 크기를 반환하여 입력 보호 장치가 중단했다. 고정 해상도로 돌아오기 전 입력을 보류하도록 도구만 보정했다. `natural-ch4-02`의 분기 후 엄격한 차선 동일성 검사는600m 곡선/직선 경계를 걸쳤으므로 타당한 불변식이 아니었다. 이 실패를 제품 포커스 결함으로 취급하지 않았다.

동일한02 실행에서 **같은 Editor tick의 mouse-up→중앙 이동(0.5061~2.0575ms)**에 맞춰1.524~1.712m 차선 변화가4회 기록됐다. 최종 도구는 Unity가 해제를 관측한 후 별도 tick에서 중앙 이동과 다음 누름을 전달한다. 이 조건에서는 두 자연 주행 모두0.8m 초과의 관측 변화가 없었다. 하지만 이 결과만으로 초기 변화의 제품 원인이나 해결을 확정할 수 없다. **다음 유효한 확인은 Unity에서 사람이 빠르게 손을 떼고 다시 잡을 때의 실제 움직임이다.** 제품 입력 코드를 추정으로 바꾸지 않았다. 이 한계는 [QA 기록](../../outputs/chapter45-detailed-design-2026-10-03/validation-cycle15/qa-issues.json)에 남겼다.

독립 코드 검토가 착지 뒤 차선 측정 누락과 입력 중단 시 해제 안전성 두 가지를 지적했고 QA 도구에서 보정했다. 새 확정 P1/P2 제품 결함은 없으며 빠른 해제 한계는 미해결 검증 항목으로 남겼다. 독립 시각 검토는 최종 원본26장을 확인했다. 에스컬레이터/2층 도착 안내와 버튼의 화면 내 배치는 보였으나 정지 이미지로 연속 이동이나 분기 전 시각적 선택 이해를 인증하지 않는다. 기존 Combat Harness overlay는 그대로다.

## 복원과 보존

동일 Unity PID83488의 clean Edit로 복귀했다. 원래 씬·play 시작 씬·QA9설정·시간·locale 및 Game View 인스턴스/위치/크기/해상도/줌/low-resolution/포커스/전경 HWND/커서가 fresh 기준과 일치한다. 화면 비교13항목이 모두 일치했다. 관측 저장78개와 native 비엔진41개 값이 일치한다. coin33031/jewel30/chapter_unlocked2를 복원하고 원래 없던 rewarded3/4/5 및 TutorialDone의 부재도 복원했다. 다섯 실행의 복원 영수증이 모두 성공했다.

제품306개 해시, 공유208자산, 기존210복구본, 보호 앱6파일, persistentData1파일이 동일하다. 기존53,461경로가 모두 존재하고 inherited tracked 삭제127개 외 새 삭제는0개다. branch `qa/chapters45-full-suite-20261002`, HEAD `295a18cab0ad4156baa9106077bc10a47ac6d734`도 그대로다.

Unity가 자연 갱신한 엔진 세션 식별자/횟수는 유지했다. 엔진 키 복원 쓰기·직접 registry 쓰기는0이다. PT_ResourcesCleanup은 이번 fresh 기준인 exists/false를 유지하며 과거 미기록 값을 복원했다는 주장은 하지 않는다.

생성 모델·추가/재사용 배치 자산·유료 호출·새 인증·설치·폰·APK·commit/push·외부 백업·기존 파일 삭제는 모두0이다. 전체1135 검사와 기존52 실패 조사는 반복하지 않았다. 이 회차는 입력 회귀 검증이며 미술 완성도나 전체 게임의 재미 인증을 추가하지 않는다.

## 증거

- [Ch4 최종27검사](../../outputs/chapter45-detailed-design-2026-10-03/validation-cycle15/natural-ch4-03/summary.json) · [Ch5 최종37검사](../../outputs/chapter45-detailed-design-2026-10-03/validation-cycle15/natural-ch5-01/summary.json)
- [관측 범위와 단일 실행·진행 분석](../../outputs/chapter45-detailed-design-2026-10-03/validation-cycle15/coverage.json)
- [저장·자산·엔진 읽기 감사](../../outputs/chapter45-detailed-design-2026-10-03/validation-cycle15/integrity.json) · [화면/초점 정확 복원](../../outputs/chapter45-detailed-design-2026-10-03/validation-cycle15/display-final.json)
- [독립 코드 검토](../../outputs/chapter45-detailed-design-2026-10-03/validation-cycle15/independent-reviews/code-review.json) · [독립 시각 검토](../../outputs/chapter45-detailed-design-2026-10-03/validation-cycle15/independent-reviews/visual-review.json)
- [원본26장과 HTTP 해시 검증](../../outputs/chapter45-detailed-design-2026-10-03/validation-cycle15/gallery.json)

재현 도구는 workspace `trellis-recovery/run-input15.py`, `validation-input15.cs`, `audit-validation15.py`다. 기존 실행 ID를 재사용하지 말고 새 fresh 기준과 새 ID를 사용한다. 원본 로그와 실패한 실행을 삭제하지 않았다.
