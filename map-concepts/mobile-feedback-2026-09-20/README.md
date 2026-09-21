# Mobile combat, UI and performance feedback

Branch: `fix/mobile-combat-ui-performance`. This follow-up has not been merged or pushed.

## Applied behavior

- Woman: measured handle/palm grip, active HP 2458 → 3687 (+50%). Disabled dependent right-hand row recalculates to 4240 without multiplying twice.
- Guard: authored Arrow2 mesh with bright gold material and short trail. Detached projectiles survive shooter deactivation and remain bounded by run cleanup/lifetime.
- FatMan: one throw per run; six placement attack cells set to the explicitly approved 100. Only vertical release pitch aims at collider height; horizontal direction stays in the authored lane.
- Tutorial: larger panel/type/icon/close target and 4.5s duration. Initial lobby uses a 90%-black layer beneath its UI.
- Bonus: red cord and gold eyelet, original enhancement icons, consistent with all 60 upgrade cards. Shared source, 450 placed visuals and 66 drop/pool prefab visuals updated. Existing torso absorption retained.
- CPU: road support uses a 16m spatial index; distant enemy rendering, animation and facing work is gated without disabling collisions.
- Rendering: baked occlusion in SR18, HighWay and RestStop; dynamically hidden roads/props excluded as occluders. RestStop has only one static candidate, so its bake alone offers little benefit. Mobile world render scale 0.8 → 0.75, MSAA 4x → 2x. Camera far plane stays 1000.

## Verification

- 82 focused Edit Mode tests pass: combat lifecycle, exact damage, authored lane/pitch, event controllers, throw direction, two-hand pose, helper/ship route behavior, camera occlusion, road height and every Bonus type.
- Both C# project builds pass; agent harness passes.
- Native traversal of both raised-road spans passes: peak deck heights 12.16–12.18m, ascent/descent pitch ±19.57°, all eight slope triggers consumed, return to ground. Four captures retained under `tmp/image-previews/combat-route-camera-2026-09-20/144942/`.
- Native `combat-v2` probe: FatMan exactly one launch over 8 simulated seconds, one hit, HP 500 → 400. Guard launches one visible shot, HP 500 → 496.4; a separate probe disables the shooter mid-flight and confirms the shot travels and hits.
- Native UI captures in three chapters: `tmp/image-previews/mobile-feedback-2026-09-20/final-{SR18,HighWay,RestStop}`. The two close-up talismans are art previews, not reward transactions; existing pickup/reroll tests cover reward behavior.
- Raw combat and measurement artifacts: `tmp/mobile-feedback-2026-09-20/`.

## Performance measurements

The Editor fixture holds the runner at the same start position, disables shooting, warms up 45 frames and measures 120 frames without screenshot work. Resolution 1080×2340, focused, vSync off, no FPS cap.

| Metric | Before | Final Editor fixture |
|---|---:|---:|
| Batches | 326.6 | 296.2 |
| Triangles | 1.923M | 1.835M |
| Enabled animators | 53 | 17 |
| Mean frame time | 16.70ms | 12.28ms |
| Mean FPS | 59.9 | 81.4 |

Editor timing varied between runs, including a slower intermediate result. This is evidence for the sampled fixture, not a sustained mobile FPS guarantee. Baked occlusion alone was a small render saving and added CPU overhead in this view (final baked-off sample 11.54ms); road indexing and animation gating address larger costs. The rejected 90m far-plane experiment cut batches dramatically but visibly removed the background, so it was not retained.

Old APK on Galaxy S22: lobby 40.09FPS (42.8°C), first gameplay 48.66FPS (44.0°C), each about 10 seconds using the app SurfaceView's actual presentation timestamps. Final measurements await unlock after the phone's automatic timeout during the build.

## Android installation

`Builds/Android/TralaleroShooter-MobileFeedback-20260920.apk`: nondevelopment ARM64 IL2CPP, test ads, 685,765,318 bytes. Build succeeded in 191.61s with 0 errors / 58 warnings. In-place `adb install -r` succeeded on SM-S901N without uninstalling or clearing app data. The installed `base.apk` SHA-256 matches `700c7ada20fcf272f57718a4ca6d33b5d888c205aa59841cc3f94b02217bb819`. Temporary signing/build settings were restored to the original bytes.

## Reproduction

- `tools/apply-mobile-feedback.cs`: native source/prefab/three-scene authoring; `UpdateBonusPrefabs` also updates drop/pool copies.
- `tools/buff-woman-health.mjs` and `tools/apply-mobile-workbook.py`: Artifact Tool authors requested cells; selective graft preserves every unrelated original cell, cache, style and ZIP part. Use the matching before snapshot and `--fatman` only for the six approved attack edits.
- `tools/bake-mobile-occlusion.cs`: run in an idle chapter, wait for background bake completion, save scene natively.
- `tools/verify-mobile-combat-play.cs`, `tools/capture-mobile-feedback-ui.cs`, `tools/measure-mobile-scene.cs`: bounded Play Mode probes with fresh output paths.
- `tools/measure-phone-fps.py`: app-scoped SurfaceFlinger frame timestamps; preserves raw intervals and battery temperature.

## Corrections caught during verification

- The first physical crate probe skimmed above the real player capsule despite apparent visual overlap. Pitching only toward collider height fixed this without tracking lateral dodges.
- Artifact Tool recalculation altered unrelated legacy cached formula results in its candidate. The installed workbook graft copies only approved cells and checks every unrelated cell/style/package part against the original; those candidate changes were discarded, not applied as balance fixes.
- Updating only placed talismans missed stored drop/pool copies; the final authoring pass explicitly covers those 66 visuals too.
- Unity can return multiple managed wrappers for one native sprite; the identity test now uses Unity equality, verified against matching instance IDs and asset paths.
