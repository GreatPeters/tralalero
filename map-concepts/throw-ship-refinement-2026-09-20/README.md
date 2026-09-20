# 뚱보 상자·배 방향 후속 수정

**후속 거절:** 사용자가 이 상자 배치에서 몸통 관통을 지적했다. 아래 단일 손 부착 방식은 최종 승인된 결과가 아니다. [양손 투척 재수정](../two-hand-crate-2026-09-20/README.md)이 이를 대체한다. 배 수정은 유지한다.

2026-09-20 사용자 후속 요청 두 개를 저장했다. 기존 상자와 얼굴의 겹침을 줄이고, 배가 길 쪽을 바라보며 대기하다가 같은 방향으로 발사하게 한다.

## 적용

- FatMan 소스 프리팹과 SR18 배치 6개: 상자 scale 50 → 35 (70%). 상자의 위쪽 테두리 지점을 오른손 원점에 고정하도록 localPosition을 메시 bounds와 localRotation에서 계산한다. 얼굴 아래, 몸 앞옆으로 들며 원래 투척 애니메이션은 유지한다.
- Ship 소스 FirePos: local +Z가 선체 대포의 +X를 향하도록 회전한다. SR18 선체는 가까운 도로 표면을 향하는 방향으로 저장했다.
- `ObstacleStats.FireFromBow`: 대기 방향을 변경하지 않고 FirePos.forward로 CannonBall을 발사한다. 이전 플레이어 예측 조준은 이번 요청으로 대체했다. 발사 시작은 기존 7.5초 이동 거리, 뚱보는 5초/2.8초 반복을 유지한다.
- 런타임 손 IK 시도는 제거했다. 기존 애니메이션을 유지하면서 상자의 실제 잡는 지점을 손에 맞추는 방식이 최종 결과다.

## 검증

- 두 C# 프로젝트 빌드 성공. 기존 ithappy SplineSpeed 미할당 경고 외 오류 없음.
- `CombatRouteFeedbackTests`: 13/13 통과. 배 테스트는 대기 회전 유지, 정확한 FirePos 생성 위치와 포구 방향 속도를 확인하도록 갱신했다.
- `play-final/report.json`: 실제 반복 투척 3회, 캐릭터 이동 0m, 손과 상자 잡는 지점 오차 0m. 머리 뼈 기준점과 상자 AABB의 최소 거리 약 .122m이며, 이는 전체 얼굴 메시의 충돌거리로 해석하지 않는다. 실제 준비/발사 화면을 별도로 시각 확인했다.
- 배의 대기→발사 회전 변화 0°, 포탄 방향과 FirePos.forward 내적 1.0, 생성 위치 오차 0m. 범위 진입 프레임에 발사했다.
- Unity는 저장된 SR18 Edit Mode로 복구했다. 실제 휴대폰 실행은 하지 않았다.

## 화면·재현

- [확인한 브라우저 갤러리](http://127.0.0.1:6753/throw-ship-refinement/) (로컬 서버).
- 원본 사본: `tmp/image-previews/throw-ship-refinement-2026-09-20/`.
- `tools/apply-throw-ship-refinement.cs`: 저장된 SR18와 소스 프리팹의 좁은 범위 적용.
- `tools/verify-throw-ship-refinement.cs`: Play Mode 투척/발사 검증과 촬영.
- `tools/build-throw-ship-gallery.py`: 원본 이미지를 그대로 복사하는 갤러리 생성.
- 이전 `tools/apply-combat-route-fixes.cs`도 최신 상자 잡는 지점과 배 방향을 사용하도록 갱신했다.

`before/`는 작업 전 사본이다. `play/`는 상자 위치만 내린 후보, `play-grip*`는 채택하지 않은 IK 후보다. 최종 결과는 `play-final/`이다. 상자만 아래로 옮기면 손에서 떠 보였고, 이 리그의 런타임 IK 보정은 포즈/자식 위치가 흔들렸다. 최종 자산의 테두리 고정은 추가 런타임 보정 없이 손 접촉을 유지한다.
