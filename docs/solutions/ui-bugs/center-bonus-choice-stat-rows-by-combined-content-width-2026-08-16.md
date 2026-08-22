---
title: Center Bonus Choice Stat Rows by Combined Content Width
date: 2026-08-16
category: ui-bugs
module: Shooter Survival bonus choice UI
problem_type: ui_bug
component: tooling
severity: low
symptoms:
  - "Attack and health stat rows appeared horizontally off-center beneath their titles and icons."
  - "The offset and label-to-value gap changed between 공격력 +6 and 체력 +40."
  - "The value looked smaller and sat on a slightly different visual baseline from the localized label."
root_cause: logic_error
resolution_type: code_fix
related_components:
  - "testing_framework"
  - "development_workflow"
tags:
  - "unity"
  - "world-space-ui"
  - "textmeshpro"
  - "horizontal-layout-group"
  - "prefab-generation"
  - "localization"
  - "ui-alignment"
---

# Center Bonus Choice Stat Rows by Combined Content Width

## Problem

The generated `Box_left` and `Box_Right` prefabs placed `Stat_Text` and
`Value_Text` in separate fixed-width regions. Each field was aligned to its own
boundary, so the visible label-and-value pair was not centered as one unit over
the choice title, icon, and altar.

## Symptoms

- `공격력 +6` appeared about 5 pixels to the right of its intended center.
- `체력 +40` appeared about 30-32 pixels to the right.
- The visible gap between the label and value differed between the two choices.
- The label used font size `0.085`, while the value used `0.075`, creating
  inconsistent weight and a small vertical drift.
- Component-reference and loose vertical-position tests passed while the
  rendered row was visibly wrong.

## What Didn't Work

The original layout right-aligned the localized label and left-aligned the value
inside independent anchor regions:

```csharp
statText.anchorMin = new Vector2(0.08f, bottom);
statText.anchorMax = new Vector2(0.60f, top);
valueText.anchorMin = new Vector2(0.64f, bottom);
valueText.anchorMax = new Vector2(0.94f, top);
```

Those fixed boundaries preserve a nominal gap but never account for the two
strings' combined preferred width. Per-prefab anchor offsets would only tune the
current Korean text and values; another locale or a different digit count would
shift the visual center again. Shader, outline, and icon changes cannot correct
this geometry.

## Solution

Create one full-width `Stat_Row` and reparent the existing referenced TMP
objects beneath it. A `HorizontalLayoutGroup` sizes both children from their TMP
preferred widths and centers their combined width:

```csharp
row.anchorMin = new Vector2(0f, StatNameBottomAnchor);
row.anchorMax = new Vector2(1f, StatNameTopAnchor);

layout.childAlignment = TextAnchor.MiddleCenter;
layout.spacing = 0.048f;
layout.childControlWidth = true;
layout.childControlHeight = true;
layout.childForceExpandWidth = false;
layout.childForceExpandHeight = false;
```

Keep the serialized localization and value references intact rather than
replacing the text objects:

```csharp
RectTransform statText =
    wall.statNameLoc.GetComponent<RectTransform>();
RectTransform valueText = wall.statValueTmp.rectTransform;

ConfigureStatRowChild(statText, row, 0);
ConfigureStatRowChild(valueText, row, 1);
```

Use the same type size and alignment for both fields:

```csharp
internal const float StatNameFontSize = 0.085f;
internal const float StatValueFontSize = StatNameFontSize;

statName.horizontalAlignment = HorizontalAlignmentOptions.Center;
statName.verticalAlignment = VerticalAlignmentOptions.Middle;
statValue.horizontalAlignment = HorizontalAlignmentOptions.Center;
statValue.verticalAlignment = VerticalAlignmentOptions.Middle;
```

After setting the generated preview strings, force one layout pass before
saving the prefab:

```csharp
LayoutRebuilder.ForceRebuildLayoutImmediate(statRow);
```

Regenerate both prefabs after changing the builder. Runtime localization and
value updates continue to target the same TMP components and automatically mark
their layout dirty.

## Why This Works

`TextMeshProUGUI` implements Unity layout sizing through its preferred width.
The row therefore centers this complete unit:

```text
localized label width + fixed spacing + formatted value width
```

When the label or digit count changes, the children resize but their combined
group remains centered. Equal typography removes the visual weight and baseline
difference. The explicit prefab-time rebuild makes the saved asset and editor
preview deterministic instead of relying on a later runtime canvas pass.

The verified 369-pixel-wide scene capture placed the attack row within about
0.5 pixel of its title/icon axis and the health row within 1 pixel. Both visible
label-to-value gaps were about 5-6 pixels.

## Prevention

- Center related localized fields by their combined content width, not by
  independent fixed anchors.
- Preserve direct serialized references when reparenting generated UI objects.
- Assert the `Stat_Row` hierarchy, child order, `HorizontalLayoutGroup`
  settings, equal font sizes, and equal vertical alignment after prefab reload.
- Instantiate both prefabs in a Preview Scene, rebuild layout and TMP meshes,
  then assert that combined visible glyph bounds remain within `0.02` Canvas
  units of the icon center.
- Include strings with different lengths and digit counts in layout regression
  tests.
- Regenerate the serialized prefabs and capture the actual gameplay composition
  after changing their builder.

The focused test first failed against the old prefabs, then passed after
regeneration. The complete relevant EditMode suites passed `37/37` and `14/14`,
the editor project built with zero warnings and errors, and the Unity console
contained no errors after verification.

## Related Issues

- [Keep world-space wall stat UI camera-facing on turning routes](keep-world-space-wall-stat-ui-camera-facing-on-turning-routes-2026-08-13.md)
- [Bake generated prefab UI previews and isolate EditMode instantiation](../workflow-issues/bake-generated-prefab-ui-previews-and-isolate-editmode-tests-2026-08-13.md)
- [Replace duplicated bonus choice icon auras with soft gradients](replace-duplicated-bonus-choice-icon-auras-with-soft-gradients-2026-08-16.md)
