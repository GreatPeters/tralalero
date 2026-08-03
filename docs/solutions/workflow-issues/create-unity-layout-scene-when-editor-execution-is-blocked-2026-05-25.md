---
title: Create Unity Layout Scenes When Editor Execution Is Blocked
date: 2026-05-25
last_updated: 2026-05-26
category: docs/solutions/workflow-issues
module: Unity scene generation workflow
problem_type: workflow_issue
component: tooling
severity: medium
applies_when:
  - "Unity MCP transport is closed while a scene must be generated"
  - "The MCP tool wrapper is closed but Unity's WebSocket server is still reachable"
  - "Unity batchmode fails because the project is already open"
  - "A preview scene can be generated from existing prefab/model GUIDs"
tags: [unity, scene-yaml, mcp, websocket, batchmode, meshyai]
---

# Create Unity Layout Scenes When Editor Execution Is Blocked

## Context
A Stage01 Noryangjin preview scene needed to be generated from existing MeshyAI assets. The preferred path was an editor utility using `EditorSceneManager` and `PrefabUtility`, triggered by a one-shot request file. That path did not run because the open Unity editor was not auto-refreshing, and a direct `Unity.exe -batchmode -executeMethod` call failed because the project was already open.

## Guidance
Keep the editor utility as the preferred path, but remove one-shot request files if the editor does not process them promptly. A stale request can later run unexpectedly and replace the user's current scene.

Before falling back to scene YAML or a duplicate batchmode project, check whether Unity's WebSocket server is still reachable directly. Read `ProjectSettings/McpUnitySettings.json` for the current port and send the same JSON request shape the MCP bridge uses:

```json
{"id":"codex-check","method":"get_scene_info","params":{}}
```

This can still execute menu items, load scenes, save scenes, and run filtered tests even when the Codex-exposed MCP transport reports `Transport closed`.

If the open editor starts accepting WebSocket messages but stops returning responses, use the duplicate short-path batchmode project for generation again. Keep that duplicate project scoped to scene generation unless it has the same test dependencies as the real project. A minimal duplicate `Packages/manifest.json` can compile editor builders, but copying `Assets/Tests/Editor/*.cs` into it without `com.unity.test-framework` will fail on `using NUnit.Framework`.

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

The Stage01_2 rebuild used a direct WebSocket call on the project-configured port to confirm the active scene and run the filtered Unity tests after the Codex MCP transport had closed:

```text
get_scene_info -> Active scene: 'Stage01_2_Noryangjin_AutoDraft'
run_tests Stage01NoryangjinSecondAutoDraftBuilderTests -> 4/4 passed
```

A later Stage01_2 rebuild used the duplicate `C:\tmp` project to run the builder when the open editor WebSocket accepted `execute_menu_item` but never returned a result. The generated scene was copied back to the real project and verified with repository-side YAML assertions; the copied test file was removed from the duplicate project because that minimal project did not include Unity's test framework package.

## Related
- [Repair Unity assets when editor command execution is blocked](repair-unity-assets-when-editor-command-path-is-blocked-2026-05-24.md)
