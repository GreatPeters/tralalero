# Larger seagull and spinning collision response

> The reaction remains current. The next user request further increases the bird by 30% and warning diameter by 20%; see [the size follow-up](../seagull-size-followup-2026-09-20/README.md). Videos below retain this iteration's earlier dimensions.

The user requested another 50% increase to the bird, a 30% increase to the black shadow, and a collision where the shark spins while the bird is knocked away spinning.

## Applied

- Bird rig and inherited contact shapes: scale 2.1 → 3.15 (+50%). Landing offset scales to 0.1125m.
- Warning diameter: maximum 2.4m → 3.12m (+30%); initial diameter 1.8m → 2.34m. The dark material is preserved.
- The prior 28m warning, 1.0s wait, 0.4s descent and grounded wait for the runner to pass are preserved.
- On one accepted contact, disable all 13 contact shapes and cancel this hazard's descent/ordinary departure coroutines. Then start the two independent reactions: the existing 720-degree shark spin and a new 1.1s outward/upward bird arc with 1080 degrees of rotation.
- The bird travels 7.5m horizontally along a direction biased away from the impact and to the side. It rises roughly 4.56m before leaving. It cannot inflict another hit during this motion. Damage remains 20% of maximum player HP, once.
- During the impact arc, sample the authored `Fly` state's spread-wing pose and freeze the Animator so the rotating silhouette remains legible. The actual clip has no root-transform curves. Reinitialization enables the Animator again; the next descent restores road-facing rotation before revealing the bird.

`tools/resize-seagull-hazard.cs` saved the source prefab and SR18 placement through native Unity APIs; `before/` retains the prior serialized assets. The collider regeneration tool uses the same landing offset.

## Verification

All 14 `SeagullContactTests` passed. These cover the new relative sizes, shadow contrast, current timing, footprint tolerance, rotated-road passing rules, outward arc/continuous three-turn motion, contact ownership, damage and resets. Because the requested bird growth exceeds the requested shadow growth, the sampled wing-tip allowance is at most 0.1m beyond the warning radius; grounded body contact and real dodge cases remain verified.

Both C# project builds and the agent-harness validator passed. Runtime retains the pre-existing ithappy `SplineSpeed.m_LastIndex` warning. Chrome playback verified the normal and half-speed videos, and the gallery tab was retained. Unity ended in saved SR18 Edit Mode.

`play-v2/report.json` contains four real physical contact/dodge cases and a separate injected mid-descent transition/pause/retry probe:

| Measurement | Result |
| --- | --- |
| Center and landed contact | 500 → 400 HP once |
| Shark rotation | 720 degrees |
| Bird rotation | approximately 1080 degrees |
| Bird horizontal travel / peak rise | 7.50m / 4.56m |
| Enabled contact shapes after impact | 0 |
| Left/right dodge | 500 HP, no shark or bird hit spin |
| Landing lead | approximately 15m ahead |
| Mid-descent hit handoff | rises 2.59m; canceled descent does not snap it back |
| Paused bird position / rotation / shark rotation change | 0 / 0 / 0 |
| Retry | hidden until descent, hit state cleared, normal orientation, Animator and 13 shapes restored |

No native console errors occurred during the final probe. The first `play-v1` capture retained normal wing flapping during the tumble, which made the silhouette collapse into hard-to-read shapes. `play-v2` uses the spread-wing impact pose and is the final visual evidence.

## Inspect and reproduce

- `tools/verify-seagull-impact-tumble.cs`: native gameplay capture, rotation/path metrics and interruption checks.
- `python tools/build-seagull-impact-gallery.py`: validates outcomes, publishes the complete six-second normal recording and a labeled half-speed impact excerpt. Both use captured frames without interpolation.
- [Normal and slow-motion impact gallery](http://127.0.0.1:6753/seagull-impact-tumble/).
- Preview PNG/video copies: `tmp/image-previews/seagull-impact-tumble-2026-09-20/`.

Verification covers the current SR18 placement and source prefab in the Editor; mobile-device performance was not tested.
