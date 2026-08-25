---
title: Adopt Official Unity CLI and Pipeline as the Codex Editor-Control Path
date: 2026-08-23
last_updated: 2026-08-23
category: docs/solutions/tooling-decisions
module: Unity editor automation
problem_type: tooling_decision
component: tooling
severity: medium
applies_when:
  - "Migrating Unity editor automation from an embedded community MCP to the official Unity CLI and Pipeline"
  - "The Pipeline package is reachable but unity status reports STATUS_NO_INSTANCES"
  - "A newly configured Codex MCP server is enabled but its tools are absent from the current session"
  - "Retiring a legacy editor bridge after the official Pipeline path is verified"
tags:
  - unity
  - unity-cli
  - unity-pipeline
  - mcp
  - codex
  - editor-automation
  - play-mode
  - migration
---

# Adopt Official Unity CLI and Pipeline as the Codex Editor-Control Path

## Context

The project originally used the embedded CoderGamester MCP Unity package and a
Node bridge. That path needed project-specific port allocation and delayed
restart patches, but it could still report `Transport closed` or fail to restart
after Play Mode and assembly reload transitions.

The replacement began as a staged migration and completed after the official
path proved stable:

- Unity CLI `1.0.0-beta.6` is installed on the workstation.
- `com.unity.pipeline` `0.5.0-exp.1` is pinned in `Packages/manifest.json`.
- Codex registers the official project-pinned server as `unity`.
- The official Unity CLI skill is installed for Codex at user scope.
- The CoderGamester package, its project port settings, and its Codex MCP entry
  have been removed.

The separate, pre-existing `com.youngwoocho02.unity-cli-connector` package
remains installed. It provides a localhost HTTP command surface for older
project workflows, is not registered as a Codex MCP server, and was outside the
scope of the CoderGamester removal. This decision standardizes the supported
Codex path; it does not claim that Pipeline is the only physical editor
integration in the repository.

## Guidance

Install the official layers separately and verify each boundary:

```powershell
winget install --id Unity.CLI --exact
unity --version
unity pipeline install --project-path .
unity pipeline list
unity command --project-path . list_open_scenes
```

`unity pipeline list` proves that the project contains the Pipeline package and
that its editor endpoint is reachable. A successful narrow command proves the
endpoint can serve this project end to end. `unity status --project-path .` is
supplemental diagnostics only: it can report `STATUS_NO_INSTANCES` while the
same Pipeline endpoint still accepts commands. When those signals disagree,
trust `unity pipeline list` plus the successful narrow command. The endpoint
port is transient editor state; let the CLI discover it instead of copying it
into a script or document.

Register the official server and install the matching agent guidance:

```powershell
unity mcp configure codex --project-path . --yes
unity skill install codex --yes
```

Use `unity` as the only supported Codex MCP server name. Do not restore the
CoderGamester bridge when Pipeline is temporarily reloading; wait for the
official endpoint or repair its package/compile state instead. The retained
HTTP connector is a separate legacy integration, not a Codex fallback.

Retire an embedded bridge across every state owner; removing only its manifest
entry is insufficient:

1. Remove the UPM dependency through `unity command package_remove`, then poll
   `package_status` and `recompile_status`.
2. Delete the embedded package gitlink and its project settings file.
3. Remove the complete `[mcp_servers.mcp-unity]` block from
   `~/.codex/config.toml`, then verify `codex mcp list` keeps `unity` and omits
   `mcp-unity`.
4. Synchronize Unity's generated C# projects and remove stale tracked
   `*.lscache` entries that still name the deleted assemblies.
5. Update the repository harness and current operational documentation in the
   same change.

The harness should enforce both sides of the migration:

```powershell
powershell -ExecutionPolicy Bypass -File tools/validate-agent-harness.ps1
```

It requires `com.unity.pipeline` and rejects the old manifest dependency,
embedded package path, and `ProjectSettings/McpUnitySettings.json`. User-level
Codex registration and live listeners remain separate operational checks.

Use direct CLI commands for narrow verification and editor operations that do
not need MCP indirection:

