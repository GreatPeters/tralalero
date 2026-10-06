- 2026-10-03: 도메인 재로드 없는 Editor Play의 Visual Scripting 구독 누적은 [좁은 Editor 보완과 실제 재진입 검증](solutions/runtime-errors/deduplicate-visual-scripting-editor-play-subscriptions-2026-10-03.md)으로 처리했다. Unity/패키지 업그레이드 때 backing-field/callback 호환성 재검증 필요.

## Thin trigger spawn overlap and Play-time reload — cycle17

A player muzzle inside a Chapter45 panel could step through its thin trigger before contact. BulletScript now resolves an existing admitted eligible overlap before positive movement, preserving shield/deck/pooling rules. Native before/after and30 directed guard cases verify the bounded correction. Separately, a delayed source compile after Play entry invalidated an ephemeral QA callback; finish compilation in Edit and verify before starting. Wait for restoration to finish before another runner; preference guards correctly rejected premature overlap. [Evidence](reviews/chapter45-validation-cycle17-2026-10-04.md).

## Small world prop labels must be checked in actual Play · 2026-10-04

Cycle16 popcorn TMP labels passed the static gameplay-component contract check but rendered oversized during actual Play; captured fontSize was-99. No root cause in shared fonts is claimed. Four new label objects were retained inactive and replaced with a small native printed texture on fitted paper panels. Actual same-frame and13second Play retests passed. Original shared font/prefab hashes remained unchanged. Static component preservation alone does not certify render scale. [Failure, correction and capture limits](reviews/chapter45-content-cycle16-2026-10-04.md).

# Reliability

## Account connection, saving and deletion — 2026-10-06

- Missing PGS game ID or an unsupported platform leaves local play available. Login/read timeouts release the start gate after 20 seconds; late callbacks are rejected by operation tokens. Save/delete requests retain their gate until their native task completes, to prevent an in-flight write from racing deletion.
- Reconcile at a real lobby, with debounced local backups and 60-second failed-sync retry. Offline SDK commit success can represent a locally accepted save; it is not a demonstrated server acknowledgement. Conflict choices replace entire saves, never add currencies or purchase levels.
- Delete requires connectivity and non-stale refreshed metadata, journals intent before networking, and clears local progress only after deletion/absence verification. Failures retain preferences and disable re-upload. On restart the journal directs the UI to deletion retry.
- The first UI fixture's ephemeral JsonUtility snapshot was `{}`. Known fake account keys/cache were removed and wallet values remained 33,031/30, but exact restoration of all original keys is not provable from it. The corrected round-tripped snapshot verified all 89 captured keys after the second fixture; its baseline is explicitly later. [Prevention](solutions/workflow-issues/round-trip-check-preference-snapshots-before-unity-play-2026-10-06.md).

## Detailed mall lifecycle and current failures — 2026-10-03 11:51 UTC

The current1–7F+B1 plan supersedes older route assertions. Lifecycle fixtures derive deck IDs and transfer duration from authored links and pass `BeginLift(lift,destinationSegment)` explicitly. They use the existing validated preference snapshot reference, preserve key absence and restore editor state. Ordinary input runs and directed position/death/transaction fixtures are labeled separately.

New warning/HUD strings previously mutated the shared GmarketHarbor font. Preserve the changed file before recovery, bind all558chapter text components to owned static fonts, then restore/verify the approved208shared-file hashes. Before every focused run, record current `PT_ResourcesCleanup` existence/value and restore it; the historical unknown value remains unknown. A preflight mismatch stops tests until inspected. Never claim the old1135-test result proves the current scene.

