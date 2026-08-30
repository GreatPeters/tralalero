---
title: Noryangjin Enemy Event Controller Migration
status: completed
date: 2026-08-30
module: Forward enemy events and animation
---

# Noryangjin Enemy Event Controller Migration

## Goal

Replace the mixed movement/fire authoring contract with one simple per-enemy
event controller. Activation spots store links only; every enemy owns its mode,
single movement target, locomotion choice, and attack behavior.

## Completed scope

- Preserved both MonoScript GUIDs while renaming the visible components to
  `Enemy Event Controller` and `Enemy Event Activation Spot`.
- Replaced the old movement modes with `AttackLoop`, `Shoot`,
  `MoveToTargetThenAttack`, and `PatrolBetweenStartAndTarget`.
- Removed the activation spot's `oneShot` authoring field. A spot disables after
  any linked event accepts activation and resets for the next run.
- Added one external target Transform for both movement modes, shared Inspector
  and map-tool authoring, a Scene position handle, and route-orthogonal attack
  facing that preserves each prefab's visual rotation offset.
- Added exact `idle`, `attack_loop`, `walk`, `run`, `die`, and `attack_once`
  states to the shared Humanoid controller. `attack_once` returns to `idle`,
  and `idle` loops continuously. The event list also exposes `AttackOnce`, and
  movement animation exposes `None` without changing existing enum values.
- Applied the controller, override assets, and serialized event defaults to all
  five canonical Forward enemy prefabs.
- Imported the CC0 Quaternius Universal Animation Library Unity FBX with its
  license and provenance record. Only `walk` and `run` now use the external
  Humanoid clips; all earlier idle, attack, death, and fallback locomotion
  assets remain intact.
- Added safe handling for zero movement speed, a target destroyed during
  movement, custom gameplay pause during projectile delay, legacy serialized
  mode value `3`, scene re-enable reset, and pooled post-enable placement.
- Updated map-tool UI, architecture, authoring documentation, reusable solution
  notes, and focused EditMode/PlayMode tests.

## Verification

- `dotnet build Assembly-CSharp.csproj -nologo`
- `dotnet build Assembly-CSharp-Editor.csproj -nologo`
- `powershell -ExecutionPolicy Bypass -File tools/validate-agent-harness.ps1`
- Official Unity Pipeline Preview Scene verification of all five prefab
  components, the connection-only spot, five event modes, six Animator states,
  override completeness, target movement, and invalid-target rejection.
- Direct Preview Scene verification that all five Humanoid rigs animate with
  the Quaternius walk clip, `walk`/`run` are the only external overrides, all
  prior motion paths remain exact, and a second setup run changes no asset
  hashes.
- `unity command recompile --project-path .` completed without compiler errors.

The in-process Unity Test Runner was intentionally not started because the open
`Noryangjin_MapTool_Mode` scene contains unsaved authoring work. Starting it can
open Unity's save-scene task and persist unrelated scene changes. The changed
tests compile; run the focused fixtures after the user saves or discards the
open scene intentionally.

## Operational sharp edges

- `Shoot` accepts activation only on an enemy with a configured held projectile,
  player reference, Animator, and `SimpleProjectile`.
- A movement target must be outside the moving enemy hierarchy. Losing it at
  runtime cancels that event for the current run.
- The legacy utility and trigger prefab filenames remain for GUID/history
  stability; their actual runtime component names are the new Event names.
- The source-level C# type rename is intentional. Unity serialization remains
  compatible through the preserved script GUIDs and `MovedFrom` metadata, but
  any out-of-repository source code must update its type names.
