---
title: Derive Editor Palettes from Runtime Prefab Registries
date: 2026-08-30
category: design-patterns
module: Unity Noryangjin map tooling
problem_type: design_pattern
component: tooling
severity: medium
applies_when:
  - "A runtime ScriptableObject already defines the canonical set of placeable composite prefabs."
  - "The source folder contains both complete gameplay objects and single-part construction prefabs."
  - "Editor content tabs need to retain semantic ownership after a prefab is placed."
tags:
  - "unity"
  - "editor-tooling"
  - "map-tool"
  - "palette"
  - "scriptableobject"
  - "prefab-registry"
  - "single-source-of-truth"
  - "regression-test"
---

# Derive Editor Palettes from Runtime Prefab Registries

## Context

The Noryangjin map tool needed to expose every gameplay obstacle in its
`기믹` tab. The obstacle folders contain both complete composite obstacles and
single parts such as `Boat`, `Oil`, `Bucket`, and `Seagull`. Scanning the folder
would therefore expose duplicates and implementation details that the runtime
never spawns directly.

The existing `ObstaclePrefabs.asset` already maps each `ObstaclePattern` to the
canonical composite prefab used by `ObstaclePooler`. The editor palette needed
to follow that registry instead of creating a second hardcoded list.

## Guidance

Load the runtime registry and extract only valid, non-`None`, unique prefab
paths while preserving its authored order:

```csharp
ObstaclePrefabs config =
    AssetDatabase.LoadAssetAtPath<ObstaclePrefabs>(ObstaclePaletteConfigPath);

foreach (ObstacleTypePrefab entry in config.obstaclePrefabs)
{
    if (entry == null ||
        entry.pattern == ObstaclePattern.None ||
        entry.prefab == null)
    {
        continue;
    }

    string path = AssetDatabase.GetAssetPath(entry.prefab);
    if (string.IsNullOrEmpty(path))
        continue;

    string normalizedPath = path.Replace('\\', '/');
    if (!paths.Contains(normalizedPath))
        paths.Add(normalizedPath);
}
```

Add those prefabs explicitly to the semantic palette section before running any
broad folder scan. `HasPaletteItem` then prevents a configured composite from
appearing a second time:

```csharp
items.Add(new PaletteItem(
    BuildPaletteDisplayLabel(prefabPath),
    prefabPath,
    prefab,
    NoryangjinMapToolPaletteCategory.Prop,
    10 + index,
    NoryangjinMapToolPaletteSection.Gimmick));
```

Use the same configured-path predicate everywhere the tool reasons about the
asset. Selection validation must accept it, and placed-object tab ownership must
recognize it as a gimmick from its prefab provenance:

```csharp
string prefabPath = GetPrefabAssetPathForPlacedObject(target);
bool isGimmick =
    target.GetComponent<NoryangjinTurnSpot>() != null ||
    IsObstaclePalettePrefabPath(prefabPath);
```

Do not infer semantic ownership from the placement parent or generic `Prop`
category alone. Those describe occupancy and layout, not which editor content
tab owns the object.

## Why This Matters

The runtime registry remains the single source of truth for what constitutes a
complete obstacle. Adding or replacing a configured obstacle changes both
runtime spawning and the editor palette without updating parallel arrays.

Explicit semantic section assignment also keeps selection and deletion
consistent after placement. A tile that appears under `기믹` but is later owned
by `오브젝트` would be visible yet difficult to manage from the tab that created
it.

## When to Apply

- A pool, factory, or spawner already owns a serialized prefab registry.
- Asset folders mix final prefabs with reusable pieces or variants.
- A map editor separates palette display category, occupancy layer, and semantic
  content tab.
- Designers expect configured runtime additions to become authoring options.

## Examples

The focused regression test reads `ObstaclePrefabs.asset`, enumerates its current
entries, and verifies for every path that:

- the palette contains exactly one item;
- the item belongs to `NoryangjinMapToolPaletteSection.Gimmick`;
- the path is accepted by palette selection;
- its visible label is non-empty and fits the tile limit;
- the prefab instantiates in a preview scene; and
- the placed instance is owned by `Gimmick`, not `Object`.

The test intentionally does not hardcode the current count of eight. A future
registry addition should join the palette without requiring a parallel test
list update.

## Related

- [Update map tool road definitions with scene road replacements](../logic-errors/update-map-tool-road-definitions-with-scene-road-replacements-2026-06-15.md)
- [Split editor ScriptableObjects into matching files](../workflow-issues/split-editor-scriptableobjects-into-matching-files-2026-06-12.md)
- [Preserve prefab root transforms in Noryangjin map tool placement](../logic-errors/preserve-prefab-transform-in-noryangjin-map-tool-placement-2026-06-02.md)
