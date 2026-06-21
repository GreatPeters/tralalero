---
title: Trim Unity bridge rope mesh by whole-triangle bounds
date: 2026-06-22
category: logic-errors
module: Unity Noryangjin map tooling
problem_type: logic_error
component: tooling
symptoms:
  - "A generated bridge mesh lost end-post and rail geometry after rope cleanup."
  - "The visible dangling ropes were removed, but the bridge end looked broken in Scene view."
root_cause: logic_error
resolution_type: code_fix
severity: medium
tags: [unity, noryangjin, mesh, bridge, fbx, asset-generation]
---

# Trim Unity bridge rope mesh by whole-triangle bounds

## Problem
`Road_Bridge_X+384_Z+11` needed a copied bridge mesh with the dangling ropes cut off at both ends. The first generated copy used triangle center points as the cut test, which removed some triangles that crossed the intended cut plane and left the end structure visibly broken.

## Symptoms
- The generated bridge copy had missing or separated end geometry.
- Scene view still showed an incorrect-looking bridge even though the target rope region had been removed.
- Regenerating from the already-trimmed asset would compound the damage instead of recovering the authored mesh.

## What Didn't Work
- Deleting triangles by center point removed triangles whose centers were outside the cut range even when part of the triangle still belonged to the bridge end.
- Reusing the previous generated mesh as input risked carrying forward the broken topology.

## Solution
Regenerate from the original imported FBX mesh and only remove a triangle when the whole triangle is beyond the cut plane and outside the deck width.

```csharp
float minX = Mathf.Min(a.x, b.x, c.x);
float maxX = Mathf.Max(a.x, b.x, c.x);
float minZ = Mathf.Min(a.z, b.z, c.z);
float maxZ = Mathf.Max(a.z, b.z, c.z);

bool outsideBridgeEnd = maxX < -34f || minX > 0.8f;
bool outsideDeckWidth = minZ < -1.45f || maxZ > 1.45f;
return outsideBridgeEnd && outsideDeckWidth;
```

The generated asset can keep the same `.meta` GUID so scene references stay stable, but the `.asset` content should come from a fresh run against `SM_Bridge_Rope_Small_Fantasy.fbx`.

## Why This Works
A triangle that crosses the end cut plane can visually carry the post, rail, or boundary rope connection. Center-point clipping treats that triangle as disposable when its centroid lands outside the range. Whole-triangle clipping preserves crossing triangles and deletes only geometry that is entirely beyond the intended cut.

For this bridge, the corrected generation removed 2165 triangles from the original mesh and left 71 cut-crossing triangles in place. Validation then found `0` remaining triangles that were both fully outside the end plane and outside the deck width.

## Prevention
- When trimming Unity mesh assets programmatically, define cut predicates in terms of min/max vertex bounds unless the cut operation also creates new cap geometry.
- Keep generated mesh utilities pointed at the original imported source asset, not a previous generated result.
- Validate generated mesh assets by parsing the saved index and vertex buffers for the exact geometric predicate, not only by visual inspection.

## Related Issues
- [Verify L-shape mesh crop intent before export](../workflow-issues/verify-l-shape-mesh-crop-intent-before-export-2026-06-12.md)
- [Run Unity scene generation through CLI connector exec when editor reload is stale](../workflow-issues/run-unity-scene-generation-through-cli-connector-exec-when-editor-reload-is-stale-2026-06-21.md)
