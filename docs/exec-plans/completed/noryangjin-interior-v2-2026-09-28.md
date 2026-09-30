# 노량진 실내·상인 품질 재작업

- 상태: 적용 및 검증 완료, SR18_Revamp 안전본.
- 사용자 지시: 레퍼런스와 다른 촌스러운 실내를 교체. 재활용보다 품질을 우선하고 부적절한 적·동작·소품·기믹은 Meshy/TRELLIS로 새로 제작·적용.
- 원본 현장 사진: `outputs/noryangjin-revamp-2026-09-27/concepts/refs/user-indoor-market.png`.

## 적용

- Meshy N09–N14 신규6종: 운전자가 탄 화물 삼륜차, 게·활어 수조, 생선 진열대, 스티로폼 상자, 남녀 상인.214크레딧 사용, TRELLIS 전환 불필요.
- 새 수조/진열대150개 안팎의 연속 배치, 흰 타일 벽·천장, 돔 조명·배관·형광등, 번호 간판·TV·디지털 시계, 젖은 바닥과 반사 프로브.
- 구형 도로 메시의 갈색 기둥은 실내에서 렌더링만 숨기고 충돌·다른 구간 표시를 보존.
- 터렛트 운전자 삽 제거, 정면 운전 자세, 상자18개 탈락, 소형 체력바와 합산 피해 표시.
- 남녀 상인7상태 연결. Unity 평가 메시 기준2.3m 정규화·바닥 침투 보정,2.5초 사망 클립 표시.
- 가까운 TV의 반투명 화면 덮임, 단계 조명으로 과노출되는 상인 재질, 돔 법선과 체력 Canvas 좌표 수정.

## 증거와 재현

자세한 명령, 모델 원장, 실플레이·시각 fixture 구분과 한계는 [적용 보고서](../../../outputs/noryangjin-interior-v2-2026-09-28/README.md)에 기록한다. 적용 갤러리는 `http://127.0.0.1:8786/interior-v2/`.

- Builder: `Assets/ShooterSurvival/Editor/NoryangjinInteriorV2Builder.cs`, main installer: `tools/install-noryangjin-revamp.cs`.
- 신규2개와 기존28개를 합친30개 회귀 검사, runtime/editor 빌드, harness 검증.
- 신규 모델 적용 후 양 분기 약302초 완주(ATT37/HP46/공속30,9999·HP고정 없음). 마지막 시각 클립 보정 후70포즈/네이티브 사망 fixture와 최종 화면 재검사. 마지막 보정 후 전체 코스를 반복한 것으로 혼동하지 않는다.
- 원본 SR18·Build Settings·Data.xlsx 해시 및 저장값 복원 확인은 보고서의 최종 영수증을 따른다.
- 알려진 한계: 에디터 재설치 직후 URP 작업 예외, 모바일 실기기 FPS 미측정. 운영 메모는 RELIABILITY.md.
