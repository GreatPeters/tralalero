import datetime,hashlib,json,pathlib,re,shutil
p=pathlib.Path(r'D:\Tralalero Shooter\Tralalero Shooter D')
out=p/'outputs/chapters45-2026-10-02'
v=out/'final-verification-20261001T212400Z'
def read(path):return json.loads(path.read_text(encoding='utf-8-sig'))
build=read(out/'build-release-final/result.json')
package=read(out/'build-release-final/package-verification.json')
boundary=read(v/'build-boundary-resolution.json')
assert build['result']=='Succeeded' and build['errors']==0 and boundary['outcome']=='accepted' and not boundary['existingContentChanges']
assert package['projectSettingsRestoredByteForByte'] and package['signatureV2Verified']
review=pathlib.Path('chapter45-final-review.md').read_text(encoding='utf-8')
assert 'FINAL CORRECTED PACKAGE ACCEPTANCE' in review,'Independent corrected-package acceptance must be recorded first'
stamp=datetime.datetime.now(datetime.timezone.utc).strftime('%Y-%m-%d %H:%M:%S UTC')
final=(out/'FINAL-VERIFICATION.md').read_text(encoding='utf-8')
for name in ['QUALITY-REPORT.md','HANDOFF-RECONCILIATION.md']:
    source=out/name; archive=out/(source.stem+'-initial'+source.suffix)
    assert not archive.exists(),'Do not overwrite initial handoff archive'
    shutil.copyfile(source,archive)
(out/'QUALITY-REPORT.md').write_text(final+'\n\nInitial implementation, design and asset/capture details are preserved in [the historical report](QUALITY-REPORT-initial.md). Final corrected-state results above supersede its old24-case, partial route-matrix and initial-APK figures.\n',encoding='utf-8')
handoff=f'''# Corrected Chapters 4/5 handoff

{stamp}. Implemented and independently accepted: Chapter 4 leaves the rest stop through a short highway section, follows Jamsil streets toward the shoe-topped tower, and enters the Chapter 5 lobby. Chapter 5 climbs three floors through two six-second lifts and concludes inside the recognizable shoe crown with a three-phase captain and physical offering claim.

All four final ordinary-input routes clear: Jamsil quiet241.475s / risk257.082s; ShoeTower quiet295.958s / risk302.224s. Each starts60HP/8attack, uses actual motion/fire/physics, and restores preferences with zero mismatches. No logged errors or animation warnings. Independent reviewer approved the saved source, route/visual captures and final package.

The final investigation found and repaired a real introduced issue:15 C08 city actors lacked combat/death states. Their visuals now use the existing C07 model with its matching Generic controller/avatar; root gameplay/collider values were preserved. Native actor checks21/21 and saved-scene contracts26/26 pass.

Full EditMode1073/1126 passes,53 failures; all1124 previous test statuses and53 failure signatures unchanged. The previously unclassified20 now have source/mesh diagnosis, with limits retained. This is not an all-green suite. Directed final lifecycle35/35, cityphysics9/9, tower/captainphysics25/25 and mid-lift disposal9/9 pass. Existing native campaign57/57 and workshop36/36 evidence remains linked in the historical report.

Final review APK: [TralaleroShooter-20261002-Chapters45-release-final.apk](../../Builds/Android/TralaleroShooter-20261002-Chapters45-release-final.apk), {package['bytes']:,}bytes. ARM64 non-development, local debug signature for review;0 build errors/{build['warnings']} warnings. Five scenes, full ZIP CRC and actual Android v2 signature verified. SHA256 `{package['sha256']}`. Previous APK retained; no install, distribution, push or cloud backup.

Shared test side effects were independently closed before building: transient font cache exactly restored, existing dirty materials unchanged against a real checkpoint, and generated prefab semantically equal with no demonstrated live binding loss. Exact immediate pre-suite prefab bytes were unavailable; this temporal evidence limit is explicit. All {boundary['files']:,} preexisting product files have identical pre/post-build SHA256; two expected generated Addressables linker files were independently approved ({boundary['totalAfterBuild']:,} files total). Original signing/build settings restored byte-for-byte.

No phone is connected. S22 FPS/thermal/memory and human fun/touch evaluation remain unmeasured. Automated completion and staged visual review establish scope-specific behavior/readability, not human enjoyment. Qwen-specific loading/inference was stopped; unrelated web-search companion processes and Unity/Codex/WAN work were preserved. Exact releasedRAM/VRAM was not measured.

Authoritative evidence: [final quality report](FINAL-VERIFICATION.md), [independent approval](qa/independent-final-review.md), [live progress log](../../docs/exec-plans/active/chapters45-progress-2026-10-02.md). Branch `fix/s22-performance-visual-polish-20261001`, HEAD `295a18cab`, uncommitted project changes retained. Earlier handoff is [archived](HANDOFF-RECONCILIATION-initial.md).
'''
(out/'HANDOFF-RECONCILIATION.md').write_text(handoff,encoding='utf-8')
shutil.copyfile('chapter45-final-review.md',out/'qa/independent-final-review.md')
quality=f'''## Chapters 4 and 5 - 2026-10-02 (corrected final state)

Jamsil and ShoeTower are implemented, connected to the campaign/upgrades/menu/build, and independently approved against the actual fourth intro and native captures. All four final ordinary-input routes clear with no errors or animation warnings; chapter-specific native tests26/26 and actor animation checks21/21 pass. Final directed lifecycle/physics/disposal checks all pass and user preferences restore exactly.

The final full suite is1073/1126, with53 unchanged failure signatures; the previously unclassified20 are diagnosed with explicit limits. A real newly introduced C08 combat-animation issue was fixed before the four accepted routes. This does not establish that all broad failures predate the task or certify human enjoyment.

Corrected ARM64 review APK: {package['bytes']/1e6:.1f}MB,0 errors,{build['warnings']} warnings; five scenes, CRC and v2 debug signature verified. All {boundary['files']:,} preexisting product hashes are unchanged across the build, with two approved generated linker-file additions. Shared test serialization side effects and the missing exact immediate pre-suite prefab snapshot are documented. S22/device and human playtesting remain unavailable.

[Final evidence report](../outputs/chapters45-2026-10-02/FINAL-VERIFICATION.md) | [Independent final approval](../outputs/chapters45-2026-10-02/qa/independent-final-review.md) | [Live decisions](exec-plans/active/chapters45-progress-2026-10-02.md)

'''
q=p/'docs/QUALITY_SCORE.md';s=q.read_text(encoding='utf-8-sig')
s,n=re.subn(r'(?ms)^## Chapters 4 and 5[^\n]*\n.*?(?=^## |\Z)',lambda m:quality,s,count=1);assert n==1
q.write_text(s,encoding='utf-8')
reliability=f'''## Chapters 4/5 final validation - 2026-10-02

Corrected saved-state route matrix4/4, Chapter45 tests26/26, actual actor animation21/21, lifecycle35/35, cityhazard9/9, towerhazard25/25 and lift-disposal9/9 pass. Previously accepted campaign57/57 and workshop36/36 remain separately documented. Staged callbacks and held-position fixtures are not represented as ordinary gameplay or human testing. Every preference snapshot restores with zero mismatches.

Full EditMode remains53 failures (1073/1126pass): zero new failures or changed signatures relative to the1124-case receipt. All20 previously unclassified failures have current source/mesh diagnosis without an unsupported historical-baseline claim. C08 city actor animation was a real new issue and was corrected with matching C07 visuals before the final route matrix.

Final ARM64 debug-signed review APK succeeds with0errors/{build['warnings']}warnings; complete package verification and all{boundary['files']:,}preexisting product hashes pass, plus two independently approved generated linker files. Broad tests regenerate shared assets; the preservation investigation, restored dynamic font and bounded prior dirty-checkpoint proof are retained. Do not rerun those generators in a dirty worktree without exact byte backups. S22 FPS/thermal and human enjoyment remain unverified. See [final evidence](../outputs/chapters45-2026-10-02/FINAL-VERIFICATION.md).
'''
r=p/'docs/RELIABILITY.md';s=r.read_text(encoding='utf-8-sig')
s,n=re.subn(r'(?ms)^## Chapters 4/5 final validation[^\n]*\n.*?(?=^## |\Z)',lambda m:reliability,s,count=1);assert n==1
r.write_text(s,encoding='utf-8')
for name in ['write-final-chapter-verification-report.py','finalize-corrected-chapter-docs.py']:
    shutil.copyfile(name,p/'tools'/name)
