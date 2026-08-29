---
title: Prevent Nearby Bonus Altar Duplicates on Candidate Exhaustion
date: 2026-08-17
last_updated: 2026-08-29
category: logic-errors
module: Unity Noryangjin bonus altar
problem_type: logic_error
component: tooling
symptoms:
  - "Nearby altars could reuse a bonus stat after candidate fallback or a late OnEnable erased an already committed rolledStat."
  - "Candidate filtering repopulated the pool after exclusions removed every valid row."
  - "An invalid authored roll could leave stale title, stat, value, and collider state active."
  - "A UI integration test on an inactive root could silently exercise the legacy wall path."
  - "Percent variants could show a shortened stat name or the wrong suffix while percent-specific icon art remained visible."
root_cause: logic_error
resolution_type: code_fix
severity: medium
related_components:
  - "testing_framework"
tags:
  - "unity"
  - "noryangjin"
  - "bonus-altar"
  - "candidate-exhaustion"
  - "duplicate-prevention"
  - "lifecycle-state"
  - "excel-data"
  - "inactive-hierarchy"
---

# Prevent Nearby Bonus Altar Duplicates on Candidate Exhaustion

## Problem

Nearby altar uniqueness was implemented as a preference rather than a hard
invariant. `BonusAltarRules.BuildCandidates` restored every supported row when
the non-duplicate pool became empty, so three mutually nearby Elite altars
could not honor the rule: the workbook contains only two `Rare` abilities.

A second failure mode later broke the same invariant even when candidates were
available. `WallScript` committed the selected stat, but a later
`AuthoredBonusWall.OnEnable()` cleared `rolledStat`. The next nearby altar then
observed an empty committed set and could choose the same stat. In the live
scene, two altars only `3.38` units apart both displayed
`missileDistance_normal` while both `RolledStat` values were empty.

The same boundary also exposed a data-ownership problem. Altar aliases, visible
stat names, value types, and random ranges belong to the `Data.xlsx` `보너스`
sheet. Duplicating those values in a C# switch or in expected-value test
literals lets runtime behavior drift away from the workbook without failing a
test.

Presentation policy had the same drift risk. `Percent` rows need a `%` suffix,
while `Ratio` and `Value` rows remain plain. Attack/health percent variants must
share the base stat name and icon, with their row-specific alias preserving the
variant identity.

## Symptoms

- The first two Elite altars could roll `tungtungAdd` and `boombarAdd`, while a
  third nearby Elite altar silently received one of them again.
- Two nearby Normal altars could both show the same bonus even though the
  distance filter included them, because a late enable callback erased the
  first altar's committed stat.
- Normal and Unique were less likely to reveal the flaw because their pools
  contain six and seven supported stats.
- Returning an empty list alone was insufficient: the legacy `buffType` path
  could still display or apply the prefab's stale/default bonus, and its
  collider could remain touchable.
- A first success-path UI test deactivated the root. The default
  `GetComponentInParent<AuthoredBonusWall>()` lookup then missed the authored
  marker, so the test exercised the legacy path instead of the path it claimed
  to cover.
- A related prefab test expected the temporary builder name `Altar`, although
  Unity persists a prefab root with the asset filename (`Box_left`).

## What Didn't Work

Falling back to all supported rows preserved a non-empty random pool by
discarding the actual requirement:

```csharp
return nonDuplicate.Count > 0 ? nonDuplicate : allSupported;
```

Retry-based rerolling also cannot solve pool exhaustion. With two Elite stats,
no number of retries can produce a third distinct result.

Changing the proximity threshold or candidate filtering also could not fix the
lifecycle variant. The existing exclusion works when `rolledStat` remains
committed; the failure occurred after selection, when `OnEnable` deleted the
state that `CollectNearbyRolledStats()` needed to read.

Removing only that fallback was still incomplete. `InitWall()` continued into
`SetStats()`, `SetWallSprite()`, and later `ApplyWallEffect()`. Without an
explicit invalid authored-roll state, those stages could reuse legacy state.

Keeping the Korean altar titles in `ResolveAlias` was also incomplete:

```csharp
(Rarity.Normal, "att") => "날카로운 일격"
```

The workbook could change while the compiled title stayed frozen. Tests that
repeated the same literal would only confirm that the two copies still agreed.

A blanket percent removal was incorrect, but tying the suffix to `Ratio` was
also incorrect. The actual distinction is the workbook's `Percent` value type.
Using the selected percent row's abbreviated name directly also produced `공`
instead of the shared base name `공격력`.

