---
title: Refresh Excel Character Defaults Without Domain Reload
date: 2026-08-01
category: integration-issues
module: Unity game-data reload lifecycle
problem_type: integration_issue
component: tooling
symptoms:
  - Data.xlsx playerSpeed edits were not applied immediately after stopping and re-entering Play Mode
  - The active player retained speed 6 while an explicit environment-table reload resolved the workbook value 20
  - The protected runtime Data.bytes archive remained stale until it was regenerated
root_cause: missing_workflow_step
resolution_type: code_fix
severity: medium
related_components: [EnvironmentVariableTables, PlayerScript, GameDataWorkbookAssetPostprocessor, Unity Enter Play Mode options]
tags: [unity, data-xlsx, cache-invalidation, domain-reload, asset-postprocessor, play-mode, character-defaults]
---

# Refresh Excel Character Defaults Without Domain Reload

## Problem

Saving `Data.xlsx` did not reliably update environment-driven player defaults.
The project disables both domain and scene reload on Play Mode entry, so the
static environment table and already-initialized player instances could retain
values from the previous session.

The protected `Data.bytes` archive used by built players is a separate generated
artifact and could also remain older than the source workbook.

## Symptoms

- An active player continued to report forward speed `6` after `playerSpeed`
  changed in the workbook.
- Calling `EnvironmentVariableTables.Reload()` explicitly changed the resolved
  workbook value to `20`.
- The runtime-data freshness test reported that `Data.bytes` was stale.
- A sentinel integration test still read `playerSpeed = -999` when reload work
  was deferred to a later editor callback.

## What Didn't Work

Reloading the workbook source alone was insufficient because it did not clear
`EnvironmentVariableTables._float3Map` or the per-player
`characterDefaultsInitialized` guard.

The first postprocessor implementation used `EditorApplication.delayCall`.
That made freshness depend on later editor-loop timing: the integration test
still observed the stale sentinel after two editor yields. Import completion
needed deterministic, synchronous cache invalidation.

Updating the editor cache also does not regenerate `Data.bytes`. That archive
must still be refreshed through its existing generation workflow before a
player build consumes the new values.

## Solution

Make the environment reload transactional by parsing into a temporary map and
replacing the live cache only after parsing succeeds:

```csharp
public static void Reload()
{
    using var stream = GameDataWorkbook.OpenRead(FileName);
    Dictionary<string, Float3> refreshedMap = ReadRows(stream);
    _float3Map = refreshedMap;
}
```

Expose a narrow player refresh operation that re-resolves the configured
defaults without resetting unrelated runtime state:

```csharp
public void ReloadCharacterDefaults()
{
    characterDefaultsInitialized = false;
    EnsureCharacterDefaultsInitialized();
}
```

Add an editor coordinator that:

- Matches only the canonical `Data.xlsx` asset path, case-insensitively.
- Reloads `EnvironmentVariableTables` synchronously when that asset imports.
- Refreshes loaded, nonpersistent scene players when the editor is playing.
- Repeats the reload on `EnteredPlayMode` to cover disabled domain and scene
  reload.

Finally, regenerate the protected runtime archive through
`Tools > Data > 런타임 보호 데이터 갱신` when the workbook changes need to be
included in a build.

## Why This Works

The refresh follows the dependency order: parse the latest workbook, atomically
replace the shared table, then ask retained player instances to resolve their
defaults again. A failed or incomplete workbook parse leaves the last valid map
intact.

The import hook handles edits during a normal editor session, while the
`EnteredPlayMode` hook covers retained static and scene state. Path, persistence,
and scene checks prevent unrelated workbooks, prefab assets, and unloaded
objects from being mutated.

## Prevention

- Seed the environment cache with an impossible value and verify that the asset
  postprocessor removes it before returning.
- Test that only the canonical workbook path triggers a reload.
- Test player default re-resolution independently from the editor callback.
- Keep a separate source-versus-archive freshness test for `Data.bytes`.
- Treat editor hot reload and protected runtime archive generation as distinct
  lifecycle boundaries.

## Related Issues

- [Validate enemy stat workbook reloads and preserve pool ownership](atomic-enemy-stat-workbook-reload-and-pool-safe-reset-2026-08-02.md)
- [Game data workbook architecture and workflow](../../game-data-workbook.md)
- [Use independent oracles for configuration column tests](../best-practices/use-independent-oracles-for-configuration-column-tests-2026-08-01.md)
