---
title: Prewarm bounded combat presentation pools before gameplay
date: 2026-09-13
category: performance-issues
module: Unity mobile combat presentation
problem_type: performance_issue
component: service_object
symptoms:
  - "Damage and coin numbers created canvases, text components, raycasters and tween closures on every event."
  - "Every enemy bullet impact instantiated a particle prefab and scheduled its destruction."
  - "A 32-popup call burst took 17.5637 ms in the connected Editor."
root_cause: logic_error
resolution_type: code_fix
severity: high
tags: [unity, mobile, performance, pooling, particles, tmp, lifecycle]
---

# Prewarm bounded combat presentation pools before gameplay

## Problem

Projectile pooling did not cover the presentation triggered by each projectile. DamagePopupFX performed a scene search, constructed a Canvas/CanvasScaler/GraphicRaycaster hierarchy, cloned TMP text, allocated tween closures and scheduled destruction for each number. EnemyScript_space separately instantiated and destroyed its hit particle prefab on every impact. Bursts multiplied these main-thread costs.

## Symptoms

In Unity 6000.2.6f1, a warmed call site creating 32 numeric popups took17.5637ms. The active scene's50 authored enemies shared the same two-second WALKER_HitEffect particle prefab. The Android/iOS Mobile render pipeline also used an8192 main-light shadow map.

These are code-path and configuration findings. No phone was connected; they do not establish the cause or elimination of every device hitch.

## What Didn't Work

- A Roslyn probe referencing Newtonsoft.Json failed because two loaded Editor assemblies provided JsonConvert. Use a serializable report with JsonUtility, or plain scalar text for small timing results.
- GC.GetAllocatedBytesForCurrentThread returned0 even for the allocating baseline. That measurement was rejected; do not report allocation-free gameplay from it.
- Native test filtering accepts a partial name, not a pipe-separated expression. The combined filter returned0tests; individual class runs supplied the actual54passing tests.
- A destruction test created its MonoBehaviour only in Edit Mode, where normal Play Mode lifecycle callbacks were not delivered. The unit test explicitly invokes the cleanup callback; native scene changes and the separate live teardown probe exercise the runtime boundary.
- The initial600-frame SR18 probe was stopped before writing its report. It is an incomplete attempt, superseded by the completed180-frame run. A successful start response is not completed evidence.

## Solution

- CanvasScript prewarms64 scene-owned DamagePopupPool entries in the lobby. Each retains its original world-space text styling, numeric color/scale and .75-second rise/fade. A single update loop replaces per-popup tweens, numeric SetText replaces per-hit strings, and presentation text does not receive raycasts.
- EnemyScript_space prewarms32 effects per distinct hit prefab during Awake. EnemyHitEffectPool retains attachment transforms and the original main-system duration, stops/clears particles before reuse, returns effects when the target becomes inactive, and removes active detached children when its owner is destroyed.
- Both pools recycle their oldest cosmetic entry under saturation. Projectile delivery, damage, score and currency still resolve normally. Future destroyed enemy roots can destroy attached effects; the next rental recreates only that missing slot.
- The Mobile RP Asset main-light shadow resolution is2048. MSAA4, render scale0.8, materials, colliders, camera framing and balance values were retained. This reduces shadow-map texel count to1/16; it is not a measured sixteenfold frame-rate gain. See Unity's [URP performance guidance](https://docs.unity3d.com/kr/current/Manual/urp/configure-for-better-performance.html).

## Why This Works

Object construction and glyph/mesh preparation happen before combat. Repeated hits update existing objects with bounded retained capacity instead of reconstructing scene/UI objects and scheduling their destruction. The same32-popup burst measured3.1234ms after prewarming. A separate32-hit-effect comparison measured2.5605ms for Instantiate/Destroy scheduling and0.2338ms for reuse. These are single Editor call-burst samples, excluding deferred rendering/destruction, not device frame times.

## Prevention

- Exercise more rentals than capacity, expiry, reuse after fading, pause, target deactivation and teardown. Assert both instance identity and effect state; a stable root count alone can hide stale visuals.
- Test the actual lifecycle being claimed. Edit Mode-created behaviours may need explicit callback invocation in unit tests, followed by real Play Mode teardown evidence.
- Prewarm from scene startup, and inspect each supported scene after loading. A cache that first fills on impact merely moves the first-hit hitch.
- Gate automated probes on Time.frameCount and wait for a nonempty completion report before stopping Play Mode. Keep background Editor frame pacing separate from phone performance.
- Preserve live user preferences before gameplay/tests, restore after the final scene-open callback, and verify all captured keys.

The repeatable stress probe is [probe-mobile-combat-performance.cs](../../../tools/probe-mobile-combat-performance.cs). Verification and limitations are recorded in [the execution report](../../exec-plans/completed/mobile-combat-performance-2026-09-13.md).

## Related Issues

- [Scope map optimizations to scene instances](../logic-errors/scope-map-optimizations-to-scene-instances-2026-07-30.md): preserve shared assets unless their scope matches the requested change.
- [Deduplicate workbook styles before gameplay table loads](deduplicate-workbook-styles-before-gameplay-table-loads-2026-09-12.md): separate startup data costs from combat churn.
- [Verify runtime presentation beyond numeric checks](../workflow-issues/verify-runtime-presentation-and-progression-beyond-numeric-checks-2026-09-10.md): retain physics/pool ownership and actual frame cadence during validation.
