---
title: Validate pickup artwork in moving gameplay before accepting fidelity
date: 2026-09-20
last_updated: 2026-09-23
category: workflow-issues
module: Bonus talisman visual authoring
problem_type: workflow_issue
component: development_workflow
severity: medium
applies_when:
  - A reference-based Unity effect passes tests but looks crude or invisible
  - Frozen animation samples do not establish in-game readability
tags: [unity, blender, reference-fidelity, vfx, video, visual-review]
---

# Validate pickup artwork in moving gameplay before accepting fidelity

## Context

The first talisman shipped with passing functional tests but squat paper, tiny stock icons, a box clip and barely visible absorption. The user rejected it against the concept. Technical validity and visual acceptance had been conflated.

## Guidance and correction

1. Freeze original assets, code and native captures before replacing them. Write a visual fail/pass ledger grounded in the actual and reference images.
2. Repair primary shapes with an appropriate representation. This iteration used separated Blender paper meshes, deliberate bevel normals, a bent enamel clip, metal rivet and raised emblem meshes. Do not add glow to disguise a flat placeholder.
3. Keep source, GLB reimport and Unity rendering as separate checks. Here source/reimport triangle count and bounds matched; neutral and identical-camera studio renders exposed the actual geometry.
4. Record a real physical pickup while the character moves. Fixed animation-phase captures concealed two problems: the unfolded object overlapped the character, and the curved transfer was occluded by opaque skin. A short camera-relative reveal and depth-independent transfer ribbon restored the visible causal chain. The body target stayed common to all bonuses.
5. Separate material roles. Reusing glossy navy enamel on a caption caused complete specular whiteout in other chapter lighting. The text background now uses an explicitly non-specular material, covered by a resource test.
6. Treat serialized legacy UI references as optional presentation data. Some old Bonus prefabs applied the correct reward but had no statValueTmp reference. The label now falls back to the formatted rolled data value; a regression test covers +14% without UI text.
7. Record fixed-capture-rate videos honestly: 30fps replay demonstrates simulation/visual timing, not device performance. Preserve both final videos and their collision/effect-frame metadata.
8. Extend isolated pickup checks with complete ordinary runs. The September22review captured458native screenshots over10actual attempts and found a repeatable camera obstruction, stacked-helper clutter and displayed bonus values that did not match actual stat deltas. Passing pickup transaction tests had not established those presentation contracts. These new game issues are recorded for correction; this review did not fix them.
9. Capture the displayed choice and the actual before/after stat. Ratio-based data can show`+12`while applying`round(0.12 × originalDamage)=1`; rounded label validity alone misses the semantic mismatch. Keep the source roll, starting stat and final delta in the run log.
10. Record test-policy limits. Automated aiming/avoidance, different random rolls and directly opened later chapters do not establish a human win rate or fair campaign progression. Do not hide early deaths, claim full-map coverage, or replace normal stats with endurance overrides to improve the report.
11. Name gallery image copies from their immutable source capture. Reusing`issue-04-1.png`while changing the selected source and refusing overwrite can leave the new caption above the old image. Preserve prior files and give revised source choices a new filename.

## Evidence

[Rework, source artifacts, iteration ledger and 39 passing tests](../../../map-concepts/talisman-polish-2026-09-20/README.md). Three native clips each show a physical claim and 30 effect frames. Functional reward/choice/drop/reset rules remain intact.

Related: [detached effect lifetime](common-bonus-feedback-survives-root-deactivation-2026-09-20.md), [asset-grounded VFX](ground-vfx-concepts-in-reusable-parts-2026-09-19.md).

Follow-up: [ten-run review, measured values and coverage limits](../../../map-concepts/ten-run-review-2026-09-22/README.md).

The bonus amount issue was subsequently fixed on September22: migrate the four flat ATT/HP rows to explicit Value ranges and use the one resolved pickup amount for both application and caption. Keep the generic Ratio resolver for legacy data, but remove its separate percentage-scaled display resolver. The previous base100test could not reveal divergence, and another test explicitly accepted it. New base8/base60and actual-collider checks reproduce and prevent the error. Native verified pickups are68→80for+12and500→514for+14; see the [completed correction](../../exec-plans/completed/bonus-amount-fix-2026-09-22.md). The selected remaining corrections are tracked in the [twenty-run follow-up](../../../map-concepts/review-fixes-20-runs-2026-09-22/README.md); helper formation and repeating layout were explicitly excluded by the user.

## Follow-up: pose evidence, real occluders and test steering (2026-09-23)

- Sampling multiple animation times and rendering multiple cameras in one Editor frame produced stale skinned poses. Bone transforms changed while screenshots appeared unchanged. `SkinnedMeshRenderer.BakeMesh(mesh, true)` into a temporary renderer made the evaluated pose observable. Preserve the original material and delete temporary geometry afterward. Pose samples still do not prove continuous animation timing.
- Bone origins are not the mitten's visible palm. An early knife anchor at hand-local z0.18 floated in front of the glove. Sampling vertices with strong finger weights placed the visible center near (-0.027,0.127,0.055). Fit the actual mesh handle point, then inspect the curved fingers around it. The final sampled carry/slash poses retain handle alignment within0.005m; rejected prototypes stay separately named.
- A camera component with a nominal additional-occluder slot contained a null reference, so improving its hide/fade algorithm alone could not affect the gray gantry. Bind the actual scene renderer group and verify a real1x passage. When fading a sign group, include its TMP face/outline colors and remove it from baked static occluders. Otherwise opaque lettering or baked occlusion can remain after the panel fades.
- Six RestStop tests with HP11330–13310 all died at the same22second crossing car. This did not show inadequate HP: the vehicle is an instant-kill hazard and automatic steering flipped sides as the car crossed center. Predict car position at player arrival; similarly select the toll lane open at arrival. Archive the defective-control attempts and repeat affected cases with the same purchased-level profiles. Do not lower enemy HP to compensate for test steering.
- Shared hazard components need subtype provenance. Labeling every `HighwayHazard` death as Roadblock concealed vehicle and toll deaths. Add a typed mapping covered by tests, record the actual terminal cause, and use it in the result UI and analysis. Nonfatal damage from a cleared run is not its defeat cause.
- Keep automated-run limitations explicit: ordinary lateral input does not make a bot a human player; a purchased-level fixture does not prove campaign affordability; screenshot capture and accelerated simulation do not measure device FPS. Unexpected Editor pauses must not count toward a watchdog or be silently resumed. Restore key absence as well as values and verify the restored scene after the batch.

This update reuses the existing workflow entry because its problem, visual-evidence approach and prevention rules substantially overlap. Related grip/layer detail: [authored prefab pivots](../integration-issues/verify-combat-with-authored-prefab-layers-and-pivots-2026-09-20.md). Local evidence supplied the relevant facts; session-history and GitHub issue searches were not used.

Captured with ce-compound mode:headless. Review ran sequentially per AGENTS.md; no independent artistic score or exact-reference claim is implied.
