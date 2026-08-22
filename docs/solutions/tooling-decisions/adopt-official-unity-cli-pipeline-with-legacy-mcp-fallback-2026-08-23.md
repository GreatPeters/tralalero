---
title: Adopt Official Unity CLI and Pipeline with a Legacy MCP Fallback
date: 2026-08-23
category: docs/solutions/tooling-decisions
module: Unity editor automation
problem_type: tooling_decision
component: tooling
severity: medium
applies_when:
  - "Migrating Unity editor automation from an embedded community MCP to the official Unity CLI and Pipeline"
  - "The Pipeline package is installed but unity status reports STATUS_NO_INSTANCES"
  - "A newly configured Codex MCP server is enabled but its tools are absent from the current session"
  - "The legacy MCP reports a port-in-use failure while the official Pipeline remains reachable"
tags:
  - unity
  - unity-cli
  - unity-pipeline
  - mcp
  - codex
  - editor-automation
  - play-mode
  - fallback
---

# Adopt Official Unity CLI and Pipeline with a Legacy MCP Fallback

## Context

The project originally used the embedded CoderGamester MCP Unity package and a
Node bridge. That path needed project-specific port allocation and delayed
restart patches, but it could still report `Transport closed` or fail to restart
after Play Mode and assembly reload transitions.

The replacement was installed as a staged migration rather than a destructive
swap:

- Unity CLI `1.0.0-beta.6` is installed on the workstation.
- `com.unity.pipeline` `0.5.0-exp.1` is pinned in `Packages/manifest.json`.
- Codex registers the official project-pinned server as `unity`.
- The legacy CoderGamester server remains registered as `mcp-unity` for
  temporary fallback coverage.
- The official Unity CLI skill is installed for Codex at user scope.

## Guidance

Install the official layers separately and verify each boundary:

```powershell
winget install --id Unity.CLI --exact
unity --version
unity pipeline install --project-path .
unity pipeline list
unity status --project-path .
```

`unity pipeline list` proves that the project contains the Pipeline package and
that its editor endpoint is reachable. `unity status --project-path .` proves
that the endpoint belongs to this project and is ready. The endpoint port is
transient editor state; let the CLI discover it instead of copying it into a
script or document.

Register the official server without removing the fallback, then install the
matching agent guidance:

```powershell
unity mcp configure codex --project-path . --yes
unity skill install codex --yes
```

Keep the server names distinct during the migration:

- `unity` means the official Unity CLI/Pipeline transport.
- `mcp-unity` means the embedded CoderGamester/Node transport.

Use direct CLI commands for narrow verification and editor operations that do
not need MCP indirection:

```powershell
unity list --project-path .
unity command --project-path . list_open_scenes
unity command --project-path . editor_play
unity status --project-path .
unity command --project-path . editor_stop
unity command --project-path . recompile
unity command --project-path . recompile_status
```

If installation occurs while the Editor is in Play Mode, the manifest can show
the package before Unity has resolved it or started the Pipeline endpoint. Exit
Play Mode, refresh the project, and wait for package resolution, script
compilation, and domain reload to finish. Treat a follow-up `unity status` result
as authoritative; UI automation that triggered the refresh can lose its reply
when the domain reload interrupts the capture.

Start a new Codex session after changing `~/.codex/config.toml`. An already
running session may have loaded its MCP tool registry before the `unity` entry
was added even though `codex mcp list` reports the server as enabled.

## Why This Matters

The official Pipeline discovers and authenticates the correct running Editor
instance rather than making the repository coordinate a fixed WebSocket port.
In the verified installation it exposed 142 tools, returned the active scene,
and stayed `ready` after both Play Mode entry and exit. Recompilation status also
returned `up_to_date` through the same endpoint.

The legacy bridge failed its own delayed restart because its configured port
was already held by the Unity process. That did not affect the official
Pipeline connection. Keeping both entries during evaluation preserves a
rollback path while their different names make transport-specific failures
unambiguous.

The MCP registration and the agent skill solve different problems: the MCP
entry supplies the connection, while the skill teaches later Codex sessions the
supported command surface and recovery flow.

## When to Apply

- Codex needs to inspect or control a running Unity 6 Editor.
- Play Mode transitions, script reloads, or fixed-port assumptions make an
  embedded MCP unreliable.
- The official transport should be evaluated before removing a working or
  customized fallback.
- Automation needs one project-pinned command vocabulary for local development
  and CI.

## Examples

Minimal end-to-end readiness check:

```powershell
unity pipeline list
unity status --project-path .
unity command --project-path . list_open_scenes
```

The expected result is one reachable Pipeline server, one `ready` Editor for
the current project, and the intended loaded scene. Do not accept package
presence alone as proof of a usable connection.

Play Mode resilience check:

```powershell
unity command --project-path . editor_play
unity status --project-path .
unity command --project-path . editor_stop
unity status --project-path .
```

Both status checks should return the same project in a ready state.

## Related

- [Auto-Increment MCP Unity Port On Editor Launch](../workflow-issues/auto-increment-mcp-unity-port-on-editor-launch-2026-06-15.md)
- [Run Unity Scene Generation Through CLI Connector Exec When Editor Reload Is Stale](../workflow-issues/run-unity-scene-generation-through-cli-connector-exec-when-editor-reload-is-stale-2026-06-21.md)
- [Create Unity Layout Scenes When Editor Execution Is Blocked](../workflow-issues/create-unity-layout-scene-when-editor-execution-is-blocked-2026-05-25.md)
- [Stop Play Mode Before Running Unity EditMode Tests](../workflow-issues/stop-play-mode-before-unity-editmode-tests-2026-08-15.md)
- [Unity CLI as the replacement for the in-Editor MCP server](https://docs.unity.com/en-us/unity-cli/replace-mcp-server-unity-cli)
