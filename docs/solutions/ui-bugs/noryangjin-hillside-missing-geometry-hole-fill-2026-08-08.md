---
title: Fill missing geometry in the Noryangjin distant hillside
date: 2026-08-08
last_updated: 2026-08-08
category: docs/solutions/ui-bugs
module: Unity Noryangjin environment
problem_type: ui_bug
component: tooling
symptoms:
  - "A visible open hole appeared beneath the distant Noryangjin hillside village."
  - "A double-sided material did not hide the gap because geometry was missing."
root_cause: logic_error
resolution_type: config_change
severity: low
tags: [unity, noryangjin, environment-art, missing-geometry, fbx, hole-fill, flatkit]
---

# Fill missing geometry in the Noryangjin distant hillside

## Problem

`Prop_019_STAGE01_NRY_BG_003_Distant_hillside_village_module_X-444_Z-379`
had an opening that exposed the background beneath the village. The defect was
in the mesh coverage rather than its shader: no triangles spanned the visible
gap.

The repair needed to look natural from the gameplay camera without modifying
the reusable source FBX or adding gameplay collision.

## Symptoms

- The hillside silhouette contained a conspicuous open gap.
- Rendering the source material double-sided left the gap visible.
- Early procedural rocks appeared at the wrong scale and orientation after
  export.
- Reusing the source atlas material produced patchwork colors on generated
  meshes whose UVs did not match the atlas.

## What Didn't Work

- Setting the source material to double-sided rendering (`_Cull 0`) only
  exposed the back faces of existing triangles; it could not replace absent
  geometry.
- Placing procedural spheres using unverified Blender coordinates produced an
  oversized, incorrectly oriented cluster after Unity import.
- Assigning the source atlas to those meshes produced unrelated texture
  fragments because their generated UVs were incompatible with the atlas.
- Judging placement from the root pivot alone missed vertex rotation, scale,
  and offset introduced by the import transform.

## Solution

Keep the original hillside FBX unchanged and add a separate repair asset:

1. Create
   `019_STAGE01_NRY_BG_003_Distant_hillside_village_module_HoleFill.fbx`
   with one shallow, irregular faceted rock face and four low-poly boulders.
2. Place the imported prefab in `Noryangjin_MapTool_Mode` as
   `Prop_019_HoleFill_RockCluster_X-444_Z-379`, anchored to the target module.
3. Overlap the faceted face slightly behind the existing shoreline and
   building geometry. Use the boulders to break up its exposed edge instead of
   trying to align two silhouettes exactly.
4. Assign the dedicated FlatKit material
   `019_STAGE01_NRY_BG_003_Distant_hillside_village_module_HoleFillRock.mat`
   with subdued purple-gray tones that match the distant hillside.
5. Mark the root and children static and omit colliders because the repair is
   decorative and outside the playable interaction area.
6. Validate the combined renderer bounds, then inspect both Scene view and the
   intended Game view. Confirm the gap is covered, the patch remains mostly
   hidden, and Unity reports no missing-reference or import errors.

The final hierarchy is intentionally small:

```text
Prop_019_HoleFill_RockCluster_X-444_Z-379
├── HoleFillRockFace
├── HoleFillRock_01
├── HoleFillRock_02
├── HoleFillRock_03
└── HoleFillRock_04
```

## Why This Works

The shallow face supplies the triangles that were actually missing. A small
intentional overlap prevents camera-visible seams, while the loose boulders
make the boundary read as a natural rock outcrop. Keeping the patch in a
separate FBX preserves the authoritative source asset for later reimport, and
the dedicated material avoids assumptions about the source atlas UV layout.

This pattern is suitable for distant decorative defects viewed from a limited
camera range. Repair the authoritative model instead when the opening affects
collision, navigation, close-up inspection, or many viewing angles.

## Prevention

- Use double-sided rendering only as a diagnostic for backface culling; if the
  background remains visible, inspect topology and add geometry.
- Verify imported renderer bounds and orientation immediately after export.
- Inspect the intended gameplay camera, not only an editor perspective view.
- Preserve reusable source FBXs and materials when a scene-local additive
  repair is sufficient.
- Use a dedicated material unless newly authored UVs intentionally match an
  existing atlas.
- Prefer slight hidden overlap over exact edge-to-edge contact for decorative
  environment patches.

## Related Issues

- [Scope map optimizations to scene instances](../logic-errors/scope-map-optimizations-to-scene-instances-2026-07-30.md)
- [Trim Unity bridge rope meshes by whole-triangle bounds](../logic-errors/trim-unity-bridge-rope-mesh-by-whole-triangle-bounds-2026-06-22.md)
- [Preserve MeshyAI prefab axis correction in scene builders](../workflow-issues/preserve-meshyai-prefab-axis-correction-in-scene-builders-2026-05-25.md)
- [Safely bake static Unity map scenes before retiring generators](../workflow-issues/safely-bake-static-unity-map-scenes-before-retiring-generators-2026-07-27.md)
