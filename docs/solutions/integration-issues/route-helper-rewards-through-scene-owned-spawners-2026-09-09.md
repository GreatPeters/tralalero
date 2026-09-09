---
title: Route helper rewards through scene-owned spawners and ordered road checkpoints
date: 2026-09-09
category: integration-issues
module: Noryangjin bonus helpers
problem_type: integration_issue
component: service_object
symptoms:
  - "TungTung and BoomBar bonus walls referenced a missing GameManager."
  - "TungTung chased enemies directly instead of following corners and elevated roads."
  - "The first road-following implementation stalled at a timber plank gap."
root_cause: incomplete_setup
resolution_type: code_fix
severity: high
tags: [unity, noryangjin, bonus, helper, road-following, lifecycle]
---

# Route helper rewards through scene-owned spawners and ordered road checkpoints

## Problem

Noryangjin already had a configured upgrade-helper spawner but the separate BonusWall reward path dereferenced `GameManager.S`, absent from this authored scene. Helper movement also assumed a direct line to the nearest enemy, which cannot represent intersecting roads or overpasses.

## Symptoms

- Bonus consumption could throw a NullReferenceException instead of completing the summon.
- TungTung registered a null weapon entry because it has no ranged weapon.
- Direct target pursuit could leave the road or cut a corner.

## What Didn't Work

Correcting the spawn reference alone did not fix movement. Following the player's current forward vector also turns an ahead/behind helper at the wrong position. A first per-helper route cursor passed synthetic corner tests but stopped at `(-10.68, .16, -55.64)`: the ray landed in a narrow gap between timber planks. Treating every missed ray as the end of the road was too strict.

## Solution

`WallScript` delegates to the existing `NoryangjinUpgradeExtraHelpSpawner` and retains the GameManager prefab fallback for legacy scenes. Spawn success owns the count increment. Add only actual `WeaponScript` instances to the player's list; remove them on helper destruction.

TungTung snapshots unconsumed authored corners into its own ordered cursor and advances each segment in bounded steps. It never consumes the player's turn spots and never reroutes toward an enemy across water. `NoryangjinRoadHeightFollower` samples the correct nearby deck; a miss may use support only .2/.4 units farther along the same segment and within .3 height, without changing the requested XZ position. Thus gaps are crossed without treating a missing stretch of road or another crossing level as valid support. BoomBar follows player height +2 instead of hardcoded world height 2.

## Why This Works

The scene-specific dependency already existed; the reward path needed to reuse it. Route state belongs to each traveler, while geometry remains shared. A narrow, directional support fallback distinguishes plank seams from genuine missing floor.

## Prevention

- Exercise actual bonus collision/reward callbacks, not just the spawner in isolation.
- Verify helper survival after Start, actual BoomBar projectile emission, and absence of null weapon entries.
- Test helper travel through every authored corner and both bridge levels. SR18's runtime probe covered approximately 2,587 units and all 15 corners, reaching the real exit after rising above 12 units.
- Keep pause, owner death and destruction boundaries explicit; never validate helper correctness from a transform count alone.
- Keep reward choice ownership separate: paired fixed walls grant one reward, enemy death drops remain unpaired.

## Related Issues

- [Scene-owned reference gameplay composition](transactional-reference-scene-gameplay-composition-2026-07-23.md)
- [Per-run turn spot consumption](restore-consumed-turn-spots-on-in-place-run-restart-2026-07-25.md)
- [Choice and post-turn response space](../design-patterns/validate-player-choice-and-post-turn-response-space-2026-09-09.md)
- [Applied changes and verification](../../../map-concepts/sr18-combat-polish-2026-09-09/README.md)
