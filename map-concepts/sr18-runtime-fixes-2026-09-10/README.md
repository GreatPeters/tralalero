# SR18 실플레이 결함 수정 — 2026-09-10

이전 [연속 플레이 검증](../sr18-live-playtest-2026-09-09/README.md)에서 확인한 네 결함을 수정했다. `map2`의 SR18 씬에 적용하며 기존 길230개·Props715개·조우74행과 Data.xlsx 수치는 유지한다.

## 수정

1. **탄환 중복 반납:** BulletScript가 비활성화 전에 반환을 한 번만 허용한다. BulletPooler는 큐에 들어간 정식 루트를 추적해 중복 큐 삽입을 막고, 잘못 넘어온 자식 오브젝트를 파괴하지 않는다. 성공 발사 카운터는 초기화가 끝난 뒤 증가한다.
2. **고가도로 시야:** 실제 게임 카메라에 `NoryangjinCameraOcclusion`을 설치했다. 플레이어보다 높은 도로 중 카메라~플레이어/앞길 시야를 가리는 렌더러만 일시 숨긴다. 도로 모델·충돌·재질·경로는 유지하고, 통과/컴포넌트 해제/Play 종료 시 원상 복구한다. 플레이어가 위쪽 도로를 밟을 때 바닥은 숨기지 않는다.
3. **기존 가로등4개:** `Prop_Lights_X-49_Z-139`의 외형은 남기고 ObstacleStats와 피해 트리거4개를 껐다. 새 기믹표의 가로등은 변경하지 않았다.
4. **출구:** 마지막 길 안쪽 `(248, .10, 334.5)`에 기존 GameEndTriggerTag 완료 트리거를 넣었다. 실제 도착 시 `CanvasScript.YouWin()`으로 전진/전투를 멈추고 클리어 패널을 표시한다. 잘못 남아 있던 GAME OVER 문구를 STAGE CLEAR로 바꾸고 기존 LoadGame 버튼을 PLAY AGAIN으로 제공한다. 고속도로 자동 이동은 추가하지 않았다.

추가로, 점수 텍스트의 AnimatorController가 없을 때 ScoreInc를 호출하지 않아 경고를 막는다. 정상 컨트롤러의 점수 효과는 유지한다.

## 검증

- 중복 반납4개 회귀검사는 수정 전4개 실패 → 수정 후4개 통과. Water/Bomb 중복 반환·동시 대여·GFX 보존·수명 초기화를 다룬다.
- 미사일 수명5개, 카메라 가림/복원2개, 실제 씬 구성1개, SR18 기존 씬11개, 실제 프리팹 재사용2개도 통과했다. 점수 경고 재현1개도 수정 전 실패 → 수정 후 통과했다. 총26개 관련 검사 통과, 런타임/에디터 빌드 오류0 및 하네스 검사 통과.
- 같은 위치에서 카메라 변경 전후를 렌더링했다. 가리던 도로2개만 숨고 도로 충돌230개는 유지된다. 실제 Play에서 컴포넌트 해제 시 복원과 고가도로 위 바닥 가시성을 확인했다.
- 정상 체력 실행에서 첫 가로등 구간 Z−30..−14를 지나도 HP100이 유지됐다. 이후 일반 전투/기믹 사망은 기존 게임 규칙대로다.
- 연속 전체 실행은 실제 시작 처리·PlayerMove 입력·Update/FixedUpdate·전투·충돌을 사용했다. 후반 도달을 위해 시작 후 체력만10,000,000으로 올린 **검증 전용 조건**이며 TimeScale1·공격력50 시작·적/기믹 충돌은 유지했다. 정상 난이도 완주를 주장하지 않는다.
- 전체 연속 실행: 고정 선택25곳, 적25명 사망, 성공 초기화된 발사654회, 손상 탄환0, 중복 큐 항목0. 플레이어가 Z333.244에서 멈추고 승리 패널을 활성화했다. 수십 초 후에도 위치가 유지됐다.
- 연속 실행의 예외/발사 오류는0이었다. 그 실행에서 확인한 기존 점수 애니메이터 경고는 별도 재현 후 수정했다. 완료 패널의 잘못된 문구도 실제 화면 검토로 발견하여 수정했다.

## 증거 / 복구

- 씬 백업: `tmp/backups/sr18-runtime-fixes-2026-09-10/004712/before.unity`, 승리UI 보정 전 `win-ui-010256.unity`.
- 카메라 전후: `tmp/image-previews/sr18-runtime-fixes-2026-09-10/005102/before.png`, `after.png`.
- 연속 실행: `tmp/image-previews/sr18-live-playtest-2026-09-10/005227/timeline-005230.jsonl`, `win-verification.json`, `guided-005427/`.
- 해당 연속 실행 직후 `005227/win.png`는 문구 수정 전 GAME OVER가 보이던 증거이므로 최종 UI 이미지로 쓰지 않는다. **최종 UI는 `tmp/image-previews/sr18-live-playtest-2026-09-10/010733/win.png`**다. 마지막 UI만 확인하는 근거리 출구 검사에서 STAGE CLEAR/PLAY AGAIN을 확인했다. 전체 연속 주행 기록과 이 근거리 UI 검사를 혼동하지 않는다.
- 최종 코드로 일반 사격 후 경고/오류0을 확인했다. 실제 PLAY AGAIN 버튼의 기존 onClick을 실행하여 시작 상태74개 설정·적25명·신규 기믹42파트·체력100/100으로 복원되는 것도 확인했다.
- 테스트 코인2957·보석0·기존 배속은 종료 후 복원했다. 초기화 후100/100을 확인했고 사용자 업그레이드/스킨/Data.xlsx는 변경하지 않았다.

반복 검사 도구: `ProjectilePoolLifetimeTests`, `NoryangjinCameraOcclusionTests`, `Sr18RuntimeFixSceneTests`, `CanvasScoreFeedbackTests`; 연속 실행과 종료 검사는 `tools/autodrive-sr18-live-playtest.cs`, `tools/verify-sr18-win.cs`를 사용한다. EditMode 테스트 반환 직후 Play에 들어가지 말고 TestRunner 종료/정리를 확인한다.
