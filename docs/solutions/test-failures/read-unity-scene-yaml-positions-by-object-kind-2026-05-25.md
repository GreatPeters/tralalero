---
title: Read Unity Scene YAML Positions By Object Kind
date: 2026-05-25
last_updated: 2026-05-26
category: docs/solutions/test-failures
module: Unity scene generation tests
problem_type: test_failure
component: testing_framework
symptoms:
  - "A generated Unity scene test reported a center-lane object at another prefab's side offset"
  - "The scene YAML showed the tested primitive object at the expected position"
  - "A prefab scale/position assertion read a different instance because the helper only searched before the name override"
root_cause: test_isolation
resolution_type: test_fix
severity: low
tags: [unity, scene-yaml, editmode-tests, prefabs, primitives]
---

# Read Unity Scene YAML Positions By Object Kind

## Problem
Stage02 scene-generation tests failed while checking that `Center_Coin_Line_05` was centered on the runner lane. The scene itself was correct, but the YAML test helper returned a side-offset position from an unrelated prefab override.

## Symptoms
- `Stage02HighwayAutoDraftBuilderTests.BuildScene_KeepsGameplayLaneReadable` expected `x = 0` but read `x = -10.5`, then `x = 5.95`.
- Searching the generated `.unity` file showed `m_Name: Center_Coin_Line_05` followed by `m_LocalPosition: {x: 0, ...}`.

## What Didn't Work
- Searching backward for the nearest `PrefabInstance:` block worked for prefab override names, but broke primitive GameObjects because it could bind the primitive name to a previous prefab's override block.
- Searching for `propertyPath: m_LocalPosition.x` after a primitive object's `m_Name:` also broke because the next prefab override could appear before the primitive's own `Transform` block.
- Searching only the part of a `PrefabInstance` block before `value: ObjectName` broke when Unity serialized the name modification before transform or scale modifications.

## Solution
Treat prefab override names and native GameObject names as different serialization shapes.

For prefab override names (`value: ObjectName`), read `propertyPath: m_LocalPosition.x` inside the containing `PrefabInstance:` block. For native GameObject names (`m_Name: ObjectName`), skip prefab override fields and read the following `m_LocalPosition: {x: ...}` from that object's `Transform`.

```csharp
int nameIndex = yaml.IndexOf("value: " + objectName, StringComparison.Ordinal);
bool prefabOverrideName = nameIndex >= 0;
if (nameIndex < 0)
    nameIndex = yaml.IndexOf("m_Name: " + objectName, StringComparison.Ordinal);

int positionIndex = -1;
if (prefabOverrideName)
{
    int prefabBlockStart = yaml.LastIndexOf("PrefabInstance:", nameIndex, StringComparison.Ordinal);
    if (prefabBlockStart >= 0)
        positionIndex = yaml.IndexOf("propertyPath: m_LocalPosition.x", prefabBlockStart, nameIndex - prefabBlockStart, StringComparison.Ordinal);
}

if (positionIndex < 0)
    positionIndex = yaml.IndexOf("m_LocalPosition: {x:", nameIndex, StringComparison.Ordinal);
```

For prefab override fields, scan the entire containing `PrefabInstance` block rather than assuming the name override comes last.

```csharp
int blockStart = yaml.LastIndexOf("PrefabInstance:", nameIndex, StringComparison.Ordinal);
int blockEnd = yaml.IndexOf("\n--- !u!", nameIndex, StringComparison.Ordinal);
if (blockEnd < 0)
    blockEnd = yaml.Length;

int positionIndex = yaml.IndexOf(
    "propertyPath: m_LocalPosition.x",
    blockStart,
    blockEnd - blockStart,
    StringComparison.Ordinal);
```

## Why This Works
Unity serializes prefab instance overrides and normal GameObjects differently. Prefab names and transform values often live in a `PrefabInstance` modification list, while primitive objects serialize as a `GameObject` block followed by a `Transform` block. A single substring scan can cross object boundaries unless it respects that distinction.

Within a single prefab instance, Unity does not guarantee that `m_Name`, `m_LocalPosition`, and `m_LocalScale` modifications are ordered around the human-readable object name. Tests should treat the whole `PrefabInstance` YAML block as the object's override scope.

## Prevention
- In scene YAML tests, branch on whether the object name was found as `value:` or `m_Name:`.
- For primitive GameObjects, prefer `m_LocalPosition` after `m_Name` and do not scan prefab override `propertyPath` entries.
- For prefab instances, search from the containing `PrefabInstance:` marker to the next Unity YAML document marker before reading position or scale overrides.
- When a generated-scene test reports an impossible transform, confirm the actual YAML block before changing scene-generation code.

## Related Issues
- [Use Continuous Procedural Bases For Unity Stage Layouts](../design-patterns/use-continuous-procedural-bases-for-unity-stage-layouts-2026-05-25.md)
- [Preserve MeshyAI Prefab Axis Correction In Scene Builders](../workflow-issues/preserve-meshyai-prefab-axis-correction-in-scene-builders-2026-05-25.md)
