---
title: Fix reported startup shader assertions and movie timestamps
date: 2026-10-02
status: completed
---

# 시작 로그 오류 수정

사용자 요청: 첨부한 시작 로그의 버그 수정. 정상 정보 로그와 실제 Assert/Warning을 구분했다. 기존 작업 브랜치의 모델·리그·통나무·영상 장면 원복·외곽선 묶음 개선을 보존했다.

## 완료

- 새 머티리얼에서도 외곽선 셰이더가 8개 native Assert를 내는 것을 재현. 일반 셰이더는 0개. 신규 회귀 테스트는 수정 전 1개 실패/1개 통과.
- 공유 `UsePass`를 같은 본문의 로컬 패스로 교체. 프로퍼티·외곽선 계산·RenderObjects 설정 유지. 수정 후 신규 2개 통과, 오류 0, SRP Batcher 코드 0.
- 원래 선택된 오프닝을 Baseline H.264로 정규화. 936프레임/39초/24fps/720×1280, 동일 GUID와 원래 네 번째 장면 유지. PSNR 평균 47.638dB. 원본 백업과 인코딩 가드 보존.
- 실제 시작 3회(Revamp 2회, HighWay 1회): Warning/Error/Exception/Assert 모두 0. 영상 장면 이동 0/192/480/720프레임, 이전·skip·재열기 검증. Firebase/자료 로딩 정보 로그는 유지.
- 집중 검사 19개 통과, runtime/editor dotnet build와 harness 통과, 패스 원본 동기화 검사와 diff 공백 검사 통과.
- 당일 66개 저장 항목 전후 완전 일치, 코인40228/보석30 및 원래 세션 옵션/clean Revamp/Edit Mode 복원.

원인·예방: [ce-compound 기록](../../solutions/runtime-errors/own-flatkit-passes-to-avoid-keyword-space-assertions-2026-10-02.md). 원본/수정/검증 자료: `outputs/startup-errors-2026-10-02/`. 테스트 중 멈춘 스크립트 재컴파일은 스냅샷 후 clean Editor 재시작으로 복구했다. Library 삭제나 전역 로그 억제는 하지 않았다.

이번 전달 범위는 Unity 프로젝트다. APK 재빌드/기기 설치나 실기기 성능 측정은 포함하지 않는다. 커밋·푸시는 수행하지 않았다.
