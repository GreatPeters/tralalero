---
title: Automate Unity UI Sprite Atlases with a One-Click Optimizer
date: 2026-08-26
category: docs/solutions/developer-experience
module: Unity Noryangjin UI optimization tooling
problem_type: developer_experience
component: tooling
severity: medium
related_components:
  - development_workflow
  - testing_framework
applies_when:
  - "A Unity UI has recurring draw-call or active-texture regressions as Sprites are added"
  - "Sprite Atlas V2 membership must be regenerated from actual production dependencies"
  - "An optimization workflow must be repeatable, measurable, and safe to run more than once"
root_cause: missing_tooling
resolution_type: tooling_addition
tags: [unity, sprite-atlas-v2, ui-optimization, draw-calls, editor-tooling, dependency-scan, idempotence, mobile]
---

# Automate Unity UI Sprite Atlases with a One-Click Optimizer

## Context

The Noryangjin production UI used individual Sprite textures with Sprite Packer
disabled and no runtime Sprite Atlases. At the fixed 1080x2340 pre-start view,
the controlled baseline was 67 draw calls, 35 SetPass calls, and 11 active UI
textures.

A one-time atlas setup would become stale whenever a Sprite was added, moved,
or removed. The durable requirement was one safe Editor operation that discovers
the current production dependencies, updates only the assets it owns, and proves
that authored scenes, Sprite references, and rendering quality were not changed.

## Guidance

### Rescan production dependencies instead of maintaining a list

`MobileUiOptimizerWindow` scans the saved production scene with
`AssetDatabase.GetDependencies` on every click. It keeps exact individual
Texture2D assets whose importer type is Sprite. Folder packables were rejected
because they silently absorb unused UI-kit variants and unrelated future files.

The tool groups compatible sources by co-usage:

- `HUD_Common`
- `Lobby_Setting_Menu`
- `Upgrade`

It excludes Editor reference images, RawImage/TMP textures, non-Sprite assets,
known full-screen backgrounds, and sources larger than 1024 pixels. The owned
output is limited to `Assets/ShooterSurvival/UI/Atlases/`.

### Encode the complete Atlas V2 contract

Every invocation checks and restores:

```text
Sprite Atlas mode: V2
Padding: 4
Rotation: disabled
Tight packing: disabled
Mipmaps: disabled
Filter: Bilinear
Maximum size: 2048
Android override: compressed, quality 50
Include in build: enabled
```

`ProjectSettings/EditorSettings.asset` stores Sprite Atlas V2 as the repository
default because assigning `EditorSettings.spritePackerMode` does not necessarily
flush the YAML immediately. The button still enforces V2 for the current Editor
session.

### Make safety and idempotence measurable

The operation records the scene hash and missing Sprite-reference count before
and after generation. It rejects new missing references, verifies every owned
atlas and importer, and assesses a second run. A successful second invocation
must return `changed: false` and `idempotent: 1`.

The local report is written to
`Library/MobileUiOptimizer/latest-report.json`. Focused tests additionally cover
deterministic classification, exact packable uniqueness, scene-hash preservation,
unchanged URP/MSAA settings, Android settings, and second-run no-op behavior.

### Treat Unity import and profiling boundaries as separate evidence

An external `dotnet build` can pass before Unity has imported a newly added
Editor script into its generated project files. Trigger Unity recompilation and
inspect its result before treating the external build as authoritative. The
Unity scripting profile also lacked `Convert.ToHexString`; stable hashes use
`BitConverter.ToString(hash).Replace("-", string.Empty)` instead.

Keep the resolution, scene state, MSAA, and quality level fixed across profiling
runs. Warm the Atlas cache before interpreting steady-state frame time. Editor
allocated memory drifts after imports and is not a shipping residency metric.

## Why This Matters

The retained three-atlas configuration reduced draw calls from 67 to 58, a
13.4% improvement, and active UI textures from 11 to 2. SetPass calls moved from
35 to 34 because atlasing consolidates texture state but cannot remove unrelated
materials or render states.

The tool remains useful after the initial win: newly referenced compatible
Sprites are discovered on the next click, while ownership boundaries and
idempotence prevent routine maintenance from rewriting scenes, prefabs, source
Sprite importers, runtime UI scripts, or rendering assets.

Grouping by actual co-usage matters more than minimizing atlas count. A two-atlas
experiment measured 57 draw calls, but the one-call difference did not exceed
the configured noise threshold and would load transient Upgrade content with the
Lobby atlas. A 512-pixel source cap removed five Sprites without changing any
runtime metric. Both variants were reverted.

## When to Apply

- After adding, replacing, moving, or removing production UI Sprites.
- After changing which UI assets the production scene references.
- Before profiling or producing an Android build.
- When Sprite Packer or Atlas importer settings may have drifted.
- When UI texture count or draw calls increase unexpectedly.

Revisit the grouping and size rules only when profiling shows a meaningful
change in screen co-usage or on-device texture residency. Validate cold loading,
GPU bandwidth, thermal behavior, and resident texture memory on a representative
Android device before tightening mobile budgets.

## Examples

Routine use:

```text
1. Open Tools/Shooter Survival/Optimization/Mobile UI Optimizer.
2. Press UI 아틀라스 최적화 및 검증.
3. Confirm the visual and missing-reference contracts passed.
4. Run it again when verifying maintenance; changed must be false.
5. Read Library/MobileUiOptimizer/latest-report.json for automation evidence.
```

Representative second-run report:

```json
{
  "newMissingSpriteRefs": 0,
  "atlasPages": 3,
  "idempotent": 1,
  "visualContractPassed": 1,
  "spritesPacked": 68,
  "changed": false
}
```

## Related

- [Mobile UI Atlas Optimizer operator guide](../../mobile-ui-atlas-optimizer.md)
- [Scope map optimizations to scene instances](../logic-errors/scope-map-optimizations-to-scene-instances-2026-07-30.md)
- [Migrate legacy player status UI safely](../architecture-patterns/migrate-legacy-player-status-ui-to-screen-space-hud-safely-2026-08-18.md)
- [Bake generated UI previews and isolate EditMode tests](../workflow-issues/bake-generated-prefab-ui-previews-and-isolate-editmode-tests-2026-08-13.md)
- [Match Game View to the Device Simulator preview](match-unity-game-view-to-device-simulator-preview-2026-08-25.md)
