import datetime as dt
import json
import pathlib
import sys

project = pathlib.Path(r'D:\Tralalero Shooter\Tralalero Shooter D')
root = project / 'outputs/chapters45-2026-10-02'
verification = pathlib.Path(sys.argv[1])
def read(path): return json.loads(path.read_text(encoding='utf-8-sig'))
routes = read(verification / 'route-results.json')
comparison = read(verification / 'shared-regressions/failure-comparison.json')
fixtures = read(verification / 'shared-regressions/fixture-results.json')
build = read(root / 'build-release-final/result.json')
package = read(root / 'build-release-final/package-verification.json')
boundary = read(verification / 'product-boundary.json')
resolved_boundary = read(verification / 'shared-side-effect-resolution.json')
build_boundary = read(verification / 'build-boundary-resolution.json')
assert len(routes) == 4 and {(r['scene'], r['choice']) for r in routes} == {('Jamsil',0),('Jamsil',1),('ShoeTower',0),('ShoeTower',1)}
assert all(r['summary']['outcome']=='clear' and not r['summary']['errors'] and not r['summary'].get('animationWarnings') and r['restoration']['restored'] and r['restoration']['mismatches']==0 for r in routes)
assert not boundary['criticalHashChanges'] and not comparison['newFailureNames'] and not comparison['changedFailureMessages']
assert len(fixtures)==4 and all(f['summary']['outcome']=='passed' and f['summary']['failures']==0 and not f['summary']['errors'] and f['restoration']['restored'] and f['restoration']['mismatches']==0 for f in fixtures)
assert build['result']=='Succeeded' and build['errors']==0 and package['projectSettingsRestoredByteForByte']
assert resolved_boundary['outcome']=='accepted' and build_boundary['outcome']=='accepted' and not build_boundary['existingContentChanges']
now = dt.datetime.now(dt.timezone.utc).strftime('%Y-%m-%d %H:%M UTC')
rows=[]
for r in routes:
    s=r['summary']; folder=pathlib.Path(r['folder']).relative_to(root).as_posix()
    label=('4장 잠실' if r['scene']=='Jamsil' else '5장 슈 타워') + (' 회복/방어' if r['choice']==0 else ' 위험/공격')
    rows.append(f"| {label} | {s['seconds']:.2f}초 | {s['initialHealth']:g}→{s['hp']:g} / {s['initialAttack']:g}→{s['attack']:g} | +{s['coin']-s['initialCoins']:,} / +{s['jewel']-s['initialJewels']} | [완주·복원 기록]({folder}/summary.json) |")
fixture_rows=[]
for f in fixtures:
    folder=pathlib.Path(f['folder']).relative_to(root).as_posix()
    fixture_rows.append(f"- `{f['scene']} / {f['fixture']}`: {len(f['summary']['checks'])}/{len(f['summary']['checks'])} 통과, 오류 0. `{folder}`.")
