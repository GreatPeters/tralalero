# Stable, synchronized FatMan crate throw

The user rejected the preceding two-hand throw because the lines/arms shook and the crate flew before the visible throw. Its spatial grip tests had not established acceptable motion. This correction updates the source FatMan prefab and all six SR18 placements while preserving the earlier ship, seagull and combat changes.

## Causes and correction

- The imported goalie throw kept changing arm twists while a separate late solver pinned the hands. A serialized quiet reference now supplies the entire living pose. One controller owns dip, forward stroke, both grips and recovery; the original Animator handles death.
- The old .018-model-unit forward motion was barely visible and had an independent .62-second launch timer. The new stroke moves .16 model units (0.33749m in SR18), then requests launch after the final `LateUpdate` pose. An expired timer without that pose cannot release. A consumed flag prevents duplicate launch.
- Detachment preserves the completed carry transform, including rotation. The Guard's Arrow2 timer/axis handling remains separate.
- The legacy `FlatKit/Stylized Surface With Outline` skin material produced internal black triangles during bending. A same-pose native comparison isolated that artifact. `FatMan_StableSurface.mat` uses current `FlatKit/Stylized Surface`, preserves the original texture/color and disables the hull outline. The crate's separate URP/Unlit material is untouched.

## Verification

Passed 52 focused Edit Mode tests: `FatManTwoHandPoseTests` (6), `EnemyEventControllerTests` (24), `CombatRouteFeedbackTests` (13) and `EnemyThrowDirectionTests` (9). These include four headings, sampled baked-body clearance, imported-animation invariance, source texture/color preservation and timer-before-pose rejection. Both C# project builds and `tools/validate-agent-harness.ps1` passed. The runtime build retained the pre-existing ithappy `SplineSpeed.m_LastIndex` warning.

`play-v1/report.json` records three actual native throws across 240 frames per view, at 30 fps:

| Measurement | Result |
| --- | --- |
| Early projectiles | 0 |
| Final-pose launch progress | 1.0 for all three |
| Handoff position / rotation jump | 0m / 0 degrees |
| Maximum palm-grip error | 0.0000178m |
| Resting crate / elbow jitter | 0m / 0m |
| Sampled held / first-flight torso penetration | 0 / 0 |
| Forward crate stroke | 0.33749m |
| Imported Animator enabled while alive | 0 frames |

The native pause probe held throw progress at 0.376344 with no projectile launch. The death probe entered `Dead` and re-enabled the original Animator. Native front and side images show the completed push before flight and removal of the internal black lines. No native console errors were observed.

## Evidence and reproduction

- [Normal and half-speed front/side gallery](http://127.0.0.1:6753/synced-crate-throw/). The 8-second normal video and 16-second half-speed video contain the complete native timeline, with no interpolation or omitted startup frames.
- Native data: `play-v1/report.json`, `play-v1/frames.json`, and front/side PNG sequences.
- Preview copies: `tmp/image-previews/synced-crate-throw-2026-09-20/`; gallery mirror: `tmp/image-previews/bonus-gallery-2026-09-19/synced-crate-throw/`.
- `tools/install-synced-crate-throw.cs`: run through official Unity `run_script` in clean SR18 Edit Mode; capture the source Idle reference, author the prefab and six placed actors, preserve authoring transforms and save.
- `tools/record-synced-crate-throw.cs`: native Play Mode probe, isolating unrelated player weapons/coroutines and recording both views, launch metrics, pause and death.
- `python tools/build-synced-crate-gallery.py`: verifies report gates and encodes the native timeline using imageio-ffmpeg; refuses to overwrite existing evidence.
- Baselines: `before/` contains the prior SR18 scene, FatMan prefab and affected runtime scripts.

Chrome playback was verified for both videos, showing the native front/side frames and advancing time controls. The gallery tab was retained for review. Unity was restored to `Noryangjin_MapTool_Mode_SR18`, with `saved=True` and `playing=False`.

The palm/limb calibration belongs to this imported FatMan rig. Mobile-device performance and a complete chapter playthrough were not tested by this focused correction.
