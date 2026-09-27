---
title: Keep Highway vehicle visibility out of pedestrian culling
date: 2026-09-27
module: Unity Highway vehicle presentation
problem_type: runtime_error
component: service_object
severity: medium
symptoms:
  - "Cars appeared abruptly even after advancing their configured spawn time"
  - "Distant vehicle health labels were visible without their car bodies"
root_cause: scope_issue
resolution_type: code_fix
tags: [unity, highway, visibility, culling, vehicle, native-validation]
---

# Keep Highway vehicle visibility out of pedestrian culling

## Cause and evidence

HighwayChapter2Controller activates vehicle visuals according to workbook placement timing. EnemyEventController remained on those actors as a disabled legacy combat component. Its runtime-reference resolver still added EnemyVisualDistance and EnemyContactWarning. The former toggles forceRenderingOff at75/85m using hysteresis. Because its renderer cache was taken before the separately built vehicle HP canvas, labels stayed visible while bodies disappeared.

Moving the workbook activation1.5seconds earlier did not fix that second visibility owner. In native play, HWY2_066_Taxi at90.50progress metres ahead and HWY2_105_Taxi at80.06had active, enabled model renderers with forceRenderingOff=true; nearer cars remained visible. This disproved the assumption that chapter activation alone controlled appearance.

## Resolution

EnemyEventController.ResolveRuntimeReferences excludes actors with HighwayVehicleEnemy from its pedestrian culler/contact-warning bootstrap. HighwayChapter2Controller owns their activation and retirement. Pedestrian enemies in other chapters retain the existing optimization. The normal EnemyVisualDistance regression remains relevant for that path.

Vehicle reveal timing is a separate concern: advance activation by playerSpeed×leadSeconds and starting station by carSpeed×leadSeconds so earlier visibility does not also advance nominal collision time. Preserve IDs, HP and rewards, and audit all old rows. The follow-up added eight new entry cars separately, which is the intentional difficulty change.

## Validation and related presentation lessons

- Native runtime inspection must check active/enabled/forceRenderingOff as well as distance. Static meshes and successful spawn counters cannot establish visibility.
- The first interrupted play cohort is retained and labeled; its first-gate pass is not a full-route clear. Final route and restoration receipts live in outputs/highway-visual-fixes-2026-09-27.
- Accident deformation must exclude wheels/chassis or use an intact replacement. Inspect actual vertices against road colliders; a correct origin alone is insufficient.
- Independent fork lane systems must be clipped in their shared asphalt region. A faster branch ramp must still pass the existing heading and centreline continuity contracts; the first180mtrial failed and the measured220msetting passed the probe limits.

See [workbook conversion and normal-stat verification](../workflow-issues/preserve-workbook-contracts-when-converting-highway-actors-to-vehicles-2026-09-27.md). Existing AGENTS.md already directs discovery through docs/solutions; no new instruction rule or external issue was needed.

Final evidence:72focused tests pass. First-entry passes and two full normal-growth paths clear in298.3seconds with0hidden-body frames. Final cosmetic footage removes asphalt outline seams and extends guides through the merge; all saves restore byte-identically. Preserve the interrupted first cohort separately from those accepted results.
