# Ch4·5 최종 경로와 TV 경계 검증 — cycle12

[PC 브라우저 전후·검증 화면](http://127.0.0.1:1626/validation-cycle12-20261004/) · [최종 TV 피격 원본](../../outputs/chapter45-detailed-design-2026-10-03/validation-cycle12/tv/TV41/017.png) · [실제 패배 안내](../../outputs/chapter45-detailed-design-2026-10-03/validation-cycle12/lifecycle-death-ui/02-defeat.png)

2026-10-04. cycle11의 잡기·차량 경로·경고 높이 수정을 보존하고 TV의 남은 실제 경계를 검사했다. TV 피해가 기존 타원 밖에서 발생하는 문제와 잘못된 차량 피해 안내를 수정했다. 최종 저장본으로 Ch4 좌우와 Ch5 B1/영화관을 실제 Unity에서 모두 클리어했다. 새 모델·유료 호출·폰·APK·commit/push 작업은 없다.

## 제품 변경과 근거

- `Assets/ShooterSurvival/Scenes/Tools/ShoeTower.unity`: TV 한 개에 기존 cycle11 `showSweepPath`를 적용했다. 실제 BoxCollider의 수평 외곽, 플레이어 캡슐 반경·오프셋과 여유를 반영한다. 기존 경고 오브젝트·재질과 코드를 재사용하며 런타임 사각형은 정점4/삼각형2다. 새 모델·텍스처·재질 자산 생성은0이다.
- `Assets/ShooterSurvival/Scripts/Player/PlayerDamageCause.cs`: 기존 enum 0–19 뒤에 `FallingObject=20`을 추가하고 TV 원인만 연결했다. 피격 안내는 `낙하물에 맞음`, 회피 안내는 `주황색 낙하 표시 밖으로 피하고 물체와 표시가 사라진 뒤 이동하세요.`다. 다른 기존 문자열·값은 동일하다.
- 기존 scene 줄을 비교하면 TV `damageCause: 8→20` 한 줄만 달라진다. 새 필드 직렬화는 TV true1개와 다른 hazard false17개다. 피해·낙하 시간·충돌체·위치·재질·다른 게임 동작은 유지했다. Jamsil, cycle11 RoleAction/Hazard 파일도 baseline 해시와 동일하다.

기존 표시에서 TV01/03/05/07 네 축 안쪽 접촉은 모두 실제 피해4.8을 만들었으나 플레이어 중심이 기존 타원 밖이었다. 바깥쪽 짝은 피해0이었다. 최종 사각형은 실제 피격 위치를 모두 포함했다. 이 ±8cm는 **물리 캡슐 중심의 접촉 경계** 기준이며 주황 표시 테두리 기준이 아니다.

## TV 전용 검증

완료 창은 총27개다. 표시 수정 뒤 TV21–34 14개는 native 검사49개, 경고 관측2,615개와 활성 관측1,624개를 통과했다. 실제 접촉6회 모두 당시 표시 안이었다. 이후 피해 안내만 추가한 최종본에서 TV41 실제 피격과 TV42 낙하 중 일시정지를 추가 확인했다. 이 두 창 native 검사7개도 통과했고, 별도 분석에서 TV42 HP60·접촉0을 확인했다.

| 조건 | 관측 |
|---|---|
| 네 방향 경계 안/밖 | 안쪽 각1회·HP55.2, 바깥쪽 모두 접촉0·HP60 |
| 경고 중 실제 횡이동 | 안전 회피·HP60 |
| 낙하 활성 약0.15초 뒤 이동 | 안전 회피·HP60 |
| 착지 약0.225초 뒤 중앙 진입 | 실제 접촉1회 |
| 종료 약85ms 전 중앙 진입 | 실제 접촉1회 |
| 종료 뒤 중앙 재진입 | 약3.46초 유지 중 접촉0 |
| 경고 중 / 실제 낙하 중 일시정지 | 시간·몸체 정지, 재개 후 회피 가능 |
| 표시 종료 | 실제 몸체와 표시 함께 비활성 |

첫 접촉은 완전 착지보다 약0.10–0.14초 앞설 수 있다. 낙하 몸체가 플레이어 캡슐 높이에 먼저 닿기 때문이다. 경고는 그 전부터 종료까지 유지된다. 착지 뒤에도 기존 활성 시간이 남으므로 안내에서 물체와 표시가 사라진 뒤 이동하도록 했다.

TV 전용 창은 층·초기 위치를 설정하고 다른 전투를 종료하며 자동 발사·전진을 멈춘 격리 검사다. 실제 Update·물리 충돌·피해는 게임이 처리하고 횡이동은 PlayerMove를 썼다. 경계·종료 probe의 1회 위치 설정을 일반 플레이로 집계하지 않았다. 모든 대각선·몸체 각도·사람 반응 시간 검증은 아니다.

## 최종 저장본 네 경로

같은 Unity Editor PID83488의 한 Play 세션에서 1배속, 초기HP60·공격력8, 관측 지연350ms와 제한된 이동 입력으로 진행했다. 강제 승리·체력 증폭·이동 순간이동 없이 실행했다. Ch4→Ch5는 실제 자동 연결을 두 번 탔고, 첫 Ch5 종료 뒤 다음 Ch4 선택만 QA가 수행했다. 영어 locale로 경로 관측 후 원래 ko-KR로 복원했다.

| 경로 | 활성 시간 | 종료HP | 종료공격력 | 코인 증가 | 보석 증가 | 리프트 |
|---|---:|---:|---:|---:|---:|---:|
| Ch4 왼쪽 | 302.92초 | 56.0 | 8.00 | 7686 | 35 | 0 |
| Ch5 B1 | 272.19초 | 54.0 | 8.00 | 7684 | 40 | 5 |
| Ch4 오른쪽 | 299.78초 | 50.0 | 10.56 | 7403 | 5 | 0 |
| Ch5 영화관 | 302.85초 | 37.2 | 8.00 | 8516 | 5 | 6 |

42/42 경로 검사 통과, 수집 오류·Animator 경고0. 네 번의 일반 일시정지, 두 리프트 일시정지, 영화관 생존 중 일시정지에서 위치·게임 시간 정지를 확인했다. 분기·중간보스/매니저 전투·신발 획득·종료를 포함한 실제 경로다. 첫1F 30초 게이트와 영화관 생존30초는 별도 timeline에 남긴다. 보상은 최초/재클리어 조건의 기존 지급량을 따르며 실제 클리어 뒤 완료 콜백을 반복해도 추가 지급되지 않았다. 전체 경로 통과가 모든 적의 모든 공격 경계를 개별 검사했다는 뜻은 아니다.

## 사망·재시작·보상

최종 TV 패배 UI 창은12/12 통과했다. 초기HP4에서 실제 TV 접촉으로 사망하고 유효 피해량은 남은HP4로 기록됐다. 패배 후 몸체·경고 정지, 미완료 유지, 단기 실패의 진행/클리어 보상0, 뒤늦은 중복 승리 UI 호출의 지급0을 확인했다. 실제 Replay가 새 로비HP60·초기 TV latch로 돌아가고 재진입 시1회 피해 후 정상 종료했다. 원본 패배 PNG와 active TMP 텍스트를 모두 보존했다.

영화관 사망·Replay 창은15/15 통과했다. 초기HP2·발사 중지 조건에서 실제 관객 접촉 사망, 생존 중단과 관객 정리·고정 조작 해제·카메라FOV68/추적 복원, 새 로비·타이머0·신규 첫 물결3명, 재진입 일시정지 및 제자리 조준을 확인했다. 이는 안내 수정 이전의 격리 검사다. 영화관 코드/씬 동작은 이후 바뀌지 않았고, 최종 일반 영화관 경로는 별도 전체 클리어했다.

## 보존·독립 검토·제한

이번 baseline306파일 중304개 해시는 같고 위2개만 의도한 변경이다. 공유208자산과 이전 복구210사본, 보호 앱6파일·persistentData1파일은 동일하다. 기존 제품53,461파일 존재와 기존 tracked 삭제127개의 동일성을 확인했다. 이번 삭제0, branch/HEAD 그대로다. 정확한 native 게임 설정 복원 영수증은34개이며 관측 저장값78개가 fresh baseline과 일치한다. 엔진 registry45값 중 비세션41값은 동일하고 자연 변경된 엔진 세션 값은 유지했다. 엔진 키 복원 쓰기·직접 registry 쓰기는0이다. PT_ResourcesCleanup은 이번 시작의 존재/false와 같으며 과거 미기록 값 복원을 주장하지 않는다.

Editor는 같은 PID의 멈춘 Edit 상태, QA callback0·경고 런타임 mesh0, 원래 씬/시작씬·locale·9개 QA 설정으로 돌아왔다. 정상 스크립트 재컴파일 완료·컴파일 오류0이다. 검증 도우미의 실패 시도는 숨기지 않고 [QA 이슈](../../outputs/chapter45-detailed-design-2026-10-03/validation-cycle12/qa-issues-final.json)에 남겼다. 초기 사망 검사1실패는 남은HP로 제한되는 피해량의 기대값 오류였다. 초기 scene 저장의 자동 light/TMP 부수 직렬화는 native 방식으로 정확히 되돌렸다. 첫 부분 경로는 안내 수정 발견으로 중단했고 최종4경로와 섞지 않았다. Screenshot 직후 같은 프레임 Replay로 로비가 찍힌 문제는 지연 캡처로 재검증했다.

독립 읽기 전용 AI 시각 검토1명은 원본 이미지24장, 최종 안내2장, 보정된 패배1장을 확인했다. 코드 검토1명은 변경 범위·enum 호환·경계 해석·낙하 일시정지와 잘못된 패배 캡처를 확인했다. Unity 변경은 root만 수행했다. 시각 검토는 인간 재미 평가가 아니다.

골프채·B1 카트의 표시 밖 피해는 **미재현** 상태로 남겼으며 결함을 가정한 변경은 하지 않았다. 이번에 메뉴의 새 게임/이어하기를 별도 재실행하지 않았다. 기존 검증을 현재 재검증으로 표시하지 않는다. 폰 성능·APK·전체1135검사·기존52실패 재조사·장시간 누수 보증·모든 대각 접촉·인간 반응/재미는 범위 밖이다. 남은 실행 차단은 없다.

## 근거와 재현 도구

- [TV 최종 분석](../../outputs/chapter45-detailed-design-2026-10-03/validation-cycle12/tv-final-verification.json)
- [최종 네 경로](../../outputs/chapter45-detailed-design-2026-10-03/validation-cycle12/routes-final/suite-summary.json)
- [TV 사망·Replay](../../outputs/chapter45-detailed-design-2026-10-03/validation-cycle12/lifecycle-death-ui/summary.json) · [영화관 사망·Replay](../../outputs/chapter45-detailed-design-2026-10-03/validation-cycle12/lifecycle-cinema/summary.json)
- [보존 검사](../../outputs/chapter45-detailed-design-2026-10-03/validation-cycle12/integrity.json)
- [갤러리 원본·HTTP 검사](../../outputs/chapter45-detailed-design-2026-10-03/validation-cycle12/gallery.json)
- [도구 보관 안내](../../outputs/chapter45-detailed-design-2026-10-03/validation-cycle12/source-tools/README.txt)

실행한 명령: `python -X utf8 trellis-recovery/run-routes12.py`, `python -X utf8 trellis-recovery/run-lifecycle12.py death-ui`, `python -X utf8 trellis-recovery/analyze-final12.py`, `python -X utf8 trellis-recovery/audit-validation12.py`. 기존 출력 경로를 덮어쓰지 않도록 재실행을 거부한다. 새 검증에는 새 출력과 새 baseline이 필요하다.
