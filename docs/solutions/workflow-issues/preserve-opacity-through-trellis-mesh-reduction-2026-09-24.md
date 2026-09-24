---
title: Preserve opacity through TRELLIS mesh reduction and export
date: 2026-09-24
category: workflow-issues
module: Rest-stop TRELLIS prop production
problem_type: workflow_issue
component: development_workflow
severity: medium
applies_when:
  - A transparent prop becomes opaque after TRELLIS generation or Blender texture baking
  - Low-poly faces cross material boundaries that were precise on the high-detail source
tags: [trellis, blender, alpha, opacity, baking, glb, fbx, source-preservation]
---

# Preserve opacity through TRELLIS mesh reduction and export

## Context

P01's shield geometry passed, but the actual TRELLIS request used `texture_alpha_mode=OPAQUE`; its texture alpha also predicted nearly opaque values (253–255). The effective material could not reproduce the clear panes. An initial code inspection found forced opaque alpha in the vertex-color branch, but that was not the texture branch used by this job. Keep these separate facts explicit.

The corrected high source assigned alpha 0.18 only to pane faces and kept the rim, middle reinforcement and grip opaque. The canonical low mesh retained a good silhouette, but its PBR baking/export did not preserve opacity. Reclassifying the reduced faces created jagged triangular opaque patches. Direct reduction of the original UV-separated mesh produced fragmented geometry despite meeting the numeric triangle target. Both candidates were rejected and retained.

## Guidance

Preserve the accepted low geometry, UVs and PBR channels. Bake the high source's **opacity only** to the low mesh's existing atlas using an emission pass. Merge that value into BaseColor alpha without altering RGB, link the texture Alpha output to the shader Alpha input, and enable alpha blending. Do not globally change the user's saved generation settings to repair one material.

Use two phases when source-face classification samples existing material assignments: finish classification first, then assign materials. Mutating IDs while classifying caused P01-r1's lookup failure.

Verify both texture data and actual exported rendering:

- Compare original and corrected BaseColor RGB bytes; P01-r5 had zero changed values.
- Fresh-import both GLB and FBX; check the Alpha connection and presence of both clear and opaque pixels.
- Render contrasting colored panels behind the object. A dark studio background alone does not demonstrate transparency.
- Inspect all views and retain a source comparison, since passing mesh statistics does not establish visual quality.

## Why This Matters

Low-poly face boundaries are too coarse to reproduce every pane edge. A 2K alpha atlas retains that boundary without extra triangles or replacing the accepted geometry. The transparent and opaque regions survive both exports. This is an alpha-blended approximation; it does not promise physical refraction or identical appearance across importers.

## When to Apply

- A material property disappears during an otherwise acceptable reduction/bake.
- A face-level local repair creates visible triangular boundaries.
- A saved opaque generation setting must remain unchanged for the rest of the batch.

## Examples

P01-r5 uses the smooth canonical `manual/P01-r2/source/low1` mesh at **14,583 triangles**. `tools/bake-reststop-shield-opacity.py` preserves geometry/UV and all other maps. `tools/verify-reststop-shield-opacity.py` fresh-imports each format and creates `opacity-proof-glb.png`, `opacity-proof-fbx.png` and `transparency-validation.json`. Each import retained over 1.4 million clear-range pixels and 1.88 million opaque pixels. Actual colored-background renders confirmed transparent panes and opaque reinforcement.

The original AI ledger remains `review_needed`; selection is recorded separately in `final-overrides.json`. No AI shape/texture retries or additional canonical reduction attempts were consumed by the final alpha correction. Earlier failed candidates remain available under P01-r1 through P01-r4.

## Related

- [Foliage UV and corner-normal preservation](preserve-uv-seams-and-corner-normals-for-trellis-foliage-2026-09-24.md): useful for dense foliage, but not a universal hard-surface reduction method.
- [Material-region preservation](preserve-material-regions-when-repairing-trellis-normal-artifacts-2026-09-24.md): local material repair should retain unrelated accepted surfaces.

Captured with `ce-compound mode:headless`, sequentially under the project tool mapping. Existing solution discoverability instructions were sufficient; no instruction-file change was needed.
