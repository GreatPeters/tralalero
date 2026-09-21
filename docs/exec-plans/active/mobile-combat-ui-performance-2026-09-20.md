# Mobile combat, presentation and performance feedback

Status: implementation, Editor verification and Android installation complete; final phone FPS awaits unlock.

User requirements:
1. Correct the female boss's sword grip; raise her current health by 50%.
2. Make Guard shots visibly travel from the weapon (retain Arrow2 as the requested projectile).
3. FatMan throws once per encounter/run, using his actual attack stat for crate impact damage.
4. Make the blue start/tutorial prompt clearly legible.
5. Replace the awkward top attachment on Bonus talismans with fitting decoration.
6. Bonus icons use the enhancement icon artwork.
7. Restore the user's earlier enhancement icons; redesign only if those originals cannot work.
8. Use a 90%-black background veil for the initial start state.
9. Apply effective visibility/occlusion optimizations.
10. Measure batches/frame time and improve mobile performance while preserving gameplay and map visibility.

Work order: preserve current scene state; audit authored assets and measure the current running scene; make local combat and presentation corrections; measure targeted CPU/render optimizations against the same fixture; verify shots/hits, all three chapter interfaces and route slopes/corners; record exact results and limits.

Branch: `fix/mobile-combat-ui-performance`, based on current `master`.
The initially dirty SR18 scene was copied natively to `tmp/mobile-feedback-2026-09-20/before/SR18-unsaved.unity`, with its on-disk baseline beside it. The difference contains ten UI layout values; neither copy is discarded. No raw scene/prefab YAML editing is permitted.

Initial findings:
- The original transparent enhancement art exists in `Assets/JH/UI/Upgrade`; current shop code overrides it with newer generated icons.
- The camera's Unity occlusion flag is already enabled. That checkbox alone is not an optimization result.
- `NoryangjinCameraOcclusion.CacheTraversedRoads` makes many road-support queries per frame; each scans all road colliders. Benchmark this separately from render culling.
- `EnemyEventController` enables living Animators and updates facing even for distant actors. Preserve attack timing when reducing their visual work.
- Capture frame metrics without screenshots in the sample window. Record Editor focus/frame cap and distinguish Editor measurements from phone FPS.

Verification artifacts will be under `map-concepts/mobile-feedback-2026-09-20/`; scratch backups/measurements under `tmp/mobile-feedback-2026-09-20/`.

Completed:
- All ten requested areas implemented; user-approved FatMan attack 100 applied to all six rows, preserving unrelated cells and formula caches.
- Native probe confirms one crate launch/100 damage and Guard projectile survival after owner disable.
- 82 focused tests, both C# builds and harness validation pass.
- Three-chapter UI captures and both elevated road spans verified; 12.16–12.18m peak deck height and return to ground without support/trigger regression.
- Final Editor fixture: 296 batches / 17 enabled animators / 12.28ms mean, compared with 327 / 53 / 16.70ms baseline. Timing varies; detailed evidence records baked-off results and the rejected far-plane experiment.
- APK build scheduled via `MobilePlaytestBuild.Schedule`; phone reconnected after a USB interruption.

APK build succeeded in 191.61s, 0 errors. In-place phone install succeeded and its SHA-256 matches the local APK. The device auto-locked during the build; user unlock request is pending. Temporary build settings restored; Editor is clean in SR18 Edit Mode.

Remaining: measure phone FPS and temperature, inspect actual phone rendering, finish evidence/docs. `ce-compound mode:headless` learning recorded and frontmatter validated; no `gh` executable was available for related issue search.
