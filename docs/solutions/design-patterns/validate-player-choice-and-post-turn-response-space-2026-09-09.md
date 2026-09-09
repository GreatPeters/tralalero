---
title: Validate player choice and post-turn response space
date: 2026-09-09
last_updated: 2026-09-10
category: design-patterns
module: Noryangjin SR18 encounter authoring
problem_type: design_pattern
component: testing_framework
severity: medium
applies_when:
  - "A narrow runner path needs two selectable alternatives without duplicate rewards."
  - "Encounter-count tests pass but enemies appear too soon after a turn."
tags: [unity, sr18, bonus-choice, corner-clearance, collider, level-design, cooldown, prefab-connection]
---

# Validate player choice and post-turn response space

## Context

The single-file SR18 layout passed asset-count and road-support checks, yet a single centered BonusWall offered no left/right choice and some enemies were only 8 units beyond a corner. Those checks verified the placement data, not the interaction the player needed.

## Guidance

Define observable interaction invariants before counting instances. A two-option station needs independently reachable left/right triggers, a neutral center lane, distinct effects, and at most one reward. Measure the actual player and altar colliders: here the player radius was .69 and a full-size altar trigger was about 2.83 wide. Two full-size altars crowded the roughly 6.3-wide deck. Using existing models at scale 2.25 and offsets ±1.9 kept the alternatives on the road with a traversable central gap.

Claim the pair before applying the selected effect. The claim closes the sibling and rejects later callbacks, even when the ordinary one-second wall cooldown has expired. Reset the group explicitly for a new run; do not reset it just because one member re-enables. Keep one stable workbook row per station so enabling or grading it cannot leave an unintended third or orphaned choice. Unpaired enemy drops remain unaffected.

Treat post-turn body position and activation position as different constraints. SR18 uses a minimum 28-unit body/target offset and 20-unit activation offset along the outgoing route. Validate both patrol endpoints, not only their center. Preserve corner pivots; move encounters instead. Reflow neighboring stations when an enemy moves, keeping their order and a 24-unit minimum station gap. Current values are authored distances, not universal reaction-time guarantees; collider leading edges and movement speed affect actual contact timing.

Keep those bounds active when Excel settings are reapplied. `EnemyCornerClearance` rejects movement ranges inside the body buffer and limits activation position; a correct saved scene alone is insufficient if runtime data moves the trigger back toward the corner.

## Why This Matters

Two objects do not prove two choices, and correct route counts do not prove readable combat. Geometry, collision eligibility, reward lifetime and data reload must agree. Test isolation also matters: camera-facing billboard rotations can legitimately dirty newly placed UI after a save. Compare and back up only those known changes before moving to an empty test scene rather than discarding arbitrary edits.

## When to Apply

Use for constrained-lane choices and encounter reflow around direction changes. Re-measure when models, player shape or speed change. Verify camera readability separately; the existing overhead-bridge occlusion was not solved by this encounter change.

## Examples

SR18 retained 25 reward opportunities while changing 25 singles into 25 pairs/50 altars. Nine enemies, five bonus stations and seven gimmicks moved to establish post-turn space without altering the 230 roads or 666 original scenery placements. Requested bridge-choice center positions were preserved.

55 focused tests passed. In actual Play Mode, 50 side-lane contact checks succeeded and the neutral lane overlapped neither choice. Two deterministic attack-bonus collision callbacks verified exactly one reward from either selection direction; original values were restored afterward. All 25 runtime post-turn bounds survived workbook reapplication. This is not a full-stage pacing or difficulty certification.

## Continuous-play audit correction

The later real-loop audit exposed a second validation boundary: filtering hazards to newly named `SR18_L_G` stations counted42 active parts, but the full scene contained46, including4 retained damaging lamps outside the placement workbook. Keeping scenery unchanged does not mean it is inert. Enumerate every active gameplay component in the scene and classify preserved interactive objects separately from visual props.

A projectile-travel counter also was not sufficient proof of a successful shot: it incremented before `GetComponentInChildren<BulletScript>().SetDirection`. After real collisions, two pooled Water roots had lost their GFX. A double-return reproducer demonstrated that disabling a projectile cleared its root pointer, causing the second return to target and remove the child. The next rental then failed. Earlier isolated tests had disabled collision exposure, hiding this lifecycle path.

Run at least one continuous baseline session with ordinary health and all collisions before any endurance override. If extra health is needed to reach the end, label it explicitly and do not claim balance certification. Restore test-earned currency, preserve original content, capture the real GameView including HUD, and test the exit/clear transition rather than stopping once the route coordinates are traversed. The discovered gameplay defects remain open; the improvement here is the verified testing method and corrected acceptance boundary. See the [live audit](../../../map-concepts/sr18-live-playtest-2026-09-09/README.md).

## Pattern-playtest follow-up, 2026-09-10

Measure the decisions and dynamic hazards, not only authored object counts. SR18 has24 gimmick stations but23 follow the same Bucket/Light/Hole cycle. Three buckets in one longitudinal lane take one avoidance decision. During the first bucket lock, all observed non-waiting enemies were already dead, so removing shooting produced no immediate combat problem. Treat later-section pattern counts as a static audit when normal-health runs stop in the opening; do not substitute an endurance traversal for human difficulty evidence.

