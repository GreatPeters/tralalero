---
title: Keep route-aligned enemy editor state synchronized with placement
date: 2026-08-03
last_updated: 2026-08-30
category: logic-errors
module: Unity Noryangjin map tooling
problem_type: logic_error
component: tooling
symptoms:
  - "Changing the player route without changing the snapped cursor cell could leave an enemy placement preview at its previous yaw."
  - "Final placement could recalculate the live route and use a different yaw than the visible preview."
  - "A stage reset could recapture a scene enemy's moved or death position as its new start after an off/on cycle."
  - "Rotating an initialized enemy could leave its cached route-relative attack axes pointing in the old direction."
  - "Undo or Redo could restore the Transform without restoring the enemy event cache."
root_cause: logic_error
resolution_type: code_fix
severity: medium
related_components: [ChapterEnemyProgression, PlayerScript, NoryangjinTurnSpot, EnemyEventController, EnemyPooler, GameManager, Unity Undo]
tags: [unity, noryangjin, map-tool, enemy-route, cache-invalidation, enemy-movement, undo-redo, object-pooling]
---

# Keep route-aligned enemy editor state synchronized with placement

## Problem

Noryangjin enemy placement derives its root yaw from the player's start frame
and the turn spots. An attempted editor optimization refreshed the preview only
when the snapped cursor cell changed, even though route transforms can change
without the cursor moving. The final placement still resolved the live route,
so the committed enemy could disagree with the preview.

The same invalidation rule also applies after an `EnemyEventController` has
captured its placement. The controller derives its start position and
route-relative forward/right axes from the Transform. Map-tool position and
rotation commands originally changed only the Transform, so an initialized
enemy could reset to its pre-edit start or use stale orthogonal attack axes.

The same controller also serves two different enable lifecycles. A pooled enemy
is enabled while still under `EnemyPooler`, before its final spawn position is
assigned, so it must capture placement later. A scene-authored enemy toggled by
`GameManager.ResetAllEnemiesByReEnable` already owns a valid start and must
restore it. Treating every `OnEnable` as a pooled spawn caused stage reset to
learn a moved or death position as the next run's start.

## Symptoms

- Moving or rotating the player route start could leave an enemy ghost at its
  old yaw until the cursor crossed into another cell.
- Editing a turn spot had the same stale-preview behavior.
- Clicking could place the enemy at a newly calculated yaw that the preview had
  not shown.
- Moving an initialized event enemy with **한 칸 이동**, offset, snap, or
  height controls could leave its run-reset start at the old position.
- Rotating an initialized enemy could leave its route-aligned attack axes on
  the old orientation, including after Undo or Redo.

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

For initialized enemies, matching the live Transform against remembered
authoring snapshots was not a safe way to infer whether an Undo affected that
enemy. Runtime movement can legitimately revisit an authored coordinate, so an
unrelated Undo could then rebase the cached anchor. An instance-only
`Undo.undoRedoPerformed` subscription was also incomplete because closing the
map-tool window removed the synchronization callback while Unity Undo remained
available.

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

Refresh initialized enemy placement state explicitly after every map-tool
Transform mutation:

```csharp
public void RefreshPlacementAfterAuthoringChange(
    Vector3 previousPosition,
    bool rotationChanged)
{
    if (!initialized)
        return;

    startPosition += transform.position - previousPosition;
    routeForward = HorizontalDirection(transform.forward, Vector3.forward);
    routeRight = HorizontalDirection(transform.right, Vector3.right);

    if (rotationChanged && RuntimeState == EnemyEventRuntimeState.Waiting)
        SnapToRouteDirection();
}
```

The one-cell, offset, snap, height, and rotation commands capture the previous
Transform and call this single refresh API. Position changes rebase the cached
start by the exact edit delta; rotation changes rebuild the route-relative axes.
Movement destinations remain external target Transforms and need no rebasing.

For Undo and Redo, keep one hidden `ScriptableObject` revision token per tracked
enemy. Record and increment that token in the same Unity Undo group as the
Transform edit. An editor-lifetime static listener synchronizes only targets
whose token revision actually changed:

```csharp
[InitializeOnLoad]
public sealed class NoryangjinMapToolWindow : EditorWindow
{
    static NoryangjinMapToolWindow()
    {
        Undo.undoRedoPerformed -= RefreshTrackedEnemyPlacementsAfterUndoRedo;
        Undo.undoRedoPerformed += RefreshTrackedEnemyPlacementsAfterUndoRedo;
    }
}
```

