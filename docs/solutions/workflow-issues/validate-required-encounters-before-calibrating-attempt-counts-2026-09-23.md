---
title: Validate required encounters before calibrating attempt counts
date: 2026-09-23
module: Later chapter progression and encounter lanes
problem_type: workflow_issue
component: testing_framework
severity: high
tags: [unity, balance, earned-progression, collision, random-bonuses, playerprefs]
---

# Validate required encounters before calibrating attempt counts

## Context

The user requested about30attempts to first clear Highway and RestStop after the earlier earned campaign cleared them in19and7. Stages should still last about300seconds. The target concerns natural growth, without an attempt counter that prevents victory.

## Findings

Reducing Highway rewards100→65 did not simply turn19attempts into30. A real trial still failed after34attempts, repeatedly at285seconds. The earlier clear collected six HP-percent bonuses; several later failures collected none or two. Permanent levels and an endpoint seconds-per-shop-visit statistic therefore do not certify stable progression. Inspect actual pickup choices, HP before fatal contact and earned purchases.

RestStop also had a wide outside route around live enemies. Lowering its rewards without addressing that passage would make grinding longer while allowing an edge-following player to bypass the intended growth requirement. The earlier final row could remain alive behind a cleared player.

## Implementation and evidence

RestStop reuses the existing encounter rows and passage width curve, applied in world coordinates because it has no HighwayRoute component. The player's common position method covers forward motion and lateral input. Row heading, lateral distance and elevation separate nearby roads; stationary defense and world turns retain their movement behavior. New rails provide visible geometry for every constraint. Source capsules are unchanged.

The two workbook-disabled outdoor rows in the food hall retain full width and hidden rails. All25definitions remain editable, with23active passages. Their lane/collision claims reset each run. Hazards are beyond the funnel exit: the source layout places them at least22.2m after an enemy, while the throat releases by6m after it.

Three native probes hold left, center or right input using the actual first encounter. Outgoing fire is suppressed only to isolate contact; HP and ordinary movement are not overridden. All reduce500HPto0viaEnemyContact, with final lanes−1.85/0/+1.85. Four-heading math, no-input position application, disabled food-hall rows and saved-scene alignment are also tested.

Author corridor direction from the ordered road polyline, not the enemy's facing direction. `ProjectRoadCenter` exposes the canonical forward vector through a backward-compatible overload. Both installers use it, and a reversed-facing test prevents an editor rotation from silently disabling the passage. Reinstallation corrected zero headings in the current scene, preserving the already verified probes.

## Calibration and recovery

- Keep unchanged earlier chapters as explicit carried evidence. Play each candidate from the actual earned entry wallet and levels, with recorded seeds and real purchases. Failed trials are diagnostic data, not hidden or relabeled successes.
- Treat an upgrade-path/reward interpolation as a planning estimate only. The estimator verifies purchase order before extrapolating, but a fixed clear-power threshold ignores pickup variance. Actual native clears remain the acceptance evidence.
- Snapshot preferences for the current task. Reusing a prior task's fixed backup can overwrite newer user progress. Require both base and extra-key snapshots before resetting, and reject a resume with a different recorded save-root.
- Keep game delta and physics step explicit when accelerating capture. A rendering cap or smaller GameView is not a device-performance result. Record dimensions and restore the original view, maximized state and frame cap.
- Maximize the GameView in Edit Mode before entering Play Mode. A change made during Play can be undone automatically on exit, leaving later attempts drawing the Scene view again.

Verification and the final attempt counts belong in the [task record](../../../map-concepts/later-chapters-30-2026-09-23/README.md). The initial snapshot, failed trials and physical proof remain separate from final acceptance.

## Related

- [Separate placement and driver errors from balance](separate-road-placement-and-driver-errors-from-campaign-balance-2026-09-23.md)
- [Verify runtime presentation and earned progression](verify-runtime-presentation-and-progression-beyond-numeric-checks-2026-09-10.md)
- [Keep player damage visible through hitches](../ui-bugs/keep-player-damage-visible-through-hitches-and-recovery-2026-09-23.md)
