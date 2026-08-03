---
title: Run Unity-generated dotnet builds sequentially
date: 2026-07-28
last_updated: 2026-08-03
category: workflow-issues
module: Unity generated C# build verification
problem_type: workflow_issue
component: development_workflow
severity: low
applies_when:
  - "Validating Assembly-CSharp.csproj and Assembly-CSharp-Editor.csproj from the same Unity workspace"
  - "Orchestrating repository checks that normally benefit from parallel execution"
resolution_type: workflow_improvement
tags:
  - unity
  - dotnet-build
  - msbuild
  - file-lock
  - verification
---

# Run Unity-generated dotnet builds sequentially

## Context

The runtime and editor Unity project files look like independent build targets, so it is tempting to launch both `dotnet build` commands in parallel. Unity generates them with shared dependencies and output under `Temp/bin` and `Temp/obj`, however. Concurrent builds can race while writing `Assembly-CSharp.dll` and fail with:

```text
error CS2012: Cannot open 'Temp/obj/Assembly-CSharp/Assembly-CSharp.dll'
for writing because it is being used by another process.
```

The failure is transient and does not indicate a C# compilation error.

## Guidance

Run the repository's two documented build checks sequentially:

```powershell
dotnet build Assembly-CSharp.csproj -nologo
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

dotnet build Assembly-CSharp-Editor.csproj -nologo
exit $LASTEXITCODE
```

Do not place these commands in the same parallel tool batch. If a parallel attempt already produced `CS2012`, let the build commands return and rerun them sequentially. Persistent MSBuild compiler-server processes are normal; do not kill them or clear Unity's `Temp` directory unless the lock remains after all active builds have exited.

## Why This Matters

`Assembly-CSharp-Editor.csproj` depends on the runtime assembly, so both invocations may build or copy the same runtime output. Serializing just these two checks removes nondeterministic file contention while keeping unrelated read-only validation parallelizable. It also distinguishes an orchestration failure from a real compiler diagnostic.

## When to Apply

- Both Unity-generated project files are validated in one workflow.
- Multiple checks share the workspace's `Temp/bin` or `Temp/obj` paths.
- A build reports `CS2012` for `Assembly-CSharp.dll` while another build is still active.

## Examples

Avoid:

```text
Promise.all([
  dotnet build Assembly-CSharp.csproj,
  dotnet build Assembly-CSharp-Editor.csproj
])
```

Use:

```text
build runtime -> confirm exit -> build editor
```

This recurred while verifying the missile speed-and-duration change on 2026-08-02: the parallel run failed with `CS2012`, then the immediate sequential rerun completed both projects with zero errors. The runtime build retained five unrelated existing warnings, while the Editor build was clean.

It recurred again while verifying selection-driven enemy-trigger mapping on
2026-08-03. The parallel run locked the same
`Temp/obj/Assembly-CSharp/Assembly-CSharp.dll`; the immediate sequential rerun
completed both projects with zero warnings and zero errors. This confirms that
the lock is an orchestration failure even when the codebase itself compiles
cleanly.

## Related

- [Make reference-scene gameplay composition transactional and idempotent in authored Unity maps](../integration-issues/transactional-reference-scene-gameplay-composition-2026-07-23.md) records the same transient lock as one verification pitfall during a larger integration effort.
