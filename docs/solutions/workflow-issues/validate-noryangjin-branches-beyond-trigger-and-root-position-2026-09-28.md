---
title: Validate Noryangjin branches beyond trigger state and root position
date: 2026-09-28
category: workflow-issues
module: Noryangjin revamp route and presentation
problem_type: workflow_issue
component: development_workflow
severity: high
applies_when:
  - "Adding a local route branch to a scene driven by existing turn spots and workbook placements"
  - "Replacing visible props while retaining the original shared placement data"
  - "Using generated concept images alongside real Unity play evidence"
tags: [unity, noryangjin, branches, spatial-gates, native-validation, capture-fixtures]
---

# Validate Noryangjin branches beyond trigger state and root position

## Context

The approved review required an enclosed market, an outside pier, no overlaps with legacy encounters and additional market incidents. The first application had only overhead signs above the original wooden dock. The completion pass exposed failures that a successful build and event-trigger log could not detect.

## Guidance

1. **Check the damage volume every tick.** The hose correctly triggered in the hall, then much later damaged a player on another road with the same projected station. Require facing, height, lateral and along bounds at contact time; latch completion after leaving the stretch. Test the safe half and distant lateral positions. Clamp timed damage to remaining active time so a final oversized frame cannot overcharge damage.
2. **Audit renderer bounds and combined road geometry.** Root-position suppression left shops in the curved bypass and tall posts embedded in old road meshes. Sample the new corridor against renderer bounds. Keep old road colliders; temporarily hide intersecting old road renderers only during outside travel and restore their prior state. Add new walkable surfaces below the existing support-cache root.
3. **Keep event spacing consistent with quiet windows.** A truck and box toss only50m apart caused the latter to be skipped during the former's10s reservation. Move the toss96m away and use a7s window. An event existing in the hierarchy is not proof it happens in a full run.
4. **Preserve presentation invariants through reset.** Wave animation replaced the authored long crest scale with `(1,rise,1)`. Cache/reset the complete vector and scale only height. Place legacy models using mesh-bottom bounds after fitting. Noryangjin human prefabs face local−Z; imported static prop orientation is a separate contract.
5. **Validate the actual camera.** A roof at8.5m remained almost invisible under the original40-degree-FOV/pitch19 camera; a lower pitch8 indoor frame reveals it. The camera's explicit `Skybox` material overrides `RenderSettings.skybox`; update/restore both. Reuse the project's FlatKit gradient shader for direct top/horizon colors rather than treating the procedural shader's tint as a literal output color.
6. **Keep test claims conditional on recorded state.** Two runs acquired9999midway and must stay labelled as mechanic-only. Later actual-growth runs cleared both routes in about302s with the override off. A relocated capture set its pose before `ResetState`, which restored the original heading; place it after the real start operation. Label that fixture separately from full-course completion.
7. **Configure the fixture's route.** An initial native probe invoked indoor cat/shutter events before selecting a branch. Their route guard correctly rejected the calls. Establish an indoor selection and isolate the quiet window before testing those components; do not remove the production guard to make a fixture pass.

Use `data.result.success`, not only the CLI envelope's `success`, for `run_script`. Wait for domain compilation after adding runtime types. Use saved TMP outline material assets in an editor installer; calling the instance outline setters produced four material-leak errors per installation. Explicit shared materials removed those errors.

## Why This Matters

Each of these failures can survive numeric scene counts or a compiling C# project. Incorrect roots, transforms, elapsed-time assumptions and fixture setup produce plausible yet misleading reports. The solution is a small combination of geometric boundary tests, real component/physics probes, both route playthroughs and visible native captures.

## When to Apply

Use these checks for copied Unity scenes that preserve their original workbook, combine old and new roads, or introduce timed events into a looping course. Keep generated concepts, complete-play evidence and staged section captures visibly distinct.

## Examples

- `NoryangjinRevampMechanicsTests`:13passing cases, including coin clamping, branch endpoints, hose boundaries and wave scale reset.
- `directed-mechanics.json`:8passing native checks. The invalid initial fixture is retained as `directed-mechanics-v1-invalid-route-scope.json`.
- ATT37/HP46/fire-rate30 inside/outside runs:301.9/302.0seconds, no9999/health pin; ATT20/HP12/fire-rate10 outside dies at203.5seconds.
- Source and receipts: [implementation report](../../../outputs/noryangjin-revamp-fix-2026-09-28/README.md), [work log](../../../outputs/noryangjin-revamp-fix-2026-09-28/WORKLOG.md).

## Related

- [Runtime verification beyond numeric checks](verify-runtime-presentation-and-progression-beyond-numeric-checks-2026-09-10.md).
- [Preserve image-concept scope](preserve-concept-scope-when-applying-ui-style-2026-09-15.md).
- [Preserve generated image files](persist-generated-image-before-next-prompt-2026-05-16.md).

Captured with `ce-compound mode:headless`, sequentially per AGENTS.md. Related guidance overlaps moderately on native evidence; this record adds branch geometry/event-gate failures and their exact fixtures. No session-history or GitHub issue search was performed. AGENTS.md already exposes the solution store; no instruction-file edit is needed.
