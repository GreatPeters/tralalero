# 맵툴 시작 스테이지 선택

## 2026-09-26: 테스트 속도 2배 추가

편의 탭의 테스트 속도를 `1배 (기본) / 2배 (테스트) / 3배 (테스트)`로 확장했다. 세션 값 검증·읽기와 버튼 인덱스 매핑을 함께 수정했다. 편집 모드에서는 선택만 저장하고, 기존 Play 적용 및 종료 시 1배 복원을 유지한다. 에디터 어셈블리 빌드 오류·경고 0, 2배/3배 선택 보존과 4배 거절의 네이티브 assertion 3개를 통과했다.

## 후속: 첫 시작 동영상 ON/OFF

같은 편의 탭에서 시작 스테이지 아래에 **첫 시작 동영상 (테스트) → ON / OFF**를 추가했다. 기본 ON은 기존 첫 자동 재생을 유지하고, OFF는 다음 Play에서 시작 영상 패널을 비활성화해 바로 시작 화면을 보여 준다. 수동 다시보기는 허용한다. Play 중 변경은 비활성화한다.

`OpeningStoryUI.EditorAutoPlayEnabled`는 UNITY_EDITOR 안의 SessionState 값이다. `OnEnable`에서 `ShowPage`/`PlayMovie`보다 먼저 확인하므로 OFF 경로는 영상 준비를 호출하지 않는다. 씬의 `autoPlayMovie`나 Data.xlsx/PlayerPrefs를 수정하지 않으며 플레이어 빌드에는 해당 분기가 없다.

검증: OFF 자동 시작 차단, ON 유지, 편집 모드 보존, 수동 열기 보존, 자동 재생이 아닌 기존 이야기 보존의 **5개 네이티브 assertion 검사 통과**. 기록은 `tmp/maptool-start-stage-2026-09-25/opening-assertions.json`. 런타임·에디터 어셈블리 빌드 오류 0(런타임 빌드에 기존 SplineSpeed 경고 1개). 작성 중이던 HighWay 씬과 dirty 상태는 유지했다. 실제 Play 전환/영상 디코더를 새로 실행한 통합 검사로 주장하지 않는다.

에이전트도 에디터에서 `OpeningStoryUI.EditorAutoPlayEnabled = false` 또는 `true`로 동일 설정을 바꿀 수 있다.

## 시작 스테이지 선택

상태: 구현·컴파일·네이티브 설정 검사 완료.

맵툴 **편의 → 시작 스테이지 (테스트)**에 `데이터 / 1 / 2 / 3`을 추가했다. 현재 프로젝트의 세 실행 맵을 기준으로 1=노량진 SR18, 2=HighWay, 3=RestStop으로 매핑했다. 맵 내부의 세부 구간으로 순간이동하는 기능과는 구분한다. 사용자에게 두 의미를 확인하는 선택 질문을 보냈으나 응답이 없어서, 세 플레이 맵 기준이라는 해석을 알리고 구현했다.

- 번호 선택은 다음 Editor Play에 적용한다. 편집 중인 씬은 열거나 이동하거나 저장하지 않는다.
- `데이터`는 치트 전에 지정된 Play 시작 씬을 복원한다. 원래 지정이 없었다면 현재 열린 씬에서 시작하는 Unity 기본 동작으로 돌아간다. Data.xlsx에 새 시작값을 쓰지 않는다.
- 선택은 SessionState에 저장되며 플레이 빌드에 포함되지 않는다. Play 중에는 변경을 비활성화한다.
- EnteredPlayMode에서 원래 시작 설정을 복원하고, EnteredEditMode에서 선택을 다시 적용한다. 에디터 종료 때도 원래 설정으로 돌린다. 다른 에디터 도구가 설정을 바꾼 경우 그 변경을 덮어쓰지 않는다.

구현: `NoryangjinMapToolTestStartStage.cs`, `NoryangjinMapToolWindow.DrawTestStartStageControls`.

검증: `dotnet build Assembly-CSharp-Editor.csproj -nologo -v:q` 오류·경고 0. 시작 씬 매핑 3개, 원래 사용자 지정 씬/기본 null 복원, 잘못된 번호 2개 거절, 다른 도구 설정 보존 등 **8개 NUnit assertion 사례를 열린 Unity에서 직접 실행**했다. 이미 HighWay에 저장하지 않은 편집이 있어 Test Runner의 저장 창을 우회했고, 검사 전후 동일한 씬과 dirty 상태를 유지했다. 기록은 `tmp/maptool-start-stage-2026-09-25/assertions.json`, 편집 중 씬의 사본은 `authoring-before.unity`다. 세 씬을 실제 Play로 전환하는 통합 검사는 수행하지 않았다.

편의 탭을 실제 에디터에 열었다. 다른 활성 앱이 창 일부를 가리고 있어 전체 화면 캡처 검증으로 간주하지 않는다. 사용자 작업 화면을 강제로 전환하지 않았다.

에이전트용 동일 선택 경로:

```powershell
unity command --project-path . run_script --file tools/verify-maptool-start-stage.cs --entry VerifyMapToolStartStage.Select --args '[3]'
```

0=데이터, 1~3=각 맵. 기본 동작 근거는 Unity 6.2의 [playModeStartScene API](https://docs.unity3d.com/6000.2/Documentation/ScriptReference/SceneManagement.EditorSceneManager-playModeStartScene.html)다.
