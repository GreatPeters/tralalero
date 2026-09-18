# Mobile combat performance — 2026-09-13

Status: project optimization and Editor verification complete; phone installation and device performance acceptance pending.

## Request and decisions

The user requested smoother overall play on their phone. Device model and the most affected scene were requested, but were not supplied during this run. Android adb reported no attached devices.

Inspection found per-hit construction/destruction in numeric popups and enemy particles, despite an existing projectile pool. The existing runtime60fps foreground/15fps background cap and physics catch-up budget were retained. The selected changes address measured construction costs and the Mobile pipeline's8192 shadow-map setting; gameplay balance, meshes, routes and scene composition were not changed.

## Changes

| Area | Final behavior |
| --- | --- |
| Damage and coin numbers |64 reusable world-space TMP canvases prewarmed in CanvasScript.Start; one fade/rise update loop; numeric formatting without per-event string construction; no input raycasters. |
| Enemy hit effects |32 prewarmed effects per prefab; clear/restart particles on rental; retain original local pose and duration; return after expiry or target deactivation. |
| Saturation | Recycle the oldest cosmetic entry. Damage, projectiles, score and money still resolve. |
| Scene lifetime | Scene-owned pools; explicit cleanup for particle children currently attached to enemies. Missing slots after enemy destruction recover on rental. |
| Mobile shadows |8192→2048 main-light shadow resolution;16times fewer shadow-map texels. MSAA4, render scale0.8 and shadow distance unchanged. |

The current active chapter scenes share the inspected two-second WALKER_HitEffect. This revision does not add a generic pool for every possible gameplay prefab.

## Measured evidence

Same connected Unity6000.2.6f1 Editor,32 synchronous calls per sample:

| Operation | Before | After |
| --- | ---: | ---: |
| Numeric popup calls |17.5637ms |3.1234ms |
| Hit particle calls |2.5605ms |0.2338ms |

These are individual call-burst samples after warmup, excluding deferred rendering, TMP rebuild work on later frames and deferred destruction. They demonstrate lower call-path cost, not phone FPS. The thread allocation counter returned0on the allocating baseline, so it was rejected and no measured zero-GC claim is made.

Source timing files: [popup before](../../../tmp/mobile-performance-2026-09-13/popup-before.txt), [popup after](../../../tmp/mobile-performance-2026-09-13/popup-after.txt), [particle comparison](../../../tmp/mobile-performance-2026-09-13/hit-effect-comparison.txt).

The SR18 camera-only [before](../../../tmp/image-previews/mobile-performance-2026-09-13/before.png) and [after](../../../tmp/image-previews/mobile-performance-2026-09-13/after.png) screenshots were inspected at540×960. The road, character and scenery remain readable. These captures do not include screen-overlay HUD proof or full-chapter shadow comparisons.

## Verification

- Native Edit Mode:54/54passed across CombatPresentationPoolTests(4), ProjectilePoolLifetimeTests(5), EnemyEventControllerTests(24), EnemyThrowDirectionTests(9), Sr18CombatPolishTests(12).
- The4new tests cover capacity/identity retention through200popup and100particle rentals, numeric/coin state reset, expiry, paused time, target deactivation and teardown/reinitialization.
- Real Play Mode teardown removed the old particle pool and all its externally attached children; a new pool contained32idle effects. [Result](../../../tmp/mobile-performance-2026-09-13/live-teardown.txt).
- Runtime and Editor dotnet builds passed with zero errors. The final Editor build reports one existing SplineSpeed.m_LastIndex warning. [Build log](../../../tmp/mobile-performance-2026-09-13/editor-build.log).
- The project agent-harness validator passed. [Log](../../../tmp/mobile-performance-2026-09-13/harness-validation.log).
- Native error-console checks after the live probes returned zero entries.

The [repeatable probe](../../../tools/probe-mobile-combat-performance.cs) advances once per actual game frame, starts the normal run and adds a deliberate synthetic visual burst every six frames. It is a presentation stress check, not a balance or full-chapter completion test.

| Chapter | Completed frames | Synthetic events | Popup objects retained | Original hit objects retained | Travelled |
| --- | ---: | ---: | ---: | ---: | ---: |
| SR18 |180 |240 |64 |32 |33.77m |
| HighWay |180 |240 |64 |32 |31.36m |
| RestStop |180 |240 |64 |32 |82.37m |

[SR18 report](../../../tmp/mobile-performance-2026-09-13/sr18-complete/report.json), [HighWay report](../../../tmp/mobile-performance-2026-09-13/highway/report.json), [RestStop report](../../../tmp/mobile-performance-2026-09-13/reststop/report.json).

RestStop reached ordinary player death before its sample ended; later synthetic effects intentionally exercised cleanup while gameplay was stopped. Its66.75ms median/74.24ms p95 reflect background Editor pacing (target15fps), not an Android timing result. The other Editor reports also include focus-dependent scheduling and must not be compared as device benchmarks. An earlier600-frame SR18 attempt was stopped without a report and is not counted.

## Recovery and rerun

- Before-images of modified source/settings and63preference keys are in `tmp/backups/mobile-performance-2026-09-13/`. Existing uncommitted project work was preserved.
- After all tests and scene changes, the63captured keys were restored and compared with zero mismatches. [Result](../../../tmp/mobile-performance-2026-09-13/preferences-restored.txt).
- The Editor was returned to clean Edit Mode with the original SR18 scene,9roots. No scene files were saved by this task.
- For another probe, take a fresh ChapterPlaytestPreferences snapshot, enter Play Mode in the intended scene, invoke `MobileCombatPerformanceProbe.Begin(label, 180)` using official Pipeline `run_script`, and wait for that label's report before exiting. Restore preferences after reopening the original scene. Use a fresh label: previous evidence is protected.

Phone acceptance still needs the actual device and an updated Android build: measure frame-time percentiles during ordinary combat, the30-second hall defense and a sustained warm-device session. No APK was built or installed by this task.

## Learning captured

The sequential `ce-compound mode:headless` review produced [the pool/prewarm/lifecycle solution](../../solutions/performance-issues/prewarm-bounded-combat-presentation-pools-2026-09-13.md). Related startup and map-scope documents overlap only at the general performance topic; no consolidation or AGENTS.md change was needed.