```powershell
unity list --project-path .
unity command --project-path . --detail compact
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
as diagnostic; confirm recovery with `unity pipeline list` and a narrow
successful `unity command`. UI automation that triggered the refresh can lose
its reply when the domain reload interrupts the capture.

When the CoderGamester package is deleted from a project where the Editor has
already loaded it, restart Unity once. File deletion alone cannot unload the
old assembly from the running AppDomain. Verify AppDomain assembly absence,
listener state, and Pipeline recovery independently after the restart.

In one verified removal, the new Unity AppDomain contained zero `McpUnity`
assemblies and Pipeline recovered normally, but `[::1]:8143` still accepted TCP
connections while Windows attributed it to the terminated old Unity PID. That
is residual operating-system socket state, not evidence that the package is
still installed. Record it explicitly and reboot Windows if closing the port is
required; do not restore or keep changing repository files to chase the orphan.

Start a new Codex session after changing `~/.codex/config.toml`. An already
running session may have loaded its MCP tool registry before the `unity` entry
was added even though `codex mcp list` reports the server as enabled.

## Why This Matters

The official Pipeline discovers and authenticates the correct running Editor
instance rather than making Codex coordinate a fixed WebSocket port.
In the verified installation it exposed 142 tools, returned the active scene,
and stayed `ready` after both Play Mode entry and exit. Recompilation status also
returned `up_to_date` through the same endpoint.

The removed bridge failed delayed restarts because its configured port could be
held by the Unity process. Removing that Codex transport eliminates its port
race and leaves one authenticated, discoverable Codex editor-control boundary.
The retained HTTP connector remains a separate local trust boundary and should
be evaluated independently if it is later retired.

The MCP registration and the agent skill solve different problems: the MCP
entry supplies the connection, while the skill teaches later Codex sessions the
supported command surface and recovery flow.

## When to Apply

- Codex needs to inspect or control a running Unity 6 Editor.
- Play Mode transitions, script reloads, or fixed-port assumptions make an
  embedded MCP unreliable.
- The official transport has been verified and a legacy bridge can be retired.
- Automation needs one project-pinned command vocabulary for local development
  and CI.

## Examples

Minimal end-to-end readiness check:

```powershell
unity pipeline list
unity command --project-path . list_open_scenes
unity status --project-path .
```

The required result is one reachable Pipeline server and the intended loaded
scene from the command. A `ready` status is useful corroboration, but
`STATUS_NO_INSTANCES` does not invalidate the successful command. Do not accept
package presence alone as proof of a usable connection.

Play Mode resilience check:

```powershell
unity command --project-path . editor_play
unity command --project-path . editor_status
unity command --project-path . editor_stop
unity command --project-path . editor_status
```

Both command checks should reach the same project and report the expected play
state.

Report removal verification as separate oracles rather than one broad “removed”
claim:

```text
Repository artifacts: absent
Codex mcp-unity registration: absent
CoderGamester AppDomain assemblies: absent
Official Pipeline: reachable; narrow scene command passed
Builds, tests, and harness: passed
Former listener: closed, or recorded as external Windows residue requiring reboot
```

## Related

- [Retired: Auto-Increment MCP Unity Port On Editor Launch](../workflow-issues/auto-increment-mcp-unity-port-on-editor-launch-2026-06-15.md)
- [Call Unity CLI Connector Commands With Params Payloads](../workflow-issues/call-unity-cli-connector-commands-with-params-payloads-2026-06-06.md)
- [Run Unity Scene Generation Through CLI Connector Exec When Editor Reload Is Stale](../workflow-issues/run-unity-scene-generation-through-cli-connector-exec-when-editor-reload-is-stale-2026-06-21.md)
- [Create Unity Layout Scenes When Editor Execution Is Blocked](../workflow-issues/create-unity-layout-scene-when-editor-execution-is-blocked-2026-05-25.md)
- [Stop Play Mode Before Running Unity EditMode Tests](../workflow-issues/stop-play-mode-before-unity-editmode-tests-2026-08-15.md)
- [Unity CLI as the replacement for the in-Editor MCP server](https://docs.unity.com/en-us/unity-cli/replace-mcp-server-unity-cli)
