---
title: Stamp copied Unity map-tool objects at arbitrary tiles
date: 2026-07-27
last_updated: 2026-08-03
category: docs/solutions/design-patterns
module: Unity Noryangjin map tooling
problem_type: design_pattern
component: tooling
severity: low
applies_when:
  - Authors need to repeat one adjusted scene instance at separated map tiles.
  - Copies must preserve prefab connection, overrides, added children, and transforms.
  - A copy mode needs a transparent preview and layer-aware footprint validation.
  - Editor-tool mode cancellation must clean up transient objects and materials.
tags: [unity, noryangjin, map-tool, copy-paste, scene-view, prefab-instance, editor-undo, placement-preview]
---

# Stamp copied Unity map-tool objects at arbitrary tiles

## Context

The Noryangjin map tool already had `이어 복붙`, which is deliberately a
directional chain operation: each new copy becomes the next source and is
positioned by road footprint or renderer-edge geometry. Authors also need a
different workflow for repeating an adjusted object at unrelated tiles.
Re-selecting its palette prefab loses instance-specific overrides, while
forcing arbitrary stamping into continuation would mix two different placement
contracts.

The new workflow uses `복사하기` on the selected-object card. It immediately
enters `붙여넣기 중` mode, shows a transparent copy at the hovered tile, and
stamps another copy on each valid SceneView click. The original copied object
remains the source, so the author can place the same instance state at multiple
separated tiles.

## Guidance

Keep continuation and clipboard stamping as separate actions even though both
reuse Unity's native scene duplication.

For the actual placed object, call `GameObjectUtility.DuplicateGameObject`.
That preserves the connected prefab instance, property overrides, added
children, parent, and editor Undo behavior. `UnityEngine.Object.Instantiate`
is appropriate only for the hidden transient preview, which is destroyed
without becoming authored scene data.

Store one transient source instance ID as the complete paste-mode state. A
second boolean is redundant if canceling always clears the source:

```csharp
private int copiedPlacedObjectInstanceId;

private GameObject ResolveCopiedPlacedObject()
{
    if (copiedPlacedObjectInstanceId == 0)
        return null;

    GameObject source =
        EditorUtility.InstanceIDToObject(copiedPlacedObjectInstanceId) as GameObject;
    source = ResolveVisualPickSelectionTarget(source, GameObject.Find(RootName));
    if (CanCopyPlacedObject(mapToolEnabled, source))
        return source;

    copiedPlacedObjectInstanceId = 0;
    DestroyPlacementPreview();
    DestroyPlacementPreviewMaterials();
    return null;
}
```

Translate the object by the difference between its original anchor and the
hovered destination anchor. This preserves the source's within-tile X/Z
offset and its exact Y height:

```csharp
Vector2Int anchorOffset = targetAnchor - sourceAnchor;
return MoveObjectPositionByGridStep(
    sourcePosition,
    anchorOffset.x,
    anchorOffset.y,
    fineCellSize);
```

Do the same translation to the source footprint cells before checking
occupancy. Feed those translated cells and the source placement layer into the
existing layered collision rule. This keeps roads, ordinary objects,
seagull-perch objects, and backgrounds consistent with normal palette
placement instead of inventing a copy-only collision system.

The transparent preview must clone the copied scene instance rather than its
prefab asset so instance overrides and added children remain visible. Disable
its colliders and behaviours, give all children `HideAndDontSave`, and reuse
the existing preview-material transparency path.

Treat mode cleanup as one interaction contract. Clear the source ID and
destroy both the preview object and its generated materials when:

- the author clicks `붙여넣기 중` again;
- a palette item or selection-clear action is chosen;
- MapTool changes to `OFF`;
- refresh resets transient tool state; or
- the copied source becomes invalid or leaves the map-tool root.

The same contract applies to SceneView modes that do not create a preview,
such as clicking enemies to assign them to an activation trigger. Give every
temporary interaction one explicit owner. For selection-driven modes, derive
that owner from the actual `Selection.activeGameObject`, not the object under
the cursor or an Inspector-only button.

