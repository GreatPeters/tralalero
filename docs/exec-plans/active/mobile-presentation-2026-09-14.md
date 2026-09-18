# Mobile UI, audio and install-size revision

Started2026-09-14. Plan: docs/plans/2026-09-14-001-feat-mobile-ui-audio-size-plan.md.

## Requested outcomes

The current user message requests all eleven items: lateral upgrade +5%×10, major APK reduction, natural retreating knife grip, current attack/health in upgrades, working phone cosmetic model, several UI directions unified across all surfaces, full SFX coverage, two original BGM candidates inspired by the linked track, KERISKEDU_B SDF (or GmarketSansTTFBold SDF2), UI-first priority, and a newly visible coin.

## State

- U1 baseline preservation/audit complete: live dirty scene copied with saveAsCopy preserving dirty state/path; source-before.zip saved; preference snapshot includes63legacy keys plus3new lateral keys.
- U2 in progress: appended LATERAL_SPEED enum/id10, applied one capped additive lateral response multiplier, generalized owned-stat synchronization, extended analytics and preference snapshot handling, and added UpgradeSummaryUI. Theme/card/header scene integration is pending.
- Artifact Tool authored10rows in B303:J312 (the existing table starts at B2, not A1). Existing16992cells across13sheets retain values/formulas/styles/number formats and key sheet metadata. New long text wraps. Verified candidate copied into canonical Data.xlsx. The new price curve reuses COIN_BONUS levels1–10:70,105,...385coins.
- Native tests before workbook installation:6/8passed; the missing-data and saved-level recomputation cases failed as expected. Rerun after installation is the next verification point.
- U3–U5 and U7–U8 pending.
- U6 generation started independently: two original64-second stereo44.1kHz BGM candidates are complete (a_midnight_tide and b_ghost_arcade), approximately68seconds generation each. Selection, looping, compression, SFX and runtime integration remain pending.
- Planning/headless review complete in the main thread (repository sequential-agent mapping). Corrected incomplete source paths and the final size pass's dependency edges; retained explicit glyph, native preview, purchase persistence and audio lifetime checks. Remaining implementation observations concern device rendering, exact size reduction and local audio execution, not unresolved product decisions.
- Latest native APK baseline699039439bytes; SHA25675609afc636f6571e1090f022a77956265c1b791e87a44a0e956bc4e22bf3535.
- Phone from completed prior task: Samsung SM-S901N, Android16, ARM64; adb serial R5CT60Y9RSM. Confirm current authorization before acting.
- Original Editor scene now reports dirty. Preserve its live state before tests or scene switches. Many pre-existing uncommitted files belong to earlier work; do not revert them.
- Both requested fonts are available in Assets/JH/Font. Existing runtime GameUIFont forces Jigmo.
- BuildReport packed groups149. Largest sampled entries: URP Lit93.9MB; FlatKit22.4MB; Gmarket17.7MB; Noto17.7MB; KERIS16.8MB; skybox15.0MB; hole model14.2MB; Jigmo TTF10.2MB; each of seven fitted shoe mesh assets5.7–7.0MB; many base/normal/emission textures2.5MBeach.
- Complete audit: map-concepts/mobile-presentation-2026-09-14/baseline/build-content-v2.json has3736assets,84stale texture-property references to49unique textures and7font assets. Packed type totals include419.9MiBtextures,121.5MiBshaders,96.3MiBGameObjects,55.2MiBfont assets and54.0MiBstandalone meshes. Initial build-content.json omitted nested arrays through JsonUtility and is retained as a failed export; v2uses the explicitly selected Newtonsoft assembly and was parsed/validated.
- Installed audio generation: C:/AI/StableAudio3, optimized/tflite, with CPU Small-Music and Small-SFX models supported. Read its actual CLI/Gradio interface and cached weights before generation. Existing Run-StableAudio.ps1 uses12threads, port7861, no share. Do not automatically run Start-StableAudio.ps1 because it opens a browser/writes outside the workspace.
- Installed generation roots also include C:/AI/TRELLIS2-AMD and C:/AI/ComfyUI-Creative-AMD. Keep heavy generation bounded; do not disturb other queues/processes.
- The reference YouTube page could not be opened by web, but search identifies the song title as The Sound of Your Fear, commonly credited to Midi Blosso/Midi Blossom. Confirm the linked version/atmosphere where feasible; generate original music, not a copied recording.

## Presentation implementation update

- User selected Ocean Pop: bright ocean colors, cream cards, navy text, gold action buttons. Three palettes × two native candidate screens retained under `tmp/image-previews/mobile-presentation-2026-09-14/ui-candidates/`.
- Lateral workbook installation followed by **8/8 native LateralUpgradeTests passing**. Actual upgrade shop has ten cards, live attack/max-health, wallet and functioning vertical scroll. Native v3 captures show +0%→+5%, level0/10 on the last card. Actual purchase/max-level flow remains to verify.
- All three playable scenes have received the common authoring builder: shop, upgrades, story/movie/transition, pause, settings, result screens, HUD and lobby. Last small slider/lobby refinements still need applying to earlier scenes.
- `CosmeticPreview` now renders through the normal game-camera path for two frames per change, using a single-sample RT. Editor live1024×768 preview had227896visible pixels; Android acceptance remains pending.
- Runtime font changed to KERISKEDU_B SDF. Twenty dependent prefabs/21labels migrated and TMP default updated; three automatic-Resources legacy fonts moved to `Fonts/LegacyArchive` preserving GUIDs and backups. Final dependency/glyph audit remains.
- Native v3 screen captures: `tmp/image-previews/mobile-presentation-2026-09-14/actual-v3/`. No Unity errors during capture. Story/shops/pause are visually verified; slider track correction followed v3 inspection.
- Found and fixed: missing Unity components can be fake-null, so editor builder uses explicit `== null` semantics; inactive nested canvases lost overrideSorting until OnEnable, now MobileUILayer reapplies it; EquipmentCardGrid cached width/count while actual content height reset to0, now also compares required height. Global GameObject.Find("Canvas") selected leaked preview surfaces, so native proof scripts scope to active-scene roots.
- BGM two64-second candidates complete. Three additional Stable Audio3 SFX jobs are running sequentially with8CPUthreads (harbor, traffic, knife), original prompts/seeds retained. Audio selection/processing/runtime integration pending.
- The editable task source snapshot, per-scene before-ui snapshots, per-asset font migration backups and66-key preference snapshot remain under `tmp/backups/mobile-presentation-2026-09-14/`.

