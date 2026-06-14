---
title: Verify L-shape mesh crop intent before export
date: 2026-06-12
category: workflow-issues
module: Unity MeshyAI asset editing workflow
problem_type: workflow_issue
component: tooling
severity: low
applies_when:
  - "Cutting a straight piece out of an L-shaped Unity FBX module"
  - "User marks a screenshot with the region to remove or keep"
tags: [unity, blender, fbx, meshyai, noryangjin, asset-cropping]
---

# Verify L-shape mesh crop intent before export

## Context

While creating a new straight piece from `048_STAGE01_NRY_ROAD_040_Noryangjin_modular_right_90_timber_road_module`, the first crop preserved the upper horizontal arm. The user actually wanted the opposite: exclude the upper arm and keep the lower vertical section as the new straight module.

## Guidance

For L-shaped road modules, resolve the requested crop into an explicit keep range before exporting. Use the imported FBX bounds to identify the turn point, then state whether the crop keeps values above or below that plane.

For the 048 right-turn module, the source bounds were approximately:

```text
x: -0.2778 .. 0.2779
z: -0.4998 .. 0.4997
turn threshold: z = 0.15
```

Keeping the lower straight section meant bisecting at `z = 0.15` and preserving `z <= 0.15`. Keeping `z >= 0.15` instead produces the upper horizontal arm, which is a different asset.

After cutting, verify the exported FBX before creating the Unity prefab:

```text
bounds_z should end at the cut plane
materials should include the original material and the cut-cap material
Unity console should have no import errors after Assets/Refresh
```

## Why This Matters

Screenshot annotations can describe either the area to keep or the area to remove. For asymmetric L modules, that ambiguity produces a valid-looking but wrong asset. A numeric keep range gives the work a checkable contract before Unity imports the result.

## When to Apply

- Creating a straight, corner, or partial road module from an existing L/T/cross road FBX.
- Replacing or duplicating MeshyAI FBX-backed Unity prefabs.
- Filling cut faces after Blender bisect operations.

## Examples

Wrong interpretation: keep the red-marked upper horizontal region and export `top_straight_3grid`.

Corrected interpretation for this task: remove that upper region and export `bottom_straight_3grid` from the lower vertical section.

## Related

- [Preserve MeshyAI prefab axis correction in scene builders](preserve-meshyai-prefab-axis-correction-in-scene-builders-2026-05-25.md)
- [Repair Unity assets when editor command execution is blocked](repair-unity-assets-when-editor-command-path-is-blocked-2026-05-24.md)
