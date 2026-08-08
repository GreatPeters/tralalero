---
title: Verify Unity API removals with a full Assets search and build
date: 2026-08-04
category: workflow-issues
module: Unity C# API refactoring
problem_type: workflow_issue
component: development_workflow
severity: medium
applies_when:
  - "Removing or renaming a public, internal, or static Unity C# API"
  - "Simplifying one gameplay mode while legacy modes remain in the project"
  - "A reviewer reports that an API appears unused"
root_cause: scope_issue
resolution_type: workflow_improvement
related_components: [testing_framework, tooling]
tags: [unity, csharp, api-removal, reference-search, build-verification, refactoring]
---

# Verify Unity API removals with a full Assets search and build

## Context

During a Noryangjin Player, Enemy, and Wall cleanup, a scoped review reported
that `CoinDropUtility.GetCoinAmount(EnemyType)` was unused. Removing the
overload looked safe inside the feature area being reviewed, but the full
runtime build failed because `BarrelScript` and the legacy `EnemyScript` still
called it.

The overload was restored immediately. The runtime and editor assemblies then
built successfully. The review finding was useful as a cleanup candidate, but
its search scope was not sufficient evidence for deletion.

## Guidance

Treat an "unused" review finding as a hypothesis. Before removing a
public/static API, search all authored C# under `Assets`, not only the current
feature directory:

```powershell
rg -n --glob '*.cs' 'GetCoinAmount\(' Assets
```

When the member may be referenced by Unity serialization, a UnityEvent,
reflection, or a string-based editor workflow, also search the serialized
asset types that can hold the reference:

```powershell
rg -n --glob '*.prefab' --glob '*.unity' --glob '*.asset' 'GetCoinAmount' Assets
```

After the change, build the complete consumer assemblies sequentially:

```powershell
dotnet build Assembly-CSharp.csproj -nologo
dotnet build Assembly-CSharp-Editor.csproj -nologo
```

The runtime build is the compiler-backed check for gameplay callers. The
editor build covers editor tools and tests. Serialized-asset searches cover
indirect references that compilation cannot prove absent.

If any retained consumer appears, keep the compatibility member or migrate
that consumer explicitly in the same change. Do not broaden a scene-specific
cleanup silently into removal of another game mode's contract.

## Why This Matters

Unity projects commonly keep multiple gameplay modes, editor tools, prefabs,
scenes, and legacy scripts in one `Assembly-CSharp` graph. A symbol can be
unused by the active scene and still be required by code that ships or by an
editor workflow.

A scoped text search and a full build answer different questions:

- The scoped search asks whether the current feature uses the member.
- The full `Assets` search asks which authored consumers mention it.
- The builds ask whether every compiled consumer still type-checks.
- Asset-contract checks ask whether serialized or name-based consumers remain.

Using these as independent checks prevents a locally correct cleanup from
breaking a different scene or mode.

## When to Apply

- Removing or renaming overloads, utility methods, properties, events, or
  shared types.
- Deleting a MonoBehaviour method that might be a Unity message or UnityEvent
  target.
- Privatizing or removing serialized fields used by prefabs, scenes, or
  ScriptableObjects.
- Simplifying one gameplay mode while other modes still compile in the same
  runtime assembly.

Private implementation details with no reflection, serialization, or Unity
message role can use a narrower check. Shared APIs should use the full flow.

## Examples

The unsafe flow was:

```text
scoped review says unused -> delete overload -> full build discovers callers
```

The safe flow is:

```text
full Assets search -> classify every caller -> make the narrow edit
-> build Assembly-CSharp -> build Assembly-CSharp-Editor
-> run relevant serialized-asset contracts
```

In this case, the failed runtime build resolved the remaining calls to the
`EnemyTier` overload and reported type-conversion errors at
`BarrelScript.cs:165` and `EnemyScript.cs:233`. Restoring the `EnemyType`
overload preserved those existing modes without weakening the Noryangjin
cleanup.

## Related

- [Run Unity-generated dotnet builds sequentially](run-unity-generated-dotnet-builds-sequentially-2026-07-28.md)
- [Reapply external prefab YAML after Unity script recompilation](reapply-prefab-yaml-after-unity-script-recompile-2026-08-03.md)
- [Keep Unity map production menu workflows and automation paths in sync](keep-unity-map-production-menu-workflows-and-automation-paths-in-sync-2026-07-27.md)
