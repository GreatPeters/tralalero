---
title: Run Unity Scene Generation Through CLI Connector Exec When Editor Reload Is Stale
date: 2026-06-21
category: docs/solutions/workflow-issues
module: Unity scene generation workflow
problem_type: workflow_issue
component: tooling
severity: medium
applies_when:
  - "MCP Unity commands time out or remain queued while the Unity editor is still open"
  - "The configured MCP WebSocket port is unavailable or refuses direct requests"
  - "A newly added editor utility compiles with dotnet but is not yet loaded in Unity's current AppDomain"
  - "A scene can be generated safely from existing registered prefabs and Unity editor APIs"
tags: [unity, cli-connector, exec, scene-generation, mcp, noryangjin]
---

# Run Unity Scene Generation Through CLI Connector Exec When Editor Reload Is Stale

## Context

The Noryangjin map-tool concept layout needed to be generated inside the live map-tool scene. The normal path was an editor utility and menu item, but MCP Unity calls stayed queued, the configured MCP WebSocket failed to accept a direct connection, and Unity had not loaded the new editor class into the current AppDomain even though `Assembly-CSharp-Editor.csproj` compiled successfully.

## Guidance

Keep the checked-in editor utility as the durable path, but do not wait indefinitely for a stale Unity editor reload when the scene can be generated deterministically. First probe the Unity CLI Connector health endpoint and use the `/command` schema with `params`:

```powershell
Invoke-RestMethod -Uri "http://127.0.0.1:<connector-port>/health"
```

If `/health` responds, prefer narrow commands in this order:

- `refresh_unity` with `compile = 'request'` to give Unity a chance to load the new editor script.
- `menu` for the checked-in menu item if Unity reports the item exists.
- `exec` with a self-contained C# script when the menu item or new type is still unavailable.

For the `exec` fallback, send only code that can stand alone in the current editor AppDomain. Do not call the just-added builder type if Unity has not loaded it yet. Inline the minimum required scene-generation logic instead: load the target scene, find prefabs by asset path or GUID through `AssetDatabase`, instantiate with `PrefabUtility`, remove only the generated object-name prefix for this pass, save the scene, and write a short report under `Temp/`.

Use an explicit generated-object prefix such as `Road_Concept`, `Prop_Concept`, or `Concept_Background`, so the fallback can regenerate its own placements without deleting manual map-tool work. After success, remove any one-shot request marker that the editor utility would otherwise process later.

## Why This Matters

Unity can be in a split state where repository-side C# builds pass, but the open editor has not refreshed enough to expose the menu item or type. Direct scene YAML generation loses Unity serialization behavior, while waiting for MCP can leave the user blocked. CLI Connector `exec` keeps the work inside Unity's editor API without depending on the stale class load.

## When to Apply

- `dotnet build Assembly-CSharp-Editor.csproj -nologo` passes, but Unity cannot find the new editor menu or type.
- MCP Unity commands time out in the queue and direct WebSocket recovery fails.
- Unity CLI Connector `/health` responds on a local port.
- The operation can be expressed as an idempotent generated pass with clear object-name prefixes.

## Examples

For the Noryangjin concept layout, the fallback loaded `Assets/ShooterSurvival/Scenes/Tools/Noryangjin_MapTool_Mode.unity`, placed registered map-tool roads and Stage01 props, saved the scene, and wrote a report:

```text
Route nodes: 26
Placed objects: 117
Missing prefabs: 0
```

Repository-side verification then checked that the saved scene contained generated concept objects:

```powershell
Select-String `
    -Path Assets/ShooterSurvival/Scenes/Tools/Noryangjin_MapTool_Mode.unity `
    -Pattern "Road_Concept|Prop_Concept|Concept_Background" |
    Measure-Object
```

The checked-in menu item still matters for future clean rebuilds after Unity completes a proper script reload:

```text
Tools/MeshyAI/Build Noryangjin MapTool Concept Layout
```

## Related

- [Call Unity CLI Connector commands with params payloads](call-unity-cli-connector-commands-with-params-payloads-2026-06-06.md)
- [Create Unity Layout Scenes When Editor Execution Is Blocked](create-unity-layout-scene-when-editor-execution-is-blocked-2026-05-25.md)
- [Auto-Increment MCP Unity Port On Editor Launch](auto-increment-mcp-unity-port-on-editor-launch-2026-06-15.md)