## Next actions (current)

### Latest checkpoint — native Editor waiting

- New `MobilePresentationTests` compiles (5tests) but the native async invocation never produced a verdict and its MCP request timed out after300seconds. Pipeline lists the original Editor PID10140as reachable, but narrow eval times out on its main thread. Process title is `RestStop ... *`; the test framework's SaveModifiedSceneTask calls `SaveCurrentModifiedScenesIfUserWantsTo`. A save dialog is the current inference, not a visually confirmed dialog.
- Computer Use skill initialized and retried/reset as documented, but every list_windows call returned `native pipe ... os error2`. No UI input was sent. An async user question asks whether Unity has a save dialog and requests Save if present. **Keep this pending; do not infer a click or terminate the original Editor.**
- ADB36 `devices -l` currently returns an empty list. The new APK has not been built or installed. The earlier installed699MBAPK remains the only measured device release.
- `MobileContentOptimizer` is implemented and separately compiled through a temporary project including the file (original Unity csproj has not yet imported it). It has NOT run. It cleans only nonexistent shader texture slots, applies role-specific Android texture overrides, allows medium compression only for the explicit rigid-weight cosmetic shoe meshes, and aligns the default Graphics pipeline to the already-selected Mobile quality. Inspect Analyze output before Start; texture work runs one importer per Editor update and reports to `optimization/progress.json`.
- Material/texture targets: UI2048/ASTC4×4; character albedo2048/ASTC6×6; normal/emission512; mask256; large environment1024; small environment/sky512. No gameplay road/collision mesh changes. The existing Mobile and PC quality platform exclusions were read and preserved. Unity's official URP stripping documentation confirms that default and enabled quality pipeline assets both influence the variant set: https://docs.unity3d.com/kr/6000.0/Manual/urp/shader-stripping.html .
- Native knife authoring completed: `Knife_RelaxedCarry.anim` (3.53333354seconds) changes only arm muscle channels of the YellowMan idle slot, also used for the carry layer. Original fishing pose was the cause of raised empty hand/strong wrist bends. Candidate01HumanPoseHandler rendering changed orientation unexpectedly and was rejected; candidate03uses authored muscle curves. Final attack/death PNG hashes exactly match their before-front captures. Finalpose images are in `tmp/image-previews/mobile-presentation-2026-09-14/knife-final/`.
- New coin assets were natively built:1.16m diameter,600triangles,1800vertices,one opaque vertex-color material,zero textures. CoinPickup now attaches one shared token instead of six primitive renderers; trigger/proximity and exact one-shot grant are retained. Gameplay-camera/phone evidence remains pending.
- All audio production finished:26effects + music/harbor/traffic,1,681,731source bytes. Native import configured music streaming, other effects mono22.05kHz ADPCM, ambience Vorbis. Two62-second OGG candidate loops remain outside Assets. GameAudioService and gameplay/UI consumers compile. First-launch focus resume and shared-RNG consumption were caught in local review and corrected. Actual listening/device mix still pending.
- `tools/verify-mobile-runtime.cs` is prepared for a fresh Play Mode lobby: exact ten purchases/2275coins, no overcharge at max, stat headers, actual1.5lateral response, lane/pause/stationary guards, bounded audio and mute. It snapshots/restores touched preference values and runtime route/health state; still restore the original66-key task snapshot after final scene/test activity.
- `tools/build-mobile-playtest.cs` and `tools/audit-mobile-build-size.cs` now accept output paths while retaining previous defaults and overwrite guards. New build should use `tmp/mobile-presentation-build-2026-09-14` and `Builds/Android/TralaleroShooter-MobileUI-20260914.apk`.
- Runtime and editor dotnet builds passed. Temporary editor validation including MobileContentOptimizer passed with package warnings only. Harness validation passed. Compound solution frontmatter validated using `python -X utf8` (plain Python tried cp949and failed to read the Unicode evidence text). No commits or phone data clearing performed.
- Latest source refinements require **reapplying MobilePresentationBuilder to all three scenes after recovery**: audio button bindings, buy-label maximum state, removal of obsolete workshop art, slider track/handle cleanup, preserved 3D lobby with moved hint, and lighter preview platform. Current saved v3screens do not include all of these final refinements.

Finish native UI refinements/meaningful tests, knife grip and coin; integrate original audio; apply measured content reduction; review/test/build/install and verify on phone. Restore preference state after the final scene/test activity. This is ongoing work, not a completed release.

## Initial next-action record (superseded)

1. Finish plan handoff/review, then execute U1 with fresh scene/source/workbook/preferences backups and detailed packed/material/font audit.
2. Inspect StableAudio3 optimized CLI and user-font metadata/license while baseline reports are collected.
3. Implement U2 exact ten-level upgrade and current-stat presentation, then native UI candidates and preview fix.
4. Keep this log current with concrete changes, rejected candidates, tests and final device/build evidence.
