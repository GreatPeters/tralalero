---
title: Repair Unity assets when editor command execution is blocked
date: 2026-05-24
last_updated: 2026-08-23
category: docs/solutions/workflow-issues
module: Unity asset import workflow
problem_type: workflow_issue
component: tooling
severity: medium
applies_when:
  - "The official Unity Pipeline endpoint is temporarily unavailable during an asset repair task"
  - "Unity batchmode cannot open the project because the editor already has it open"
  - "Generated FBX assets need material mappings before the editor can be driven again"
tags: [unity, meshy, asset-import, materials, unity-pipeline]
---

# Repair Unity assets when editor command execution is blocked

> Status: updated. References to a closed MCP transport below describe the
> historical CoderGamester incident. The current Codex path is Unity CLI plus
> Pipeline. The separate `com.youngwoocho02.unity-cli-connector` package remains
> installed for older workflows but is not the Codex MCP fallback.

## Context
MeshyAI FBX assets imported with sibling texture files but no extracted material assets or prefab assets. The intended editor utility path was:

1. Add a `Tools/맵 제작 도구/유지보수/재질 및 프리팹 복구` menu item.
2. Run it through MCP Unity.
3. Let Unity create materials, copy textures, and save prefabs.

That path was blocked because the MCP transport closed. A direct Unity batchmode fallback also failed because the project was already open in another Unity instance.

## Guidance
Keep two repair paths available:

- Preferred: confirm `unity pipeline list`, run a narrow read such as
  `unity command --project-path . list_open_scenes`, then invoke the checked-in
  editor utility through a targeted official `unity command` such as `menu`.
- Fallback: generate file-level `.mat`, `.mat.meta`, copied texture assets, texture `.meta`, and FBX `.meta` `externalObjects` mappings from the filesystem.
- If a newly copied FBX has a valid `.fbx.meta` but `AssetDatabase.FindAssets("t:Model")` still misses it, include a filesystem scan under `Assets/ShooterSurvival/Models/MeshyAI` and call `AssetDatabase.ImportAsset(..., ForceUpdate)` before the repair pass.

Treat `unity status --project-path .` as supplemental diagnostics. If it reports
`STATUS_NO_INSTANCES` while Pipeline is reachable and the narrow command
succeeds, the command result is authoritative.

For Meshy-generated FBX files, the embedded source material name is commonly `Material.001`. A valid FBX material remap block looks like:

```yaml
  externalObjects:
  - first:
      type: UnityEngine:Material
      assembly: UnityEngine.CoreModule
      name: Material.001
    second: {fileID: 2100000, guid: <material-guid>, type: 2}
```

The historical fallback left a one-shot request marker for the editor utility,
so the next Unity refresh could create real prefabs. New repair flows should use
an explicit, documented retry rather than leave a marker that may run later in
an unrelated editor session.

## Why This Matters
Unity model assets can look broken even when the FBX and PNG files exist, because the imported model has no external material mapping. Creating material assets is not enough; the FBX importer metadata must also map the embedded material source to those external materials.

## When to Apply
- Generated model assets render gray or pink after importing Meshy zips.
- `Materials/MeshyAI` and `Prefabs/MeshyAI` are empty while `Models/MeshyAI` contains FBX and texture PNG files.
- Pipeline or batchmode cannot currently execute an editor repair method.

## Examples
The MeshyAI repair pass wrote:

- `70` URP Lit material assets under `Assets/ShooterSurvival/Materials/MeshyAI`.
- `346` copied texture PNG assets under `Assets/ShooterSurvival/Textures/MeshyAI`.
- `70` FBX `.meta` material remaps from `Material.001` to generated material GUIDs.

Separate `.prefab` assets still require Unity editor execution through
`PrefabUtility`. In the historical run, a one-shot request marker completed that
step after reload; current work should invoke the utility explicitly through
the official CLI once Pipeline recovers.

## Related
- [Extract Meshy zips from a short temp path](extract-meshy-zip-in-short-temp-path-2026-05-24.md)
- [Precompute PowerShell conditionals before PSCustomObject literals](precompute-powershell-conditionals-before-pscustomobject-2026-05-24.md)
- [Adopt Official Unity CLI and Pipeline as the Codex Editor-Control Path](../tooling-decisions/adopt-official-unity-cli-pipeline-as-codex-editor-control-path-2026-08-23.md)
