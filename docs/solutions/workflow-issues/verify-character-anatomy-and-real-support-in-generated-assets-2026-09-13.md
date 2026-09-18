---
title: Verify character anatomy and real support in generated assets
date: 2026-09-13
category: workflow-issues
module: Chapter character and transition production
problem_type: workflow_issue
component: development_workflow
severity: high
applies_when:
  - "Generating recurring characters for chapter movies and rigged game assets"
  - "Removing ground fragments or checking animated foot contact after export"
tags: [blender, unity, wan, trellis, anatomy, animation, ground-contact, validation, import-scale]
---

# Verify character anatomy and real support in generated assets

## Context

The chapter-polish revision exposed three ways plausible output can violate its actual contract. A three-shoe shark was described as having three legs, while the user intended two legs and a shoe-wearing tail. A rig's Root local axes were confused with world vertical. Fresh glTF evidence then appeared to float despite valid evaluated contact because an importer-created Icosphere bone-display helper polluted the render bounds.

A visible white floor also need not occupy the lowest part of a mesh. A sound barrier had a slab around18percent of total height, with spurious geometry below it. A short guardrail retained disconnected scraps after its main slab was removed; these inflated its depth from0.61m to1.92m.

## Guidance

1. Describe attachment and function, not just counts. The protagonist has **two legs plus one tail whose end wears the third sneaker**. The tail is the third ground support. A third belly-attached leg plus an additional free tail is wrong. The current `modeling-contract.md` records this user correction.
2. Correct first-frame anatomy before generating motion. Rest-stop v2 inputs/videos were superseded; four v3 inputs show the continuous rump-to-tail-shoe connection. The rest-stop retains a shark about twice adult height and frightened people looking up at it. Keep the lower body planted and animate head/arms and people's reactions when complex locomotion is unnecessary.
3. Convert world displacement into the pose bone's local axes. Grounding uses `root.bone.matrix_local.to_3x3().inverted() @ Vector((0,0,-min_z))`, then reevaluates the mesh before keyframing. A Root pointing along world Z need not use local Z for vertical movement.
4. Compare motion in seconds or use the source frame rate. These one-second actions use30fps. A default24fps glTF import represents their range as0.8–24.8. Set30fps before import to sample the intended1/8/16/24/31 frames.
5. Separate render helpers from asset geometry. After pose evaluation, hide objects referenced by `pose_bone.custom_shape` from diagnostic render/bounds. The generated Icosphere is a bone-display helper, not exported character geometry. Retain the incorrect evidence and write corrected `evidence-fresh-grounded-*` renders. Never move a character to compensate for an evidence-floor defect.
6. Find floor artifacts visually, then repair the verified geometry. Horizontal-face deletion alone left a rim on car67; remove the slab band including its walls. Barrier64 requires an explicit higher search band and planar cut. Guardrail49 requires coincident-seam connectivity and removal of disconnected low flat remnants. Keep normal camera, tire and booth bases; do not broaden removal thresholds globally.
7. Keep export, fresh import and Unity placement acceptance separate. Mesh checks/contact sheets do not prove final prefab materials, instance overrides, road height, camera views or combat wiring. Offline compilation cannot replace Play Mode tests when Unity's main thread is blocked.
8. Calibrate Unity skin measurements with a known displacement before authoring a correction. For these100x FBX hierarchies, `BakeMesh(mesh,false)` returned already-scaled vertices; applying `renderer.transform.TransformPoint` multiplied scale again. An apparent26cm gap was actually2.6mm. Changing a root bone by a known world displacement exposed the100x measurement response. Independent `bone.localToWorldMatrix * bindpose * vertex` weighted skinning confirmed all nine clips' sampled floor errors within3mm. The rejected bake never changed any controller; its candidates are archived outside Assets. Do not infer that `BakeMesh` plus the full renderer matrix is universally correct from a unit-scale fixture.
9. Preserve imported root/visual scale and convert hand sockets using world dimensions. These static prop FBXs and rig hierarchies use100x conversion transforms. Normalizing the imported visual root toone shrank props; adding a nominal hand child offset without compensating scale made held projectiles enormous. Normalize only the gameplay prefab root and preserve the imported visual transform.

## Why This Matters

Counts cannot distinguish a tail shoe from an extra limb, nor a physical plinth from an accidental floor. Bounds cannot distinguish real geometry from importer helpers. Numeric checks, semantic visual review and live Unity checks cover different failures.

Corrected fresh-import checks at30fps put the nine mascots' sampled body minima within0.000001m of ground; each retains18bones and six actions. Subsequent native integration succeeded after the Editor recovered without being killed. Native weighted skin matrices initially found2.61mm at five phases; denser60fps verification found at most3.83mm for living actions and14.47mm at a death interpolation extreme. See `review/native-poses-skin-matrices-60fps.json`. These small transient differences are accepted; no runtime mesh-baking cost or new corrective rig was introduced. Earlier BakeMesh reports without skin-matrices in the filename are rejected scale diagnostics. Repeated gameplay calibration is tracked separately in the active execution log.

These exact minima apply to GLB. Separate FBX reimport exposed default animation-key simplification, including up to0.0046m lift. A retained `-full-keys.fbx` export disables simplification and removes that lift. Up to0.008m walk/run penetration remains in Blender's FBX import and must be checked in Unity. Never transfer a GLB measurement to FBX without measuring that format.

## Examples and evidence

- `tools/rig-chapter-mascots.py`, `tools/inspect-chapter-rig-poses.py`, `tools/validate-chapter-assets.py`
- `tools/refine-highway-props.py`; final49/64 exports in `outputs/chapters-polish-2026-09-12/props-v5/`, other repaired props in `props-v3/`
- `map-concepts/chapters-polish-2026-09-12/references/transitions/*-v3.png`
- `outputs/chapters-polish-2026-09-12/transitions/reststop-B-v3/` retains sample/decode workflows
- [Execution log](../../exec-plans/completed/chapters-style-balance-ads-2026-09-12-log.md)

## Related documentation

- [Runtime presentation and progression beyond numeric checks](verify-runtime-presentation-and-progression-beyond-numeric-checks-2026-09-10.md)
- [Verify mesh crop intent before export](verify-l-shape-mesh-crop-intent-before-export-2026-06-12.md)
- [Protect active scenes from broad EditMode tests](protect-active-unity-scenes-from-broad-editmode-test-runs-2026-07-18.md)
