# Seagull lands before the runner arrives

> Timing from this pass is retained. Size and collision reaction are subsequently revised in [the larger-bird impact pass](../seagull-impact-tumble-2026-09-20/README.md).

The user rejected the preceding timing: the bird finished descending after the shark had reached/passed it. At the supplied video's approximately two-second moment, the bird should already be on the road.

The previous correction advanced the warning but also lengthened its wait to 2.1 seconds. Verifying an eventual hit did not establish that landing completed ahead of the runner. The fixed 0.6-second grounded hold also prevented simply advancing the landing: the bird could leave before the player arrived.

## Correction

- Warning begins at 28m instead of 21m.
- Pre-descent warning is 1.0s instead of 2.1s; descent is 0.4s instead of 0.5s.
- After a minimum 0.6s grounded hold, the bird stays until the runner has passed the impact point by 2m along the bird's road-facing direction. Lateral movement does not prematurely release it. An already-consumed contact allows departure; contact remains single-hit.
- The 2.4m maximum dark warning, rig/contact scale 2.1, original animations and 20%-HP/two-turn penalty are preserved.
- Source prefab and the SR18 placement were saved through native Unity APIs. `before/` retains their previous serialized state.

## Measured native results

`play-v1/report.json` records completed landing, signed remaining road distance, runner passing and departure, in addition to the prior damage/spin checks.

| Case | Complete landing | Departure/contact |
| --- | --- | --- |
| Center approach | 15.38m ahead | Touches the grounded bird; 500 → 400 HP once, 720-degree spin |
| Left dodge | 15.56m ahead, frame 55 | Leaves after the runner is 2.48m past; no damage/spin |
| Right dodge | 15.56m ahead, frame 55 | Leaves after the runner is 2.48m past; no damage/spin |
| Runner initially held 12m back | Remains grounded until contact at frame 84 | 500 → 400 HP once, 720-degree spin |

Moving warning onset was measured at 27.76–27.93m. The 30fps dodge capture shows the bird already grounded at frame 60 (2.0s), matching the requested reference moment. The center-contact probe starts with the normal first-run warmup, so its absolute frame number differs; the landing lead distance remains consistent. All full recordings contain 180 frames, with no retiming or omitted frames. No native console errors occurred during the probe.

All 12 `SeagullContactTests` passed, including rotated-road passing decisions. Both C# project builds and the agent-harness validator passed, retaining only the pre-existing runtime ithappy `SplineSpeed.m_LastIndex` warning. Chrome playback and the before/after gallery were verified; Unity ended in saved SR18 Edit Mode. This is focused Editor verification of the current runner speed and placement; arbitrary speed changes or mobile devices were not tested.

## Reproduce and review

- `tools/resize-seagull-hazard.cs`: native installation, clean SR18 Edit Mode.
- `tools/verify-seagull-early-landing.cs`: full native gameplay recording and lifecycle measurements.
- `python tools/build-seagull-landing-gallery.py`: gates landing before arrival, grounded contact and correct departure; encodes native after footage and retains the previous native before video.
- [Before/after videos and the 2-second comparison](http://127.0.0.1:6753/seagull-early-landing/).
- Local preview copies: `tmp/image-previews/seagull-early-landing-2026-09-20/`.
