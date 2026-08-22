---
title: Isolate dedicated MeshyAI assets from the generic repair sweep
date: 2026-08-09
last_updated: 2026-08-10
category: docs/solutions/workflow-issues
module: MeshyAI asset pipeline
problem_type: workflow_issue
component: development_workflow
severity: medium
applies_when:
  - "A Meshy model uses a dedicated sanitizer, importer, and prefab builder"
  - "Imported visuals must avoid inheriting legacy GFX scale or rotation"
  - "Gameplay logic and sibling visuals require one lifetime root"
  - "FlatKit outline materials must work across platform and Quality renderers"
tags: [meshy-ai, unity-assets, asset-import, prefab-generation, lifetime-root, flatkit, urp-renderers, fail-closed]
---

# Isolate dedicated MeshyAI assets from the generic repair sweep

## Context

The Feast of Fortune bonus wall uses two derived FBX models, one shared material, and two gameplay prefabs cloned from a `WallScript` template. Placing those FBXs under `Models/MeshyAI/Gameplay_Walls/FeastOfFortune` accidentally made them eligible for `MeshyAiAssetRepairer.RepairAll`.

The generic repairer assumes one independently repaired asset per model folder. It derives output identity from the containing folder and looks for source textures beside the FBX. Both Feast of Fortune models therefore resolved to the same `FeastOfFortune` material and generic prefab paths, while their actual shared textures lived under `Textures/MeshyAI/Gameplay_Walls`.

## Guidance

Put Meshy models with a dedicated import contract under an underscore-prefixed directory. `MeshyAiAssetRepairer.IsRepairTarget` excludes any relative path component that starts with `_`.

```text
Assets/ShooterSurvival/Models/MeshyAI/
├── Stage01_Noryangjin/          # Generic repair targets
└── _Gameplay_Walls/             # Dedicated builders only
    └── FeastOfFortune/
        ├── FeastOfFortune_Left.fbx
        └── FeastOfFortune_Right.fbx
```

The dedicated builder owns the paired asset contract:

- keep Left and Right as distinct model assets;
- assign one explicit, non-emissive shared material;
- create two separately placeable gameplay wall prefabs;
- preserve the template's `WallScript`, trigger collider, and UGUI hierarchy;
- preserve the imported FBX root's axis-correction transform when nesting it.

Make the source-preparation tool fail closed. A Meshy FBX can include a real
`geometry_0` plus Blender's default `Cube`, `Camera`, and `Light`. Prefer the
known geometry explicitly, permit only explicitly understood helper meshes,
and fail when any unexpected mesh appears. If `geometry_0` is absent, require
exactly one mesh rather than guessing by polygon count. Also resolve the source
and output paths before importing and reject any collision among the source
FBX, cleaned FBX, and extracted texture; otherwise a valid invocation typo can
overwrite the only source artifact.

When two Meshy variants must be compared at the same vertex budget, run the
topology operations in an explicit order: merge coincident vertices, apply the
Decimate modifier, remove loose vertices, and only then measure the final
shortfall. Decimate operates in discrete collapses, so a ratio can land a few
vertices below the requested count. Choose the nearest result that does not
exceed the target and subdivide individual long edges only for a tightly
bounded shortfall (this importer permits at most 32 vertices). Fail if the
result is above the target or too far below it instead of silently publishing
unequal comparison assets.

Keep each transformation stage inside its own function and patch it with
function-scoped context. An ambiguous edit after a repeated
`source.data.update()` can accidentally move the Decimate modifier block into
the target-matching function, leaving conversion outputs unwritten. Re-run the
complete conversion after any tool edit, then re-import the generated FBX in
Blender and assert the exact vertex count, mesh count, face count, and absence
of authoring cameras or lights.

For a replacement visual, inspect the template transform before choosing its
parent. The legacy `GFX` object can carry scale or rotation intended for the
old mesh. Keep the imported model as a direct prefab-root child when inheriting
that transform would distort it, while retaining the existing `WallScript`,
trigger collider, and UGUI on the legacy hierarchy.

For fixed-value gameplay walls, treat the generated prefab root as the lifetime object. The template `GFX` child can own `WallScript`, its trigger collider, and UGUI while the imported Meshy visual remains a sibling under the same root. Put the authored-value marker on that root and resolve it from `WallScript` with `GetComponentInParent`.

Keep serialized gameplay ownership single-sourced:

