# 감성과 공포 — BGM 후보 10곡

사용자 요구 U87에 맞춘 새 후보. 캔디크러쉬 사가 월드맵의 감성, 히어로즈3 메인 메뉴의 판타지 방향, The Sound of Your Fear의 크리피한 분위기를 함께 요청했다.

## 듣기

- [10곡 비교 청음 페이지](listen/index.html)
- [전체 재생목록](listen/all-10.m3u8)
- 파일 폴더: `C:\Users\ljh\tralalero Shooter\map-concepts\emotional-horror-bgm-2026-09-15\listen`

| 번호 | 제목 | 제작 방향 | MP3 |
|---|---|---|---|
| 01 | 사탕빛이 꺼진 뒤 | 아련한 오르골과 어두운 동화 왈츠 | [듣기](listen/01_candy_after_dark.mp3) |
| 02 | 잊힌 왕국의 문 | 서정적인 관현악과 장엄한 불안감 | [듣기](listen/02_forgotten_kingdom.mp3) |
| 03 | 물속에 잠긴 자장가 | 피아노 자장가 아래로 스며드는 공포 | [듣기](listen/03_drowned_lullaby.mp3) |
| 04 | 마지막 등불의 약속 | 항구의 그리움과 따라오는 그림자 | [듣기](listen/04_lanterns_last_promise.mp3) |
| 05 | 멈춘 회전목마 | 장난감 왈츠와 낡은 카니발의 섬뜩함 | [듣기](listen/05_broken_carousel.mp3) |
| 06 | 돌아갈 수 없는 밤길 | 전진하는 리듬과 쓸쓸한 신스 멜로디 | [듣기](listen/06_midnight_return.mp3) |
| 07 | 아무도 없는 만찬 | 고풍스러운 선율과 비어 있는 공간의 긴장 | [듣기](listen/07_empty_banquet.mp3) |
| 08 | 검은 밀물의 진혼곡 | 깊은 현악의 슬픔과 무거운 공포 | [듣기](listen/08_black_tide_requiem.mp3) |
| 09 | 금 간 도자기의 기억 | 가녀린 추억의 멜로디와 불안한 오르골 | [듣기](listen/09_porcelain_memory.mp3) |
| 10 | 새벽 직전의 문 | 희망이 잠깐 비치는 서정적인 공포 | [듣기](listen/10_gate_before_dawn.mp3) |

각 곡의 MP3(192kbps), WAV, OGG 파일을 같은 이름으로 제공한다. 원본은 64초 스테레오44.1kHz이며, 앞뒤 무음 정리와 2초 겹침 처리를 한 배포본은 56.41~62초다. 파일별 설명·BPM·악기는 생성 방향이며, 실제 연주의 악보 전사나 청음 평가 결과는 아니다. 자동 신호 검증과 실제 선호도 판단은 구분한다.

## 참고와 생성 기록

제공한 URL의 제목은 YouTube oEmbed로 확인했다. 웹 페이지 본문 접근은 실패했다. 원본 녹음을 다운로드하거나 생성 모델에 입력하지 않았으며, 사용자가 말한 정서와 악기·편곡 지시를 바탕으로 로컬에서 새 음원을 생성한다.

- [Candy Crush Saga OST - World Map](https://www.youtube.com/watch?v=gcFB7mjZiVk)
- [Heroes Of Might And Magic III Soundtrack-Main Menu](https://www.youtube.com/watch?v=kKg-dlrVH9k)
- [THE SOUND OF YOUR FEAR | MIDI BLOSSO](https://www.youtube.com/watch?v=jj3yM0bhHNw)

기존 설치된 Stable Audio3 Small-Music / SAME-S, fp32,8steps,CFG1.5,CPU8threads. 최초 Seeds26091501–26091510. 8번 원본은 중간 약4.5초의 무음 때문에 제외하고, 지속되는 반주를 지시한 seed26092508의 새 생성본으로 교체했다. 최종8번의 전체 프롬프트·로그는 `raw-retry-08/`에 있다. 나머지 모델·디코더 위치와 전체 프롬프트는 `raw/bgm-requests.json`, 생성 로그·시간·원본은 `raw/`에 남긴다. 기존의 2곡·5곡 후보와 게임에 설치된 `Resources/Audio/Mobile/music.ogg`는 이 제작 스크립트의 출력 대상이 아니다.

재현 명령(PowerShell):

```powershell
& 'C:/AI/StableAudio3/optimized/tflite/.venv/Scripts/python.exe' 'tools/generate-emotional-horror-bgm.py'
& 'C:/AI/StableAudio3/optimized/tflite/.venv/Scripts/python.exe' 'tools/generate-emotional-horror-bgm.py' --retry-track 8
& 'C:/AI/StableAudio3/optimized/tflite/.venv/Scripts/python.exe' 'tools/prepare-emotional-horror-bgm.py'
```

스크립트는 기존 결과 덮어쓰기를 거부한다. 다시 제작할 때에는 새 출력 폴더와 seed를 사용한다. 후처리는 일정 gain으로 목표 -18LUFS에 가깝게 맞추되 true peak -2dB 한도를 우선하며, 이어붙인 파형에 동적 limiter를 걸지 않는다. 최종 WAV와 디코딩한 MP3의 레벨, 무음 구간, 루프 경계의 인접 샘플 차이, SHA256은 `signal-report.json`에 기록한다. 크로스페이드는 샘플 연결을 부드럽게 하는 처리이며 악절이 음악적으로 완벽히 맞는 루프라는 보증은 아니다.

모델 출처: [Stable Audio3](https://github.com/Stability-AI/stable-audio-3). 모델 이용 조건은 설치본의 [Stability AI Community License](https://stability.ai/license)를 따른다. 생성 결과를 CC0로 재분류하지 않으며 모델 가중치는 게임에 포함하지 않는다.

## 확인 결과

- 10개 고유 마스터와 MP3/WAV/OGG 총30개 파일, 청음 페이지·재생목록의31개 파일 링크를 확인했다. MP3는 FFmpeg로 디코딩해 검사했다. `verification.json`과 `signal-report.json`에 수치를 보존했다.
- 최종 WAV true peak는 모두 -2dB 이하, MP3는 -2.25dB 이하다. 가장 긴 -55dB 이하 구간은2.3초이며, 첫 시도의 긴 내부 무음은 재생성으로 해결했다. `processing-attempt-01/`은 제외한 첫 후처리 결과다.
- 선율의 강약을 유지하고 peak 한도를 우선했으므로 실제 WAV 음량은 -23.32~-17.99LUFS로 차이가 남는다. 특히8~10번은 더 조용할 수 있다. 최종 파일의 clipping 검사는 원본에 이미 생긴 모든 왜곡을 제거했다는 뜻이 아니다. 5번 원본에서 최대 진폭 근처94샘플(전체0.0017%)을 기록했다.
- CUA에 연결된 브라우저가 없어 화면 캡처·브라우저 재생 동작 확인은 하지 못했다. 페이지 구조, 모든 로컬 링크의 HTTP200 응답, 음원 디코딩을 확인했으며 `listen/index.html`은 서버 없이 직접 열 수 있다.
