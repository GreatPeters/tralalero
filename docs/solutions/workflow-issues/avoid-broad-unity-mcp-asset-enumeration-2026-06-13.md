---
title: Avoid broad Unity MCP asset enumeration in large projects
date: 2026-06-13
last_updated: 2026-08-23
category: docs/solutions/workflow-issues
module: Unity MCP asset workflow
problem_type: workflow_issue
component: tooling
severity: medium
applies_when:
  - "A Unity project has thousands of assets and the task only needs a narrow asset subset"
  - "A broad editor-integration query times out while a narrow file or AssetDatabase query would suffice"
  - "A Unity editor menu item already exists for the asset mutation"
tags: [unity, unity-pipeline, assetdatabase, materials, urp]
---

# Avoid broad Unity MCP asset enumeration in large projects

> Status: updated. The `unity://assets` and `unity://scenes_hierarchy` examples
> below are historical CoderGamester resource calls. That package was removed;
> use official `unity command` operations and narrow repository searches now.

## Context
A Poly Universal Pack material conversion task only needed material shader updates, but reading broad MCP resources such as `unity://assets` and `unity://scenes_hierarchy` timed out and made the open Unity editor unstable.

## Guidance
Prefer narrow project-side queries and official editor commands over
whole-project resource enumeration:

- Use shell searches scoped to the relevant asset root, for example `rg -l "m_Shader:" "Assets/polyperfect/Poly Universal Pack" -g "*.mat"`.
- Use existing Unity editor utilities for mutations that need Unity APIs, then trigger them with `unity command --project-path . menu --path "<menu-path>"`.
- Use `unity command --project-path . console --level error --tail 50` for narrow verification instead of dumping all Unity resources.
- If batchmode is blocked because the project is already open, run the menu item in the open editor rather than removing lock files.

Confirm reachability with `unity pipeline list` and one narrow successful
command. `unity status` may return `STATUS_NO_INSTANCES` despite a working
Pipeline endpoint, so it is not the authoritative reachability check.

## Why This Matters
Whole-project editor resource reads can force Unity to load or inspect too much
state at once. In large asset-heavy projects, that can time out or destabilize
the editor. Narrow file searches plus targeted menu execution keep the editor
responsive and still let Unity handle serialization details.

## When to Apply
- The task targets a known package, folder, material family, prefab group, or scene.
- The exact asset paths can be found with `rg --files`, `rg -l`, or a narrow Unity `AssetDatabase.FindAssets` call.
- A menu item or editor utility already wraps the required Unity API work.

## Examples
For Poly Universal Pack shader conversion, avoid reading all assets through an
editor bridge. Instead:

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
- [Adopt Official Unity CLI and Pipeline as the Codex Editor-Control Path](../tooling-decisions/adopt-official-unity-cli-pipeline-as-codex-editor-control-path-2026-08-23.md)
