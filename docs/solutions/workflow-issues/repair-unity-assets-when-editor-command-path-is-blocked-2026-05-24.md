---
title: Repair Unity assets when editor command execution is blocked
date: 2026-05-24
category: docs/solutions/workflow-issues
module: Unity asset import workflow
problem_type: workflow_issue
component: tooling
severity: medium
applies_when:
  - Unity MCP transport is closed during an asset repair task
  - Unity batchmode cannot open the project because the editor already has it open
  - Generated FBX assets need material mappings before the editor can be driven again
tags: [unity, meshy, asset-import, materials, mcp]
---

# Repair Unity assets when editor command execution is blocked

## Context
MeshyAI FBX assets imported with sibling texture files but no extracted material assets or prefab assets. The intended editor utility path was:

1. Add a `Tools/MeshyAI/Repair Materials And Prefabs` menu item.
2. Run it through MCP Unity.
3. Let Unity create materials, copy textures, and save prefabs.

That path was blocked because the MCP transport closed. A direct Unity batchmode fallback also failed because the project was already open in another Unity instance.

## Guidance
Keep two repair paths available:

- Preferred: run an editor utility that uses `AssetDatabase`, `TextureImporter`, `Material`, and `PrefabUtility`.
- Fallback: generate file-level `.mat`, `.mat.meta`, copied texture assets, texture `.meta`, and FBX `.meta` `externalObjects` mappings from the filesystem.

For Meshy-generated FBX files, the embedded source material name is commonly `Material.001`. A valid FBX material remap block looks like:

```yaml
  externalObjects:
  - first:
      type: UnityEngine:Material
      assembly: UnityEngine.CoreModule
      name: Material.001
    second: {fileID: 2100000, guid: <material-guid>, type: 2}
```

The fallback should still leave a one-shot request marker for the editor utility, so the next Unity refresh can create real prefabs when the editor command path becomes available.

## Why This Matters
Unity model assets can look broken even when the FBX and PNG files exist, because the imported model has no external material mapping. Creating material assets is not enough; the FBX importer metadata must also map the embedded material source to those external materials.

## When to Apply
- Generated model assets render gray or pink after importing Meshy zips.
- `Materials/MeshyAI` and `Prefabs/MeshyAI` are empty while `Models/MeshyAI` contains FBX and texture PNG files.
- MCP or batchmode cannot currently execute an editor repair method.

## Examples
The MeshyAI repair pass wrote:

- `70` URP Lit material assets under `Assets/ShooterSurvival/Materials/MeshyAI`.
- `346` copied texture PNG assets under `Assets/ShooterSurvival/Textures/MeshyAI`.
- `70` FBX `.meta` material remaps from `Material.001` to generated material GUIDs.

Separate `.prefab` assets still require Unity editor execution through `PrefabUtility`; keep the one-shot request marker until Unity reloads and runs the editor utility.

## Related
- [Extract Meshy zips from a short temp path](extract-meshy-zip-in-short-temp-path-2026-05-24.md)
- [Precompute PowerShell conditionals before PSCustomObject literals](precompute-powershell-conditionals-before-pscustomobject-2026-05-24.md)
