---
title: Separate continuous slope pitch from paused route corners
date: 2026-09-05
category: design-patterns
module: Noryangjin SR18 slope traversal
problem_type: design_pattern
component: tooling
severity: medium
applies_when:
  - "A runner projects forward movement onto the ground plane but needs to traverse authored ramps."
  - "An existing turn trigger pauses translation and also acts as an analytics or route checkpoint."
  - "An authored route passes above earlier segments at different heights."
tags: [unity, sr18, slope, pitch, height-following, checkpoints, testing, camera]
---

# Separate continuous slope pitch from paused route corners

## Context

The SR18 player had 15 horizontal corner triggers and repaired ramp geometry, but forward movement projected `transform.forward` onto the ground plane. Setting the trigger's X target alone could tilt the character without ascending. Reusing the paused corner routine at every slope boundary would also stop movement and rebase the lane origin unnecessarily.

## Guidance

Use an opt-in `NoryangjinRoadHeightFollower` on the SR18 player. Keep horizontal movement and the old non-opted-in behavior unchanged, then project the requested position onto configured road MeshColliders near its current foot height. Restrict eligibility to the authored road root, upward-facing surfaces and a short ±0.75 search interval. Try tiny longitudinal nudges across decorative plank cracks. Hold the requested height if no support is found; do not snap to arbitrary scenery or a distant deck.

Represent slope transitions explicitly with `NoryangjinTurnSpot.IsSlopeTransition`. These spots ask for a moving pitch blend through `PlayerScript.RequestSlopePitch`; ordinary spots retain `RequestWorldRotation` and its translation lock. Advance pitch during normal forward movement, do not rebase the lateral lane, and apply rotation through the existing player/projectile delta path. Cancel the blend on reset, disable and ordinary corner entry.

Exclude slope-only triggers from both `TryGetRouteProgress` and `ChapterEnemyProgression.CollectRouteTurns`. A renderless trigger's presence in the same hierarchy does not make it a new route checkpoint. Expose the mode in the normal map tool so authors and agents can edit the same behavior.

For SR18, four ramps each need an entry and a level-return spot. Eight new triggers target −19.573°, +19.573° or 0° with a 0.25-second transition. The 15 corner checkpoints remain unchanged. Compute trigger-forward offsets using the incoming pitch: rotating the capsule's offset center changes the leading contact distance, even when its effective shape is spherical.

## Why This Matters

Visual orientation, terrain support, route topology and progression are separate concerns. A full-height downward ray can choose the upper crossing while the player is below it; a pitch trigger counted as a corner can change stage analytics and enemy ordering. Local support sampling and explicit transition semantics avoid both problems without changing every player's physics or adding new route branches.

## When to Apply

This approach fits static authored roads with modest per-frame height changes. It is not a general falling/terrain controller. Changed road membership requires recaching; altered player shape, very large speed/time-factor changes or a jump beyond the probe range require revalidation. The current component preserves explicit Rigidbody position writes and does not depend on gravity or changing the existing constraints.

## Examples

- Three lateral positions continuously traversed both elevation spans in 4,320 height samples without snapping between decks.
- Unit tests verified moving pitch, unchanged horizontal distance, no corner lock/lane rebase, reset, pause, missing-follower/wrong-heading rejection, and one-shot checkpoint exclusion.
- One initial test expected `FindObjectsByType` to see objects in a preview scene. It returned no route turns. The global-query assertion was moved to the actual SR18 scene test; preview fixtures still test explicit scene enumeration. Do not weaken the production filter to satisfy an invisible test fixture.
- A bounded Play Mode probe moved the real character through both elevation spans. All eight pitch triggers fired, both runs peaked at Y=12.17645 and returned to Y=0.160856, and 421 recorded samples contained no stationary intervals or paused corner turns. The character's existing child camera followed the slope. Four runtime screenshots were inspected.
- Verification passed 9 SR18 tests, 6 slope tests and 48 existing turn tests. The Editor build passed with two existing warnings. This is not a full-stage combat or mobile-performance certification.

## Related

- [Slope setup and runtime evidence](../../../map-concepts/sr18-slope-spots-2026-09-05/README.md)
- [Collider entry versus corner pivot](align-turn-trigger-entry-with-player-collider-and-corner-pivot-2026-09-05.md)
- [Road surface validation](../logic-errors/validate-road-decks-instead-of-renderer-bounds-2026-09-05.md)
