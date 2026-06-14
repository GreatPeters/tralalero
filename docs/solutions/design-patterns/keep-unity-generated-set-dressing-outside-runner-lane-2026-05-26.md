---
title: Keep Unity Generated Set Dressing Outside Runner Lane
date: 2026-05-26
category: docs/solutions/design-patterns
module: Unity scene generation workflow
problem_type: design_pattern
component: tooling
severity: medium
applies_when:
  - "A Unity editor stage builder places market buildings, awnings, or large props around a runner lane"
  - "Concept art shows dense side detail, but the generated game camera view becomes blocked"
  - "Imported prefab local scale values are not meaningful enough to review by eye"
tags: [unity, scene-generation, meshyai, runner-camera, scale-calibration]
---

# Keep Unity Generated Set Dressing Outside Runner Lane

## Context
Stage01_2 Noryangjin used repaired MeshyAI storefront and awning prefabs to create a dense harbor market. The scene technically contained the right ingredients, but the runner preview looked wrong because large blue and cream storefronts sat too close to the center lane and were scaled high enough to block the road, coins, and horizon.

## Guidance
Treat side buildings as camera-framing walls, not center-lane objects. In editor scene builders, encode lane clearance and visual scale as testable generation rules:

- Put major facades beyond the railing/shoulder instead of near the playable lane.
- Keep awning primitives from extending toward x=0 far enough to occlude the path.
- Use visual target bounds (`targetHeight`, `targetMaxXZ`, and multiplier), not prefab local scale numbers, as the source of truth.
- Lower smoothness/specular values for painted market materials so Lit shaders do not make fallback materials look like glossy plastic.
- Add scene YAML tests for representative facade x positions, awning width, prefab scale overrides, and material roughness.

For Stage01_2, the useful guardrails were:

```csharp
private const float PropSideOffset = 3.05f;
private const float MarketSideOffset = 4.85f;

PlaceMarketBuilding(
    context, parent, nodes[2], -1, "014",
    "left_market_facade_near",
    -0.45f, 4.35f, 4.45f, -3f, 0.78f);
```

The corresponding test should assert the generated scene contract rather than only checking that objects exist:

```csharp
Assert.That(ReadFirstPositionXAfterName(sceneYaml, "014_left_market_facade_near"), Is.EqualTo(-4.85f).Within(0.05f));
Assert.That(ReadFirstPositionXAfterName(sceneYaml, "Stage01_2_Left_Blue_Awning_Canopy_00"), Is.LessThan(-4.0f));
Assert.That(ReadFirstScaleXOverrideAfterName(sceneYaml, "015_right_sashimi_restaurant_near"), Is.LessThan(260f));
```

## Why This Matters
Dense environment dressing can satisfy a concept checklist while still failing in the actual game camera. Runner/shooter stages need a readable center lane, visible pickups, and a horizon cue. When generated scenes are reviewed from the Unity Scene view alone, oversized side prefabs can appear acceptable even though the Game view is blocked.

Generated MeshyAI prefab scale overrides can also be misleading because imported assets may use large unit-conversion values. Tests should therefore lock the intended generated positions and relative scale ceilings, not rely on subjective inspection after every rebuild.

## When to Apply
- Building concept draft scenes from imported marketplace or AI-generated prefabs.
- A vertical runner camera must show the lane, pickups, obstacle line, and destination.
- Fallback materials are generated in code and may be assigned to many child renderers.

## Examples
Before: major market facades at x=+-3.05 with target heights near 7 to 8 meters and awnings extending toward the middle of the pier.

After: facades at x=+-4.85, crop walls at x=+-5.25, lower foreground props at x=+-3.05, reduced building target heights near 3.6 to 4.4 meters, and market material smoothness around 0.14 to 0.24.

## Related
- [Use Continuous Procedural Bases For Unity Stage Layouts](use-continuous-procedural-bases-for-unity-stage-layouts-2026-05-25.md)
- [Preserve MeshyAI Prefab Axis Correction In Scene Builders](../workflow-issues/preserve-meshyai-prefab-axis-correction-in-scene-builders-2026-05-25.md)
