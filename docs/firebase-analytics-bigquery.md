# Firebase Analytics와 BigQuery 플레이 로그

이 문서는 Unity 클라이언트가 Firebase Analytics 이벤트를 전송하고, Firebase의
Google Analytics 원시 이벤트 내보내기를 통해 BigQuery에서 리텐션과 한 판
플레이 로그를 분석하는 흐름을 설명한다.

현재 프로젝트 기준 SDK 버전은 Firebase Unity SDK `13.14.0`, Android 앱 ID는
`com.mzkoreagames.tralaleroshooter`다. Firebase 프로젝트 생성, 앱 등록, 구성 파일
발급 및 Analytics-BigQuery 콘솔 연결은 저장소 밖에서 수행해야 하는 외부
선행 작업이며, 이 문서는 해당 연결이 완료되었다고 간주하거나 주장하지 않는다.

## 데이터 흐름

1. Firebase가 `first_open`, `session_start`, `user_engagement`와
   `engagement_time_msec`를 자동 수집한다.
2. Unity에서 `game_round_start`와 `game_round_end` 이벤트를 Firebase Analytics로
   전송한다.
3. Firebase 프로젝트의 Google Analytics 데이터를 BigQuery에 연결한다.
4. GA4 원시 이벤트가 `events_YYYYMMDD` 또는
   `events_intraday_YYYYMMDD` 테이블로 내보내진다.
5. `tools/analytics/bigquery/`의 Standard SQL을 프로젝트와 Analytics 데이터셋에
   맞게 바꿔 실행한다.

BigQuery는 플레이어 앱에서 직접 호출하지 않는다. 서비스 계정 키나 BigQuery
자격 증명을 Unity 클라이언트, `Assets/`, APK 또는 AAB에 포함하면 안 된다.

## 저장소에 고정된 SDK

공식 Firebase Unity SDK `13.14.0`의 UPM 패키지를 `GooglePackages/`에 고정했다.
Git은 `*.tgz`와 `*.aar`를 LFS 대상으로 취급한다.

| 패키지 | SHA-256 |
| --- | --- |
| `com.google.external-dependency-manager-1.2.186.tgz` | `46684B475C2A39844C44C07945B5AEE02895C41A9BFF97D5CD4B5D9E85E021D8` |
| `com.google.firebase.app-13.14.0.tgz` | `BB54CC7AAB6DEC3430BC2F628E9A500D44A7E5BB05727D0372D30D6B68438FCB` |
| `com.google.firebase.analytics-13.14.0.tgz` | `ABB780995D77A98ACD3362201E3B849651717EADF726DD22D277CE98638C3A3B` |

`Packages/manifest.json`은 이 로컬 아카이브를 참조한다. Android 빌드 의존성은
External Dependency Manager가 해석한다. 해석 결과인
`Assets/GeneratedLocalRepo/Firebase`, `mainTemplate.gradle`,
`settingsTemplate.gradle`, `gradleTemplate.properties`와
`ProjectSettings/AndroidResolverDependencies.xml`을 저장소에 유지한다.
Firebase Unity AAR/POM 산출물은 저장소에 유지하며 AAR은 Git LFS 대상으로
취급한다.

## Firebase 외부 설정

