# Smaller seagull landing warning and dodge space

> The later user request increases this candidate by 50% and improves warning contrast/lead time. Current values are in [the readability follow-up](../seagull-warning-readability-2026-09-20/README.md); this document retains the previous iteration's evidence.

The user rejected the road-wide landing shadow and marked small circles as the desired avoidable footprint. The saved SR18 warning reached 13.824m: a 10.24-unit sprite multiplied by the scene's 1.35 override. The source prefab had a 3.072m warning but a 5.06m animated wingspan. Fixing contact alone had left this oversized obstacle hard to evade.

## Applied

- The warning now grows from 0.6m to 1.6m diameter. The source prefab and the single placed SR18 hazard share those values.
- The bird rig scale changed from 5 to 1.4, giving an approximately 1.42m maximum wingspan. All 13 existing bone-local contact shapes shrink with the same rig, preserving visual contact alignment. Landing offset is 0.05m.
- Removed the old 1.35 override from `NoryangjinRefinement`, corrected component defaults for the existing shadow sprite, and aligned the collider regeneration tool's landing offset.
- The trigger distance, warning/drop durations, one-hit limit, two-turn spin and maximum-health 20% damage remain unchanged.

## Verification

Two new tests initially failed on the real prefab and saved placement at warning widths 3.072m and 13.824m. After native asset installation, all 10 `SeagullContactTests` passed, including sampled sitting/flying vertices inside the warning circle. `Sr18RuntimeFixSceneTests.Seagull_DeliversMovingColliderContactsToTheDamageOwner` also passed.

An unrelated existing test in that suite, `ExitAndLegacyScenery_AreSafeWithoutChangingRoadGeometry`, still expects 230 road children. Both the pre-change backup and final scene contain 236 (`road-count-baseline.json`). This correction did not change roads or weaken that assertion.

Both C# project builds and the agent-harness validator passed. Runtime retains the pre-existing ithappy `SplineSpeed.m_LastIndex` warning.

The native `play-v2/report.json` contains four physical gameplay probes:

| Case | Result |
| --- | --- |
| Approach without steering | 500 → 400 HP; one contact; 720-degree spin |
| Steer left after warning | 1.49986m offset; 500 HP; no contact or spin |
| Steer right after warning | 1.49986m offset; 500 HP; no contact or spin |
| Touch after landing | 500 → 400 HP; one contact; 720-degree spin |

The probe invokes the same `PlayerMove` method used by keyboard/touch input after a five-frame reaction delay, retaining real forward movement and physics. Each video includes all 180 captured frames at 30fps. Maximum measured shadow diameter was 1.5999999m. No native console errors occurred during these probes.

The first probe's 1.8m right offset placed the player's outer edge outside the narrow approach strip. The final offset is 1.5m. `inspect-seagull-road-width.cs` checked the full body width (±0.7m) across the 18m encounter segment: no samples extend beyond the road, allowing for the authored mesh seams of at most 0.15m. Raw point-ray misses still occur at those seams on the center path as well, so the retained reports do not equate them to off-road movement. No road geometry was changed.

## Reproduce and inspect

- `tools/resize-seagull-hazard.cs`: official Unity `run_script`, clean SR18 Edit Mode; backs up and saves prefab/scene through native APIs.
- `tools/verify-seagull-dodge-size.cs`: native Play Mode contact, delayed steering left/right and landed-contact recording.
- `tools/inspect-seagull-road-width.cs`: read-only road extent and mesh-seam inspection.
- `python tools/build-seagull-dodge-gallery.py`: checks outcome gates and publishes the captured PNG sequence without frame interpolation.
- [Actual dodge video gallery](http://127.0.0.1:6753/seagull-dodge-size/); local previews in `tmp/image-previews/seagull-dodge-size-2026-09-20/`.
- `before/` retains the previous serialized prefab and SR18 scene. `play-v1/` retains the first wider-steering probe; `play-v2/` is the final evidence.

Chrome visibly played both dodge videos with advancing frames; the gallery tab was retained. Unity was restored to saved SR18 Edit Mode (`saved=True`, `playing=False`).

The size calibration assumes the authored sprite/rig and unit-scale hazard root. Re-measure if either asset changes. This was a focused Editor verification, not a mobile-device playthrough.
