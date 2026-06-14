---
title: Avoid broad Unity MCP asset enumeration in large projects
date: 2026-06-13
category: docs/solutions/workflow-issues
module: Unity MCP asset workflow
problem_type: workflow_issue
component: tooling
severity: medium
applies_when:
  - "A Unity project has thousands of assets and the task only needs a narrow asset subset"
  - "MCP Unity whole-project resources such as unity://assets or unity://scenes_hierarchy time out"
  - "A Unity editor menu item already exists for the asset mutation"
tags: [unity, mcp, assetdatabase, materials, urp]
---

# Avoid broad Unity MCP asset enumeration in large projects

## Context
A Poly Universal Pack material conversion task only needed material shader updates, but reading broad MCP resources such as `unity://assets` and `unity://scenes_hierarchy` timed out and made the open Unity editor unstable.

## Guidance
Prefer narrow project-side queries and editor menu execution over whole-project MCP resource enumeration:

- Use shell searches scoped to the relevant asset root, for example `rg -l "m_Shader:" "Assets/polyperfect/Poly Universal Pack" -g "*.mat"`.
- Use existing Unity editor utilities for mutations that need Unity APIs, then trigger them through `execute_menu_item`.
- Use `get_console_logs` with `includeStackTrace: false` for verification instead of dumping all Unity resources.
- If batchmode is blocked because the project is already open, run the menu item in the open editor rather than removing lock files.

## Why This Matters
Whole-project Unity MCP resource reads can force Unity to load or inspect too much state at once. In large asset-heavy projects, that can time out, disconnect the MCP bridge, or destabilize the editor. Narrow file searches plus targeted menu execution keep the editor responsive and still let Unity handle serialization details.

## When to Apply
- The task targets a known package, folder, material family, prefab group, or scene.
- The exact asset paths can be found with `rg --files`, `rg -l`, or a narrow Unity `AssetDatabase.FindAssets` call.
- A menu item or editor utility already wraps the required Unity API work.

## Examples
For Poly Universal Pack shader conversion, avoid reading all assets through MCP. Instead:

```text
Tools/Polyperfect/Convert Poly Universal Pack Materials To URP
```

Then verify narrowly:

```powershell
rg -n "m_Shader: \{fileID: (45|46), guid: 0000000000000000f000000000000000, type: 0\}" "Assets/polyperfect/Poly Universal Pack" -g "*.mat"
```

No output from that search means the Built-in Standard and Standard Specular references in that package are gone.

## Related
- [Repair Unity assets when editor command execution is blocked](repair-unity-assets-when-editor-command-path-is-blocked-2026-05-24.md)
- [Create Unity Layout Scenes When Editor Execution Is Blocked](create-unity-layout-scene-when-editor-execution-is-blocked-2026-05-25.md)
