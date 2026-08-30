---
title: Prevent Humanoid reimport pose overrides when saving nested Unity prefabs
date: 2026-07-27
category: docs/solutions/workflow-issues
module: Unity Forward enemy animation asset workflow
problem_type: workflow_issue
component: tooling
severity: medium
applies_when:
  - "Converting FBX-backed character prefabs from Generic to Humanoid"
  - "Saving prefabs through `PrefabUtility.LoadPrefabContents` after an FBX reimport"
  - "A controller-only prefab edit unexpectedly adds nested bone rotation overrides"
tags: [unity, humanoid, animator, prefabutility, fbx, meshyai, serialization, idempotency]
---

# Prevent Humanoid reimport pose overrides when saving nested Unity prefabs

## Context

Five Forward enemy prefabs originally needed Humanoid Avatars, one shared
controller, and per-character override controllers. The current controller has
`idle`, `attack_loop`, `walk`, `run`, `die`, and `attack_once` states.
The editor utility correctly changed each Animator, but its first
`PrefabUtility.SaveAsPrefabAsset` pass also serialized unrelated changes:

- `m_LocalRotation` overrides appeared on bones inside nested FBX instances.
- Newly introduced Unity-version fields appeared on otherwise untouched
  components.
- The intended one-line controller change became a much broader prefab diff.

The extra bone values were not authored animation configuration. The nested
FBX source had just been reimported as Humanoid, and saving the loaded prefab
captured the loaded model's current pose differences as property overrides.
Removing those values and rerunning an idempotent setup produced only the
intended Animator references.

## Guidance

Treat FBX reimport and prefab persistence as two separate mutation stages.
After reimport finishes, apply only the Animator properties that belong to the
new contract and avoid saving when the prefab already matches:

```csharp
bool requiresSave =
    animator.avatar != expectedAvatar ||
    animator.runtimeAnimatorController != expectedController ||
    animator.applyRootMotion ||
    !animator.enabled;

if (!requiresSave)
    return;
```

For nested FBX prefab instances, snapshot their existing
`PropertyModification` keys before changing the Animator. After recording the
Animator override, discard only newly introduced pose rotations that were not
present in the snapshot:

```csharp
PropertyModification[] modifications =
    PrefabUtility.GetPropertyModifications(nestedRoot) ??
    Array.Empty<PropertyModification>();

PropertyModification[] filtered = modifications
    .Where(modification =>
        originalKeys.Contains(GetModificationKey(modification)) ||
        !IsTransientHumanoidPoseOverride(modification))
    .ToArray();

PrefabUtility.SetPropertyModifications(nestedRoot, filtered);
```

Limit the transient-pose predicate to Transform rotation properties. Do not
blanket-revert prefab modifications, because authored root rotation, scale,
materials, and other overrides may be intentional:

```csharp
return modification.target is Transform &&
       (modification.propertyPath.StartsWith("m_LocalRotation.") ||
        modification.propertyPath.StartsWith("m_LocalEulerAnglesHint."));
```

Finally, review the prefab YAML diff and execute the setup twice. A second run
should leave the generated controller, overrides, and prefabs byte-for-byte
unchanged.

## Why This Matters

A valid Humanoid Avatar and a compiling AnimatorController do not prove that
the prefab edit was clean. Unintended bone overrides can pin or skew a
character's imported rest pose, while unrelated serializer churn obscures the
actual controller change during review.

The snapshot-and-filter approach preserves pre-existing authored overrides,
removes only pose data introduced by the current setup pass, and makes the
tool safe to rerun. The no-op guard also avoids broad Unity reserialization
when nothing needs repair.

## When to Apply

- Converting Generic MeshyAI or Mixamo-style FBXs to Humanoid.
- Assigning Avatars or AnimatorOverrideControllers to prefabs that nest an FBX
  model prefab.
- Building repeatable editor repair tools with
  `LoadPrefabContents`/`SaveAsPrefabAsset`.
- Reviewing a prefab diff where the requested change concerns only Animator
  references but bone transforms also changed.

## Examples

The clean result for a nested FBX prefab is a controller reference change such
as:

```diff
- objectReference: {fileID: 9100000, guid: <old-controller>, type: 2}
+ objectReference: {fileID: 22100000, guid: <character-override>, type: 2}
```

It should not include new overrides like:

```diff
+ propertyPath: m_LocalRotation.x
+ value: 0.056353718
```

Verification should cover all three layers:

1. Each imported animation clip reports `isHumanMotion`.
2. Each prefab Animator owns a valid Humanoid Avatar and its expected override
   controller.
3. A preview-scene Animator can enter all six current states,
   `attack_once` returns to `idle`, and a second setup run changes no files.

## Related

- [Preserve MeshyAI Prefab Axis Correction In Scene Builders](preserve-meshyai-prefab-axis-correction-in-scene-builders-2026-05-25.md)
- [Preserve prefab root transforms in Noryangjin map tool placement](../logic-errors/preserve-prefab-transform-in-noryangjin-map-tool-placement-2026-06-02.md)
- [Avoid broad Unity MCP asset enumeration in large projects](avoid-broad-unity-mcp-asset-enumeration-2026-06-13.md)
