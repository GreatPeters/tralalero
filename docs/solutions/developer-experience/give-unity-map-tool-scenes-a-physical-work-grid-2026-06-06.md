---
title: Give Unity map tool scenes a physical work grid
date: 2026-06-06
last_updated: 2026-08-30
category: docs/solutions/developer-experience
module: Unity Noryangjin map tooling
problem_type: developer_experience
component: tooling
severity: medium
applies_when:
  - "An editor-only Unity placement scene is hard to read before any authored stage geometry exists"
  - "Scene view placement depends on a custom Handles grid that can disappear when the tool window is not active"
  - "A neutral guide surface must not be treated as gameplay or placed map content"
tags: [unity, map-tool, scene-view, grid, noryangjin]
---

# Give Unity map tool scenes a physical work grid

## Context
The Noryangjin map tool scene opened with only a root, camera, light, and custom SceneView Handles grid. When the view drifted or the map tool window was not drawing overlays, the user had no stable surface to read. A first pass that added only a large Lit floor made the view worse because the material picked up scene lighting and read as a dark slab.

## Guidance
Editor placement scenes should include a non-gameplay physical work surface:

- A neutral Unlit floor below the placement height.
- Thin physical grid-line cubes above that floor so cells remain visible even outside the tool overlay path.
- A small origin marker for orientation.
- Initial SceneView framing when the tool scene is first created.
- Names that do not match placement-object patterns such as `Road_*_X+00_Z+00` or `Prop_*_X+00_Z+00`.

In `NoryangjinMapToolWindow`, `OpenOrCreateMapToolScene()` re-applies scene defaults for existing scenes without replacing their current SceneView framing. Newly created tool scenes receive the initial automatic framing. `SetupMapToolSceneDefaults()` creates `MapTool_Work_Floor`, `MapTool_Work_Grid`, and `MapTool_Origin_Post`, while placed-object collection still only accepts objects whose names encode grid coordinates.

Keep the placement cell size stable when authors need more layout room. Expand the work-grid display radii instead so snapping, prefab scale, and existing object coordinates do not change. The Noryangjin map tool defaults to `-300..+300` main cells on both X and Z (601 by 601 cells) while retaining the `1.125` main-cell size. Authors can change the horizontal X and vertical Z top-view radii independently to `50..1200` from `편의 > 작업 그리드 범위`; the presets set both axes together. This repaints only the Editor overlay and does not rewrite the authored scene.

Refresh actions and existing-scene opens should restore the map-tool guide objects without moving an already-authored `MapTool_Camera` or replacing the author's current SceneView framing. Scene creation may assign an initial camera transform and SceneView preset; subsequent repair paths preserve them.

For high-contrast top-view grid overlays, do not depend only on an editor-window boolean such as `isTopSceneView`. Script reloads and window recreation can reset that serialized UI state while the SceneView is still in an actual top orthographic orientation. Gate the overlay on either the explicit tool toggle or the SceneView camera state: orthographic, with its forward vector pointing down toward `Vector3.down`.

## Why This Matters
A map tool is only useful if authors can immediately see where a placement will land. Custom overlay grids are helpful but fragile because they depend on an editor window subscription and current SceneView state. Physical guides make the scene readable after reloads, screenshots, and accidental view changes.

Unlit materials matter for tooling guides. Lit materials can turn a neutral floor brown, too dark, or otherwise inconsistent depending on render pipeline, skybox, and directional light. That makes the guide compete with actual map content instead of clarifying placement.

Stateful editor windows make this more subtle: the scene can still contain hundreds of active `MapTool_Work_Grid_*` and `MapTool_Work_SubGrid_*` objects, but the readable black top-view Handles overlay can vanish if the window's top-view flag resets. In that case, inspect both the scene objects and the draw predicate before assuming the grid asset was deleted.

## When to Apply
- The scene is an editor-only layout or map-authoring workspace.
- The first step in the workflow is placing assets on an empty plane.
- The tool uses SceneView overlays, but the workspace should still be readable when overlays are absent.
- Guide objects must not affect occupancy checks, prefab placement, gameplay, or export logic.

## Examples
Before: `Noryangjin_MapTool_Mode.unity` had only `Roads`, `Props`, `MapTool_Camera`, and `MapTool_DirectionalLight`, so the user saw a confusing default grid/floor and had trouble placing assets.

After: the scene contains a stable `MapTool_Work_Floor`, visible `MapTool_Work_Grid_*` line objects, and `MapTool_Origin_Post`. Creating the tool scene starts from a readable orthographic angle, while reopening an authored scene preserves its current framing.

After the 2026-06-12 reload fix: `DrawStableTopViewWorkGridOverlay` receives the active `SceneView` and draws when `ShouldDrawStableTopViewWorkGridOverlay(...)` sees either the tool's top-view toggle or a top orthographic SceneView rotation. The regression test uses `Quaternion.Euler(90f, 0f, 0f)` to keep that fallback behavior explicit.

## Related
- [Use Continuous Procedural Bases For Unity Stage Layouts](../design-patterns/use-continuous-procedural-bases-for-unity-stage-layouts-2026-05-25.md)
- [Preserve prefab root transforms in Noryangjin map tool placement](../logic-errors/preserve-prefab-transform-in-noryangjin-map-tool-placement-2026-06-02.md)
- [Create Unity Layout Scenes When Editor Execution Is Blocked](../workflow-issues/create-unity-layout-scene-when-editor-execution-is-blocked-2026-05-25.md)
