---
title: Call Unity CLI Connector commands with params payloads
date: 2026-06-06
last_updated: 2026-08-23
category: docs/solutions/workflow-issues
module: Unity CLI Connector verification workflow
problem_type: workflow_issue
component: tooling
severity: low
applies_when:
  - "Maintaining a pre-existing direct Unity CLI Connector HTTP workflow"
  - "Reading historical Unity EditMode test calls made through POST /command"
tags: [unity, cli-connector, test-runner, verification, historical]
---

# Call Unity CLI Connector commands with params payloads

> Status: historical for Codex automation. The separate
> `com.youngwoocho02.unity-cli-connector` package remains installed, but it is
> not registered as a Codex MCP server. Current Codex work should use official
> `unity command`; the HTTP payload below is retained for older direct-connector
> maintenance.

## Context

The Unity CLI Connector exposes a local HTTP endpoint at `/command`, but its request schema is easy to mix up with MCP tool schemas. Sending test arguments under `args` or `parameters` can produce misleading failures, including missing required fields or zero-test runs.

## Guidance

For current test execution, inspect the official schema and call the command
directly:

```powershell
unity pipeline list
unity command --project-path . --detail full --query run_tests
unity command --project-path . run_tests --mode editor --filter NoryangjinMapToolGridUtilityTests.SceneViewTopMode_UsesExactOverheadOrthographicView
```

A successful narrow command is the reachability proof even if `unity status`
reports `STATUS_NO_INSTANCES`.

For intentional maintenance of the retained HTTP connector, send command
arguments under `params`. For EditMode tests, use `mode` and `filter`, not
MCP-style `testMode` or `testFilter`.

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

- The retained connector is being maintained explicitly rather than used as a
  Codex fallback.
- A historical direct HTTP call needs to be reproduced or interpreted.
- A connector-filtered Unity test run returns `All 0 test(s) passed`.

## Examples

Historical before: POSTing `{ command, args: { testMode, testFilter } }` returns
parsing or missing-field errors.

Historical after: POSTing `{ command, params: { mode, filter } }` runs the
intended Unity tests and reports the exact pass/fail count.

## Related

- [Create Unity layout scene when editor execution is blocked](create-unity-layout-scene-when-editor-execution-is-blocked-2026-05-25.md)
- [Adopt Official Unity CLI and Pipeline as the Codex Editor-Control Path](../tooling-decisions/adopt-official-unity-cli-pipeline-as-codex-editor-control-path-2026-08-23.md)
