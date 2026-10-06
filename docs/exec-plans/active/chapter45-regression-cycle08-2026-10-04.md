# UI 수정 후 실제 플레이 회귀 — cycle08

최신 위임: 현재 코드에서 Ch4 두 분기, Ch5 B1/영화관, 위험경고 중첩, 층 이동,30초 생존, 실물 신발 획득과 보상을 검증한다. 재현 결함만 수정한다. 공용 HUD/메뉴 전체 번역은 확장하지 않는다. 60개 요구사항 내 남은 결함과 미검증/범위 밖 항목을 구분한다.

현재 기준: cycle07 완료 후 PID83488 clean idleShoeTower. 이번 시작 시점의 코드·씬·locale 등306개 hash, 게임 설정6개(coin,jewel,chapter_unlocked,chapter_rewarded_4,chapter_rewarded_5,TutorialDone;모두int)의 존재/값/타입과 locale, EditorSessionState9개 및 시계/시작씬을 캡처했다. 관측 PlayerPrefs78개와 실제 Unity native registry는 읽기 비교용으로 캡처했다. native registry 직접 쓰기/전체 import/엔진 세션 복원은 금지한다. 허용한 게임6키 중 달라진 항목만 PlayerPrefsAPI로 복원한다. 과거 snapshot을 새 기준으로 사용하지 않는다.

게임 코드는 아직 변경하지 않는다. 기존 cycle05의350ms 관찰 지연·제한된 PlayerMove 입력을 재사용하되 과거 전체 저장 카탈로그 복원 코드는 새 runner에서 제거했다. 기본60HP/공격8,1배속으로 거리왼쪽→실제 자동Ch5→B1, 이후 QA가 다음Ch4장면을 선택해 거리오른쪽→실제 자동Ch5→영화관의4경로를 진행한다. 모든 경로 영어Locale에서 UI수정 경로를 사용한다. 이전 한국어 표시 보존은 cycle07 evidence로 구분한다. 이동·전투·승리를 강제로 설정하지 않는다.

일반 플레이·실제 리프트·생존전 pause/resume 및 완료 후 중복 보상 방지 호출을 검사한다. 완료 콜백 반복은 실제 완주 이후에만 실행하며 완주 유도에 사용하지 않는다. 게임플레이 경로 통과와 별도 인위적 경고 중첩 fixture는 구분한다. 폰/APK/전체1135검사/추가지출/설치/인증/commit/push/외부백업/기존파일삭제/Editor재시작 없음.

상태: 네 실제 경로 실행 시작. 결과는 outputs/chapter45-detailed-design-2026-10-03/regression-cycle08에 보존한다.

## 중간 결과

첫4경로 실제완주,42/42 검사·오류0. 자연경로에서 전투구역 진입/선택확정/보안문 안내의 번역 누락을 재현했다. 동일 정지화면에33개 문구를 재현해 범위를 확정한 뒤 Chapter45PresentationText.cs 한 파일에 정확한 문구만 추가했다. 수정 전 UI fixture184개는 한국어·레이아웃·큐 동작 검사이며 영어 번역 통과를 뜻하지 않는다. 수정 후 한·영/경고/보상 fixture217/217 통과. 최종코드4경로 재실행 중이다. 각 단계의 게임6키 복원 영수증은 분리 보존한다. native registry 및 엔진 관리 키에 직접 쓰기 없음.

## 완료

최종 코드4경로 완주·42/42, 별도 native UI217/217. 추가 회귀/번역/레이아웃 오류0. 지정 게임6키와 Editor 임시상태 복원, 관측78개/엔진 제외41개 일치. 이번 엔진 키 복원0회, 같은PID83488 clean idleShoeTower. 306기준 중 표시파일1개 외 보존. 60행 중 이번 직접/부분근거 22행과 이전근거 유지 38행을 구분했고 잔여 미술·사람 평가/금지된폰성능 범위를 명시했다. 보고서: `docs/reviews/chapter45-regression-cycle08-2026-10-04.md`. 갤러리: http://127.0.0.1:1626/regression-cycle08-20261004/
