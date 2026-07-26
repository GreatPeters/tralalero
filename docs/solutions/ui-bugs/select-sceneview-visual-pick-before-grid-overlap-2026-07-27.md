---
title: Select hovered height labels before SceneView visual picks
date: 2026-07-27
last_updated: 2026-07-27
category: docs/solutions/ui-bugs
module: Unity Noryangjin map tooling
problem_type: ui_bug
component: tooling
symptoms:
  - "Clicking a visible seagull perch selected the road or ocean backdrop underneath it."
  - "Selection changed according to placement-layer priority instead of the object visibly under the mouse."
  - "A gray Y-height label could name a fish scrap while clicking it selected the water behind it."
root_cause: logic_error
resolution_type: code_fix
severity: medium
tags: [unity-editor, noryangjin, map-tool, scene-view, selection, visual-picking, height-label, hover]
---

# Select hovered height labels before SceneView visual picks

## Problem

The Noryangjin map tool converted a SceneView click into a fine-grid cell and
then selected from every placed object whose footprint contained that cell.
When a small prop such as a seagull perch overlapped a road or water backdrop,
the result followed a fixed placement-layer priority rather than the renderer
the author was visibly pointing at.

Using SceneView's visual pick fixed that case, but exposed a second mismatch:
the tool's gray `Y` label is a 2D GUI overlay and is not returned by
`HandleUtility.PickGameObject`. A gray label belonging to a fish scrap could
therefore sit over water while the same click selected the water renderer
behind the label.

## Symptoms

- Clicking a visible seagull perch selected the road or water underneath it.
- The SceneView could visually identify the small prop, but the map tool
  discarded that information and recomputed selection from grid occupancy.
- Hovering a fish-scrap `Y -2.00` label drew the gray interaction state, but
  clicking that gray area selected the ocean backdrop.

## What Didn't Work

- Giving the seagull placement layer a hard-coded higher priority would fix one
  prefab while preserving the same failure for future overlapping objects.
- Expanding the seagull footprint would make more cells candidates, but would
  not tell the selector which rendered surface the mouse actually targeted.
- Treating `PickGameObject` as the unconditional first choice still ignored
  the 2D height-label overlay and selected the water renderer underneath it.
- Removing all grid fallbacks would regress rendererless turn spots and broad
  water selection areas that intentionally use collider or renderer bounds.

## Solution

Build clickable rectangles from the same positions, style, and text used to
draw the placed-object height labels. Resolve the last-drawn rectangle under
the mouse so overlapping labels obey their visual stacking order:

```csharp
internal static GameObject ResolveHoveredHeightLabelTarget(
    IReadOnlyList<KeyValuePair<GameObject, Rect>> labels,
    Vector2 mousePosition)
{
    GameObject hovered = null;
    foreach (KeyValuePair<GameObject, Rect> label in labels)
    {
        if (label.Key != null && label.Value.Contains(mousePosition))
            hovered = label.Key;
    }

    return hovered;
}
```

Use the hovered gray height label before the visual SceneView pick, and retain
the grid-overlap selector only as the final fallback:

```csharp
internal static GameObject ResolveSceneSelectionTarget(
    GameObject hoveredHeightLabelTarget,
    GameObject visualPickTarget,
    GameObject gridOverlapTarget)
{
    return hoveredHeightLabelTarget ?? visualPickTarget ?? gridOverlapTarget;
}
```

The click path computes only the fallbacks it needs:

```csharp
GameObject heightLabelPick =
    FindPlacedObjectHeightLabelAtMouse(currentEvent.mousePosition);
GameObject visualPick = heightLabelPick == null
    ? ResolveVisualPickSelectionTarget(
        HandleUtility.PickGameObject(currentEvent.mousePosition, false),
        GameObject.Find(RootName))
    : null;
GameObject gridPick = heightLabelPick == null && visualPick == null
    ? FindPlacedObjectOverlappingCursor(placementGridCellSize)
    : null;

Selection.activeGameObject =
    ResolveSceneSelectionTarget(heightLabelPick, visualPick, gridPick);
```

The gray background is drawn only for the label returned by the same
rectangle resolver, so the UI state and click result share one source of
truth.

## Why This Works

The height label is editor GUI, not scene geometry. Unity's 3D picker can
correctly answer which renderer is behind that pixel while still being wrong
for the explicit gray UI target in front of it. Hit-testing the label's GUI
rectangle first makes the visible interactive state authoritative.

The three selection paths now answer separate questions in a deterministic
order:

1. Which gray `Y` label is the mouse explicitly hovering?
2. Which visible renderer is under the mouse?
3. Which authored grid, collider, or bounds area contains the cursor?

In the affected scene, the live pre-fix probe found the fish-scrap label at
GUI `(351.3, 296.5)` while `PickGameObject` resolved the ocean backdrop.
After the fix, the same label rectangle resolves the fish scrap as the final
selection even though the visual pick remains water.

## Prevention

- Treat an explicitly highlighted editor GUI control as authoritative before
  querying the 3D scene behind it.
- Derive hover rendering and click hit-testing from the same GUI rectangle
  calculation so a gray state cannot promise a different selection target.
- When labels overlap, test and select the last-drawn target to match visual
  stacking order.
- Treat SceneView visual picking as authoritative for rendered objects only
  after editor GUI overlays decline the click.
- Resolve picked prefab children to their map-tool placement root before
  exposing transform controls.
- Reject visual picks outside the map-tool root so ordinary scene objects
  cannot leak into the tool's editing panel.
- Keep separate tests for height-label priority, visual-pick root resolution,
  and rendererless or broad-area fallback selection.
- Validate overlapping selection with real SceneView picking, not only pure
  footprint calculations.

## Related Issues

- [Select map-tool visuals by their authoritative geometry](select-rendererless-turn-spots-by-trigger-footprint-2026-07-26.md)
- [Resolve selected prefab children to Noryangjin map-tool placement roots](../logic-errors/resolve-selected-prefab-child-to-map-tool-placement-root-2026-06-08.md)
- [Delete map-tool broad footprints with the empty-cell tool](../logic-errors/delete-map-tool-broad-footprint-with-empty-cell-2026-06-20.md)
- [Prefer prefab placement previews over SceneView line grids](../developer-experience/prefer-prefab-placement-previews-over-sceneview-line-grids-2026-06-06.md)
