# Ch4·5 예고 범위·실제 피해 대조 — cycle11

[PC 브라우저 전후 비교 갤러리](http://127.0.0.1:1626/telegraph-cycle11-20261004/) · [킥보드 원본 화면](../../outputs/chapter45-detailed-design-2026-10-03/telegraph-cycle11/windows/V21/021.png)

2026-10-04. 기존 cycle09/10 변경과 상세 기획 적용을 유지한 후속 검증이다. 실제 피해가 예고 밖에서 발생한 이동 잡기·차량 경로와 도로 아래 묻힌 경고 표시를 수정했다. 제품 변경은 코드 2개와 Jamsil 장면 1개다. 새 게임·챕터 구조·난이도 재설계는 하지 않았다.

## 재현과 적용

| 대상 | 변경 전 실제 근거 | 적용 |
|---|---|---|
| 학생 잡기 | G04에서 실제 StudentGrab 접촉 당시 플레이어는 반경 1.25m 표시 중심에서 1.941m 떨어져 있었다. 실제 타격 중심과는 0.970m, 판정 반경은 1.3025m였다. G04의 목표 골퍼는 먼저 죽었으므로 골퍼 피해 근거가 아니다. | 기존 돌진 경고 경로를 StudentGrab에도 사용. 이동 중 타격 위치를 덮는다. |
| 전도자 잡기 | P02에서 표시 중심 거리 1.710m에 실제 접촉. 전도자 피해는 HP58→55였고 앞선 PhoneShopper의 2 피해와 구분했다. | Preacher에도 같은 이동 경로 표시. |
| 잡기 후방 접촉 | R03에서 Acting 첫 프레임에 플레이어를 한 번 0.98m 뒤로 배치. 실제 물리 접촉이 약 0.048초 뒤 Contacts0→1, HP60→58을 만들었다. 이전 경로 시작은 배우 뒤 0.6475m에 그쳤다. | 시작 여유를 배우 뒤 1.5m로 확장. R04에서 같은 실제 접촉 위치가 표시 안에 들어왔다. 수동 피해·Hit·Contact·Tick 호출은 없었다. |
| 역주행 킥보드 | V01 실제 접촉 위치 z926.290, 표시 중심 z938.504. 작은 고정 표시 약 12m 앞에서 접촉했다. | 두 이동 끝점의 실제 BoxCollider 외곽과 플레이어 캡슐 여유를 포함하는 경로 사각형. |
| 측면 오토바이·택시 | V07/V08에서 실제 차량·플레이어 접촉이 작은 고정 표시의 옆에서 발생했다. | 킥보드 1, 오토바이 3, 택시 3, 총 7개에 경로 표시를 명시적으로 활성화. |
| 도로 아래 경고 | 독립 원본 화면 검토에서 킥보드 표시가 안 보였다. 지면 .145m에 표시 .035m였고 맨홀·철근도 사전 표시가 묻혀 있었다. | 실제 보이는 도로/보행로 높이를 측정해 15개 활성 위험의 표시 높이·두께를 조정. 몸체·충돌 위치는 유지. |

수정 파일:

- `Assets/ShooterSurvival/Scripts/Game/Chapters45/Chapter45RoleAction.cs`: StudentGrab/Preacher 경로 및 공유 후방 여유.
- `Assets/ShooterSurvival/Scripts/Game/Chapters45/Chapter45Hazard.cs`: 선택형 `showSweepPath`, 기존 표시 오브젝트의 런타임 사각형과 해제/복구 수명 관리.
- `Assets/ShooterSurvival/Scenes/Tools/Jamsil.unity`: 7개 활성화, 15개 표시 transform. Unity 저장 시 `showSweepPath: 0` 12개와 기존 `hideWhileArriving: 0` 302개가 기본값으로 직렬화됐다. 아주 작은 x/z float 반올림 외 다른 기존 장면 줄은 같다.

`TickRole`, `ContactPlayer`, `Fire`, `ConfigureCleanerWarning`, `OnTriggerStay`, 위험 `Tick`/`Hit` 본문은 복구 사본과 동일하다. 충돌체·몸체·재질 YAML 블록도 동일하다. ShoeTower 장면은 변경하지 않았다. 경고의 형태와 보이는 높이를 바꿨으며 피해량·속도·타이밍·쿨다운은 바꾸지 않았다.

## 현재 실행 증거

같은 native Unity Editor PID83488에서 초기 위치/선행 조우 상태를 지정한 짧은 창 60개, 후방 물리 재현 2개, 일시정지·수명주기 창 1개를 완료했다. 합계 63개다. 이후 Update·충돌·피해는 게임의 실제 실행으로 발생했다. 일반 전체 경로 완주나 메뉴/이어하기 재시험으로 세지 않는다.

최종 영향 표본 15개에서 1,245개 활성 상태를 대조했다. 이 수는 전부를 정밀 충돌 경계 검사했다는 뜻이 아니다. 변경 역할은 활성 타격 원주 16방향 및 접촉 위치의 경로 포함 여부, 변경 차량은 실제 충돌체 bounds와 접촉 위치 포함 여부를 검사했다. 맨홀·철근·기존 압박/횡단 위험은 활성 표시·몸체·종료 상태와 실제 접촉/회피 결과를 검사했다.

| 최종 표본 | 관측 결과 | 활성 대조 수 |
|---|---|---:|
| S21 학생 | HP54, 학생 접촉 3회 모두 경로 안. 지정 목표의 Contacts는 1. | 96 |
| P21 전도자 | HP55, 전도자 접촉 1회 경로 안. 다른 일반 접촉 피해 구분. | 25 |
| V21 / V22 킥보드 | 위험선 HP55.2·접촉1 / 오른쪽 회피 HP60·접촉0 | 52 / 48 |
| V23 / V24 오토바이 | 위험선 HP55.2·접촉1 / 왼쪽 회피 HP60·접촉0 | 80 / 81 |
| V25 / V26 택시 | 위험선 HP55.2·접촉1 / 왼쪽 회피 HP60·접촉0 | 78 / 80 |
| H11 / H12 맨홀 | 점유선 HP0·접촉1 / 왼쪽 회피 HP60·접촉0 | 101 / 210 |
| H13 / H14 철근 | 점유선 HP0·접촉1 / 왼쪽 회피 HP60·접촉0 | 102 / 156 |
| V27 / V28 기존 압박·횡단 | 각각 HP55.2·접촉1, 표시와 활성 상태 일치 | 57 / 47 |
| B21 농구 돌진 | 오른쪽 회피 HP60·접촉0 | 32 |

L02 수명주기 11개 검사 통과: native 일시정지 중 시간/몸체/표시 정지, 사전 경고→활성 이동, 안전선 피해 없음, 정상 종료 시 몸체·표시 함께 숨김, 취소 시 원래 mesh, 같은 재질/transform/충돌 속성, disable 및 ResetForRun 정리. 종료 후 disable/enable/reset은 명시적인 시험 조작이다. Editor 복귀 후 런타임 경고 mesh는 0개이며 QA callback도 없다. 컴파일 완료·오류 0. 63개 완료 결과의 수집된 게임 오류는 0이다. 아래 시험 도우미 오류까지 포함한 전체 Editor 세션이 오류 없이 진행됐다는 뜻은 아니다.

## 조사했으나 완료 판정을 확대하지 않은 항목

- 골프채: 5개 접근 조건에서 G02/G05 경고·공격은 관측했으나 접촉하지 않았다. G03/G04는 골퍼가 먼저 죽었다. 기존 원형 표시 1.25m와 계산된 타격 반경 1.88m 차이는 남은 기하 후보다. 실제 표시 밖 골퍼 피해를 재현하지 못해 이번에는 수정하지 않았다.
- TV: H03/H06에서 경고·낙하·종료는 관측했지만 TV 접촉은 0이었다. 관측 HP 감소는 가드 접촉이었다. TV의 위험 경계 접촉까지 검증했다고 하지 않는다.
- B1 카트: V04/V09에서 예고·횡단·종료, 카트 접촉 0. 고정 표시와 약 5.7m 횡단의 차이는 남은 후보이며 실제 표시 밖 카트 피해의 근거가 없다. B1 적 피해와 구분했다.
- 가드·매니저: 관측된 접촉은 기존 표시 안이었다. 모든 경계/방향이 확인됐다는 뜻은 아니다.
- 청소부: cycle10의 적용과 검증을 유지했다. `ConfigureCleanerWarning` 본문 동일, 표적 반복 시험 0. T03 주변에 청소부가 자연히 등장하는 것은 별개다.

투척은 지면 지속 피해와 구분했다. T02 접시 1발은 Acting 종료 뒤에도 이동했고, T03 팝콘 3발도 Recovering 중 남았다. T04 홍보원은 Warning1.443초→Acting2.604초→Recovering2.858초, 5발을 발사했다. 3.068초 HP60→58 때 역할 Contacts는 0, 투사체는 4발 남았다. 이는 조준 표시 종료 후 이동 중인 투사체 접촉이다. `SimpleProjectile` 자체의 보이는 물체·충돌·수명이 피해를 소유하며 소스는 바꾸지 않았다. 바닥 원이 모든 미래 탄도까지 나타낸다고 해석하지 않았다. PhoneShopper 등 평상시 몸 접촉도 예고 근접 공격과 별개다.

## 독립 화면·코드 검토

구현에 참여하지 않은 기존 AI 화면 검토자 2명이 원본 PNG를 총 142회 읽었다(40+50+36+16, 중복 이미지 포함). 초기 검토의 안 보이는 차량 경로 지적을 실제 지면 높이 수정으로 연결했다. 최종 검토에서는 긴 주황 경로, 위험선과 분리된 회피 방향, 차량·표시 동시 종료가 읽혔다. 맨홀·철근 16장에서는 위험물 등장 전 주황 경고와 빈 회피 방향이 보였으며 해당 선택 프레임 끝까지 위험물이 남아 종료는 미관측으로 기록했다.

코드 검토자 1명이 후방 캡슐 문제를 지적했고 R03/R04 실제 물리 재현으로 수정·종결했다. 최종 차이에서 새 P1/P2 지적은 없었다. 이는 AI 검토이며 사람의 반응 속도·재미·모바일 성능을 인증하지 않는다. 근접 배우/도구가 가리는 정확한 접촉 순간과 일부 차량 종료 연출의 자연스러움은 계속 한계다.

갤러리는 원본 해상도 PNG 559장과 비교 12쌍이다. HTML 포함 560개에 HTTP200 및 SHA256 일치를 검사했고 스크립트 구문 검사도 통과했다. 같은 초기 조건/입력의 가장 가까운 native 시각을 표시한다. 프레임 시각까지 완전히 같거나 픽셀 결정적이라는 주장은 하지 않는다. 브라우저 사람 조작 검증은 별도다. 링크는 이 PC의 localhost 서버다.

## 보존 및 비용

기준 제품 306개 중 승인된 3개만 달라졌고 303개는 SHA256 동일하다. 공유 자산 208개, 이전 복구 사본 210개, 이번 제품 복구 사본 4개, 보호 사용자 앱 파일 6개, persistent data 파일 1개를 비교했다. 기존 제품 53,461개는 존재를 검사한 수이며 모두를 새로 내용 해시한 수는 아니다. 요구사항 장부도 별도 PC 내부 사본을 만든 뒤 기존 60행·기존 필드를 보존하면서 해당 행에만 cycle11 근거를 추가했다.

관측 게임 설정 78개 동일. native registry 45개 중 비엔진 값 41개 동일. 엔진 session 값 3개의 자연 변화는 그대로 두었고 직접 registry/엔진 session 복원 쓰기는 0이다. 허용된 게임 키·QA Editor 설정 복구 영수증 67개를 보존했다. `PT_ResourcesCleanup`은 이번 시작 시 존재/false를 기록하고 그대로 확인했다. 기록되지 않은 과거 값까지 복원했다고 주장하지 않는다. 같은 Editor, 원래 장면/시작 장면/언어/QA 설정을 확인했다.

새 모델·텍스처·재질·제품 asset 생성 0, 유료/AI 생성 호출 0. 기존 표시 오브젝트·재질·충돌체를 재사용했다. 코드가 만드는 경고 mesh는 환경 AI 모델 생성물이 아니다. 이번 추가 최대치는 잡기 6개와 차량 7개에 52정점/26삼각형이며 기존 농구 경로는 제외한 수다. 기기 성능은 측정하지 않았다.

폰·APK·새 인증/설치·commit/push·외부 백업·기존 파일 삭제 없음. 기존 tracked 삭제 127개는 유지됐고 새 삭제는 0. branch `qa/chapters45-full-suite-20261002`, HEAD `295a18cab0ad4156baa9106077bc10a47ac6d734` 유지. 과거 전체 테스트의 통과/실패 수는 현재 결과로 재사용하지 않았다.

## 실패한 도우미와 복구

완료 창 63개 외 실패 시도 4개(G01/L01/R01/R02)도 삭제하지 않고 남겼다. G01은 목표 골퍼보다 앞선 층에 시작해 대상을 못 찾았고 다음 표본에서 시작 거리를 고쳤다. L01은 JSON으로 raw Vector3 배열을 보낼 때 `.normalized`가 재귀적으로 직렬화되어 JsonSerializationException과 ErrorPause가 발생했다. 도우미를 float 배열로 고쳤다. R01/R02는 이전 동적 `run_script` 어셈블리에 남은 callback 때문에 막혔다. 새로 컴파일한 같은 클래스의 static Stop만으로 과거 어셈블리의 구독이 해제된다고 볼 수 없었다.

정지 Edit 상태에서 `Telegraph` 도우미가 소유한 정확한 callback 6개만 제거하고 시험 유발 pause를 해제했다. 엔진 session 쓰기·Unity 재시작은 없었다. R03/R04와 L02는 그 뒤 성공했고 최종 잔여 callback은 0이다. 실패 시도도 저장/Editor 복구 결과를 포함한다.

## 근거 위치와 재현 범위

기본 폴더: `outputs/chapter45-detailed-design-2026-10-03/telegraph-cycle11/`.

- [검증 집계](../../outputs/chapter45-detailed-design-2026-10-03/telegraph-cycle11/verification.json), [보존](../../outputs/chapter45-detailed-design-2026-10-03/telegraph-cycle11/integrity.json), [제품 차이](../../outputs/chapter45-detailed-design-2026-10-03/telegraph-cycle11/product-diff-verification.json)
- [지면 측정/적용](../../outputs/chapter45-detailed-design-2026-10-03/telegraph-cycle11/ground-application.json), [최종 Editor](../../outputs/chapter45-detailed-design-2026-10-03/telegraph-cycle11/editor-final.json), [도우미 복구](../../outputs/chapter45-detailed-design-2026-10-03/telegraph-cycle11/helper-recovery.json)
- [화면 검토 A](../../outputs/chapter45-detailed-design-2026-10-03/telegraph-cycle11/independent-reviews/review-a.json), [화면 검토 B](../../outputs/chapter45-detailed-design-2026-10-03/telegraph-cycle11/independent-reviews/review-b.json), [최종 차량](../../outputs/chapter45-detailed-design-2026-10-03/telegraph-cycle11/independent-reviews/post-review-b.json), [최종 맨홀·철근](../../outputs/chapter45-detailed-design-2026-10-03/telegraph-cycle11/independent-reviews/fatal-review-a.json), [코드 검토 요약](../../outputs/chapter45-detailed-design-2026-10-03/telegraph-cycle11/independent-reviews/code-review-summary.json)
- `windows/<표본>/result.json`에는 연속 상태·실제 접촉·오류·native 프레임 시각이 있고 같은 폴더에 원본 PNG가 있다. R03/R04는 `rear-probe.json`, L02는 `lifecycle.json`도 포함한다.
- `source-tools/`에 실행 당시 case/runner/C# 도우미를 보존했다. workspace의 `run_department_routes.py` 공식 CLI wrapper에 의존하므로 독립 실행 패키지로 보지 않는다. 이미 완료된 표본 ID는 덮어쓰기를 거부한다. 추가 실행이 필요하면 새 출력과 새 native 저장 기준을 먼저 마련해야 한다.

이번 범위에서 수정 후 남은 실행 차단은 없다. 골프·카트의 기하 후보와 TV 접촉 경계, 일반 전체 경로/이어하기, 사람의 조작·재미, 기기 성능은 이 결과로 완료 처리하지 않는다.
