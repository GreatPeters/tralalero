---
title: Keep Unity Map Production Menu Workflows and Automation Paths in Sync
date: 2026-07-27
last_updated: 2026-09-02
category: docs/solutions/workflow-issues
module: Unity map production editor menu workflow
problem_type: workflow_issue
component: tooling
severity: medium
applies_when:
  - "Reorganizing Unity MenuItem paths for a user-facing workflow"
  - "Adding, removing, or hiding Tools/맵 제작 도구 commands or folder shortcuts"
  - "Keeping internal Unity recovery APIs after removing their MenuItem registration"
  - "Simplifying editor convenience controls that rely on automatic import or build hooks"
  - "Changing menu paths referenced by CLI automation or operational guides"
tags: [unity, map-production, editor-menu, menuitem, automation, regression-tests, documentation, data-workbook]
---

# Keep Unity Map Production Menu Workflows and Automation Paths in Sync

## Context

The map-production menu mixed map authoring, generated-scene builders, repair utilities, and reference-folder commands at one level. Moving it to `Tools/맵 제작 도구` and grouping those commands made the human workflow clearer, but it also changed paths used as identifiers by Unity CLI automation and operational documentation.

A successful editor build did not reveal two gaps found during review: there was no regression test for the complete menu topology, and several CLI examples still invoked retired paths.

The same contract applies when adding or removing a command. In August 2026 the
authoring surface was deliberately reduced to two entries: `자료 위치 안내` and
`맵툴 열기`. Scene creation, Forward installation, enemy repair, optimization,
and maintenance methods remained callable internal APIs, but their `MenuItem`
attributes were removed.

In September 2026, a user-facing `문서` submenu added direct openers for the
three canonical root documents and the specialized map plan. This expanded the
intentional surface to six entries without restoring any recovery or maintenance commands.

That pruning also simplified the map tool's `편의` tab to one workbook action,
`Data.xlsx 열기`. This was safe because workbook imports still reload editor
tables automatically and `GameDataBuildPreprocessor` still generates and
validates the protected archive before player builds. Review caught two kinds
of documentation drift that compilation could not: the Forward guide still
read like an installation command was visible, and the optimization guide
initially implied the Map1-only UI optimizer also covered Map2.

## Guidance

Treat a Unity `MenuItem` path as an operational interface rather than a display-only label.

- Keep the visible map-production surface intentional. The current complete set is `Tools/맵 제작 도구/문서/기획서 열기`, `Tools/맵 제작 도구/문서/밸런스 문서 열기`, `Tools/맵 제작 도구/문서/개발 문서 열기`, `Tools/맵 제작 도구/문서/맵 기획서 열기`, `Tools/맵 제작 도구/자료/자료 위치 안내`, and `Tools/맵 제작 도구/노량진 맵 제작/맵툴 열기`.
- Remove UI registration at the attribute boundary. If automation or recovery still needs the operation, remove `[MenuItem]` but retain the underlying `public static` method.
- When adding, removing, or moving a command, update every documentation, CLI, and MCP consumer in the same change.
- Update `MapProductionToolMenuTests` in the same patch as every menu-surface change. An exact-set assertion intentionally treats both a missing intended command and an unexpected additional command as regressions.
- Lock the complete command surface with an EditMode test that reflects over the editor assembly's static methods, extracts `MenuItem` constructor arguments, and compares the full `Tools/맵 제작 도구/` path set.
- For immediate-mode editor UI, keep a separate exact contract for action labels. The map-tool workbook area should expose only `Data.xlsx 열기`; manual archive generation and validation remain diagnostic `Tools/Data` commands.
- Document automatic replacements before removing manual controls. Import hooks own editor refresh; build preprocessors own player-build archive generation and validation.
- Keep an agent invocation convention for hidden methods. After checking scene, dirty-state, and Play Mode preconditions, agents can invoke a documented static method with `unity command eval "<Type.Method>();" --project-path .`.
- State scope boundaries explicitly. `MobileUiOptimizerWindow` applies the scene-static pass to Map1; Map2 automation activates Map2 and invokes `NoryangjinMapStaticOptimizer.OptimizeCurrentScene()` through Pipeline.
- Keep external JSON requests ASCII-only when menu paths contain Korean or other non-ASCII text.

