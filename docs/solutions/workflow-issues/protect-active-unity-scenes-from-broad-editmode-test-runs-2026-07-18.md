---
title: Protect Active Unity Scenes from Broad EditMode Test Runs
date: 2026-07-18
last_updated: 2026-08-05
category: docs/solutions/workflow-issues
module: Unity Noryangjin map tooling
problem_type: workflow_issue
component: testing_framework
severity: medium
applies_when:
  - "Running Unity EditMode tests that place, delete, activate, or save map-tool objects"
  - "The authored scene is open in the same editor instance used by MCP Unity tests"
  - "A broad test filter times out before returning a final result"
root_cause: test_isolation
resolution_type: workflow_improvement
tags:
  - "unity"
  - "editmode-tests"
  - "test-isolation"
  - "active-scene"
  - "map-tool"
  - "mcp-unity"
  - "scene-safety"
---

# Protect Active Unity Scenes from Broad EditMode Test Runs

## Context

Running the complete `NoryangjinMapToolGridUtilityTests` fixture through the open Unity editor timed out at the MCP boundary. The editor continued working after the request timed out, temporarily switched to an untitled test scene, and later returned to `Noryangjin_MapTool_Mode`.

The run also saved test-created prefab instances and active-state changes into the authored map-tool scene. The scene had been clean before verification, but afterward `git diff` showed hundreds of unrelated YAML lines. Narrow single-test filters completed normally and did not leave the same broad mutation footprint.

A later narrow `PlayerCharacterDefaultsTests` EditMode run exposed a separate risk: even tests that only create and destroy temporary `GameObject` instances can save an already-dirty authored scene before the test starts. The connector's `allow_dirty_scenes` option bypasses only its own preflight guard. Unity Test Framework still schedules `SaveModifiedSceneTask`, which calls `EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()` for EditMode runs.

The same failure mode recurred while verifying a single enemy-facing regression test. `Noryangjin_MapTool_Mode` already had unsaved authoring work, the test request timed out, and subsequent MCP scene queries also timed out while Unity remained responsive. This is consistent with the Test Runner waiting behind the modal save decision rather than executing the test.

Two tempting signals were insufficient:

- An MCP timeout did not mean the Unity test run had stopped.
- `get_scene_info` reporting a clean scene meant the in-memory scene was saved, not that the saved asset still matched the repository baseline.

## Guidance

### Record the scene baseline before broad editor tests

Before running a fixture that can touch scene objects, record:

```powershell
git status --short -- Assets/ShooterSurvival/Scenes/Tools/Noryangjin_MapTool_Mode.unity
git diff --stat -- Assets/ShooterSurvival/Scenes/Tools/Noryangjin_MapTool_Mode.unity
```

For higher-risk generation tests, also record the scene file hash. A known-clean baseline makes it possible to distinguish test pollution from user work.

### Prefer the narrowest filter that proves the change

For a small editor-tool change, run the exact new tests first:

```text
NoryangjinMapToolGridUtilityTests.PlacedObjectHeightLabel_IsClickableOnlyInSelectionMode
NoryangjinMapToolGridUtilityTests.PlacedObjectHeightLabel_ClickRectWrapsLabelCenterWithPadding
NoryangjinMapToolGridUtilityTests.PlacementSummary_PrefersSelectedPlacedObjectOverCursorObject
```

Use the broad fixture only when its additional coverage is necessary. Scene-mutating tests should create an isolated preview scene and close it in `finally`, rather than depend on whichever authored scene is active:

```csharp
Scene previewScene = EditorSceneManager.NewPreviewScene();
try
{
    // Arrange and verify scene-coupled behavior inside previewScene.
}
finally
{
    EditorSceneManager.ClosePreviewScene(previewScene);
}
```

### Do not treat `allow_dirty_scenes` as save protection

Leave `allow_dirty_scenes` unset when an authored scene has unsaved changes. A blocked request is safer than letting Unity Test Framework enter its own save-scene task. The connector flag permits the run; it does not guarantee that the scene remains only in memory.

For unattended verification, use one of these safe states before calling `run_tests`:

- the user has intentionally saved the open scene;
- the user has intentionally discarded its pending edits;
- the test runs in a separate project copy or editor instance where saving cannot overwrite active authoring work.

If the scene must remain dirty, stop at compilation or another read-only verification step instead of forcing the in-process Test Runner through the connector.

If a run has already reached the modal save decision, preserve the authored work by having the user choose Cancel. Use Don't Save only when the user explicitly intends to discard those scene edits. Do not send blind keyboard input, terminate Unity, or restart the editor when the prompt cannot be observed reliably; those recovery attempts can discard the unsaved scene that the guardrail is meant to protect.

### Treat connector timeout and editor completion as separate states

After a timeout, do not immediately start another test run. Check `Editor.log`, the Unity process state, and the active-scene title until the original run finishes and the authored scene is active again. Then retry only the narrow, idempotent test command.

### Verify repository state after tests

Always compare the scene asset to its pre-test baseline:

```powershell
git status --short
git diff --check
git diff --stat
```

If the scene was clean before the run and only test-created changes are present, remove exactly that diff and reload the scene from disk. If the scene was already modified, do not restore it wholesale; isolate the test-owned objects or stop for user direction.

## Why This Matters

Unity's EditMode test runner shares an editor process with the authored project. Tests that use the active scene, global hierarchy lookup, placement helpers, or save APIs can mutate real assets even though the tests themselves pass. A transport timeout makes this more dangerous because the caller may assume the command failed while Unity continues executing and saving.

Narrow filters reduce exposure, but real isolation comes from preview scenes, exact cleanup in `finally`, and repository-state checks around the run. These checks protect both manual scene work and the credibility of test evidence.

## When to Apply

- Verifying Unity editor tools through MCP Unity or another in-process test runner.
- Running tests that instantiate prefabs or toggle map-tool work objects.
- Testing builders that call `EditorSceneManager.SaveScene`.
- Retrying after a test request times out while Unity remains responsive.

## Examples

In the observed run, the broad fixture request timed out while Unity continued processing. Once the editor returned to the authored scene, the scene diff contained test-positioned puffer enemies, fish scraps, buoys, and active-state toggles. Fourteen reverse hunks removed the first group, and a fresh diff removed the remaining three hunks; reloading the scene then restored the clean authored state.

The three exact regression tests each returned `1/1 passed`, and the final editor script compilation completed with zero errors and warnings. This was sufficient evidence for the selection-label and thumbnail changes without rerunning the mutation-heavy fixture.

Also avoid this unattended request while a scene is dirty:

```json
{
  "command": "run_tests",
  "params": {
    "mode": "EditMode",
    "filter": "PlayerCharacterDefaultsTests",
    "allow_dirty_scenes": true
  }
}
```

It may pass all tests while persisting the scene's existing in-memory edits to disk during Test Runner setup.

In the later enemy-facing run, the safe fallback was to leave the dirty scene untouched, stop retrying MCP commands, compile both runtime and editor assemblies, and report the Unity test as pending rather than claiming a pass.

## Related

- [Generate Unity Map-Tool Sibling Scenes with Fail-Closed Verification](generate-unity-map-tool-sibling-scenes-fail-closed-2026-07-15.md)
- [Create a Unity Layout Scene When Editor Execution Is Blocked](create-unity-layout-scene-when-editor-execution-is-blocked-2026-05-25.md)
- [Read Unity Scene YAML Positions by Object Kind](../test-failures/read-unity-scene-yaml-positions-by-object-kind-2026-05-25.md)
