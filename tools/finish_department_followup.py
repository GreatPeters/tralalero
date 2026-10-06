import datetime,json,pathlib,hashlib,statistics,shutil
from PIL import Image,ImageChops,ImageStat
ROOT=pathlib.Path(r'D:\Tralalero Shooter\Tralalero Shooter D')
D=ROOT/'outputs/department-store-2026-10-02'; F=D/'followup-v4'
def j(p):return json.loads(p.read_text(encoding='utf-8-sig'))
def write(p,s):p.write_text(s,encoding='utf-8')
def wj(p,s):write(p,json.dumps(s,ensure_ascii=False,indent=2))
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
runs=j(F/'ordinary-routes.json');assert len(runs)==2
table=['| 현재 v5 선택 | 클리어 시간 | 리프트 | 오류/애니메이션 경고 | 설정 복원 불일치 |','| --- | --- | --- | --- | --- |']
for r in runs:
 s=r['summary'];assert s['outcome']=='clear' and not s['errors'] and not s['animationWarnings'] and r['restoration']['mismatches']==0
 assert s['lifts']==2 and s['lastLiftSeconds']==6 and s['initialHealth']==60 and s['initialAttack']==8
 assert all(c['Selected'] and c['Selection']==r['choice'] and c['Rewarded'] for c in s['choices'])
 assert next(t for t in s['targets'] if t['name']=='CrownCaptain')['Health']==0
 table.append(f"| {r['choice']} | {s['seconds']:.3f}초 | 2 | 0 / 0 | 0 |")
tests=j(F/'focused-test-status.json');assert tests['summary']['passed']==37 and tests['summary']['failed']==0
assets=j(F/'focused-assets-restored.json');assert assets['verified']==208 and not assets['remainingMismatches']
assert j(F/'focused-preferences-restored.json')['mismatches']==0
before=j(F/'focused-state-before.json');after=j(F/'focused-state-after.json')
assert before['editorCleanupExists']==after['editorCleanupExists']==True and before['editorCleanup']==after['editorCleanup']==False
assert not before['playerCleanupExists'] and not after['playerCleanupExists'] and not after['play']
direct=[]
for r in j(F/'directed-fixtures.json'):
 s=r['summary'];assert s['outcome']=='passed' and s['failures']==0 and not s['errors'] and r['restoration']['mismatches']==0
 direct.append(f"- 현재 v5 `{r['fixture']}`: **{len(s['checks'])}/{len(s['checks'])} 통과**, 오류 0, 설정 복원 불일치 0.")

v3=next(r for r in j(D/'ordinary-polish-routes.json') if r['scene']=='ShoeTower')
v5=next(r for r in runs if r['choice']==1)
metrics={}
for label,r in [('v3',v3),('v5',v5)]:
 data=[json.loads(line) for line in (pathlib.Path(r['folder'])/'timeline.jsonl').read_text(encoding='utf-8-sig').splitlines()]
 data=[x for x in data if x['floor']==0 and not x['lift'] and x['distance']>10]
 metrics[label]={'folder':r['folder'],'samples':len(data),'scope':'floor 0, not lift, distance > 10m, choice 1','counters':{k:{'median':statistics.median(x['render'][k] for x in data),'max':max(x['render'][k] for x in data)} for k in ['batches','setPassCalls','triangles']}}
wj(F/'render-comparison.json',metrics)
perf=['| 선택 1의 1층 | Batches 중앙/최대 | SetPass 중앙/최대 | 삼각형 중앙/최대 |','| --- | --- | --- | --- |']
for label,m in metrics.items():
 c=m['counters'];perf.append('| '+label+' | '+' | '.join(f"{c[k]['median']:,.0f} / {c[k]['max']:,}" for k in ['batches','setPassCalls','triangles'])+' |')

visual=F/'visual-20261002T170332889';lighting=[]
lt=['| 게임 카메라 위치 | 조명 ON/OFF RGB 평균 절대 차이 (0–255) | 해석 |','| --- | --- | --- |']
for pos in ['0000','0045','0175','0320']:
 a=visual/('final-'+pos+'.png');b=visual/('lights-off-'+pos+'.png');diff=ImageChops.difference(Image.open(a).convert('RGB'),Image.open(b).convert('RGB'))
 row={'station':pos,'on':str(a),'off':str(b),'differentBoundingBox':diff.getbbox(),'meanRgbAbsoluteDifference':ImageStat.Stat(diff).mean};lighting.append(row)
 lt.append('| '+str(int(pos))+'m | '+', '.join(f'{n:.4f}' for n in row['meanRgbAbsoluteDifference'])+' | '+('화면 차이 없음' if row['differentBoundingBox'] is None else '일부 화면 영역에만 변화')+' |')
