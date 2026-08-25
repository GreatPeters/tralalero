---
title: Create Unity Layout Scenes When Editor Execution Is Blocked
date: 2026-05-25
last_updated: 2026-08-23
category: docs/solutions/workflow-issues
module: Unity scene generation workflow
problem_type: workflow_issue
component: tooling
severity: medium
applies_when:
  - "The official Unity Pipeline endpoint is temporarily unavailable while a scene must be generated"
  - "Unity batchmode fails because the project is already open"
  - "A preview scene can be generated from existing prefab/model GUIDs"
tags: [unity, scene-yaml, unity-pipeline, batchmode, meshyai]
---

# Create Unity Layout Scenes When Editor Execution Is Blocked

## Context
A Stage01 Noryangjin preview scene needed to be generated from existing MeshyAI assets. The preferred path was an editor utility using `EditorSceneManager` and `PrefabUtility`, triggered by a one-shot request file. That path did not run because the open Unity editor was not auto-refreshing, and a direct `Unity.exe -batchmode -executeMethod` call failed because the project was already open.

## Guidance
Keep the editor utility as the preferred path, but remove one-shot request files if the editor does not process them promptly. A stale request can later run unexpectedly and replace the user's current scene.

Before falling back to scene YAML or a duplicate batchmode project, check the
official editor connection directly:

```powershell
unity pipeline list
unity command --project-path . list_open_scenes
unity status --project-path .
```

If Pipeline is resolving packages or recompiling, wait and retry. If the editor
is in Safe Mode, fix the reported compiler errors and restart it. The removed
CoderGamester WebSocket and `McpUnitySettings.json` are not recovery paths.
Treat `unity status` as supplemental: it can report `STATUS_NO_INSTANCES` while
the narrow command works, so Pipeline reachability plus the command result is
authoritative. The separate `com.youngwoocho02.unity-cli-connector` package
remains installed for pre-existing workflows but is not a Codex MCP fallback.

Historical connector note: before the Pipeline migration, the duplicate
short-path batchmode project was used when a legacy WebSocket accepted messages
but stopped returning responses. For current work, reach that fallback only
after `unity pipeline list`, a narrow official command, package resolution, and
Safe Mode checks all fail. Keep the duplicate project scoped to scene generation
unless it has the same test dependencies as the real project. A minimal
duplicate `Packages/manifest.json` can compile editor builders, but copying
`Assets/Tests/Editor/*.cs` into it without `com.unity.test-framework` will fail
on `using NUnit.Framework`.

When a rough preview scene is still needed, generate a direct `.unity` fallback only from stable source GUIDs:

- Parse the generated prefab assets to find their `m_SourcePrefab` FBX GUID.
- Emit scene `PrefabInstance` records that reference the FBX source prefab.
- Override position, rotation, scale, and display name in `m_Modifications`.
- Create the `.unity.meta` and folder `.meta` files so Unity can import the scene later.
- Verify every emitted source GUID exists under the expected model folder.

## Why This Matters
Unity command execution can be unavailable even when the project is open. A direct scene YAML fallback is not as rich as a live `PrefabUtility` pass, but it can produce a reviewable preview scene without closing the user's editor or destroying unsaved work.

## When to Apply
- The task is preview/layout generation, not runtime-critical scene authoring.
- Existing prefab or FBX GUIDs are already stable.
- The layout can tolerate approximate scale and rotation until Unity reimports and artists review it.

## Examples
The Stage01 draft fallback wrote:

```text
Assets/ShooterSurvival/Scenes/Generated/Stage01_Noryangjin_AutoDraft.unity
PrefabInstanceCount=164
MissingSourceGuids=0
```

The editor utility remains useful for a later clean rebuild:

```text
Tools/맵 제작 도구/자동 생성/Stage01 노량진 초안 씬
```

Historical note: before the Pipeline migration, one Stage01_2 rebuild used the
now-removed direct WebSocket bridge to confirm the active scene and run tests.
The current equivalent is:

```text
unity command --project-path . list_open_scenes
unity command --project-path . run_tests --mode editor --filter Stage01NoryangjinSecondAutoDraftBuilderTests
```

A later Stage01_2 rebuild used the duplicate `C:\tmp` project to run the builder when the open editor WebSocket accepted `execute_menu_item` but never returned a result. The generated scene was copied back to the real project and verified with repository-side YAML assertions; the copied test file was removed from the duplicate project because that minimal project did not include Unity's test framework package.

## Related
- [Adopt Official Unity CLI and Pipeline as the Codex Editor-Control Path](../tooling-decisions/adopt-official-unity-cli-pipeline-as-codex-editor-control-path-2026-08-23.md)
- [Repair Unity assets when editor command execution is blocked](repair-unity-assets-when-editor-command-path-is-blocked-2026-05-24.md)
