# SR18 전투·보너스·기믹 기획 그림

상태: 대화의 배치 초안을 시각화한 **미적용 기획 그림**. 밸런스 확정안이나 실행 가능한 전투 배치 파일이 아니다.

이 자료는 구역·부분 패턴 예시다. 시작부터 출구까지의 전체 적군·BonusWall 연결은 [2026-09-06 전체 흐름 초안](../sr18-full-enemy-bonus-flow-2026-09-06/README.md)을 우선한다.

- [전체 배치도](../../tmp/image-previews/sr18-encounter-planning-2026-09-05/01-overall-plan-v2.png): 실제 SR18 길·상점 위에 A~F 구역과 전투 묶음 17개, 고정 보너스 위치 7개, 기믹 묶음 위치 6개를 개념 표식으로 표시했다. 표식은 개별 적 수나 최종 배치 예산이 아니다.
- [위험·보상 선택 확대](../../tmp/image-previews/sr18-encounter-planning-2026-09-05/02-risk-reward-choice-v2.png): 같은 길의 일반/엘리트 보너스와 우측 접근 시 적을 발동시키는 구상이다. 실제 BonusWall·경비원·검형 적 프리팹을 사용했다. 보너스 효과는 랜덤이며 두 보너스의 강제 상호 배제 기능을 구현했다는 뜻이 아니다.
- [기믹→전투→회수 확대](../../tmp/image-previews/sr18-encounter-planning-2026-09-05/03-gimmick-combat-reward-v2.png): 실제 구멍·적·보너스 프리팹으로 판단을 순서대로 나누는 구간을 보여준다.

스크립트는 `tools/render-sr18-encounter-planning.cs`, 프리팹과 예시 Transform 기록은 `preview-manifest.json`이다. 현재 맵과 팔레트 설정을 읽어 별도 Unity PreviewScene에서 렌더했다. 적 행동·충돌·획득 로직은 그림에서 실행하지 않으며, 설명 기호도 실제 씬에는 생성하지 않았다.

원본 씬 디스크 해시와 dirty 상태가 렌더 전후 동일함을 확인했다. 길·상점·프리팹·공유 재질은 저장하거나 수정하지 않았다. 실제 적용 전에는 적의 본체/투사체 범위, BonusWall 획득 판정과 1초 접촉 제한, 기믹의 카메라 가림·동시 위협량을 검증해야 한다.
