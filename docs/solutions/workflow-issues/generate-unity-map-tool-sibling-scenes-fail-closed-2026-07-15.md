---
title: Generate Unity Map-Tool Sibling Scenes with Fail-Closed Verification
date: 2026-07-15
category: docs/solutions/workflow-issues
module: Unity Noryangjin map tooling
problem_type: workflow_issue
component: development_workflow
severity: medium
applies_when:
  - "Generating a derived Unity map-tool scene while the authored source scene must remain byte-for-byte unchanged"
  - "An editor builder must run only against one exact target scene and route skeleton"
  - "Generated layouts must preserve prefab provenance and clear gameplay lanes"
  - "Tests inspect Unity scene YAML containing prefab-instance name overrides"
tags:
  - "unity"
  - "map-tool"
  - "scene-generation"
  - "fail-closed"
  - "prefab-provenance"
  - "lane-bounds"
  - "yaml-testing"
  - "source-preservation"
---

# Generate Unity Map-Tool Sibling Scenes with Fail-Closed Verification

## Context

A derived map-tool scene must be safe to regenerate without modifying its authored source or deleting manual placements. The Noryangjin Mode 2 layout was created as a sibling of the existing map-tool scene, kept its copied 19-road W-to-N-to-E route, and added Stage01 prefab dressing under a dedicated ownership prefix.

Several checks that looked sufficient at first were not:

- Counting 19 road children did not prove their transforms or source prefabs were unchanged.
- `GameObject.Find` could resolve a same-named root from another additively loaded scene.
- A YAML test looking only for `m_Name: Prop_Layout2_` counted zero generated prefab instances because Unity stored their names in a `PropertyModification` `value:` field.
- A combined refresh, recompile, and menu invocation timed out after compilation, even though retrying only the menu command succeeded.
- Two water tiles had correct-looking positions but the wrong final orientation, leaving a visible gap that became obvious only from adjacent rotations and renderer bounds.
- A failed preview capture could leave an older image that no longer represented the current scene.

## Guidance

Use a fail-closed sequence that validates every stable input before deleting or placing anything.

### 1. Separate the source and target contracts

Save the working source scene to a sibling target once, then make the generator accept only the exact target asset path. Record the source file's SHA-256 before the first mutation and compare it again at the end.

```csharp
internal static bool CanBuildScenePath(string scenePath)
{
    return string.Equals(scenePath, TargetScenePath,
        StringComparison.OrdinalIgnoreCase);
}
```

Do not treat a similar scene name as sufficient, and never fall back to saving the active scene under the target path.

### 2. Scope hierarchy lookup to the target scene

Search `scene.GetRootGameObjects()` rather than the global hierarchy. Include inactive roots and verify that the root, `Roads`, and `Props` all belong to the target scene handle.

```csharp
GameObject root = scene.GetRootGameObjects()
    .SingleOrDefault(candidate => candidate.name == RootName);

if (root == null ||
    roads.gameObject.scene.handle != scene.handle ||
    props.gameObject.scene.handle != scene.handle)
{
    throw new InvalidOperationException("Invalid target hierarchy.");
}
```

This prevents an additively loaded scene with the same root name from receiving the mutations.

### 3. Pin the copied route as a logical signature

Validate each direct road child before touching generated props. The signature should include:

- object name;
- local position, rotation, and scale with a small numeric tolerance;
- `PrefabInstanceStatus.Connected`;
- the exact source prefab asset path.

Checking only the child count permits a moved, rotated, scaled, or disconnected stand-in road to pass while all coordinate-based dressing becomes misaligned.

### 4. Preflight every prefab and transform input

Build one required-prefab set that includes both generated placement specs and copied props that will be repositioned. Load all assets and validate palette offsets, yaw, height, spec coordinates, and final scale components before clearing the old generated set.

```csharp
static void ValidateScale(string prefabPath, Vector3 scale)
{
    bool invalid =
        float.IsNaN(scale.x) || float.IsInfinity(scale.x) ||
        float.IsNaN(scale.y) || float.IsInfinity(scale.y) ||
        float.IsNaN(scale.z) || float.IsInfinity(scale.z) ||
        Mathf.Abs(scale.x) <= 0.0001f ||
        Mathf.Abs(scale.y) <= 0.0001f ||
        Mathf.Abs(scale.z) <= 0.0001f;

    if (invalid)
        throw new InvalidOperationException($"Invalid scale for {prefabPath}: {scale}");
}
```

Preflight does not make the whole operation transactional, but it removes the common case where missing or invalid input is discovered only after existing output has been destroyed.

### 5. Give generated objects an ownership prefix

Only delete direct children whose names start with a generator-owned prefix such as `Prop_Layout2_`. Preserve copied/manual props and move the small number of intentional exceptions by exact identity.

