---
title: "Retired: Auto-Increment MCP Unity Port On Editor Launch"
date: 2026-06-15
last_updated: 2026-08-23
category: docs/solutions/workflow-issues
module: Unity MCP connection workflow
problem_type: workflow_issue
component: tooling
severity: medium
applies_when:
  - "Reading an MCP Unity port-collision incident from before the 2026-08-23 Pipeline migration"
  - "Verifying that the removed CoderGamester listener is gone after its package was deleted"
tags: [unity, mcp, websocket, port-conflict, editor-tooling, retired]
---

# Retired: Auto-Increment MCP Unity Port On Editor Launch

> Status: superseded. The CoderGamester package,
> `ProjectSettings/McpUnitySettings.json`, and the Codex `mcp-unity` entry were
> removed on 2026-08-23. Do not reapply this port-allocation workaround.

## Context
MCP Unity was repeatedly disconnecting, and Unity logs showed the server failing to start because configured ports were already occupied. In this project, the main Unity process and AssetImportWorker processes can hold nearby localhost ports at the same time, so manually changing from `8091` to `8092` can immediately collide.

## Guidance
Use the official Unity CLI and `com.unity.pipeline` instead of allocating a
project-owned WebSocket port:

```powershell
unity pipeline list
unity command --project-path . list_open_scenes
```

If the endpoint is temporarily unavailable, wait for package resolution,
compilation, and domain reload. Check Safe Mode and compiler errors before
changing project files. Do not restore the deleted package or settings file as
a fallback.

If CoderGamester was already loaded when its package was deleted, restart Unity
once so the old AppDomain releases its listener. Confirm the formerly
configured port is no longer listening, then verify recovery with
`unity pipeline list` and the narrow command above. `unity status` may report
`STATUS_NO_INSTANCES` despite a working command and is not authoritative here.

## Why This Matters
Pipeline discovers an authenticated per-editor endpoint and does not require a
shared, versioned port. Removing the duplicate server eliminates the original
port collision rather than moving it to another number.

## When to Apply
- Do not apply this workaround to the current repository.
- Use this document only when interpreting history from before the official
  Pipeline migration.

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

Current verification:

```powershell
Test-Path Packages\com.gamelovers.mcp-unity
Test-Path ProjectSettings\McpUnitySettings.json
unity pipeline list
unity command --project-path . list_open_scenes
```

Both path checks should be `False`, the former CoderGamester port should have no
listener after the one-time Unity restart, and Pipeline should report one
reachable server that completes the narrow command.

## Related
- [Adopt Official Unity CLI and Pipeline as the Codex Editor-Control Path](../tooling-decisions/adopt-official-unity-cli-pipeline-as-codex-editor-control-path-2026-08-23.md)
- [Create Unity Layout Scenes When Editor Execution Is Blocked](create-unity-layout-scene-when-editor-execution-is-blocked-2026-05-25.md)
- [Call Unity CLI Connector Commands With Params Payloads](call-unity-cli-connector-commands-with-params-payloads-2026-06-06.md)
