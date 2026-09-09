# Reliability

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
- Run `unity --version` and `unity pipeline list`.
- Run `unity command --project-path . list_open_scenes` and confirm the expected active scene.
- Optionally run `unity status --project-path .` for extra diagnostics; do not fail an otherwise successful reachability check only because it reports `STATUS_NO_INSTANCES`.
- Use `tools/validate-agent-harness.ps1` to sanity-check repo-side harness prerequisites.
