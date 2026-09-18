# 마지막 걷기 장면: 5B / 14B FP8 비교

기존 마지막 약5초 장면을 보존하고 Wan2.2 I2V-A14B FP8 비교본 하나를 생성했다. 이후 사용자의 적용 요청으로 게임 오프닝 MP4의 마지막121프레임을14B 버전으로 교체했다. 앞363프레임은 기존 오프닝과 디코딩 해시가 같고, 전체484프레임/24fps/20.17초 및 자막 구간을 유지한다. 기존 전체 영상은 `installed/opening-before-14b.mp4`에 보관했다. [설치 검증](installed/installation.json).

Unity 가져오기에서484프레임/576×1024를 확인했고, 실제 Play Mode에서16초/384프레임과 네 번째 자막이 함께 재생되는 것을 확인했다. [실행 검증](installed/unity-playback.json) · [게임 적용 화면](../../tmp/image-previews/opening-walk-14b-2026-09-12/installed-in-game.png). 확인 후 원래 씬의 Edit Mode로 돌아왔다. 영상 GUID와 씬 참조는 유지했다.

바로 보기: [좌우 비교](comparison-5b-vs-14b.mp4) · [절반 속도 비교](comparison-half-speed.mp4) · [14B 영상](shot-04-14b-fp8.mp4) · [기존5B 영상](shot-04-5b-existing.mp4)

## 결과와 한계

- 성공한14B 실행은 **3244.136초(54분4초)** 걸렸다. 기존5B 마지막 장면은567.35초(9분27초)였다. 다운로드 약12분26초 및 중단한 첫 메모리 방식 시험은 성공 실행 시간에 포함되지 않는다.
- 생성된121프레임과4개 정상 속도 MP4를 전부 디코딩해 프레임 수·크기를 검증했다. 절반 속도 비교본도 디코딩 검증했다. 원화와 기존5B 파일은 SHA256이 그대로다. 자세한 값은 [validation.json](validation.json)에 있다.
- 프레임30·60·120 및 좌우 비교 이미지를 확인했다. 이 표본에서는14B의 배경 표지판에 보이는 색 번짐이 줄었지만, 중간 프레임의 얼굴/몸 잔상과 세 다리 형태 유지 문제는 남는다. 이번 한 결과로14B가 전반적으로 크게 우월하다고 결론내리지 않는다.
- 두 모델 모두 같은 작은 temporal tiled decode 설정을 사용했다. 공통 잔상이 모델 자체의 한계인지 디코딩 설정의 영향인지는 분리 검증하지 않았다. 향후 디코더 비교를 하려면 샘플링 latent를 먼저 파일로 보존해야 한다. 이 실험은 모델별 실제 워크플로 결과 비교이며 파라미터 수만의 효과를 증명하지 않는다.
- 첫 실행은 dynamic VRAM 방식으로5단계까지 처리한 뒤 메모리 조사 목적으로 중단했다. 기록은 `attempt-01-dynamic/`에 보존했다. 같은 화질 설정에서 `--disable-dynamic-vram`만 바꾼 두 번째 실행을 완료했다. 공유 메모리 할당은 줄었으나 속도 개선은 제한적이었다. [조사 기록](memory-investigation.json).
- Windows 메모리 표본은 고노이즈 단계 후반부터 약15초 간격으로 수집했다.129개 표본에서 관측한 최대 전용 메모리는24.09GiB, 공유 메모리는0.10GiB였다. 모든 순간의 최고 사용량을 보장하는 수치는 아니다. ComfyUI의 사용 가능 메모리는 torch의 미사용 캐시를 포함하므로 Windows 전용 메모리 점유와 직접 비교하지 않는다.
- `compare.html`에 동시 재생·배속·프레임 이동 UI도 준비했으나, CUA가 연결된 브라우저를 찾지 못해 실제 브라우저 조작/스크린샷 검증은 못 했다. 검증된 MP4들을 기본 전달물로 사용한다.
- 작업 전용 ComfyUI 서버는 큐가 빈 것을 확인하고 종료했다. 모델 파일은 다음 사용을 위해 설치 위치에 남겨뒀다.

## 비교 조건

- 같은 입력 원화: `Assets/JH/UI/Opening/Animated/Story_04.png`
- 같은 긍정·부정 프롬프트, 시드202609114,121프레임,24fps, 총20스텝
- 생성 해상도512×896; 비교용 출력은 기존과 같은576×1024
- 기존5B: TI2V5B FP16, UniPC, CFG5, shift8
- 신규14B: I2V-A14B FP8 scaled, Euler, CFG3.5, shift5,10/20스텝에서 전문가 전환
-14B는 공식 일반 모드 설정을 따르며 가속 LoRA를 사용하지 않는다. 모델별 VAE와 샘플링 방식이 다르므로 파라미터 수 하나만 바꾼 실험으로 해석하지 않는다.

원본 영상은 재생성하지 않는다. 원화와 기존 영상의 SHA256은 `comparison-contract.json`에 기록한다. 기존5B 장면의 서버 실행 시간은567.35초였다. 신규 시간은 모델 준비/다운로드와 구분해 측정한다.

## 파일과 재현

- `source.png`: 비교에 사용한 동일 원화
- `shot-04-5b-existing.mp4`: 기존5B 영상의 동일 사본
- `workflow-api.json`: 실행할14B 네이티브 ComfyUI 그래프
- `official-template.json`: 참고한 공식 일반 모드/터보 모드 템플릿
- `models-manifest.json`: 필요한 모델만 나열한 고정 리비전·SHA256
- `models-ready.json`: 다운로드/기존 파일 검증 결과
- `request.json`, `history.json`, `memory-samples.jsonl`: 생성 상태와 측정 근거
- `generation-report.json`: 완료 시 프레임 수와 실제 실행 시간
- `frames-14b/`: 생성된 원본 프레임
- `shot-04-14b-native.mp4`: 확대하지 않은512×896 영상
- `shot-04-14b-fp8.mp4`: 기존과 같은 출력 규격의14B 영상
- `comparison-5b-vs-14b.mp4`: 왼쪽 기존5B / 오른쪽 신규14B
- `comparison-half-speed.mp4`: 프레임 보간 없이 절반 속도로 재생하는 좌우 비교본
- [중간 프레임 비교 PNG](../../tmp/image-previews/opening-walk-14b-2026-09-12/comparison-middle.png)

`python -X utf8 tools/prepare-opening-walk-14b.py`로 필요한 두 FP8 모델과 VAE를 검증한다. 기존 로컬 ComfyUI가 실행 중인 상태에서 `python -X utf8 tools/compare-opening-walk-14b.py --generate --encode`로 실행·수집·인코딩한다. 이미 완료된 생성과 영상은 보존한다.

참고: [ComfyUI 공식 Wan2.2 가이드](https://docs.comfy.org/tutorials/video/wan/wan2_2), [공식 FP8 배포](https://huggingface.co/Comfy-Org/Wan_2.2_ComfyUI_Repackaged), [Wan 모델 라이선스](https://huggingface.co/Wan-AI/Wan2.2-I2V-A14B#license-agreement).