wj(F/'lighting-ab.json',lighting)
old=j(D/'preservation-final.json');pres=j(F/'preservation-final.json');backup=j(F/'preserved-v3.json')
assert not pres['invalidRecoveryCopies'] and not pres['sharedAssetHashMismatches'] and not pres['missingPreexistingProducts']
assert all(sha(pathlib.Path(r['recoveryPath']))==r['sha256'] for r in backup['files'])
jamsil='Assets/ShooterSurvival/Scenes/Tools/Jamsil.unity'
assert pres['intentionalSceneHashes'][jamsil]==old['intentionalSceneHashes'][jamsil]
productBefore=j(D/'product-metadata-before.json')
sources=['Assets/ShooterSurvival/Prefabs/RestStopProduction20260925/F10.prefab','Assets/ShooterSurvival/Models/RestStopProduction20260925/F10/F10.fbx','Assets/ShooterSurvival/Models/RestStopProduction20260925/F10/Surface_0.mat']
sourceInfo=[]
for p in sources:
 f=ROOT/p;assert f.exists()
 now=[f.stat().st_size,f.stat().st_mtime_ns]
 sourceInfo.append({'path':p,'currentSha256':sha(f),'unchangedInitialSizeMtime':productBefore[p]==now})
assert all(r['unchangedInitialSizeMtime'] for r in sourceInfo)
extra={'utc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'v3RecoveryFilesValid':len(backup['files']),'jamsilByteHashUnchangedSinceV3':True,'sourceF10':sourceInfo,'sourceHashLimit':'F10 original handoff content hash was not separately recorded; initial full-product size/mtime is unchanged, current hashes recorded.'}
wj(F/'followup-preservation.json',extra)
presText=f"공유 자산 208개 해시 불일치 0, 최초 복구 사본 210개 정상, 후속 수정 전 v3 파일 2개 복구 사본 정상, 기존 제품 파일 누락 0이다. Jamsil의 SHA-256은 이전 v3와 정확히 같다. 재사용한 F10 원본 3개는 최초 전체 제품 목록의 크기/수정 시각과 같으며 현재 해시도 기록했다. 최초 F10의 별도 내용 해시는 없으므로 전체 원본 해시 비교라고 표현하지 않는다. 전체 제품 파일은 경로/크기/수정 시각을 비교했고, 모든 기존 파일의 내용을 해시한 것은 아니다. 제한 검사 중 공유 자산 변경은 {len(assets['changedDuringTests'])}개였다."
report=pathlib.Path(__file__).with_name('department-followup-report.md').read_text(encoding='utf-8')
for token,value in {'ROUTE_TABLE':'\n'.join(table),'LIGHT_TABLE':'\n'.join(lt),'DIRECTED':'\n'.join(direct),'PERF_TABLE':'\n'.join(perf),'PRESERVATION':presText}.items():report=report.replace('{{'+token+'}}',value)
assert '{{' not in report
write(ROOT/'docs/reviews/department-store-backend-followup-2026-10-02.md',report)
write(F/'art-review-v5.md','''# Independent art review — v5

Reviewer: art_standards_review. Read-only inspection of eight native images; no Unity commands or file changes.

The dark upper side-wall issue from v4 is resolved: shelves and inner displays are visible in final-0000, final-0320 and architecture-final. ON/OFF comparison shows actual warm light at display entrances, goods and columns, strongest at the lower-left shop of the architectural camera. The effect is localized; it does not establish a broadly improved atrium lighting mood.

The F10 closeup shows one plant naturally placed on the counter. It does not verify all three plants visually or ordinary-gameplay visibility. Four gameplay camera poses showed no new blockage of the player, route edges or existing obstacles.

Remaining gaps: repetitive shops, box-like merchandise, broad empty floor, limited skylight impression in the gameplay camera. Suitable description: stylized department store with improved display depth and partial warm lighting. No new blocking visual defect was found in the reviewed images. Photo-reference richness remains unfinished.
''')

def afterTitle(relative,marker,text):
 p=ROOT/relative;s=p.read_text(encoding='utf-8-sig')
 if marker not in s:
  first,rest=s.split('\n',1);write(p,first+'\n\n'+text+'\n'+rest.lstrip('\n'))
afterTitle('docs/reviews/department-store-2026-10-02.md','후속 v5 보완','> 후속 v5 보완: 이 문서는 v3 시점의 이력이다. [최신 적용·재검증](department-store-backend-followup-2026-10-02.md), [TRELLIS/Meshy 정확한 경로 진단](department-generation-diagnosis-2026-10-02.md)을 먼저 참고한다. 새 AI 생성은 여전히 0개다.\n')
afterTitle('ARCHITECTURE.md','Department-store follow-up v5','''## Department-store follow-up v5 — 2026-10-02

Latest ShoeTower scenery revision is `20261002T170206609`; Jamsil remains byte-identical to the verified v3 scene (`20261002T162312204`). Tower-only installation deepens retail alcoves, changes upper returns to clear glass, adds six shadowless localized spotlights and reuses three existing TRELLIS F10 plants with original materials. Original lights are now part of the protected-state comparison. No gameplay transport or story change; two existing six-second lifts remain authoritative. New AI models: 0. [Current evidence and exact generation blockers](docs/reviews/department-store-backend-followup-2026-10-02.md).

The continuation section below describes the earlier v3 implementation.
''')
afterTitle('docs/README.md','백화점 후속 v5','- **백화점 후속 v5 · 2026-10-02 최신**: [매장 깊이/국소 조명·현재 버전 검증](reviews/department-store-backend-followup-2026-10-02.md), [생성 경로 차단 진단](reviews/department-generation-diagnosis-2026-10-02.md), [후속 갤러리](http://127.0.0.1:1625/). 신규 AI 생성 0개. 아래 v3 결과는 이전 단계의 기록이다.\n')
afterTitle('docs/QUALITY_SCORE.md','Department-store follow-up v5','''## Department-store follow-up v5 — 2026-10-02

Both current ShoeTower ordinary-input choices clear with two lifts and boss defeat; final focused tests 37/37, lifecycle 37/37 and campaign transaction checks 57/57. Shared recovery hashes and known preferences restored. Display depth and upper-side visibility improved; six lights have a measured localized effect. Three old TRELLIS F10 plants reused; new AI models remain 0. Added retail geometry/plants increase render counters, and mobile hardware performance is unverified. Repetitive merchandise, empty floor and limited gameplay skylight remain art gaps. [Current evidence](reviews/department-store-backend-followup-2026-10-02.md). The v3 section below is historical.
''')
afterTitle('docs/RELIABILITY.md','Generation access diagnosis boundaries','''## Generation access diagnosis boundaries — 2026-10-02

Treat refused local status connections, process creation denied, missing client Python dependencies and forbidden external sockets as separate failures. Existing local TRELLIS and Meshy paths exist even when no callable connector is exposed. Do not claim invalid Meshy credentials or zero balance without an HTTP response. No elevation/alternate route may bypass a specifically denied generator path; report the exact time, target and minimum user action. [Recorded diagnosis](reviews/department-generation-diagnosis-2026-10-02.md). Warm-light appearance claims require same-camera geometry-preserving ON/OFF evidence, not emissive material values alone.
''')
afterTitle('docs/exec-plans/active/chapter4-reference-visuals-2026-10-02.md','Latest follow-up v5','''## Latest follow-up v5 — 2026-10-02

Current ShoeTower retail-depth/glass/local-light improvements and three reused TRELLIS F10 plants are applied; Jamsil v3 is unchanged. Both current tower choices and directed lifecycle/campaign checks pass; final focused tests 37/37 and known-state restoration pass. New AI generation remains 0: TRELLIS process creation is denied after a refused local endpoint, and Meshy balance access is blocked before HTTP. [Current result](../../reviews/department-store-backend-followup-2026-10-02.md), [exact diagnosis](../../reviews/department-generation-diagnosis-2026-10-02.md). Remaining work is generation access, reference fidelity and mobile verification, not unanswered placement approval.
''')
write(D/'status.json',json.dumps({'updatedUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'phase':'followup-applied-verified-open-generation-art-device-gaps','towerRevision':'20261002T170206609','jamsilRevision':'20261002T162312204','ordinaryCurrentTowerRoutes':2,'focusedTests':tests['summary'],'directedChecks':94,'newAiModels':0,'reusedTrellisF10Plants':3,'remaining':['TRELLIS official interpreter execution denied WinError5 after endpoint refusal10061','Meshy balance HTTP never reached; external socket denied10013; no credit authorization','Repetitive simple retail props, empty floor, weak gameplay skylight','Mobile hardware performance unverified; APK deferred','Historical52 broad failures unresolved'],'report':'docs/reviews/department-store-backend-followup-2026-10-02.md','diagnosis':'docs/reviews/department-generation-diagnosis-2026-10-02.md'},ensure_ascii=False,indent=2))
print(json.dumps({'routes':[(r['choice'],r['summary']['seconds']) for r in runs],'tests':tests['summary'],'render':metrics,'jamsilUnchanged':True,'report':'docs/reviews/department-store-backend-followup-2026-10-02.md'},ensure_ascii=False))
