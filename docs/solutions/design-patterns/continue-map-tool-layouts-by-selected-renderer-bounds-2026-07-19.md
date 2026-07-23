---
title: Continue Unity map-tool layouts by placement-specific geometry
date: 2026-07-19
last_updated: 2026-07-23
category: docs/solutions/design-patterns
module: Unity Noryangjin map tooling
problem_type: design_pattern
component: tooling
severity: medium
applies_when:
  - Authors need to repeat a manually adjusted road or prop without rebuilding its placement settings.
  - Roads must repeat by their authored grid footprint instead of renderer padding.
  - Tiled backgrounds need a controlled seam overlap without being snapped to the road grid.
  - Repeated copies must preserve prefab identity, child objects, transform overrides, and editor Undo support.
  - Safe recovery exposes that a previously working editor feature existed only in uncommitted working-tree code and must be reconstructed from durable design records.
symptoms:
  - The upper-right directional continuation controls disappear after recovery to committed HEAD.
  - A continued road can look correct while its blue occupied-tile footprint changes on the next chained copy.
  - A detached copy can fall back from a manual 10x6 footprint to renderer bounds and shorten the continuation interval.
root_cause: wrong_api
resolution_type: code_fix
tags: [unity, noryangjin, map-tool, duplication, prefab-instance, grid-footprint, renderer-bounds, editor-undo]
---

# Continue Unity map-tool layouts by placement-specific geometry

## Context

Manual palette placement is inefficient when an author has already adjusted a road's rotation, scale, height, or companion children and wants to continue the same structure. Re-selecting the prefab and reconstructing those settings is not equivalent to duplicating the authored placement.

The July 23 restoration exposed a separate persistence failure. The toolbar action and its tests had been implemented previously but never committed, so safe recovery correctly restored committed HEAD without them. The surviving document described the interaction and geometry precisely enough to reconstruct the feature test-first. After recovery, absence of the `이어 복붙` symbols and tests was evidence of missing working-tree state, not evidence that the design had been intentionally removed.

Continuation geometry is not universal. Roads are logical grid modules, sea and ground backgrounds are freeform visual tiles, and ordinary props are arbitrary rendered objects. Applying renderer-edge contact to all three produced a Basic-road interval of `11.475`: the authored `50 * 0.225 = 11.25` span plus one fine cell. Mathematically touching background renderer bounds could still leave the same `0.225` visual seam.

Continuation also depends on prefab provenance. `UnityEngine.Object.Instantiate` copied the selected hierarchy's visible state but detached the result from its prefab. The first copy could therefore look correct, while the next copy could no longer resolve its palette entry and silently fall back to renderer bounds. In the reproduced Basic-road chain, the first copy reached `X+62`, but continuing from it reached `X+89` instead of `X+112`. The same loss made a copied RightTurn display the wrong blue occupied-tile footprint.

## Guidance

Resolve the current Unity selection to the semantic map-tool placement root and duplicate that root with `GameObjectUtility.DuplicateGameObject`. This uses Unity Editor duplication semantics, preserving a connected prefab instance, its overrides, added children, and its parent. Do not use `UnityEngine.Object.Instantiate` for this scene-authoring action.

Expose the action in the MapTool's upper-right toolbar as `북/동/남/서` plus `이어 복붙`. Prefer the selected placement root; if the selection is empty or unrelated, fall back to the last placed object. Select and register the duplicate after every action so another click extends the chain. Disable both controls while MapTool is `OFF`, and reject the singleton `Background_Water` backdrop.

Unity 6000.2.6f1 exposes the public one-argument `DuplicateGameObject` overload. An attempted `DuplicateGameObject(source, false)` call does not compile in this project. Let the native editor duplication register creation Undo, then call `Undo.RecordObject` for the duplicate root and transform before assigning the final map-tool name and position. Keep the whole operation in the existing Undo group and register a complete snapshot of the MapTool window before changing its cursor. One Undo then removes the copy and restores the previous cursor; Redo restores the connected prefab instance.

For scene copies that were already detached by the old implementation, normal prefab lookup still returns an empty path. Recover only exact known road names: parse a valid `Road_<RoadPiece.Label>_X<int>_Z<int>` suffix and map the label through `RoadPieces`. This restores the palette footprint without guessing from arbitrary object names. Then choose the movement rule from the placement layer.

### Roads

Treat the palette's manual displayed footprint as the authoritative geometry. Scale the footprint to the fine work grid, project its inclusive cell span onto the selected cardinal direction, and move by that integer cell delta. Use the same calculated anchor for the transform delta, coordinate suffix, and continuation cursor.

For example, a Basic road with a `6 x 10` palette footprint occupies `30 x 50` fine cells. Continuing north or south advances `50` cells, exactly `11.25` world units at the `0.225` fine-cell size. Renderer padding must not affect that interval.

Swap the footprint axes only when the placed road has an additional odd quarter-turn relative to the prefab's authored root yaw. Compare relative yaw, not absolute yaw: a Basic road placed at `90` degrees is rotated from its native `0`, while a RightTurn prefab authored at `90` and placed at `90` is not.

### Backgrounds

Keep sea and manually placed ground freeform. Calculate the opposing renderer-edge distance, then subtract one fine cell so adjacent copies overlap by `0.225` and hide their visible seam. Do not snap the actual background transform; rounding is only for its coordinate suffix and the toolbar cursor. If neither object has measurable renderer size, retain the full one-cell fallback movement.

