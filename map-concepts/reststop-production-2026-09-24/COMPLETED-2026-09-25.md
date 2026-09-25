# 휴게소 TRELLIS 제작 완료 — 2026-09-25

**전체 90종 완료. 남은 21종 제작도 끝났다. 새 생성이나 이전 S10 체크포인트 재개는 필요하지 않다.**

- 완성 모델 갤러리: [http://127.0.0.1:8772/](http://127.0.0.1:8772/)
- 원본 입력 PNG 90종: [같은 서버의 입력 이미지](http://127.0.0.1:8772/inputs/)
- 전체 ZIP: [reststop-assets-r1.zip](http://127.0.0.1:8772/reststop-assets-r1.zip), 6,319,598,606 bytes, 약 6.3GB.
- 최종 폴더: `C:/Users/ljh/tralalero Shooter/outputs/reststop-production-2026-09-24/delivery-r1/`
- ZIP 원본: `C:/Users/ljh/tralalero Shooter/outputs/reststop-production-2026-09-24/reststop-assets-r1.zip`
- 상세 안내: `C:/Users/ljh/tralalero Shooter/map-concepts/reststop-production-2026-09-24/README.md`

소품·구조·차량 82종과 인물 8종이다. 인물은 18본/9동작씩, 총 72클립을 포함한다. 정적 최종 모델은 각각 15,000삼각형 이하이며 실제 다각도 렌더와 새 FBX/GLB 검증을 통과했다. 일부 물체의 재질/노멀/형상을 직접 보정했고 사소한 잔여 차이는 각 `visual-review.json`에 기록했다. `MATERIALS.md`가 있는 모델은 해당 재질 구성과 투명도 연결을 유지한다.

## 검증과 상태

- 최종 파일 1,444개. ZIP CRC 검사 통과, 패키지 JSON 377개 파싱 성공.
- 갤러리 자원 402개와 원본 입력 PNG 90개 HTTP 검사 통과.
- 전체 ZIP을 HTTP로 끝까지 읽어 로컬 파일과 SHA-256 일치를 확인했다.
- SHA-256: `2c4a81ce05a5d8c238a71c8cd56ead0515e8de005ec8a597e59a3f746cc7743b`
- 영수증: 출력 폴더의 `delivery-r1-receipt.json`, `delivery-r1-verification.json`, `reference-link-verification.json`, `completion-runtime.json`.
- 연결된 브라우저가 없어 UI 조작/스크린샷 검증은 수행하지 않았다. 실제 모델 렌더와 HTTP/전체 다운로드를 검증했다.
- 생성 실행기와 작업 전용 TRELLIS 백엔드 8189는 종료했다. 모델 갤러리 8772는 유지한다.

원래 자동 후보의 `review_needed`나 `halted`는 실패 기록을 보존한 상태다. **최종 선택은 `assets/.trellis-automation/state.json`과 `final-overrides.json`을 합친 결과**, 리그 선택은 `accepted-rigs.json`이다. 실패한 후보를 보고 완료 모델을 다시 생성하지 않는다.

이번 제작물은 기존 고속도로·휴게소 Unity 씬이나 기존 Blender v4에 적용하지 않았다. 사용자 Git 업로드를 조작하지 않았다.

## PC 재부팅 뒤 홈페이지만 닫혔을 때

모델과 ZIP은 디스크에 저장되어 있다. 8772 리스너가 없는 것을 확인한 다음 아래 **읽기 전용 HTTP 서버만** 다시 시작한다. 모델 생성 실행기를 재개할 필요는 없다.

```powershell
Start-Process -FilePath 'C:/AI/TRELLIS2-AMD/venv/Scripts/python.exe' -ArgumentList @('-m','http.server','8772','--bind','127.0.0.1','--directory','"C:/Users/ljh/tralalero Shooter/tmp/image-previews/reststop-production-2026-09-24"') -WindowStyle Hidden
```

예전 `REBOOT-RESUME.md`와 `tools/resume-reststop-production.ps1`은 S10에서 정상 중지했던 과거 체크포인트용이다. 현재 완료 상태는 이 문서와 최신 README/선택 기록을 따른다.
