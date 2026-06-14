---
title: Preserve MeshyAI Prefab Axis Correction In Scene Builders
date: 2026-05-25
category: docs/solutions/workflow-issues
module: Unity scene generation workflow
problem_type: workflow_issue
component: tooling
severity: medium
applies_when:
  - "Instantiating generated MeshyAI prefabs through `PrefabUtility.InstantiatePrefab`"
  - "Building Unity layout scenes from repaired FBX-backed prefabs"
  - "A generated road, prop, or module appears upright, edge-on, or missing in the scene"
tags: [unity, meshyai, prefabutility, scene-generation, rotation]
---

# Preserve MeshyAI Prefab Axis Correction In Scene Builders

## Context
Stage01 Noryangjin auto layout generated the expected prefab instances, but the road looked missing and the scene showed mostly posts and rail pieces. The source `046_STAGE01_NRY_ROAD_038_Noryangjin_wet_straight_pier_road_module.png` clearly had a wet wooden floor, so the issue was in scene instantiation rather than the design reference.

The repaired MeshyAI prefab is itself a prefab instance of the FBX. Its root transform carries an axis correction, commonly `x = -0.7071068`, `w = 0.7071068`, to lay the imported FBX flat in Unity.

## Guidance
When an editor scene builder instantiates one of these prefabs, do not replace the prefab root rotation with a fresh yaw-only quaternion. Preserve the prefab's local rotation and multiply the desired yaw on top:

```csharp
instance.transform.SetParent(parent, false);
Quaternion prefabAxisCorrection = instance.transform.localRotation;
instance.transform.localPosition = position;
instance.transform.localRotation = Quaternion.Euler(0f, yaw, 0f) * prefabAxisCorrection;
```

Avoid this pattern for FBX-backed MeshyAI prefabs:

```csharp
instance.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
```

That overwrites the axis correction and can make floor/road assets stand upright or render edge-on.

## Why This Matters
The generated prefab can look correct in isolation while appearing broken only after automated scene placement. If the scene builder throws away the prefab's root transform correction, the asset's visual orientation changes even though the source prefab path, material, and scale are all valid.

This is especially confusing for road modules because the user-visible symptom is "there is no road," even though the correct road prefab was instantiated.

## When to Apply
- Building preview/layout scenes from `Assets/ShooterSurvival/Prefabs/MeshyAI`.
- Writing `PrefabUtility.InstantiatePrefab` helpers that set transform rotation.
- Patching `.unity` YAML fallback output for generated MeshyAI prefabs.

## Examples
In `Stage01NoryangjinAutoDraftBuilder`, the road instances should preserve the prefab correction. A quick scene YAML check for the first road segment should show the axis correction:

```text
value: 046_stage01_1_wet_pier_floor_00
propertyPath: m_LocalRotation.w
value: 0.7071068
propertyPath: m_LocalRotation.x
value: -0.7071068
```

The same scene should not include visible helper labels such as `Stage01_1 Straight Pier` when the goal is to match a concept reference.

## Related
- [Create Unity Layout Scenes When Editor Execution Is Blocked](create-unity-layout-scene-when-editor-execution-is-blocked-2026-05-25.md)
- [Repair Unity Assets When Editor Command Path Is Blocked](repair-unity-assets-when-editor-command-path-is-blocked-2026-05-24.md)
