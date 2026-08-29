---
title: Avoid Cross-Lane Chrome on Adjacent World-Space Bonus Choices
date: 2026-08-28
last_updated: 2026-08-29
category: ui-bugs
module: Unity Noryangjin bonus altar UI
problem_type: ui_bug
component: tooling
symptoms:
  - "Two nearby bonus-choice labels looked like one broken horizontal UI strip."
  - "A title-first layout made the reward amount harder to scan while running."
  - "Prefab regeneration did not update scene instances that retained child-property overrides."
  - "Deterministic preview capture faced the UI toward the wrong camera."
root_cause: logic_error
resolution_type: code_fix
severity: medium
related_components:
  - "testing_framework"
  - "development_workflow"
tags:
  - "unity"
  - "world-space-ui"
  - "bonus-wall"
  - "prefab-generation"
  - "screen-space-overlap"
  - "visual-regression"
  - "textmeshpro"
  - "billboard"
---

# Avoid Cross-Lane Chrome on Adjacent World-Space Bonus Choices

## Problem

The two Noryangjin bonus altars sit close together in the gameplay camera. A
full-width title, backplate, or decorative frame could look valid on one prefab
but merge with its neighbor in screen space. The result read as a detached HUD
strip instead of two physical choices.

The selected visual direction also made the numeric reward the primary decision
signal: a large `+999` or `+11%`, a compact icon-and-label badge underneath, and
the colored portal below. The generated prefab, authored scene instances, and
deterministic preview all had to converge on that same hierarchy.

## Symptoms

- The two background rectangles visually joined across the lane center.
- `ATK SPEED` either overflowed or forced a much wider card than short labels.
- A rebuilt `Box_left.prefab` looked correct in isolation while the open map kept
  stale child layout overrides.
- The preview tool sometimes captured an edge-on or displaced Canvas because an
  `[ExecuteAlways]` billboard reassigned its rotation during the capture frame.

## What Didn't Work

- Shrinking a nearly full-width backplate only tuned the overlap; it retained the
  same cross-lane silhouette.
- Adding a wooden gate or info plate introduced a competing prop and covered the
  portal icon without making the information feel more grounded.
- Removing the old hierarchy from the prefab alone did not clear overrides on
  already-authored scene instances.
- Pointing the Canvas at the preview camera for one editor tick was insufficient;
  `WallStatCanvasBillboard.LateUpdate` could immediately point it back at the
  main camera.

## Solution

Use a data-first hierarchy with no full-width choice title:

```text
GFX/Canvas
├── Stat_Value        # +999 / +11%, large and centered
└── Stat_Badge        # compact dark plate with family-colored outline
    ├── Stat_Icon
    └── Stat_Name     # HEALTH / ATK SPEED
```

`FeastOfFortuneWallSetup.ConfigureStatIcon` reparents the existing localized
label and icon into `Stat_Badge`, leaves the value directly under the Canvas,
and removes obsolete `Stat_Row`, `Choice_Title`, icon-aura, and chrome children.
Both TMP rows use auto-sizing and no wrapping so the maximum intended samples
remain stable:

```csharp
label.enableAutoSizing = true;
label.fontSizeMax = StatNameFontSize;
label.fontSizeMin = StatNameMinFontSize;
label.textWrappingMode = TextWrappingModes.NoWrap;

value.enableAutoSizing = true;
value.fontSizeMax = StatValueFontSize;
value.fontSizeMin = StatValueMinFontSize;
```

The builder also creates a four-layer warp on each pedestal:
`WaterVortexOuter`, `WaterVortexInner`, `WarpCompass`, and `WaterFoam`.
`BonusChoiceAltarVfx.RotateWarpLayers` rotates adjacent layers in opposite
directions so the portal reads as flowing water rather than a single spinning
decal:

```csharp
float rotationDelta = rotationSpeed * motionSpeed * deltaTime;
glowRoot.Rotate(0f, rotationDelta, 0f, Space.Self);
waterVortexInner.Rotate(0f, -rotationDelta * 2.25f, 0f, Space.Self);
warpCompass.Rotate(0f, -rotationDelta * 1.35f, 0f, Space.Self);
waterFoam.Rotate(0f, rotationDelta * 1.15f, 0f, Space.Self);
```

After regenerating the canonical prefab, run the editor refresh command for
open-scene altar instances. It replaces each canonical instance while preserving
its parent, sibling index, local transform, name, and rarity, then re-applies map
tool configuration. This clears stale child overrides deterministically.

During deterministic preview capture, face the UI at the preview camera and
temporarily disable every `WallStatCanvasBillboard`. Restore the original
rotation and enabled state in `finally` after the PNG is written.

## Why This Works

The large value provides one fast visual target per lane. The smaller badge is
wide enough for `ATK SPEED` but narrow enough that two neighboring choices keep
a visible gap. Color remains local to the value, badge outline, beam, and warp,
so the pair separates without a new full-width surface.

The builder owns hierarchy convergence, the scene refresh owns prefab-instance
convergence, and the capture tool owns temporary camera state. Treating those as
separate boundaries avoids a correct prefab being hidden by stale scene data or
a correct runtime billboard producing a misleading preview.

## Prevention

- Test paired world-space UI at the actual mobile gameplay resolution.
- Include worst-case strings such as `ATK SPEED`, `+999`, and `+11%` in layout
  assertions and visual captures.
- Make generated-prefab cleanup idempotent by deleting obsolete child names.
- Provide an explicit open-scene refresh whenever authored instances may retain
  child overrides from an older generated hierarchy.
- Disable `[ExecuteAlways]` camera-follow behavior while a deterministic preview
  tool controls the same transform.
- Test warp layers through a small extracted rotation method so direction and
  relative speed are verifiable without waiting for Play Mode.

## Related Issues

- [Center bonus choice stat rows by combined content width](center-bonus-choice-stat-rows-by-combined-content-width-2026-08-16.md)
- [Keep world-space wall stat UI camera-facing on turning routes](keep-world-space-wall-stat-ui-camera-facing-on-turning-routes-2026-08-13.md)
- [Restore authored bonus choice VFX baselines on re-enable](restore-authored-bonus-choice-vfx-baselines-on-reenable-2026-08-15.md)
- [Bake generated prefab UI previews and isolate EditMode tests](../workflow-issues/bake-generated-prefab-ui-previews-and-isolate-editmode-tests-2026-08-13.md)
