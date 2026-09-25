# 휴게소 TRELLIS 소품·인물 제작

상태: **2026-09-25 완료** — 선택 모델 90/90종, 인물 8종/72동작. 남은 21종을 포함해 실제 시각 검토·새 FBX/GLB 검증과 최종 ZIP CRC/다운로드 검증을 마쳤다. 생성 실행기와 작업 전용 8189 백엔드는 종료했고 갤러리 8772는 유지한다. 최종 폴더는 `outputs/reststop-production-2026-09-24/delivery-r1`, ZIP은 `reststop-assets-r1.zip`이다. [완료 안내](../../../map-concepts/reststop-production-2026-09-24/COMPLETED-2026-09-25.md). 사용자 Git 업로드와 기존 Unity 씬은 변경하지 않았다.

## 범위와 실행 상태

- [제작 계약](../../../map-concepts/reststop-production-2026-09-24/contract.md): 소품·구조·차량 82종과 인물 8종, 총 90종.
- [작업 안내와 재개 방법](../../../map-concepts/reststop-production-2026-09-24/README.md).
- [실행 기록](../../../map-concepts/reststop-production-2026-09-24/execution-log.md): 단계별 승인·거절과 보정 이력.
- [진행 수치](../../../map-concepts/reststop-production-2026-09-24/progress.json) 및 [제작 갤러리](http://127.0.0.1:8772/): 현재 결과.
- 실제 선택본은 출력 폴더의 자동화 상태와 `final-overrides.json`, 인물은 `accepted-rigs.json`으로 결정한다. 거절된 자동 후보와 재시도 원장은 보존한다.
- F03·F04 자동 감량 중단, R11 완료 상태 조회 오류와 V04 재부팅 중단 기록은 보존했다. 마지막 실행 인자는 `--only V05,V06 --defer F03,F04,R11,V04 --resume`이었다. 실제 종료는 `completion-runtime.json`, 마지막 로그는 `runner-post-reboot-*.log`에서 확인한다. 저장된 생성 설정과 설치된 자동화 엔진은 유지했다. 추가 재개는 필요하지 않다.

## 별도 복구 현황

- R11 복구 완료: `outputs/reststop-production-2026-09-24/manual/R11-r1/texture1/low1`의14,285삼각형 모델을 선택했다. 원래 중단 기록이 남아 있으므로 재개 인자의 `R11`은 유지한다.
- R12 복구 완료: `outputs/reststop-production-2026-09-24/manual/R12-r3/texture1/low1`의14,726삼각형 모델을 선택했다. 새 GLB·FBX의 각3개 수직 단면에서35개 개구부를 확인했다.
- S02 복구 완료: `outputs/reststop-production-2026-09-24/manual/S02-r11`의11,971삼각형 모델을 선택했다. TRELLIS 본체와 별도로 원래 고밀도 소스에 맞춘 곡면/측면 유리·선반2단을 구성했다. 새 GLB·FBX 기술 검사와 각 형식9개 투과/6개 선반 검사, GLB5방향 및 FBX 렌더를 통과했다. 첫 취소 재질 시도와 실패한 canonical 감량2회는 소비 기록 그대로 보존한다. 자세한 보정 범위와3재질 유지 조건은 선택 폴더 README에 있다.
- S08 보정 완료: `manual/S08-r5/source/low1`의14,772삼각형 모델을 선택했다. 원래 형태1회와 수동 본체 재질1회, 부품 보존 감량1회를 사용했다. 본체12,108삼각형과 고정 내부 부품2,664삼각형·4재질이며 새 GLB·FBX/다각도/18개 투과광선·5단·200개 타공 검사를 통과했다. 원래 표준/캐시 후보의 선반 부재, 수치상 붕괴한3면과 면적0인2면 정리 기록은 보존했다.
- 새 수동 GPU 작업은 일반 배치의 실제 미응답 검토 게이트를 유지하고 큐가 비어 있을 때만 순서대로 실행한다. 기본은 `final_compare`이며, S08에서는 `--held-stage texture_source`로 S09의 재질 검토를 명시적으로 유지했다. 요청 기록에 해당 단계와 판정 경로를 남기며 수동 GPU 완료 전에는 게이트를 해제하지 않는다. 명령은 자원 제한 래퍼로 실행하며 시도 원장을 초기화하거나 중단된 제출을 무조건 다시 보내지 않는다.

## 완료 조건

- [x] 90종의 선택 모델이 실제 다각도 렌더와 새 FBX·GLB 임포트 검증을 통과했다.
- [x] 인물 8종은 본·동작, 새 임포트 포즈 비교와 실제 미리보기 영상 검증을 통과했다.
- [x] `tools/package-reststop-production.py --check-only`가 90종/8리그 완료를 확인했다. ZIP CRC와 전체 다운로드 SHA-256이 일치한다.
- [x] 갤러리와 안내 문서를 전체 완료 상태로 갱신했다. 브라우저 연결 부재로 UI 조작 대신 HTTP 자원/다운로드를 검증했다.

기존 Unity 씬과 Blender v4는 이 독립 에셋 제작 과정에서 변경하지 않는다. 차량 조립 크기 기준은 별도 JSON으로 함께 제공한다.
