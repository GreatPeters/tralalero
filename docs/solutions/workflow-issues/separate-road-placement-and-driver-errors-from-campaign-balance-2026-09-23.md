---
title: Separate road placement and driver errors from campaign balance
date: 2026-09-23
module: Road encounters and earned campaign testing
problem_type: workflow_issue
component: testing_framework
severity: high
tags: [unity, balance, road-placement, idempotence, playtest, earned-progression]
---

# Separate road placement and driver errors from campaign balance

## Problem

Repeated late-chapter deaths looked like insufficient health or expensive upgrades. Three different failures were involved: a real out-of-lane placement, converging encounter partners, and a test driver steering toward a hazard on a different road. Raising player stats would conceal these defects and distort the campaign economy.

## Evidence and failed approaches

- A RestStop pair dealt 2915 and 1729 contact damage during the same crossing. Sideways patrols converged instead of retaining separate lanes.
- E10 TireBruiser stood at x234.7 on a road centered at x240: lane −5.3, outside the player's ±4.4 range. The generic alignment repair treated the left actor as the center when no HighwayRoute or corner marker existed. Repeating the repair moved it another 1.1 m.
- A traffic trace requested lane336.4 because a toll on another parallel road passed an ahead-only filter. The driver then crossed the entire road too late and died. This is not evidence that the player needs more HP; major traffic hazards intentionally remain lethal.
- Friendly shots copied the owner's rotation rather than sampling the road at their own progress. After correcting that, the driver still aimed in the owner's tangent frame instead of the target's road lane. That separate input defect needed correction too.

## Solution

Use the authored road polyline as the placement authority. `RestStopChapterBuilder.ProjectRoadCenter` projects onto a segment with matching heading, preserving station and height. Both the focused pair installer and the older general alignment tool use it. The 25 pairs sit at ±1.1 m, forward/back patrols stay in their lanes, and the pair shares one contact claim. RestStop does not have HighwayRoute and does not gain highway funnel geometry from this repair.

Make the authoring operation idempotent and verify the saved scene. The focused native tests check both actor lanes, patrol direction, pair membership, absence of added barriers, and repeated center projection. Apply the installer twice before accepting the result.

The driver now rejects laterally remote hazards, clamps its desired lane before predicting collision, and considers moving traffic 32 m ahead. Continuous-road aiming uses target road coordinates. It still submits ordinary movement, with no teleports, disabled hazards or health overrides.

Run each revised chapter from its actual earned entry checkpoint. Preserve failed cohorts and record workbook/driver hashes, seeds, purchases, wallet arithmetic, damage causes, and true chapter completion. Reused unchanged earlier chapter records are not additional newly played attempts. A `GameOver` flag or a stopped clock alone does not prove a clear.

## Interpretation limits

An endpoint gain divided by purchase visits is a pacing indicator, not a causal claim that each upgrade adds a fixed number of seconds. A visit can buy multiple upgrades; random helpers and steering affect survival. Normal-speed native footage and physical-contact fixtures complement the numeric cohort. Fixed game cadence is not a device FPS benchmark or a population win-rate estimate.

The Pipeline async test start returns an acknowledgment with zero totals. Poll `test_status`, decode its JSON string where necessary, then require a completed result with nonzero cases and no failures. Do not interpret the acknowledgment as an empty test suite.

Wait for the real result presentation as well as the completion flag. A1.3-second capture showed only the stopped game because the win panel waits2real seconds and transition video loading also takes time. The harness now waits2.6seconds and the driver waits3.5before cleanup. Replays of the measured clear state/seed visibly confirmed the reward/next-chapter UI. Do not silently relabel the earlier pre-panel PNGs as victory-panel evidence.

## Related

- [Campaign record and final evidence](../../../map-concepts/campaign-balance-2026-09-23/README.md)
- [Verify runtime presentation and earned progression](verify-runtime-presentation-and-progression-beyond-numeric-checks-2026-09-10.md)
- [Preserve native scene rig bindings](../integration-issues/rebuild-enemy-prefabs-without-stale-scene-rigs-2026-09-23.md)
- [Damage visibility through hitches and recovery](../ui-bugs/keep-player-damage-visible-through-hitches-and-recovery-2026-09-23.md)
