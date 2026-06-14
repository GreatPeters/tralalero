---
title: Preserve prefab root transforms in Noryangjin map tool placement
date: 2026-06-02
category: docs/solutions/logic-errors
module: Unity Noryangjin map tooling
problem_type: logic_error
component: tooling
symptoms:
  - "Placed map-tool objects appeared with incorrect size or rotation."
  - "Registered MeshyAI prefabs had authored root rotations and scales that were lost after placement."
root_cause: logic_error
resolution_type: code_fix
severity: medium
tags: [unity, noryangjin, map-tool, prefabs, transform]
---

# Preserve prefab root transforms in Noryangjin map tool placement

## Problem
The Noryangjin map tool placed the correct registered prefabs, but some objects looked incorrectly sized or rotated after installation. The assets themselves already carried imported root transform corrections, so changing every palette entry would only hide the real issue.

## Symptoms
- Road and prop prefabs that looked correct as project assets appeared wrong after placement.
- MeshyAI prefab roots commonly used values such as `rotation.x = 270` and `scale = 100`, but placed instances lost those authored values.

## What Didn't Work
- Treating palette scale as an absolute replacement made registered assets depend on hand-entered values.
- Applying a yaw-only rotation directly to the instance root erased the prefab's import-axis correction.

## Solution
Preserve the prefab root transform and apply map-tool controls as deltas.

```csharp
instance.transform.rotation = BuildPalettePlacementRotation(
    prefab.transform.rotation,
    direction,
    placement.yawOffset);
instance.transform.localScale = BuildPalettePlacementScale(
    prefab.transform.localScale,
    placement.scale);
```

`BuildPalettePlacementRotation` multiplies the cursor yaw on top of the prefab's base rotation. `BuildPalettePlacementScale` multiplies the prefab's authored root scale by the palette scale multiplier.

## Why This Works
Unity imported FBX-backed prefabs often encode unit conversion and axis correction at the prefab root. Replacing that transform with a clean map-tool value changes the asset, even when the prefab path is correct. Keeping the root transform and applying yaw/scale as a delta preserves the imported asset contract.

## Prevention
- For editor placement tools, treat prefab root rotation and scale as authored defaults, not disposable values.
- Add tests that verify prefab base rotation is preserved and palette scale is a multiplier.
- When a placed asset looks wrong but the project prefab looks correct, inspect the placement helper before editing every registered prefab.

## Related Issues
- [Preserve MeshyAI Prefab Axis Correction In Scene Builders](../workflow-issues/preserve-meshyai-prefab-axis-correction-in-scene-builders-2026-05-25.md)
