---
title: Flatten Road Prefabs As Surface Skins For Unity Runner Previews
date: 2026-05-27
category: docs/solutions/design-patterns
module: Unity stage generation
problem_type: design_pattern
component: tooling
severity: medium
applies_when:
  - "A stage ROAD prefab has the right floor texture but includes rails, posts, or side clutter"
  - "A runner camera needs one continuous playable path rather than separate road modules"
  - "A concept-matching scene should use the stage asset folder without turning the route into an asset showroom"
tags: [unity, road-prefabs, scene-builders, noryangjin, visual-regression]
---

# Flatten Road Prefabs As Surface Skins For Unity Runner Previews

## Context

Stage01_2 Noryangjin needed to look closer to `stage_01_2_noryangjin_concept_batch_v1`: a wet wooden pier lane between tall fish-market storefronts, with water and boats visible beyond the path. A purely procedural deck fixed the length/readability problem, but it lost some of the stage-specific ROAD texture. Instantiating ROAD prefabs as full modules was also wrong because their rails and posts read as center-lane clutter in the runner camera.

## Guidance

Use a continuous procedural base for collision and overall route shape, then place ROAD prefabs as flattened visual surface skins. Keep the imported prefab axis correction, fit the prefab to the desired footprint, then apply non-uniform local scale after fitting.

For the Stage01_Noryangjin ROAD prefabs, local axes after the imported rotation make `localScale.y` useful for length and `localScale.z` useful for flattening visible height. The builder now uses `046`, `047`, and `048` as skin layers on top of `Stage01_2_Continuous_Concept_Pier`, rather than using them as full road modules.

```csharp
GameObject instance = InstantiateStagePrefab(context, prefix, parent,
    position, yaw, name, targetMaxXZ, 0f, groundY, scaleMultiplier);

Vector3 scale = instance.transform.localScale;
scale.y *= lengthMultiplier;
scale.z *= heightMultiplier;
instance.transform.localScale = scale;
AlignBottom(instance, groundY);
```

Add tests for both object identity and scale relationship:

```csharp
Assert.That(sceneYaml, Does.Contain("value: 046_road_surface_skin_near_00"));
float roadSkinScaleX = ReadFirstScaleOverrideAfterName(sceneYaml,
    "046_road_surface_skin_near_00", "x");
Assert.That(ReadFirstScaleOverrideAfterName(sceneYaml,
    "046_road_surface_skin_near_00", "y"), Is.GreaterThan(roadSkinScaleX * 1.8f));
Assert.That(ReadFirstScaleOverrideAfterName(sceneYaml,
    "046_road_surface_skin_near_00", "z"), Is.LessThan(roadSkinScaleX * 0.35f));
```

## Why This Matters

ROAD prefabs often contain more than floor: railings, posts, curbs, signs, or module edges. In Scene view that can look like useful detail, but in a 9:16 runner view it can make the path look too short, broken, or blocked. Flattening lets the generated scene keep the prefab's material identity while the procedural base controls path continuity and playable width.

Non-uniform scaling must happen after the normal bounds fitting step. If it happens before `FitEnvelope`, the fit can erase the intended skin shape. If it happens before preserving prefab axis correction, the wrong local axis may be flattened.

## When to Apply

- The concept reference shows a continuous road, pier, or floor.
- The available ROAD prefabs have correct texture/detail but unwanted side geometry.
- The camera preview should look like gameplay, not a top-down asset lineup.
- A visual regression test can assert representative ROAD skin names and non-uniform scale.

## Examples

Before: repeated full `046/047/048` modules made the lane read as short disconnected chunks, with rail/post silhouettes competing with the center path.

After: `Stage01_2_Pier_Plank_Row_*` and `Stage01_2_Road_Playable_Collider` define the route, while `046_road_surface_skin_near_00`, `047_road_surface_skin_curve_04`, and `048_road_surface_skin_wide_foreground` add ROAD surface identity as flattened skins.

## Related

- [Prefer Stage Prefab Set Dressing Over Fake Helpers For Reference Matching Unity Scenes](prefer-stage-prefabs-over-fake-helpers-for-reference-matching-unity-scenes-2026-05-26.md)
- [Use Continuous Procedural Bases For Unity Stage Layouts](use-continuous-procedural-bases-for-unity-stage-layouts-2026-05-25.md)
- [Preserve MeshyAI Prefab Axis Correction In Scene Builders](../workflow-issues/preserve-meshyai-prefab-axis-correction-in-scene-builders-2026-05-25.md)
