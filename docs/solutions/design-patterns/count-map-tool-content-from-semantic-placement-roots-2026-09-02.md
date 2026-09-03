---
title: Count map-tool content from semantic placement roots
date: 2026-09-02
category: design-patterns
module: Unity Noryangjin map-tool information statistics
problem_type: design_pattern
component: tooling
severity: medium
applies_when:
  - Adding editor statistics derived from a fixed prefab registry
  - Counting scene placements while prefab instances contain nested implementation children
  - Displaying stable category rows for both populated and empty placement roots
related_components: [NoryangjinMapToolWindow, NoryangjinMapToolInformationSnapshot, NoryangjinMapToolGridUtilityTests]
tags: [unity-editor, map-tool, prefab-registry, direct-child-counting, empty-state, contract-testing]
---

# Count map-tool content from semantic placement roots

## Context

The Noryangjin map tool needed an `정보` tab that reports every configured road type, the road total, and object counts grouped by displayed type. A scene hierarchy is not itself a counting contract: one placed uphill road can contain a pillar companion, and ordinary prefabs can contain many meshes and helper objects. Recursively counting descendants would inflate the number the author understands as “placements.”

The first regression test also covered only Basic and RightTurn roads with plain synthetic objects. Review correctly identified that this did not prove the complete six-road registry, zero-count rows, real prefab label resolution, or nested-child exclusion.

## Guidance

Define the counting boundary as the direct children of the semantic placement containers.

```csharp
foreach (Transform child in parent)
{
    if ((child.gameObject.hideFlags & HideFlags.DontSaveInEditor) != 0)
        continue;

    string label = typeResolver(child.gameObject);
    counts[label] = counts.TryGetValue(label, out int count)
        ? count + 1
        : 1;
}
```

For Noryangjin, one direct child of `Roads` or `Props` is one authored placement. Nested meshes, road companions, colliders, markers, and generated helpers remain implementation details of that placement.

Use the existing registry as the type authority. `RoadPieces` supplies the six road prefab paths and Korean labels; the existing palette-label resolver supplies object display names. Do not infer road types from scene-instance names when a prefab connection is available.

Keep the full road schema visible even when categories are empty:

```csharp
foreach (string label in InformationRoadTypeLabels)
{
    int count = countedRoads.TryGetValue(label, out int value) ? value : 0;
    roadRows.Add(new NoryangjinMapToolTypeCount(label, count));
    roadTotal += count;
    countedRoads.Remove(label);
}
```

Cache the snapshot so hundreds of prefab-path lookups are not repeated on every IMGUI repaint. Invalidate it on hierarchy changes, Undo/Redo, and label changes, and retain an explicit refresh action for unusual prefab-state changes.

## Why This Matters

The author-facing number means “how many things I placed,” not “how many GameObjects Unity serialized.” Direct placement roots preserve that meaning even when a prefab gains or loses internal children.

A registry-derived UI also treats `0` as meaningful data. If only discovered types are rendered, an empty but supported road category disappears and the panel no longer describes the full tool capability. Consuming the same registry for placement buttons, information rows, and tests prevents those surfaces from drifting apart.

## When to Apply

- A Unity Editor tool summarizes scene content grouped by prefab or palette type.
- Prefabs contain companion objects, visual children, generated markers, or other nested implementation details.
- A fixed registry defines every supported category, including categories with zero instances.
- Counts are cached to avoid repeated editor-only asset lookups during IMGUI repaint events.

## Examples

The minimum regression contract should cover all of these in one isolated preview scene:

1. Empty `Roads` and `Props` produce all six road rows at zero, road total zero, and no object rows.
2. Every real `RoadPiece` prefab is instantiated once; every category reports one and the total reports six.
3. One real Props prefab is instantiated twice and another once; display-name grouping reports `2:1` with total three.
4. A nested child is added below a placement root; the total remains unchanged.
5. The tab order is fixed as `오브젝트 → 적군 → 기믹 → 보너스 → 정보`, and the information tab exposes no placement palette sections.

Compilation through `dotnet build Assembly-CSharp-Editor.csproj -nologo` verifies the implementation and tests compile. A Unity Pipeline run that discovers the target test but times out before an execution verdict is an infrastructure failure, not evidence that the test passed or failed; report it separately.

## Related

- [Update map tool road definitions with scene road replacements](../logic-errors/update-map-tool-road-definitions-with-scene-road-replacements-2026-06-15.md)
- [Derive Editor palettes from runtime prefab registries](derive-editor-palettes-from-runtime-prefab-registries-2026-08-30.md)
- [Resolve selected prefab children to Noryangjin map tool placement roots](../logic-errors/resolve-selected-prefab-child-to-map-tool-placement-root-2026-06-08.md)
- [Noryangjin gameplay and map-tool guide](../../noryangjin-gameplay-maptool.md)
