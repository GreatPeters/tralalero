"""Finalize claims only from completed build, test and restoration receipts."""
from pathlib import Path
import hashlib,json
ROOT=Path(__file__).resolve().parents[1];D=ROOT/'outputs/s22-polish-2026-10-01'
build=json.loads((D/'build-release/result.json').read_text(encoding='utf-8'));restore=json.loads((D/'restore-verified.json').read_text(encoding='utf-8'));final=json.loads((D/'final-runtime.json').read_text(encoding='utf-8'))
assert build['result']=='Succeeded' and build['errors']==0
assert restore['registryByteIdentical'] and not restore['dirty'] and not restore['play']
tests=[json.loads(p.read_text(encoding='utf-8')) for p in (D/'tests').glob('*.json') if not p.stem.endswith('-request')]
assert all(x['status']=='completed' and x['summary']['failed']==0 for x in tests)
total=sum(x['summary']['passed'] for x in tests);r=final['render'];t=final['timing'];apk=ROOT/build['output'];old=ROOT/'Builds/Android/TralaleroShooter-20260930-AppIcon.apk'
cost=json.loads((D/'optimized-final-occlusion.json').read_text(encoding='utf-8'))['meanMs']
digest=hashlib.file_digest(apk.open('rb'),'sha256').hexdigest();old_size=old.stat().st_size
(D/'delivery.json').write_text(json.dumps(dict(apk=str(apk),bytes=apk.stat().st_size,sha256=digest,previousBytes=old_size,reductionPercent=100*(1-apk.stat().st_size/old_size),tests=total,restored=True),indent=2),encoding='utf-8')
p=ROOT/'docs/reviews/s22-improvements-applied-2026-10-01.md';s=p.read_text(encoding='utf-8').split('## 검증과 한계')[0]
s=s.replace('검증 진행 중','적용·빌드·저장값 복원 확인')
s=s.replace('후속 측정 0.790 ms.',f'최종 재측정 {cost:.3f} ms (캐시 적용 직후 0.790 ms).')
s='\n'.join(line for line in s.splitlines() if not line.startswith('> 이 파일은 작업 중'))+'\n'
s=s.replace('중간 같은 시작 화면 표본은 294→123 배치, 약 183.7만→46.6만 삼각형, 111→53 SetPass였다. 전체 씬의 메시 합계와 프레임당 제출량은 다른 지표다. 최종 설정/LOD 수정 이후의 수치와 APK 크기는 최종 검증 절에 별도로 기록한다.',f'최종 같은 시작 화면은 **294→{r["batches"]} 배치**, **1,837,086→{r["triangles"]:,} 삼각형**, **111→{r["setPass"]} SetPass**였다. 위치·카메라·1080×2340 해상도를 유지한 로비 표본이다. 앞선 기록의 123배치/46.6만 삼각형은 중간 단계이며 최종값으로 재사용하지 않았다. 전체 씬 메시 합계와 프레임당 제출량은 다른 지표다.')
s=s.replace('## 그대로 믿지 않은 지적','- **영상:** 기존에 선택한 프롤로그 앞 30초는 보존하고 마지막 9초를 실제 시장 도착으로 교체했다. 고속도로·휴게소 진입도 같은 게임 모델의 실제 보행·차량 이동·피신 동작으로 구성했다. 216/121/121개 프레임이며 정지 그림을 이어 붙인 슬라이드쇼가 아니다. 오프닝 936프레임/39초와 0·8·20·30초 이동 버튼 규칙을 유지했다.\n\n## 그대로 믿지 않은 지적')
s+=f'''## 검증과 남는 한계

| 검증 | 실제 결과 | 해석 범위 |
|---|---|---|
| Editor 테스트 | 13개 묶음 **{total}개 통과**, 실패 0 | 바뀐 가림·바닥 보호·간판·코스튬·영상 시간·렌더 설정 등. 전체 저장소 테스트가 모두 통과했다는 의미는 아니다. |
| 빌드·정적 확인 | runtime/editor dotnet build 성공, harness 성공, 소스 diff 공백 검사 통과 | Unity가 저장한 YAML의 빈 value 뒤 공백은 별도로 남는다. 씬 YAML을 수동 정리하지 않았다. |
| 안쪽 시장 | 중간 적용본 312.73초 완주. 최종 재실행은 263.6초 EnemyContact 사망, 시장 분기는 완료 | 마지막 실행을 완주로 표시하지 않는다. 원인을 새 성능 회귀로 단정할 증거도 없다. |
| 바깥 부두 | 첫 조작기 95.29초 Wave 사망. 안전 구간을 읽는 조작기로 **297.2초 완주** | 9999·빠른 좌우·1배속. 자동 조작/기믹 검증이며 자연 플레이 승률은 아니다. |
| 휴게소 | 입구부터 102.41초 관찰, **30초 방어 완료**, 경찰 23명, 네 방향 조준·이동 재개 | 체력 유지와 일부 이동 위험 비활성화를 쓰는 구간 검증. 자연 난이도/교통 생존성은 미측정. |
| 고속도로 | 최종 **110초** 실제 진행 관찰 | 차량·라벨·분기 표시 확인. 챕터 전체 완주와 휴대폰 장기 프레임 안정성은 별도다. |
| 런타임 오류 | 위 마지막 시장/고속도로 실행, 앞선 휴게소 검사에서 Error/Exception/Assert 0 | 등록한 관찰 구간 내의 결과다. |
| 영상 | 새 클립 바인딩, 마지막 장면 frame744/page3, EOF·skip·replay·disable 원복 확인 | 카메라 활성 유지, 월드 culling mask 0→원래 값, RenderTexture 해제. 실제 합성 화면도 확인했다. |

최종 PC 시작 화면 240프레임(90프레임 준비)의 중앙값은 **{t['medianMs']:.2f} ms**, p95 **{t['p95Ms']:.2f} ms**다. 별도 함수 측정은 10.03→0.790ms였고 빌드 후 최종 재확인은 **{cost:.3f} ms**다. 전체 프레임과 함수 비용은 측정 범위가 다르며 S22 FPS로 바꾸어 말하지 않는다.

고속도로의 마지막 28개 표본은 88~751배치, 73~657 SetPass였다. 혼잡 구간의 그리기 호출 부담은 아직 남아 있어 실기기에서 우선 확인할 부분이다.

캐릭터·의상·도착 영상의 정체성과 바닥·간판은 개선했지만, 겹친 반투명 천장/기둥에서 대비가 낮아지는 구간은 남는다. 보존한 프롤로그와 실제 게임의 화풍도 완전히 같지는 않다. 경매 음성은 여전히 합성 음성이며 사람의 연기 품질을 보증하지 않는다. 모든 생성 흔적을 없앴다고 주장하지 않는다.

ADB 최종 장치 목록도 비어 있었다. 실기기의 발열 누적·GPU 프레임 시간·배터리·입력 지연은 측정하지 못했다. 기존 광범위 Noryangjin 테스트 기록에는 26개 실패가 있었고 이번에는 그 전체 묶음을 다시 통과시켰다고 주장하지 않는다.

## 설치 파일과 복원

ARM64 일반 APK 빌드가 **성공**했다. 오류 {build['errors']}개, 경고 {build['warnings']}개. 실제 포함 씬은 Revamp·HighWay·RestStop 세 개다.

경고 원문도 보존했다. Toon 34건, FlatKit 표면 19건, 물 2건의 셰이더 경고와 Player Pipeline 비활성 안내, 광고 SDK의 iOS 플러그인 대상 안내, Android 현지화 메타데이터 안내가 각 1건이다. 경고가 없었다고 표시하지 않는다. 새 제조 소품 셰이더의 컴파일 오류는 없었다.

- 파일: `{build['output']}`
- 용량: **{apk.stat().st_size/1e6:,.1f} MB**. 9월 30일 APK {old_size/1e6:,.1f} MB 대비 **{100*(1-apk.stat().st_size/old_size):.1f}% 감소**.
- SHA-256: `{digest}`
- 현재 기기에 설치하거나 플레이했다고 주장하지 않는다. 연결된 S22가 없으므로 전달물은 설치 가능한 APK다.
- 코인 **40,218**, 보석 **30**과 전체 저장 레지스트리를 작업 시작 시점으로 복원했다. 바이트 일치, Play 중지, 원래 개편 씬의 clean 상태, 원래 테스트 옵션 복귀를 확인했다.

원본 모델/영상을 보존했다. 새 자산은 Generated/S22Polish 및 JH/UI/S22Polish에 분리했고, 원본 카탈로그·메타 설정은 작업 전 백업을 남겼다. 유료 Meshy 생성 비용은 0이다.

원시 기록: `outputs/s22-polish-2026-10-01/`. 그림 출처 목록: `applied-figure-manifest.json`. 재현 명령과 빌드/복원 도구는 같은 날짜의 실행 계획에 기록했다. PNG는 실제 Unity 출력과 측정 그래프이며 검증 화면을 이미지 생성으로 재구성하지 않았다.
'''
p.write_text(s,encoding='utf-8')
q=ROOT/'docs/QUALITY_SCORE.md';text=q.read_text(encoding='utf-8');entry=f'''\n## S22 improvements — 2026-10-01 (applied)

CPU occluder cache, scoped renderer/texture reductions, original-plus-reviewed-LOD1 groups, three manufactured props, two-front-feet/tail-shoe anatomy, sign/floor/camera/readout/voice changes and native arrival movies are installed. Targeted Editor suites: {total}/{total}. Android ARM64 release APK: {apk.stat().st_size/1e6:,.1f} MB, {build['errors']} build errors. Runtime/editor builds and harness pass. Preferences and the original clean scene/session are restored.

Final outside route clears297.2s at1x; final inside reaches263.6s before EnemyContact after completing the branch (an earlier intermediate inside cleared312.73s). RestStop's staged endurance coverage completes the authored30s holdout. These are test-override/coverage results, not natural balance. The phone remains disconnected; no device FPS/thermal guarantee. Residual translucent-structure contrast and the retained prologue's style difference remain explicit. [Report](reviews/s22-improvements-applied-2026-10-01.md). Earlier audit section below is the pre-fix state.
'''
if '## S22 improvements — 2026-10-01 (applied)' not in text:q.write_text(text.replace('# Quality Score','# Quality Score\n'+entry,1),encoding='utf-8')
print(json.dumps({'tests':total,'apkMB':apk.stat().st_size/1e6,'sha256':digest}))
