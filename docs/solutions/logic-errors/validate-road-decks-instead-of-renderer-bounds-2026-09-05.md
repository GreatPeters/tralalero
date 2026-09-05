---
title: Validate actual road decks instead of support-inclusive renderer bounds
date: 2026-09-05
category: logic-errors
module: Noryangjin SR18 road geometry
problem_type: logic_error
component: tooling
symptoms:
  - "SR18 road corners visibly disconnected despite passing renderer and collider AABB overlap tests."
  - "Uphill and downhill used the same orientation and one exaggerated stair instance for an entire height change."
root_cause: missing_validation
resolution_type: code_fix
severity: high
tags: [unity, map-tool, road-deck, mesh, collider, pivot, geometry, regression]
---

# Validate actual road decks instead of support-inclusive renderer bounds

## Problem

The 230-road SR18 scene passed its placement, counts, crossing-height, and AABB seam assertions, but the user found disconnected L corners. The approved coordinates described the prefab pivots rather than the centerline of the visible wooden deck. Poles extended outside the deck and made those disconnected meshes appear to overlap in bounds tests.

## Symptoms

- The Basic, LeftTurn, and RightTurn prefabs all use the same straight `SM_Pier_Long_Fantasy` mesh. A turn label does not imply an L-shaped mesh.
- The deck center is near local X=-1.5, while the pivot is at one edge. Yaw changes move that offset to a different side of the intended route.
- Original renderer bounds extend to local Z=-5.3286, but downward rays at the deck center miss from Z=-5.0 through -5.3. Usable planks reach approximately -4.95.
- Scaling one stair mesh by Y=24.3 created excessive step heights. The downhill prefab's authored orientation had also been overwritten with the same yaw-only transform as the uphill.

## What Didn't Work

- Checking three-dimensional AABB separation still accepted a corner where only support geometry overlapped.
- A first correction using the support-inclusive 5.3 length left gaps near each straight seam. Surface samples exposed the wrong length immediately; a direct scan of the original plank center established 4.95 as the useful length.
- Extending both intersecting decks through the entire corner removed the hole but created overlapping plank surfaces. Trimming the incoming piece to meet the full-width outgoing piece removed that visual artifact.

## Solution

The source Map1 and existing Map2 assets remain unchanged. SR18 uses six derived, shared meshes under `Assets/ShooterSurvival/Models/Generated/SR18RoadDecks/`, retaining original prefab connections and materials. Each road's MeshFilter and MeshCollider reference the same derived mesh.

The extension starts at the copied endpoint's `TransformPoint(-1.5, 0, -4.9)`, not its root pivot. Its 179 logical modules and ten turns still follow the selected SR18 spec at pitch 11.25. The placement report's `target.routeOriginWorld` records that distinction from `source.endpointWorld`.

Derived meshes start from the original pier's vertices, triangles, UVs, and materials. Root scale remains `(2.2, 4, 2.24)`. For a normal deck or ramp, the local geometry is transformed as follows:

```csharp
v.x += 1.5f;
float worldZ = v.z / 4.95f * (length + 0.24f) + 0.12f;
v.z = worldZ / 2.24f;
v.y = (shortPillars ? Mathf.Max(v.y, -0.5f) : v.y)
    + rise * (worldZ / length) / 4f;
```

`length=11.25` for normal decks and ramps. The corner's outgoing piece uses `length=14.55`, extending behind the logical corner by half the 6.6-wide deck. Its incoming piece instead maps to length 7.95 and subtracts 3.3 from worldZ, so the two surfaces meet rather than occupy the same square. Normals and bounds are recalculated after vertex changes.

Flat elevated decks sit at Y=12. Each ascent/descent spans three road roots, with rise +4/-4 per root. The four runs are 70–72, 87–89, 151–153, and 156–158; the flat upper crossings lie between them. Shortened elevated supports preserve clearance above lower roads. This changes the new-road mix to Basic157/Uphill6/Downhill6/RightTurn8/LeftTurn2 without changing the 230-root total.

## Why This Works

Logical topology and geometric connectivity answer different questions. Reconstructing the route spec verifies the correct path; sampling the visible collider surface verifies that the path is actually covered by a deck. Requiring the renderer and collider to share the same mesh prevents an invisible filler collider from disguising a visible hole.

`Sr18RoadDecks_SupportThreeLanesAcrossEveryJoinAndRamp` samples the centerline and lanes 2 units either side at 0.25-unit intervals. Each ray must hit an upward-facing surface within 0.45 of its independently expected height. Only the local route neighbors are eligible, so another floor at a crossing cannot falsely supply the missing surface. Small ±0.06 longitudinal nudges tolerate the original plank mesh's tiny decorative cracks. The 24,702 samples failed on the original scene and pass after repair.

## Prevention

- Verify pivots, facing, deck ends, and actual mesh identity before treating palette labels as geometric contracts.
- Keep AABB checks as broad checks, and use actual surface tests for continuity.
- Inspect every corner at useful scale, plus side views of slopes and over/under crossings. Whole-map screenshots alone hide small but important gaps.
- Keep source-scene hashes separate from the editable target. Use before/after target hashes to prove tests do not save it; do not pin an obsolete broken target hash.
- After live geometry edits or script reload, confirm scene dirty state and save the authorized target before starting Unity tests. A pending save dialog can stall the Pipeline test command.

Verification: SR18 tests 4/4 passed, including 24,702 surface samples; Editor build passed; all ten new and five preserved corners, the source join, and crossings were visually checked. Runtime Y-following remains outside this authoring repair.

## Related Issues

- [Placement-specific continuation geometry](../design-patterns/continue-map-tool-layouts-by-selected-renderer-bounds-2026-07-19.md)
- [Protect active scenes from EditMode tests](../workflow-issues/protect-active-unity-scenes-from-broad-editmode-test-runs-2026-07-18.md)
- [Current SR18 scene contract](../../noryangjin-sr18-roads-scene.md)
