---
title: Safely Bake Static Unity Map Scenes Before Retiring Generators
date: 2026-07-27
category: docs/solutions/workflow-issues
module: Unity Noryangjin map tooling
problem_type: workflow_issue
component: development_workflow
severity: medium
applies_when:
  - "Baking a large authored Unity scene before removing its one-shot generators"
  - "Running dynamically compiled C# from PowerShell during scene production"
  - "Generating connected roads, constrained prop layouts, or atlas-backed materials"
  - "A source scene must remain byte-for-byte unchanged while a sibling scene is finalized"
symptoms:
  - "Dynamic C# source is serialized as an object instead of a string by a PowerShell connector call"
  - "Road counts pass while a pivot or endpoint leaves a real connection gap"
  - "A constrained placement pass cannot find a valid dock slot before the scene is saved"
  - "A plane using a full Meshy atlas repeats unrelated quadrants instead of continuous water"
root_cause: missing_workflow_step
resolution_type: workflow_improvement
tags:
  - "unity"
  - "static-scene-baking"
  - "powershell"
  - "geometry-validation"
  - "placement-validation"
  - "material-uvs"
  - "scene-testing"
  - "map-production"
---

# Safely Bake Static Unity Map Scenes Before Retiring Generators

## Context

A protected Unity map scene had to remain byte-for-byte unchanged while a sibling was finalized as a static authored scene. The sibling needed exact route and prop budgets, preserved gameplay composition, navigable markets, continuous water, a connected highway handoff, and no Build Settings entry.

Persistent generators were the wrong ownership model after the layout was approved. Temporary one-shot editor code was still useful for the large placement pass, but the result—not the generator—was the deliverable. Several failures showed that object counts and a plausible screenshot were not enough:

- PowerShell `Get-Content -Raw` source reached a connector as an extended object rather than a plain string.
- A highway deck looked connected but its asymmetric pivot left a physical seam.
- Placing small market props first left no balanced, collision-free slots for large buildings.
- The water plane rendered multiple unrelated portions of its Meshy texture atlas.
- An invalid placement pass had to abort without overwriting the last valid scene.

## Guidance

### Protect the source and target separately

Pin the protected scene by asset GUID and a reviewed SHA-256 value. Permit bulk mutation only for the exact sibling path, and restore the previous active scene when the operation finishes.

```csharp
const string Map1 =
    "Assets/ShooterSurvival/Scenes/Tools/Noryangjin_MapTool_Mode.unity";
const string Map2 =
    "Assets/ShooterSurvival/Scenes/Tools/Noryangjin_MapTool_Mode_2.unity";

if (!string.Equals(targetPath, Map2, StringComparison.Ordinal))
    throw new InvalidOperationException($"Refusing to modify {targetPath}");
```

Do not derive the expected source hash from the file under test. A hash computed and asserted in the same run only proves that the file is self-consistent, not unchanged.

### Treat the bake as a transaction

Perform all placements in memory, validate the finished hierarchy and rendered geometry, then save once. If a placement or validation fails, close or reload the sibling without saving and restore the prior active scene.

```csharp
BuildRoads(map2);
BuildMarkets(map2);
BuildWater(map2);
JoinHighway(map2);

ValidateRoadCount(map2, 150);
ValidatePropCount(map2, 511);
ValidateRouteAndBranches(map2);
ValidateMarketsAndQuays(map2);
ValidateWaterCoverage(map2);
ValidateHighwayContact(map2);

if (!EditorSceneManager.SaveScene(map2, Map2))
    throw new InvalidOperationException("Map 2 save failed.");
```

A failed “no dock slot available” pass is safe when it throws before `SaveScene`. It is not safe if partial changes are saved and later mistaken for a valid authored layout.

### Normalize dynamically compiled source at the connector boundary

When PowerShell-loaded C# is serialized through a connector, force it back to a plain `System.String` before building the request payload:

```powershell
$source = Get-Content -LiteralPath $oneShotScript -Raw
$source = [string]::Copy($source)
```

This is a boundary fix, not a reason to retain the script. Delete the temporary `.cs` and `.meta` after the successful bake.

### Allocate constrained geometry from largest to smallest

Reserve large market buildings before tanks and small props. Use the combined bounds of every child renderer instead of transform pivots or assumed prefab sizes:

```csharp
static Bounds RendererBounds(GameObject root)
{
    Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
    if (renderers.Length == 0)
        throw new InvalidOperationException($"{root.name} has no renderers.");

    Bounds bounds = renderers[0].bounds;
    for (int index = 1; index < renderers.Length; index++)
        bounds.Encapsulate(renderers[index].bounds);
    return bounds;
}
```

