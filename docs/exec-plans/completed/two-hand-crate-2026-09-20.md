# FatMan two-hand crate correction

Status: completed. [Implementation, measured results and native video](../../../map-concepts/two-hand-crate-2026-09-20/README.md).

User rejected the previous single-hand placement because the crate crossed the body. Explicit latest requirement: hold it with both hands.

Root causes established from native rig/mesh measurements:
- Crate parented to one hand followed that hand's swing through the torso.
- A wrist/one-point distance was not a measurement of the visible palms or the body mesh.
- Generic Arrow2 launch alignment rotated the crate 90 degrees, changing its rear extent at release.
- FatMan's short arms require reachable handle targets and a small shoulder reach; arbitrary Unity IK targets drifted on the layered rig in earlier attempts.

Implementation: independent model-space crate, fixed shallow basket dimensions, both arm chains solved against palm-centred grips after facing/grounding, simulation-clock wind-up and release recovery. Preserve crate orientation at launch; arrows keep their axis correction. Restore authored arm locals before animation to prevent accumulation when culled.

Verification complete: 4 geometry tests (256 sampled poses), 13 combat regressions, both assemblies and agent harness pass. Native 210-frame front/side video contains three launches, no body/head vertices inside the held or just-launched crate, minimum measured torso separation .080m and visible palm errors below .000024m. The actor does not move and the crate does not rotate at release. Gallery/video verified in Chrome; Editor restored to saved SR18 Edit Mode. Previous prefab/scene captured under `map-concepts/two-hand-crate-2026-09-20/before/`.

Review correction: the first re-enable frame could finish rebinding after pose work, leaving empty-hand alignment. The prop now waits for the rig-settling frame before becoming visible. Idle and attack-only synthetic tests alone did not catch this lifecycle boundary; native per-frame measurement did.
