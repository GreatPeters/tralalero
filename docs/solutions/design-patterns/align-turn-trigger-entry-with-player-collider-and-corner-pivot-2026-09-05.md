---
title: Align turn-trigger entry with the player collider while preserving the corner pivot
date: 2026-09-05
category: design-patterns
module: Noryangjin SR18 turn-trigger authoring
problem_type: design_pattern
component: tooling
severity: medium
applies_when:
  - "A runner turns on trigger entry and uses the turn spot root as its next lane origin."
  - "Existing trigger centers are at corners but wide lanes or character collider offsets change activation timing."
tags: [unity, sr18, turn-spot, collider, corner, route, authoring]
---

# Align turn-trigger entry with the player collider while preserving the corner pivot

## Context

SR18 already had all 15 required corner spots with correct positions and target yaw. Ten extension spots were only 4 units wide, and every trigger was centered directly on its corner. A character contacts a trigger with its leading collider surface, not its root pivot: the current capsule first touched about 1.18 units before the corner, while outer lanes missed the narrow extension gates.

## Guidance

Keep the turn-spot root at the actual corner. `PlayerScript.CompleteWorldYawTurn` rebases lateral movement around that root, so moving the entire object to compensate entry timing moves the next lane's center too. Offset only `BoxCollider.center` in the incoming direction.

For the measured uniform-scale, sphere-shaped player capsule:

```text
world radius                 = 0.46 * 1.5 = 0.69
world forward center offset  = 0.06 * 1.5 = 0.09
trigger half-depth           = 0.8 / 2   = 0.40
chosen entry lead            = 0.08
forward trigger center       = 0.69 + 0.09 + 0.40 - 0.08 = 1.10
```

Use an 8 × 2 × 0.8 world-space gate. Convert desired dimensions and center offsets back through each existing root scale; SR18's source spots have scales 2 and 2.5 across the road, while extension spots have scale 1. Retain roots, connected prefabs, absolute outgoing yaw, zero pitch and the 0.5-second turn duration. Reuse valid existing spots rather than adding duplicates at every visual intersection.

Back up dirty live scenes with `SaveScene(..., saveAsCopy: true)` before authorized authoring, then save only the exact target. `tools/align-sr18-turn-spots.cs` records the measured player dimensions and all before/after gate settings.

## Why This Matters

Corner placement, collision entry and lane rebasing are three related but different positions. Treating them as one point produces early turns; translating the pivot to hide that error changes downstream movement. Collider-only offsets keep those responsibilities separate without introducing runtime movement code.

## When to Apply

Use the formula only after measuring the effective collider shape, scale, forward-center offset and incoming orientation. The current capsule's height is less than its diameter and its scale is uniform, so the collider is a sphere. Recompute the support distance for an elongated/rotated capsule or another shape. Runtime physics step size, movement speed and time multiplier still affect the exact callback frame. The 0.08 lead is an authored tolerance, not a universal constant.

## Examples

- Before, the center lane touched about 1.16–1.18 before the corner and ±3-unit lanes missed all ten narrow extension gates.
- After, all 15 gates cover five lanes spanning ±3.3; analytical first contacts occur 0.06–0.08 before the corner. A player at the Y+12 deck does not overlap the ground gate.
- `Sr18TurnSpots_CoverEveryLaneAndTriggerAtTheCornerCenter` verifies 75 entries using sphere-to-oriented-box distance, guarded by assertions on the actual capsule shape. An initial EditMode `Physics.ComputePenetration` probe returned no overlaps in the isolated setup, so it was not used as evidence of gameplay collision. The mathematical test is explicitly not a full runtime traversal test.
- Seven SR18 tests and the Editor build pass. Root positions, target rotations, roads and scenery remain unchanged. Numbered overview captures show actual saved outgoing directions; markers exist only in the capture preview.

## Related

- [SR18 placement and recovery record](../../../map-concepts/sr18-turn-spots-2026-09-05/README.md)
- [Gameplay turn behavior](../../noryangjin-gameplay-maptool.md)
- [Road surface geometry validation](../logic-errors/validate-road-decks-instead-of-renderer-bounds-2026-09-05.md)
