---
title: Measure road queries and actor visibility before camera clipping
date: 2026-09-20
category: performance-issues
module: Unity forward runner mobile performance
problem_type: performance_issue
component: service_object
severity: high
symptoms:
  - "The start scene used about 327 batches and 53 enabled animators."
  - "Each camera road-support sample scanned every road collider."
root_cause: logic_error
resolution_type: code_fix
tags: [unity, mobile, performance, occlusion, animation, road, profiling]
---

# Measure road queries and actor visibility before camera clipping

## Problem

The user's mobile run lagged and an Editor screenshot showed 341 batches / 17FPS. Render work was substantial, but a repeated full-road collider scan also consumed CPU. The existing camera occlusion checkbox was already enabled without baked data.

## Symptoms

A controlled stationary start fixture measured 326.6 batches, 1.923M triangles and 53 enabled animators. Disabling the custom road occlusion component improved mean frame time from 16.70ms to 11.94ms with almost unchanged rendering, isolating its road-support loop as meaningful CPU work.

## What Didn't Work

- Reducing the far plane to 90m cut rendering dramatically but visibly deleted the background. The final far plane remains 1000m.
- Enabling baked occlusion alone reduced a little geometry, but its CPU overhead could exceed that saving at the start. Do not equate fewer batches with faster frames.
- Screenshot encoding inside the measured frame window distorted results. Capture after the sample window.
- A route probe started before hazards were isolated and died on an unrelated trigger. Restart the run and isolate hazards before traversal; do not treat a paused/dead runner as a road-support failure.
- A Sprite test used managed `SameAs`; Unity returned different wrappers for the same native instance ID. Use Unity object equality when the required identity is the native asset.

## Solution

Index static road collider bounds into 16m XZ cells when roads are configured. Downward support probes inspect only their cell's candidates; retain the existing vertical range, normal and nearest-hit rules. Test negative coordinates, cell boundaries, crossing decks and explicit reconfiguration.

Attach `EnemyVisualDistance` to runtime event actors. Poll at 0.15s; hide artwork beyond 85m and restore below 75m. Preserve original renderer flags, colliders and root logic. Event controllers skip distant animation/facing; crate windup and recovery still complete. Detached shots explicitly restore rendering and belong to run cleanup rather than shooter deactivation.

Bake chapter visibility from safe opaque static geometry. Roads and explicitly camera-hidden groups may be occludees but must not be baked occluders: a hidden roof must not leave invisible occlusion behind. RestStop has only one static candidate, limiting the value of its bake.

The mobile pipeline changes world scale 0.8 → 0.75 and MSAA 4x → 2x; full-resolution overlay UI remains legible. Verify the actual pipeline and Android build, not just an Editor-only quality setting.

## Why This Works

The spatial index reduces repeated CPU queries without changing the route. Visibility gating removes distant skinning and pose work while keeping attacks/collision alive. The final Editor fixture had 296.2 batches, 1.835M triangles, 17 enabled animators and 12.28ms mean. Intermediate timings varied and the baked-off final view was 11.54ms; report those limits instead of selecting only favorable FPS.

## Prevention

Use a focused, uncapped, fixed-resolution Editor fixture with warmup and no capture work during sampling. Measure actual mobile presented frames separately using the app's SurfaceView layer, deduplicate timestamps and record battery temperature. Never infer sustained phone FPS from Editor stats. Preserve user app data with in-place installs.

Use native two-span slope traversal and actual projectile hits alongside unit tests. See [implementation, data preservation and device evidence](../../../map-concepts/mobile-feedback-2026-09-20/README.md) and the [authored combat geometry lesson](../integration-issues/verify-combat-with-authored-prefab-layers-and-pivots-2026-09-20.md). Existing pooling guidance in [bounded presentation pools](prewarm-bounded-combat-presentation-pools-2026-09-13.md) remains complementary.
