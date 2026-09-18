---
title: Separate authored enemy resets from reusable encounter spawns
date: 2026-09-13
category: design-patterns
module: Road chapter route and holdout encounters
problem_type: design_pattern
component: tooling
severity: high
applies_when:
  - "A scene-authored enemy is reused at different spawn locations."
  - "A forward runner temporarily becomes a stationary combat encounter."
  - "A curved road has an offset branch and keeps route-relative controls."
tags: [unity, enemy-pooling, holdout, curves, playerprefs, playtesting]
---

# Separate authored enemy resets from reusable encounter spawns

## Context

The rest-stop holdout reused cartoon police through a bounded inactive array. Its planned door schedule was correct, but `EnemyEventController.OnEnable` restored the actor's first captured placement. Assigning a new transform immediately before enabling it did not change that cached start. Earlier tests proved the schedule and timer while missing the actual spawn locations.

The same revision replaced paused highway corners with continuous curves. Offset branches magnified discontinuities in the normals of a piecewise-linear centerline. A numerical heading check found a 13.86-degree jump over one progress metre even though every sampled point had floor support.

## Guidance

Keep ordinary authored restart semantics. Add an explicit inactive-actor spawn preparation step that invalidates the old placement capture before setting the new transform. Enabling and activating the actor then captures the new door. Reject preparation on an active actor so a live encounter is not silently reset. Test one reused actor across all four door positions; checking only the first spawn or a schedule array is insufficient.

Record actual per-door spawn counts and the difference between the requested and activated position. The verified holdout produced 6/7/8/6 police from four doors with zero position error. Leave the legacy `EnemyPooler` ownership rules intact; a separate scene-owned array is not automatically recognized by an ancestor-type check.

Give stationary combat its own player movement lock. Keep the global combat clock running, hold the player's world position, aim only newly emitted shots, and rotate the visible body independently of the route/camera frame. Restore the exact camera pose, visible-body rotation, movement permission and inactive police population on every exit. Exercise successful completion, pause and death with real projectile/contact callbacks.

Use a continuously differentiable centerline before applying an offset normal to form a bypass. Bake the road meshes from the same sampling function used by the player. Check heading change as well as support at both lateral limits and on both branches. Existing projectile rotation, helper navigation and analytics may depend on old corner triggers and need explicit opt-in route handling.

## Why This Matters

Timer success, a declared four-door schedule, or a count of animated enemies can all pass while the visible game is wrong. Authored reset and pooled rental are different lifecycles. Floor continuity and heading continuity are also different contracts. Each needs evidence at its consumer.

## Examples and verification

- `EnemyEventController.PrepareSpawnAt` and `RestStopHoldout.Spawn` implement the new capture boundary. `RoadChapterPatternTests.ReusedPoliceCaptureTheNewDoorInsteadOfResettingToTheFirstDoor` reproduces the old failure.
- Nineteen focused tests include route support/continuity, recovery without reducing excess health, movement ownership, repeated spawns, and progress reporting without old triggers.
- The successful 30-second holdout used seeded late-chapter upgrades; the full highway traversal used a maintained health override. Neither is a replacement for an ordinary progression cohort.
- A one-time endurance health boost was replaced by a health wall during the route, so that diagnostic died before the exit. Maintain and label the override for a reachability-only run, and keep the failed result as history.
- Existing Edit Mode analytics tests wrote real upgrade PlayerPrefs. Snapshot before any native tests, compare after them, and restore proven test writes only after the last scene-open callback. Final verification matched all 63 original keys.
- A QA scene copied without the complete prefab/model metadata had missing references despite existing files. Synchronize dependency metadata and verify real renderers before accepting screenshots.

## Related

- [Evidence and scope](../../../map-concepts/road-patterns-2026-09-13/README.md).
- [Pool ownership during workbook reload](../integration-issues/atomic-enemy-stat-workbook-reload-and-pool-safe-reset-2026-08-02.md) covers global reset inclusion; this encounter's issue is recapturing a reusable actor's position.
- [Verify runtime presentation beyond numeric checks](../workflow-issues/verify-runtime-presentation-and-progression-beyond-numeric-checks-2026-09-10.md).
- [Separate slope pitch and paused route corners](separate-slope-pitch-from-paused-route-corners-2026-09-05.md).
