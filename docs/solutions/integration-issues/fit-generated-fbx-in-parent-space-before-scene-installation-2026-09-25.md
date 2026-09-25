---
title: Fit generated FBX dimensions in parent space before scene installation
date: 2026-09-25
category: integration-issues
module: Rest-stop production Unity importer
problem_type: integration_issue
component: tooling
severity: high
symptoms:
  - "Bathroom partitions extended above the roof after nonuniform FBX resizing."
  - "An arch looked like a solid panel even though its source preview was correct."
  - "A service-area placement based on an empty group origin appeared in distant scenery."
root_cause: logic_error
resolution_type: code_fix
tags: [unity, fbx, trellis, transforms, bounds, material-slots, native-validation]
---

# Fit generated FBX dimensions in parent space before scene installation

## Problem

A reviewed 90-model FBX/GLB library needed installation into two existing Unity scenes. Import succeeded, but the first native assembly renders showed tall partitions and flattened arches. Files that passed Blender review still required a Unity coordinate-space and scene-placement check.

## Symptoms

- The importer measured bounds in the prefab parent's axes, then multiplied the FBX root's local scale by those axis-specific ratios.
- FBX axis conversion made some local axes differ from the measured axes. A target depth therefore enlarged a vertical dimension.
- B10's long horizontal span ran along Z, while the architectural recipe expected X. Matching only three target bounds still stretched the wrong silhouette.
- The existing `Highway_RestStop` group had an unhelpful origin. Children were authored at meaningful world positions; the empty parent's position was not the facility center.

## What Didn't Work

- Assigning `model.localScale *= desired / bounds.size` componentwise could not preserve dimensions when the model root carried a rotation.
- Correcting the scaling space alone did not decide whether a long, thin asset should span X or Z.
- Using the container's world position to place new service details put them in grass, then beside an unrelated tower. Native bounds and rendered views exposed both issues before acceptance.

## Solution

`tools/import-production-unity.cs` keeps the source FBX transform intact and inserts neutral `Fit/Orientation` parents. The orientation parent handles a documented role-specific horizontal alignment. The fit parent receives scaling in the same axes used for measurement, then the evaluated geometry is centered and grounded.

```csharp
var bounds = GeometryBounds(model, prefabRoot);
fit.localScale = new Vector3(
    desired.x / bounds.size.x,
    desired.y / bounds.size.y,
    desired.z / bounds.size.z);
bounds = GeometryBounds(model, prefabRoot);
fit.localPosition -= new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
```

Assert final dimensions and ground height after fitting. Render complete assemblies before installing them. A canopy panel also needs columns and elevation; treating a roof-only asset as an entire six-unit-tall canopy is a semantic assembly error, not a scale conversion.

For placements, inspect actual child geometry and nearby buildings. Use the existing facility's frame and an explicit location, check the new truck against building bounds, and adjust a review camera separately from the authored gameplay camera.

The importer also extracts exact GLB material regions instead of flattening FBX renderers to one material. This retained 99 material regions, PET opacity and the sink exterior's deliberately disconnected normal map. The scenes were reopened and compared against recorded enemy/collider/camera/bonus contracts.

## Why This Works

Axis-specific ratios are valid only in the coordinate system in which the bounds were measured. A neutral parent separates the game's placement/scale convention from the FBX's import correction. Semantic orientation and assembly construction remain explicit decisions, verified with native images.

## Prevention

- Retain failed and corrected native renders (`imported-v1`, `imported-v2`, `imported-v3`) rather than substituting source thumbnails as Unity evidence.
- Validate actual clip target paths and bone movement, not just animation names. The installed library passed 90 mesh checks, 99 material checks and 72 bound moving clips.
- Keep gameplay roots and projectile references when replacing visuals; require exactly one Animator on saved enemy instances.
- Baseline unrelated test failures against the original scenes. Six older UI/workbook assertions failed identically before and after this installation; they were documented rather than rewritten into a false green result.
- Pipeline's `run_script` result has its own nested `success`; an outer CLI success does not prove compilation/execution passed. Dynamically compiled scripts in this project also see duplicate Newtonsoft types; resolve the intended `Newtonsoft.Json` assembly explicitly rather than binding an ambiguous type.

## Related Issues

- [Preserve MeshyAI prefab axis correction](../workflow-issues/preserve-meshyai-prefab-axis-correction-in-scene-builders-2026-05-25.md): related transform principle, but that failure overwrote a rotation during placement rather than applying scale in the wrong axes.
- [Rebuild enemy prefabs without stale scene rigs](rebuild-enemy-prefabs-without-stale-scene-rigs-2026-09-23.md).
- [Installation and evidence](../../../map-concepts/reststop-scene-integration-2026-09-25/README.md).

Captured with `ce-compound mode:headless`, sequentially per AGENTS.md. Session history was skipped; GitHub issue search was unavailable because `gh` and a GitHub connector were absent. AGENTS.md already documents discovery of this knowledge store. The older axis-correction guidance remains valid; no broad refresh was performed.
