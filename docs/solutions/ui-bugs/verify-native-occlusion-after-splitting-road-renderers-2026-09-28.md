---
title: Verify native occlusion after splitting road renderers
date: 2026-09-28
category: ui-bugs
module: Noryangjin camera and combined road scenery
problem_type: ui_bug
component: development_workflow
severity: high
symptoms:
  - "A saved scene and passing occlusion tests still showed an opaque roof hiding the shark"
  - "Protecting the walkable renderer also protected a tall wall bundled into the same mesh"
  - "Individual ceiling tiles faded as alternating strips instead of a coherent roof"
root_cause: logic_error
resolution_type: code_fix
tags: [unity, camera, occlusion, mesh, native-validation, pipeline, save-dialog]
---

# Verify native occlusion after splitting road renderers

## Problem

The screenshot-feedback pass required translucent buildings while preserving the visible walking deck. An initial implementation passed numeric opacity checks but the actual bridge-exit image still showed only roof panels, the health bar and sea.

## Symptoms

- Combined SR18 meshes included planks, long underwater supports and high crossbars. The floor-preservation guard skipped the entire renderer.
- A3.31m-wide ceiling tile could cover the shark without also intersecting the road six metres ahead. Requiring both made small tiles stay opaque.
- Fading only tiles hit by three narrow body rays left alternating opaque strips over the character.
- At the market exit the camera rose to12.7m while still behind the7.8m roof, putting the roof directly between camera and shark.

## What Didn't Work

- Extending the registered renderer list alone did not correct the geometry/decision mismatch.
- Projected screen-area thresholds per small tile missed a large aggregate occluder.
- A paused diagnostic moved from a distant fixture to the exit without letting visibility groups reactivate. Its renderer inventory omitted the roof that appeared during normal Play. The replacement diagnostic writes ray candidates during the actual capture callback.
- Copying the split mesh for every instance created416assets/146.83MB. That was unnecessary duplication, not a rendering requirement.

## Solution

`NoryangjinFeedbackV3Builder.SplitRoadScenery` separates only the affected low dock modules into floor and high scenery **rendering**. The original `MeshCollider.sharedMesh` remains the source mesh. `NoryangjinRoadScenerySplit.source` enables repeatable installation. Mesh assets are shared by original GUID/fileID and triangle partition; ten assets/3.53MB serve208instances. A saved-scene dependency check removes only unreferenced generated duplicates.

The opt-in feedback camera evaluates independent body/aisle rays rather than requiring simultaneous body and road occlusion. Ceiling tiles, backing and lighting are grouped per bay so a hit fades one coherent assembly. Hollow portals/shutter frames use their actual collision mesh to keep an open aperture from behaving like a solid AABB. Materials are temporary instances and restore when unbound; no walking collider is removed.

The market camera stays beneath the roof through the loading bay, then returns to the outdoor profile after clearing it. Fresh Play captures verify both the approach and the exit. Static opacity assertions are supporting evidence, not the visual acceptance gate.

The user's requested real Claude Code review also caught two composition regressions. A retained reversing truck still crossed the newly built warehouse, clipping its wall and tuna pallets; the final installer removes that incident. Two replacement floors shared the same top plane and visibly flickered even though all469support samples passed. The correction retains the collision top at0.1m, disables its renderer, and limits a separate visible surface to the entrance interval at0.111m. Collision continuity and coplanar rendering are different acceptance checks.

## Why This Works

The implementation distinguishes three independent contracts: visible walkable surface, blocking geometry, and camera occlusion. Classification by combined minimum Y or root position cannot represent all three. Splitting only the renderer, grouping the authored roof, and tracing real portal geometry preserve navigation while making the screen readable.

## Prevention

- Keep the small-tile regression in `NoryangjinFeedbackV3Tests`, existing uphill-road visibility tests, the native collider/source audit and469-point connector projection.
- Capture during settled native Play state, with camera pose and fade counts in the receipt. Never move a paused fixture and assume its culling state updated.
- Read nested `data.result.success`. A successful CLI envelope can contain an invalid filter/compilation failure. A timeout can also leave a completed, saved mutation; inspect state before replaying it.
- Before `run_tests`, verify a clean saved scene and wait for completion before issuing a builder mutation. Temporary model imports can dirty a scene even when their roots are later destroyed. The Unity test runner's `SaveModifiedSceneTask` may call `SaveCurrentModifiedScenesIfUserWantsTo`, leaving a hidden save prompt. Preserve scene recovery data before recovering a confirmed task-owned Editor; do not delete it or launch repeated Editors against one project.
- Do not weaken production route/quiet guards to make an isolated timer fixture pass. Label forced deadline expiry separately from full-course play.
- A character-root height is not necessarily the floor. Market bodies have an authored0.12m child offset. Compare evaluated runtime skin vertices against the actual projected floor, and preserve a failed fixture receipt when its reference plane was wrong. Measure the generic rig's hips response instead of assuming a1:1translation, or a correction can turn a floating death pose into an underground one.
- Record genuine action counters separately from idle-state resets. An animation command count can grow while no new bid is raised. Stop restarting an already-idle animator and keep the raw receipts associated with the exact run quoted in prose.

## Related Issues

- [Bullet lanes, reward height and actual debug settings](../workflow-issues/verify-bullet-lanes-and-reward-height-in-live-play-2026-09-28.md): moderate overlap in native evidence; this entry adds renderer partitioning and aggregate roof occlusion.
- [Branch validation beyond roots](../workflow-issues/validate-noryangjin-branches-beyond-trigger-and-root-position-2026-09-28.md).
- [Current report and preserved failed captures](../../../outputs/noryangjin-feedback-v3-2026-09-28/TEST-REPORT.md).

Recorded with `ce-compound mode:headless`, sequentially per AGENTS.md. No session-history lookup. The existing instruction file already exposes `docs/solutions/`; no discovery edit is needed.
