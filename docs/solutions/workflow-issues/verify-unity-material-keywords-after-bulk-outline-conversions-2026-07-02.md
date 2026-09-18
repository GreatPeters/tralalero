---
title: Verify Unity Material Keywords After Bulk Outline Conversions
date: 2026-07-02
last_updated: 2026-09-17
category: docs/solutions/workflow-issues
module: Unity Noryangjin map tooling
problem_type: workflow_issue
component: tooling
severity: low
applies_when:
  - "Bulk-converting Unity .mat assets to FlatKit Stylized Surface"
  - "Adding outline settings to many MeshyAI map-tool materials"
  - "Editing Unity material YAML outside the Inspector"
tags: [unity, material-yaml, flatkit, shader-keywords, noryangjin]
---

# Verify Unity Material Keywords After Bulk Outline Conversions

## Context
Noryangjin map-tool props needed consistent FlatKit outlines. The fast path was a mechanical YAML update across many `Assets/ShooterSurvival/Materials/MeshyAI/Stage01_Noryangjin` materials: switch to `FlatKit/Stylized Surface`, enable `DR_OUTLINE_ON`, and set the reference outline values from the seafood display stall.

The first pass proved that matching float properties is not enough. A material can have `_OutlineEnabled: 1` and `_OutlineWidth: 2` while the shader keyword state is still wrong or contradictory. A later standalone-wall integration exposed a second gap: even correct material state does not schedule FlatKit's `Outline` pass when the active URP Renderer Data lacks `ObjectOutlineRendererFeature`.

## Guidance
For bulk FlatKit outline conversions, verify four layers together:

1. The material uses the current `FlatKit/Stylized Surface` shader. Do not fall back to `FlatKit/Stylized Surface With Outline`; FlatKit marks it as deprecated on Unity 2022.3 and newer.
2. Shader keywords and outline float properties agree, with no keyword present in both the valid and invalid lists.
3. Every URP Renderer Data asset reachable through Graphics, platform, or Quality settings contains an active `ObjectOutlineRendererFeature`.
4. A representative camera or Inspector check confirms that Unity imported the serialized state and actually draws the outline.

The material-side contract includes:

```yaml
m_Shader: {fileID: 4800000, guid: bee44b4a58655ee4cbff107302a3e131, type: 3}
m_ValidKeywords:
  - DR_CEL_EXTRA_ON
  - DR_OUTLINE_ON
  - _CELPRIMARYMODE_SINGLE
  - _DETAILMAPBLENDINGMODE_MULTIPLY
  - _GRADIENTSPACE_WORLD
  - _OUTLINESPACE_SCREEN
  - _TEXTUREBLENDINGMODE_MULTIPLY
m_InvalidKeywords:
  - _UNITYSHADOWMODE_NONE
```

Then verify the outline floats:

```yaml
- _OutlineEnabled: 1
- _OutlineWidth: 2
- _OutlineDepthOffset: 0.005
- _OutlineScale: 1
- _OutlineSpace: 0
- _CameraDistanceImpact: 0.2
```

When using text replacement, check that a keyword does not remain in both `m_ValidKeywords` and `m_InvalidKeywords`. After writing the YAML, run `Assets/Refresh` through Unity and sample at least one changed material with `get_material_info` or the Inspector so the editor view agrees with the file.

On Unity 6, keep the legacy `SRPDEFAULTUNLIT` material pass disabled and install the renderer feature idempotently. Search the target renderer's feature list before creating a sub-asset, activate the existing or new feature, and register the material when automatic material tracking is enabled:

```csharp
ObjectOutlineRendererFeature outlineFeature = null;
foreach (var rendererFeature in rendererData.rendererFeatures)
{
    if (rendererFeature is ObjectOutlineRendererFeature candidate)
    {
        outlineFeature = candidate;
        break;
    }
}

if (outlineFeature == null)
{
    outlineFeature = ScriptableObject.CreateInstance<ObjectOutlineRendererFeature>();
    outlineFeature.name = "Flat Kit Per Object Outline";
    outlineFeature.Create();
    AssetDatabase.AddObjectToAsset(outlineFeature, rendererData);
    rendererData.rendererFeatures.Add(outlineFeature);
}

outlineFeature.SetActive(true);
outlineFeature.autoReferenceMaterials = true;
outlineFeature.RegisterMaterial(material, true);
EditorUtility.SetDirty(outlineFeature);
EditorUtility.SetDirty(rendererData);
```

## Why This Matters
FlatKit's outline variant is keyword-driven, but URP still needs its Renderer Feature to schedule that pass. The YAML can look visually close to the reference material while Unity selects the wrong variant because the keyword list is stale, or while Unity never draws the correct pass because the active quality renderer omits the feature. Material-only tests therefore provide false confidence across platform or quality switches.