The prefab-name assertion was also coupled to a transient implementation
detail. `SaveAsPrefabAsset` normalizes the persisted root to the filename, so
`Box_left.prefab` loads as `Box_left`, not the name assigned before saving.

## Solution

Candidate construction now returns exactly the supported grade rows minus the
stats already committed by nearby altars. There is deliberately no fallback:

```csharp
if (excludedStats == null || !excludedStats.Contains(row.stat))
    candidates.Add(row);

return candidates;
```

`AuthoredBonusWall.CollectNearbyRolledStats()` gathers committed stats from
active altars in the same scene. It compares XZ distance and uses the larger of
the two serialized thresholds, so proximity is symmetric. A successful roll
commits its stat immediately for the next altar to observe.

The committed value must also survive later enable callbacks. `OnEnable()` now
only synchronizes the authored grade and VFX state; `BeginRoll()` is the sole
operation that clears the previous commitment:

```csharp
private void OnEnable()
{
    SyncWallAuthoringState();
}

public void BeginRoll()
{
    rolledStat = null;
}
```

`LateOnEnable_DoesNotEraseCommittedStatBeforeNextRoll` fixes this state
transition in place: `CommitRoll -> OnEnable` preserves the stat, and the next
explicit `BeginRoll` clears it. The test failed before the fix and passed after
it. A live Noryangjin run then produced distinct `hpPercent` and `attPercent`
choices with both committed records populated.

When no candidate remains, `WallScript` logs an error and fails closed. The
authored altar calculates a zero value, clears and disables its visible text
and icon, disables its collider and trigger, skips the normal presentation
path, and applies no gameplay effect:

```csharp
private bool HasInvalidAuthoredRoll()
{
    return !hasSelectedBonusRow &&
           GetComponentInParent<AuthoredBonusWall>(true) != null;
}
```

This guard is checked by `SetStats`, `SetWallSprite`, and `ApplyWallEffect`.
An impossible third Elite altar therefore cannot turn a stale serialized
`buffType` into a duplicate reward.

The authored roll reads each presentation and value field from the parsed
workbook row:

```csharp
bonusAlias = BonusAltarRules.ResolveAlias(selectedBonusRow);
bonusValue = BonusAltarRules.ResolveValue(selectedBonusRow, Random.value, baseValue);
selectedDisplayRow = BonusTables.ResolveDisplayRow(selectedBonusRow);
statNameText.text = BonusAltarRules.ResolveDisplayName(selectedDisplayRow);
```

`ResolveAlias` uses `별칭`, falling back only to `이름` and then `항목` when a
cell is blank. `ResolveDisplayName` uses `이름`, then `항목`. `ResolveValue`
interpolates between `최소` and `최대`, applies `수치 타입` semantics, and
rounds only discrete stats. This removes the hardcoded rarity/stat-to-Korean
title switch without inventing replacement data in code.

Display-only rules now also have one runtime source:

```csharp
BonusAltarRules.FormatDisplayValue(value);
BonusAltarRules.ResolveIconResourceName(buffType);
BonusAltarRules.ResolveLocalizationKey(buffType);
```

The formatter appends `%` only for `BonusValueType.Percent`; `Ratio` and `Value`
remain plain. Attack/health percent variants reuse the same-rarity base row's
`이름`, the ordinary icon, and the base localization key, while their alias,
effect, and value type remain owned by the originally selected row. The editor
prefab builder consumes the shared icon resolver, so regeneration cannot restore
percent-specific icon art.

```csharp
float random01 = Random.value;
bonusValue = BonusAltarRules.ResolveValue(row, random01, baseValue);
displayBonusValue = BonusAltarRules.ResolveDisplayValue(row, random01);
string text = BonusAltarRules.FormatDisplayValue(
    displayBonusValue,
    row.valueType);
```

The mapping is composed once as `BuffType -> stat key -> display stat key`.
`attPercent` collapses to `att` and `hpPercent` to `hp` for name, icon, and
localization presentation. `BonusTables.ResolveDisplayRow` performs the
same-rarity row lookup once when the altar rolls, and `WallScript` caches the
result. `UpdateStatUI` is the only remaining value writer.

The map tool now exposes one reusable `Box_left` prefab. `AuthoredBonusWall`
owns the Normal/Elite/Unique grade and synchronizes the child `WallScript`;
Elite maps to the workbook's existing `Rare` rows. Re-enabling an inactive
wall also performs only the `OnEnable` roll instead of immediately rolling a
second time from `WallManager.InIt()`.

Tests verify the persisted contract instead of the builder's temporary name:

