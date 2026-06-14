---
title: Call Unity CLI Connector commands with params payloads
date: 2026-06-06
category: docs/solutions/workflow-issues
module: Unity CLI Connector verification workflow
problem_type: workflow_issue
component: tooling
severity: low
applies_when:
  - "Calling the Unity CLI Connector HTTP endpoint directly from Codex"
  - "Running Unity EditMode tests through POST /command"
tags: [unity, cli-connector, test-runner, verification]
---

# Call Unity CLI Connector commands with params payloads

## Context

The Unity CLI Connector exposes a local HTTP endpoint at `/command`, but its request schema is easy to mix up with MCP tool schemas. Sending test arguments under `args` or `parameters` can produce misleading failures, including missing required fields or zero-test runs.

## Guidance

Send command arguments under `params`. For EditMode tests, use `mode` and `filter`, not MCP-style `testMode` or `testFilter`.

```powershell
$body = @{
    command = 'run_tests'
    params = @{
        mode = 'EditMode'
        filter = 'NoryangjinMapToolGridUtilityTests.SceneViewTopMode_UsesExactOverheadOrthographicView'
        allow_dirty_scenes = $true
    }
} | ConvertTo-Json -Depth 6

Invoke-RestMethod `
    -Uri 'http://127.0.0.1:8093/command' `
    -Method Post `
    -Body $body `
    -ContentType 'application/json'
```

If Unity has not picked up new tests yet, refresh and request compilation first:

```powershell
$body = @{
    command = 'refresh_unity'
    params = @{
        mode = 'force'
        compile = 'request'
        force = $true
    }
} | ConvertTo-Json -Depth 5
```

## Why This Matters

The connector can successfully accept the HTTP request while still running no tests if the payload or Unity compilation state is wrong. A zero-test pass is not verification evidence; refresh Unity, then run a class or exact test filter and check that the expected test count is nonzero.

## When to Apply

- A local connector `/health` endpoint reports `ready: true`.
- You need Unity Test Runner evidence from the editor rather than only `dotnet build`.
- A filtered Unity test run returns `All 0 test(s) passed`.

## Examples

Before: POSTing `{ command, args: { testMode, testFilter } }` returns parsing or missing-field errors.

After: POSTing `{ command, params: { mode, filter } }` runs the intended Unity tests and reports the exact pass/fail count.

## Related

- [Create Unity layout scene when editor execution is blocked](create-unity-layout-scene-when-editor-execution-is-blocked-2026-05-25.md)