## When to Apply
- Converting a known folder of generated MeshyAI materials from URP Lit to FlatKit.
- Normalizing a map-tool palette so future prefab placements inherit consistent outlines.
- Repairing `.mat` files after Unity or an MCP helper updates properties but does not preserve shader keywords.
- Adding or changing platform-specific URP assets, Quality tiers, or Renderer Data.
- Building outlined materials through an idempotent editor asset pipeline.

## Examples
Repository-side verification can stay simple and folder-scoped:

```powershell
$targets = Get-ChildItem Assets\ShooterSurvival\Materials\MeshyAI\Stage01_Noryangjin -Recurse -Filter *.mat |
  Where-Object { $_.FullName -notmatch '\\_old\\' -and $_.FullName -notmatch '_ROAD_' }

foreach ($file in $targets) {
  $text = Get-Content -Raw -LiteralPath $file.FullName
  foreach ($check in @(
    'guid: bee44b4a58655ee4cbff107302a3e131',
    '- DR_OUTLINE_ON',
    '- _OutlineEnabled: 1',
    '- _OutlineWidth: 2',
    '- _OutlineDepthOffset: 0.005',
    '- _CameraDistanceImpact: 0.2'
  )) {
    if ($text -notlike "*$check*") { "$($file.Name): missing $check" }
  }
}
```

Also check the editor's view for representative files:

```text
Material: 001_STAGE01_NRY_PROPS_001_Blue_fish_crate
Shader: FlatKit/Stylized Surface
_OutlineEnabled: 1
_OutlineWidth: 2
_OutlineDepthOffset: 0.005
_CameraDistanceImpact: 0.2
```

Test the renderer contract separately from the material contract. The Noryangjin bonus-wall test loads every sub-asset from the FlatKit example renderer plus `Assets/Settings/Mobile RP.asset` and `Assets/Settings/PC RP.asset`, finds an active `ObjectOutlineRendererFeature`, and verifies that the wall material is registered in each one. This catches the case where `DR_OUTLINE_ON` and `_OutlineEnabled` both pass but a platform or quality renderer still cannot draw the outline.

Do not infer the deployment renderer list from the currently visible Game view. Trace the Universal Render Pipeline assets referenced by platform and Quality settings, then inspect every Renderer Data asset they reference. A bonus-wall review caught a real gap where PC and the FlatKit example renderer were configured, but `Mobile RP.asset` had no outline feature; the material would have looked correct in-editor and lost its outline on the mobile tier.

## Follow-up: preserve authored surface data and transparent render state

The 2026-09-17 Merchant/new-content pass found 174 newly authored surfaces still using Lit/Unlit or the deprecated outline shader. Use `GeneratedStylizedSurface.Apply` at the end of the relevant importers, after setting source maps and tint. An asset-only conversion leaves future authoring able to recreate inconsistent materials.

Changing `material.shader` resets explicit render queue and override tags in this project. The first preservation check caught `HallClerestory` moving from queue 3000 to 2000. A focused regression subsequently caught `RenderType` changing from Transparent to Opaque. Preserve `rawRenderQueue` across shader assignment and explicitly derive `RenderType` from `_Surface`/`_AlphaClip`. Keep alpha blending, ZWrite, culling and shadow/depth pass state. The installed FlatKit shader has no `_SURFACE_TYPE_TRANSPARENT` variant; validate its blend state, queue, tags and pixels instead of requiring that URP Lit keyword.

```csharp
int queue = material.rawRenderQueue;
material.shader = Shader.Find("FlatKit/Stylized Surface");
material.renderQueue = queue;
material.SetOverrideTag("RenderType", transparent ? "Transparent" : cutout ? "TransparentCutout" : "Opaque");
```

Compare all converted assets against a pre-change backup, including `_BaseMap`/`_BumpMap` references and UV transforms, `_BaseColor`, blend/depth/cull state and queue. Do not overwrite these by copying a whole reference material. All 174 passed the retained comparison. Use milder shading (0.45 threshold, 0.08 transition and a tint-relative 72% shade) to avoid the documented ramp-blackening failure. Glass and road paint omit silhouette outlines.

Native checks: `GeneratedStylizedSurfaceTests` passes preservation/idempotence, the complete material manifest, and registration in Mobile, PC and FlatKit example renderers. Evidence: `map-concepts/new-content-stylized-2026-09-17/README.md`. A long apply request exceeded the CLI's 30-second wait while Unity continued saving; verify completion before retrying. The repeatable command records a 120-second request budget. Ephemeral `run_script` sees two Newtonsoft definitions in this Editor; return structured objects to the CLI instead of referencing `JsonConvert` in those scripts.

## Related references
- [Handle Inline Unity Material Keyword Lists](handle-inline-unity-material-keyword-lists-2026-05-25.md)
- [Avoid Broad Unity MCP Asset Enumeration](avoid-broad-unity-mcp-asset-enumeration-2026-06-13.md)
