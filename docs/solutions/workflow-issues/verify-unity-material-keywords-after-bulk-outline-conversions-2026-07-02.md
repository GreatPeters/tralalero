---
title: Verify Unity Material Keywords After Bulk Outline Conversions
date: 2026-07-02
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

The first pass proved that matching float properties is not enough. A material can have `_OutlineEnabled: 1` and `_OutlineWidth: 2` while the shader keyword state is still wrong or contradictory.

## Guidance
For bulk FlatKit outline conversions, verify three layers together:

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

## Why This Matters
FlatKit's outline pass is keyword-driven. The YAML can look visually close to the reference material while Unity still compiles or selects a variant without the intended outline if the keyword list is stale. A contradictory valid/invalid keyword block is especially easy to miss during bulk edits because the float values are correct and the file still parses.

## When to Apply
- Converting a known folder of generated MeshyAI materials from URP Lit to FlatKit.
- Normalizing a map-tool palette so future prefab placements inherit consistent outlines.
- Repairing `.mat` files after Unity or an MCP helper updates properties but does not preserve shader keywords.

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

## Related
- [Handle Inline Unity Material Keyword Lists](handle-inline-unity-material-keyword-lists-2026-05-25.md)
- [Avoid Broad Unity MCP Asset Enumeration](avoid-broad-unity-mcp-asset-enumeration-2026-06-13.md)