## Why This Matters

Menu paths are used by people, recovery playbooks, and automation. Renaming a path can leave the command fully functional in Unity while breaking every external caller that still uses the old string. Conversely, deleting a method merely because its menu item is unwanted can remove a valid recovery capability. Compilation only proves that the remaining C# is valid; it does not prove that the intended menu surface, hidden automation surface, documentation, and replacement workflows agree.

The same distinction applies to workbook buttons. Removing manual refresh and
validation controls is safe only while automatic import reload and build-time
archive protection remain verified. A stale guide can effectively resurrect a
retired workflow by telling users or agents to look for an obsolete command.

Non-ASCII names introduce another boundary: a literal Korean command passed through PowerShell or a console may arrive as `???`. JSON `\uXXXX` escaping avoids that transport corruption.

## When to Apply

- A Unity editor command is renamed or moved into a submenu.
- A routine author menu is being reduced while recovery and automation methods must remain callable.
- A shortcut opens a design, output, preview, or generated-image directory.
- CLI Connector or MCP automation invokes commands by menu path.
- Destructive or rarely used commands are removed from the visible menu.
- Manual editor controls are replaced by import hooks or build preprocessors.
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

For a hidden recovery API, remove the attribute without deleting the method:

```csharp
public static void Configure()
{
    // Idempotent prefab repair remains callable by tests and Pipeline.
}

string[] expected =
{
    "Tools/맵 제작 도구/문서/개발 문서 열기",
    "Tools/맵 제작 도구/문서/기획서 열기",
    "Tools/맵 제작 도구/문서/맵 기획서 열기",
    "Tools/맵 제작 도구/문서/밸런스 문서 열기",
    "Tools/맵 제작 도구/노량진 맵 제작/맵툴 열기",
    "Tools/맵 제작 도구/자료/자료 위치 안내"
};
```

The workbook convenience action can use a separate exact label contract:

```csharp
CollectionAssert.AreEquivalent(
    new[] { "Data.xlsx 열기" },
    gameDataButtonLabels);
```

For an internal automation call, avoid the retired menu path entirely:

```powershell
unity command eval "ForwardEnemyMovementSetup.Configure();" --project-path .
```

Verification for this change consisted of:

```text
dotnet build Assembly-CSharp.csproj -nologo
dotnet build Assembly-CSharp-Editor.csproj -nologo
powershell -ExecutionPolicy Bypass -File tools/validate-agent-harness.ps1
static map-production MenuItem count -> 6
MapProductionToolMenuTests -> 7 fixtures expected
Unity Pipeline targeted run -> timed out after 300 seconds; no test verdict
```

During the earlier July menu-pruning pass, the narrow Unity EditMode invocation
produced no verdict because the authored Noryangjin scene was already dirty and
the Pipeline test request timed out. Do not save or rewrite a user's authored
scene merely to make verification run. The September document-menu pass kept
that scene dirty and unchanged while the six narrow menu fixtures completed
successfully through the reachable Pipeline server.

Also search the repository for retired path fragments before finishing. A historical explanation may retain an old path when it is explicitly labeled as historical, but live invocation examples must use the current path.

## Related

- [Run Unity Scene Generation Through CLI Connector Exec When Editor Reload Is Stale](run-unity-scene-generation-through-cli-connector-exec-when-editor-reload-is-stale-2026-06-21.md)
- [Call Unity CLI Connector Commands With Params Payloads](call-unity-cli-connector-commands-with-params-payloads-2026-06-06.md)
- [Protect active Unity scenes from broad EditMode test runs](protect-active-unity-scenes-from-broad-editmode-test-runs-2026-07-18.md)
- [Verify Unity API removals with a full Assets search and build](verify-unity-api-removals-with-full-assets-search-and-build-2026-08-04.md)
- [Update Map-Tool Road Definitions With Scene Road Replacements](../logic-errors/update-map-tool-road-definitions-with-scene-road-replacements-2026-06-15.md)
