---
title: Prefer Stage Prefab Set Dressing Over Fake Helpers For Reference Matching Unity Scenes
date: 2026-05-26
last_updated: 2026-09-13
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

## Road chapter follow-up, 2026-09-13

Open the actual requested images before deciding what the asset inventory means. The correct road references are in `output/meshy_images/`, not `output/meshy/_images/`. Existing gameplay improvements did not establish visual fidelity to those images.

- **Measure continuity, not object count.** HighWay already contained102 acoustic panels, but their approximately4.4m length and12m spacing left large gaps. Route-aligned panels and a continuous green band change the visible boundary without changing the road sampler or collision geometry. Leave fork openings clear rather than connecting a decorative wall across a playable branch.
- **Check imported asset types.** The sky PNG was imported as a `Cubemap`. `LoadAssetAtPath<Texture2D>` returned null without a compile error, and `Skybox/Panoramic` rendered gray. Loading the actual `Cubemap` into `Skybox/Cubemap`'s `_Tex` fixed the visible sky. A file extension does not determine its Unity object type.
- **Use lit copies for new scenery.** Megacity vegetation used an Unlit texture atlas. Enlarging it produced bright, flat crowns that obscured the horizon. Scene-specific Lit copies of the atlas, smaller crowns and more distant placement gave the new foliage lighting and depth without modifying the shared source materials.
- **Inspect fascia depth against the real mesh.** The fuel canopy was about8.06m deep; trim at localZ3.5 remained inside its roof. Moving the accent to approximatelyZ4.08 put it on the outside face. Correct naming and coordinates are insufficient proof of visibility.
- **Protect behavior separately from appearance.** The Pipeline tool snapshots gameplay transforms and serialized route/traffic/holdout/enemy settings, then compares before saving and after reload. New decoration carries no colliders. Scene-specific material changes are allowed while source materials, the workbook and signed archive stay intact.
- **Review the Game view after placement.** Edit-mode camera renders omit overlay UI and do not exercise occlusion or camera transitions. Directed live runs exposed overly strong checkerboard contrast in the food hall; quieter stone finishes retain the clear cross-shaped battle space. Preserve failed images alongside the corrected captures, and label upgraded section-entry tests accurately.

The implementation and evidence are in [road reference visuals](../../../map-concepts/road-reference-visuals-2026-09-13/README.md) and `tools/road-reference-visuals.cs`. This is a composition/material improvement, not a claim of exact reference reproduction or Android performance acceptance.

## Related

### Native implementation follow-up

The [approved-concept implementation](../../../map-concepts/approved-road-concepts-2026-09-13/README.md) retained the normal camera but initially placed the new roof below it: local cameraY8.364 inherited a1.5 player scale, putting the camera near12.67world metres. Camera-local equality was therefore insufficient. Measure the camera's world height, preserve the camera, and place architecture above it; verify actual gameplay images as well as transform assertions. Bind complete sign/shutter assemblies to existing occlusion handling so labels do not remain floating.

Reusing human rigs for a3-head proportion requires coordinated geometry and rest-bone changes, rigid hand-equipment relocation and re-grounding of existing animation keys. Preserve the source file version (these sources needed installed Blender5.2.1), verify fresh GLB/FBX and actual Unity skin matrices, and keep stable combat prefab/controller references. New TRELLIS counters were generated as isolated modules with blank sign panels and native Korean labels, then reused around the perimeter. Do not replace an open lobby with a corridor merely to put all props in the camera.

Road paint needs readable shading: the line renderers did not provide useful lighting data for a Lit material, so dedicated unlit paint restored contrast without changing geometry. Fit exterior panels at their lateral curve radius rather than the centerline segment length. Check scenery's actual support surface too: the elevated road was atY0 while CityGround was atY-6.5; potted vegetation placed near road height floated above the surrounding land. Ground non-colliding landscape by rendered minimumY and provide a separate shoulder surface under road-level lamps. Legacy integration checks must sample the authored continuous route, and test-runner filters must yield nonzero test counts; a pipe-separated literal filter is not a union of classes.

The subsequent [six concept previews](../../../map-concepts/road-style-concepts-2026-09-13-v1/README.md) exposed a prompt-level variant of the same layout problem: common runner instructions introduced bonus pedestals into the food-hall encounter. The first correction raised the camera to expose every door, but the user's later explicit correction rejected that view. The [v2 concepts](../../../map-concepts/road-style-concepts-2026-09-13-v2/README.md) preserve the normal rear gameplay camera, allow the rear entrance to stay off-screen, and show a roofed restaurant rather than a cutaway courtyard. Do not change the camera merely to make every mechanic visible in one concept.

The same review required human NPCs with three-head proportions, opposing two-lane carriageways and a yellow centerline. Describe road topology as a left-to-right sequence of edge/divider/center markings and inspect the drawn result: extra dashed toll guides made the first v2 toll approach ambiguous and needed removal. A style reference does not authorize copying its penguin characters, extra limbs or incorrect lane count. Role-label references, override those defects explicitly, and keep concept verification separate from Unity implementation; the latest design requires the original rear-view holdout even while the saved runtime still has its earlier raised camera.

In the subsequent [360-degree holdout concepts](../../../map-concepts/reststop-360-concepts-2026-09-13-v1/README.md), the user clarified that fixed position still allows full body/aim rotation. The attempt to illustrate this with a giant near-foreground rear attacker pushed the shark toward the middle of the image and was explicitly rejected: it felt like a different camera despite the prompt saying fixed camera. The [corrected v2](../../../map-concepts/reststop-360-concepts-2026-09-13-v2/README.md) uses the approved gameplay image as the edit base, preserves the large bottom-third shark and the room's perspective, and changes only body/aim orientation. Rear targets can remain outside the image. Camera direction alone is not enough; compare player screen position, apparent scale and floor projection. Never require an off-screen mechanic to be visible at the expense of the user's camera contract.

The user's later real-photo reference added an independent venue constraint: a broad open rest-stop lobby with stores at the far perimeter, not a narrow restaurant aisle. The [open-hall concepts](../../../map-concepts/reststop-open-hall-concepts-2026-09-13-v1/README.md) keep the player low and close while moving the room boundaries/props outward. A larger unobstructed world space and a wider camera are different changes. Label the photograph as the spatial reference and the approved game image as the camera/art reference, so neither requirement silently overrides the other.

- [Keep Unity Generated Set Dressing Outside Runner Lane](keep-unity-generated-set-dressing-outside-runner-lane-2026-05-26.md)
- [Flatten Road Prefabs As Surface Skins For Unity Runner Previews](flatten-road-prefabs-as-surface-skins-for-unity-runner-previews-2026-05-27.md)
- [Use Continuous Procedural Bases For Unity Stage Layouts](use-continuous-procedural-bases-for-unity-stage-layouts-2026-05-25.md) applies when the camera needs one readable playable surface. Stage01_2 now follows that route-base pattern while still using Stage01 prefabs for identity.
- [Preserve MeshyAI Prefab Axis Correction In Scene Builders](../workflow-issues/preserve-meshyai-prefab-axis-correction-in-scene-builders-2026-05-25.md)