For the Noryangjin sibling, the large-building budget was reserved as `6 / 6 / 5 / 5` across four quay clusters. Every later candidate was rejected if its bounds overlapped another market object, missed its quay, or crossed the required route clearance.

### Validate joins by positive bounds overlap

Asymmetric imported pivots make “positioned at the endpoint” unreliable. Require a small, explicit overlap along the join axis:

```csharp
float overlap = routeEndBounds.max.x - highwayBounds.min.x;
Assert.That(overlap, Is.GreaterThanOrEqualTo(0.25f));
```

The same principle applies to road turns, quays, water rows, tunnels, and other modular seams.

### Crop every populated atlas channel consistently

Clone a scene-specific material instead of changing the shared source material. Apply the same scale and offset to every populated channel used by the shader:

```csharp
Vector2 scale = new(0.5f, 0.5f);
Vector2 offset = new(0.5f, 0f);

foreach (string property in new[]
{
    "_BaseMap",
    "_MainTex",
    "_BumpMap",
    "_EmissionMap",
    "_MetallicGlossMap"
})
{
    if (!map2Water.HasProperty(property))
        continue;

    map2Water.SetTextureScale(property, scale);
    map2Water.SetTextureOffset(property, offset);
}
```

Preserve the new material's `.meta` and GUID with the scene. A color channel that looks correct can still hide mismatched normal, emission, or metallic atlas sampling.

### Replace generator tests with static-scene contract tests

Once the bake passes, remove the persistent builders, their `.meta` files, their regeneration menus, and tests whose only contract is “the builder can rebuild.” Keep the ordinary prefab-aware map-tool editing path.

The replacement test should open scenes additively, restore the previous active/dirty state in `finally`, and assert:

- protected source GUID and SHA-256;
- copied prefab identity and transforms;
- exact route, branch, turn-spot, and prop budgets;
- renderer-bounds market collision, clearance, and quay contact;
- water material identity, atlas transform, overlap, and exterior margin;
- highway contact;
- target exclusion from Build Settings;
- absence of stale generator menu entry points.

## Why This Matters

Large Unity YAML is too noisy for reliable object-by-object review. A scene may have the correct child counts while containing a disconnected turn, an obstructed lane, an unbalanced market, an atlas artifact, or an accidental runtime entry.

Hash protection catches source-scene mutation. Save-after-validation prevents partial layouts from replacing a known-good sibling. Renderer-bounds checks measure the geometry Unity actually displays instead of assumptions about pivots. A scene-specific material avoids changing Map 1 or unrelated prefabs. Retiring the generators then prevents an obsolete menu command or reload hook from silently replacing the approved static artifact.

## When to Apply

- A large authored scene is derived once from a protected source scene.
- Bulk placement is useful during construction but regeneration is not a supported product feature.
- Visual correctness depends on imported pivots, renderer extents, clearances, or texture-atlas regions.
- The derived scene must remain editable through normal prefab-aware tools.
- A scene should stay outside Build Settings until its runtime contract is approved.

Do not use this pattern when procedural regeneration is itself a supported requirement. In that case, retain an idempotent generator, version its input contract, and test repeated generation explicitly.

## Examples

The final Noryangjin contract preserved the current Map 1 prefix and then verified:

```text
Roads: 24 copied + 118 main + 8 branches = 150
Props: 162 copied + 349 authored = 511
Turn spots: 2 copied + 6 authored = 8
Large market buildings: 6 / 6 / 5 / 5
Market overlaps: 0
Highway overlap: 0.25 units
Water exterior margin: at least 70 units
Build Settings: Map 2 absent
Map 1 SHA-256: unchanged
```

The targeted EditMode filters were:

```text
NoryangjinMapToolMode2SceneTests
MapProductionToolMenuTests
```

## Related

- [Generate Unity Map-Tool Sibling Scenes with Fail-Closed Verification](generate-unity-map-tool-sibling-scenes-fail-closed-2026-07-15.md)
- [Keep Unity Map Production Menus and Automation Paths in Sync](keep-unity-map-production-menu-workflows-and-automation-paths-in-sync-2026-07-27.md)
- [Protect Active Unity Scenes from Broad EditMode Test Runs](protect-active-unity-scenes-from-broad-editmode-test-runs-2026-07-18.md)
- [Scale Live Map-Tool Object Matches Into Route Plans](../design-patterns/scale-live-map-tool-object-matches-into-route-plans-2026-07-19.md)
- [Stamp Copied Map-Tool Objects at Arbitrary Tiles](../design-patterns/stamp-copied-map-tool-objects-at-arbitrary-tiles-2026-07-27.md)
- [Noryangjin Map 2 static authored-scene contract](../../noryangjin-map2-authored-scene.md)
