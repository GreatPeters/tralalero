# TRELLIS / Meshy 생성 경로 진단 — 2026-10-02

신규 AI 모델 생성은 **0개**다. 로컬 생성 실행기와 Meshy 클라이언트는 실제로 존재한다. 현재 세션에 직접 호출 가능한 TRELLIS/Meshy 커넥터가 없는 사실과, 기존 로컬 경로의 실행·접속 차단은 구분해야 한다. 설치·새 인증·권한 변경·유료 생성·네트워크 우회를 하지 않았다.

## TRELLIS: 서비스 미접속과 실행 권한 차단

기존 저장 기록에 따르면 작업 소유 백엔드는 2026-10-01 18:04:17 UTC에 PID 13336으로 시작됐고, 같은 날 18:23:12.9844049 UTC에 PID 13336/37920을 정상 종료했다. 18:25:06.5529086 UTC 재확인에서 해당 프로세스와 리스너가 모두 0이었다. 근거는 `outputs/chapters45-2026-10-02/assets/backend-owner.json`, `backend-released.json`이다. 이는 이전 서비스의 종료 기록이며 현재 Windows 권한 상태를 증명하지 않는다.

| 시각 UTC | 대상 / 작업 | 실제 결과 |
| --- | --- | --- |
| 2026-10-02 16:33:44.484697 | `http://127.0.0.1:8189/health` 읽기 | `WinError 10061`, 연결 거부 |
| 2026-10-02 16:44:35.093894 | `http://127.0.0.1:8189/task-uv-raster-status` 읽기 | `URLError`, 내부 `WinError 10061`, 연결 거부 |
| 2026-10-02 16:44:39.157565 | 보조 TCP `connect_ex` | 반환 10035. 이 보조 결과를 10061이나 ACL 증거로 해석하지 않는다 |
| 2026-10-02 16:44:39.161650 | 기존 설치의 Python으로 정상 서비스 시작 | `PermissionError`, `WinError 5`, errno 13. 프로세스 생성 실패, 시작된 백엔드 없음 |

확인한 공식 로컬 실행 순서는 아래와 같다. 여기서 ‘공식’은 이 프로젝트가 기존에 사용하는 승인된 실행 경로를 뜻한다.

1. `C:\AI\TRELLIS2-AMD\venv\Scripts\python.exe`
2. 프로젝트 `tools/run-limited-generation.py --script tools/run-trellis-uv-math.py --record <이번 작업 내부 기록 경로>`
3. 설치된 `C:\AI\TRELLIS2-AMD\ComfyUI\main.py --listen 127.0.0.1 --port 8189 --use-pytorch-cross-attention --disable-pinned-memory`

기존 래퍼는 CPU 스레드 4개/낮은 우선순위/제한된 affinity 및 시작 전 가용 RAM 8GiB 조건을 둔다. 부트스트랩은 `HF_HUB_OFFLINE=1`, `TRANSFORMERS_OFFLINE=1`을 사용한다. 이 진단은 새 설치나 모델 다운로드를 시도하지 않았다. 생성 제출과 이전 작업 기록 변경을 수행하는 `chapters45-assets-trellis.py`는 실행하지 않았다.

최초 `C:\AI\TRELLIS2-AMD` 목록 읽기 역시 접근 거부였으나 그 최초 시도 시각은 별도 원시 기록으로 남지 않았다. WMI 프로세스 상세/네트워크 진단도 일반 권한에서 거부되어 **NTFS ACL, 보안 제품, 관리형 실행 환경 정책 중 어느 계층인지 확정하지 못했다**. 재시작의 프로세스 생성 거부는 정확한 시각과 오류가 기록되어 있다. 확인되지 않은 Python 프로세스를 종료하지 않았다.

필요한 최소 조치: 사용자가 기존 TRELLIS 서비스를 정상 데스크톱 계정에서 실행하면 이 세션은 같은 로컬 포트의 상태를 다시 확인할 수 있다. 자동 실행을 원한다면 해당 설치 실행 경로에 대한 실행 환경 권한 문제가 먼저 해결되어야 한다. 이 문서는 그 권한 변경을 수행하거나 우회하는 지시가 아니다. 재시도에 새 결제·설치가 필수라는 증거는 없다.

## Meshy: 계정 상태 미확인, 생성·크레딧 사용 없음

프로젝트 `tools/meshy_client.py`와 기존 자격증명 검색 경로(`MESHY_API_KEY` 또는 사용자 `.meshy/api_key`)가 있다. 키 값은 로그나 문서에 출력하지 않았다.

| 시각 UTC | 작업 | 실제 결과 |
| --- | --- | --- |
| 2026-10-02 16:44:39.166142 | 현재 Python에서 로컬 클라이언트 import | `ModuleNotFoundError: requests`; 의존성을 설치하지 않음 |
| 2026-10-02 16:45:17.708176 | 동일한 기존 키를 사용하여 표준 라이브러리로 `GET https://api.meshy.ai/openapi/v1/balance` | 키 파일 로드 성공, `URLError` 내부 `WinError 10013` 소켓 접근 차단. HTTP 응답 전에 실패 |

따라서 키가 서버에서 유효한지, 계정이 연결 가능한지, 잔액이 얼마인지는 확인되지 않았다. Meshy 서버의 401/403 응답으로 설명해서는 안 된다. 기존 클라이언트의 의존성 누락과 외부 소켓 차단도 별개다. 새 인증·생성 제출·유료 크레딧 사용은 0회다.

필요한 순서는 승인된 실행 환경에서 `api.meshy.ai:443` 접속 문제 해결 → 기존 키로 읽기 전용 잔액 확인 → 구체적인 생성 수량·모델·해상도에 맞는 크레딧 한도 승인이다. 네트워크 허용이나 잔액 조회 가능 여부만으로 유료 생성 승인이 생기지는 않는다.

[공식 Balance API](https://docs.meshy.ai/en/api/balance)는 잔액 읽기 API다. [공식 API 가격표](https://docs.meshy.ai/en/api/pricing)의 2026-10-02 조회 기준 예시로 Meshy 7.1/6 image-to-3D 2K는 30 credits(메시 20 + 텍스처 10), Meshy 6-lite는 15 credits, remesh는 5 credits다. 이 값은 향후 예산 판단용이며 이번 작업에서 선택·구매·제출한 작업이 아니다. 생성 전에 실제 선택 옵션의 최신 가격을 다시 확인해야 한다.

## 생성 의존 작업과 별도로 진행한 작업

- **현재 권한으로 적용:** Unity 네이티브 매장 깊이/진열 알코브/상층 유리 측벽, 6개 국소 조명, 기존 TRELLIS F10 화분 3개 재사용. 새 Blender 대체 모델이나 새 AI 생성물로 표시하지 않는다.
- **아직 생성 의존:** 참조 분위기에 맞춘 다양한 상품·쇼윈도 중심 소품·형태가 풍부한 환경 모델. TRELLIS.2 우선, 부족한 결과만 승인된 Meshy 범위로 보완하는 원칙을 유지한다.
- **생성 외 남은 품질:** 반복적인 매장 배치, 넓은 빈 바닥, 플레이 시점의 약한 천창 표현. 조명 개선은 일부 매장에 국한된다. 기능 검사 통과가 이 미술 완성을 의미하지 않는다.

원시 기록: `outputs/department-store-2026-10-02/backend-followup/diagnosis.json`, `meshy-balance-readonly.json`. 화면·현재 버전 검증은 [후속 적용 보고서](department-store-backend-followup-2026-10-02.md)에서 별도로 기록한다.
