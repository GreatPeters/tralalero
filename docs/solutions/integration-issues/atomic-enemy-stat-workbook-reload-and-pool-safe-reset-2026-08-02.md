---
title: Validate Enemy Stat Workbook Reloads and Preserve Pool Ownership
date: 2026-08-02
category: integration-issues
module: enemy-stat-data-pipeline
problem_type: integration_issue
component: tooling
symptoms:
  - A malformed Data.xlsx save could leave caches and live scene consumers on different data revisions
  - Stage reset could activate inactive enemies that were still queued below EnemyPooler
root_cause: missing_validation
resolution_type: code_fix
severity: high
related_components: [GameDataWorkbookAssetPostprocessor, MonsterGrowthTables, GameManager, EnemyPooler, Unity Test Runner]
tags: [unity, data-xlsx, live-reload, enemy-stats, enemy-pooling, atomic-validation]
---

# Validate Enemy Stat Workbook Reloads and Preserve Pool Ownership

## Problem

Adding workbook-driven enemy growth crossed two stateful boundaries: live editor
configuration reload and inactive enemy lifecycle. The initial implementation
could mutate some caches before discovering a malformed sheet, and an
inactive-inclusive stage scan could mistake pooled inventory for defeated
encounter enemies.

## Symptoms

- An invalid or partially saved `Data.xlsx` could be rejected after one or more
  caches had already reloaded.
- Retained players and `GameManager` instances could observe a mixture of old
  and new configuration values.
- Stage reset could activate enemies that were inactive because they were
  waiting inside an `EnemyPooler`.
- Ignoring every inactive enemy was also incorrect because defeated encounter
  enemies outside a pool must be restored for the next stage.

## What Didn't Work

Reloading each table before validating the whole source made validation too
late. Permissive numeric conversion was also unsafe for combat data because
invalid text could become a plausible-looking zero.

Likewise, re-enabling every result from `FindObjectsInactive.Include` ignored
why each object was inactive. Local active state alone cannot distinguish pool
inventory from a defeated scene encounter.

## Solution

Use the complete workbook schema as a commit gate before clearing any table
cache or refreshing live consumers:

```csharp
GameDataWorkbookEditor.ValidateSourceWorkbookOrThrow();
EnvironmentVariableTables.Reload();
MonsterTables.Reload();
MonsterGrowthTables.Reload();
UpgradeTables.Reload();
BonusTables.Reload();
SkinTables.Reload();
PatternTables.Reload();

ReloadLoadedPlayerDefaults();
ReloadLoadedMonsterStats();
```

The schema strictly parses the optional `몬스터 성장` sheet when it exists.
It requires exactly one `Normal`, `Elite`, and `Boss` row, unique positive row
IDs, finite non-negative damage, and finite positive health. Blank, malformed,
`NaN`, and infinite numeric values fail before cache invalidation begins.

Classify inactive enemies using both their active state and ownership ancestry:

```csharp
public static bool ShouldResetEnemyByReEnable(GameObject enemyObject)
{
    if (enemyObject == null)
        return false;

    bool isInactiveQueuedPoolObject =
        !enemyObject.activeSelf &&
        enemyObject.GetComponentInParent<EnemyPooler>(includeInactive: true) != null;
    return !isInactiveQueuedPoolObject;
}
```

The stage reset loop uses this predicate before toggling an enemy. Active
encounter enemies and inactive defeated enemies outside a pool are reset;
inactive pool descendants retain their queued state.

## Why This Works

Full-source validation prevents a known-invalid workbook revision from reaching
any live cache or scene consumer. Strict numeric parsing preserves the
difference between missing data and a deliberate combat value.

The enemy predicate preserves lifecycle ownership rather than inferring intent
from inactivity alone:

- Inactive below `EnemyPooler`: queued inventory; do not activate.
- Inactive outside a pool: disabled encounter enemy; restore it.
- Active enemy: reset normally.

## Prevention

- Validate the complete configuration source before invalidating any cache or
  updating retained runtime objects.
- Keep strict parsing for gameplay-critical values; never coerce malformed
  numbers to zero.
- Whenever `FindObjectsInactive.Include` is used, classify inactive objects by
  owner and lifecycle before changing their active state.
- Preserve regression coverage for malformed growth rows, endpoint generation
  from the real workbook, pooled inactive enemies, and defeated encounter
  enemies.
- If concurrent workbook writes become possible, strengthen this further by
  parsing every runtime table from the validated byte snapshot and swapping the
  entire cache set as one revision. The current validation and reload phases
  still open the source separately.

## Verification

- `MonsterGrowthAndMapToolEnemyTests`: 14/14 passed.
- `GameDataWorkbookTests`: 15/15 passed.
- Runtime and editor C# builds completed with zero errors.
- `tools/validate-agent-harness.ps1` passed.

## Related Issues

- [Refresh Excel character defaults without domain reload](refresh-excel-character-defaults-without-domain-reload-2026-08-01.md)
- [Restore consumed turn spots when restarting Unity runs in place](restore-consumed-turn-spots-on-in-place-run-restart-2026-07-25.md)
- [Game data workbook architecture and workflow](../../game-data-workbook.md)
- [Use independent oracles for configuration column tests](../best-practices/use-independent-oracles-for-configuration-column-tests-2026-08-01.md)
