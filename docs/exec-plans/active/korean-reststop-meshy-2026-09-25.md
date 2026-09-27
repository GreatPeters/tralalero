# 휴게소 한국형 재구성 · Meshy · 이후 고속도로

상태: 휴게소·고속도로 적용 완료. 기록: `map-concepts/meshy-reststop-2026-09-25/README.md`, `map-concepts/meshy-highway-2026-09-25/README.md`. Meshy 잔액 2,482. 이후 이미지 생성은 Codex(사용자 정정).

- 기획: `docs/plans/2026-09-25-reststop-korean-entry-meshy-plan.md`
- 예산: Meshy 4,550 중 휴게소 1,708 사용, 잔액 2,842. 재생성은 결과가 나쁜 것만 최대 2회.
- 재현: `tools/install-reststop-korean.cs` Main → Refine1 → Refine2 → Refine3 (각 한 번). 방어전 검사: `tools/reststop-korean-playtest.cs` Setup/Begin, 저장값은 `ChapterPlaytestPreferences.SnapshotAt/RestoreAt`.
- 남은 일: 사람이 직접 한 난이도 확인, 실기기 성능 측정, 핸드드라이어 바람·과속방지턱 점프 기믹(플레이어 API 필요).
