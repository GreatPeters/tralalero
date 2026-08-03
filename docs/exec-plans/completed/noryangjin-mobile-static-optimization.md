# Noryangjin Mobile and Static Optimization

## Goal

Reduce the Noryangjin maps' mobile rendering and build cost without marking
enemies, triggers, route controls, or other runtime objects static and without
changing the reusable source water prefab.

## Completed

- Added idempotent optimization commands for the active Noryangjin scene and
  for the saved Map 1 plus Map 2 pair.
- Marked only safe environment mesh leaves `Batching Static`: 184 renderers in
  Map 1 and 653 in Map 2. Existing unrelated static flags are preserved.
- Excluded semantic enemy roots and roots containing runtime behavior,
  animation, physics, skinned rendering, effects, lights, navigation, or
  timeline components. Stale batching flags are removed from those roots.
- Tagged the work floor, work grid, and origin post `EditorOnly` so authoring
  guides are stripped from player builds.
- Disabled post-processing and the URP depth/opaque texture copies on
  `MapTool_Camera`.
- Replaced 118 Map 1 ocean instances and 196 Map 2 water tiles with
  two-triangle scene overrides. The source prefab, source model mesh, and
  source material remain unchanged.
- Applied a low-cost water rendering policy that removes unnecessary shadows,
  probes, motion vectors, dynamic occlusion hints, and non-trigger collision.
- Applied Android overrides to 96 used Stage01 Noryangjin textures: 48 use a
  1024 maximum and 48 auxiliary maps use a 512 maximum. Mipmaps, streaming
  mipmaps, compressed automatic Android format, and Mobile quality texture
  streaming are enabled.
- Connected map-tool placement, duplication, and paste flows to the same safe
  per-root classification so newly authored content does not require a broad
  static flag on a parent hierarchy.

## Measured Result

| Scene | Before | After | Reduction |
| --- | ---: | ---: | ---: |
| Map 1 active environment triangles | 1,487,389 | 435,419 | 70.7% |
| Map 2 active environment triangles | 3,026,495 | 1,935,637 | 36.0% |

These measurements cover the active environment meshes in the authored
scenes. They do not claim a device-specific frame-rate improvement.

The same 1080x1920 `MapTool_Camera` editor render changed from 92 to 59
batches/draw calls and from 736,505 to 183,711 visible triangles. This is a
controlled before/after signal, not an Android frame-rate claim.

The same 1080x1920 Map 1 editor-camera render changed from 92 to 59 batches,
92 to 59 draw calls, 736,505 to 183,711 visible triangles, 542,121 to 186,793
vertices, and 23 to 22 SetPass calls. These are controlled editor comparison
numbers, not Android-device guarantees.

## Editor Workflow

- Active scene with Undo, no automatic save:
  `Tools/맵 제작 도구/노량진 맵 제작/최적화/현재 씬 모바일 최적화`
- Both authored scenes, with automatic save:
  `Tools/맵 제작 도구/노량진 맵 제작/최적화/맵 1·2 모바일 최적화`

The two-scene command refuses to run over an already dirty target scene so it
cannot silently save unrelated authoring changes.

## Verification

- `NoryangjinMapStaticOptimizerTests` covers safe classification, dynamic
  exclusions, idempotence, camera overrides, water renderer policy, source
  prefab integrity, Android texture budgets, and the real Map 1/Map 2 scene
  contracts.
- `MapProductionToolMenuTests` protects both menu paths.
- Repository verification commands:

```powershell
dotnet build Assembly-CSharp.csproj -nologo
dotnet build Assembly-CSharp-Editor.csproj -nologo
powershell -ExecutionPolicy Bypass -File tools/validate-agent-harness.ps1
```

Verification on 2026-07-30:

- `NoryangjinMapStaticOptimizerTests`: 11/11 passed.
- `NoryangjinMapToolMode2SceneTests`: 2/2 passed.
- `MapProductionToolMenuTests`: 2/2 passed.
- Full EditMode: 395/401 passed. The six remaining failures are the existing
  gameplay-integration null assertion and five stale map-tool palette/visual
  expectations.
- Both C# builds completed with zero warnings and zero errors.
- `tools/validate-agent-harness.ps1`: passed.

Final verification on 2026-07-30:

- `NoryangjinMapStaticOptimizerTests`: 11/11 passed.
- `NoryangjinMapToolMode2SceneTests`: 2/2 passed.
- `MapProductionToolMenuTests`: 2/2 passed.
- Full EditMode suite: 395/401 passed. The remaining six failures match the
  pre-existing gameplay integration null and stale map-tool palette/visual
  expectations; this optimization introduced no new full-suite failure.
- Both C# projects built with zero warnings and zero errors.
- `tools/validate-agent-harness.ps1` passed.

## Remaining Device Validation

Map 2 remains excluded from Build Settings and is still a static authored
reference scene. Static batching can trade draw-call reduction for additional
mesh memory, while texture streaming behavior depends on the target device and
camera path. Before release, profile representative low-end Android hardware
for batches/SetPass calls, main and render thread frame time, static batching
memory, texture residency, and peak memory.
