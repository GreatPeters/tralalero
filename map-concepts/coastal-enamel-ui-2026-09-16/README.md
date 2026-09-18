# Coastal Enamel UI — 적용 완료

2026-09-16, 사용자가 선택한 `exec-1d132918-ead4-46b7-89e0-e2c910a1dde2.png` / 1번 스타일을 실제 게임에 연결했습니다.

## 적용 범위

노량진(SR18), 고속도로, 휴게소의 로비, 전투 HUD, 일반·챕터 강화, 꾸미기, 설정/일시정지, 패배/완료, 스토리 영상/챕터 전환, 네 가지 튜토리얼 안내, 보석 부족 팝업.

- 파랑·아이보리 패널, 노란 주요 버튼, 은색 테두리와 읽기 쉬운 실제 TMP 문구.
- 챕터 강화는 회당 공격력·체력 각각 +5%, 해금한 챕터별 최대 5회. 기존 레벨·소유 키와 가격 체계를 유지합니다.
- 꾸미기 목록의 피부 8종은 2D 아이콘, 위의 큰 미리보기는 실제 모델입니다. 다시 열면 현재 장착 아이템부터 보여줍니다.
- 영상은 실제 기존 파일로 재생하며, 네 개 장면 표시와 건너뛰기·다시보기·다음 장면이 연결돼 있습니다.
- 설정의 실제 소리/진동/슬라이더, 이어하기/재도전, 보상형 광고 흐름을 유지합니다. 제공되지 않은 약관·개인정보 URL은 기존처럼 비활성입니다.

## 분리 소재

- `Assets/ShooterSurvival/UI/CoastalEnamel/`: 원본 크기의 PNG 46개.
- `CoastalEnamel-Sprites.psd`: PNG와 같은 해상도의 소재별 46레이어. 처음에는 첫 레이어만 보이며, 나머지 레이어의 표시를 켜서 편집합니다. 래스터 소재 레이어이며 문구·가격은 Unity의 실제 TMP 텍스트로 별도 구성했습니다.
- `CoastalEnamel-UI-kit.zip`: PNG, PSD, 소재 명세서 묶음.
- `sprite-manifest.json`: 출처, 잘라낸 영역, 실제 알파, Unity 9-slice 경계.
- `sources/`: 보관된 생성 시트와 프롬프트. 가려진 배경·텍스트 없는 패널은 재구성했고, 상점 일러스트/챕터 그림은 승인 시안에서 잘랐습니다. 기존 일반 강화 아이콘 일부는 재사용했습니다.
- `tools/prepare-coastal-ui.py`: 자르기·알파 추출·원본 크기 PSD 생성. 작은 목록용 테두리는 같은 팔레트로 만든 단순 UI 도형입니다.

## 검증

- Native Unity 테스트 **24/24 통과**: Coastal UI 9, 챕터 강화 10, 꾸미기 인벤토리 5. 전체 결과는 `native-tests.json`.
- 세 씬의 실제 진입/닫기/탭/목록 끝 접근, 재화 부족 무차감, 현재 착용 모델, 일반·챕터 실제 구매, 중복 무차감, 설정 토글, 영상의 연속 Next/Replay/Skip 및 리소스 해제를 확인했습니다.
- 실제 챕터 영상 두 개도 렌더링과 건너뛰기를 확인했습니다. 승리 화면의 보석 보상 문구는 `ChapterProgression.clearRewardText`에 다시 연결했습니다.
- 1080×2340과 1080×1920에서 캡처 및 목록 끝 접근 확인. 증거: `tmp/image-previews/coastal-enamel-ui-2026-09-16/final/` 및 `interaction-checks/`.
- UI 밖 씬 오브젝트/컴포넌트 **27,960개 동일**: `gameplay-scene-comparison.json`.
- 재화·업그레이드·꾸미기·튜토리얼 등 추적한 **79개 키 복원 확인**, 최종 코인141/보석0. 설정 스냅샷은 인터랙션 테스트 전에 캡처했습니다. `preference-restoration.txt`.
- `dotnet build Assembly-CSharp.csproj -nologo`, `dotnet build Assembly-CSharp-Editor.csproj -nologo`, `powershell -ExecutionPolicy Bypass -File tools/validate-agent-harness.ps1` 통과.
- 최종 검증 구간의 Unity 콘솔에 새 오류 없음. 테스트 후 원래 SR18 씬과 1080×2340 Game View로 돌아왔으며 Play Mode를 종료했습니다.

## 재적용

공식 Unity Pipeline의 `eval_file`로 `tools/coastal-ui-import.cs`, `tools/coastal-ui-apply.cs`를 순서대로 실행합니다. 적용은 저장된 깨끗한 Edit Mode에서만 가능합니다. 신선한 상태 백업은 `tmp/backups/coastal-enamel-ui-2026-09-16/`에 있습니다.

## 범위와 한계

Unity Editor에서 적용과 동작을 검증했습니다. 새 APK 빌드/기기 배포는 이번 요청에 포함되지 않았습니다. 실제 게임 배경·모델·영상 및 현재 가격/상태를 쓰므로 생성 시안의 예시 화면을 픽셀 단위로 붙인 결과는 아닙니다. `first-pass/`와 `refined/`는 수정 과정의 증거이며, 완료 화면은 `final/`입니다. 보석 보상 아이콘 최종 확인은 `final/Noryangjin_MapTool_Mode_SR18/20-victory-final.png`입니다.
