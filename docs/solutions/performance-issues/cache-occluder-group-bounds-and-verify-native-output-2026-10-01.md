---
title: Cache occluder bounds without losing restoration or native visual correctness
date: 2026-10-01
category: performance-issues
module: Noryangjin camera and mobile asset pipeline
problem_type: performance_issue
component: development_workflow
severity: high
symptoms:
  - "The camera scanned about 1310 renderer groups every frame."
  - "A same-start microbenchmark spent about 10.03 ms inside occlusion processing."
root_cause: logic_error
resolution_type: code_fix
tags: [unity, mobile, occlusion, bounds, lod, rendertexture, profiling]
---

# Cache occluder bounds without losing restoration or native visual correctness

## Problem

The S22 report identified a large Editor CPU cost in camera occlusion. Shader changes alone would not remove the repeated renderer traversal. An optimization also had to preserve temporary transparency, moving groups, physical colliders, and restoration after leaving an area.

## Solution

`NoryangjinCameraOcclusion` caches a conservative bounds envelope in each authored group's local space. Each update transforms the eight-corner-equivalent envelope through the current matrix, compares it with the camera/player/45-metre look-ahead query, and visits renderers only for intersecting or previously affected groups. Bounds transforms use absolute basis-vector extents, so rotation and nonuniform scale remain conservative. A four-metre margin covers the limited authored child motion; arbitrary independent child movement still requires recaching.

An `affected` flag is essential: a group that leaves the query must continue receiving restoration updates until its previous fade or hidden state is restored. Otherwise optimization leaves scenery permanently translucent. The new moving/distant-group test exercises skip, entry and restoration. A same-start follow-up measured 0.790 ms inside the function, versus 10.03 ms before. These are PC Editor measurements, not S22 timings.

## What did not work

- A test receipt with nine passing cases was from an old loaded assembly. Explicitly recompile and check that the new tenth case ran. The focused-suite runner now archives prior receipts and reruns requested suites rather than silently accepting cached passes.
- Aggressive welded LOD2 meshes retained nominal bounds but visibly damaged roof, fish-sign and UV structure in native renders. Remove that level from all 320 installed groups; keep original plus the reviewed LOD1, without a distance cull. Triangle counts and unchanged bounds do not certify shape quality.
- FlatKit's pre-light vertex-color mode blends toward a shared shaded color, washing dark prop regions out. Inspect real shader code and native images, not only imported color arrays. Keep region color multiplied by lighting for manufactured props.
- Movie UI leaves its camera enabled and suppresses its world culling mask. Restore mask/clear flags/background on every exit. A decoder flag alone does not prove visible playback. In the first lifecycle fixture, CaptureScreenshot was followed by Skip in the same coroutine frame, so the deferred capture showed the lobby. Waiting through end-of-frame corrected that evidence error; those images did not establish the earlier camera-disable diagnosis.
- Immediate capture receipts can contain a stale image. A clean Editor restart recovered the backbuffer after temporary cinematic camera experiments; a domain reload did not. Visually inspect fresh composited frames before accepting a route capture. Preserve rejected runs separately.

## Prevention

1. Pair an optimization with an explicit restoration invariant and a measurable same-location comparison.
2. Review native LOD0/LOD1 silhouettes and UVs before deployment. Retain original assets and identify rejected candidates in the report.
3. Respect prefab axis corrections when staging assets; multiply scene yaw by the prefab's existing rotation.
4. For skinned edits, close actual geometric boundary loops and match boundary weights. Rewriting native Mesh buffers with `Clear` and explicit arrays avoided stale rendering observed with a previous serialization-copy approach.
5. Lift newly created TMP using its transform anchor: its renderer bounds can be unbuilt. Add the regression before changing all installed signs.
6. Snapshot current preferences before any Play bot. Do not restore an older session's save. Device tests, Editor measurements and coverage bots must keep separate labels.

## Related records

- [Audit evidence handling](../workflow-issues/qualify-build-and-capture-evidence-in-mobile-audits-2026-10-01.md): moderate overlap in evidence handling; this entry concerns the implemented cache/restoration and native optimization gates.
- [Prefab axis correction](../workflow-issues/preserve-meshyai-prefab-axis-correction-in-scene-builders-2026-05-25.md).
- [Native road occlusion](../ui-bugs/verify-native-occlusion-after-splitting-road-renderers-2026-09-28.md).
- Implementation receipts: `outputs/s22-polish-2026-10-01/`; original diagnosis: [S22 audit](../../reviews/s22-performance-visual-audit-2026-10-01.md).

AGENTS.md already requires searching this knowledge store. No additional instruction-file rule was needed.