공식 [Firebase Unity 설정 가이드](https://firebase.google.com/docs/unity/setup)에
따라 다음 항목을 별도로 완료해야 한다.

1. Firebase Console에서 Android 앱 ID
   `com.mzkoreagames.tralaleroshooter`를 등록한다.
2. 해당 앱에서 발급한 `google-services.json`을
   `Assets/google-services.json`에 둔다.
3. 파일의 Firebase 프로젝트 ID와 모바일 SDK 앱 ID를 검토한 뒤
   `Tools/Analytics/Firebase 대상 고정`을 실행한다. 이 값은
   `ProjectSettings/FirebaseAnalyticsDestination.json`에 기록되며, 이후 다른
   프로젝트의 구성 파일로 바뀌면 Android 빌드를 막는다.
4. Firebase Console에서 Google Analytics 사용 여부와 데이터 수집 정책을
   확인한다.
5. 실제 기기 빌드에서 DebugView 또는 Firebase 로그를 사용해 이벤트 수신을
   확인한다.

Unity 메뉴 `Tools/Analytics/Firebase 설정 검증`은 로컬 SDK 아카이브의 SHA-256,
구성 파일 구조, Android 패키지 ID, 고정된 Firebase 프로젝트/앱 대상, Android
수집·광고 기본값, Unity 6용 Gradle 설정 템플릿과 해석된 Firebase AAR/POM을
확인한다. 생성된 AAR/POM도 고정 SHA-256으로 검사하므로, Git LFS 파일을 받지
못한 checkout이나 일부만 해석된 Maven 저장소는 빌드 전에 실패한다. Android
빌드 전처리기도 같은 검증을 수행하므로, 검토하지 않은 Firebase 프로젝트나
누락된 구성·의존성으로 AAB를 만들지 않는다.
`Tools/Analytics/Firebase 연결 문서 열기`는 이 문서를 연다.

Firebase Analytics의 데스크톱 구현은 비기능성 stub이므로 Editor Play Mode에서
전송 성공 여부를 검증할 수 없다. Editor에서는 이벤트를 Firebase로 넘기지
않으며, DebugView와 실제 전송 검증은 Android 기기 빌드에서 수행한다.

`google-services.json`은 Firebase 클라이언트 구성 파일이지 관리자용 비밀 키가
아니다. 그래도 올바른 Firebase 프로젝트에서 내려받은 파일이어야 하며, 저장소에
서비스 계정 JSON이나 개인 키를 함께 두면 안 된다.

현재 iOS Bundle ID는 템플릿 값이고 `GoogleService-Info.plist`도 없다. Firebase
iOS 의존성은 최소 iOS 15를 요구하므로 실제 iOS Bundle ID와 배포 타깃을 정한 뒤
별도로 앱 등록과 설정을 해야 한다. 이번 연결의 출시 준비 대상은 현재 Android
앱 ID다.

## BigQuery 연결

[Firebase의 BigQuery 내보내기 가이드](https://firebase.google.com/docs/projects/bigquery-export)에
따라 Firebase Console의 **Project settings > Integrations > BigQuery**에서
Google Analytics 제품을 연결한다.

연결 시 Google Cloud 프로젝트, 데이터셋 위치, 일일 내보내기 및 필요하다면
스트리밍 내보내기 설정을 확인한다. 이 작업에는 Firebase/Google Cloud 권한과
프로젝트의 결제 설정이 필요할 수 있다. 저장소의 코드나 SQL만으로 콘솔 연결을
완료할 수 없으므로, 출시 전 반드시 Console에서 연결 상태와 첫 테이블 생성을
직접 확인한다.

GA4 내보내기 테이블의 상세 스키마는
[Google Analytics BigQuery Export 스키마](https://support.google.com/analytics/answer/7029846)를
참조한다.

- `events_YYYYMMDD`는 날짜별 확정 테이블이다. 지연 도착 이벤트로 인해 최근
  날짜의 테이블이 다시 갱신될 수 있다.
- 스트리밍 내보내기를 켜면 당일 데이터가
  `events_intraday_YYYYMMDD`에 먼저 들어올 수 있다.
- 당일 확정 테이블이 만들어지면 해당 intraday 테이블은 교체 또는 제거될 수
  있다. 운영 집계는 확정 일일 테이블을 기준으로 재실행할 수 있어야 한다.
- 제공된 SQL은 중복 집계를 피하기 위해 기본적으로 숫자 날짜 접미사의 확정
  `events_YYYYMMDD` 테이블만 읽는다.

## 커스텀 이벤트 계약

이벤트 이름과 파라미터 이름은 배포 후 BigQuery 쿼리의 계약이 되므로 임의로
바꾸지 않는다. Firebase 이벤트 작성 규칙은
[Unity Analytics 이벤트 가이드](https://firebase.google.com/docs/analytics/unity/events)를
참조한다.

### `game_round_start`

| 파라미터 | 의미 |
| --- | --- |
| `round_id` | 한 판 시작부터 종료까지 유지되는 비식별 고유 ID |
| `scene_name` | 플레이한 Unity 씬 |
| `game_mode` | 게임 모드 |
| `chapter` | 현재 챕터 |
| `stage` | 현재 스테이지 |
| `max_stage` | 해당 챕터의 마지막 스테이지 |
| `client_event_time_ms` | 오프라인 큐 적재 전에 기록한 UTC Unix 밀리초 |

### `game_round_end`

종료 이벤트에도 위 공통 파라미터를 포함하며 다음 파라미터를 추가한다.

| 파라미터 | 의미 |
| --- | --- |
| `outcome` | 완료, 사망, 중단 등 종료 결과 |
| `chapter_progress_pct` | 종료 시점 챕터 진행률 |
| `coins_earned` | 해당 판에서 획득한 코인 |
| `play_time_ms` | 일시정지를 제외한 해당 판 활성 플레이 시간 |
| `end_pos_x`, `end_pos_y`, `end_pos_z` | 사망 또는 종료 시 게임 월드 좌표 |
| `upgrade_levels` | 업그레이드 레벨 스냅샷 문자열 |
| `upgrade_flat` | 고정 수치 업그레이드 스냅샷 문자열 |
| `upgrade_pct` | 비율 업그레이드 스냅샷 문자열 |

현재 종료 결과는 `win`, `death`, `abandoned`다. `abandoned`는 모드 변경,
재시작, 명시적 종료, 또는 활성 판의 체크포인트가 남은 채 앱이 끝났을 때의
다음 실행 복구에서 남긴다.

일반 씬의 `chapter_progress_pct`는
`clamp(stage / max_stage * 100, 0, 100)`이다. 로드된 Noryangjin 씬에서는
이미 소비됐거나 현재 활성화된 `NoryangjinTurnSpot`을 경로 체크포인트로 사용해
`stage = consumed + 1`, `max_stage = total + 1`,
`chapter_progress_pct = consumed / total * 100`으로 기록한다. 따라서 경로
시작은 0%, 모든 체크포인트 통과 후는 100%다. 이 값은 턴 경계 기반 진행률이지
턴 사이의 연속 거리나 결승점까지 남은 거리를 뜻하지 않는다.

업그레이드 스냅샷은 키 정렬이 일정한 간결한 형식으로 직렬화해야 한다. 이벤트
파라미터 크기 제한을 넘지 않게 유지하고, 값이 커지면 상세 업그레이드 로그를
별도 서버 파이프라인으로 옮긴다.

현재 9개 업그레이드는 다음 짧은 키를 순서대로 사용한다:
`att`, `hp`, `as`, `ps`, `bd`, `cb`, `hr`, `tt`, `bb`. 각 스냅샷 문자열은
Firebase 기본 문자열 파라미터 한도에 맞춰 최대 100자로 제한한다.

`round_id`는 한 판 내에서만 같은 값을 사용한다. SQL은
`user_pseudo_id + round_id`로 시작과 종료를 결합하고, 재전송된 이벤트는 가장
이른 시작과 가장 늦은 종료를 사용한다. 오프라인 재전송으로 Firebase 수신 시각이
늦어질 수 있으므로 SQL은 `client_event_time_ms`를 실제 발생 시각으로 우선
사용하고, 원본 `event_timestamp`도 수신 시각으로 함께 보존한다.

`GameManager`가 없는 씬은 `GameplayAnalyticsSceneContext`의 Inspector 값에서
챕터와 게임 모드를 읽는다. 명시적 컨텍스트의 stage/max-stage는 기본적으로
보존하며 `Use Turn Spots For Progress`를 켰을 때만 경로 체크포인트가
stage/max-stage/progress를 덮어쓴다. 컨텍스트가 없는 Noryangjin 씬은 이를
자동으로 사용하고, 사용할 수 있는 턴 스팟이 없을 때만 기존 `1 / 1 / 10`
호환 기본값을 사용한다.

## 런타임 내구성

- Android manifest는 Firebase 초기화 전 수집을 꺼 두고 Advertising ID 권한과
  수집, 광고 개인화 신호를 비활성화한다. 런타임은 저장된 수집 선택을 Firebase
  초기화 후 적용한다.
- 현재 제품 기본 선택은 첫 설치에서 수집 허용이다. 개인정보 화면에서
  `FirebaseAnalyticsRuntime.SetCollectionEnabled(false)`를 호출하면 그 선택이
  재실행 후에도 유지되고, 아직 보내지 않은 큐와 활성 판 체크포인트를 삭제한다.
  명시적 opt-in이 필요한 배포 지역에서는 출시 전에 같은 API를 동의 UI에
  연결하고 첫 설치 기본 정책도 해당 법적 근거에 맞게 바꾼다.
- `FirebaseApp.CheckAndFixDependenciesAsync`가 성공한 뒤에만 네이티브 Analytics를
  호출한다.
- 초기화 전이거나 일시적으로 전송할 수 없는 이벤트는 PlayerPrefs의 128개
  제한 로컬 큐에 저장하고 다음 초기화에서 순서대로 넘긴다.
- 활성 판은 시작 시, 15초마다, 앱 일시정지 및 종료 시 체크포인트한다.
- 정상적인 승리와 사망은 종료 이벤트를 한 번만 만든다. 강제 종료로 체크포인트가
  남으면 다음 실행에서 `abandoned` 종료 이벤트를 한 번 만든다.
- `play_time_ms`는 `TimeManager.isGameRunning`인 동안의 unscaled time만 합산하므로
  일시정지와 게임오버 대기 시간은 제외한다.
- Firebase Unity Analytics에는 강제 전송 완료를 보장하는 `Flush()` API가 없다.
  로컬 큐에서 Firebase SDK로 넘긴 뒤 실제 업로드는 SDK의 오프라인 전송 정책에
  맡긴다.

## 쿼리 실행

두 파일의 `YOUR_PROJECT.analytics_PROPERTY_ID`를 실제
`Google Cloud 프로젝트 ID.Analytics 데이터셋 ID`로 교체한다.

- `tools/analytics/bigquery/round_logs.sql`: 시작/종료 이벤트를 한 행으로 합치고
  진행률, 코인, 사망 위치, 활성 플레이 시간 및 업그레이드 상태를 보여준다.
  시작/종료 각각의 챕터 값과 종료 기준 대표 값을 구분하고, 보고 기간 경계의
  미관찰 이벤트와 실제 누락 후보를 별도 `join_status`로 분류한다.
- `tools/analytics/bigquery/retention_and_playtime.sql`: `first_open` 기준
  D1/D7/D30 정확 일자 리텐션, Firebase 자동 앱 참여 시간, 날짜·챕터별 한 판
  활성 플레이 시간 집계를 각각의 결과 집합으로 출력한다.

두 SQL 모두 필수 파라미터의 개수와 실제 GA4 값 타입을 검사한다. 중복, 누락,
잘못된 타입 또는 허용 범위를 벗어난 클라이언트 값은 정상 집계에서 제외하고
별도 품질 결과 집합으로 출력한다. 합계는 `BIGNUMERIC`으로 계산해 조작된 큰
`INT64` 값 하나가 집계 전체를 overflow시키지 않게 한다.

현재 SQL 계약은 `client_event_time_ms`가 포함된 새 이벤트 스키마를 기준으로
한다. 이 파라미터가 없던 시험·구버전 이벤트는 수신 시각으로 조용히 대체하지
않고 격리 결과에 남긴다. 실제 서비스에 구버전 이벤트가 이미 존재한다면
대시보드 전환일을 정하거나, 구버전 전용 변환 뷰를 별도로 만들어야 한다.

코인 10억, 한 판 활성 시간 24시간, 월드 좌표 절댓값 100만 등 SQL의 허용
범위는 손상·조작 데이터가 집계를 망가뜨리지 않게 둔 초기 운영 한계다. 게임
경제나 맵 크기가 이 범위에 가까워지면 출시 데이터 계약과 함께 명시적으로
조정하고 품질 결과의 격리 비율을 확인한다.

기본 날짜 범위는 비용과 아직 완성되지 않은 코호트의 왜곡을 줄이기 위한
예시다. 실제 대시보드에서는 쿼리 상단의 날짜를 조정하고, D30은 최소 30일의
관찰 기간이 끝난 코호트만 포함한다.

리텐션의 사용자는 Firebase/GA4의 가명 `user_pseudo_id` 기준이다. 앱 재설치,
기기 변경 또는 데이터 초기화 시 같은 사람이 새 사용자로 보일 수 있다. 계정
기반 결합이 필요하다면 개인정보를 직접 보내지 말고, 별도의 개인정보·동의
검토를 거친 내부 비식별 ID 전략을 사용한다.

## 보안과 데이터 신뢰 경계

- 이메일, 전화번호, 실명처럼 개인을 직접 식별하는 값을 이벤트 이름이나
  파라미터로 보내지 않는다.
- GA4 원시 내보내기에는 `user_pseudo_id` 외에도 자동 수집된 기기/OS/앱 정보,
  대략적 지역, 유입 경로 및 동의 관련 필드가 포함될 수 있다. 따라서 데이터셋을
  “완전 익명”으로 분류하지 않고 접근권한과 보관기간을 정한다.
- `end_pos_*`는 현실 위치가 아니라 게임 월드 좌표만 기록한다.
- 국가별 개인정보 고지, 동의, 삭제 및 보관 정책을 출시 전에 검토한다.
- 클라이언트에서 보낸 코인, 진행률, 업그레이드 및 플레이 시간은 패치된 앱이나
  자동화 도구가 위조할 수 있다.
- 따라서 이 데이터는 제품 분석과 이상 징후 탐색에는 사용할 수 있지만, 보상
  지급, 경제 정산, 밴 또는 치팅 판정의 단독 근거로 사용하면 안 된다.
- 신뢰가 필요한 값은 서버가 판 상태를 검증하고 서버 로그를 BigQuery로 보내는
  별도 권위 경로가 필요하다.

## 출시 전 확인

- Firebase SDK `13.14.0` 의존성이 실제 Unity 프로젝트에 해석되는지 확인한다.
- 올바른 `Assets/google-services.json`이 빌드에 포함되는지 확인한다.
- Android 패키지 ID가 `com.mzkoreagames.tralaleroshooter`인지 확인한다.
- 검토한 프로젝트/앱을 `Tools/Analytics/Firebase 대상 고정`으로 기록한다.
- `Tools/Analytics/Firebase 설정 검증`이 성공하는지 확인한다.
- 개인정보 고지와 수집 허용/철회 UI가 런타임 API에 연결되는지 확인한다.
- 실제 기기에서 시작/종료 이벤트와 모든 파라미터를 확인한다.
- Firebase Console에서 Analytics-BigQuery 연결 상태를 확인한다.
- 확정 `events_YYYYMMDD` 테이블이 생성된 뒤 두 SQL을 실행한다.
- 업로드 실패, 앱 강제 종료 및 오프라인 종료로 인한 누락 후보, 경계 미관찰,
  파라미터 격리 비율을 모니터링한다.
