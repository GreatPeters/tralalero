---
title: Spawn Canonical Runtime Bonus Altars from Forward Enemy Deaths
date: 2026-08-17
category: integration-issues
module: Unity Forward enemy bonus drops
problem_type: integration_issue
component: tooling
symptoms:
  - "All five Forward enemies spawned the legacy random_wall_normal prefab instead of the canonical Box_left bonus altar."
  - "Enemy-death drops used a legacy vertical offset instead of the defeated enemy's ground position."
  - "Enemy-death Box_left instances kept the prefab's smaller nonuniform root scale instead of the map tool's 3x3 presentation."
  - "A child WallScript could miss the root RuntimeBonusWall marker and resolve the legacy global overlay."
  - "Transient drops and persistent map-authored altars did not share one explicit composite-root lifetime contract."
root_cause: config_error
resolution_type: code_fix
severity: high
related_components:
  - "testing_framework"
  - "development_workflow"
tags:
  - "unity"
  - "forward-enemy"
  - "enemy-death"
  - "bonus-altar"
  - "prefab-guid"
  - "runtime-marker"
  - "hierarchy-lookup"
  - "regression-test"
---

# Spawn Canonical Runtime Bonus Altars from Forward Enemy Deaths

## Problem

The Noryangjin map tool and authored scenes had migrated to one data-driven
`Box_left` fortune altar, but the five Forward enemy prefabs still serialized
`random_wall_normal` in `EnemyScript_space.bonusWall`. `EnemyDeath()` therefore
kept instantiating the retired wall through a separate runtime entry point.

The replacement is a composite prefab: its lifetime marker belongs on the root,
while `WallScript` lives on a child. That hierarchy must be respected by stage
cleanup and by runtime-only overlay suppression.

## Symptoms

- Killing any Forward enemy produced the legacy random wall.
- The drop appeared above the death point because the old path added `0.95` to
  world Y.
- After switching the prefab reference, the drop still appeared smaller and
  faced a different direction because runtime spawning skipped the map-tool
  scale multiplier and yaw offset.
- Putting `RuntimeBonusWall` only on the root made it invisible to a child
  `WallScript` that used a same-object lookup.
- Map-authored and enemy-drop altars risked sharing visuals without a reliable
  persistent-versus-transient lifetime distinction.

## What Didn't Work

Changing only the map-tool palette did not affect enemy deaths. The runtime
spawn source was the serialized field on each enemy prefab, not the palette:

```csharp
[SerializeField] private GameObject bonusWall;
```

Updating only one enemy would also be incomplete because all five prefabs own
their own serialized reference. Likewise, adding the runtime marker to the
composite root was necessary for cleanup but insufficient while child logic
checked only itself:

```csharp
GetComponent<RuntimeBonusWall>()
```

## Solution

All five canonical Forward enemy prefabs now reference
`Assets/ShooterSurvival/Prefabs/Walls/New/Box_left.prefab`. Death spawning uses
the enemy root position, applies the map-tool result of scale `(3,3,3)` and Y
rotation `180°`, preserves the prefab's default Normal grade, and ensures
exactly one removable marker on the spawned root:

```csharp
private GameObject SpawnBonusAltar()
{
    GameObject altar = Instantiate(
        bonusWall,
        transform.position,
        DroppedBonusAltarRotation);
    altar.transform.localScale = DroppedBonusAltarScale;
    if (altar.GetComponent<RuntimeBonusWall>() == null)
        altar.AddComponent<RuntimeBonusWall>();

    return altar;
}
```

`GameManager.ClearRuntimeBonusWalls()` can therefore destroy the complete
enemy-drop composite. Map-tool placement still calls
`KeepAsMapAuthoredWall()`, so authored altars remain across stage preparation.

Child wall logic resolves the root marker through the hierarchy:

```csharp
if (GetComponentInParent<RuntimeBonusWall>(true) != null)
    return null;
```

That prevents an enemy-drop altar from re-entering the legacy global
post-processing overlay path merely because its `WallScript` is nested.

## Why This Works

The root GameObject is now the single lifetime boundary for the composite
altar. The same `Box_left` supplies visuals, workbook-driven bonus selection,
and interaction for both authored and dropped altars, while `RuntimeBonusWall`
expresses only lifetime policy:

- default marker state means transient enemy drop;
- `KeepAsMapAuthoredWall()` means persistent map placement.

Hierarchy-aware lookup lets child behavior observe that root-owned policy
without duplicating the marker on every child.

## Prevention

- Enumerate `ForwardEnemyArchetypeCatalog.Definitions` and assert every enemy's
  serialized `bonusWall` equals the canonical `Box_left` asset; checking only
  for a non-null reference will not catch a legacy prefab.
- Test the spawned object, not just serialized references: world position,
  scale, yaw, `AuthoredBonusWall`, `BonusWallLifetimeRoot`, and exactly one
  removable `RuntimeBonusWall` are part of the contract.
- Put lifecycle markers on a composite root and make child logic use
  `GetComponentInParent(..., true)` when it consumes root-owned policy.
- Verify the transient/persistent split independently: enemy drops must be
  removable and overlay-free, while map-authored altars must remain.
- After externally editing Unity prefab YAML, refresh and rerun the all-prefab
  contract because the editor can reserialize stale loaded state.

## Related Issues

- [Reapply external prefab YAML after Unity script recompilation](../workflow-issues/reapply-prefab-yaml-after-unity-script-recompile-2026-08-03.md)
- [Bake generated prefab UI previews and isolate EditMode tests](../workflow-issues/bake-generated-prefab-ui-previews-and-isolate-editmode-tests-2026-08-13.md)
- [Prevent nearby bonus altar duplicates on candidate exhaustion](../logic-errors/prevent-nearby-bonus-altar-duplicates-on-candidate-exhaustion-2026-08-17.md)
- [Isolate dedicated MeshyAI assets from generic repair](../workflow-issues/isolate-dedicated-meshyai-assets-from-generic-repair-2026-08-09.md)
