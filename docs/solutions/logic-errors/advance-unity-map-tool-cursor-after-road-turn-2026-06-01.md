---
title: Advance Unity map tool cursors after applying road turns
date: 2026-06-01
category: docs/solutions/logic-errors
module: Unity Noryangjin map tooling
problem_type: logic_error
component: tooling
symptoms:
  - Noryangjin map tool could place left and right road modules without changing the active heading.
  - Advance-after-road used the entry direction, so the cursor moved to the wrong next grid cell after a corner.
root_cause: logic_error
resolution_type: code_fix
severity: medium
tags: [unity-editor, map-tool, noryangjin, road-turns, tdd]
---

# Advance Unity map tool cursors after applying road turns

## Problem
`NoryangjinMapToolWindow` treated straight, left-turn, and right-turn road modules as if they all exited in the same direction they entered. That made repeated placement unreliable: after placing a corner, the next road tile would be stamped from the wrong grid coordinate unless the user manually corrected the cursor and direction.

## Symptoms
- Left and right road buttons placed the correct prefab but did not update the active `direction`.
- `advanceAfterRoad` moved the cursor using `DirectionToStep(direction)` before any turn-specific heading was applied.

## What Didn't Work
- Existing grid utility tests covered world conversion, snapping, yaw, and naming, but not turn semantics.
- Scene-builder tests validate generated Stage01_2 output, but they do not exercise the interactive map tool cursor workflow.

## Solution
Model the road turn explicitly and update the heading before advancing the cursor:

```csharp
direction = NoryangjinMapToolGridUtility.DirectionAfterRoadTurn(direction, roadPiece.Turn);
Vector2Int step = NoryangjinMapToolGridUtility.DirectionToStep(direction);
gridX += step.x;
gridZ += step.y;
```

The regression test should assert both the resulting heading and the cursor step. For example, north plus `Left90` should produce west and `(-1, 0)`, while east plus `Right90` should produce south and `(0, -1)`.

## Why This Works
Road placement has two different headings: the heading used to orient the piece being placed, and the heading used by the next piece leaving that tile. Straight pieces keep both headings the same; corner pieces do not. Applying the turn before cursor advancement keeps the tool aligned with the road path the user is authoring.

## Prevention
- When adding new road variants, define whether they are straight, left-turn, right-turn, or a different transition before wiring the button.
- Keep map-tool cursor tests focused on editor-independent utility methods so they run quickly in EditMode.
- For interactive Unity tools, test the authoring state transitions directly, not only generated scene output.
- After adding Unity Editor tests, force a script recompile or asset refresh and confirm the expected test count increased before trusting a green run.

## Related Issues
- `docs/solutions/workflow-issues/create-unity-layout-scene-when-editor-execution-is-blocked-2026-05-25.md`
- `docs/solutions/design-patterns/flatten-road-prefabs-as-surface-skins-for-unity-runner-previews-2026-05-27.md`
