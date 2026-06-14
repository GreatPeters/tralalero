---
title: Prefer prefab placement previews over SceneView line grids
date: 2026-06-06
category: docs/solutions/developer-experience
module: Unity Noryangjin map tooling
problem_type: developer_experience
component: tooling
severity: medium
applies_when:
  - "A Unity editor placement tool needs to show where the next object will land"
  - "A SceneView overlay grid follows the cursor and obscures the working surface"
  - "Placement validity must match the same occupancy rules used by the final placement action"
tags: [unity, map-tool, scene-view, placement-preview, noryangjin]
---

# Prefer prefab placement previews over SceneView line grids

## Context

The Noryangjin map tool originally leaned on a cursor-following SceneView line grid to explain placement. In practice that made the view noisy: the author wanted the actual object that would be installed to follow the mouse, with a simple green or red signal under it.

## Guidance

For interactive Unity editor placement tools, show a temporary `HideAndDontSave` preview instance of the selected prefab at the same transform the final placement will use. Keep line grids optional and off by default; use a compact footprint fill under the preview for placeable/blocked feedback. The preview object itself should keep the source material colors and textures with a uniform alpha, so validity color does not hide the asset shape.

The preview should not participate in scene state:

```csharp
preview.name = PlacementPreviewName;
preview.hideFlags = HideFlags.HideAndDontSave;
foreach (Collider collider in preview.GetComponentsInChildren<Collider>(true))
    collider.enabled = false;
foreach (MonoBehaviour behaviour in preview.GetComponentsInChildren<MonoBehaviour>(true))
    behaviour.enabled = false;
```

Validity should reuse the same footprint and layered occupancy helpers as real placement, so the user sees the same answer the click handler will enforce.

Do not compute the placement footprint from only the prefab bounds size anchored at the cursor cell. Many imported prefabs have root pivots that are not centered on the visible mesh, so a size-only footprint drifts away from the preview. For automatic footprints, calculate cells from the transformed preview or placed object's renderer bounds.

Physical work-grid lines should be generated on cell boundaries, not on cell centers. If cell `0` is centered at world `0`, the adjacent grid lines belong at `-0.5 * cellSize` and `+0.5 * cellSize`; otherwise the floor lines visibly cut through the placement cells. The green footprint fill should use the same half extent and visual height as the work-grid line mesh, so SceneView projection puts the black-line intersection and green-cell corner on the same pixel.

Do not make placement validity feedback more visually aggressive than the authoring task needs. A bright lime-green cell fill is enough for placeable cells; blocked cells can use the same treatment in red. Avoid outlines, thick signal lines, or hatch marks unless the author explicitly asks for high-contrast debug visualization. In SceneView, draw the placement-validity fill as a GUI overlay after the scene has rendered; if it is drawn as a world-space floor handle, transparent prefab previews and black grid lines can compound into a muddy olive even when the source color is bright lime.

## Why This Matters

A moving line grid explains coordinates, but it does not answer the author’s real question: “What object will land here, and is this spot valid?” A prefab preview reduces the mental translation between palette icon, grid cell, object bounds, rotation, scale, and collision state.

Using `HideAndDontSave` plus disabled colliders/scripts prevents the preview from being saved, selected as map content, or counted as an occupied cell.

Using the preview's actual renderer bounds prevents the green/red floor indicator from splitting away from the object when the prefab pivot is offset.

Keeping validity color on the floor, not the object, matters once assets become visually dense. A solid green or red object hides the mesh and makes it harder to judge placement. Clone the source materials for the preview, set their base color alpha to 50%, and configure the copies for transparent rendering.

SceneView color feedback must survive the actual render stack, not just pass a color-value test. If a fully bright source color still looks muddy, the likely problem is draw order or render-layer composition, not RGB values. Move the fill to a final GUI overlay layer before adding stronger visual language.

When a placed object has to be hand-aligned, treat that as either a one-off transform edit or an intentional prefab default update. The map tool should expose separate actions for "save this individual scene object" and "apply this transform as the prefab-wide placement default." The prefab-wide action copies the selected placed object's X/Z offset, height offset, yaw offset, and scale multiplier into the palette defaults asset. Without that explicit prefab-wide action, transform edits remain individual scene edits.

For map-authoring views, expose explicit SceneView presets instead of asking the author to adjust the editor camera manually. A compact toolbar toggle between an exact top orthographic view and the readable angled default view is enough: the top view helps align roads against the floor grid, while the angled view keeps object silhouettes legible during placement.

Once placement follows the SceneView mouse cursor, remove joystick-style cursor controls from the primary panel. Use that space for controls that affect what will actually be placed next, especially the yaw angle. Angle controls should work before placement by editing the selected palette item's placement yaw and refreshing the ghost preview immediately; when a placed object is selected, the same area can edit that individual object's rotation.

For fine alignment, keep the placement cell size stable and subdivide the visible work grid instead. The Noryangjin map tool creates physical grid line objects under `Noryangjin_MapTool/MapTool_Work_Grid`; each main placement cell is divided into five subcells with thinner helper lines, and top view draws a matching fixed-pixel overlay so zoomed-out alignment remains readable.

Mouse placement should snap to the visible subcell grid by default. Keep `Shift` as the coarse-snap modifier: normal movement advances one subcell, while `Shift` moves in five-subcell increments from the cursor cell where `Shift` was first pressed. Do not snap `Shift` movement to global origin-based multiples, because that makes the preview jump to unrelated grid columns instead of stepping from the author's current working point. When the coordinate unit changes to subcells, placement positions, bounds footprints, selection, deletion, and manual footprint occupancy must use the same subcell size; otherwise the preview appears to move precisely while the actual object or blocked area still uses the old coarse grid.

## When to Apply

- The editor tool places prefabs into a scene from a palette.
- The placement surface already has a stable physical or visual reference grid.
- The tool can compute placeable/blocked state before committing an object.
- The author regularly switches between precise grid alignment and object-shape inspection.
- Mouse-following placement has replaced manual cursor stepping.
- The author needs visual subcell guides for hand-tuned X/Z offsets without changing placement occupancy.
- The author expects normal placement to follow each visible subgrid cell, with `Shift` reserved for coarse jumps.

## Examples

Before: moving the mouse drew a large cyan SceneView line grid around the cursor. It was technically useful for coordinates, but it covered the scene and did not show the selected prefab.

After: moving the mouse updates a temporary `MapTool_Placement_Preview` object to the exact placement transform. The footprint beneath it is green when the current layered occupancy check allows placement and red when it is blocked.

## Related

- [Give Unity map tool scenes a physical work grid](give-unity-map-tool-scenes-a-physical-work-grid-2026-06-06.md)
- [Preserve prefab root transforms in Noryangjin map tool placement](../logic-errors/preserve-prefab-transform-in-noryangjin-map-tool-placement-2026-06-02.md)
