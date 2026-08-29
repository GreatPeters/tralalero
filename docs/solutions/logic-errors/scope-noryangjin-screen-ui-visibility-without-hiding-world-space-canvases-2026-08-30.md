---
title: Scope Noryangjin Screen UI Visibility Without Hiding World-Space Canvases
date: 2026-08-30
category: docs/solutions/logic-errors
module: Unity Noryangjin map tooling
problem_type: logic_error
component: tooling
symptoms:
  - "A root World Space Canvas could be disabled by a control intended only for screen UI."
  - "Mixed active states across root screen-space Canvases could appear fully disabled."
  - "Prefab-backed active-state edits could fail to persist without explicit override recording."
root_cause: logic_error
resolution_type: code_fix
severity: medium
related_components:
  - "testing_framework"
  - "development_workflow"
tags:
  - "unity"
  - "noryangjin"
  - "map-tool"
  - "canvas"
  - "screen-space-ui"
  - "world-space-ui"
  - "mixed-state"
  - "editor-undo"
---

# Scope Noryangjin Screen UI Visibility Without Hiding World-Space Canvases

## Problem

The Noryangjin map tool needed `UI 활성화` and `UI 비활성화` controls for clearing screen UI out of the authoring view. The control must affect only root screen-space canvases while preserving player, enemy, bonus, and other world-space UI.

## Symptoms

- A root `Canvas` using `RenderMode.WorldSpace` could be mistaken for screen UI and disabled.
- A mixed set of active and inactive root screen-space canvases appeared as fully disabled.
- A selected disabled tab could not clearly express that the scene was actually mixed.
- Direct activation changes needed Undo, scene dirtiness, repainting, and prefab-instance override recording to remain recoverable and persistent.

## What Didn't Work

The first target predicate treated every root Canvas as screen UI:

```csharp
foreach (GameObject root in scene.GetRootGameObjects())
{
    Canvas canvas = root.GetComponent<Canvas>();
    if (canvas != null)
        canvases.Add(canvas);
}
```

Root hierarchy position is not a UI-role contract. A world-space marker or health display can also be a scene root.

The first state reduction was binary: any inactive Canvas made the aggregate look disabled. That loses the distinction between `all inactive` and `mixed`, so the toolbar can show a state that is not true.

## Solution

Define the target set by both scene ownership and render mode:

```csharp
private static List<Canvas> GetSceneRootCanvases(Scene scene)
{
    var canvases = new List<Canvas>();
    if (!scene.IsValid() || !scene.isLoaded)
        return canvases;

    foreach (GameObject root in scene.GetRootGameObjects())
    {
        Canvas canvas = root.GetComponent<Canvas>();
        if (canvas != null && canvas.renderMode != RenderMode.WorldSpace)
            canvases.Add(canvas);
    }

    return canvases;
}
```

Represent target visibility as a three-state toolbar index:

```csharp
internal static int GetSceneRootCanvasVisibilityTabIndex(
    Scene scene,
    out int canvasCount)
{
    List<Canvas> canvases = GetSceneRootCanvases(scene);
    canvasCount = canvases.Count;
    if (canvasCount == 0)
        return -1;

    bool firstCanvasActive = canvases[0].gameObject.activeSelf;
    foreach (Canvas canvas in canvases)
    {
        if (canvas.gameObject.activeSelf != firstCanvasActive)
            return -1;
    }

    return firstCanvasActive ? 0 : 1;
}
```

The indices mean:

- `0`: every targeted screen-space Canvas is active.
- `1`: every targeted screen-space Canvas is inactive.
- `-1`: no target exists or the target set is mixed. `canvasCount` distinguishes those cases.

`GUILayout.Toolbar` accepts `-1` as no selected tab. In a mixed state, either action remains available and can normalize all targets.

Apply changes through the existing active-state helper, then complete the Unity editor persistence contract:

```csharp
foreach (Canvas canvas in GetSceneRootCanvases(scene))
{
    bool canvasChanged = SetGameObjectActive(
        canvas.gameObject,
        active,
        undoName,
        recordUndo);

    if (canvasChanged && PrefabUtility.IsPartOfPrefabInstance(canvas.gameObject))
        PrefabUtility.RecordPrefabInstancePropertyModifications(canvas.gameObject);

    changed |= canvasChanged;
}

if (changed)
{
    EditorSceneManager.MarkSceneDirty(scene);
    SceneView.RepaintAll();
}
```

## Why This Works

`Canvas.renderMode` captures the distinction the feature actually cares about. Root scope avoids touching nested player, enemy, and bonus canvases; the render-mode guard also protects a world-space Canvas that happens to live at the scene root.

The explicit mixed state prevents a lossy boolean reduction. The author sees no selected action until they choose the desired normalized state.

Undo recording captures the exact prior per-object state, including a mixed state. Marking the scene dirty makes the authoring change saveable, while `RecordPrefabInstancePropertyModifications` keeps a connected prefab instance from reverting after save and reload.

## Prevention

- Define Canvas visibility tools by role: screen space versus world space, roots versus descendants, and active scene versus all loaded scenes.
- Do not reduce a multi-object aggregate to a boolean when mixed state changes which actions remain available.
- Route editor mutations through existing Undo-aware helpers.
- Record prefab-instance property modifications after programmatic changes.
- Test inside a preview scene and always close it in `finally`.
- Cover two root screen-space canvases starting from opposite `activeSelf` values.
- Assert that root and nested world-space canvases remain active through both toolbar actions.
- Verify Undo restores the original mixed state and Redo reapplies the normalization.
- Keep a before/after hash guard around the authored map-tool scene when running focused EditMode verification.

## Related Issues

- [Migrate legacy player status UI to screen-space HUD safely](../architecture-patterns/migrate-legacy-player-status-ui-to-screen-space-hud-safely-2026-08-18.md)
- [Resolve selected prefab children to map-tool placement roots](resolve-selected-prefab-child-to-map-tool-placement-root-2026-06-08.md)
- [Scope Unity map optimizations to scene instances](scope-map-optimizations-to-scene-instances-2026-07-30.md)
- [Stamp copied map-tool objects at arbitrary tiles](../design-patterns/stamp-copied-map-tool-objects-at-arbitrary-tiles-2026-07-27.md)
