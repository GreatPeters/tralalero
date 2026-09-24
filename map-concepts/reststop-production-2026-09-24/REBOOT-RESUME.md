# 재부팅 전 저장 및 재개

2026-09-25 03:16 KST에 생성 프로세스 종료를 확인했다. **90종 중 선택 모델 69종**, 인물 **8종의 리그와 72개 동작**을 저장했다. 전체 제작은 아직 완료 전이다.

## 저장 지점

- **S10 첫 형태 생성 완료 → 시각 검토 대기**. 형태 1회 사용, 재질 0회. 형태 GLB와 다각도 이미지 5장, 성공한 요청 이력을 저장했다. 재질이나 다음 소품 생성은 시작하지 않았다.
- 체크포인트: `outputs/reststop-production-2026-09-24/checkpoints/reboot-20260925-031512/`.
- `manifest.json`: 상태·설정·원장·S10 결과 등 185개 저장 파일의 SHA-256. `selected-results.json`: 선택본 69종 위치, 파일 크기, GLB 해시.
- 원래 결과는 `outputs/reststop-production-2026-09-24/`에 그대로 있다. `final-overrides.json`, `accepted-rigs.json`, 모든 실패/재시도 원장을 보존했다.
- `shutdown-verification.json`: 생성 프로세스 없음, 전용 8189 포트 없음. PID 33332/55168과 44980/43652를 종료했다. 다른 작업의 Python 프로세스는 종료하지 않았다.

기존 실행기에는 외부에서 정상 종료를 요청할 통로가 없었다. 이미 저장된 S10 검토 게이트와 빈 생성 큐를 확인하고 체크포인트를 만든 뒤 **유휴 프로세스를 OS 명령으로 종료**했다. 실행 중인 생성 요청을 취소한 것은 아니며, 애플리케이션의 정상 종료 API를 사용했다고 주장하지 않는다. 결과 조회용 HTTP 갤러리 서버는 남겨 두었다.

## 재개

Codex에 **“휴게소 TRELLIS 작업을 REBOOT-RESUME.md 기준으로 이어서 진행해줘”**라고 요청하면 된다. 자동 실행 예약이나 재부팅 후 자동 시작은 설정하지 않았다.

직접 실행할 경우 프로젝트 폴더에서:

```powershell
Set-Location -LiteralPath 'C:\Users\ljh\tralalero Shooter'
powershell -ExecutionPolicy Bypass -File tools/resume-reststop-production.ps1 -CheckOnly
powershell -ExecutionPolicy Bypass -File tools/resume-reststop-production.ps1
```

첫 명령은 확인만 한다. 두 번째 명령은 새 로그 이름으로 자원 제한 실행기를 시작하며 `--defer F03,F04,R11 --resume`을 전달한다. 기존 설정과 원장을 유지한다. S10의 이미 저장된 첫 형태를 다시 사용하고 **실제 이미지 검토 판정이 나올 때까지 기다린다**. 판정 파일을 임의로 통과시키거나 원장을 삭제하지 않는다. F03/F04/R11은 선택된 수동 복구본이 있으나 원래 실패 이력이 남아 있어 `--defer`를 유지한다.

재부팅 후 갤러리가 열리지 않으면 별도 PowerShell에서 다음 명령을 실행하고 [결과 갤러리](http://127.0.0.1:8772/)를 연다. 이 명령은 결과 파일만 제공하며 생성하지 않는다.

```powershell
Set-Location -LiteralPath 'C:\Users\ljh\tralalero Shooter'
python -m http.server 8772 --bind 127.0.0.1 --directory tmp/image-previews/reststop-production-2026-09-24
```

## 다음 실행의 중지 장치

작업 실행기에 `pause-request.json` 확인을 추가했다. 이 파일이 있으면 명시적인 `--resume` 없이 시작할 수 없다. 다음 실행부터는 파일을 생성하면 현재 생성/렌더 단계가 끝난 후 검토 경계에서 원장을 저장하고 중지한다. 검토 대기 중에도 확인한다. 이 코드 변경은 이미 실행 중이던 과거 프로세스에 소급 적용되지 않았으며, 이번 종료는 위의 유휴 프로세스 종료 방식이었다. 해당 장치는 로컬 단위 검사로 확인했고 새 생성으로 시험하지 않았다.