This listener remains active even when the map-tool window is closed.

### Separate scene re-enable from pooled placement capture

Use initialization state and pool ancestry together; `OnEnable` alone does not
identify why the object became active:

```csharp
private void OnEnable()
{
    ResolveRuntimeReferences();

    if (initialized && !IsQueuedPoolObject())
        ResetForNewRun();
    else
        PrepareForPlacementCapture();

    ActiveControllers.Add(this);
}

private bool IsQueuedPoolObject()
{
    return GetComponentInParent<EnemyPooler>(includeInactive: true) != null;
}
```

An initialized non-pooled enemy restores `startPosition`. A first-enable enemy
or one still parented under `EnemyPooler` clears initialization so the first
runtime update captures the position assigned by the spawner. Globally
preserving the old start breaks pooled reuse; globally recapturing breaks scene
reset. The scope check preserves both contracts.

## Why This Works

The preview and committed placement now call the same resolver against the same
live route state, so route edits cannot invalidate only one side of the authoring
contract. Early prefab filtering and component-scoped searches recover the
important performance win without introducing a stale cache. Separating route
projection geometry from gameplay direction also keeps spatial selection and
authored movement semantics correct at the same time.

For initialized movement, explicit mutation notifications keep the derived
cache synchronized at the moment the authoring command succeeds. The revision
token participates in Unity's actual Undo history, so the callback does not
guess from coordinates and cannot confuse ordinary runtime movement or an
unrelated Undo with a placement edit. The static listener gives the cache the
same editor lifetime as the Undo operation it follows.

For runtime re-enable, initialization state proves whether a meaningful start
already exists and pool ancestry proves whether that start belongs to the next
spawn. Keeping these signals separate prevents transient runtime transforms
from becoming authored reset data.

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
- Treat every editor command that changes a Transform as an invalidation point
  for runtime state derived from that Transform.
- Route one-cell movement, offset, snap, height, and rotation through one cache
  refresh contract instead of fixing only the first reported control.
- Record a small per-target revision token in the same Undo group when a
  derived cache must follow Undo/Redo; do not infer history from positions.
- Test window-closed Undo/Redo, rotation-axis restoration, and an unrelated
  Undo while the runtime object happens to revisit a remembered authored
  coordinate.
- Test scene re-enable and late pooled placement as separate cases. The first
  must restore the captured start; the second must capture the position assigned
  after enable.
- Create lifecycle tests in an isolated Preview Scene and clear static
  registries in setup/teardown so a dirty authored scene is never test state.

The verified change passed the focused Unity EditMode tests, both C# project
builds, and the agent harness. The authored Map 1 scene hash was unchanged by
the test run.

The 2026-08-30 lifecycle extension passed Unity compilation, both C# builds,
the agent harness, and direct Preview Scene contract verification. The
in-process Test Runner was intentionally not started while the authored map
scene contained unsaved work.

## Related Issues

- [Prefer prefab placement previews over SceneView line grids](../developer-experience/prefer-prefab-placement-previews-over-sceneview-line-grids-2026-06-06.md)
- [Preserve prefab root transforms in Noryangjin map-tool placement](preserve-prefab-transform-in-noryangjin-map-tool-placement-2026-06-02.md)
- [Resolve selected prefab children to the map-tool placement root](resolve-selected-prefab-child-to-map-tool-placement-root-2026-06-08.md)
- [Bake generated prefab UI previews and isolate EditMode tests](../workflow-issues/bake-generated-prefab-ui-previews-and-isolate-editmode-tests-2026-08-13.md)
- [Bootstrap enemy growth in scenes without GameManager](../integration-issues/bootstrap-enemy-growth-in-scenes-without-game-manager-2026-08-03.md)
- [Atomic enemy stat reload and pool-safe reset](../integration-issues/atomic-enemy-stat-workbook-reload-and-pool-safe-reset-2026-08-02.md)
- [Restore authored VFX baselines on re-enable](../ui-bugs/restore-authored-bonus-choice-vfx-baselines-on-reenable-2026-08-15.md)
- [Restore consumed turn spots on in-place run restart](../integration-issues/restore-consumed-turn-spots-on-in-place-run-restart-2026-07-25.md)
