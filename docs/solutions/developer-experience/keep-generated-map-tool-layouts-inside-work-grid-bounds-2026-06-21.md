---
title: Keep Generated Map Tool Layouts Inside Work Grid Bounds
date: 2026-06-21
category: docs/solutions/developer-experience
module: Noryangjin map tool layout generation
problem_type: developer_experience
component: tooling
severity: medium
applies_when:
  - "Generating a Unity map-tool scene from scripted route coordinates"
  - "The Game view shows the authored layout pushed to one edge with empty water or background filling the frame"
  - "Generated objects use multiple parent buckets such as Roads, Props, and Water"
tags: [unity, map-tool, scene-generation, camera-framing, noryangjin]
---

# Keep Generated Map Tool Layouts Inside Work Grid Bounds

## Context

A generated Noryangjin map-tool layout initially used route coordinates from `Z -65` to `Z +30`, while the map tool work grid is designed around the default `-20..+20` cell range. The result looked wrong in Game view: the authored harbor-market set was pushed toward the top of the frame and the lower half showed mostly empty water.

## Guidance

Treat the map tool grid bounds as part of the generation contract. Keep generated route and set-dressing coordinates inside the visible authoring range unless the tool explicitly expands the work grid. For the Noryangjin concept pass, the generated route was compacted to 14 nodes and the generated coordinate range was verified at `X -19..19`, `Z -18..22`.

Frame the camera from the route bounds instead of fixed coordinates. After creating the route, build bounds from the route nodes, expand by a practical side margin for facades and harbor props, then set the map-tool camera from that center.

Clear generated prefixes in every parent bucket that can contain generated objects. If background objects are placed under `Water` with names like `Prop_Concept...`, clearing only `Concept_` under `Water` leaves stale far-away objects that keep polluting the scene and verification range.

## Why This Matters

Map-tool scenes are authoring surfaces, not infinite preview canvases. If generated content falls outside the grid, the user sees a broken composition and the placement tool becomes harder to reason about. Bounds-based camera framing and prefix cleanup make regeneration deterministic and keep screenshots honest.

## When to Apply

- A scripted layout generator emits grid coordinates directly.
- Existing generated content is regenerated in place rather than creating a fresh scene.
- Scene verification can parse generated object names with `_X.._Z..` suffixes.

## Examples

Before: route nodes reached `Z -65`, water/background props remained under `Water`, and the camera was hard-coded to `(16, 20, -23)`.

After: route nodes stay near the work grid, stale `Water/Prop_Concept...` children are cleared, and `MapTool_Camera` is centered from generated route bounds.

## Related

- [Give Unity map tool scenes a physical work grid](give-unity-map-tool-scenes-a-physical-work-grid-2026-06-06.md)
- [Run Unity Scene Generation Through CLI Connector Exec When Editor Reload Is Stale](../workflow-issues/run-unity-scene-generation-through-cli-connector-exec-when-editor-reload-is-stale-2026-06-21.md)
