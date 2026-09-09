---
title: Verify Unity cosmetic previews and purchase-test recovery
date: 2026-09-10
category: workflow-issues
module: Noryangjin cosmetic shop
problem_type: workflow_issue
component: development_workflow
severity: medium
applies_when:
  - "Adding part cosmetics to an imported skinned model"
  - "Testing purchases through ephemeral Unity Pipeline scripts"
tags: [unity, cosmetics, preview, playerprefs, pipeline, tmp-font]
---

# Verify Unity cosmetic previews and purchase-test recovery

## Context

The original shark uses one skinned mesh and material for body and shoes. Whole-shark variants have incompatible geometry/UVs. A usable part shop required a preserved rig, independent visual materials, a head attachment and live purchase persistence. First previews exposed scale, shader, layout and font assumptions.

## Guidance

- Copy the original mesh, retaining weights, bind poses, UVs, normals and blend shapes; partition its triangles into two submeshes. Do not replace it with another skin's texture just because the filename looks related. Keep source FBX and colliders untouched.
- Inspect `BakeMesh` coordinates before applying transforms. This imported model's baked vertices already included the import scale; `TransformPoint` applied that scale again. Calibrate the hat against head-weighted baked vertices, then convert the chosen world socket back to head-local coordinates. Confirm both still and animated poses.
- FlatKit's main color is `_BaseColor`. `_Color` was absent. Copying the shark material onto a hat also copied its emission texture; use a fresh URP/Lit accessory material. Verify tint/emission together.
- Put a square AspectRatioFitter inside a bounded preview-area parent. On the area itself, FitInParent expanded to the full screen and obscured cards. Explicitly choose a Korean TMP font; the first available font was Latin-only. Use ScreenCapture for overlay UI; a camera-only Pipeline screenshot omitted it.
- Dispose hidden preview stages, RenderTextures and materials explicitly in Editor generation tools. Delayed `Destroy` means multiple equips in one frame can leave several mounts: hide all prior mounts before creating the next one.
- Snapshot purchase prefs before testing. `JsonUtility.ToJson` returned `{}` for a class emitted by `run_script`, despite a populated list. A successful tool return was insufficient. Use a verified primitive TSV/serializer round trip and reject an empty restore. The known initial values were recovered in this session; later tests restored all 44 captured keys through the corrected helper.
- After a recompile or scene reload, verify readiness/a completed frame before chaining dependent actions. A returned reload call does not certify Start/OnEnable completion.

## Why this matters

9999 damage masked an unrelated late-ramp encounter defect: a single shot could remove E23 immediately after landing, while ordinary shots needed more flat-road time. Repeated normal-stat runs died at the same coordinate. Moving that enemy from 3.42 to 21.75 units after the descent fixed the approach budget without reducing its health. Always keep boosted traversal and ordinary-stat balance evidence separate.

Static thumbnails alone cannot certify navigation, localized labels, currency debits, ownership, animated attachment or recovery of the user's test state. The live loop bought three independent parts, rejected a repeat debit, reloaded the scene, and completed SR18 wearing them.

## Verification and related records

`CosmeticInventoryTests` and `CosmeticShopIntegrationTests` pass 8 tests. The source workbook is validated separately from the exported candidate, and Data.bytes matches it. See [release record](../../../map-concepts/noryangjin-release-2026-09-10/README.md) and [scene-test isolation](protect-active-unity-scenes-from-broad-editmode-test-runs-2026-07-18.md).
