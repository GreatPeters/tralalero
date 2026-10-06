---
title: Qualify actual build and native capture evidence in mobile audits
date: 2026-10-01
category: workflow-issues
module: Unity mobile performance and visual review
problem_type: workflow_issue
component: development_workflow
severity: high
applies_when:
  - "Diagnosing device lag using an Editor and local APK artifacts"
  - "Reviewing generated characters and imported animation visually"
tags: [unity, android, profiling, build-report, screenshot, skinned-mesh, evidence]
---

# Qualify actual build and native capture evidence in mobile audits

## Context

The S22 audit initially found Revamp absent from EditorBuildSettings, but the latest build script and actual BuildReport included Revamp explicitly. The same investigation produced plausible-looking yet invalid instant model animation previews. A transport success or a PNG file was insufficient evidence of the claim being made.

## Guidance

1. Pin the audit to the actual build's scene array, receipt and APK, not the current editor default scene list. Use a device APK hash when a device is reachable; otherwise state that the prior installation receipt has not been reverified.
2. Keep source movie sizes, VideoClip metadata sizes, packed asset sizes, compressed APK sizes and runtime memory separate. In this audit a VideoClip's packed row was only about 200 bytes; its movie payload lived in a separate resource. The APK was 1.526GB while packed assets summed to 2.223GB.
3. Keep whole-scene geometry inventories separate from UnityStats frame submissions. Runtime static batching can make several renderers reference the same combined mesh; summing its entire index buffer per renderer inflates the count.
4. Inspect PNG contents. Pipeline `screenshot --view game` returned a camera-only picture without overlay UI. `ScreenCapture.CaptureScreenshot` produced the composited Play frame. Label the former as camera-only or exclude it.
5. Do not judge a rig from a failed capture fixture. Immediate SampleAnimation/Render gave unreliable bone pose/bounds; a follow-up bake attempt yielded zero bounds. Reject those motion images. A direct live subject capture gave a correctly framed player with 2.92×2.59×4.47m renderer bounds. Valid curve paths prove binding coverage, not natural movement.
6. Use repeated on/off profiling with warmup and no screenshot work in the sample window. Here the camera occlusion component visited 1,310 candidate groups. With identical 242 batches/1,335,308 triangles, later ON conditions had 19.42/19.35ms median versus OFF 9.23/8.69ms. This establishes a PC contribution, not a device FPS or speedup guarantee.
7. Recheck visual failures at 1x. In this audit the strong sea-through-floor artifact was seen at 2x; the 1x run confirmed the ceiling/sign-heavy camera framing but not equal transparency. Preserve that distinction.
8. Read nested `result.success`, await running command sessions, and inspect output artifacts. Blender 4.4's `hasattr(bpy.ops.wm, 'fbx_import')` was misleading: the operator proxy existed while the operator was unregistered. Using `bpy.ops.import_scene.fbx` through a local wrapper completed the independent topology inspection.

## Why This Matters

An apparent optimization may only change camera contents. An apparent deformation may only be a stale capture. An APK inventory is not a RAM profile. Incorrectly merging these signals yields confident diagnoses that fail on the user's phone and can motivate unnecessary asset regeneration.

## When to Apply

Use for mobile lag reports, native visual reviews, build migrations, saved/unsaved scene differences, and generated mesh validation. Reversible runtime experiments still need preference/session restoration and exact scope labels.

## Examples and Records

- [Full S22 report](../../reviews/s22-performance-visual-audit-2026-10-01.md), with diagnostic scripts and raw evidence locations.
- [Prior mobile measurement lesson](../performance-issues/measure-road-queries-and-actor-visibility-before-camera-clipping-2026-09-20.md): warmup, camera fidelity and device timing remain valid; this audit concerns the expanded renderer-group traversal as well as build/capture provenance.
- [Native road occlusion review](../ui-bugs/verify-native-occlusion-after-splitting-road-renderers-2026-09-28.md).

The investigation is complete as a report. Gameplay optimizations and art changes have not been applied. No broad refresh or remote issue search was needed for this local evidence-handling lesson. AGENTS.md already documents discovery of docs/solutions, so no instruction-file edit was necessary.
