---
title: Batch the legacy FlatKit hull outline instead of interleaving it per object
date: 2026-10-01
category: performance-issues
module: FlatKit outline shader and Mobile/PC URP renderers
problem_type: performance_issue
component: rendering
severity: high
symptoms:
  - "Dense HighWay frames drew 747 batches with 663 SetPass calls (SetPass ~ 0.87 x batches)."
  - "FlatKit/Stylized Surface With Outline reported SRP Batcher incompatibility codes 10, then 19."
  - "Faded (transparent-queue) copies of outlined scenery still drew an opaque outline pass."
root_cause: config_error
resolution_type: code_fix
tags: [unity, urp, srp-batcher, flatkit, outline, setpass, mobile]
---

# Batch the legacy FlatKit hull outline instead of interleaving it per object

## Problem

Most project materials (162) use `FlatKit/Stylized Surface With Outline`. Its outline was a legacy CG pass with no `LightMode`, so URP drew it as `SRPDefaultUnlit` in the same opaque draw call list as `UniversalForwardOnly`: surface, outline, next surface, outline. Each switch is a SetPass. The other passes included `SimpleLitInput.hlsl`, whose `UnityPerMaterial` layout differs from FlatKit's, so the shader was also SRP Batcher incompatible.

## Solution

1. Every pass shares `LibraryUrp/StylizedInput.hlsl`. The initial implementation used `UsePass` for ForwardLit, ShadowCaster, GBuffer, DepthOnly, DepthNormals and Meta. **2026-10-02 correction:** those six pass bodies are now declared locally, synchronized by `tools/sync-legacy-outline-passes.py`. Cross-shader `UsePass` reproduced native 71/75-keyword assertions on Unity 6000.2.6f1 even for a newly constructed material. The unused Universal2D pass remains dropped. See [the startup correction](../runtime-errors/own-flatkit-passes-to-avoid-keyword-space-assertions-2026-10-02.md).
2. The outline is HLSL with the original screen-space expansion (`/ _ScreenParams.xy * _OutlineWidth * distance * 2`, depth offset, fog) and `LightMode = "OutlineLegacy"`.
3. `tools/install-legacy-outline-feature.cs` adds a URP `RenderObjects` feature (opaque queue, AfterRenderingOpaques, pass `OutlineLegacy`) to `Mobile RP` and `PC RP`. All outlines draw together after opaques.
4. Every `UnityPerMaterial` variable must exist in the Properties block. `_DetailMap`, `_DetailMapColor`, `_DetailMapImpact` (default 0, identity in all three blend modes) were added as hidden properties. Code 19 named the first missing one.

Result at the same HighWay moment: SetPass 663 → 100 with identical batches/triangles. Outlines unchanged in captures.

## Checks

- `ShaderUtil.GetSRPBatcherCompatibilityCode` / `GetSRPBatcherCompatibilityIssueReason` (internal, reflection) give the exact reason. Force-reimport the shader before reading; the cached shader reported old passes.
- Compare `UnityStats.setPassCalls` at a fixed run time; `batches` does not drop under the SRP Batcher.
- Do not reuse FlatKit's `Outline` LightMode: the existing "Flat Kit Per Object Outline" feature overrides that pass with another shader and keyword.

## Sharp edges

- A renderer without the feature shows no legacy outlines. Rerun the installer after adding renderers or quality levels.
- Transparent-queue copies are filtered out by the opaque RenderObjects range; `TemporarySceneryFade` also disables `OutlineLegacy` explicitly.
- SRP Batcher compatibility code 0 alone does not prove runtime validity: also construct a fresh material and bind every pass without unexpected logs (`LegacyOutlineShaderTests`).
