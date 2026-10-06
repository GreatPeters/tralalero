---
title: Apply combined S22 performance and visual review
date: 2026-10-01
status: completed
---

# S22 성능·시각 품질 개선 적용

사용자 승인: Claude artifact `https://claude.ai/artifact/Dft5AkhivSWuUANkX4JeEN`와 `docs/reviews/s22-performance-visual-audit-2026-10-01.md`를 함께 참고해 실제 개선 적용. 필요 시 Meshy 사용 허용. 분석으로 끝내지 않는다.

## 기준과 해석

- 기존 사용자 직접 요구(두 발+꼬리 끝의 신발, 게임플레이 카메라, 9999/빠른 좌우 테스트, 역동적인 파도, 파랑/크림 UI)를 우선한다.
- 링크의 잘못된 즉시 스키닝 미리보기 기반 동작 결론은 실제 Animator로 재검증한다. 정상 리그를 추측으로 교체하지 않는다.
- S22 칩셋/발열, 영상의 정확한 패킹 비중, 그림자/SSAO 패스 비용은 현재 기기/설정에서 확인된 범위만 주장한다.
- 숨겨진 객체는 비활성이라는 이유만으로 삭제하지 않는다. 런타임 재활성화·참조와 구형 장식의 역할을 구분한다.
- 폰이 미연결이어도 프로젝트 적용·에디터 검증·설치 가능한 빌드는 진행한다. 실기기 FPS는 미측정 상태로 표시한다.

## 구현 단위

### U1. CPU 병목

카메라 가림 후보의 캐시/근처 선별, 원복 상태 보존, 불필요한 순회/할당 제거. 차량 체력 표시 변경 시 갱신, 거리/시간에 따른 가림 검사, 실제 활성 차량 중심의 교통·충돌 계산. 시계의 표시 의미와 갱신 주기 정리.

검증: 가림·오르막·포털·원복 회귀 테스트, 같은 위치 A/B, 고속도로 차량 충돌/빈 통로 보존.

### U2. 모바일 렌더·빌드 구성

현재 Mobile URP에 불필요한 SSAO/HDR/업스케일·그림자 비용 정리. 캐릭터 식별성을 유지하며 배경 윤곽선 비용 축소. 구형 SR18 대신 개편 씬을 챕터 순환과 빌드의 기준으로 연결. 임의의 80m 클리핑으로 배경을 지우지 않는다.

검증: 실제 세 씬/그림자/투명 효과, 챕터 로딩 경로, 빌드 성공. 기기 API/칩셋 추측으로 Vulkan/GLES를 강제 전환하지 않는다.

### U3. 모델·텍스처 경량화와 명백한 소품 결함

구멍/가드레일/자판기/표지판을 역할에 맞는 형상으로 교체 또는 단순화, 큰 반복 배경에 LOD/구간 가시성 적용. Android 텍스처 해상도·스트리밍 정리. N19 작업코드·차량의 가짜 글자는 명시적인 라벨로 대체. 원본 소스/충돌 계약 보존.

검증: 네이티브 다면·게임 카메라 전후 PNG, 삼각형/텍스처·빌드 크기 측정, 실제 충돌 경계.

### U4. 카메라·가독성·시장 연출

경사로의 천장/간판 중심 구도, 실내 간판/출구 가림, 흰 바닥 대비, 겹치는 피해·보상·파도 효과 정리. 군중 배치/색/시차와 실제 경매 동작 검수. 기계적 메시지를 짧은 현장 안내로 정돈. 방송과 상인의 목소리를 구분한다.

검증: 1배속 경사 진입/양 분기/휴게소 자연 진입·방어전/고속도로 밀집, 숫자·UI 상태 검증.

### U5. 주인공과 이야기 일관성

기존 직접 요구에 맞는 두 발+꼬리 신발 구조를 게임·꾸미기·영상에 일관되게 연결. 기준 시트와 실제 리그/장착물을 함께 검증한다. 오프닝의 빈 벽 장면과 장소/캐릭터 불일치 정리. 모델 생성은 필요성과 기존 장착/동작 호환성을 확인한 후 사용한다.

검증: 플레이어 다면, 보행/피격/장착 조합, 같은 주인공이 보이는 영상·로비·상점.

### U6. 통합 검증과 전달

관련 Editor 테스트, runtime/editor 빌드, 사용자 저장값 복원, 대표 경로 1배속 실제 플레이, 비교 보고서/PNG, Android 빌드. 성능 수치와 시각 품질을 동시에 검토한다. 발견 교훈은 ce-compound headless로 기록한다.

## 작업 영역

브랜치 `fix/s22-performance-visual-polish-20261001`. 변경 전 사용자 작업과 감사 보고서를 보존한다. 진행 상태/실험 원본은 `outputs/s22-polish-2026-10-01/`, 결과 그림은 `tmp/image-previews/s22-polish-2026-10-01/`에 기록한다. 이 계획 본문은 구현 지시의 기준이며, 세부 진행은 별도 상태 기록에 둔다.


