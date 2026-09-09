---
title: Avoid binary black cel shadows on SR18 ramps
date: 2026-09-05
category: ui-bugs
module: Noryangjin SR18 ramp materials
problem_type: ui_bug
component: tooling
symptoms:
  - "Inclined timber planks appear almost black with long triangular streaks while adjacent flat decks look normal."
  - "The artifact varies with ramp orientation and resembles stretched or corrupted texture."
root_cause: config_error
resolution_type: config_change
severity: medium
tags: [unity, sr18, flatkit, cel-shading, material, ramps, visual-regression]
---

# Avoid binary black cel shadows on SR18 ramps

## Problem

SR18's generated uphill/downhill decks reused the flat pier material. Its aggressive, nearly binary black cel shading obscured the timber on inclined faces. Geometry continuity tests passed because the problem was in rendering, not the path or collider.

## Symptoms

- `SR18_Rising_Deck` displayed large dark streaks across the plank tops; the flat deck next to it remained readable.
- The material was `Noryangjin_RoadBasic_SubtleOutline`, using `FlatKit/Stylized Surface`, `_SelfShadingSize=0.838`, `_ShadowEdgeSize=0.016`, and black `_ColorDim`.
- The inspected rising mesh had 3,061 normals with minimum non-degenerate face/normal dot about 0.999. Reversed surface normals were not the cause.

## What Didn't Work

- Disabling outline width, the outline-enabled property and `DR_OUTLINE_ON` on temporary material clones left the dark bands unchanged.
- Disabling primary cel shading removed the bands without changing geometry or texture, establishing the cause, but also removed useful surface shading. It was a diagnostic control, not the final fix.
- A 0.30 self-shading size removed more bevel shading than needed. A 0.45 size with a 0.08 transition retained plank definition while removing the broad black regions.
- The first application attempt refused transient dirty scene state after a test run. A fresh scene-state check and explicit target save allowed the guarded application. No scene reopening or global save was needed.

## Solution

Create a dedicated material by cloning the original through Unity's asset API and changing only these properties:

```csharp
var material = new Material(source);
material.SetFloat("_SelfShadingSize", 0.45f);
material.SetFloat("_ShadowEdgeSize", 0.08f);
```

Store it at `Assets/ShooterSurvival/Materials/Generated/SR18_RoadSlope.mat`. Apply it as a connected-prefab renderer override to only the 12 SR18 uphill/downhill roots (70–72, 87–89, 151–153, 156–158). The shader, keywords, wood texture, texture scale/offset and colors remain inherited from the original. No mesh, UV, road transform, collider, flat-road material, source prefab or other map changes are needed.

The guarded application is `tools/fix-sr18-ramp-shading.cs`; it requires the original material on all 12 ramps and refuses existing output assets. The pre-change scene backup is `tmp/backups/sr18-before-ramp-shading-20260905-083357.unity.tmp`. After asset/import callbacks settle, explicitly save the exact SR18 scene again and confirm it is clean.

## Why This Works

Tilting the surface changes its lighting response. With a large shading threshold, a narrow transition and black shadow color, small differences between plank faces become large black areas. Lowering the threshold and widening the transition preserves the original textured material while allowing the inclined tops to remain readable. The variation was orientation-dependent shading, not intermittent texture loading.

## Prevention

- Separate texture, geometry/normal, outline and lighting hypotheses with fixed-camera, fixed-geometry renders. Use transient cloned materials for probes; never mutate a shared source material just to capture a comparison.
- `Sr18Ramps_UseSlopeSafeShadingWithoutChangingTheirWoodTexture` failed on the original 0.838 value and passes on the corrected scene. It checks all 12 overrides, both tuning values, unchanged texture/shader/keywords and an untouched flat-road material.
- All six SR18 tests pass, retaining road-surface, route and market checks. The Editor build and agent-harness validator pass.
- `tools/capture-sr18-ramps.cs` captures all four ramp runs from persisted materials. Inspect direction variants, not just the first ascent. Images are under `tmp/image-previews/sr18-ramp-shading-2026-09-05/`: `before.png`, `outline-disabled.png`, `cel-disabled.png`, and `applied-ramp-070/087/151/156.png`.
- Re-evaluate these material values if lighting or the art direction changes. These static checks do not establish runtime traversal or mobile performance.

## Related Issues

- [Road-deck geometry validation](../logic-errors/validate-road-decks-instead-of-renderer-bounds-2026-09-05.md)
- [FlatKit material keyword and renderer verification](../workflow-issues/verify-unity-material-keywords-after-bulk-outline-conversions-2026-07-02.md)
- [SR18 scene contract](../../noryangjin-sr18-roads-scene.md)
