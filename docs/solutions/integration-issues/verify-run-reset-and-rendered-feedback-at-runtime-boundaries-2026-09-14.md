---
title: Verify run reset and rendered feedback at runtime boundaries
date: 2026-09-14
category: integration-issues
module: Unity player lifecycle and combat presentation
problem_type: integration_issue
component: service_object
severity: high
symptoms:
  - "A scene retry retained a static missile-duration wall bonus."
  - "A generated swipe graphic existed in the hierarchy but had no CanvasRenderer."
  - "A direct-child UI lookup missed labels below the panel Fill node."
  - "Immediate paused screenshots showed the frame before damage; an immediate result panel hid the death tumble."
root_cause: scope_issue
resolution_type: code_fix
tags: [unity, retry, static-state, ui, animation, runtime-verification]
---

# Verify run reset and rendered feedback at runtime boundaries

## Problem

Scene reload, run start, prefab activation, animation evaluation and rendered frames are distinct boundaries. Treating one as evidence for another left bonus state alive and hid otherwise-existing presentation.

## Symptoms

`BulletScript.runDurationPercentBonus` survived the fallback scene reload because only GameManager reset paths cleared it. An authored `SwipeStartHint` lacked a CanvasRenderer. The hint panel's labels were under `StartHint/Fill`, so a direct `Find("Hint")` left the explanatory line visible behind the new hand. Immediate captures after health mutation contained the previous backbuffer. Results covered the hole tumble immediately.

## What Didn't Work

- Component existence alone did not prove a visible graphic. The first screenshot exposed the missing CanvasRenderer; a later hierarchy dump exposed the intermediate Fill node.
- Copying a source attack and changing root curves did not supply convincing movement evidence while the inspection player continued past the enemies. `player.movement=false` only blocks lateral input; forward travel has its own speed. Inactive source prefabs also need explicit activation before their event controller can run.
- Changing a field through `??` is unsafe with Unity's Editor fake-null component references. The installer now checks `rb == null` before adding a Rigidbody.
- The first local music engine launch was denied by the shell sandbox; authorized escalation ran the already-installed engine. Output guards retained the unsuccessful attempt and final five-track run separately.
- Optional deletion of experimental animation clips was rejected by automatic approval review for incomplete reference coverage. The cleanup was abandoned; clips remain preserved, and final controllers reference the selected new clips.

## Solution

Clear static run-only duration in Player Awake and the normal ResetState boundary; restore weapon defaults plus purchased effects there. Keep the persistent upgrade/equipment keys separate from transient run amounts. Verify the actual defeat Continue → scene load, not just a method call.

Use one scene-player vignette, the existing popup pool, model-only spin/fall and a player-owned fallen-pole cooldown. Freeze the player's Rigidbody for lethal hazard presentation and restore its kinematic state on reset. Stop gameplay immediately, but show the result panel after the0.9-second death window.

Give custom UGUI graphics an explicit CanvasRenderer requirement. Search the observed nested panel hierarchy. Save/reload all target scenes and inspect rendered Play Mode images before accepting UI changes.

The final melee clips start from the neutral humanoid pose and use bounded anticipation/cut/recovery curves. The shared carry mask overrides arms only. The inspection harness explicitly holds forward speed at0, checks activation success and records move/attack/return states alongside captures.

## Why This Works

The reset belongs to the state owner rather than a particular scene-flow implementation. The visual feedback owns only presentation state and preserves the route/camera. Frame-gated evidence exercises what the player sees, while native regression tests exercise exact numeric/reset/cooldown contracts.

## Prevention

- Test scene construction and same-instance reset independently for static values; also run the actual retry button once.
- Preserve live scenes and user preferences before native tests; restore preferences after the final scene load.
- Assert successful activation and advancing runtime state before interpreting animation screenshots.
- Do not infer that a disabled input flag freezes forward travel. Record both position and runtime state.
- Delay capture until a rendered frame, and defer state-changing actions until that capture has completed.
- Preserve unrelated dirty work by reviewing against a fresh source snapshot, not the entire branch diff.
- Retain prior diagnostics honestly. Two old SR18 snapshot tests had unrelated3-versus6 enemy and717-versus740 prop expectations; they are not passing tests.

## Related

- [Execution and evidence](../../../map-concepts/combat-feedback-2026-09-14/README.md).
- [Authored reset versus pooled spawn](../design-patterns/separate-authored-enemy-resets-from-pooled-spawns-2026-09-13.md).
- [Bounded presentation pools](../performance-issues/prewarm-bounded-combat-presentation-pools-2026-09-13.md).
