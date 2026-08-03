---
title: Keep route-aligned enemy previews synchronized with placement
date: 2026-08-03
category: logic-errors
module: Unity Noryangjin map tooling
problem_type: logic_error
component: tooling
symptoms:
  - "Changing the player route without changing the snapped cursor cell could leave an enemy placement preview at its previous yaw."
  - "Final placement could recalculate the live route and use a different yaw than the visible preview."
root_cause: logic_error
resolution_type: code_fix
severity: medium
related_components: [ChapterEnemyProgression, PlayerScript, NoryangjinTurnSpot]
tags: [unity, noryangjin, map-tool, placement-preview, enemy-route, cache-invalidation]
---

# Keep route-aligned enemy previews synchronized with placement

## Problem

Noryangjin enemy placement derives its root yaw from the player's start frame
and the turn spots. An attempted editor optimization refreshed the preview only
when the snapped cursor cell changed, even though route transforms can change
without the cursor moving. The final placement still resolved the live route,
so the committed enemy could disagree with the preview.

## Symptoms

- Moving or rotating the player route start could leave an enemy ghost at its
  old yaw until the cursor crossed into another cell.
- Editing a turn spot had the same stale-preview behavior.
- Clicking could place the enemy at a newly calculated yaw that the preview had
  not shown.

## What Didn't Work

Using only `gridX` and `gridZ` as the refresh key was incomplete memoization:

```csharp
if (cursorCellChanged)
    UpdatePlacementPreview(selectedItem);
```

The preview resolver also reads `PlayerScript.transform` and every relevant
`NoryangjinTurnSpot`, so unchanged cursor coordinates do not imply unchanged
output. Separately, using the geometric line between slightly offset turn-spot
centers as the enemy yaw could introduce small diagonal rotations.

## Solution

Keep preview and placement live by recalculating the preview on every tracked
SceneView pointer event:

```csharp
if (ShouldTrackSceneMouseForPlacementPreview(currentEvent.type))
{
    gridX = hoverCell.x;
    gridZ = hoverCell.y;
    UpdatePlacementPreview(selectedItem.Value);
}
```

Make the correct refresh inexpensive by rejecting ordinary palette items before
performing any route lookup:

```csharp
if (!IsEnemyPalettePrefabPath(prefabPath))
    return authoredRotation;
```

Find the small set of relevant components with `FindObjectsByType`, filtered to
the target scene, instead of walking every root hierarchy on each enemy-preview
update. Preserve two directions on each reconstructed route section:

```csharp
public Vector3 Direction { get; }       // nearest-point projection geometry
public Vector3 TravelDirection { get; } // authored player forward axis
```

Nearest-route selection continues to use `Direction`; automatic enemy root yaw
uses `TravelDirection`. This prevents laterally offset trigger centers from
turning a cardinal route direction into a diagonal yaw.

## Why This Works

The preview and committed placement now call the same resolver against the same
live route state, so route edits cannot invalidate only one side of the authoring
contract. Early prefab filtering and component-scoped searches recover the
important performance win without introducing a stale cache. Separating route
projection geometry from gameplay direction also keeps spatial selection and
authored movement semantics correct at the same time.

## Prevention

- Treat placement preview equivalence as a contract: preview and commit should
  share the resolver and all mutable inputs.
- Do not cache derived editor state using cursor position alone when scene
  components also affect the result.
- Optimize by rejecting irrelevant object types and narrowing lookup scope
  before caching mutable scene-derived results.
- Test route sections with laterally offset turn spots and assert that yaw still
  follows the exact initial or outgoing direction.
- Cover the no-player fallback and Undo/Redo path so automatic alignment does
  not destroy authored manual rotation or editor recovery.

The verified change passed the focused Unity EditMode tests, both C# project
builds, and the agent harness. The authored Map 1 scene hash was unchanged by
the test run.

## Related Issues

- [Prefer prefab placement previews over SceneView line grids](../developer-experience/prefer-prefab-placement-previews-over-sceneview-line-grids-2026-06-06.md)
- [Preserve prefab root transforms in Noryangjin map-tool placement](preserve-prefab-transform-in-noryangjin-map-tool-placement-2026-06-02.md)
- [Bootstrap enemy growth in scenes without GameManager](../integration-issues/bootstrap-enemy-growth-in-scenes-without-game-manager-2026-08-03.md)