Resolve the selected placed-object root within the actual map-tool root, then
read the mode-owner component from that placed object. Activate it only while
the map tool and its map tab are enabled. This keeps hierarchy membership,
mode guards, and selection ownership explicit without duplicating a concrete
helper signature in the documentation.

Route every context change through the same stop method. This includes the
Escape selection clear, `OnDisable`, MapTool OFF, refresh, Undo/Redo refresh,
primary-tab and content-tab changes, palette selection, paste-mode entry,
selection clearing, and owner invalidation. SceneView callbacks keep running
even when the editor window shows another tab, so a hidden mode is still
capable of consuming clicks and mutating the scene. Keep serialized mappings
hidden from the Inspector when the intended authoring contract is object
selection in SceneView.

For responsive hover assignment, reuse the height-label rectangles already
built for the current SceneView event, draw noninteractive connection visuals
only during `EventType.Repaint`, and limit grid fallback to direct children of
the `Enemies` container instead of traversing the whole authored map.

Reject the singleton `Background_Water` object just as continuation does.
Its dedicated palette action updates the one backdrop in place.

## Why This Matters

Separating the two copy semantics makes both predictable. `이어 복붙` answers
"continue from here in this direction," while clipboard stamping answers
"place this exact authored instance at this tile." Sharing only the native
duplication and occupancy primitives avoids duplicated prefab, Undo, and
collision logic.

Using the source-anchor delta rather than snapping the copied transform avoids
silently erasing manual sub-cell placement. Keeping a single transient state
value prevents the UI from saying `붙여넣기 중` after its source was cleared.
Cleaning the preview materials on every exit path prevents editor-only
resources and ghost previews from surviving a canceled mode.

## When to Apply

- Stamping manually rotated, scaled, raised, or offset props at separated
  SceneView tiles.
- Repeating connected prefab instances that contain overrides or added child
  objects.
- Adding a modal placement workflow to a Unity editor tool that already has
  palette selection and transient previews.
- Reusing an authored object's footprint with existing layer-aware occupancy
  rules.

Do not use this pattern for directional adjacency rules; keep those in
continuation. Do not use it for singleton update-in-place backdrops.

## Examples

A source named `Road_Basic_X+12_Z-05` at
`(2.93, -2.0, -1.24)` pasted to anchor `(20, 3)` with a `0.225` fine-cell
size lands at `(4.73, -2.0, 0.56)`. Its Y, rotation, local scale, connected
prefab path, property overrides, and added children remain unchanged.

Verification should include:

- pure tests for anchor-delta position and footprint translation;
- an isolated preview-scene test using a connected prefab with a non-default
  transform and an added child;
- exact regression tests for `이어 복붙` and semantic placement-root selection;
- runtime and editor project builds; and
- a before/after scene hash so EditMode verification cannot silently rewrite
  the authored map-tool scene.

Create scene-object and Undo fixtures in
`EditorSceneManager.NewPreviewScene()` and close the preview scene in a
`finally` block. A narrow test filter does not by itself protect an already
dirty authored scene: if Unity opens `Scene(s) Have Been Modified`, do not
automatically choose Save or Don't Save. Cancel the run and preserve the
author's scene state.

## Related

- [Continue Unity map-tool layouts by placement-specific geometry](continue-map-tool-layouts-by-selected-renderer-bounds-2026-07-19.md)
- [Resolve selected prefab children to Noryangjin map-tool placement roots](../logic-errors/resolve-selected-prefab-child-to-map-tool-placement-root-2026-06-08.md)
- [Prefer prefab placement previews over SceneView line grids](../developer-experience/prefer-prefab-placement-previews-over-sceneview-line-grids-2026-06-06.md)
- [Protect active Unity scenes from broad EditMode test runs](../workflow-issues/protect-active-unity-scenes-from-broad-editmode-test-runs-2026-07-18.md)
- [Select hovered height labels before SceneView visual picks](../ui-bugs/select-sceneview-visual-pick-before-grid-overlap-2026-07-27.md)
