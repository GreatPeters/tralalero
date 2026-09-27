---
title: Validate Highway traffic across Play sessions and live hazards
date: 2026-09-26
category: workflow-issues
module: Highway traffic and native scene authoring
problem_type: workflow_issue
component: testing_framework
severity: high
applies_when:
  - "Changing scene-authored traffic paths while workbook initialization can reactivate hazards"
  - "Verifying runtime clocks in a Unity project with domain reload disabled"
tags: [unity, highway, traffic, domain-reload, native-authoring, playtest]
---

# Validate Highway traffic across Play sessions and live hazards

## Context

The user approved actual HighWay scene implementation based on the native three-lane preview. An initial full main/bypass cohort cleared, but later checks found three separate problems: workbook-enabled roadblocks, a centre log truck combined with a left roadblock, and static traffic suppression surviving the next Play session. A successful one-session clear did not cover these conditions.

## Guidance

1. **Inspect runtime hazard state.** `EncounterPlacementController` reapplies workbook enable flags at run start. `OncomingLaneTraffic` caches hazards including inactive ones, then rejects spawns crossing enabled, unbroken roadblock colliders. Turning off an authored GameObject alone is insufficient. Scene hazard transforms remain authoritative; move conflicting roadblocks without changing workbook IDs or activation rules.
2. **Reset clock-dependent state at each boundary.** `suppressedUntil` and `clearUntilTime` refer to `Time.time`, which restarts on Play entry. Reset them in `SubsystemRegistration`, initialize new instances through `Start`/`ResetForRun`, and reset traffic/spill instances explicitly in `ChapterProgression.BeginRun`. Clear static blocker references at subsystem registration; let their owners unregister on scene cleanup.
3. **Check the combined obstacle arrangement.** A head-on centre truck and centre/right logs need a free left path. Moving only the truck from the right lane exposed a left roadblock near 756m. The installer now separates sixteen authored roadblocks from log-event, toll, pothole, fork and mandatory encounter windows. Exclude inactive objects named `Template` from placement edits; they are source objects, not route obstacles.
4. **Separate a CLI response from mutation completion.** The Pipeline server can return a 30-second main-thread timeout while a bulk mesh operation continues. Check receipts and the actual Editor state before retrying. Flush deferred Undo records before saving, then reopen and verify persisted meshes, colliders, lanes and actor counts. Unity objects require `== null`, not C# `??`, when missing components can be fake-null wrappers.
5. **Compare the latest reference itself.** The live native gallery had advanced from v1 to v3. Before final capture, compare its dimensions and appearance instead of trusting the first preview script. The final scene uses 3.6m lanes, lane centres -4.2/-0.6/3.0, yellow median lines and the v3 median spacing. The user's all-head-on rule and empty background-road approval override the preview's decorative same-direction cars.
6. **Do not let tests hide their conditions.** Record real chapter completion, actual branch flags, all used traffic lanes, observed directional errors and geometric overlaps. The final cohort uses the map-tool 9999 preset once, normal lateral movement, enabled colliders and no teleport/health pin. This is not a campaign balance result. Restore preference key presence as well as values and the original play-start scene selector.

## Why This Matters

The project disables domain reload. Recompiling before each trial accidentally reset statics and masked suppression leakage. The retained `play-v4` cohort had zero ordinary cars despite a traffic-enabled scene; `Suppressed` remained true after `Time.time` restarted. The focused lifecycle test reproduced the failure before the code change and passed after it. Separately, ordinary scene flags did not describe the hazards the player actually faced after workbook initialization.

The native camera follows the player laterally. Three equal lanes ahead do not imply all three remain visible beside the shark at either edge. The delivered report states that existing-camera limit; avoid calling this a framing or device-performance certification.

## When to Apply

- Scene traffic changes, head-on conversion, fork edits or obstacle relocation.
- Any static deadline/cache in this no-domain-reload project.
- Bulk Pipeline scene authoring or focused NUnit execution against the active Editor.

## Examples

- `lifecycle-red.json`: prior-session suppression test failed as expected before correction.
- `final-traffic-tests.json`:22focused cases passed, including scene-start/subsystem resets and spill disable/re-enable state cleanup. `OnDisable` must reset the phase, not merely destroy the truck.
- `final-gimmick-tests.json`: 5 existing road-gimmick cases passed.
- `play-v4/`: retained rejected cohort. Zero vehicles and deaths are diagnostic evidence, not a clear result.
- `play-v5/`: final scene mainline clear, retained failed bypass attempt, timelines, actual PNGs and preference restoration receipt. The bypass driver initially reacted only after physical logs spawned, ignoring the visible warning; it failed near1653m before the second fork.
- `play-v6/`: separate bypass replay with the driver responding to the visible red lane warning before logs spawn. No game configuration, HP pin or collision override was added for this retry.
- `play-v5/restart-armed.json`, `restart-verified.json`, `exit-verified.json`: suppression deliberately set5000seconds ahead, Play restarted without recompilation, new suppression=false/blockers=0/traffic=1/harness=1; after exit, harness=0 and scene clean.
- `tools/install-highway-three-lanes.cs`: native installation, reference refinement and saved-scene verification. `MeshObject` updates existing mesh holders so a partial authoring failure can be repaired without duplicating geometry.

If NUnit appears stuck and the main thread times out, inspect the native Unity window. Here a Save Scene dialog blocked the run. Cancel the pending test, save an Editor-native copy, compare it with the persisted scene and resolve only the observed changes. In this case the only difference was sub-micrometre RectTransform serialization drift; saving allowed the focused test to run. Do not kill the Editor or discard an unknown dirty scene.

## Related

- [Highway combat and coverage evidence](audit-highway-branches-with-combat-and-coverage-separated-2026-09-26.md)
- [No-domain-reload workbook lifecycle](../integration-issues/refresh-excel-character-defaults-without-domain-reload-2026-08-01.md)
- [Combat Harness creation after scene setup](../runtime-errors/create-combat-harness-after-scene-setup-2026-09-26.md)
- [Separate placement and driver errors from balance](separate-road-placement-and-driver-errors-from-campaign-balance-2026-09-23.md)

Local implementation evidence is authoritative here. No GitHub issue was required or opened.
