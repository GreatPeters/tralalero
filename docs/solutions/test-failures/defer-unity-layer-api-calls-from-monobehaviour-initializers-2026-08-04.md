---
title: Defer Unity layer API calls from MonoBehaviour initializers
date: 2026-08-04
category: test-failures
module: Noryangjin runtime cleanup
problem_type: test_failure
component: testing_framework
symptoms:
  - "UnityException reports that NameToLayer cannot run from a MonoBehaviour constructor or field initializer"
  - "An unrelated prefab contract test fails because Unity records the lifecycle exception as an unhandled log"
root_cause: wrong_api
resolution_type: code_fix
severity: medium
related_components: [development_workflow, tooling]
tags: [unity, monobehaviour, field-initializer, layermask, editmode-tests, prefab-loading]
---

# Defer Unity layer API calls from MonoBehaviour initializers

## Problem

`ExtraHelpBuffScript` cached its enemy layer mask with a static field
initializer. C# compilation succeeded, but an EditMode prefab test later failed
while Unity constructed the component.

## Symptoms

- Unity logged `NameToLayer is not allowed to be called from a MonoBehaviour
  constructor (or instance field initializer)`.
- The failure appeared while loading a different prefab because asset loading
  determines when the component type is constructed and the pending log is
  observed.
- Both generated `.csproj` builds passed; this restriction is enforced by the
  Unity runtime rather than the C# compiler.

## What Didn't Work

Treating `LayerMask.GetMask("Enemy")` as a harmless pure calculation was
incorrect. It calls Unity's layer lookup internally, so moving it into a
`static readonly` initializer only moved work earlier into a forbidden engine
lifecycle phase.

## Solution

Keep the cached mask as ordinary runtime state and initialize it from
`Awake()`, where Unity API calls are permitted:

```csharp
private int enemyLayerMask;

private void Awake()
{
    enemyLayerMask = LayerMask.GetMask("Enemy");
    // Other component initialization...
}
```

The physics query continues using the cached integer, so no layer-name lookup
is added to the per-frame enemy search.

## Why This Works

Unity controls `MonoBehaviour` construction and serialization. Static and
instance field initializers can run while the native object is not ready for
engine API access. `Awake()` runs after Unity has created the component, so the
layer lookup is legal while retaining the intended one-time cache.

## Prevention

- Restrict `MonoBehaviour` field initializers to constants, primitive values,
  and managed allocations that do not call `UnityEngine.Object` APIs.
- Initialize layer names, tags, scene objects, components, resources, and other
  Unity-owned state in `Awake()`, `OnEnable()`, or `Start()` according to the
  required lifetime.
- Keep EditMode tests that load the real prefabs. Unhandled Unity logs catch
  lifecycle violations that `dotnet build` cannot detect.
- When a prefab test reports an exception from a seemingly unrelated asset,
  inspect static and instance initializers on every component Unity loaded
  before weakening the test's log assertions.

## Related Issues

- [Run Unity-generated dotnet builds sequentially](../workflow-issues/run-unity-generated-dotnet-builds-sequentially-2026-07-28.md)
- [Verify Unity API removals with a full Assets search and build](../workflow-issues/verify-unity-api-removals-with-full-assets-search-and-build-2026-08-04.md)
