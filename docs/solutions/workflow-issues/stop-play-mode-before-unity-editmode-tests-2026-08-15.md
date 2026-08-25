---
title: Stop Play Mode Before Running Unity EditMode Tests
date: 2026-08-15
last_updated: 2026-08-23
category: docs/solutions/workflow-issues
module: Unity Test Harness
problem_type: workflow_issue
component: testing_framework
severity: medium
applies_when:
  - "Running Unity EditMode tests through official Pipeline or another in-process editor command path"
  - "The Unity editor state reports playing, paused, or another non-EditMode state"
  - "A previous test request timed out and later editor commands also stop responding"
related_components:
  - "development_workflow"
  - "tooling"
tags:
  - "unity"
  - "editmode-tests"
  - "play-mode"
  - "test-runner"
  - "cli-connector"
  - "unity-pipeline"
---

# Stop Play Mode Before Running Unity EditMode Tests

> Status: updated. The connector health sequences below describe the historical
> direct HTTP workflow. Current Codex work uses official Unity CLI/Pipeline;
> the retained `com.youngwoocho02.unity-cli-connector` is separate and is not a
> Codex MCP fallback.

## Context

A filtered EditMode test was requested while the Unity health endpoint reported
`state=paused`. In this connector, `paused` means the player is still running;
it does not mean the editor is ready for EditMode tests.

Unity rejected the run with `InvalidOperationException: This cannot be used
during play mode`, emitted `PostbuildCleanup` test-tree errors, and never
returned a final result before the HTTP request timed out. A later
`manage_editor stop` command queued behind the wedged test request and timed out
as well.

## Guidance

Treat editor state as a required test preflight. For current Codex work:

1. Run `unity pipeline list`, then query state with
   `unity command --project-path . editor_status`.
2. If the state is `playing` or `paused`, stop Play Mode first.
3. Poll `editor_status` until it explicitly reports Edit Mode and the official
   command path is responsive.
4. Submit one exact filtered test request and wait for its final result before
   sending another editor command.

Use `unity command --project-path . editor_stop` and
`unity command --project-path . run_tests --mode editor --filter <exact-filter>`
for steps 2 and 4. `unity status --project-path .` can report
`STATUS_NO_INSTANCES` despite a working Pipeline command, so it is not the
authoritative state or reachability check.

Do not treat a timeout or a zero-test response as a pass. Inspect `Editor.log`
for the play-mode exception, cleanup errors, and command-queue blockage before
retrying. Do not send recovery commands through the same queue until the
original test request has completed or the connector is known to be responsive.

If Unity verification remains unavailable, report each independent check
separately. A successful `.csproj` build or prefab serialization inspection is
useful evidence, but it does not prove that the Unity test passed.

## Why This Matters

EditMode tests require Unity to be outside Play Mode. Pausing freezes the game
loop without ending the player session, so the invalid preflight creates a
misleading chain of secondary failures: an immediate Unity exception,
Test Runner cleanup errors, a connector timeout, and recovery commands that
cannot move past the unfinished request.

Verifying the editor state first preserves the command queue and makes the test
result trustworthy.

## When to Apply

- Before filtered or broad EditMode runs in a shared authoring editor.
- After gameplay or visual verification that may have left Unity playing or
  paused.
- When `PostbuildCleanup`, missing test-tree, or play-mode
  `InvalidOperationException` messages appear.
- Before retrying after a test request times out while Unity remains alive.

## Examples

Current official sequence:

```text
unity pipeline list
unity command --project-path . editor_status
unity command --project-path . editor_stop
unity command --project-path . editor_status
unity command --project-path . run_tests --mode editor --filter <one exact test>
wait for explicit pass or fail
```

Historical connector sequence:

Healthy sequence:

```text
health -> state=paused
manage_editor stop
health -> state=edit_mode, responsive
run_tests(filter=<one exact test>)
wait for explicit pass or fail
```

Invalid sequence:

```text
health -> state=paused
run_tests(filter=<one exact test>)
timeout
manage_editor stop
timeout because stop queued behind the wedged test
```

Classify results precisely:

```text
1 test passed                         -> pass
1 test failed                         -> fail
0 tests or request timeout            -> inconclusive; investigate
play-mode InvalidOperationException   -> invalid preflight; stop and retry
```

## Related

- [Protect Active Unity Scenes from Broad EditMode Test Runs](protect-active-unity-scenes-from-broad-editmode-test-runs-2026-07-18.md)
- [Call Unity CLI Connector Commands with Params Payloads](call-unity-cli-connector-commands-with-params-payloads-2026-06-06.md)
- [Bake Generated Prefab UI Previews and Isolate EditMode Instantiation](bake-generated-prefab-ui-previews-and-isolate-editmode-tests-2026-08-13.md)
- [Adopt Official Unity CLI and Pipeline as the Codex Editor-Control Path](../tooling-decisions/adopt-official-unity-cli-pipeline-as-codex-editor-control-path-2026-08-23.md)