summary=comparison['afterSummary']; shared=comparison['sharedRuntimeCohort']; chapter=comparison['chapter45']
text=f'''# 4·5장 최종 검증 완료 기록

{now}. 이전 인계에서 남았던 최종 코드의 네 경로 재완주와 공용 런타임 재검증을 완료했다. 이 기록이 이전 경로별 검증 시점 및 APK 상태를 대체한다.

## 이번에 발견하고 수정한 결함

미분류 실패를 조사하던 중, 이번 Jamsil에서 사용한 C08 안내원 15개에 공격·사망 애니메이션 상태가 없는 실제 표현 결함을 찾았다. 기존 오류 전용 로그는 Animator 경고를 놓쳤다. 독립 검토 후 해당 외형만 완전한 C07 모델과 그 모델의 Generic 아바타/컨트롤러로 바꿨다. 전투 루트·체력·보상·경로·충돌체는 전후 직렬화 지문이 동일하다. 원본 공용 프리팹은 수정하지 않았다.

실제 공격/사망 API, Animator 상태, BakeMesh 변형, 사망 후 비활성화 검사는 21/21 통과했다. 초기 검사 화면의 공격 파일명만으로 정확한 순간을 증명하지 않으며, 상태/메시 기록이 공격 동작의 근거다. 두 씬의 실제 컨트롤러/클립 계약 검사도 추가했다. [수정 근거](qa/flagger-repair-20261001T211830870/receipt.json), [네이티브 동작 검사](play/20261001-212000-195-Jamsil-0/native-actor-animation/summary.json).

## 같은 최종 상태에서 네 경로 완주

| 경로 | 완주 시간 | 체력 / 공격력 | 코인 / 보석 변화 | 근거 |
| --- | ---: | --- | ---: | --- |
{chr(10).join(rows)}

네 실행 모두 오류·애니메이터 경고 0건, 저장 복원 불일치 0건이다. 기본 HP60·공격력8, 1배속, 실제 이동/발사/접촉을 사용했으며 강제 승리·순간이동·스탯 고정은 없다. 두 Tower 경로에서 각각 실제 6초 승강기 두 번과 물리적 신발 회수가 완료됐다. Jamsil은 두 경로 모두 실제 ShoeTower 로비로 전환됐다. 위험 경로 보상은 전투 뒤 지급됐으며 공격 선택은 실제 공격력을 9.6으로 바꿨다.

입력 봇은 적 체력·안전 차선 정보를 사용한다. 모든 완주에서 체력이 유지된 결과는 이 봇의 회피 가능성을 보여주며 사람의 재미나 터치 난도를 증명하지 않는다. 네 실행 사이 핵심 코드·씬·설정 해시 변경은 0건이다. [경계 비교]({verification.name}/product-boundary.json).

## 최종 공용 회귀 검사

최종 전체 네이티브 EditMode: **{summary['passed']}/{summary['total']} 통과, {summary['failed']} 실패**. 4·5장 검사는 **{chapter['passed']}/{chapter['total']} 통과**. 독립 검토자가 정한 공용 런타임 묶음은 {shared['passed']}/{shared['total']} 통과, {shared['failed']} 실패이며, 실패는 이전 기록과 대조했다. 새 실패 이름 0건, 달라진 실패 메시지 0건, 동일 실패 서명 {len(comparison['sameFailureSignatures'])}건이다. 전체 검사를 통과했다고 표현하지 않는다.

{chr(10).join(fixture_rows)}

위 단계별 검사는 실제 승강기 중 일시정지·사망·재시도, 도우미 상태, 발사체/층 판정, 실제 위험 영역 접촉, 컴포넌트 비활성화·씬 언로드 정리를 재검증했다. 임시 배치 검사이며 일반 완주와 구분한다. 전체 검사와 각 단계별 검사 뒤 준비한 사용자 저장 데이터가 복원됐다. [전체 비교]({verification.name}/shared-regressions/failure-comparison.json), [단계별 결과]({verification.name}/shared-regressions/fixture-results.json).

기존 53건의 발생 시점 분류는 과거 동일 서명26, 작업 전 입력6, 기존 계약/신규 메뉴 혼합1, 작업 전 기준 미확정20이다. 20건 각각의 현재 실패 원인은 추가로 대조했다. 신발27/31본 및 꼬리 신발 위치 기대값은 의도된 리그와 충돌하며, 실제16개 메시의 인덱스·가중치는 정상이다. 그 밖에 구형 씬 개수/계층/이름 및 UI 기대값이 후속 검사를 막는 구체적인 공백을 기록했다. 이것이 모든 과거 실패를 무해하거나 작업 전부터 있었다고 입증하는 것은 아니다. [20건 원인·잔여 위험](qa/remaining20-source-triage.md), [현재 메시 근거](qa/shared-rig-static-evidence.json).

## 저장 상태와 패키지

- 브랜치 `fix/s22-performance-visual-polish-20261001`, HEAD `295a18cab`. 새 커밋·푸시 없음, 기존 미커밋 변경 유지.
- 저장 씬: `Assets/ShooterSurvival/Scenes/Tools/Jamsil.unity`, `Assets/ShooterSurvival/Scenes/Tools/ShoeTower.unity`.
- 수정 뒤 새로 빌드한 검토 APK: `Builds/Android/TralaleroShooter-20261002-Chapters45-release-final.apk` (프로젝트 루트 기준).
- ARM64 비개발 빌드, {build['bytes']:,}바이트, 오류0·경고{build['warnings']}, {build['seconds']:.2f}초. ZIP 전체 CRC, 5개 씬, Android v2 서명 검증 완료. 로컬 Android 디버그 서명이며 스토어 제출용 서명이 아니다. 기존 APK는 보존했다.
- APK SHA256: `{package['sha256']}`. ProjectSettings는 빌드 전과 바이트 단위로 동일하게 복원됐다.

## 자산·독립 검토와 한계

설치된 Trellis2 생성은 실제 수행했지만 얇게 나온 결과는 거절했다. 최종 신발 건축은 실제 네 번째 인트로를 기준으로 Blender에서 입체 재구축·검토·수정한 자산이다. 기존 생성된 Meshy 모델을 사용했으며 이번 안내원 수정도 기존 C07 자산을 재사용했다. 신규 Meshy 유료 생성·구매·충전·새 자격증명 설정은 없다. 신규 AI 오디오는 생성하지 않았으며 기존 음악/효과음, Jamsil 교통음을 사용한다. Tower 별도 분위기음 클립은 없다. Qwen 전용 모델/컨트롤러 중지와 부재는 확인했지만 정확한 반환 RAM/VRAM은 측정하지 않았다.

설계, 비평, 구현, QA, 최종 검토를 분리했고 root만 씬/Unity를 변경했다. [독립 최종 검토](qa/independent-final-review.md)가 승인 범위와 실제 화면 근거를 기록한다. 남은 한계는 S22 실기기 FPS·메모리·발열, 사람의 재미/터치 편의성, 모든 유료 신발 스타일의 새 애니메이션 검증, 실패한 구형 씬 검사 뒤의 미실행 단언들이다. 이번 범위에 남은 재현 가능한 수정 대상은 확인되지 않았다.

라이브 로그: `D:\\Tralalero Shooter\\Tralalero Shooter D\\docs\\exec-plans\\active\\chapters45-progress-2026-10-02.md`.
'''
text += f'''

## Shared test side effects and preservation

The build guard detected 21 shared assets re-saved by the broad test suite. All affected bytes were backed up before remediation. The transient dynamic-font cache returned exactly to its proven clean baseline, followed by 11 native visual stations with no errors or missing glyphs. The two previously modified materials match a real 17:35 dirty-worktree checkpoint byte-for-byte. The bonus-box prefab matches all serialized properties of that checkpoint after normalizing regenerated local IDs. External-reference classification and the missing immediate pre-suite byte snapshot are explicitly recorded in the [independent boundary resolution]({verification.name}/shared-side-effect-resolution.json). No dirty asset was reset to HEAD. This temporal evidence limit is retained; complete byte-for-byte preservation of the immediate pre-suite prefab is not claimed.

Every preexisting product file was hashed before and after the final build: **{build_boundary['files']:,} existing files, zero content changes**, plus two expected generated Addressables linker files (`link.xml` and its importer metadata), {build_boundary['totalAfterBuild']:,} files total. The reviewer inspected and accepted these additions. The broad test suite was not rerun after preservation closure, because its generator would mutate the assets again. The four accepted ordinary routes use neither the bonus-box prefab nor its touched materials; their source/scene hashes remain the tested values.
'''
(root/'FINAL-VERIFICATION.md').write_text(text,encoding='utf-8')
print(json.dumps({'written':str(root/'FINAL-VERIFICATION.md'),'routes':len(routes),'tests':summary,'buildWarnings':build['warnings']},ensure_ascii=False))