### Ordinary objects

Continue arbitrary props by opposing renderer edges with no forced grid snap and no seam overlap. For an eastward copy, the offset remains:

```csharp
NoryangjinMapToolDirection.East => new Vector3(
    sourceBounds.max.x - duplicateBounds.min.x,
    0f,
    0f);
```

After moving any copy:

- update its coordinate suffix from the calculated duplicate anchor;
- select the duplicate so the next click continues from it;
- record final name and transform mutations in the same Undo group as native editor duplication;
- mark the duplicate and scene dirty;
- disable both the direction control and duplication action while the map tool is `OFF`.

Keep this action separate from palette placement. Palette placement rebuilds an instance from prefab defaults, while continuation duplication intentionally preserves the selected scene instance's current state and children.

The implementation and its regression tests live in `Assets/ShooterSurvival/Editor/NoryangjinMapToolWindow.cs` and `Assets/Tests/Editor/NoryangjinMapToolGridUtilityTests.cs` respectively. Keep both changes in the same durable checkpoint; restoring only one recreates either an unprotected feature or tests for symbols that no longer exist.

## Why This Matters

Each layer now follows its authoring invariant. Grid-authored roads cannot accumulate mesh-padding drift, tiled backgrounds retain arbitrary transforms while hiding their seam, and ordinary objects still adapt to rotated, scaled, or asymmetrical renderer geometry. Preserving prefab identity ensures those rules remain discoverable on the second and later copies rather than degrading after the first action. Exact-name recovery gives old detached roads the same footprint behavior without pretending that they became connected prefab instances.

Selecting the new copy turns a single action into a repeatable chain. Resolving to the placement root prevents a selected child mesh from being duplicated without its owning road or companion hierarchy.

Gating the toolbar controls with the map-tool enabled state is part of the interaction contract. An editor action that still mutates the scene while the tool says `OFF` makes the toggle misleading and is easy to miss when adding a compact toolbar button.

## When to Apply

- Directional repetition of grid-authored roads, including prefabs with native root rotation.
- Freeform repetition of sea or manually placed ground that needs a one-fine-cell seam overlap.
- Bounds-based repetition of arbitrary docks, walls, or props.
- Scene instances whose manual transform overrides must be retained.
- Editor workflows where the most recently created object should become the next repetition source.

Do not use continuation duplication for singleton backdrops such as `Background_Water`; those should keep their dedicated update-in-place behavior.

## Examples

If a selected Basic road uses the `30 x 50` fine-cell footprint, choosing north advances its anchor and transform by `50` cells rather than by its renderer AABB length. A regression test must perform the action twice: the detached implementation passed the first interval but changed the second anchor from the expected `X+112` to `X+89`.

For a native-rotation RightTurn with a `10 x 6` footprint, verify both axes: east advances `50` fine cells (`11.25` units), while north advances `30` fine cells (`6.75` units). If a selected ocean backdrop is continued east, its renderer bounds overlap the copy by one `0.225` fine cell. The duplicate becomes selected in each case, so the next click continues the same chain.

Tests should assert that the first and second copies remain `PrefabInstanceStatus.Connected`, resolve to the same prefab asset path, and retain added children and transform overrides. Also cover already-detached known-road names, all four directions for normal and swapped road footprints, relative prefab yaw, actual native-rotated turn prefabs, exact background overlap, ordinary-object no-overlap policy, the zero-renderer fallback, selection-first/last-placed source resolution, Undo/Redo, and source-scene preservation.

Use preview scenes and narrow EditMode filters, record the active scene hash before the run, and compare it afterward so verification does not silently mutate the open authored scene. A preview scene cannot be made active, so operate on objects moved into it without calling `SceneManager.SetActiveScene`. Do not try to create a second additive unsaved scene when the test runner already owns an untitled unsaved scene; Unity rejects that setup.

For the July 23 restoration, nine exact continuation tests passed individually. `Assembly-CSharp.csproj` and `Assembly-CSharp-Editor.csproj` built with zero warnings and errors, the agent-harness validation passed, and the authored `Noryangjin_MapTool_Mode.unity` SHA-256 remained unchanged from its pre-test baseline.

## Related

- [Advance Unity map tool cursors after applying road turns](../logic-errors/advance-unity-map-tool-cursor-after-road-turn-2026-06-01.md)
- [Delete broad-footprint map-tool objects from any occupied cell](../logic-errors/delete-map-tool-broad-footprint-with-empty-cell-2026-06-20.md)
- [Preserve prefab root transforms in Noryangjin map tool placement](../logic-errors/preserve-prefab-transform-in-noryangjin-map-tool-placement-2026-06-02.md)
- [Prefer prefab placement previews over Scene-view line grids](../developer-experience/prefer-prefab-placement-previews-over-sceneview-line-grids-2026-06-06.md)
- [Resolve selected prefab children to Noryangjin map tool placement roots](../logic-errors/resolve-selected-prefab-child-to-map-tool-placement-root-2026-06-08.md)
- [Generate Unity map-tool sibling scenes fail-closed](../workflow-issues/generate-unity-map-tool-sibling-scenes-fail-closed-2026-07-15.md)
- [Protect active Unity scenes from broad EditMode test runs](../workflow-issues/protect-active-unity-scenes-from-broad-editmode-test-runs-2026-07-18.md)
