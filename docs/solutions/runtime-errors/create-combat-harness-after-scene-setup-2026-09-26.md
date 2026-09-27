---
title: Create Combat Harness after scene setup
date: 2026-09-26
category: runtime-errors
module: Combat Harness lifecycle
problem_type: runtime_error
component: development_workflow
severity: medium
symptoms:
  - "Starting HighWay reports Some objects were not cleaned up when closing the scene, listing Combat Harness."
root_cause: async_timing
resolution_type: code_fix
tags: [unity, combat-harness, play-mode, scene-cleanup, domain-reload]
---

# Create Combat Harness after scene setup

## Problem and evidence

Unity 6000.2.6f1 reported an uncleaned `Combat Harness` when entering Play Mode in HighWay. Both domain and scene reload were disabled (`enterPlayModeOptionsEnabled=true`, options `3`), and the map-tool start-scene override pointed to HighWay.

The existing Editor.log repeatedly showed this sequence, including lines 171288–171305 before the fix:

```text
[CombatHarness] Bootstrap created runtime instance.
CombatHarness.EnsureHarnessInstance()
CombatHarness.EnsureOnEnterPlayMode(EnterPlayModeOptions)
Entering Playmode with Reload Domain disabled.
Some objects were not cleaned up when closing the scene.
The following scene GameObjects were found:
Combat Harness
```

The engine's question about `OnDestroy` was generic. The actual creation site was the editor's `[InitializeOnEnterPlayMode]` callback, which ran during the transition before runtime scene setup. Creating a scene object there put it into the outgoing scene's cleanup window.

## Solution

Remove `EnsureOnEnterPlayMode` from `Assets/ShooterSurvival/Scripts/Harness/CombatHarness.cs`. Retain the existing `[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]` bootstrap, duplicate lookup and `DontDestroyOnLoad` behavior. Add a comment explaining why the earlier editor callback must not create objects.

The runtime initialization callback also runs when entering Play Mode with reloads disabled. Unity documents runtime initialization in the Editor in its [RuntimeInitializeOnLoadMethod reference](https://docs.unity.com/en-us/engine/6000.5/script-reference/unityengine/runtimeinitializeonloadmethodattribute); actual behavior was verified on this project's 6000.2.6f1 Editor. No teardown suppression or reload-setting change was needed.

## Verification

- `dotnet build Assembly-CSharp.csproj -nologo -v:q`: passed, zero errors; one existing `SplineSpeed.m_LastIndex` CS0649 warning.
- `dotnet build Assembly-CSharp-Editor.csproj -nologo -v:q`: passed, zero errors and warnings.
- Official Pipeline `recompile` / `recompile_status`: completed. Reflection confirmed zero `InitializeOnEnterPlayMode` hooks on `CombatHarness` in the loaded assembly.
- Two live HighWay `editor_play` / `editor_stop` cycles, retaining options `3` and the start-scene override. Each run had exactly one active harness in `DontDestroyOnLoad`; each exit left zero harnesses and a clean authored scene.
- Each run's captured error console was empty. New Editor.log output after the baseline at line 173200 showed two successful `Bootstrap` creation paths and no scene-cleanup error or exception.
- HighWay and RestStop scene SHA-256 hashes matched the pre-verification baseline. Final Editor state: HighWay open, not playing, not dirty.

This was live lifecycle verification, not a Unity Test Runner suite. Other reload-option combinations were not exercised. Existing wave utility tests cover wave arithmetic rather than editor scene transitions, so they could not detect this failure.

## Prevention

Use editor enter-play initialization for resetting editor state, not creating runtime scene objects. Keep runtime object creation after scene setup. For regression checks, enter and exit the affected map twice with the same start-scene override and reload settings; check both cleanup errors and the one-during-play/zero-after-exit object counts. Checking only that the error vanished could hide a harness that no longer starts.

## Related

- [Protect active Unity scenes from broad EditMode test runs](../workflow-issues/protect-active-unity-scenes-from-broad-editmode-test-runs-2026-07-18.md)
- [Map-tool start-stage implementation](../../exec-plans/completed/maptool-start-stage-2026-09-25.md)
