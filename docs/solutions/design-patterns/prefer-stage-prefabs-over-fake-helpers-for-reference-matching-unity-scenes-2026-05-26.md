---
title: Prefer Stage Prefab Set Dressing Over Fake Helpers For Reference Matching Unity Scenes
date: 2026-05-26
last_updated: 2026-05-27
category: docs/solutions/design-patterns
module: Unity stage generation
problem_type: design_pattern
component: tooling
severity: medium
applies_when:
  - "A generated Unity stage must match a concept image using an existing stage asset folder"
  - "The asset folder already contains road, water, market, prop, and pickup prefabs"
  - "Procedural helper geometry starts making the Game view look synthetic or over-scaled"
  - "Imported road modules read as loose prop rows, rail-heavy clutter, or one-off set pieces instead of one continuous runner surface"
tags: [unity, stage-generation, prefabs, noryangjin, scene-builders]
---

# Prefer Stage Prefab Set Dressing Over Fake Helpers For Reference Matching Unity Scenes

## Context

Stage01_2 Noryangjin drifted away from the concept reference because the generated scene mixed real `Stage01_Noryangjin` prefabs with fake helper geometry: crop walls, water blockers, awning shells, foreground silhouettes, and generic road plates. The first prefab-only correction then overcorrected: road modules and the dropped-fish pickup prefab made the Scene view look like a separated asset showroom rather than the `stage_01_2_noryangjin_concept_batch_v1` runner view.

## Guidance

When a stage-specific prefab folder already contains the needed visual vocabulary, side composition should come from those prefabs first. Use imported market, boat, water-context, aquarium, crate, sign, lamp, and harbor props for identity. For the playable route, keep the continuous surface readable in the camera that matters. If road modules appear as disconnected rows or bring rail clutter into the lane, use a continuous procedural deck as the base and reuse ROAD prefabs only as flattened surface skins.

For Stage01_2, the scene builder now uses:

- A continuous procedural wet pier deck for the runner surface, with named plank rows, edge beams, seams, and a foreground extension so the 9:16 camera never starts over open water.
- Flattened `046`, `047`, and `048` ROAD surface skins over the procedural deck, preserving Stage01 road material identity without using full modules as the playable route.
- `014`, `015`, `016`, `021`, `030`, `031`, `043` market and seafood storefront prefabs for the side canyon.
- `017`, `018`, `019`, `037`, `038`, `041`, `042` harbor, boat, water, hill, gull, buoy, and dock context prefabs, with the hill pushed off-center so the sea stays visible.
- `027`, `028`, `029`, and `036` lamps, signs, and utility poles scaled high enough to frame the upper 9:16 runner view.
- Primitive `Stage01_2_Center_Gold_Coin_Line_*` coins because the available `009` prefab is a dropped fish, not a gold coin line.
- An invisible `Stage01_2_Road_Playable_Collider` as the gameplay helper.

Tests should assert both sides of the rule: required stage prefab names must be present for set dressing, and known fake helper object names must be absent. For reference-matching scenes, they should also encode view-critical layout decisions: the path should read as one continuous pier, ROAD skins should be non-uniformly flattened, water should be visible in the runner camera, foreground pixels should remain on pier planks rather than open water, upper-mid preview samples should not be mostly blank sky, side market buildings should have bounded scale, and the preview camera should use a stable field of view.

## Why This Matters

Procedural helpers are useful for early layout, but they can fight imported asset scale, material response, and silhouette. If a concept image depends on wet planks, fish market stalls, boats, water, and foreground clutter, fake walls and plates produce the wrong read even when their positions are technically correct.

The narrowed rule makes future review easier. A scene YAML diff should show real Stage01 set dressing for place identity, but the route base can be procedural when the concept review proves imported road modules are the cause of the bad view. If those ROAD prefabs contain useful floor detail, flatten them as visual skins over the route base instead of discarding them entirely. The important thing is to encode that decision in tests and preview-image checks, not rely on memory.

## When to Apply

- The user explicitly asks for a generated Unity stage to match a concept batch image.
- A stage folder such as `Stage01_Noryangjin` already contains numbered prefabs for market, harbor, and side dressing roles.
- The preview looks synthetic, blocked, or over-scaled because the builder added helper meshes.
- The road module prefabs do not compose into a continuous runner-view surface at the target camera angle, but their floor detail still helps the stage read correctly.

## Examples

Before:

```csharp
CreateMarketCanopy(parent, "Stage01_2_Left_Blue_Awning_Canopy_00", position);
InstantiateStagePrefab(context, "009", parent, position, 0f,
    "center_gold_pickup_line_00", 0.72f, 0.58f, 0.05f);
```

After:

```csharp
CreatePrimitiveObject(context, parent,
    "Stage01_2_Pier_Plank_Row_00_Lane_00",
    PrimitiveType.Cube, position, rotation, scale, wetWoodMaterial);
CreateRoadSurfaceSkin(context, parent, "046", position, yaw,
    "road_surface_skin_near_00", 6.35f, 0.035f, 0.92f, 2.55f, 0.16f);
InstantiateStagePrefab(context, "014", parent, position, 0f,
    "left_market_facade_near", 3.15f, 3.35f, 0f);
CreateGoldCoin(context, parent,
    "Stage01_2_Center_Gold_Coin_Line_00", position, yaw, goldMaterial);
```

## Related

- [Keep Unity Generated Set Dressing Outside Runner Lane](keep-unity-generated-set-dressing-outside-runner-lane-2026-05-26.md)
- [Flatten Road Prefabs As Surface Skins For Unity Runner Previews](flatten-road-prefabs-as-surface-skins-for-unity-runner-previews-2026-05-27.md)
- [Use Continuous Procedural Bases For Unity Stage Layouts](use-continuous-procedural-bases-for-unity-stage-layouts-2026-05-25.md) applies when the camera needs one readable playable surface. Stage01_2 now follows that route-base pattern while still using Stage01 prefabs for identity.
- [Preserve MeshyAI Prefab Axis Correction In Scene Builders](../workflow-issues/preserve-meshyai-prefab-axis-correction-in-scene-builders-2026-05-25.md)