with (p/'docs/exec-plans/active/chapters45-progress-2026-10-02.md').open('a',encoding='utf-8') as f:
    f.write(f'\n\n## {stamp} - Corrected final delivery verified\n- Final checker approved all four saved-code ordinary routes, actual visual captures,26/26 chapter tests,21/21 actor animation and final shared native checks. Full suite1073/1126,53same failure signatures; current causes of the remaining20 are documented.\n- New APK: Builds/Android/TralaleroShooter-20261002-Chapters45-release-final.apk; {package["bytes"]:,}bytes, ARM64 non-development, local debug-signed review. Build0errors/{build["warnings"]}warnings; ZIPCRC/five scenes/Androidv2 signature all verified. SHA256{package["sha256"]}.\n- All {boundary["files"]:,} preexisting product files have unchanged pre/post-build SHA256; two approved generated Addressables linker files were added; original settings restored exactly. EarlierAPK and reports are preserved. No commit/push/install/distribution/cloudbackup.\n- Authoritative current reports: outputs/chapters45-2026-10-02/FINAL-VERIFICATION.md, QUALITY-REPORT.md, HANDOFF-RECONCILIATION.md and qa/independent-final-review.md.\n- Remaining limits: disconnected S22, human fun/touch, phoneFPS/thermal,53broad failures and the explicitly bounded immediate pre-suite prefab-byte evidence gap. No unresolved scoped source/visual correction or approval blocker remains.\n')
print(json.dumps({'report':str(out/'FINAL-VERIFICATION.md'),'apk':package['path'],'allPreexistingProductHashesUnchanged':True,'reviewerApproved':True}))