- `WallScript.buffType` owns which stat the wall changes;
- the root marker stores only the exact numeric value;
- `SetRandomStat` skips randomization whenever the root marker exists;
- movement, out-of-bounds destruction, and collection target the marked root so the gameplay child and sibling visual stay synchronized.

Disabling or moving only `WallScript.gameObject` is incorrect for this hierarchy. It can leave a collected Meshy model visible or move the collider away from its rendered wall.

The reset path must use the same lifetime boundary. After collection has
deactivated the root, a parent lookup that excludes inactive objects can no
longer find it, and activating only the child cannot reactivate its parent.
Resolve `BonusWallLifetimeRoot` with `includeInactive: true` and have
`WallManager` reactivate the complete lifetime object before rerolling stats
and refreshing UI.

FlatKit outline setup is also a renderer contract, not just a material
contract. Register the wall material with an active
`ObjectOutlineRendererFeature` in every renderer data asset reachable through
Graphics and Quality settings. In this project Android and the Graphics
default resolve through `[FlatKit] Example URP Asset` to
`[FlatKit] Example Renderer.asset`, while standalone resolves to `PC RP.asset`.
Checking renderer names by intuition instead of following those GUID references
makes Android silently lose the outline.

When moving an already imported model into the excluded directory, move its `.meta` file with it or use `AssetDatabase.MoveAsset`. Preserving the GUID keeps existing prefab model references intact.

## Why This Matters

A later generic repair pass can otherwise process both FBXs with the same folder identity. Because it cannot find the dedicated texture set beside either model, it reconfigures the shared material as though normal and metallic maps were absent. It also repeatedly writes the same generic Meshy prefab path, leaving an unrelated last-model-wins artifact. The two gameplay prefabs may still exist, but both render through the modified shared material.

The underscore boundary makes asset ownership visible in the filesystem and keeps future whole-project repair runs from mutating specialized gameplay assets.

## When to Apply

- One Meshy export is split into Left/Right or other derived variants.
- Several models share one material or texture set.
- Folder name alone cannot provide a unique output identity for each model.
- A model is embedded into a gameplay prefab with scripts, colliders, or UI.
- The dedicated builder uses different emission, texture, or import policies from the generic repairer.
- A composite wall is deactivated on collection and reactivated for a later stage.
- The project selects different URP renderer data for mobile and standalone quality tiers.
- Old/new comparison variants must share one exact topology budget without sharing model, texture, material, or prefab GUIDs.

Keep using the generic repairer for its intended one-folder, one-asset, co-located-texture structure.

## Examples

Tests for a paired gameplay asset should verify that the two wall prefabs reference different FBX GUIDs, share the intended material, retain `WallScript`, its UGUI references, and a trigger collider, and leave `_EMISSION` disabled. For fixed bonus walls, also verify that the authored marker is on the prefab root, `WallScript` remains the sole owner of `buffType`, randomization is disabled, exact left/right values survive builder regeneration, and consuming the wall disables the complete root. A source-splitting tool should also fail when its split plane crosses faces instead of silently creating torn halves.

For a whole-scene Meshy export such as `Wall_Special`, verify that the cleaned
model contains exactly one `MeshFilter` and no `Camera` or `Light`, the visual
is a direct root sibling rather than a child of `GFX`, inactive-parent lifetime
lookup and reactivation resolve the complete root, and both renderer data assets
actually reachable from Mobile and PC Quality settings register the FlatKit
material.

For an old/new quality comparison, also assert that each generated FBX imports
at the exact requested vertex count, has comparable bounds, and owns distinct
model, texture, material, and prefab GUIDs. Register both outlined materials in
every reachable renderer data asset so the comparison does not change with the
active quality tier.

## Related

- [Repair Unity assets when editor command execution is blocked](repair-unity-assets-when-editor-command-path-is-blocked-2026-05-24.md)
- [Verify L-shape mesh crop intent before export](verify-l-shape-mesh-crop-intent-before-export-2026-06-12.md)
- [Preserve MeshyAI prefab axis correction in scene builders](preserve-meshyai-prefab-axis-correction-in-scene-builders-2026-05-25.md)
- [Preserve prefab transform in Noryangjin map-tool placement](../logic-errors/preserve-prefab-transform-in-noryangjin-map-tool-placement-2026-06-02.md)
- [Verify Unity material keywords after bulk outline conversions](verify-unity-material-keywords-after-bulk-outline-conversions-2026-07-02.md)
- [Reapply external prefab YAML after Unity script recompilation](reapply-prefab-yaml-after-unity-script-recompile-2026-08-03.md)