Real campaign transitions exposed four `incompatible keyword space` assertions between `ArchitecturalGlazingFinish` andURP/Lit. Its complete URP pass now uses `Fallback Off`; forward glazing remains supported. Original shader and failed run are preserved. A fresh actual transition/reward fixture passed57/57 with zero errors and exact preference restoration. [Fallback semantics](https://docs.unity3d.com/Manual/SL-Fallback.html); the concrete diagnosis is supported by local before/after receipts, not by assuming every Unity keyword assertion has this cause.

Pipeline `recompile_status` may return a serialized JSON string. Decode it before reading `status`; a tool-format error does not imply the underlying compile or scene mutation failed. After a partial runner failure, inspect content hashes and saved receipts and resume only the unfinished step. Do not resubmit scene installers blindly.

Evidence: `outputs/chapter45-detailed-design-2026-10-03/`, especially `hud-font-isolation-restoration.json`, `glazing-fallback-fix.json`, current directed and ordinary receipts. APK/mobile FPS,52historical broad-suite failures, paid role-body art and final art quality remain separate outstanding scopes.


## Ch4/Ch5 environment art and Meshy towel - 2026-10-03

Current saved environment and towel pass four normal routes and focused/directed regression. Shared 208 asset hashes match; 210 local recovery copies are valid. No existing product paths were removed. Fine tower glazing is distance-gated. A Pipeline 30-second response timeout can leave authoring running: verify completion receipt and idle saved-scene state before the next action; never duplicate the installer. [Lessons](solutions/workflow-issues/validate-native-art-against-game-camera-and-safe-mesh-surgery-2026-10-03.md). Historical PT_ResourcesCleanup remains unknown; current before/after values are recorded.

## Current retail production — 2026-10-03

- Local task-owned TRELLIS8189 completed real generation; no install, new authentication or paid call. The original GUI/runtime settings were left unchanged. Resource bounds and exact prompt IDs are recorded. A task-owned stalled job was stopped only after confirming no other queue item; it must not be blindly resubmitted.
- Generated mesh validation requires actual reimport and four views. Low polygon counts, closed-edge statistics or a good front image alone did not establish usable towels. Delivery candidates with holes or bad shading were excluded.
- Copied combined decorative meshes remove only twelve verified cube triangles per placeholder. Original assets remain intact. Native receipt files are authoritative after a Pipeline30-second response timeout; both delayed operations completed and were verified rather than duplicated.
- Final test before/after records preserve the currently observed PT_ResourcesCleanup state (Editor present/false; Player absent). Its historical value before earlier broad tests remains unknown.
- [Current report and operational evidence](reviews/chapter45-model-production-2026-10-03.md).


## Generation access diagnosis boundaries — 2026-10-02

Treat refused local status connections, process creation denied, missing client Python dependencies and forbidden external sockets as separate failures. Existing local TRELLIS and Meshy paths exist even when no callable connector is exposed. Do not claim invalid Meshy credentials or zero balance without an HTTP response. No elevation/alternate route may bypass a specifically denied generator path; report the exact time, target and minimum user action. [Recorded diagnosis](reviews/department-generation-diagnosis-2026-10-02.md). Warm-light appearance claims require same-camera geometry-preserving ON/OFF evidence, not emissive material values alone.

## Chapters 4–5 operational boundaries

- Department-store QA copies use the current verified-snapshot-reference protocol, not the obsolete assumption that each Prepare writes `before-prefs.tsv`. `test_status` may return JSON encoded in a string; parse it and wait for completion before restoring state. The final focused run records current `EditorPrefs/PT_ResourcesCleanup=false`; this does not recover an unknown historical value.
- The department-store installer checks protected gameplay both before save and after reopen. Do not rerun the initial chapter or street builder over it. Mid-install interruption requires inspecting per-scene progress/receipt files before retry. New models still require a working authorized TRELLIS/Meshy path; native primitives are not evidence of AI generation.
- The initial chapter builder creates the base scenes; later fold, crown, hazard-fit and polish steps are not interchangeable. Preserve current scene backups and receipts before rerunning any authoring tool. `refine-chapters45.cs` is a one-time migration and must not be blindly repeated.
- `run_script` can finish inside Unity after the CLI response timeout. Check the native receipt, editor state and output timestamps before deciding whether to retry a mutation.
- Run native tests and Play Mode serially with imports. Importing runtime scripts during a test or play run can reload its domain and invalidate the evidence.
- `chapters45-playtest.cs` requires preference restoration after every stopped run. Check `restored.json` for zero mismatches before preparing another. Baseline stats and ordinary inputs are recorded separately from directed lifecycle fixtures.
- Lift ownership and projectile deck checks must be retested after movement, helper or weapon changes. A pause must freeze warning/transfer clocks and prevent queued trigger damage; an overlapping goal may claim once after resume.
- Editor draw counts do not establish Galaxy S22 frame rate or thermals. Device evidence remains unverified whenever ADB has no connected authorized device.

## Startup keyword-state assertions — 2026-10-02

- Unity 6000.2.6f1 reproduced `State comes from an incompatible keyword space` / `Keyword state size mismatch (71 vs 75 keywords)` while constructing a fresh legacy FlatKit material. Local pass declarations fix the ownership mismatch; do not reintroduce cross-shader `UsePass` merely because SRP Batcher compatibility remains 0. Run `LegacyOutlineShaderTests` and `python tools/sync-legacy-outline-passes.py --check` after FlatKit updates.
- The selected original opening was High-profile H.264. Its normalized Baseline replacement retains 936 frames/39 seconds, the original fourth scene and the same asset GUID. If replacing the movie again, validate actual startup/seek warnings in addition to duration; preserve the newest source before re-encoding. `tools/encode-startup-opening-baseline.py` rejects a source changed since its dated snapshot.
- Firebase's Editor/desktop delivery-unavailable message is an informational `Debug.Log` guarded by the platform conditional, not a Firebase initialization failure. Do not disable mobile analytics or hide shader assertions to clean the console.
- This session's fresh preference snapshot is `outputs/startup-errors-2026-10-02/preferences-before.tsv` (40,228 coins/30 jewels). Older snapshots in the sections below are historical and must not be used for this correction.

[Cause, fix and verification](solutions/runtime-errors/own-flatkit-passes-to-avoid-keyword-space-assertions-2026-10-02.md).

## S22 implementation — 2026-10-01

- Restore the current task's snapshot with `S22PolishVerification`, not an older audit snapshot. No phone is connected; Editor measurements are not device or thermal measurements.
- Movie UI keeps the camera enabled and temporarily uses a zero world culling mask. Restore its mask, clear flags and background after EOF, skip, replay, failure and disable. Inspect a visible movie frame in addition to decoder logs. `ScreenCapture.CaptureScreenshot` is deferred: skipping in the same coroutine frame records the lobby instead.
- Disable temporary preview cameras before releasing their targets. A stale solid-color backbuffer after offscreen experiments required an Editor restart; domain reload was insufficient. Reject those captures as evidence.
- Aggressive LOD2 candidates broke roof/UV structure and were removed from all installed groups. Only original and reviewed LOD1 remain. Prop colors require correct FBX color-space conversion, and triangle winding must be inspected independently of counts.
- A full historical market rebuild can overwrite later presentation patches. Reapply the S22 scoped patch tools using their receipts/backups. Do not blindly rerun the guarded presentation installer, which removes duplicate roots and varies actor scales.

The diagnostic section below is the pre-fix state. [Applied report](reviews/s22-improvements-applied-2026-10-01.md), [implementation lesson](solutions/performance-issues/cache-occluder-group-bounds-and-verify-native-output-2026-10-01.md).

## S22 audit measurement limits - 2026-10-01

The current Revamp camera occlusion traverses1,310groups per rendered update and has a demonstrated large Editor CPU cost. The report and repeatable tools are in [the S22 audit](reviews/s22-performance-visual-audit-2026-10-01.md); the mitigation has not been implemented. Preserve the user's requested transparency behavior when narrowing candidates.

Do not infer current APK scenes from EditorBuildSettings: the2026-09-30build script supplies its own four-scene list. Keep packed asset size, APK compressed size and runtime RAM distinct. Native camera-only screenshots may omit overlay UI; invalid immediate skinned-mesh captures must not be diagnosed as gameplay deformations. [Evidence-handling lesson](solutions/workflow-issues/qualify-build-and-capture-evidence-in-mobile-audits-2026-10-01.md).

## Noryangjin feedback pass — 2026-09-29

- Snapshot PlayerPrefs at the start of every session that runs Play bots (`ChapterPlaytestPreferences.SnapshotAt`). Restoring an older snapshot would erase the user's own play between sessions; this pass left coin/jewel as found because no fresh snapshot existed.
- `codex exec` 0.149 fails with the default model ("requires a newer version of Codex"); pass `-m gpt-5.5` for concept images.
- Scene builders that attach a body under an existing object must delete the body from the previous install first, or reinstalls stack hidden copies (the N20 boss did).
- The stationary capture tool disables enemies; add `#keep-enemies` to `tmp/claude-probe/shots.txt` to frame the boss.

## Noryangjin grade separation and editor reload — 2026-09-28

- This project enters Play without a domain reload. After a script recompile Unity restores private UnityEngine.Object fields but not plain C# objects, so `BonusPadVisual` kept its part renderers with a null `MaterialPropertyBlock` and threw every frame. Error Pause then froze Play-mode capture tools. The block is now recreated lazily. Treat any "first Play after recompile" exception flood as this class of bug first.
- Exception stack traces are off in this project (`StackTraceLogType.None`), so the console and editor log show bare `NullReferenceException`. For diagnosis, set `Application.SetStackTraceLogType(LogType.Exception, StackTraceLogType.ScriptOnly)` from a `run_script`, hook `Application.logMessageReceived`, then set it back to `None`.
- The running editor writes its log to the `-logFile` path on its command line, not `%LOCALAPPDATA%\Unity\Editor\Editor.log`.
- `run_script` of the installer fails with a network error while a recompile is still running. Poll `recompile_status` until `completed`, then install; confirm `crossing lift 9m` in the report before playtesting.
- The market lift is a post-process (`NoryangjinCrossingLiftBuilder`) over the finished layout. Any new builder that places market objects must run before it inside `InstallNoryangjinRevamp.Main`, or its objects stay at ground level inside the raised span.

## Noryangjin feedback v3 verification

Before official Pipeline `run_tests`, require a saved clean scene and await the test result before starting a builder. Temporary importer objects can dirty the scene and the Unity test runner may open a hidden save dialog. Preserve recovery backups before restarting a confirmed task-owned D Editor. A30s Pipeline timeout can occur after a builder actually saved its scene; inspect the scene and task-specific audit before repeating a mutation. Read nested command success, not only the transport envelope.

The feedback scene's combined road renderer split preserves each original collision mesh and uses shared generated partitions. Rebuild only through the installer; do not replace the source collider with its floor-only visual mesh. See [native occlusion diagnosis](solutions/ui-bugs/verify-native-occlusion-after-splitting-road-renderers-2026-09-28.md).

## Noryangjin debug review protocol — 2026-09-28

The user explicitly requests the9999 and fast-lateral test selectors ON. Use `NoryangjinDebugReview.Prepare` and `Finish`; `Finish` restores the save snapshot, then intentionally selects stage1,1x speed and both toggles ON. The older normal-growth reproduction entry points intentionally turn overrides off and must not be mistaken for this requested protocol. These toggles are session-only and apply at run preparation, not authored default stats.

Automated steering must use active collider bounds instead of a prop pivot. A fallen lamp's root and collision center were on different sides of the route. Aim along an enemy's actual lane when testing shooting: a center-only bot can miss an intentional pair of enemies. Preserve unsuccessful attempts and distinguish driver limitations from product faults. See `outputs/noryangjin-debug-review-2026-09-28/`.

## Noryangjin interior V2 generation and editor rebuilding — 2026-09-28

- Meshy generation uses a durable paid-task ledger. Resume known IDs; never automatically resubmit unknown/failed paid creation requests. N09–N14 consumed214credits. New imports use existing folder conventions but do not replace earlier IDs.
- Use the official Unity CLI installer, then check inner `data.result.success`. Rebuilding the large scene emitted an Editor-only null exception followed by three URP `ZBinningJob` NativeArray job-conflict errors with empty stacks. Skipping cached reflection-cubemap imports did not eliminate them. The saved scene and subsequent native Play runs worked and showed no new runtime exceptions. Do not claim this engine/editor issue is fixed; inspect post-install console and Play separately.
- Old combined road meshes contain very tall posts. Disable only their renderers during indoor travel, preserve colliders, restore on exit/disable. Near-camera hanging displays must be suppressed as groups; the ordinary 40% scenery fade is inadequate for a screen next to the camera.
- Blender evidence renders use absolute output paths and `--python-exit-code 1`; the Blender4.4 `wm.fbx_import` operator proxy is not proof that the operator exists. The working FBX entry is `bpy.ops.import_scene.fbx`.
- Report: `outputs/noryangjin-interior-v2-2026-09-28/README.md`; related learning: `docs/solutions/workflow-issues/validate-reference-interiors-and-retargeted-poses-in-native-camera-2026-09-28.md`.

## Noryangjin branch and event isolation — 2026-09-28

Spatial event triggers are not enough: a triggered periodic hose must still check facing, height and lateral/along bounds on every damage tick. Otherwise a later perpendicular road at the same projected station can be hit. Hose completion is latched after passing the stretch, spin drain clamps to its remaining active duration, and wave resets preserve the authored crest scale. Large incident spacing must outlast the preceding quiet window; the original 50m truck/toss gap skipped the toss and was increased to 96m with a 7s reservation.

Legacy road modules combine decks and tall rails. Clearing nearby `Props` by root position left rails intersecting the new pier and old shops in the curved approach. The installer checks renderer bounds against sampled bypass corridors; `NoryangjinCrossingVisibility` hides only intersecting old road renderers during outside travel, then restores their recorded state. Walkable colliders stay under the original road root and original SR18 remains untouched.

A native `run_script` response can have outer success while its inner compilation result failed. After adding runtime types, wait for `recompile_status` completion and inspect `data.result.success` before treating installation as complete. Use saved TMP outline material assets in editor installers; setting TMP's per-instance outline properties generated four edit-mode material leak errors. The corrected installer reports no console errors.

QA records must inspect debug flags throughout a run. Two early Noryangjin runs had the 9999 option enabled around the fork and are explicitly excluded from difficulty evidence. Later normal-growth runs clear both routes without that override. Route-scoped mechanic fixtures must select the relevant route before invoking an event; an initial unselected fixture incorrectly failed the cat/shutter checks. See `outputs/noryangjin-revamp-fix-2026-09-28/`.

## Native Highway presentation updates — 2026-09-27

Repeated procedural Mesh edits must refresh native channels/render buffers, not only serialize CPU state. Keep hidden canopy meshes in a separate combined group so runtime roof toggles do not enable the pre-combination source renderers. Overlay Canvas sorting order does not put graphics behind a camera-space HUD; reserve its screen region explicitly and verify native screenshots. See solutions/ui-bugs/refresh-native-mesh-buffers-and-reserve-camera-hud-space-2026-09-27.md. Pipeline run_script static fields do not survive separate compiled invocations; use SessionState for small cross-call capture state. Reload the saved owned scene after tests if the test run dirtied it; do not save test-generated scene changes.

## Compulsory Highway traffic and physical exit rewards — 2026-09-27

Stage progress gaps do not prove separation on a curved lane. Admit full rows atomically, keep their member motion coupled and compare their proposed collider poses in world space before moving. Tests must include the coordinates between lane centres. QA must retain already-destroyed slots; choosing only living enemies can steer a bot out of a safe opening.

Vehicle health labels use the gameplay camera, minimum projected size and occlusion tests. The predictive contact-warning component must not render on vehicles, even if attached accidentally. Actual damage is logged with the overlapping vehicle IDs. Exit pickups require a living player in the physical trigger and consume both choices after one award; no bonus is granted merely by reaching a progress value. Rush speed/effects are scoped to the open segment and reset on merge/disable.

Check inner Pipeline success before dependent operations and verify clean Edit Mode after test execution. One later asynchronous workbook run stalled; it was cancelled, and the same15existing NUnit methods were invoked directly and passed. This is separately recorded, not counted as an asynchronous-runner success. All task-wide preferences are restored against the original snapshot at completion. See solutions/runtime-errors/use-world-clearance-and-preserve-opened-slots-in-highway-combat-2026-09-27.md.

## Highway screenshot follow-up — 2026-09-27

EnemyEventController's runtime-reference resolution also ran on disabled vehicle controllers and added the pedestrian75mvisual culler. This left distant HP labels visible while car bodies had forceRenderingOff=true. Vehicle actors now skip that bootstrap (including its pedestrian contact warning); HighwayChapter2Controller owns vehicle visibility. See solutions/runtime-errors/keep-highway-vehicle-visibility-out-of-pedestrian-culling-2026-09-27.md.

Do not deform every mesh vertex near an accident car's nose: the same mesh can contain wheels, so a dent operator also distorts tire contact. Reuse the intact silhouette and communicate the accident with pose/lamps/smoke/triangle. Road forks share asphalt at their entrance; independently drawing every line from both carriageways creates crossings. Clip branch paint to the region outside the main carriageway and hide the obsolete main outer edge while the roads overlap. Left-green/right-red guidance is explicitly allowed by the latest user request.

Earlier vehicle visibility must account for motion: advancing spawn time alone changes difficulty and collision timing. Move its starting station by speed×leadSeconds while advancing player-progress activation by runSpeed×leadSeconds. Preserve original placement IDs and HP/rewards. Audit both the data equation and native admission/visibility. Ground-pivot correctness alone does not establish tire clearance on the curved road; compare transformed low mesh vertices against authored road colliders. The follow-up records before/after sweeps and native captures in outputs/highway-visual-fixes-2026-09-27.

## Chapter2 vehicle combat and verification boundaries — 2026-09-27

Vehicle-specific hooks must remain conditional so other chapters retain their contact/reward behavior. A shield needs the full incoming car health before PlayerScript clamps damage to remaining player HP; qualifying the shield after that clamp fails at low health. The actual default projectile is BulletKind.Water, so restricting chain/shatter to Bomb makes both rewards inert. Directed runtime probes cover the actual default shot, low-health shield, fatal-hole exclusion, single consumption, magnet pickup, tanker chains and effect expiry.

Match traffic clearance to the live player capsule (observed1.38mwide), not a guessed visual radius. Automated driving must commit to an accident's weak lane and stay in the opened corridor until past its queue. It must also account for projectile flight time and its finite range. Editor callbacks can see a zero movement delta before the next game update; that does not prove an approaching car stopped. Use a conservative configured closing-speed bound in the driver.

The offset branch can compress world distance relative to the route-progress parameter. Do not compare a world-space collider half-length directly with a progress-distance difference for predictive driving. Sample both future world poses and project the relative vector into the car's frame. Include PlayerMove's Lerp smoothing; raw input delta is not the immediate displacement.

PlayerPrefs in this Windows Editor are registry values. Preserve kinds and exact value bits: Mono may return an unsigned DWORD as Int64. The QA snapshot serializes every value canonically, restores known cached keys and native values, and byte-compares before/after. Keep these files outside the public report folder. Restore the exact playModeStartScene as well as the map-tool selector.

Native appearance matters: fitting a log by width makes a metre-wide log many metres long; fit its long axis. FlatKit's inherited white shadow/rim colors can wash out untextured native props even when BaseColor is correct. Compare captures with the reference, including vehicle noses/tails. For curved scenery, world AABBs are only a broad phase; transform corridor samples into each renderer's local bounds before treating an overlap as real.

## Three-lane Highway lifecycle and collision boundaries — 2026-09-26

`EncounterPlacementController` re-applies workbook enable flags at run start. An authored roadblock disabled in Edit Mode may therefore be active during play. Incoming traffic checks live hazard/collider state before accepting its path; a saved active flag alone is not a corridor test. Centre traffic is also excluded from the actual forced combat passage at its predicted meeting station. Car spacing must use measured inactive-template geometry, not a three-metre fallback for a long vehicle's first spawn.

Highway's log trucks use an opt-in head-on mode and unregister their blocker on cleanup/disable. Rear-chase and adjacent ambient controllers stay disabled in the saved scene. The road and hazard update is HighWay-only; preserve the existing RestStop serialized directions and workbook.

Pipeline `run_script` may time out while a large native mesh import/install continues. Inspect its retained receipt and live state; do not rerun an authoring mutation blindly. Reopen the saved scene and verify mesh references/configuration. The map-tool's play-start scene can differ from the currently open Editor scene; select HighWay for QA and restore the prior choice afterward.

Traffic suppression/clearance use `Time.time` deadlines. With domain reload disabled, reset them on subsystem registration and new-run initialization, or a later Play session can spawn no cars at all. Explicitly repeat Play without recompilation to exercise this boundary. A centre log truck also needs the left corridor clear of static props; sixteen roadblocks are spaced away from event windows. [Retained failures and verification guidance](solutions/workflow-issues/validate-highway-traffic-across-play-sessions-and-live-hazards-2026-09-26.md).

## Combat Harness play-entry lifecycle — 2026-09-26

Do not create runtime scene objects from `InitializeOnEnterPlayMode`: HighWay's start-scene transition with reloads disabled left `Combat Harness` in scene cleanup. Its existing `AfterSceneLoad` runtime bootstrap owns automatic creation. Verify both a single live harness and zero objects after stopping; see [diagnosis and two-cycle verification](solutions/runtime-errors/create-combat-harness-after-scene-setup-2026-09-26.md).

## Korean highway pass — 2026-09-25

- Parenting a new visual under a non-uniformly scaled instance and rotating it skews the mesh (oversized oncoming cars). Place replacements beside the old instance and size them from `Mesh.bounds`; `Renderer.bounds` is empty for inactive objects.
- Cloning an authored roadblock for an enemy ability also clones its instant-death contact. Strip hazard components from runtime clones.
- Oncoming traffic still kills on contact by design; route-coverage runs disable `HighwayOncomingTraffic` as well as `HighwayHazard`.

## Meshy generation and Korean rest-stop install — 2026-09-25

- Meshy ledger (`outputs/meshy-reststop-2026-09-25/ledger.json`) is shared by parallel workers through `ledger.lock`; each process overrides only rows it created or advanced. Stopping a worker mid-step can leave a server task without a local id or file: run `tools/meshy_ledger_repair.py` then `tools/meshy_fetch_missing.py` before restarting, and restart only pipelines that resume pending tasks. 41 credits were duplicated before these guards existed.
- Meshy action FBX files carry per-file bone scale keys (idle about 17% taller than walk); the character importer strips scale curves. Meshy vehicles come out nose toward -Z. A remesh has new UVs and its own textures; re-extract and pick the base-color map, never the largest file (it can be the normal map).
- A nested body prefab silently refuses re-parenting of its hand props; unpack nested instances before moving props, or they are destroyed with the old body.
- `NoryangjinCameraOcclusion` only fades explicit scenery to 40%; a full roof needs `RestStopBuildingVisibility`. The holdout disables that occlusion and hides only its own `overheadOccluders`.
- Existing crossing/toll hazards still kill on contact. Route-coverage playtests (`tools/reststop-korean-playtest.cs`, endurance) disable their colliders and components; that run is not a difficulty result.

## Rest-stop production import and scene preservation — 2026-09-25

Keep FBX axis conversion below neutral Fit/Orientation parents; nonuniform scaling must use the axes in which bounds were measured. Preserve GLB material slots and intended alpha/normal connections. The installed scenes contain new visual children and replacement combat Bodies; old scene rebuilders can erase those changes. Validate saved instances and ranged ownership after reopening. Do not treat a scenery group's origin as its geometric center, or a successful outer Pipeline response as a successful script. Reproduction scripts, before-scene backups and known unchanged legacy test failures: `../map-concepts/reststop-scene-integration-2026-09-25/README.md`.

## Damage visibility and campaign calibration — 2026-09-23

A new hit must not consume frame time that elapsed before it was received. Player damage notices skip their receipt frame and cap presentation decay during hitches; enemy popup saturation cannot replace the reserved player slot. Ordinary healing and HP bonuses cannot revive a committed death, and a zero-health finish callback cannot grant a clear. See `docs/solutions/ui-bugs/keep-player-damage-visible-through-hitches-and-recovery-2026-09-23.md`.

Curved-road projectiles must follow their own road progress and height rather than only copying the owner's yaw. The automatic test driver must aim at road-relative target lanes after this change; tangent-space aiming can falsely suggest bad combat balance. Stationary defense is a separate free-aim mode. Compare earned wallets/purchases and actual chapter-completion state, not just a stopped game clock.

RestStop pair alignment must project onto the canonical polyline, never use the existing left actor as the center. Otherwise repeated installers shift actors out of the shootable road. Exclude hazards on laterally remote parallel roads from QA steering before predicting arrival. See [placement and driver diagnosis](solutions/workflow-issues/separate-road-placement-and-driver-errors-from-campaign-balance-2026-09-23.md).

RestStop motion/body sources and native fitted tools are separate. Keep the source prefabs and saved placements in sync with `tools/install-reststop-campaign-motion.cs`; older asset generators may recreate their previous launch attachments. Workbook preparation seconds retime the attack to its authored strike phase. Retain the current normalized strike setting when changing clips (Coffee0.5, rebuilt highway roles0.46).

Campaign testing temporarily used540×1170 (index26). Final verification restored index23/1080×2340 and all77original preference records; see `map-concepts/campaign-balance-2026-09-23/final-state.json`. Keep the same snapshot/restore discipline for reruns. Copied earlier chapter checkpoints are reused evidence, not additional newly played attempts.

## Highway reconstructed prefab integration — 2026-09-23

Replacing a source prefab Body can leave a new inherited Body beside a scene-added visual and invalidate scene-specific launch references. Importers must clear all old visual rigs, restore projectiles from the role contract and inspect the saved/reopened scene. HighWay currently requires one Animator per50actors and34valid ranged launch bindings. Gate coverage tests must include real patrol movement; static capsule math missed a center gap. Keep the highway post-pose launch queue resettable and separate from the Noryangjin crate owner. Details: `docs/solutions/integration-issues/rebuild-enemy-prefabs-without-stale-scene-rigs-2026-09-23.md`.

## Readability and automated playtest integrity — 2026-09-23

Damage provenance must be recorded before HUD/death notification and remain unchanged after a fatal hit. Classify crossing vehicles and toll barriers separately even though both use `HighwayHazard`. Reconfigure and restore camera-fade material owners when scenes/player bindings change; remove fading structures from static occluders and rebake after authoring. A null additional-occluder reference silently leaves the actual gantry opaque.

Later-chapter repeated deaths can be caused by QA steering. Predict crossing-car position and the open toll lane at arrival; reacting to current positions can turn the bot into the hazard. Preserve failed attempts, label corrected reruns, and never infer enemy balance from instant-kill traffic contacts. A purchased-level test save does not prove the player can naturally afford those levels at chapter entry. Keep original key presence/values and tutorial state, restore in `finally`, and verify the scene is clean in Edit Mode. Editor pauses are excluded from the QA watchdog rather than silently resumed. See `map-concepts/review-fixes-20-runs-2026-09-22/README.md`.

## Mobile visibility and launched attacks — 2026-09-20

Detached Guard/FatMan shots must survive shooter deactivation; register them with run cleanup and bound their own lifetime. Clones from distance-culled templates must clear `forceRenderingOff`. Visual distance gating must leave collision/event roots enabled and complete an active crate stroke before release. The road spatial index assumes authored roads remain static during a run; call `Configure` after a layout change. Exclude camera-hidden geometry from baked static occluders. Workbook edits must preserve unrelated formula caches, not silently apply an importer's recalculation as new combat balance. Evidence: `map-concepts/mobile-feedback-2026-09-20/README.md`.

## Common Bonus feedback — 2026-09-20

WallScript deactivates its lifetime root immediately after applying the reward; the talisman transfer therefore runs in a separate scene-owned visual object. It must never apply a second reward and must cancel on player loss/death/run exit or claim-timestamp reset. Generated glow textures require explicit Single sprite import; a PNG with Multiple mode and no slices yields a null runtime Sprite. Rebuild only from clean Edit Mode. See `map-concepts/common-talisman-applied-2026-09-20/README.md`.

Bonus floor pad (2026-09-27): `RefreshContent` disables every renderer under a Bonus root that is not part of the talisman or `BonusPadVisual`; a new child visual must be added to that exclusion, or the second refresh silently hides it (seen first as "icons lying on the road"). Do not set `TMP_Text.outlineWidth/outlineColor` on bonus captions: in Edit Mode it instances a material per label and fails the talisman tests; use the shared `OutlinedCaption` copy. Pad visibility follows the talisman's active state, because pickup and invalid rolls hide only that object. HighWay random gates keep 214 disabled legacy renderers so `MysteryGate.questions/icons` stay valid; rerunning `InstallHighwayChapter2.Interface` re-applies `MysteryPads`. Random gate outcome is still decided by route distance, not by which of the two pads the player crosses.

## Codex image-click blank panel — 2026-09-19

Codex VS Code 26.908.40401 has a user-reported image-click path that leaves the conversation body blank. A local thumbnail guard is installed on disk, with original HTML backed up; it needs `Developer: Reload Window` before use. Browser fixture checks passed, but native verification is pending. Extension updates may remove it. See `docs/solutions/workflow-issues/protect-codex-image-clicks-without-changing-thread-state-2026-09-19.md` for status and rollback. Preserve session data; an external gallery or a successful fixture is not proof that the native panel is repaired.

## Coastal UI production — 2026-09-16

Theme replacement must set scalable Images to Sliced and preserve entire fasteners/shadows inside fixed borders; inherited Tiled modes repeat decorations. Rebuilt children require rebinding external serialized fields, including chapter-clear rewards. The new all-scene authoring entry rejects dirty/playing scenes and restores the original scene setup. Runtime and state-restoration evidence: `map-concepts/coastal-enamel-ui-2026-09-16/README.md`.

## Combat feedback revision (2026-09-14)

Temporary missile duration is static and must be reset at Player Awake as well as ResetState; a scene reload alone does not clear it. Settled-pole cooldown is player-owned, so overlapping poles/colliders cannot multiply contact damage within one second. Hole death freezes the player's physics body while only the model tumbles, and retry restores its prior kinematic state. Results wait0.9seconds before covering death motion.

The latest focused82tests pass. Two older SR18 snapshot tests still expect3FatMan actors/717props versus the already-authored6/740; see the combat-feedback report. Directed probes must explicitly hold forward speed when inspecting fixed-camera melee, and must capture after a rendered frame rather than pausing immediately after mutating health.

## Mobile menu and audio lifecycle — 2026-09-14

Authoring inactive nested canvases does not prove their eventual sorting: MobileUILayer reapplies it on enable. Scroll caches must compare actual content height after Unity layout resets. ScreenCapture proves overlay composition; a camera-only image does not. Cosmetic previews now use two normal URP frames per change with a single-sample RenderTexture, then idle; dispose clears the stage/target on close. Android confirmation is pending.

GameAudioService bounds effects to12voices, gives warnings/player outcomes priority over gunfire, and pauses/stops audio across focus changes. A background first launch must start loops when focus returns even if no earlier Play occurred. Use a private cosmetic variation counter so audio cannot advance gameplay RNG.

Native tests can block in SaveModifiedSceneTask when prefab/import changes dirty an already-saved scene. The current first MobilePresentationTests invocation timed out after300seconds, with no result; do not treat it as a failed assertion or enqueue duplicates. Windows Computer Use is also unavailable (`native pipe ... os error2`) in this session. Preserve the active Editor and ask for the visible save step when direct control is unavailable.

## Live Android build and USB recovery — 2026-09-14

Use a one-shot Editor update for the phone APK builder when a verified pending delayCall is not serviced. A successful callback registration is not a build result. Restore temporary signing in memory and compare ProjectSettings on disk to its before-image; this Editor left the temporary flag serialized until a guarded byte-exact recovery. An ADB interface can show PnP code0while USB serial retrieval fails with Win32 error31. Physical cable reconnection resolved that condition here; subsequent unauthorized state required phone approval. See `solutions/workflow-issues/schedule-unity-builds-through-one-shot-update-2026-09-13.md`.

## Combat presentation pools — 2026-09-13

Prewarm64 numeric popups from CanvasScript.Start and32 hit effects per prefab from enemy Awake before firing. Saturation recycles the oldest cosmetic entry without suppressing combat or currency. Returning particles must clear old emissions; disabling an enemy returns its attached effects, and destroying the pool also cleans active children outside its own hierarchy. A destroyed enemy root can remove its attached visual, so the next rental repairs that slot. Unit tests explicitly simulate teardown callbacks for Edit Mode-only objects; real scene teardown is a separate runtime check.

The performance probe advances once per Time.frameCount and writes a completion report. Never stop it based only on the start response. Background RuntimePerformanceBudget pacing can target15fps, so Editor frame percentiles cannot certify mobile60fps or be compared across focus states.

## Approved road/hall integration — 2026-09-13

Preserve the fresh task snapshot before any play/test. `apply-approved-road-concepts.cs` refuses repeated initial installs and unsaved/non-Edit authoring. Use the authored `HighwayRoute` sampler in integration checks; the old straight builder points miss the curved road at420m. Include inherited player scale when placing overhead architecture: local cameraY8.364 becomes approximately12.67world metres. Raised roof and grouped fascia/shutter occlusion preserve the actual game camera.

Re-proportion geometry and rest bones together, keep carried props rigid relative to hands, re-ground all six actions and revalidate imported clips. The original rig files require installed Blender5.2.1, not system4.4. Source counter renders must normalize studio illumination for their larger size. Native test-name filtering uses partial strings; a literal pipe-separated class list returned zero tests and was rejected in favour of individual nonzero class runs.

## Road scenery authoring — 2026-09-13

`tools/road-reference-visuals.cs` operates only on saved Edit Mode scenes through official Pipeline. `Prepare` refuses an existing backup directory; initial Apply methods refuse an existing scenery root. Its live helper uses a fresh63-key preference snapshot, upgraded section-entry runs and one movement call per rendered frame. Exit Play Mode before `RestorePreferences`, and verify the exact key-existence/value comparison. Do not rerun older restore or scene-building scripts. A PNG imported as Cubemap must be assigned to a matching sky shader; a null Texture2D load can silently produce gray sky despite successful compilation.

## Road patterns

- Reusable police must call `PrepareSpawnAt` while inactive. Repositioning before ordinary `OnEnable` alone restores the first authored start and can invalidate an otherwise correct four-door schedule.
- Stationary combat owns a movement lock rather than the global running flag. Success/death/disable reset the lock, camera and visual rotation; wave and traffic timers respect gameplay pause. Actual holdout completion and death cleanup were exercised.
- Offset routes require continuous normals as well as supported points. Bake and move through the same curve sampler; check both branches and both lateral extremes.
- Native Edit Mode regression tests can write real PlayerPrefs even when gameplay QA uses an isolated product. Snapshot before tests, compare afterward, and restore confirmed test writes after the last scene-open callback. The road-pattern revision verified all63original keys after restoration.

## Chapter revision verification (2026-09-13)

Original Editor10140 resumed servicing Pipeline without being killed; the exact cause of recovery is unconfirmed. Its live scene and58preference keys were preserved before native authoring. Run tests asynchronously after saving, and inspect actual scene/archive state after a timeout because a started mutation can finish after transport failure. A timeout error can also enable Editor error-pause; inspect pause state before declaring a gameplay stall.

The separate graphics-enabled QA project uses real file copies and isolated company/product preferences with analytics disabled. Use a short path (`tmp/q`) to avoid Windows path limits. Never share writable Asset/Library databases with the original Editor or merge the QA-only bootstrap. Address each Editor by project path through discovery. Native screenshots require a real portrait GameView resolution; camera-only captures omit overlay UI.

Gate automated movement by Time.frameCount. A live measurement found828Editor callbacks across248game frames, so invoking deltaTime-based PlayerMove on each Editor callback multiplied movement. Final progression files must include movement_calls<=game_frames; earlier cohorts are diagnostics. Current tools snapshot actual chapter entry state and distinguish saving from greedy-affordable purchases.

Task-owned generation runs with bounded CPU affinity/thread counts, below-normal priority and a free-memory startup check through `tools/run-limited-generation.py`. TRELLIS and Wan GPU queues run sequentially; diagnostic Blender rendering uses two CPU threads. These controls address background resource contention, not a proven cause of the user's game freeze. Wan latents are retained before diffusion is unloaded and VAE decoding begins, permitting a decode retry without repeating sampling.

The installed ad service initializes MobileAdsEventExecutor before consent, expires cached ads, retries no-fill after30seconds on idle screens, invalidates loads after45seconds and times out consent-update/SDK initialization after60seconds. It rejects superseded callbacks. An open consent form has no reading timeout. Earned may arrive after closed and after the defeat UI is destroyed, so credit is guarded by round/attempt identity rather than UI lifetime. Native Editor placeholder success(+90exactly once) and immediate close(0coins, Continue enabled) passed; physical-device/network behavior remains a separate validation boundary.

Android build guards must match the shared dependency and SDK floor. Ads11.5.0requiresEDM1.2.187and Android API24; an old Firebase exact-version/hash guard rejected the new manager, then the Android manifest merger rejected the oldAPI23floor. Keep the exact archive hash check, update its documented pin, and reject incompatible minimum versions before expensive shader compilation. A native build is scheduled once outside the eval request via delayCall and writes its own result/restoration files.

Startup data cost (2026-09-12): repeated full stylesheet appends made Data.xlsx's style XML27MB and each ExcelDataReader creation about0.8seconds. The source was compacted with effective-format/data preservation and its protected archive regenerated. Sheet graft writers now reuse matching style definitions. Same-Editor first movie frame improved18.078→2.156seconds. Preserve this deduplication path during future workbook updates; see `solutions/performance-issues/deduplicate-workbook-styles-before-gameplay-table-loads-2026-09-12.md`.

Android analytics smoke follow-up (2026-09-12): use a real Android SDK runtime, not Editor stubs, and distinguish SDK logging, upload response, Firebase DebugView receipt and later BigQuery export as separate evidence. The read-only API36 emulator produced a matched38.133-second start/death pair and HTTP204 receipts, independently seen in DebugView. Its MSAA/FlatKit rendering failures mean that result is not a visual/performance certification. Source project settings were restored and the task AVD was stopped; raw logs remain under tmp/analytics-flow-20260912.

## Equipment and chapter revision (2026-09-12)

Body atlas meshes preserve the original rig. Replacement footwear has a matching closed body mesh and a skinned shoe mesh in the original bind space. The imported FBX's current pose is not a neutral fitting frame. The shop restores a neutral pose and CPU-bakes it once per selection for reliable manual rendering; player animation remains skinned. Preview stages deactivate before deferred destruction, and a next-frame render handles first-open allocation.

`AssetDatabase.Refresh` returning does not prove script reload completion. Wait for fresh compiled assemblies before invoking changed authoring code. Detached jobs can disappear across domain reloads; use resulting files and read-back as evidence before retrying. `ScreenCapture.CaptureScreenshot` captures a later frame, so hold UI state until the file exists.

The progression harness seeds and re-rolls through the real encounter reset API before starting. Initial scene rolls previously preceded the seed. Test snapshots for this revision are the57-key file under`tmp/backups/skins-progression-2026-09-12`; older snapshots must not be used to restore this user's state.

Rest-stop construction is a guarded one-shot command; subsequent layout changes use the recorded refinement script. Opening a temporary map-tool palette window can mark work objects dirty even when a saved scene copy has no content difference. Compare that copy before clearing the dirty flag or running a builder. Do not blindly discard an unsaved scene. Generated architecture must be checked for surface tears and its actual front axis; the wayfinding sign required a90-degree visual normalization.

2026-09-11 two-chapter workflow: keep the live scene saved before starting EditMode tests; a pending Save dialog can stall Pipeline. Native render resources such as `MaterialPropertyBlock` must be initialized in Awake/Start, not MonoBehaviour field initializers. Story playback invalidates its RenderTexture before Stop and waits for seek completion before following the video clock. Authoring builders refuse to replace an existing HighWay scene; subsequent work uses the normal map tool or a narrowly guarded refinement method. See the [two-chapter acceptance record](../map-concepts/two-chapter-2026-09-11/README.md) for evidence and recovery details.

Replacing a copied chapter's Roads hierarchy requires explicit rebinding of the player height follower and camera occlusion root. Flat opening movement does not establish correctness on elevated turns. Projectile consumers must receive damage before pool deactivation; retaining the payload alone does not guarantee delivery of the receiver's callback. Seagull's child collider belongs to its parent's Rigidbody so the damage callback reaches ObstacleStats. Directed probes reject paused Play Mode, including an Editor error-pause after a transient Pipeline timeout.

## Presentation and progression follow-up

Pooled bullets retain launch damage through deactivation so the other collision callback can read it. Enemy receivers use that payload, including BoomBar's percentage. Visible SR18 lamps have consistent damage, feedback and projectile consumption. Humanoid ground correction moves only visuals, while carry and look-at preserve readable motion.

The opening blocks the public start handler until dismissed; defeat/clear also reject direct starts. Video allocation is idempotent and textures are released on disable/skip/destroy. Equipment effect keys replace previous values on each run. Purchased levels are re-evaluated against current workbook values on lobby load.

Unity Pipeline may time out during import/archive work while the operation continues. Inspect the resulting scene/file or `GetRuntimeArchiveStatus` before repeating a mutation. During this revision the archive initially failed a Windows replacement and a retry timed out, then read-back confirmed `Current`; no stale archive was accepted.

## Run health, cosmetic purchases and test isolation (2026-09-10)

HP rewards increase current and maximum health together and reset at the next run. Permanent helper percentages produce one helper per unlocked type, never one per percentage point. Money rejects negative spending and upgrade purchases require a live wallet.

Cosmetics keep ownership and equipped item per slot in PlayerPrefs. Purchases are guarded against reentry and owned items never debit again. Appearance changes must not modify the source FBX or collision hierarchy. Preview cameras/materials/render textures are disposed on close; Editor-only preview generators explicitly call `Dispose`. The repeated equip path hides every pending old hat before Unity's deferred destruction.

Before live purchase tests run `tools/noryangjin-ui-test-prefs.cs` Begin and verify a nonempty TSV; Restore only after stopping Play. A JsonUtility snapshot of an ephemeral Pipeline-defined type produced `{}` and was unusable; the current helper rejects incomplete snapshots. Preserve backups rather than overwriting them. See the linked workflow learning in `docs/README.md`.

## SR18 contact and pickup rules (2026-09-10 follow-up)

Eight SR18 authored lamps use `canBeShotDown=false`: auto-fire leaves their contact colliders enabled. The defaulttrue setting retains the earlier disarm/reset behavior elsewhere. Four legacy decorative lamps remain disabled. FatMan entry side offsets are stored as0 so workbook reapplication cannot put their wide bodies back over roadside props.

Bonus pickup uses one timestamp on the player across all walls. Check the2-second interval before claiming a pair or changing wall lifetime; rejected pair contacts must not consume a choice or advance the timestamp. Initialize/reset to negative infinity so a pickup atTime.time0 cannot bypass the same-frame lock. Tests: `BonusWallCooldownTests`, `Sr18ContactPairsTests`; current record: `map-concepts/sr18-contact-pairs-2026-09-10/README.md`.

## SR18 shot-lamp and opening verification (2026-09-10)

Light obstacles disarm on a bullet hit before their animated bounds sweep the road. Reset/disable must cancel the Transform-owned tween and restore the captured original collider states; queued player contacts after disarming must remain harmless. Standing-lamp contact damage still applies. Regression fixture: `ObstacleLampSafetyTests`.

SR18's first three modified bucket groups use12-unit intervals based on measured input speed, and two reward approaches use25 units so labels do not hide the next hole. Use `tools/autodrive-sr18-opening-playtest.cs` with normal health for input/collision checks, then restore test rewards. TestRunner's completed result can precede its cleanup: wait until `IsRunActive=false` before reopening SR18 or entering Play. See `map-concepts/sr18-opening-rhythm-2026-09-10/README.md`.

Open observation: after the final97-second gameplay run reset, two stackless Editor exceptions reported `This cannot be used during play mode` during the same period as prefab PreviewImporter work. The caller is not established. Keep this separate from in-run gameplay failures; retained log excerpts and the verification boundary are in the opening-rhythm record.

## Known Failure Modes
- Pooled projectile returns must be idempotent before deactivation clears cached ownership. Regression coverage is in `ProjectilePoolLifetimeTests`; inspect full continuous collisions, not just rental counts. See `solutions/runtime-errors/guard-projectile-return-before-deactivation-2026-09-10.md`.
- Unity test result callbacks may arrive before TestRunner scene cleanup finishes. Confirm `TestRunnerApi.IsRunActive` is false and allow editor cleanup before opening gameplay/entering Play; otherwise the late EditMode cleanup can report an error during Play even when assertions passed.
- The official Unity Pipeline endpoint can be temporarily unavailable while Package Manager resolves packages, scripts compile, or the domain reloads.
- `unity status --project-path .` can return `STATUS_NO_INSTANCES` even while the project's Pipeline endpoint accepts commands. Status is a useful diagnostic signal, not the final reachability oracle.
- Deleting an editor package does not unload code from an already-running Unity AppDomain. After removing an already-loaded CoderGamester package, its localhost listener can remain alive until Unity is restarted once.
- Codex reads MCP configuration at session startup, so a newly registered `unity` server may require a new Codex session before its tools appear.

## Recovery Notes
- Treat the official `unity` MCP server as the supported Codex path. Run `unity pipeline list`, then use `unity command --project-path . list_open_scenes` as a narrow end-to-end read check. Together, a reachable Pipeline entry and a successful narrow command are authoritative even if `unity status --project-path .` disagrees.
- Use `unity status --project-path .` only as additional editor-state diagnostics. The Unity CLI discovers the authenticated Pipeline endpoint; do not copy its transient port into project state.
- If the Pipeline package is missing or stale, run `unity pipeline install --project-path .` or `unity pipeline upgrade --project-path .`, then wait for package resolution and domain reload to finish.
- After deleting the already-loaded CoderGamester package, restart Unity once to unload its assemblies and release its listener. Verify that the formerly configured CoderGamester port is no longer listening, then rerun `unity pipeline list` and the narrow `list_open_scenes` command to prove Pipeline recovered.

## Current Mitigations
- `Packages/manifest.json` pins the official `com.unity.pipeline` package, and the user-level Codex configuration registers the official server as `unity` with this project path.
- The official Unity CLI uses per-instance discovery and remained reachable through verified Play Mode enter/exit transitions. Prefer `unity command` for direct, low-overhead editor operations when MCP indirection is unnecessary.
- The legacy CoderGamester package and project-level port settings were removed. Do not restore them alongside Pipeline.
- The separate, pre-existing `com.youngwoocho02.unity-cli-connector` localhost HTTP package remains installed for older project workflows. It was not removed with CoderGamester and is not registered as a Codex MCP server.
- Combat runtime harness bootstraps itself after scene load in editor or development contexts.

## Protected Game-Data Build

- The editable workbook lives at `Assets/ShooterSurvival/GameData/Editor/Data.xlsx`; player code must not read `StreamingAssets`.
- `Assets/ShooterSurvival/Resources/GameData/Data.bytes` is generated output. The player verifies its RSA signature, decrypts it once, and reuses the in-memory workbook for all table loaders.
- The build preprocessor regenerates the archive when the workbook changed and rejects a missing source, a raw `Assets/StreamingAssets/Data.xlsx` copy, an invalid signature, a stale archive, a changing/partially saved source, or a workbook that fails required gameplay-sheet parsing.
- Archive publication is atomic. Missing or modified protected data raises `GameDataIntegrityException`; the monster-stat formula fallback does not absorb that integrity failure.
- Editor imports validate one complete workbook snapshot before any live cache or scene object is refreshed. A malformed optional `몬스터 성장` sheet therefore cannot leave player defaults and enemy stats on different workbook revisions.
- Stage reset may scan inactive enemies so pooled objects receive current stats, but it re-enables only enemies that were already active. Never force inactive `EnemyPooler` descendants active without dequeuing them through the pool.
- Saving `Data.xlsx` reloads editor gameplay tables automatically; the map tool intentionally exposes only `Data.xlsx 열기`. Player builds regenerate and validate the protected archive automatically. Use the manual `Tools/Data` commands only to diagnose protected-data output outside the normal build flow.

## Firebase Analytics Delivery

- Firebase Unity App and Analytics `13.14.0` plus External Dependency Manager `1.2.186` are pinned as local UPM archives under `GooglePackages/`.
- Android builds fail early when a pinned archive hash changes, `Assets/google-services.json` is missing or malformed, its package is not `com.mzkoreagames.tralaleroshooter`, or its project/app IDs differ from the reviewed destination pin. Review a new config with `Tools/Analytics/Firebase 대상 고정`, then run `Tools/Analytics/Firebase 설정 검증`.
- Keep `Assets/Plugins/Android`, `Assets/GeneratedLocalRepo/Firebase`, and `ProjectSettings/AndroidResolverDependencies.xml` together. Unity 6 requires the custom main, properties, and settings Gradle templates; the settings template exposes the generated Firebase Maven repository to Gradle.
- The build guard runs before External Dependency Manager's scene-build auto-resolution. Resolved AAR/POM files must therefore be versioned with the change and fully materialized by Git LFS in CI; a missing file, LFS pointer, or hash mismatch fails early with a Force Resolve instruction.
- Do not call `PlayServicesResolver.ResolveSync` through an editor main-thread command queue. It waits while the queue it needs is occupied and can freeze the editor. Disable the one-time prompt first, call the callback-based `Resolve(...)`, and observe completion without blocking Unity's main thread.
- Firebase native calls begin only after `CheckAndFixDependenciesAsync` reports `Available`. Events created before that point persist in a 128-event PlayerPrefs queue and are retried in order.
- Active rounds checkpoint every 15 seconds and on pause/quit. A remaining checkpoint becomes one `abandoned` round on the next launch; mode changes, reload, and explicit quit also close the current round as `abandoned`.
- `client_event_time_ms` preserves when an event occurred before an offline retry. BigQuery exposes that time separately from Firebase receipt time and quarantines duplicate, missing, mistyped, or implausible client parameters.
- Firebase Analytics exposes no managed forced-upload/acknowledgement API. Moving an event from the project queue to Firebase is not proof that BigQuery received it.
- Firebase Analytics is a non-functional desktop stub in the Unity Editor. Do not treat Editor Play Mode as an end-to-end test; validate delivery and DebugView on an Android device.
- Disabling collection through `FirebaseAnalyticsRuntime.SetCollectionEnabled(false)` clears unsent events and the unfinished-round checkpoint. Re-enabling is persistent and does not restore discarded telemetry.
- BigQuery export is an external asynchronous system. Initial linkage can take time to create tables; streaming is best-effort, and finalized `events_YYYYMMDD` tables should be the basis for repeatable reporting.
- iOS is not release-configured: the project still has a placeholder Bundle ID, no `GoogleService-Info.plist`, and the pinned Firebase iOS dependencies require iOS 15 or later.

## Noryangjin Scene Gameplay Composition

- The authored Noryangjin scene already contains its Forward gameplay composition. `NoryangjinForwardGameplayInstaller.InstallIntoOpenNoryangjinScene` remains an internal recovery API rather than a user-facing map-production menu command; it rejects any target other than the stopped `Noryangjin_MapTool_Mode` scene.
- The installer temporarily opens `Forward March Mode` and clones its configured scene instances. This preserves serialized references that can be lost when rebuilding the player, UI, or services from unrelated bare prefabs.
- The map scene's `Original` must remain the visible character under `Noryangjin_Player`; the cloned Forward renderers should remain disabled.
- `TimeManager`, `SettingsManager`, and `BulletPooler` are scene-local dependencies. A partial or duplicate Managers setup is unsafe because singleton ownership and stale cross-scene references can conflict; the installer validates the complete set instead of silently creating a second partial set. Do not make the entire Managers root persistent across scene loads.
- A successful install keeps enabled Build Settings entries in this order: `Noryangjin_MapTool_Mode` at index `0`, then `Forward March Mode` at index `1`.
- Before accepting a changed scene, verify there is one player rig and one of each required service, the Original renderer and camera are active, the pre-start/shop UI appears, Start enables movement and attack, and a turn spot produces pause → rotation → route-relative resume.

## Noryangjin Map 2 Static-Scene Integrity

- `Noryangjin_MapTool_Mode_2.unity` is a baked authored scene. The former Stage01 draft, concept-layout, and Map 2 regeneration commands were removed; do not make opening the project or using the map tool rebuild this scene.
- `NoryangjinMapToolMode2SceneTests` protects Map 1 by GUID and SHA-256, checks the copied gameplay roots and prefix, and validates Map 2 route counts, turn spots, market clearance, water margin, and highway contact.
- Keep Map 2 out of Build Settings until its runtime contract is decided. The current Forward installer intentionally accepts Map 1 only.
- The player moves at the constant `playerSpeed` value1 of 8 units/second with no acceleration. The approximately 1,582-unit reference geometry therefore takes about 198 seconds before turn pauses. Treat the reference workbook's five-chapter wave timing as an unimplemented requirement, not validated runtime behavior.

## Noryangjin SR18 Static-Scene Integrity

- Apparent ramp texture corruption can be the flat-road material's binary black cel shading, not broken UVs. SR18's 12 ramps use `SR18_RoadSlope.mat` with self-shading size 0.45 and edge size 0.08; shared flat-road materials remain unchanged. Preserve the slope-material test and inspect both ascent/descent directions when changing lighting. See `solutions/ui-bugs/avoid-binary-black-cel-shadows-on-sr18-ramps-2026-09-05.md`.

- Renderer AABB contact is not proof of deck continuity: support posts extend beyond the walkable plank ends. The SR18 surface test samples 24,702 points across three lanes and requires upward-facing collider surfaces near the expected deck height; every corrected road uses the same mesh for rendering and collision. Preserve this test when editing the geometry.

- `Noryangjin_MapTool_Mode_SR18.unity` is an independent build-excluded sibling with the approved dense market applied. It must not replace or mutate Map 1 or the existing Map 2.
- `NoryangjinSr18SceneTests` validates the 51-road copied prefix, 179-road extension, 15 turn spots, three height-separated crossings, consecutive road seams, the 306-shop roadside placement record, ground support, empty encounter roots and Build Settings exclusion.
- Ground support alone does not establish a valid layout. The rejected 417-shop layout passed support tests while 252 ground bounds overlapped road bounds and interior rows invented a second route language. `Sr18Market_LeavesTheExistingTimberRoadsVisible` independently rejects shop/quay bounds overlapping any timber road and placements farther than 6 units from the nearest road boundary. The current narrow quays support the full storefront bounds, including awnings; the prior 0.7-inset workaround is no longer used. These are static placement checks, not runtime physics or camera-occlusion tests.
- SR18's two elevated spans have now passed live player traversal with the opt-in road-height follower and eight non-stopping pitch spots. The follower queries only configured road colliders within ±0.75 of the requested foot height and preserves height on a miss; very large per-step height changes, edited/missing roads or a changed collider require revalidation. Corner checkpoints and enemy route construction exclude slope-only spots. Full-stage encounter balance, mobile performance and combat around upper/lower crossings remain unverified, so Build Settings exclusion stays in place.
- The map-tool work-grid `600` preset is EditorWindow overlay state rather than scene state; authors must select it when editing the full SR18 bounds.

## Noryangjin Bonus Altar Data Failures

- An authored altar has no valid roll when its selected grade has no runtime-supported row in the `보너스` sheet, or when nearby-altars exclusion consumes every candidate for that grade.
- An invalid authored roll fails closed: its title, stat name, value, and icon are hidden, and its collider is disabled so touching the altar cannot consume a choice that applies no effect.
- To recover from unsupported data, correct the grade/stat/range/value-type/name columns in `Assets/ShooterSurvival/GameData/Editor/Data.xlsx`, refresh the protected game data, and reload the scene so the altar rolls from a fresh enabled collider.
- To recover from candidate exhaustion, move the neighboring altars farther apart, choose a grade with another supported stat, or add another supported row for that grade before reloading the scene.
- Every Forward enemy prefab must reference `Assets/ShooterSurvival/Prefabs/Walls/New/Box_left.prefab` in `EnemyScript_space.bonusWall`. A stale `random_wall_normal` reference restores the retired wall only for enemy-death drops even when the map-tool palette is correct; verify all five prefab references with `EnemyScriptSpace_UsesOnlyTheNoryangjinAuthoringContract`.
- Enemy-death `Box_left` instances must override the prefab root transform to the map-tool result: local scale `(3,3,3)` and world Y rotation `180°`. Instantiating the prefab without that override restores its smaller nonuniform authoring scale `(1.964...,1.35,1)`.
- Keep the enemy-drop `RuntimeBonusWall` marker on the `Box_left` root so stage cleanup destroys the complete composite altar. Child `WallScript` instances must resolve that marker through their parent hierarchy before deciding whether to use the legacy global post-processing overlay.

## Road Oncoming-Lane Traffic Clearance (2026-09-26)

- `OncomingLaneTraffic` cars (lanes ±4.2) have no colliders and follow route distance, so anything static in their corridor (|lane| 2.5–6.0) is driven through. `tools/install-traffic-clearance.cs` (`DryRun`, `Run`, `Signs`) removes such props/roadblocks inside the active ranges, moves potholes to lanes 0/±0.8 with their sign on the shoulder, and cuts toll plazas (±14) out of `activeRanges`. Re-run `DryRun` after adding any road prop in HighWay or RestStop.
- Cars keep a per-lane gap (sorted by distance, leader clamp) and refuse to spawn onto an occupied gap; they brake for anything in `OncomingLaneTraffic.Blockers` (deer via `DeerHop`, rolling logs via `LogTruckSpill`). New moving hazards that share the side lanes must register there.
- The legacy `HighwayOncomingTraffic` is disabled in HighWay (component kept for tests): its Refine2-rebuilt Meshy cars were pitched 270° and it killed on contact. Do not re-enable without rebuilding its car visuals.
- Lane directions (`OncomingLaneTraffic.laneDirections`, user-set 2026-09-26): left lane -1 head-on, right lane +1 the shark's way (spawn behind, overtake, queue behind the shark while it holds the lane, no rear-end damage).
- Deer hazards run through `WaterDeerCrossing` (right-to-left, continuous, 20% max-health + model spin via `CosmeticHitSpin`, cause `Wildlife`); their `HighwayHazard` is disabled and `HighwayHazard.OnTriggerEnter` now ignores disabled components (physics messages reach disabled behaviours). The coverage harness's endurance mode no longer affects deer damage.
- The log truck drives in the centre lane (`truckLane` 0.4) so it never overlaps oncoming cars.
- Bonus talismans at ±3 still overlap the car corridor edge; cars can visually clip them. Known gap.
- `EditorSceneManager.playModeStartScene` is reset by domain reloads; set it immediately before `editor_play` when a playtest must start in a specific scene.

## RestStop / HighWay Stage Split (2026-09-26)

- RestStop keeps only route d 380-1430 (deceleration lane -> parking -> building -> fuel exit road). Gameplay outside was deleted (inactive enemies still count in stat progression) and its placement rows pruned from `Data.xlsx` with `tools/prune-reststop-placements.py` (XML-level, other parts byte-identical). `EncounterPlacementController` aborts all data placement for a scene if any row names a missing object: re-export ids and prune after deleting authored gameplay.
- The remaining RestStop 90-degree corners are `RouteArcDriver` arcs; their turn spots are inactive but stay in the scene for stat ordering.
- HighWay's route is 2820 (was 2340): a straight extension with the d 1650-1990 block replayed at +740, 한울휴게소 approach signs, pylon and exit ramp. `HighwayRebuildContractTests` now expects 58 enemies / 40 ranged.
- Meshy vehicle visuals nested under non-uniformly scaled `Fit` transforms cannot be rotated in place (shear). `InstallTrafficClearance.RebuildVehicles` replaces them under the vehicle root.
- HighWay recovery bypass branches right (`fork.offset > 0`, `HighwayRoute.BypassChosen`); it used to cross the centre line and the opposing carriageway. Only `AmbientTrafficPath` drives the opposing carriageway (northbound per the user); `HighwayAmbientTraffic` is disabled because both used the same lanes.
- Road markings are world-space LineRenderers: copying a road tile by moving its transform leaves the markings behind (`install-highway-korean.cs` Refine10 repairs the copies). Shift positions when duplicating.
- Difficulty check: `RestStopKoreanPlaytest.Difficulty0926` (ATT37/HP46, fixed lane -1.2, endurance) records `damage.json`. HighWay went from 16 hits / 2.7x max HP to 23 hits / 3.8x after Refine8 (72 enemies, +5 deer, +2 log trucks, denser side traffic).
- Scene backups before the split: `outputs/restructure-2026-09-26/backup/`.

## Verification

- An unexpected Windows reboot can leave recently replaced JSON files filled with NUL bytes. A stale `status.json` is not evidence that TRELLIS is still running: check the actual worker, backend queue and machine boot time. Preserve corrupt bytes and intact ledgers before rebuilding display status. Confirm the exact old prompt history and output prefix before any bounded retry; consumed attempts remain consumed. The rest-stop 2026-09-25 interruption audit is recorded in its execution log and `checkpoints/unexpected-reboot-20260925-1105`.
- TRELLIS/ComfyUI completion can race between separate history and queue reads. A missing queue entry is not proof that generation was lost. The task-owned rest-stop client rechecks the same history ID before preserving the halt; it never resubmits automatically in that recovery path. Preserve original attempt ledgers and verify exact completed-output provenance. See [completion polling recovery](solutions/integration-issues/recheck-comfy-history-before-replaying-lost-prompts-2026-09-24.md).
- A ComfyUI interrupt response does not prove that a custom sampler stopped. Confirm its request is inactive before retrying; stop a backend process only after checking the exact task-owned worker/launcher and exclusive queue. Cancelled requests still consume their original attempt. DINO substeps and the rescaled-time CFG interval also change per-step cost, so early timing alone can understate a long texture request. See [glazed-prop partitioning and confirmed cancellation](solutions/workflow-issues/partition-glazed-trellis-props-without-resetting-attempts-2026-09-24.md).

- Rounded display-font replacement requires glyph-bound checks, not only RectTransform or font-metric Center alignment. Centered presentation text now uses MidlineGeoAligned; left/top content retains its authored alignment. Story Previous content is a preferred-size icon/label group, while Next/Skip/tab labels use symmetric insets. Always verify the active layout after enable, because inactive layout groups need not resolve their sizes.

- The final `FaithfulPresentation` pass must run after `ReferenceLobby` and generic material/layout passes; otherwise integrated chrome and rounded display typography are replaced by earlier substitutes. Live story tab sprites must cover their baked backgrounds so only the actual current scene is highlighted.
- Story chrome controls use width-scaled fixed header/footer regions. Validate both tall and short portrait viewports and every caption page; the movie-fill policy limits crop to 15% and falls back to the full image beyond that threshold. Source MP4 and playback lifetime are unchanged.

- Large TMP display strokes need sufficient atlas padding and balanced face dilation: the outline also expands inward. A wide outline on the body preset can consume all white glyph interiors even when `OUTLINE_ON` is enabled. The lobby display material pass must run after generic body-material styling.
- Independent start-hand animation resets its origin/rotation on disable. Test its full cycle; two arbitrary sinusoidal sample points may have the same X even when animation works. Evidence: `map-concepts/harbor-reference-fidelity-2026-09-17/README.md`.

- Mobile TMP SDF requires `OUTLINE_ON` as well as outline width/color; verify both in material tests and actual world-label frames. Material/atlas comparisons must use Unity native object identity across asset reloads.
- `OpeningStoryUI` must start the prepared decoder before seeking to a nonzero scene boundary. Wait for a rendered frame before allowing the next seek; a prepared but stopped player can otherwise remain at frame -1.
- Audio focus loss can arrive after one child AudioSource is destroyed but before the service is destroyed. `GameAudioService.ApplyFocus` checks each channel independently and teardown clears its ready flag.
- Pier and NPC position changes need fresh Play Mode verification; statically batched scenery does not provide reliable runtime move previews. Keep SR18's corner clearance validation active when adjusting both enemy start/target and workbook activation lead.
- Long multi-scene authoring should use official CLI `run_script --timeout_ms 120000`. A short eval timeout can leave a completed mutation behind; inspect saved state before retrying. Related evidence: `map-concepts/harbor-opening-refinement-2026-09-16/README.md`.
- Run `unity --version` and `unity pipeline list`.
- Run `unity command --project-path . list_open_scenes` and confirm the expected active scene.
- Optionally run `unity status --project-path .` for extra diagnostics; do not fail an otherwise successful reachability check only because it reports `STATUS_NO_INSTANCES`.
- Use `tools/validate-agent-harness.ps1` to sanity-check repo-side harness prerequisites.

Legacy outline pass (2026-10-01): `FlatKit/Stylized Surface With Outline` draws its outline only through the "Legacy Outline" RenderObjects feature (LightMode `OutlineLegacy`). A new URP renderer or quality level without that feature silently loses outlines on 162 materials; rerun `tools/install-legacy-outline-feature.cs` and check `LegacyOutlineFeatureInstall.Status` (SRP Batcher code 0). `tools/encode-s22-cinematics.py` overwrites `tmp/image-previews/s22-polish-2026-10-01/cinematics-motion.png`; back up first. Moving `HighwayRoute.Distance` by reflection in tests also requires moving the player onto the route, or the next `Advance` reads a huge lateral offset.


## Chapters 4/5 final validation - 2026-10-02

Corrected saved-state route matrix4/4, Chapter45 tests26/26, actual actor animation21/21, lifecycle35/35, cityhazard9/9, towerhazard25/25 and lift-disposal9/9 pass. Previously accepted campaign57/57 and workshop36/36 remain separately documented. Staged callbacks and held-position fixtures are not represented as ordinary gameplay or human testing. Every preference snapshot restores with zero mismatches.

Full EditMode remains53 failures (1073/1126pass): zero new failures or changed signatures relative to the1124-case receipt. All20 previously unclassified failures have current source/mesh diagnosis without an unsupported historical-baseline claim. C08 city actor animation was a real new issue and was corrected with matching C07 visuals before the final route matrix.

Final ARM64 debug-signed review APK succeeds with0errors/58warnings; complete package verification and all51,746preexisting product hashes pass, plus two independently approved generated linker files. Broad tests regenerate shared assets; the preservation investigation, restored dynamic font and bounded prior dirty-checkpoint proof are retained. Do not rerun those generators in a dirty worktree without exact byte backups. S22 FPS/thermal and human enjoyment remain unverified. See [final evidence](../outputs/chapters45-2026-10-02/FINAL-VERIFICATION.md).


## Ch4/5 cold Editor restart — cycle06

Ten directed save boundaries passed across fresh native Unity processes. The game persists permanent wallet/reward/unlock/upgrade/cosmetic/settings data but has no mid-run route/floor/cinema checkpoint; a QA-selected fresh scene is not Continue evidence. Capture preferences from the actual native Unity process: the same named HKCU key returned DIFFERENT data from the tool shell and native Editor in cycle06, even with escalated execution. Shell registry hashes alone did not establish game-save restoration. Restore narrowly through native PlayerPrefs and verify after another normal process restart; full native registry must be captured before future tests to cover unknown keys. This cycle restored all78 originally observed entries, but cannot compare unknown native keys with pretest values. Normal Editor exit loses SessionState, so preserve that separately. Check clean scenes and no new desktop input before each exit. The Windows launcher can retain inherited PIPE handles; use output files and Pipeline readiness, waiting through startup503 Busy without another launch. Failed fixture attempts and bounded test-reward rollback remain recorded. See [cycle06 evidence](reviews/chapter45-restart-cycle06-2026-10-03.md).


## 2026-10-04 Unity dynamic QA callbacks — cycle11

Each `run_script` invocation may compile a different dynamic assembly. Calling a newly compiled class's static Stop does not prove that delegates registered by an older assembly were removed. After a failed fixture, inspect the real Editor event invocation lists; remove only the exact QA-owned delegates in stopped Edit mode and verify zero remaining callbacks. Do not clear unrelated subscribers or rewrite engine session identity.

Serializing raw Unity Vector3 through the generic JSON path can recurse through `.normalized`, raise JsonSerializationException and trigger ErrorPause. Emit explicit float arrays. The cycle11 failure retained six owned callbacks; bounded cleanup and fixed serialization were followed by successful native rear-contact and lifecycle fixtures. All failed attempts and permitted-setting restoration receipts are retained. See [cycle11 review](reviews/chapter45-telegraph-cycle11-2026-10-04.md).


## 2026-10-04 Deferred screenshots and native scene preservation — cycle12

ScreenCapture.CaptureScreenshot is deferred. Calling Replay in the same frame can produce a lobby image under a defeat filename. Hold the native defeat state across a frame and inspect both the PNG and active UI text before claiming visual coverage.

Saving a scene can add unsaved URP light-data components and serialize TMP style/color caches. Compare against a pre-save recovery copy; restore only the exact added components/changed cache fields through the Editor, save, then verify all original scene lines. Never broadly delete original components or raw-edit scene YAML. Cycle12 retained the failed helper/partial-route evidence and repeated the corrected checks. See [cycle12 review](reviews/chapter45-validation-cycle12-2026-10-04.md).


## 2026-10-04 Protect QA preparation with restoration — cycle13

Preparation that writes a captured game item must be inside the restoration boundary. Putting TutorialDone preparation and Editor Configure before try can leak the prepared value if Configure rejects a busy/changed-input Editor. The cycle13 runner now tracks successful preparation and Play requests inside try/finally. A native invalid-scene rejection, before any Editor mutation or Play request, restored TutorialDone and matched all78observed preferences. Do not rewrite engine session identity or restore uncaptured keys. The historical unsafe runner is retained as evidence, not a runnable recommendation. See [cycle13 review](reviews/chapter45-validation-cycle13-2026-10-04.md).


## 2026-10-04 Frame edges and reload click-through — cycle14

Capture press edges in Update so a physics frame cannot miss the new anchor. Judge lobby start against cumulative horizontal displacement from the current press origin; clear armed state on release/cancel. A reload can expose a different lobby button under the pointer, so guard the new Canvas briefly and through pointer release. Native OS burst timing must be measured outside the Editor main thread when synchronous scene loading blocks that thread. Preserve exact view/cursor state and distinguish test fixture failures from product failures. See [cycle14 review](reviews/chapter45-validation-cycle14-2026-10-04.md).


## 2026-10-04 Native input observations — cycle15

Do not treat same-Editor-tick mouse release plus immediate pointer recenter as equivalent to observed-release sequencing. Preserve raw timing and unresolved product attribution. Compare consecutive samples on every floor, excluding only transfer/cross-floor edges; exact lane equality across a route bend is not a valid invariant. Release-only cleanup is distinct from new pointer movement and must stay within the owned Unity foreground. [Cycle15 evidence](reviews/chapter45-validation-cycle15-2026-10-04.md).


## Chapter45 initial overlap and paused trigger contacts — cycle18

A shot already inside an ordinary enemy could leave its collider before native contact. Physics contacts also continued while the custom game clock was paused. Initial/resumed flight now resolves existing live enemy overlap through the existing damage transaction, and Chapter45 hit eligibility waits for a running clock. Native112 conditions, six high-health conditions,30 synchronous guards and two ordinary routes passed. [Evidence](reviews/chapter45-bullet-regression-cycle18-final-2026-10-04.md).

## 2026-10-06 Name-keyed generated materials and in-play recompiles

- Never cache generated materials by texture *name*: every Meshy/TRELLIS atlas is `texture_0`. `NoryangjinClaudeFeedbackBuilder` did, so unrelated characters shared one atlas and showed camouflage patches on phones (photo 3). Key by asset GUID; `CharacterTextureMismatchRepair.Scan` and `EssentialProposalsTests` now guard all five chapter scenes.
- Editing scripts while the Editor is in Play Mode triggers "recompile and continue playing"; the reload throws a burst of NullReferenceExceptions (e.g. `FatManCratePose.OnEnable`) and Error Pause freezes the frame counter. Exception stack traces are disabled in ProjectSettings (`m_StackTraceTypes`), so these look stackless. Stop Play before editing; enable `Application.SetStackTraceLogType(LogType.Exception, ScriptOnly)` temporarily to diagnose.
- Pipeline `eval_file` has a 5 s main-thread limit; multi-scene installers report a timeout while still saving. Run one scene per call and verify file timestamps.
- `capture_game_view --save_path` resolves relative to the authoring root (`Assets/`). Save under `Assets/tmp/...` only transiently, then move to repo `tmp/` and delete the asset folder.
- URP renderScale applies to RenderTexture game cameras. With Vulkan native render passes the 0.75 intermediate mismatched the 1024×768 cosmetic preview target. `CosmeticPreview` now renders that camera at scale 1 between Begin/EndCameraRendering.