## 최종 적용 및 검증 — 2026-10-01

- 적용 보고서: docs/reviews/s22-improvements-applied-2026-10-01.md.9쪽 PDF와7개 PNG는 output/pdf 및 tmp/image-previews/s22-polish-2026-10-01/figures에 있다. 브라우저 갤러리 http://127.0.0.1:8798/.
- CPU 함수 최종0.775ms(이전10.031ms). 같은 로비120배치/483685tri/53SetPass(이전294/1837086/111). 전체 프레임 중앙14.39ms,p95 15.70ms는 Windows Editor 결과이며 S22 결과가 아니다.
- 최종122개 Editor 검사 통과. runtime/editor dotnet build 및 harness 통과. 바깥1x297.2초 완주. 최종 안쪽은 분기 완료 후263.6초 EnemyContact 사망; 중간 적용본312.73초 완주 기록과 구분. 휴게소 endurance 검사30초 방어완료/23경찰/4방향, 고속도로110초 관찰. 자연 난이도나 모든 챕터 완주를 보증하지 않는다.
- Android ARM64 일반 APK 성공:752146278bytes,오류0/경고58. 이전1526175575bytes 대비50.7169%감소. ZIP CRC 및 manifest 확인. 경고 원문은 build-release/warnings.json. SHA-256은 delivery.json. ADB 미연결로 설치/발열/FPS 실기기 검증 없음.
- 저장 레지스트리 바이트 일치, 코인40218/보석30, 원래 테스트옵션, clean Revamp/Edit Mode 복귀 확인. 사용자에게 수치와 한계를 함께 전달한다.

## 초기 계획에서 판단을 바꾼 항목

- 가장 강한 LOD2는 지붕/UV 손상 때문에 폐기.320개 그룹에는 원본+LOD1만 남겼다.
- 최초 임시 시네마틱6장면은 품질 기준 미달로 설치하지 않았다. 선택된 오프닝 앞30초를 보존하고 마지막9초/두 챕터 진입만 실제 게임 캐릭터 애니메이션으로 교체했다. 전체39초/936frames,0/8/20/30타이밍 유지. 프롤로그의 화풍 차이는 남는다.
- 영상 최적화는 카메라를 비활성화하는 초안에서 cullingMask를 비우고 복원하는 방식으로 변경했다. 첫 수명 검사 캡처는 비동기 CaptureScreenshot 바로 뒤 Skip 때문에 로비를 찍었다. 이 잘못된 증거로 카메라 비활성화의 UI 결함을 단정하지 않는다. 수정된 검사와 실제 합성 영상 화면을 보존했다.
- 보행 중 바닥을 보호하는 별도 collider/visual 연결, 간판 뒷면76개, 단축 카메라의 과한 확대 수정, 구멍 링 winding과 FBX vertex-color 변환을 네이티브 재검수 후 추가했다.
- 차량 텍스처의 모든 가짜 문자/브랜드 재작화는 수행하지 않았다. 완전한 생성 흔적 제거·전체 프롤로그 재작화·실기기60fps 달성을 완료로 주장하지 않는다. 남는 반투명 대비와 고속도로 최대751배치 부담을 보고서에 기록했다.

## 재현/검증 도구

모든 네이티브 명령은 python tools/audit-s22-driver.py tools/파일.cs:클래스.메서드 [인자] 형식의 공식 Unity Pipeline 경로를 사용한다. transient port를 고정하지 않는다.

- 적용/교정: apply-s22-mobile-assets.cs,build-s22-mobile-props.py,install-s22-mobile-props.cs,install-s22-lods.cs,refine-s22-lod-quality.cs,apply-s22-presentation.cs,refine-s22-market-view.cs,import-s22-player-tail.cs,refine-s22-prop-colors.cs,install-s22-cinematics.cs. guarded installer를 무조건 반복하지 않는다.
- 검증: run-s22-focused-tests.py,run-s22-final-validation.py,verify-s22-video-lifecycle.cs,verify-s22-native-assets.cs,capture-s22-motion.cs. 입력과 사진의 유효 범위를 출력 기록으로 확인한다.
- 빌드: build-s22-android.cs:S22AndroidBuild.Main false. 기존 결과를 덮어쓰지 않는 가드가 있다.
- 복원: s22-polish-verification.cs:S22PolishVerification.Restore,VerifyRestored;verify-s22-final-state.cs:S22FinalState.Main.
- 보고서: finalize-s22-report.py,build-s22-improvements-report.py,serve-s22-improvements.py. 서버는 loopback의 선별한 산출물만 공개하고 저장값 스냅샷을 노출하지 않는다.
- 재사용 교훈: docs/solutions/performance-issues/cache-occluder-group-bounds-and-verify-native-output-2026-10-01.md. ce-compound headless 실행; 기존 AGENTS 지식 검색 규칙으로 충분해 추가 승인/규칙 편집 없음.