Instantiate with `PrefabUtility.InstantiatePrefab` and reuse the map tool's position, rotation, and scale composition helpers. This preserves authored root-axis correction and scale instead of flattening every prefab to a generic transform.

### 6. Validate the live scene graph before saving

For every generated direct child, assert:

- its label maps to exactly one placement spec;
- its connected source prefab path matches that spec;
- its position, rotation, and scale match the calculated transform;
- its name is unique;
- non-exempt renderer bounds do not intersect any clear-lane envelope.

Keep the overlap exemption list test-owned. If a production spec silently marks a large prop as exempt, a test derived from the same flag cannot catch the regression. Pin representative points inside all lane rectangles as well.

Abort before saving when any lane warning exists. An unsaved dirty scene is visible and recoverable by rerunning the prefix-based generator; a silently saved blocked route is harder to detect.

### 7. Prove logical idempotence

Run the generator twice and compare a sorted logical signature of the generated and intentionally moved objects:

```text
name + prefab path + position + rotation + scale
```

Do not require identical scene bytes after regeneration. Unity may assign different internal file IDs even when the logical layout is identical.

For this layout the final contract was:

```text
Roads preserved: 19
Generated props: 126
Total direct Props children: 173
Lane warnings: 0
Second-run logical signature: identical
Original source SHA-256: unchanged
```

### 8. Treat YAML and previews as secondary evidence

Prefer inspecting the live scene graph with `PrefabUtility`. When an offline YAML test is useful, remember that a renamed prefab instance commonly serializes like this:

```yaml
m_Modifications:
  - propertyPath: m_Name
    value: Prop_Layout2_Upper_Display_00_X...
```

A resilient count handles both native objects and prefab overrides:

```csharp
Regex.Matches(sceneYaml, @"(?:m_Name:|value:) Prop_Layout2_").Count
```

This regex is suitable for a count guard, not for proving prefab provenance or transforms.

Delete the previous report and previews immediately before mutation. Capture new previews only after the scene saves; if capture fails, remove the partial pair and report the failure. A preview is useful for human composition review, but it is not authoritative completion evidence.

### 9. Split compilation from menu execution

After adding or changing an Editor script:

1. refresh/recompile and wait for completion;
2. inspect compilation errors;
3. invoke the menu item separately;
4. if the combined operation timed out after compilation, retry only the idempotent menu command;
5. verify counts, report contents, scene cleanliness, and the source hash rather than trusting the connector response alone.

## Why This Matters

The most expensive failure is not a compile error; it is a plausible-looking derived scene that modified the wrong asset, lost manual work, or contains disconnected and misaligned objects. Exact path, hierarchy, route, prefab, transform, and lane invariants turn those silent failures into early errors.

Live-object validation also separates authoritative state from Unity's serialization details and temporary screenshots. The result can be regenerated by another agent, reviewed without UI automation, and checked with concrete evidence.

## When to Apply

- Creating a sibling or versioned Unity scene from an actively authored map-tool scene.
- Mixing generated dressing with manual objects under the same hierarchy.
- Placing modular roads, water, railings, or other assets whose direction and bounds must join cleanly.
- Driving Unity Editor generation through MCP, CLI, or menu commands after script reloads.
- Writing tests against saved Unity scenes containing prefab instances.

## Examples

The final two east water tiles originally used the same effective orientation, so their long sides ran north-south and left a gap from the existing row. Comparing adjacent final yaw values and renderer extents showed that the new tiles needed alternating spec yaw values so their final rotations matched the neighboring rows. Rebuilding then produced continuous horizontal bounds.

The original generated-count test failed with `Expected: 126, But was: 0`. The scene was correct; the parser was not. Updating the count guard to accept prefab override `value:` fields fixed the false failure, while a separate scene test verified each connected prefab path and transform.

## Related

- [Run Unity scene generation after editor reload](../workflow-issues/run-unity-scene-generation-through-cli-connector-exec-when-editor-reload-is-stale-2026-06-21.md)
- [Read Unity scene YAML positions by object kind](../test-failures/read-unity-scene-yaml-positions-by-object-kind-2026-05-25.md)
- [Preserve MeshyAI prefab axis correction](../workflow-issues/preserve-meshyai-prefab-axis-correction-in-scene-builders-2026-05-25.md)
- [Keep generated map-tool layouts inside work-grid bounds](../developer-experience/keep-generated-map-tool-layouts-inside-work-grid-bounds-2026-06-21.md)
- [Prefer stage prefabs over fake helpers](../design-patterns/prefer-stage-prefabs-over-fake-helpers-for-reference-matching-unity-scenes-2026-05-26.md)
- [Keep generated dressing outside the runner lane](../design-patterns/keep-unity-generated-set-dressing-outside-runner-lane-2026-05-26.md)
- [Preserve prefab transforms in Noryangjin map-tool placement](../logic-errors/preserve-prefab-transform-in-noryangjin-map-tool-placement-2026-06-02.md)
