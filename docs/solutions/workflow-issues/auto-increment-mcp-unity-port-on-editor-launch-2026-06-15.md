---
title: Auto-Increment MCP Unity Port On Editor Launch
date: 2026-06-15
category: docs/solutions/workflow-issues
module: Unity MCP connection workflow
problem_type: workflow_issue
component: tooling
severity: medium
applies_when:
  - "MCP Unity repeatedly disconnects or reports Transport closed after Unity reloads"
  - "Unity Editor or AssetImportWorker processes keep adjacent MCP ports occupied"
  - "ProjectSettings/McpUnitySettings.json is the shared source of truth for the MCP bridge port"
tags: [unity, mcp, websocket, port-conflict, editor-tooling]
---

# Auto-Increment MCP Unity Port On Editor Launch

## Context
MCP Unity was repeatedly disconnecting, and Unity logs showed the server failing to start because configured ports were already occupied. In this project, the main Unity process and AssetImportWorker processes can hold nearby localhost ports at the same time, so manually changing from `8091` to `8092` can immediately collide.

## Guidance
Keep `ProjectSettings/McpUnitySettings.json` as the source of truth, but choose the port before the MCP WebSocket server starts. The settings loader should:

- Skip changes in `Application.isBatchMode` so AssetImportWorker processes do not advance the project setting.
- Increment the saved port once per main Unity editor process.
- Store a process marker under `Temp/` so script reloads in the same Unity process do not keep incrementing.
- Probe localhost IPv4 and IPv6 before accepting a port.
- If the requested `saved + 1` port is occupied, keep advancing until a free port is found.

The implementation belongs in `Packages/com.gamelovers.mcp-unity/Editor/UnityBridge/McpUnitySettings.cs`, because `McpUnityServer.StartServer()` reads `McpUnitySettings.Instance.Port` when constructing the WebSocket server.

## Why This Matters
The MCP Node bridge reads `ProjectSettings/McpUnitySettings.json`, so saving the selected port lets Codex reconnect without hardcoded defaults. Selecting the port at settings-load time also avoids the weaker pattern of starting the server, seeing a socket error, then trying to recover after clients have already disconnected.

## When to Apply
- MCP Unity logs contain `Port ... is already in use`.
- `netstat -ano` shows Unity or Unity worker processes listening on the configured port.
- The current editor process should keep one stable port through script reloads, but the next Unity launch should move forward.

## Examples
Observed collision state:

```text
127.0.0.1:8090 LISTENING main Unity process
127.0.0.1:8091 LISTENING main Unity process or Unity CLI connector
127.0.0.1:8092 LISTENING AssetImportWorker or stale Unity listener
127.0.0.1:8093 LISTENING AssetImportWorker
```

After reload, the new settings loader logged:

```text
[MCP Unity] Auto-incremented connection port from 8091 to 8092, then skipped occupied ports and selected 8094 for this Unity editor launch.
[MCP Unity] WebSocket server started successfully on localhost:8094.
```

Verification:

```powershell
dotnet build McpUnity.Editor.csproj -nologo
Get-Content ProjectSettings\McpUnitySettings.json
```

Then send a direct WebSocket request to `ws://localhost:<Port>/McpUnity` using the saved port and confirm `get_scene_info` returns the active scene.

## Related
- [Create Unity Layout Scenes When Editor Execution Is Blocked](create-unity-layout-scene-when-editor-execution-is-blocked-2026-05-25.md)
- [Call Unity CLI Connector Commands With Params Payloads](call-unity-cli-connector-commands-with-params-payloads-2026-06-06.md)
