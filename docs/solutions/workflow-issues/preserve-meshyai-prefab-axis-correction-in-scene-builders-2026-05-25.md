---
title: Preserve MeshyAI Prefab Axis Correction In Scene Builders
date: 2026-05-25
last_updated: 2026-10-02
category: docs/solutions/workflow-issues
module: Unity scene generation workflow
problem_type: workflow_issue
component: tooling
severity: medium
applies_when:
  - "Instantiating generated MeshyAI prefabs through `PrefabUtility.InstantiatePrefab`"
  - "Building Unity layout scenes from repaired FBX-backed prefabs"
  - "A generated road, prop, or module appears upright, edge-on, or missing in the scene"
tags: [unity, meshyai, fbx, prefabutility, scene-generation, rotation, native-camera, bounds]
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

## Chapter 4/5 recurrence and recovery (2026-10-02)

The locally refined sneaker crown passed isolated asset review but failed in native portrait gameplay. Its imported root carried an approximately 270-degree X correction and a scale of 100. The scene installer replaced the root rotation with yaw only, placing the sneaker on its side and shifting its apparent center. In the tower, the wrongly oriented shell covered a large portion of the camera view. An asset preview and a successful scripted gameplay clear did not establish scene visual quality.

The repair in `tools/repair-chapters45-crown-axes.cs` restores the source meshes and imported root transform before applying scene yaw. It normalizes size from actual transformed renderer bounds, centers the combined bounds in X/Z, and places the bottom at the anchor height. The interior camera cutaway tests transformed vertex centroids in world space; a source-local Y threshold would select the wrong faces after the FBX axis correction. LODGroup bounds are recalculated after geometry edits. The retained source installer must apply the same rule so a later rebuild does not reintroduce the defect.

Use this order when placing generated or locally refined FBX scenery:

1. Inspect the imported prefab root rotation and scale in Unity, even when a Blender or GLB preview looks correct.
2. Preserve that transform and compose the intended scene yaw.
3. Measure world bounds after parenting and orientation; derive scale and placement from those bounds.
4. Transform vertices before applying a world-space cutaway or clearance rule.
5. Check each LOD and inspect the actual portrait camera at approach, interior combat and goal pickup.
6. Keep source repair, native scene test and screenshot evidence together; do not rerun an old scene builder over later refinements.

Evidence for this occurrence is under `outputs/chapters45-2026-10-02`: `play/20261001-190818-232-ShoeTower-0/020-245.1-route.png` and `026-296.6-end-clear.png` expose the pre-repair problem; `qa/crown-native-axis-repair.txt` records repaired bounds. Native integration tests verify three bounded LODs and upright imported axes for both chapter crowns and the small offering. Post-repair visual approval must cite later native captures, not the earlier asset preview.

This supplements the existing prefab-axis rule; it does not authorize replacing a poor model with placeholder geometry or claiming automated inputs prove human enjoyment.

## Related
- [Fit generated FBX in parent space before scene installation](../integration-issues/fit-generated-fbx-in-parent-space-before-scene-installation-2026-09-25.md)
- [Verify native occlusion after splitting road renderers](../ui-bugs/verify-native-occlusion-after-splitting-road-renderers-2026-09-28.md)
- [Create Unity Layout Scenes When Editor Execution Is Blocked](create-unity-layout-scene-when-editor-execution-is-blocked-2026-05-25.md)
- [Repair Unity Assets When Editor Command Path Is Blocked](repair-unity-assets-when-editor-command-path-is-blocked-2026-05-24.md)