```csharp
Assert.That(
    prefab.name,
    Is.EqualTo(Path.GetFileNameWithoutExtension(prefabPath)));
```

The remaining assertions cover the meaningful behavior: one palette prefab,
an `AuthoredBonusWall`, Normal default grade, random data-driven selection,
and the `+?` authoring preview.

Workbook contract tests iterate the parsed rows and compare aliases and display
names with their authored cells. Separate tests exercise fallback semantics,
plain icon mapping, numeric endpoints, and Percent display. A deterministic UI
test proves the exact contract: both variants show `공격력`, while Percent shows
`+15%` and Ratio shows `+15`. The success-path UI test uses the selected row's
`valueType` to assert the exact rendered suffix.

## Why This Works

The valid set now has one definition:

```text
valid candidates = supported workbook rows - nearby committed stat keys
```

An empty result means no valid assignment exists; it no longer means the
constraint may be relaxed. Failing closed across selection, value generation,
presentation, collision, and application prevents stale legacy state from
reintroducing the forbidden reward through another stage.

Treating the parsed row as the sole presentation and range authority means an
Excel edit changes both the chosen label and resolved value without a matching
C# edit. Tests protect the field mapping and the rendered result instead of
copying workbook-owned content into a second table.

Separating the selected gameplay row from the cached display row preserves both
requirements: the ability keeps its own alias/effect/value semantics, while the
player sees one stable stat name. Runtime walls and generated previews cannot
diverge because they consume the same display-key and icon resolvers.

Unity runs these callbacks on the main thread, but that does not make parent
and child enable ordering a safe state boundary. Keeping invalidation inside
the explicit `BeginRoll()` transition means callback order cannot erase a
successful commitment. Committing only after the row and `BuffType` resolve
successfully still lets subsequent altars exclude real choices rather than
partial or failed rolls.

## Prevention

- Keep `AllNearbyStatsExhausted_ReturnsNoDuplicateCandidate`; it excludes both
  Rare stat keys and requires an empty result.
- Keep `LateOnEnable_DoesNotEraseCommittedStatBeforeNextRoll`; committed stats
  must survive lifecycle synchronization until `BeginRoll()` starts the next
  selection transaction.
- Keep workbook candidate-count tests (`Normal: 6`, `Rare: 2`, `Unique: 7`) so
  level design can see each grade's uniqueness capacity.
- Keep workbook-row contract tests that require every supported row to provide
  `별칭`, `이름`, `항목`, `수치 타입`, `최소`, and `최대`; do not repeat the
  workbook's current Korean values as code-owned expected literals.
- Keep suffix semantics explicit: only `Percent` appends `%`; `Ratio` and
  `Value` remain plain.
- Cache a separate display row when variants share a visible name but retain
  different aliases and effects. Test the final two-line UI directly, including
  `공격력 +15%` and `공격력 +15`.
- Treat hard exclusions as fail-closed. Do not restore rejected candidates to
  satisfy an API that prefers a non-empty random pool.
- Assert both invalid and valid presentation paths. The invalid case must clear
  text and disable the collider; the valid case must render the selected row's
  alias, name, and resolved value.
- Match Unity's runtime hierarchy in integration tests. Use an active authored
  root for the normal path, and request `includeInactive: true` explicitly only
  when inactive ancestry is part of the contract.
- Route every authored grade change through `AuthoredBonusWall.Configure` and
  display the root grade in editor UI.
- Test prefab identity using its persisted asset filename and verify gameplay
  through components and serialized behavior.
- Add map-tool validation before placing a fully connected same-grade cluster
  larger than that grade's distinct stat pool if level designers begin using
  more than two adjacent choices.

## Related Issues

- [Bake generated prefab UI previews and isolate EditMode tests](../workflow-issues/bake-generated-prefab-ui-previews-and-isolate-editmode-tests-2026-08-13.md)
- [Restore authored bonus choice VFX baselines on re-enable](../ui-bugs/restore-authored-bonus-choice-vfx-baselines-on-reenable-2026-08-15.md)
- [Isolate dedicated MeshyAI assets from generic repair](../workflow-issues/isolate-dedicated-meshyai-assets-from-generic-repair-2026-08-09.md)
- [Resolve a selected prefab child to its map-tool placement root](resolve-selected-prefab-child-to-map-tool-placement-root-2026-06-08.md)
- [Atomically reload enemy stats and reset pooled runtime state](../integration-issues/atomic-enemy-stat-workbook-reload-and-pool-safe-reset-2026-08-02.md)
- [Use independent oracles for configuration column tests](../best-practices/use-independent-oracles-for-configuration-column-tests-2026-08-01.md)
