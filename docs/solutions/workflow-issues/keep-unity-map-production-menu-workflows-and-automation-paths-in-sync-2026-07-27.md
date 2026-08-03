---
title: Keep Unity Map Production Menu Workflows and Automation Paths in Sync
date: 2026-07-27
last_updated: 2026-07-27
category: docs/solutions/workflow-issues
module: Unity map production editor menu workflow
problem_type: workflow_issue
component: tooling
severity: medium
applies_when:
  - "Reorganizing Unity MenuItem paths for a user-facing workflow"
  - "Adding or moving Tools/맵 제작 도구 commands or folder shortcuts"
  - "Changing menu paths referenced by CLI automation or operational guides"
tags: [unity, map-production, editor-menu, menuitem, automation, regression-tests, documentation]
---

# Keep Unity Map Production Menu Workflows and Automation Paths in Sync

## Context

The map-production menu mixed map authoring, generated-scene builders, repair utilities, and reference-folder commands at one level. Moving it to `Tools/맵 제작 도구` and grouping those commands made the human workflow clearer, but it also changed paths used as identifiers by Unity CLI automation and operational documentation.

A successful editor build did not reveal two gaps found during review: there was no regression test for the complete menu topology, and several CLI examples still invoked retired paths.

The same contract applies when adding a command rather than renaming one. The
Forward enemy movement work added
`Tools/맵 제작 도구/노량진 맵 제작/게임플레이/적 이동 기능 연결`; its code
compiled and ran, but the exact-set menu test also had to gain that path before
the workflow was complete.

## Guidance

Treat a Unity `MenuItem` path as an operational interface rather than a display-only label.

- Group commands by workflow. The current groups are `자료`, `노량진 맵 제작`, `자동 생성`, and `유지보수`.
- When adding, removing, or moving a command, update every documentation, CLI, and MCP consumer in the same change.
- Update `MapProductionToolMenuTests` in the same patch as every new
  map-production `MenuItem`; an exact-set assertion intentionally treats an
  unregistered new command as a regression.
- Lock the complete command surface with an EditMode test that reflects over the editor assembly's static methods, extracts `MenuItem` constructor arguments, and compares the full `Tools/맵 제작 도구/` path set.
- Pin operationally important folder targets separately. The Noryangjin map-plan shortcut targets `outputs/chapter_campaign_reference_orthogonal_20min`.
- Keep external JSON requests ASCII-only when menu paths contain Korean or other non-ASCII text.

## Why This Matters

Menu paths are used by people, recovery playbooks, and automation. Renaming a path can leave the command fully functional in Unity while breaking every external caller that still uses the old string. Compilation only proves that attributes and methods are valid C#; it does not prove that the intended menu surface is complete or that its consumers agree on the current paths.

Non-ASCII names introduce another boundary: a literal Korean command passed through PowerShell or a console may arrive as `???`. JSON `\uXXXX` escaping avoids that transport corruption.

## When to Apply

- A Unity editor command is renamed or moved into a submenu.
- A shortcut opens a design, output, preview, or generated-image directory.
- CLI Connector or MCP automation invokes commands by menu path.
- Destructive or rarely used commands are moved under a maintenance group.
- Korean or other non-ASCII text appears in an externally invoked menu path.

## Examples

The menu topology test should assert the exact set rather than checking only one representative command:

```csharp
return typeof(DesignReferenceWindow).Assembly
    .GetTypes()
    .SelectMany(type => type.GetMethods(
        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
    .SelectMany(method => method.CustomAttributes)
    .Where(attribute => attribute.AttributeType == typeof(MenuItem))
    .Select(attribute => attribute.ConstructorArguments[0].Value as string)
    .Where(path => path != null && path.StartsWith("Tools/맵 제작 도구/", StringComparison.Ordinal))
    .ToArray();
```

For a new command, update the attribute and the expected topology together:

```csharp
[MenuItem(
    "Tools/맵 제작 도구/노량진 맵 제작/게임플레이/적 이동 기능 연결",
    false,
    2311)]
public static void Configure()
{
    // Idempotent prefab setup.
}

string[] expected =
{
    "Tools/맵 제작 도구/노량진 맵 제작/게임플레이/적 이동 기능 연결",
    // Existing production commands...
};
```

For an external CLI call, encode the current Korean path:

```json
{"command":"menu","params":{"menu_path":"Tools/\uB9F5 \uC81C\uC791 \uB3C4\uAD6C/\uB178\uB7C9\uC9C4 \uB9F5 \uC81C\uC791/\uB9F5\uD234 \uC52C \uC5F4\uAE30 \uB610\uB294 \uC0DD\uC131"}}
```

Verification for this change consisted of:

```text
dotnet build Assembly-CSharp-Editor.csproj -nologo
Unity EditMode filter MapProductionToolMenuTests -> 2/2 passed
```

The Forward enemy movement addition repeated the check by directly invoking
`MapProductionToolMenus_AreGroupedByUserWorkflow`; it passed after the new path
was added to the expected set.

Also search the repository for retired path fragments before finishing. A historical explanation may retain an old path when it is explicitly labeled as historical, but live invocation examples must use the current path.

## Related

- [Run Unity Scene Generation Through CLI Connector Exec When Editor Reload Is Stale](run-unity-scene-generation-through-cli-connector-exec-when-editor-reload-is-stale-2026-06-21.md)
- [Call Unity CLI Connector Commands With Params Payloads](call-unity-cli-connector-commands-with-params-payloads-2026-06-06.md)
- [Update Map-Tool Road Definitions With Scene Road Replacements](../logic-errors/update-map-tool-road-definitions-with-scene-road-replacements-2026-06-15.md)
