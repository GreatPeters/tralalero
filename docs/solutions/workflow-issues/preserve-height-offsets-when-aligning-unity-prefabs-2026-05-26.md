---
title: Preserve Height Offsets When Aligning Unity Prefab Instances
date: 2026-05-26
category: docs/solutions/workflow-issues
module: Unity stage generation
problem_type: workflow_issue
component: tooling
severity: medium
applies_when:
  - "A scene builder places prefab instances with a world-space vertical offset and then calls an AlignBottom helper"
  - "Reference-matching stage previews are missing lamps, signs, birds, or other elevated set dressing"
  - "A generated camera preview needs pixel-level regression checks for composition"
symptoms:
  - "Objects created with position + Vector3.up still appear on the floor"
  - "The runner preview has too much empty sky because elevated market and harbor props are missing"
  - "Scene YAML shows the object y position near zero despite an intended vertical offset"
root_cause: logic_error
resolution_type: code_fix
tags: [unity, scene-builders, prefab-alignment, visual-regression, noryangjin]
---

# Preserve Height Offsets When Aligning Unity Prefab Instances

## Context

`Stage01NoryangjinSecondAutoDraftBuilder` placed lamps, signs, and flying gulls with expressions like `position + Vector3.up * 1.45f`, then called `InstantiateStagePrefab(..., groundY: 0f)`. The helper ends by calling `AlignBottom(instance, groundY)`, so the supplied vertical offset was overwritten and the elevated props dropped back to the floor.

The visual symptom was easy to miss in Scene view because the prefabs still existed. In the 9:16 runner preview, though, the upper frame looked empty: lamps and gulls were not where the reference image expected them, and the scene kept reading as a low prop layout instead of a dense fish-market harbor.

## Guidance

When a prefab-instantiation helper aligns the renderer bounds after placement, treat `groundY` as the source of truth for final vertical placement. If an object should hang, float, or sit above the deck, pass that height to the alignment helper instead of only adding `Vector3.up` to the position.

Before:

```csharp
InstantiateStagePrefab(context, "027", parent,
    Side(node, -3.35f, -0.95f) + Vector3.up * 1.45f,
    node.Yaw, "left_warm_market_lamp_near",
    0.85f, 0.85f, 0f, 0.85f);
```

After:

```csharp
InstantiateStagePrefab(context, "027", parent,
    Side(node, -3.35f, -0.95f) + Vector3.up * 1.45f,
    node.Yaw, "left_warm_market_lamp_near",
    0.85f, 0.85f, 1.45f, 0.85f);
```

Also add YAML and preview-image assertions for important visual composition:

```csharp
Assert.That(ReadFirstPositionYAfterName(sceneYaml,
    "027_left_warm_market_lamp_near"), Is.GreaterThan(1.2f));
Assert.That(ReadFirstPositionYAfterName(sceneYaml,
    "041_left_flying_gull_open_harbor"), Is.GreaterThan(6f));

Color32[] upperMidSamples =
{
    ReadPreviewPixel(300, 940),
    ReadPreviewPixel(330, 940),
    ReadPreviewPixel(360, 940),
    ReadPreviewPixel(390, 940),
    ReadPreviewPixel(420, 940)
};
Assert.That(CountBlankSkySamples(upperMidSamples), Is.LessThan(3));
```

## Why This Matters

Bounds alignment helpers are useful for imported MeshyAI prefabs because their origins and axes vary. The same helper can silently destroy intentional vertical composition if callers assume world-space `position.y` survives the final align step.

For generated reference scenes, missing elevated objects changes the whole read: the floor may be correct, but the camera still feels empty because the upper frame lacks lamps, signs, masts, and birds. Preview-image checks catch that class of regression better than YAML-only object-presence tests, but sample a small band rather than one exact pixel so a bright boat highlight or gap between skyline towers does not create a false failure.

## When to Apply

- A builder calls `AlignBottom`, `FitEnvelope`, or similar bounds-based placement helpers after setting transform position.
- A prop should be above ground, hanging, floating, or flying.
- The object is present in YAML but missing from the expected part of the camera preview.
- A concept-matching scene needs a stable generated PNG for visual review.

## Related

- [Prefer Stage Prefab Set Dressing Over Fake Helpers For Reference Matching Unity Scenes](../design-patterns/prefer-stage-prefabs-over-fake-helpers-for-reference-matching-unity-scenes-2026-05-26.md)
- [Keep Unity Generated Set Dressing Outside Runner Lane](../design-patterns/keep-unity-generated-set-dressing-outside-runner-lane-2026-05-26.md)
- [Preserve MeshyAI Prefab Axis Correction In Scene Builders](preserve-meshyai-prefab-axis-correction-in-scene-builders-2026-05-25.md)
