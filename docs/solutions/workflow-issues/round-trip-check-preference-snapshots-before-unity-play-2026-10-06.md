---
title: Round trip check preference snapshots before Unity Play
date: 2026-10-06
category: workflow-issues
module: Unity Pipeline account UI verification
problem_type: workflow_issue
component: development_workflow
severity: high
applies_when:
  - "A run_script fixture snapshots preferences before entering Play Mode."
  - "The snapshot DTO is declared in the ephemeral in-memory script assembly."
tags: [unity, pipeline, playerprefs, snapshot, jsonutility, recovery, account]
---

# Round trip check preference snapshots before Unity Play

## Context

The account UI fixture created a serializable nested DTO inside a Pipeline `run_script` assembly and passed it to `JsonUtility.ToJson`. The tool returned a capture count of 89, but the file contained only `{}`. Reading it after Play produced a null `rows` field. The count proved that preferences were enumerated, not that their data was durably saved. Compiled application DTOs passed their separate Unity serialization tests; the failure was in the ephemeral fixture.

The first preview connected a fake account, showed its deletion confirmation and canceled. It never called real authentication or deletion. Only the known new `play_account_owner`, `play_account_base` and `play_account_auto_login` keys were removed when the owner exactly matched `__preview_account__`; the matching preview cache was removed. The before/after wallet remained 33,031 coins and 30 jewels. We cannot prove exact restoration of all original keys from the failed file, and did not infer absent values from historical snapshots. Preserve that limitation in the execution record.

## Guidance

For ephemeral DTOs, use the existing reflection wrapper around the actual `Newtonsoft.Json` assembly. A direct `using Newtonsoft.Json` is ambiguous here because Unity Localization embeds another copy. Before entering Play, serialize, deserialize into the expected DTO type, check the expected row count/key/type set, and compare every value and absence marker. Write the file only after that round trip passes. Treat the file's actual contents as evidence, not the capture method's return count.

Retain failed snapshots. The corrected fixture writes `ui-preview-preferences-v2.json` and verifies its round trip before use; restoration verifies all 89 captured keys and key absence. This corrected baseline is captured after first-preview recovery, not misrepresented as the missing original baseline.

## Why This Matters

Play startup itself can save derived upgrade values, analytics state and default preferences. A broken snapshot must therefore stop a verification run before any mutation. After such a failure, repair only changes supported by evidence; do not borrow old session snapshots or reset the whole registry.

## When to Apply

Use for every new ephemeral Unity fixture that captures mutable game or Editor preferences, especially account, upgrade and reward tests. Existing source-controlled DTOs still need a round-trip gate when used for recovery.

## Examples

`tools/essential-account-preview.cs` verifies `ReadRows(text)?.rows?.Length` before writing and then checks values and absence after restoration. `ui-preview-restoration.json` records the corrected run's verification. Its minimized GameView screen captures were also stale; UI evidence instead uses a native camera render with temporary overlay routing and exact restoration. Label that method accurately, and exclude stale frames from acceptance.

Related: [Unity material keyword verification](verify-unity-material-keywords-after-bulk-outline-conversions-2026-07-02.md) and [preserve shop styling during runtime refresh](../ui-bugs/keep-shop-reference-styling-through-runtime-refresh-2026-09-17.md).
