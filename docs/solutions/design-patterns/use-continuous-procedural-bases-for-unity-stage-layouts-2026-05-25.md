---
title: Use Continuous Procedural Bases For Unity Stage Layouts
date: 2026-05-25
category: docs/solutions/design-patterns
module: Unity scene generation workflow
problem_type: design_pattern
component: tooling
severity: medium
applies_when:
  - "A generated Unity stage should read as one coherent route, pier, road, or floor"
  - "MeshyAI prefabs are better used as set dressing than as the full playable surface"
  - "A stage builder must match a concept image before final authored art exists"
tags: [unity, scene-generation, meshyai, procedural-layout, stage-design]
---

# Use Continuous Procedural Bases For Unity Stage Layouts

## Context
The Stage01 Noryangjin draft initially placed repeated MeshyAI road prefabs as the playable path. Even after preserving prefab axis correction, the result still looked like a row of loose prop modules: the road surface did not read as one continuous pier, side objects felt like a prefab lineup, and water appeared as large stage blocks instead of background.

## Guidance
For concept-matching Unity layout scenes, create the playable base as a continuous procedural object first. Then use repaired MeshyAI prefabs only for props, gates, boats, pickups, and distant context.

In `Stage01NoryangjinAutoDraftBuilder`, the stronger pattern is:

```csharp
CreatePrimitiveObject(
    context,
    roads,
    "Stage01_1_Continuous_Pier_Deck",
    PrimitiveType.Cube,
    new Vector3(0f, -0.18f, centerZ),
    new Vector3(DeckWidth, 0.36f, deckLength),
    deckMaterial);
```

Add cross seams, edge beams, sparse rope posts, and flat water planes as separate primitives. Keep center-lane pickups and player preview aligned to the procedural base, while moving crates, nets, anchors, and carts to the shoulders.

Do not let the continuous base replace the place identity. For Noryangjin-style stages, keep a dedicated market-building pass such as `03_Market_Buildings` and place storefront, sashimi stall, seafood display, awning, and aquarium prefabs along both sides of the pier. The base solves route readability; the buildings supply the stage's subject.

## Why This Matters
Generated asset prefabs often contain rich detail, but using them as the entire road can produce visual repetition and unclear traversal space. A continuous base gives the stage a stable silhouette and readable route. It also makes later prop placement easier because there is an explicit lane, shoulder, rail, and water boundary.

The continuous base should not be one large visible slab. If the camera looks down the path, a single cube deck with thick cross seams reads like a placeholder grid. Use many narrow planks with slight color variation on top of a hidden underframe so the surface reads as a wet wooden pier.

## When to Apply
- The concept reference shows a continuous surface, but available prefabs are modular or overly detailed.
- The gameplay path must remain readable in a runner/shooter camera.
- The scene builder is an editor tool, so generated materials and primitives can be created deterministically.

## Examples
Before: instantiate `046_stage01_1_wet_pier_floor_00..10` as the path, or replace that with one large brown cube and no surrounding market buildings.

After: create `Stage01_1_Continuous_Pier_Deck`, `Deck_Plank_Row_00_Lane_00..`, `Deck_Cross_Seam_00..`, `Left_Rope_Post_00..`, and `Harbor_Water_Left_Flat_Plane` procedurally, then place MeshyAI market buildings and props around that structure.

## Related
- [Preserve MeshyAI Prefab Axis Correction In Scene Builders](../workflow-issues/preserve-meshyai-prefab-axis-correction-in-scene-builders-2026-05-25.md)
- [Create Unity Layout Scenes When Editor Execution Is Blocked](../workflow-issues/create-unity-layout-scene-when-editor-execution-is-blocked-2026-05-25.md)
