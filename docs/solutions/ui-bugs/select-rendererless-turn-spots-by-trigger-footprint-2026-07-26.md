---
title: Select map-tool visuals by their authoritative geometry
date: 2026-07-26
last_updated: 2026-07-26
category: docs/solutions/ui-bugs
module: Unity Noryangjin map tooling
problem_type: ui_bug
component: tooling
symptoms:
  - "A rendererless turn spot displayed a broad pink trigger area, but only its anchor cell was selectable."
  - "Clicking most of the visible trigger area selected an overlapping road or another object."
  - "The target-yaw editor did not appear because the intended turn spot was not selected."
  - "A large ocean backdrop displayed `Y -3.00`, but clicking visible water outside its manual 1x1 placement footprint selected nothing."
  - "The selection result was null even though the cursor world position was inside the ocean renderer bounds."
root_cause: logic_error
resolution_type: code_fix
severity: medium
tags: [unity-editor, noryangjin, map-tool, scene-view, selection, collider, renderer-bounds, water-backdrop]
---

# Select map-tool visuals by their authoritative geometry

## Problem

The Noryangjin Map Tool draws each rendererless `NoryangjinTurnSpot`
from its `BoxCollider` trigger, but cursor selection previously derived
overlap from renderer bounds. The visible pink footprint and the actual
clickable footprint therefore disagreed, preventing authors from reliably
opening the target-yaw controls.

The same mismatch later appeared in the tiled
`017_STAGE01_NRY_BG_001_Ocean_water_plane_backdrop`. Its visible renderer
covered a large area, while its authored placement footprint remained `1x1`.
Selection reused that small placement footprint, so clicking visible water
outside the anchor area returned no object.

## Symptoms

- The pink trigger area covered several grid cells.
- Only the small anchor cell near the yellow center marker reliably selected
  the turn spot.
- Clicking elsewhere inside the pink area could select the road underneath.
- Without the turn spot selection, `목표 Y 회전` did not appear in the map-tool
  editor.

- The ocean backdrop's cyan `Y -3.00` label appeared over visible water, but
  clicking near the label could still leave the selection empty.
- At cursor cell `(163, -4)`, the world point `(36.675, -0.900)` was inside
  the ocean renderer bounds, but the old selection footprint covered only
  fine-grid cells `X 177..181 / Z 19..23`.

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

Fixing only the singleton `Background_Water` did not address the screenshot.
That object has no coordinate suffix and lives under the dedicated `Water`
container. The failing `Y -3.00` labels belonged to repeated, grid-managed
`Ocean_water_plane_backdrop` prefab instances under `Props`.

The cyan height text itself is drawn with `Handles.Label`; it is display-only,
not an interactive control. The click must resolve the object from the cursor
cell underneath the label.

The first ocean regression-test setup instantiated the prefab at its asset
scale but did not apply the map-tool placement multiplier. Its bounds occupied
one fine cell, so a `> 25` assertion failed for the setup rather than the
selection fix. Applying `BuildPalettePlacementScale` and the saved yaw made the
test instance match the authored scene placement.

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

Keep the ocean's `1x1` manual footprint for placement and occupancy, but give
selection its own prefab-specific geometry policy:

```csharp
internal List<Vector2Int> GetPlacedObjectSelectionFootprintCells(
    GameObject target,
    Vector2Int anchor,
    float normalizedCellSize)
{
    string prefabPath = GetPrefabAssetPathForPlacedObject(target);
    return IsOceanWaterBackdropPrefabPath(prefabPath)
        ? BuildBoundsFootprintCells(
            CalculateRendererBounds(target),
            origin,
            normalizedCellSize)
        : GetPlacedObjectDisplayedFootprintCells(
            target,
            anchor,
            normalizedCellSize);
}
```

`FindPlacedObjectOverlappingCursor` calls this helper for ordinary rendered
placements after retaining the turn-spot trigger branch. The ocean path is an
exact normalized prefab-path comparison, so other manual `1x1` props retain
their existing selection footprint.

`MapToolTurnSpotSelectionFootprint_UsesEntireTriggerArea` verifies that a
three-cell-wide trigger produces all three selectable cells rather than only
the center anchor.

`OceanWaterBackdropSelection_UsesRendererBoundsBeyondOneByOneFootprint`
instantiates the real ocean prefab in a preview scene, applies its saved
placement scale and yaw, verifies that its manual footprint is still `1x1`,
and asserts that the selection cells equal the full renderer-bounds cells.

The editor project build passed with zero warnings and errors. The new exact
Unity EditMode test passed, as did four focused selection and bounds tests.
The previously failing live cursor cell `(163, -4)` then resolved to
`Prop_017_STAGE01_NRY_BG_001_Ocean_water_plane_backdrop_X+177_Z+19`.
The authored map-tool scene SHA-256 remained unchanged. A broad class run
passed 156 of 161 tests; its five failures were unrelated existing asset,
label, scale, and color expectations outside this selection path.

## Why This Works

Hit testing and editor visualization now share the same geometry source for
turn spots: `BoxCollider.bounds`. Every grid cell represented by the pink
trigger footprint can select the turn spot, while rendered roads and props
retain their existing selection rules.

The ocean backdrop now follows the same principle using `Renderer.bounds`.
Its manual `1x1` footprint remains authoritative for placement occupancy, but
it no longer incorrectly limits the visible click target. Keeping selection
geometry separate prevents the fix from changing placement blocking or the
palette footprint badge.

## Prevention

- Derive editor hit testing from the same geometry used to draw the
  interactive editor representation.
- Do not assume every selectable authoring helper has a renderer.
- Keep component-specific geometry rules scoped to that component instead of
  changing shared renderer-bound behavior globally.
- Test multi-cell selection footprints for rendererless triggers and markers.
- Distinguish singleton backdrops from repeated grid-managed background
  prefabs before changing selection behavior.
- Do not assume a placement or occupancy footprint is also the correct
  interactive selection footprint for a large visual tile.
- In prefab-backed bounds tests, apply the same saved placement scale and yaw
  used by the map tool before asserting world-space coverage.

## Related Issues

- [Delete map tool broad footprints with the empty cell tool](../logic-errors/delete-map-tool-broad-footprint-with-empty-cell-2026-06-20.md)
- [Resolve selected prefab children to map-tool placement roots](../logic-errors/resolve-selected-prefab-child-to-map-tool-placement-root-2026-06-08.md)
- [Continue map-tool layouts by selected renderer bounds](../design-patterns/continue-map-tool-layouts-by-selected-renderer-bounds-2026-07-19.md)
- [Prefer prefab placement previews over SceneView line grids](../developer-experience/prefer-prefab-placement-previews-over-sceneview-line-grids-2026-06-06.md)
- [Restore consumed turn spots on in-place restart](../integration-issues/restore-consumed-turn-spots-on-in-place-run-restart-2026-07-25.md)
