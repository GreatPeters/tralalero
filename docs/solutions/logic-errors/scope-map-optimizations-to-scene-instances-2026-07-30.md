---
title: Scope Unity map optimizations to scene instances
date: 2026-07-30
category: logic-errors
module: Unity Noryangjin mobile optimization
problem_type: logic_error
component: tooling
symptoms:
  - "A map-only optimizer rewrote a reusable Meshy ocean prefab used by other scenes."
  - "Depth, opaque-texture, and post-processing overrides were applied to every URP camera."
  - "Water-like roots could lose trigger collision or receive an unintended replacement mesh."
  - "A formerly static object could retain ForceNoMotion after becoming dynamic."
root_cause: scope_issue
resolution_type: code_fix
severity: high
related_components:
  - "testing_framework"
  - "development_workflow"
tags:
  - "unity"
  - "editor-tooling"
  - "scene-instance-overrides"
  - "prefab-integrity"
  - "urp-camera"
  - "optimization-scope"
---

# Scope Unity map optimizations to scene instances

## Problem

The first `NoryangjinMapStaticOptimizer` implementation optimized the two
authored maps by saving changes into the reusable ocean prefab and by applying
camera settings to every URP camera in a target scene. A scene-specific mobile
optimization therefore had a project-wide persistence boundary: recovery and
non-map scenes could inherit the water change, while later overlay or effect
cameras could silently lose rendering features.

## Symptoms

- `PrefabUtility.LoadPrefabContents` plus `SaveAsPrefabAsset` replaced the
  shared ocean prefab's original 8,917-triangle mesh and material.
- Every consumer of that prefab could receive the two-triangle water
  optimization, even outside Map 1 and Map 2.
- The camera loop disabled depth texture, opaque texture, and post-processing
  without distinguishing `MapTool_Camera` from auxiliary cameras.
- Broad water matching treated collision and trigger colliders alike.
- Clearing a stale `BatchingStatic` flag did not restore the renderer policy
  that had been set while the object was static.

## What Didn't Work

Editing the prefab asset looked efficient because all ocean instances changed
at once. It was the wrong ownership boundary: the source prefab is reusable,
whereas this optimization belongs only to two authored scene instances.

Likewise, iterating over all URP cameras was simple but encoded an assumption
that the scenes would always have one camera. An active map could look correct
while an effect or overlay camera was already misconfigured.

Matching any object below `Water` and disabling every collider was also too
broad. A future water trigger or specialized plane would satisfy the heuristic
without being a safe optimization target. Finally, broad
`AssetDatabase.SaveAssets()` calls could persist unrelated dirty assets.

## Solution

Keep the generated low-poly meshes as independent project assets and record
only scene-instance overrides on the known ocean and Map 2 water placements:

```csharp
bool isOcean = IsOceanWater(instance);
Mesh expectedMesh = isOcean
    ? EnsureLowPolyOceanMesh()
    : EnsureLowPolyWaterTileMesh();

filter.sharedMesh = expectedMesh;
PrefabUtility.RecordPrefabInstancePropertyModifications(filter);

if (isOcean)
{
    renderer.sharedMaterial = LoadLowCostWaterMaterial();
    PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
}
```

The ocean quad is generated from the source mesh bounds, so its scene
placements keep their authored footprint. The source prefab remains connected
to the original 8,917-triangle FBX mesh.

Limit camera changes to the explicit map-tool camera contract:

```csharp
if (!string.Equals(
        camera.name,
        "MapTool_Camera",
        StringComparison.Ordinal))
{
    continue;
}

data.requiresDepthOption = CameraOverrideOption.Off;
data.requiresColorOption = CameraOverrideOption.Off;
data.renderPostProcessing = false;
```

Automatic water optimization now recognizes only the known ocean token or the
`Mode2_Water_` prefix. The explicit water helper preserves trigger colliders:

```csharp
foreach (Collider collider in
         instance.GetComponentsInChildren<Collider>(true))
{
    if (!collider.enabled || collider.isTrigger)
        continue;

    collider.enabled = false;
}
```

Static classification is reversible. When a root becomes dynamic, remove only
`BatchingStatic`, preserve every unrelated static flag, and restore
`ForceNoMotion` to `Object`. Generated meshes, the Map 2 water material, and
Quality Settings use `AssetDatabase.SaveAssetIfDirty` instead of a global
asset save.

## Why This Works

Scene-instance overrides put the performance change at the same scope as its
measured benefit. Other prefab consumers keep their source geometry and
material, while Map 1 and Map 2 still receive two-triangle water.

Exact camera identity and narrow water recognition replace structural guesses
with explicit eligibility boundaries. Trigger-aware collision handling
preserves gameplay, and reversible static state prevents an object's renderer
configuration from drifting when its role changes.

The corrected contract is covered by tests that preserve an `EffectCamera`,
keep a trigger collider enabled, and compare the source prefab before and
after optimizing an instance. The optimized scenes retain 118 two-triangle
ocean instances, while the source prefab still reports 8,917 triangles.

## Prevention

- Test protected non-targets as well as optimized targets. Assert source prefab
  identity, secondary-camera settings, and trigger state.
- Prefer scene-instance overrides for scene-specific performance work. Write a
  prefab asset only when the user-facing feature explicitly changes future
  placement defaults.
- Make editor automation fail closed on exact scene paths, object identities,
  or documented prefixes instead of broad hierarchy categories.
- Preserve unrelated static flags and make every policy change reversible.
- Use targeted dirty-asset saves and verify a second optimizer run is a no-op.
- Review prefab, scene, material, and Project Settings diffs separately before
  handing off bulk Unity editor automation.

## Related Issues

- [Scale live map-tool object matches into route plans](../design-patterns/scale-live-map-tool-object-matches-into-route-plans-2026-07-19.md)
- [Safely bake static Unity map scenes before retiring generators](../workflow-issues/safely-bake-static-unity-map-scenes-before-retiring-generators-2026-07-27.md)
- [Prevent Humanoid reimport pose overrides when saving nested Unity prefabs](../workflow-issues/prevent-humanoid-reimport-pose-overrides-when-saving-unity-prefabs-2026-07-27.md)
- [Resolve selected prefab children to Noryangjin map tool placement roots](resolve-selected-prefab-child-to-map-tool-placement-root-2026-06-08.md)
