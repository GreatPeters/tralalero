---
title: Bootstrap enemy growth in scenes without GameManager
date: 2026-08-03
category: integration-issues
module: enemy-stat-data-pipeline
problem_type: integration_issue
component: tooling
symptoms:
  - "Noryangjin enemies kept prefab stats even though chapter growth parsed correctly."
  - "Live Data.xlsx reload reported zero active GameManager consumers in the Noryangjin scene."
root_cause: incomplete_setup
resolution_type: code_fix
severity: high
related_components: [ChapterEnemyStatController, ChapterEnemyProgression, GameManager, GameDataWorkbookAutoReload, NoryangjinForwardGameplayInstaller]
tags: [unity, data-xlsx, enemy-stats, noryangjin, scene-bootstrap, live-reload, game-manager]
---

# Bootstrap enemy growth in scenes without GameManager

## Problem

Chapter growth parsing and interpolation were implemented inside `GameManager`,
but the authored Noryangjin gameplay scene deliberately does not contain a
`GameManager`. The workbook and unit calculations could therefore pass while
the real map-tool enemies never received the new stats.

## Symptoms

- `MonsterGrowthTables` loaded all chapter/tier rows successfully.
- Synthetic `GameManager` tests produced correct endpoint values.
- Inspecting the actual Noryangjin scene showed `TimeManager`, upgrade services,
  and analytics context, but no `GameManager` component.
- Workbook live reload searched only for `GameManager`, so its log reported
  `activeGameManagers=0` for Noryangjin.

## What Didn't Work

Keeping route ordering and stat injection private to `GameManager` assumed that
every gameplay scene shared the legacy Forward scene composition. Adding more
tests around `GameManager` did not expose the missing consumer because those
tests constructed a manager that the target scene does not have.

Relying only on an installer-added serialized component was also incomplete.
The existing scene should work immediately, and this project disables scene
reload on Play Mode entry, so a bootstrap that listens only to
`SceneManager.sceneLoaded` can miss the already-loaded active scene.

## Solution

Move the reusable work into `ChapterEnemyProgression`:

- collect non-pooled enemies and turn spots from a specific scene;
- calculate turn-aware route distance;
- sort enemies deterministically;
- interpolate all placed enemies with `index / (count - 1)`; and
- apply the resolved tier-specific damage and health.

Add `ChapterEnemyStatController` as the Noryangjin consumer. It captures the
player's initial route frame before movement, applies stats after ordinary enemy
`Start` methods, and bootstraps in both cases:

```csharp
[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
private static void RegisterSceneBootstrap()
{
    SceneManager.sceneLoaded -= OnSceneLoaded;
    SceneManager.sceneLoaded += OnSceneLoaded;
}

[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
private static void BootstrapActiveScene()
{
    EnsureForScene(SceneManager.GetActiveScene());
}
```

The installer persists the controller for newly installed Noryangjin scenes,
while the runtime bootstrap covers existing authored scenes. The workbook
postprocessor also finds loaded controllers and calls `ApplyStats()` after
cache invalidation, giving them the same live-edit behavior as `GameManager`.

## Why This Works

The stat algorithm no longer owns scene lifecycle. Both the legacy manager and
the Noryangjin controller call the same route-ordering implementation, so the
target scene receives the exact logic verified by unit tests.

Registering `sceneLoaded` covers later scene transitions. Running a second
bootstrap at `AfterSceneLoad` covers the active scene when Play Mode starts with
scene reload disabled. The controller caches the initial player transform, so
an Excel edit made after the player has moved cannot reorder the chapter from
the player's new position.

## Prevention

- Before placing runtime behavior in an existing manager, inspect the actual
  target scene for that component rather than assuming reference-scene parity.
- Keep reusable calculations independent from the lifecycle component that
  invokes them.
- Cover both a pure calculation and the real scene consumer. Here,
  `MonsterGrowthAndMapToolEnemyTests` verifies controller-driven stat injection
  without a `GameManager`, and `NoryangjinGameplayIntegrationTests` verifies
  that loading the authored scene produces a chapter controller.
- For projects with domain or scene reload disabled, test both the current
  active scene and later `sceneLoaded` transitions.
- When adding live configuration reload, enumerate every runtime consumer type;
  a successful cache refresh is not proof that all scenes re-applied the data.

## Related Issues

- [Validate enemy stat workbook reloads and preserve pool ownership](atomic-enemy-stat-workbook-reload-and-pool-safe-reset-2026-08-02.md)
- [Make reference-scene gameplay composition transactional and idempotent](transactional-reference-scene-gameplay-composition-2026-07-23.md)
- [Restore consumed turn spots on in-place run restart](restore-consumed-turn-spots-on-in-place-run-restart-2026-07-25.md)
