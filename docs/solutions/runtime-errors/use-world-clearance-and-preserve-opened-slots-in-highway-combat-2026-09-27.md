---
title: Use world clearance and preserve opened slots in Highway combat
date: 2026-09-27
module: Unity Highway compulsory vehicle combat
problem_type: runtime_error
component: service_object
severity: high
symptoms:
  - "Long vehicles overlapped on curves despite a positive progress-distance gap"
  - "The validation driver left a cleared lane and drove into another vehicle"
  - "Earlier traffic visibility did not guarantee readable vehicle health labels"
root_cause: logic_error
resolution_type: code_fix
tags: [unity, highway, traffic, collision, visibility, playtest, data-workbook]
---

# Use world clearance and preserve opened slots in Highway combat

## Context

The user changed the chapter from avoidable traffic to compulsory vehicle destruction, added about50%more cars and moved the fast-road effect from the cars to the shark. The old admission rule deliberately left a free lane, so preserving it would defeat the new request.

## Resolution

Admit an entire authored front row atomically and brake its living members together. Preserve an opened slot after a member dies. Progress-distance spacing remains a useful first check, but a curved/offset route is not an isometric axis: final clearance must compare the collider poses produced by the same sampler used to place the cars. Bound the permitted group step when its proposed world poses penetrate another body. The initial overlap cohort is retained; later accepted telemetry records no overlaps.

The driving test also needs dead slots. Its first version searched only living enemies, so a bullet that cleared the next row early removed the best passage from the bot's candidate set. The bot then steered into the remaining truck. Reconstruct a dead member's virtual position from an active member of its group and retain that lane until the row's rear passes. Treat that as a QA correction, not evidence that the player needs more health.

Balance the new compulsory moving rows against their actual shooting opportunity. Keep parked-accident health separate, preserve type multipliers and store the exposure budgets in the workbook. After changing spawn stations to compensate earlier appearance, calculate cached HP from the same station used by the Excel formula; otherwise opening Excel silently changes the balance.

## Visibility and false damage labels

The old collision-warning text describes potential damage, not a health transaction. Suppress that renderer for vehicle actors rather than hiding real damage notifications. Record actual damage alongside overlapping collider IDs. Health labels should additionally consider the gameplay camera, projected body size and occlusion; an active renderer alone does not mean the car is visible to the player.

Do not assume a shader property exists because another shader uses that name. The supplied TMP mobile shader uses unity_GUIZTestMode as a CanvasRenderer-owned draw-state input, not a serialized _ZTestMode field. A material-float assertion was therefore invalid. Check the actual world-canvas configuration and test the rendered/occluded behavior.

## Verification workflow

- Separate ordinary route runs, a no-fire weaving attempt and directed collision/pickup/lifecycle fixtures.
- Require actual ChapterProgression completion for a clear. A driver's complete.json merely means its scheduled attempts ended.
- Check nested Pipeline success before starting the next mutation. A failed prepare must not be followed by entering Play.
- Keep the original task-wide preference snapshot as well as per-cohort snapshots. Compare and restore the original at final delivery, including key absence and start-scene settings.

Evidence and final status: outputs/highway-combat-revision-2026-09-27. See also [the earlier visibility ownership fix](keep-highway-vehicle-visibility-out-of-pedestrian-culling-2026-09-27.md) and [workbook preservation](../workflow-issues/preserve-workbook-contracts-when-converting-highway-actors-to-vehicles-2026-09-27.md).

This was documented with ce-compound in headless mode, sequentially under the repository's agent rules. Existing AGENTS.md already provides discovery guidance; no external issue or commit was created.

Final evidence: two normal-growth route clears (298.3/268.3seconds), a no-fire death with0projectiles,16directed assertions and82test cases. The final15workbook tests used direct method invocation after an async-runner stall and cancellation. All original preference bytes/start-scene settings were restored; protected scenes remained unchanged. Failed cohorts remain separate from these accepted results.
