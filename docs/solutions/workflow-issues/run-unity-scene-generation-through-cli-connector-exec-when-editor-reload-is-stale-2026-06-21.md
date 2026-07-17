---
title: Run Unity Scene Generation Through CLI Connector Exec When Editor Reload Is Stale
date: 2026-06-21
last_updated: 2026-07-15
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
  - "A Unity menu path containing non-ASCII characters reaches the connector with question marks"
tags: [unity, cli-connector, exec, scene-generation, mcp, noryangjin, powershell, unicode]
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

Do not assume that the first responsive port belongs to this editor. Probe the expected CLI Connector candidates and select the response whose project path matches the current workspace. The MCP WebSocket port in `ProjectSettings/McpUnitySettings.json` is a separate service and must not be reused as a hardcoded CLI Connector port.

When a menu path contains Korean or other non-ASCII characters, keep the HTTP request body ASCII-only by encoding those characters as JSON `\uXXXX` escapes. This avoids losing the menu name at the PowerShell or console encoding boundary before the connector parses the JSON. For example:

```json
{"command":"menu","params":{"menu_path":"Tools/MeshyAI/\uB178\uB7C9\uC9C4 \uB9F5\uD234 \uC5F4\uAE30 \uB610\uB294 \uC0DD\uC131"}}
```

Send that raw JSON body with `Content-Type: application/json`. If a literal Korean path arrives as `???`, treat the failed menu lookup as a no-op, rebuild the request with Unicode escapes, and verify the scene state after the retry.

For the `exec` fallback, send only code that can stand alone in the current editor AppDomain. Do not call the just-added builder type if Unity has not loaded it yet. Inline the minimum required scene-generation logic instead: load the target scene, find prefabs by asset path or GUID through `AssetDatabase`, instantiate with `PrefabUtility`, remove only the generated object-name prefix for this pass, save the scene, and write a short report under `Temp/`.

Use an explicit generated-object prefix such as `Road_Concept`, `Prop_Concept`, or `Concept_Background`, so the fallback can regenerate its own placements without deleting manual map-tool work. After success, remove any one-shot request marker that the editor utility would otherwise process later.

## Why This Matters

Unity can be in a split state where repository-side C# builds pass, but the open editor has not refreshed enough to expose the menu item or type. Direct scene YAML generation loses Unity serialization behavior, while waiting for MCP can leave the user blocked. CLI Connector `exec` keeps the work inside Unity's editor API without depending on the stale class load.

## When to Apply

- `dotnet build Assembly-CSharp-Editor.csproj -nologo` passes, but Unity cannot find the new editor menu or type.
- MCP Unity commands time out in the queue and direct WebSocket recovery fails.
- Unity CLI Connector `/health` responds on a local port.
- The operation can be expressed as an idempotent generated pass with clear object-name prefixes.
- A valid menu item cannot be found because its non-ASCII path was corrupted before reaching Unity.

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

During the 2026-07-15 map-tool workspace expansion, a PowerShell request containing the literal menu path `Tools/MeshyAI/노량진 맵툴 열기 또는 생성` reached the CLI Connector as question marks and failed without changing the scene. Retrying with the escaped JSON path above succeeded. The final saved scene was clean and contained a `452.25 × 452.25` floor with `804` grid children, while the existing `13` Roads and `42` Props remained intact.

## Related

- [Call Unity CLI Connector commands with params payloads](call-unity-cli-connector-commands-with-params-payloads-2026-06-06.md)
- [Create Unity Layout Scenes When Editor Execution Is Blocked](create-unity-layout-scene-when-editor-execution-is-blocked-2026-05-25.md)
- [Auto-Increment MCP Unity Port On Editor Launch](auto-increment-mcp-unity-port-on-editor-launch-2026-06-15.md)
- [Verify MeshyAI workbook migrations with stable selectors](verify-meshyai-workbook-migrations-with-stable-selectors-2026-06-01.md)
