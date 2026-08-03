---
title: Avoid Synchronous EDM Resolution From Unity Editor Command Queues
date: 2026-07-30
category: docs/solutions/workflow-issues
module: Unity Android dependency resolution workflow
problem_type: workflow_issue
component: development_workflow
severity: medium
applies_when:
  - "Triggering External Dependency Manager resolution from Unity CLI Connector exec, MCP, or another editor main-thread command queue"
  - "`PlayServicesResolver.ResolveSync(...)` appears attractive because the caller needs a completion result"
  - "EDM auto-resolution displays a prompt whose callback owns continuation"
  - "Unity becomes unresponsive or EDM's static queue remains stuck after a resolver dialog is closed"
root_cause: thread_violation
resolution_type: workflow_improvement
tags: [unity, edm4u, dependency-resolution, resolvesync, main-thread, cli-connector, deadlock, firebase]
---

# Avoid Synchronous EDM Resolution From Unity Editor Command Queues

## Context

Firebase package installation required External Dependency Manager for Unity
(EDM4U) to resolve Android artifacts. Calling
`PlayServicesResolver.ResolveSync(true)` from a Unity CLI Connector `exec`
froze the editor. After the approved restart, forcibly closing the one-time
`Enable Android Auto-resolution?` window removed the UI without invoking the
callback that owned the resolver continuation, leaving EDM's in-memory queue
stuck.

This does not mean `ResolveSync` is universally broken. The unsafe combination
is a synchronous resolver wait inside editor main-thread automation, especially
when an interactive prompt can participate in the flow.

## Guidance

Return control to Unity immediately and observe completion through the
callback-based API:

```csharp
const string statusKey = "edm4u.resolve.status";
string runId = Guid.NewGuid().ToString("N");

GooglePlayServices.SettingsDialog.PromptBeforeAutoResolution = false;
UnityEditor.SessionState.SetString(statusKey, runId + ":running");

GooglePlayServices.PlayServicesResolver.Resolve(
    resolutionComplete: null,
    forceResolution: true,
    resolutionCompleteWithResult: success =>
    {
        UnityEditor.SessionState.SetString(
            statusKey,
            runId + (success ? ":succeeded" : ":failed"));
    });

return runId;
```

Poll the status in a later command. Do not keep the original HTTP, MCP, or
editor command blocked while waiting for the callback.

Before scheduling an automated resolve:

- Save or otherwise account for dirty scenes and identify the exact main Unity
  process.
- Configure EDM's prompt preference before starting. If a resolver prompt does
  appear, choose an offered option; closing the `EditorWindow` is not a
  substitute for invoking its selection callback.
- Use a unique status key or operation ID so a previous success cannot be
  mistaken for the current run.
- Avoid sending a second resolution request while the first callback is
  pending.

If Unity is already frozen, first attempt a graceful close. Terminate only the
verified main editor PID, and only with user authorization. Restarting is the
preferred way to clear abandoned static resolver state.

If a prompt was closed and the restarted editor immediately restores a stuck
EDM state, inspect rather than blindly clear it. In EDM4U 1.2.186 the relevant
invariants are:

- `resolutionJobs` contains the `null` marker used to represent an active job;
- no resolver window or background job remains able to signal completion;
- `autoResolving` remains `true`; and
- `autoResolveJobId` is `0`.

Only after confirming that exact orphaned state may a one-off recovery clear
the resolver queue and reset those auto-resolution flags before scheduling a
fresh callback-based `Resolve(...)`. Treat reflection against these private
fields as version-specific emergency recovery, not reusable product tooling.

Completion means more than receiving `success = true`. Verify all of the
following:

- `ProjectSettings/AndroidResolverDependencies.xml` contains the expected
  package set and output files.
- Generated AAR/POM files are materialized rather than zero-byte files or Git
  LFS pointers, and their pinned hashes match.
- Gradle templates reference the generated Maven repository and exact
  dependencies.
- The setup validator passes, the active scene is clean, and authored scene
  hashes are unchanged.

## Why This Matters

EDM4U 1.2.186 implements `ResolveSync` by scheduling a job, waiting on a
`ManualResetEvent`, and trying to pump its main-thread work queue. A command
handler already running inside Unity's editor main-thread queue can prevent the
nested work or GUI callback needed to signal that event.

The resolver queue also appends a `null` in-progress marker. Its normal
completion path removes that marker and starts the next job. An interactive
dialog whose close action is `SelectedNone` does not invoke its completion
callback when forcibly destroyed, so the marker and queued work can remain
orphaned.

Using the callback API lets Unity's update and GUI loops continue. Verifying the
resolved files separately also prevents a successful callback from masking a
partial repository or unsmudged LFS checkout.

## When to Apply

- An editor automation command needs to force Android dependency resolution.
- The resolver can show auto-resolution, Jetifier, Gradle-template, or other
  callback-driven dialogs.
- Unity health checks time out immediately after `ResolveSync`.
- New resolve requests queue forever after a resolver window was closed.

## Examples

The failed run left four queue entries, including one `null` in-progress
marker, with `autoResolving = true` and `autoResolveJobId = 0`. After the exact
stuck state was reset, prompt-before-auto-resolution was disabled and an
asynchronous `Resolve(...)` callback reported success.

Repository verification then confirmed five pinned Android dependencies, four
hashed Firebase Unity AAR/POM artifacts, a clean active scene, and 41/41 passing
Firebase setup-validator tests.

The queue and synchronous-wait behavior is visible in the
[EDM4U 1.2.186 `PlayServicesResolver` source](https://github.com/googlesamples/unity-jar-resolver/blob/v1.2.186/source/AndroidResolver/src/PlayServicesResolver.cs).

## Related

- [Run Unity Scene Generation Through CLI Connector Exec When Editor Reload Is Stale](run-unity-scene-generation-through-cli-connector-exec-when-editor-reload-is-stale-2026-06-21.md)
- [Call Unity CLI Connector commands with params payloads](call-unity-cli-connector-commands-with-params-payloads-2026-06-06.md)
- [Create Unity Layout Scenes When Editor Execution Is Blocked](create-unity-layout-scene-when-editor-execution-is-blocked-2026-05-25.md)
- [Auto-Increment MCP Unity Port On Editor Launch](auto-increment-mcp-unity-port-on-editor-launch-2026-06-15.md)
