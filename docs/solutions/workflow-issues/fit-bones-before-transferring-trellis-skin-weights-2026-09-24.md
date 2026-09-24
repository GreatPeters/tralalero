---
title: Fit bones before transferring TRELLIS skin weights
date: 2026-09-24
category: workflow-issues
module: Rest-stop TRELLIS production and humanoid rigging
problem_type: workflow_issue
component: development_workflow
severity: high
applies_when:
  - Rigging generated humanoids with baked UV seams and open surfaces
  - Reusing a skeleton or skin weights from a visually similar generated body
  - Validating FBX and GLB animation exports
tags: [trellis, blender, skinning, bone-heat, rigging, animation, fbx, glb]
---

# Fit bones before transferring TRELLIS skin weights

## Context

The first rest-stop character passed static TRELLIS shape, texture and reduction review, but its first six rig candidates did not pass visual deformation review. Several had valid weight sums, 18 bones and nine moving exported actions while the forearm flattened into a strip or the vest followed the hand. A technically valid rig was not a finished character.

## Guidance

1. Inspect the skeleton inside the actual rest mesh in both front and side projections before changing the weight solver. The old donor surface matched the new surface closely (mean nearest-surface distance 0.00187 units), but that did not validate the donor bones. The donor torso was offset forward and sideways, while the elbow and wrist were too low. Transferring its weights faithfully preserved a bad fit.
2. Preserve the generated render mesh and UVs. Direct Blender bone heat failed on the open/seamed surface. A temporary closed voxel proxy at 0.025 units allowed bone heat to solve. Transfer its weights back by nearest triangle and barycentric interpolation, then remove the proxy. The original silhouette, triangles and texture coordinates remain intact.
3. Give coincident UV-seam vertices identical weights. Cache transferred weights by rounded rest position, keep at most four influences and normalize. A per-vertex color classification can silently separate weights at a UV seam.
4. Treat rigid stylized forms deliberately. In this compact mascot family, pin the upper head to Head and the lower shoe sole to its Foot bone. Root is a control bone, not a skin influence. These are authored choices for this body family, not universal proportions for every human.
5. Inspect the complete action extrema. First/middle/last samples miss the quarter-cycle walking and attack peaks. Render at 0, 0.25, 0.5, 0.75 and 1 of each action, and produce the real encoded animation.
6. Compensate ankle pitch for the thigh and shin rotations in the simple in-place walk. The old `-phase * 8` foot pitch tilted both soles upward, visually suggesting hovering even when one heel had a numeric minimum Z of zero. Set foot pitch to `-thigh_angle - shin_angle` for the authored flat-foot step. This does not implement foot IK or world-space locomotion.
7. Reopen both exported formats at the intended 30 fps. Check skinned-body ownership, UVs, materials, triangle budget, required bones, weight sums, non-static actions and evaluated world-space ground contact. The initial FBX lost the intended effect of a second pelvis vertical translation; this workflow authors vertical contact through Root alone.
8. Keep visual acceptance separate from generation and numeric validation. The gallery reads an explicit accepted-rig manifest and requires a passing visual receipt, a passing fresh-import receipt and an existing animation movie. Earlier rejected revisions stay outside the accepted manifest.
9. Compare exported deformation against the native blend, not just action counts and weight sums. `tools/compare-reststop-export-poses.py` checks joint positions and more than 512 deformed surface samples at five phases of every action after fresh FBX and GLB imports. The first seven accepted characters passed a 0.02 world-unit tolerance; their largest surface discrepancies were 0.00922–0.01128 units. This check includes fractional frames and does not claim bit-identical interpolation across formats. Its receipt is included in the final package gate.

## What did not work

- Geodesic nearest-bone weights with a misplaced skeleton still assigned garment areas to arms.
- Broad glove color/bounding-box overrides picked up neighboring vest and trousers and worsened tearing.
- A good surface-to-surface registration score was wrongly treated as evidence that the donor skeleton was anatomically fitted.
- Voxel heat alone improved weight smoothness but did not repair the misplaced donor bones.
- Ground minimum and action-count checks passed some visually bad candidates; they were necessary but insufficient.

## Evidence and limits

`H01-r8` and `H02-r2` were accepted after refitting the skeleton, using proxy heat and correcting sole pitch. Each has 18 bones, nine actions and a fully decoded 338-frame / 30 fps preview. Fresh FBX and GLB checks pass. Remaining minor source artifacts include the chef hat's gray marks and a small apron pinch at the widest arm pose. The fall is an authored cartoon motion supported by the oversized head, not a ragdoll simulation.

- Reproducible rig: `tools/rig-reststop-proxy.py` and `tools/reststop-rig-actions.py`.
- Independent checks: `tools/verify-reststop-rig.py`.
- Actual action render: `tools/render-reststop-rig-preview.py`.
- Rejected bone-fit evidence: `outputs/reststop-production-2026-09-24/rigged/H01-r6/bone-fit-diagnostic.png`.
- Accepted evidence: `outputs/reststop-production-2026-09-24/rigged/H01-r8/` and `H02-r2/`, including `visual-review.json`, `fresh-rig-validation.json`, `video-validation.json` and `animation-preview.mp4`.

The full 90-asset production was still running when this lesson was captured. These two accepted rigs do not establish acceptance of the other six characters or all props. Each needs its own generated-body, fit, motion and export review.

## Related

- [Imported timing and mesh ownership in Blender previs](verify-imported-timing-and-mesh-ownership-before-blender-previs-render-2026-09-23.md).
- [Rebuild enemy prefabs without stale scene rigs](../integration-issues/rebuild-enemy-prefabs-without-stale-scene-rigs-2026-09-23.md).

Captured with `ce-compound mode:headless`, with research performed sequentially under the repository's tool mapping. Existing AGENTS.md already makes `docs/solutions/` discoverable. No session-history or issue lookup was needed for the retained local evidence.
