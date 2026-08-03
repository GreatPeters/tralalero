# 노량진 모바일·정적 최적화 가이드

## 적용 결과

노량진 맵의 움직이지 않는 환경 메시만 `Batching Static`으로 분류한다.
맵 루트나 `Props` 전체를 정적으로 지정하지 않으므로 적, 회전 스팟,
이동 트리거처럼 런타임에 상태나 Transform이 바뀌는 오브젝트는 영향을
받지 않는다.

| 항목 | 맵1 | 맵2 |
| --- | ---: | ---: |
| 안전한 정적 Renderer | 184 | 653 |
| 활성 환경 삼각형 수(적용 전) | 1,487,389 | 3,026,495 |
| 활성 환경 삼각형 수(적용 후) | 435,419 | 1,935,637 |
| 삼각형 감소 | 70.7% | 36.0% |

같은 `MapTool_Camera`를 1080×1920으로 렌더한 에디터 비교에서는
배치/드로우콜이 `92 → 59`, 화면 삼각형이 `736,505 → 183,711`로
감소했다. 이 수치는 동일 카메라의 전후 비교용이며 Android 실기기
프레임률을 대신하지 않는다.

동일한 맵1 `MapTool_Camera` 1080x1920 에디터 렌더 비교에서는 배치와
드로우콜이 각각 `92 -> 59`, 화면 삼각형은 `736,505 -> 183,711`,
정점은 `542,121 -> 186,793`, SetPass는 `23 -> 22`로 감소했다.
이 수치는 같은 씬의 전후 비교용이며 Android 기기 성능을 대신하지 않는다.

정적 분류는 각 환경 `MeshRenderer` 오브젝트의 기존 플래그를 보존하면서
`Batching Static`만 추가한다. 정적 환경에는 불필요한 모션 벡터도 끈다.
다음 중 하나라도 해당하는 배치 루트는 동적으로 간주하여 정적 배칭에서
제외하고, 잘못 남아 있던 `Batching Static` 플래그도 제거한다.

- 이름이나 프리팹 경로가 적 오브젝트임을 나타내는 경우
- `MonoBehaviour`, `Animator`, `Animation`, `Rigidbody`,
  `CharacterController` 또는 Joint가 있는 경우
- `SkinnedMeshRenderer`, 파티클, Trail, Line, Cloth 또는 Light가 있는 경우
- NavMesh Agent나 Playable Director가 있는 경우

따라서 회전 스팟, 적 이동 발동 스팟, 적 캐릭터와 그 밖의 런타임 동작
오브젝트는 계속 움직일 수 있다.

## 물과 제작 가이드

- 맵1의 바다 배경 인스턴스 118개는 씬 인스턴스 오버라이드로
  2-triangle 메시와 저비용 타일 수면 머티리얼을 사용한다.
- 맵2의 수면 타일 196개도 씬 인스턴스 오버라이드로 2-triangle 메시를
  사용한다.
- 두 경우 모두 원본 수면 프리팹과 원본 모델 메시·머티리얼은 변경하지
  않는다. 최적화된 메시 에셋만 별도로 생성해 씬 인스턴스가 참조한다.
- 수면 Renderer의 그림자, 라이트/리플렉션 프로브, 모션 벡터와 동적
  오클루전 힌트를 끄고, 게임 동작에 쓰이지 않는 비-트리거 Collider를
  비활성화한다.
- `MapTool_Work_Floor`, `MapTool_Work_Grid`, `MapTool_Origin_Post`는
  `EditorOnly`로 태그되어 플레이어 빌드에서 빠진다.

## 카메라와 텍스처

`MapTool_Camera`는 후처리를 사용하지 않으며 URP Depth Texture와 Opaque
Texture 복사를 명시적으로 끈다. 이 설정은 이름이 다른 효과 카메라에는
적용하지 않는다.

두 노량진 씬에서 실제 사용하는 Stage01 Noryangjin 텍스처 96개에는
Android 전용 임포트 제한이 적용된다.

- 디테일·베이스·노멀 계열 48개: 최대 `1024`
- Emission, Metallic, Roughness, Occlusion, Mask 계열 48개: 최대 `512`
- Android 포맷: Automatic compressed, compression quality `50`
- mipmap 및 streaming mipmap 활성화
- `Mobile` 품질 단계의 Texture Streaming 활성화

이 제한은 노량진 Stage01 텍스처 루트 아래에서 두 씬이 참조하는
텍스처에만 적용한다. 다른 스테이지나 서드파티 에셋의 임포트 설정은
일괄 변경하지 않는다.

## 실행 방법

현재 열려 있는 노량진 씬만 최적화하려면 다음 메뉴를 실행한다.

`Tools/맵 제작 도구/노량진 맵 제작/최적화/현재 씬 모바일 최적화`

이 명령은 Undo를 기록하고 씬을 Dirty 상태로 남기므로 결과를 확인한 뒤
직접 저장한다.

맵1과 맵2를 함께 최적화하고 저장하려면 다음 메뉴를 실행한다.

`Tools/맵 제작 도구/노량진 맵 제작/최적화/맵 1·2 모바일 최적화`

이 명령은 두 씬 중 하나라도 저장되지 않은 변경을 갖고 있으면 작업을
중단한다. 먼저 사용자 편집을 저장하거나 취소한 뒤 다시 실행해야 한다.
두 명령은 반복 실행해도 같은 상태를 유지하도록 작성되어 있다. 맵툴로
새 환경 프리팹을 배치하거나 복제할 때도 같은 안전 분류가 적용된다.

## 검증

Unity EditMode에서 다음 필터를 실행한다.

- `NoryangjinMapStaticOptimizerTests`: 정적/동적 분류, 기존 정적 플래그
  보존, 반복 실행, 카메라 복사 차단, 원본 수면 프리팹 보존, 텍스처
  예산과 두 실제 씬 계약을 검사한다.
- `MapProductionToolMenuTests`: 두 최적화 메뉴가 제작 도구 표면에
  유지되는지 검사한다.

2026-07-30 검증 결과는 최적화 테스트 `11/11`, 맵 씬 보호 테스트
`2/2`, 메뉴 테스트 `2/2` 통과다. 전체 EditMode는 `395/401`로,
남은 6개는 작업 전부터 존재하던 게임플레이 통합 null 1개와 맵툴
팔레트·비주얼 기대값 5개다.

저장 후에는 다음 저장소 검증도 실행한다.

```powershell
dotnet build Assembly-CSharp.csproj -nologo
dotnet build Assembly-CSharp-Editor.csproj -nologo
powershell -ExecutionPolicy Bypass -File tools/validate-agent-harness.ps1
```

2026-07-30 적용본은 최적화 테스트 `11/11`, 씬 보호 테스트 `2/2`,
제작 메뉴 테스트 `2/2`, 전체 EditMode `395/401`을 통과했다. 전체
테스트의 남은 6건은 적용 전부터 존재한 게임플레이 통합 null 1건과
맵툴 팔레트/비주얼 기대값 5건이다. 두 C# 빌드는 경고·오류 없이
완료됐고 저장소 하네스 검증도 통과했다.

맵2는 현재 Build Settings에서 제외된 정적 검토 씬이다. 위 수치는
에디터의 씬 계약과 메시 삼각형 수를 기준으로 한 결과다. 최종 Android
기기에서는 대표 저사양 기종의 Unity Profiler와 Memory Profiler로
배치/SetPass, 프레임 시간, static batching 메모리와 텍스처 스트리밍
피크를 다시 확인하는 것이 유용하다.
