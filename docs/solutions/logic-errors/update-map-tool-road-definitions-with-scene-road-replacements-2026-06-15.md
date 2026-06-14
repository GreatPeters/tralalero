---
title: Update map tool road definitions with scene road replacements
date: 2026-06-15
category: logic-errors
module: Unity Noryangjin map tooling
problem_type: logic_error
component: tooling
symptoms:
  - "The Noryangjin map tool scene showed replacement road objects, but the road buttons still exposed the previous straight, left-turn, and right-turn road set."
  - "Searching the editor code still found old Stage01 road labels after the scene-level replacement."
root_cause: missing_workflow_step
resolution_type: code_fix
severity: medium
tags: [unity, noryangjin, map-tool, road-prefabs]
---

# Update map tool road definitions with scene road replacements

## Problem
Replacing road objects directly in `Noryangjin_MapTool_Mode.unity` was not enough because the interactive road buttons are driven by `NoryangjinMapToolWindow.RoadPieces`. The scene could contain the desired three road examples while the tool still offered the previous straight, left-turn, and right-turn set.

## Symptoms
- The tool still showed the old road options even after the scene YAML contained `Road_Basic_Pier_Long_Fantasy_X-08_Z+00`, `Road_Bridge_Rope_Small_Fantasy_X+00_Z+00`, and `Road_Uphill_Pier_Rope_Stairs_Pillars_Fantasy_X+08_Z+00`.
- Old Stage01 road labels such as straight, left, and right road modules remained in editor code.

## What Didn't Work
- Editing only the scene placement fixed the static preview but did not change future placements from the map tool window.
- Creating wrapper prefabs was unnecessary once `RoadPieces` could point directly at the Polyperfect source prefabs and attach companion prefabs during placement.

## Solution
Update `RoadPieces` itself to the intended authoring set:

```csharp
new RoadPiece("Basic", "기본길", ".../Pier_Long_Fantasy.prefab", NoryangjinMapToolRoadTurn.Straight)
new RoadPiece("Bridge", "다리", ".../Bridge_Rope_Small_Fantasy.prefab", NoryangjinMapToolRoadTurn.Straight)
new RoadPiece("Uphill", "오르막길", ".../Pier_Rope_Stairs_Fantasy.prefab", NoryangjinMapToolRoadTurn.Straight, ".../Pier_Pillars_Fantasy.prefab")
```

Add a `CompanionPrefabPaths` field to `RoadPiece` and instantiate those companions as children of the main road instance. For the uphill road, this places `Pier_Pillars_Fantasy` with `Pier_Rope_Stairs_Fantasy` as a single authored road action.

Also remove old Stage01 road label mappings and keep Stage01 `_ROAD_` prefabs out of the selectable palette so the map tool no longer exposes the previous road set.

## Why This Works
The map tool has two separate surfaces: existing scene objects and the editor window's placement definitions. Future road placement follows `RoadPieces`, not whatever happens to be present in the scene. Changing the registry and validating it with reflection tests prevents the UI from drifting away from the requested scene layout.

## Prevention
- When replacing map tool primitives, verify both the scene YAML and the editor registry that drives placement buttons.
- Add tests that reflect private road registry entries and assert exact prefab paths plus companion prefab paths.
- Search editor code for old labels and prefab identifiers before declaring a road-set migration complete.

## Related Issues
- `docs/solutions/logic-errors/advance-unity-map-tool-cursor-after-road-turn-2026-06-01.md`
- `docs/solutions/logic-errors/preserve-prefab-transform-in-noryangjin-map-tool-placement-2026-06-02.md`
