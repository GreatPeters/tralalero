---
title: Select rendererless turn spots by their full trigger footprint
date: 2026-07-26
category: docs/solutions/ui-bugs
module: Unity Noryangjin map tooling
problem_type: ui_bug
component: tooling
symptoms:
  - "A rendererless turn spot displayed a broad pink trigger area, but only its anchor cell was selectable."
  - "Clicking most of the visible trigger area selected an overlapping road or another object."
  - "The target-yaw editor did not appear because the intended turn spot was not selected."
root_cause: logic_error
resolution_type: code_fix
severity: medium
tags: [unity-editor, noryangjin, map-tool, turn-spot, scene-view, selection, collider]
---

# Select rendererless turn spots by their full trigger footprint

## Problem

The Noryangjin Map Tool draws each rendererless `NoryangjinTurnSpot`
from its `BoxCollider` trigger, but cursor selection previously derived
overlap from renderer bounds. The visible pink footprint and the actual
clickable footprint therefore disagreed, preventing authors from reliably
opening the target-yaw controls.

## Symptoms

- The pink trigger area covered several grid cells.
- Only the small anchor cell near the yellow center marker reliably selected
  the turn spot.
- Clicking elsewhere inside the pink area could select the road underneath.
- Without the turn spot selection, `목표 Y 회전` did not appear in the map-tool
  editor.

## What Didn't Work

The shared displayed-footprint path is correct for rendered roads and props:

```csharp
GetPlacedObjectDisplayedFootprintCells(
    child.gameObject,
    anchor,
    normalizedCellSize);
```

It is not authoritative for `NoryangjinTurnSpot`.
`CalculateRendererBounds` returns a zero-sized fallback because the trigger
intentionally has no renderer. Expanding the shared renderer-bounds helper to
all colliders would also be too broad: colliders on ordinary props do not
necessarily describe their intended map-tool footprint or occupancy.

## Solution

Keep the normal displayed-footprint behavior for ordinary objects and use the
trigger bounds only for turn-spot selection:

```csharp
NoryangjinTurnSpot turnSpot =
    child.gameObject.GetComponent<NoryangjinTurnSpot>();
List<Vector2Int> selectionCells = turnSpot != null
    ? BuildTurnSpotSelectionFootprintCells(
        turnSpot,
        origin,
        normalizedCellSize)
    : GetPlacedObjectDisplayedFootprintCells(
        child.gameObject,
        anchor,
        normalizedCellSize);
```

The helper converts the same world-space collider bounds used by the visible
trigger into grid cells:

```csharp
internal static List<Vector2Int> BuildTurnSpotSelectionFootprintCells(
    NoryangjinTurnSpot turnSpot,
    Vector3 currentOrigin,
    float currentCellSize)
{
    BoxCollider trigger = turnSpot != null
        ? turnSpot.GetComponent<BoxCollider>()
        : null;
    return trigger != null
        ? BuildBoundsFootprintCells(
            trigger.bounds,
            currentOrigin,
            currentCellSize)
        : new List<Vector2Int>();
}
```

`MapToolTurnSpotSelectionFootprint_UsesEntireTriggerArea` verifies that a
three-cell-wide trigger produces all three selectable cells rather than only
the center anchor.

The editor project build passed. The Unity MCP test request timed out at the
editor transport layer, so it did not provide a Unity Test Runner result.

## Why This Works

Hit testing and editor visualization now share the same geometry source for
turn spots: `BoxCollider.bounds`. Every grid cell represented by the pink
trigger footprint can select the turn spot, while rendered roads and props
retain their existing selection rules.

## Prevention

- Derive editor hit testing from the same geometry used to draw the
  interactive editor representation.
- Do not assume every selectable authoring helper has a renderer.
- Keep component-specific geometry rules scoped to that component instead of
  changing shared renderer-bound behavior globally.
- Test multi-cell selection footprints for rendererless triggers and markers.

## Related Issues

- [Delete map tool broad footprints with the empty cell tool](../logic-errors/delete-map-tool-broad-footprint-with-empty-cell-2026-06-20.md)
- [Resolve selected prefab children to map-tool placement roots](../logic-errors/resolve-selected-prefab-child-to-map-tool-placement-root-2026-06-08.md)
- [Continue map-tool layouts by selected renderer bounds](../design-patterns/continue-map-tool-layouts-by-selected-renderer-bounds-2026-07-19.md)
- [Prefer prefab placement previews over SceneView line grids](../developer-experience/prefer-prefab-placement-previews-over-sceneview-line-grids-2026-06-06.md)
- [Restore consumed turn spots on in-place restart](../integration-issues/restore-consumed-turn-spots-on-in-place-run-restart-2026-07-25.md)