Recheck safe lanes after each obstacle state transition. The new G05 lamp retained an enabled damage50 trigger after toppling: its X collision width grew from about1.65 to6.64 and shifted toward the formerly clear lane. Two normal runs took damage there, one lethally from44.92 health. A separate function-level probe confirmed the transformed bounds; that probe was not continuous gameplay. A driver that avoids a hazard's original root position does not prove the current lane is safe after animation.

Inspect actual HUD rendering alongside numeric data. `PlayerStatusHud.SetHealth` changes `Image.fillAmount`, but the current SR18 scene's `HealthFill` is Sliced. The real frame showed23/100 with a full red bar; checking only the assigned ratio would miss it. This remains an open finding, not a fix delivered by the playtest.

Preserve run boundaries: after death, `CanvasScript.GameOverSequence` resets the scene after3 real seconds. A late screenshot can show the start screen and the timeline can return toHP100 without a new run. Use the first death sample, match images to their times, and document input policy plus random-reward differences before comparing run lengths.

Evidence, three normal-health runs and unapplied design proposals: [pattern playtest](../../../map-concepts/sr18-pattern-playtest-2026-09-10/README.md). The guidance is an extension of the existing interaction-validation lesson; no duplicate solution document or gameplay change was created.

## Related records

### Contact formations and global pickup cooldown

The next user review showed that a lamp automatically disarmed by one bullet loses its hazard role in an auto-fire game. Keep the earlier safe-topple behavior available, but distinguish it from authored durable contact hazards. SR18's eight authored lamps now set `canBeShotDown=false`; the four explicitly decorative lamps retain their prior contract. Reproduce with actual player-collider contact rather than inferring damage from enabled-component counts.

A wide enemy's center point can be supported while its body extends over roadside props. Centered FatMan entry paths fixed the three wide actors without changing their models or combat stats. Verify the real rendered body/feet and run-start data reapplication, not just the saved target point.

For an unavoidable two-enemy formation, check coverage using the actual scene Collider and its authored scale/offset. Two objects at symmetric positions do not prove closure of the center gap or lane edges. The two SR18 formations pass81 lane samples each and leave an open side when the corresponding enemy is removed. Independent workbook rows and activation spots keep each member's existing death/drop/stat ownership intact.

For a shared pickup cooldown, reserve the successful timestamp before applying effects and only after eligibility/pair ownership checks. An initial timestamp of0 plus a special `timestamp==0` bypass allows same-frame duplicates at time0. Negative infinity makes the first/restarted pickup eligible without that exception. Test0/1.5/1.99/2 seconds, a different wall, a blocked pair that remains unclaimed, and restart.

Cloning a scene prefab instance with `Object.Instantiate` retained its values but lost its prefab connection. The corrected authoring flow reconnects with `ConvertToPrefabInstance` while explicitly preserving unmatched components/GameObjects, matched property overrides and the assigned root name. Verify the resulting connection and activation references; do not assume copying a prefab instance copies its authoring relationship. Artifact Tool authored the two new data rows, while a narrow row merge preserved all other workbook ZIP parts and native styles.

Applied evidence: [SR18 contact/formation update](../../../map-concepts/sr18-contact-pairs-2026-09-10/README.md).

### Implemented opening refinement

The user authorized the follow-up implementation in [SR18 opening rhythm](../../../map-concepts/sr18-opening-rhythm-2026-09-10/README.md). Shot lamps now claim `_lampFallen`, capture original collider enable states and disarm before the fall animation. The player-contact branch also rejects already queued callbacks. Target the fall tween at its Transform so reset/disable can stop it, then restore only the original collider states. Preserve disabled scenery colliders. Read the hinge bounds before disabling physics, because disabled colliders may report empty bounds.

Scene-space clearance is insufficient for slalom timing. The first8-unit pattern allowed1 second between centers, while the actual keyboard-equivalent lateral speed was about3 units/second (`15 * 1 / 125 * 25`). Crossing between lane edges, including the player's leading/trailing collider, needed more time. The final12-unit spacing provides1.5 seconds. Verify with real `PlayerMove` and active colliders; do not move transforms by hand and call that a reachable input path.

The first16-unit reward/hole approach passed geometry checks but the altar labels covered the hazard in GameView. A25-unit approach made the hole visible and avoided a plank seam under the rewards. Probe both reward footprints before moving either side: a failed second-side probe must not leave a partially moved pair. Use top-deck support rather than accepting the first lower or side hit. Retain first-pass images/records so the reason for changing the candidate remains reviewable.

For the HUD, setting `Image.type=Filled` made the ratio functional, but the old Background sprite's nine-slice caps stretched. A small solid sprite preserves the bar's intended readable area. Verify non-full health in an actual frame as well as type/value assertions. The original findings above describe the pre-fix state and are retained as evidence.

2026-09-10 follow-up: guarded projectile returns, camera occluder handling, decorative legacy lamps and the exit/clear flow were implemented and retested. The earlier failure observations above remain as historical evidence; see the [fix record](../../../map-concepts/sr18-runtime-fixes-2026-09-10/README.md). The distinction between normal-health balance runs and endurance overrides still applies.

- [Turn-trigger entry and corner pivots](align-turn-trigger-entry-with-player-collider-and-corner-pivot-2026-09-05.md)
- [Nearby bonus uniqueness and lifecycle](../logic-errors/prevent-nearby-bonus-altar-duplicates-on-candidate-exhaustion-2026-08-17.md)
- [Applied layout, backup and actual camera image](../../../map-concepts/sr18-choices-and-corner-space-2026-09-08/README.md)
---
