---
title: Reapply external prefab YAML after Unity script recompilation
date: 2026-08-03
last_updated: 2026-08-04
category: docs/solutions/workflow-issues
module: Unity prefab serialization and refactoring
problem_type: workflow_issue
component: development_workflow
severity: medium
applies_when:
  - "Editing Unity prefab YAML externally during a serialized-field refactor"
  - "Recompiling scripts while Unity has affected prefabs loaded in editor state"
  - "Migrating one serialized contract across several prefab variants"
  - "Converting serialized runtime state to private or NonSerialized fields"
related_components: [testing_framework, tooling]
tags: [unity, prefab-yaml, serialization, script-recompile, asset-refresh, authoring-contract, contract-testing, script-guid]
---

# Reapply external prefab YAML after Unity script recompilation

## Context

Five `EnemyScript_space` prefabs were migrated by editing their serialized
YAML outside Unity while the Editor still had the affected assets and old
script shape loaded. A later script recompile/import rewrote
`Enemy_FatMan.prefab` from stale editor state and restored its obsolete
serialized fields. The other four prefabs happened to retain the migration,
so checking only one successful file would have missed the partial rollback.

An EditMode authoring-contract test that enumerated all five canonical
prefabs caught the mismatch. The filesystem edit and Unity's loaded asset
database had temporarily become competing sources of truth.

A later Player, Wall, and Enemy cleanup exposed a second blind spot: C# builds
and reflection assertions passed while eight Wall prefabs still contained
obsolete `missileSpeedSpr`, `missileSpeedUniqueSpr`, and `missileSpeed` keys.
Reflection proved that the fields were absent from `WallScript`; it could not
prove that Unity YAML had stopped serializing their old values.

## Guidance

Use this order when a component refactor also requires direct prefab YAML
edits:

1. Finish the C# serialized-field changes.
2. Let Unity finish compilation, assembly reload, and script import.
3. Apply or reapply the narrow YAML migration after the new component shape
   is loaded.
4. Run `AssetDatabase.Refresh()` or **Assets > Refresh**.
5. Inspect both the raw YAML and Unity's `SerializedObject` view. For raw
   checks, resolve the target `MonoScript` GUID and inspect only matching
   `!u!114` `MonoBehaviour` blocks.
6. Make the test fail when an expected script block is absent; otherwise a
   wrong asset path can produce a vacuous pass.
7. When removing a component, delete its GameObject component-list entry and
   its YAML object block, then verify that every remaining file ID resolves.
8. Run an exact contract test across every migrated prefab or asset.
9. Review the final diff after Unity finishes importing, then refresh once
   more to prove the result is stable.

Treat Unity as an active writer. A successful text replacement is not proof
that a migration survived the next import or save.

Do not use a repository-wide forbidden-key assertion when another component
can legitimately own a field with the same name. Scope the assertion to the
serialized block whose `m_Script` line contains the target script GUID.

Explicit refresh/import is appropriate here because another process changed
the asset on disk. Do not generalize it into an extra reimport after a change
already saved through `PrefabUtility` or `SerializedObject`; that can create
unnecessary serializer and shader churn.

## Why This Matters

Unity can reserialize a prefab from its loaded object graph during script
compilation, import, inspector activity, or asset saving. If that graph still
represents the old component contract, it can overwrite a correct external
edit.

The migration is complete only when these three representations agree:

- the current C# serialized-field contract;
- the prefab YAML on disk;
- Unity's imported `SerializedObject` state.

Checking every prefab matters because the rollback can be selective.
Likewise, compiled metadata and reflection are not independent evidence for
the file on disk: both can be correct while stale YAML remains ignored in the
asset text.

## When to Apply

- Renaming, removing, or replacing `[SerializeField]` fields.
- Bulk-editing `.prefab`, `.unity`, or `.asset` YAML outside Unity.
- Changing a `MonoBehaviour` while related prefabs are open or loaded.
- Migrating several prefab variants to one exact authoring contract.
- Seeing obsolete serialized keys return in only some assets after import.

## Examples

Unsafe ordering:

```text
1. Patch prefab YAML externally.
2. Change the component's serialized fields.
3. Unity recompiles and reserializes a prefab from stale loaded state.
4. Legacy keys return in that prefab.
```

Stable ordering:

```text
1. Finish and compile the component change.
2. Wait for Unity's assembly reload.
3. Apply the prefab YAML migration.
4. Refresh the AssetDatabase.
5. Verify raw YAML and SerializedObject values.
6. Run the complete prefab-contract test and inspect the final diff.
```

The contract test should enumerate the authoritative prefab catalog rather
than sample one asset:

```csharp
foreach (ForwardEnemyArchetypeDefinition definition in
         ForwardEnemyArchetypeCatalog.Definitions)
{
    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
        definition.PrefabPath);
    EnemyScript_space enemy = prefab.GetComponent<EnemyScript_space>();
    var serializedEnemy = new SerializedObject(enemy);

    Assert.That(
        serializedEnemy.FindProperty("enemyData").objectReferenceValue,
        Is.Not.Null,
        definition.PrefabPath);
    Assert.That(
        serializedEnemy.FindProperty("bonusWall").objectReferenceValue,
        Is.Not.Null,
        definition.PrefabPath);
}
```

Complement Unity-side assertions with a disk search for forbidden legacy
keys when deliberate contract removal is part of the migration. A reusable
disk assertion can isolate the correct serialized component before checking
field names:

```csharp
string scriptGuid = FindScriptGuid<T>();
string scriptMarker = $"guid: {scriptGuid}";

foreach (string assetPath in authoritativeAssetPaths)
{
    string yaml = File.ReadAllText(Path.GetFullPath(assetPath));
    bool foundComponent = false;

    foreach (string componentYaml in FindMonoBehaviourBlocks(yaml, scriptMarker))
    {
        foundComponent = true;
        Assert.That(componentYaml, Does.Not.Match(@"(?m)^\s*obsoleteField\s*:"));
    }

    Assert.That(foundComponent, Is.True, $"Missing expected script in {assetPath}");
}
```

The broader implementation for Player, Wall, ExtraHelp, projectiles, and
enemy data lives in
`Assets/Tests/Editor/NoryangjinRuntimeCleanupContractTests.cs`. It derives the
five Forward enemy paths from `ForwardEnemyArchetypeCatalog.Definitions`
instead of duplicating the catalog.

If Unity-side verification opens a save prompt for an already-dirty authored
scene, do not save, discard, or bypass the prompt. Stop at builds and disk
contracts, or run the test later from an isolated clean project state.

## Related

- [Verify Unity material keywords after bulk outline conversions](verify-unity-material-keywords-after-bulk-outline-conversions-2026-07-02.md)
- [Run Unity scene generation through CLI Connector exec when editor reload is stale](run-unity-scene-generation-through-cli-connector-exec-when-editor-reload-is-stale-2026-06-21.md)
- [Repair Unity assets when the editor command path is blocked](repair-unity-assets-when-editor-command-path-is-blocked-2026-05-24.md)
- [Prevent Humanoid reimport pose overrides when saving nested Unity prefabs](prevent-humanoid-reimport-pose-overrides-when-saving-unity-prefabs-2026-07-27.md)
- [Protect active Unity scenes from broad EditMode test runs](protect-active-unity-scenes-from-broad-editmode-test-runs-2026-07-18.md)
- [Use independent oracles for configuration column tests](../best-practices/use-independent-oracles-for-configuration-column-tests-2026-08-01.md)
