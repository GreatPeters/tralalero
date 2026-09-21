# Reliability

## Mobile visibility and launched attacks — 2026-09-20

Detached Guard/FatMan shots must survive shooter deactivation; register them with run cleanup and bound their own lifetime. Clones from distance-culled templates must clear `forceRenderingOff`. Visual distance gating must leave collision/event roots enabled and complete an active crate stroke before release. The road spatial index assumes authored roads remain static during a run; call `Configure` after a layout change. Exclude camera-hidden geometry from baked static occluders. Workbook edits must preserve unrelated formula caches, not silently apply an importer's recalculation as new combat balance. Evidence: `map-concepts/mobile-feedback-2026-09-20/README.md`.

## Common Bonus feedback — 2026-09-20

WallScript deactivates its lifetime root immediately after applying the reward; the talisman transfer therefore runs in a separate scene-owned visual object. It must never apply a second reward and must cancel on player loss/death/run exit or claim-timestamp reset. Generated glow textures require explicit Single sprite import; a PNG with Multiple mode and no slices yields a null runtime Sprite. Rebuild only from clean Edit Mode. See `map-concepts/common-talisman-applied-2026-09-20/README.md`.

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

## Verification

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
