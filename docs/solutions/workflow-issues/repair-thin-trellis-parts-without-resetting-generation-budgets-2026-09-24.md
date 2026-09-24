---
title: Repair thin TRELLIS parts without resetting generation budgets
date: 2026-09-24
last_updated: 2026-09-24
category: workflow-issues
module: Rest-stop automatic-door asset production
problem_type: workflow_issue
component: development_workflow
severity: high
applies_when:
  - Bounded image-to-mesh attempts fail on a thin structural member
  - A corrected generated mesh still needs its first AI texture pass
  - Manual repair work shares one GPU with a running staged batch
tags: [trellis, blender, thin-geometry, attempt-ledger, cooldown, quaternion, semantic-validation]
---

# Repair thin TRELLIS parts without resetting generation budgets

## Context

B06 was an open automatic-door frame with a continuous thin bottom guide. m1 and m3 broke the guide into end fragments; m2 filled the doorway with a plate. The three permitted AI shape attempts were exhausted, and none had used a texture attempt. The usable generated jambs/header/sensor could still be retained.

## Guidance

1. **Check preprocessing before changing it.** The input already had alpha, and the existing graph applied cached u2net background removal. A read-only reproduction showed the rail intact before removal, after removal and in the black-composited/cropped node-equivalent image. No input or preprocessing setting was changed. This was a generated-geometry failure, not demonstrated loss of input pixels.
2. **Use the generated construction as the repair source.** The correction kept m3's frame and sensor, removed malformed lower fragments and swept their measured cross section into a thin continuous guide. The opening remained empty. The helper is specific to this open, upright frame, not a universal mesh-cleaning command.
3. **Respect Blender's active rotation representation.** Imported glTF objects can remain in Quaternion mode. Assigning `rotation_euler.z` did not rotate the geometry; the central-opening ray hit a jamb and stopped the first two script runs before a deliverable was written. Setting `rotation_mode='XYZ'` before the intended 90-degree Z rotation fixed the alignment. Check transformed bounds and semantic rays, not a Boolean saying rotation was attempted.
4. **Do not reset the failed automatic ledger.** All three rejected shapes remain recorded. The corrected shape received its own visual acceptance. A separate texture ledger, shared across repair revisions by input hash, counts original plus manual texture attempts against the same total limit of three. B06 used one texture-only TRELLIS call, with the parent m3 seed 221803 and unchanged generation settings.
5. **Serialize the GPU deliberately.** Manual texture work started only while the normal batch was held at a real, unanswered final-comparison gate, the queue was empty, and at least 60 seconds had passed since the latest generation history. B07's already-inspected verdict was saved separately, then released after the B06 GPU work ended. The normal per-file cooldown protected the next batch job. No automatic review or attempt record was rewritten.
6. **Verify the function after reduction and export.** Fresh FBX and GLB imports each passed nine rays through the doorway and five continuity probes along the lower guide. The guide occupied about 41% of the frame depth, distinguishing it from an unwanted floor slab. The final file contained 14,491 triangles and passed ordinary UV/finite/degenerate/loose-vertex checks.

## Evidence and limits

- `tools/inspect-reststop-preprocess.py` and `outputs/reststop-production-2026-09-24/reviews/B06-preprocess/`.
- `tools/restore-reststop-door-track.py`; corrected white shape in `manual/B06-r1/shape/`.
- `tools/texture-reststop-corrected-shape.py`; shared accounting in `manual-texture-ledgers/`.
- `tools/verify-reststop-door-opening.py`.
- Accepted output: `outputs/reststop-production-2026-09-24/manual/B06-r1/texture1/low1/`, with `repair.json`, `validation.json`, `opening-validation.json` and `visual-review.json`.

The selected frame is cooler/shinier and its guide finish is brighter/simpler than the reference; those remain minor visual notes. These are asset/export checks, not a Unity physics test. The overall 90-asset batch was still running when this completed repair was recorded.

## Later shelf case: valid numbers are not a valid shape

E03 m1 produced a large invented oval plate on the rear and strand-like shelf brackets. m2 collapsed into radiating strips in all five actual GLB renders. A read-only Trimesh check found finite coordinates and an ordinary-sized bounding box, so the tiny-looking result was not simply a distant outlier skewing the camera. It still failed semantic shape review. m3 then produced a plain back, three shelf levels and solid supports; its first texture and first 14,524-triangle reduction passed visual and fresh FBX/GLB checks. No manual geometry edit or fourth AI shape was needed. Evidence is in `assets/E03_736ccc967c/stages/` and `reviews/E03-m1-profile.json` under the same production root. Preserve the failed attempts and use the remaining bounded attempt; do not treat finite-coordinate checks as visual acceptance.

## Inspect the reference before describing its contents

The E06 catalog title translated as a tree planter, but the actual reference contained only the empty circular stone surround. Progress commentary incorrectly inferred a tree and foliage from the title; that was corrected explicitly after inspecting the reference. The delivered 14,875-triangle asset correctly contains only the planter, with trees handled as separate catalog assets. For atomic-asset production, names indicate purpose and do not reliably enumerate included geometry. Use the image as the source of truth both for acceptance and for user-facing progress descriptions.

The same reporting mistake recurred on R09 after a Korean console listing was garbled: progress described a crash barrel before viewing the input, which actually showed a parking wheel stop. Generation still used the correct immutable input; the commentary was explicitly corrected. Do not infer an asset from unreadable console text or category order. Read the actual image before describing geometry, and use UTF-8 output or escaped JSON for catalog labels. Until the image is inspected, report the ID/stage only.

## Texture-budget exhaustion on the canopy

G01's accepted roof shape exhausted all three texture attempts: the first duplicated the underside lights as soft dark dots, the second damaged the upper finish and fixture marks, and the third added large black tear-like graphics. The selected local correction retained the first result's body vertices, UVs, clean upper roof and fascia material. Only the soffit material and six small framed flush-light features were corrected, without a fourth AI request. The corrected source passed visual review before its first bounded Blender reduction; the final 14,770-triangle result passed fresh FBX/GLB and upper/lower comparisons.

`refine-reststop-corrected-texture.py --reviewed-source` records these reductions separately and includes original/canonical-recovery counts in the same two-attempt cap. It refuses source-hash changes and interrupted replay. Its existing B06 texture-attempt mode was regression-checked by reusing the already accepted cache, without running another reduction. Evidence is in `manual/G01-r1/source/`, `manual-refinement-ledgers/` and `tools/repair-reststop-canopy-soffit.py` under the production workspace. Both the corrected high-detail source and the original first AI texture are preserved; do not label a manually corrected high source as an untouched AI output.

## Derive a road variant from an accepted TRELLIS source

R02's three shapes were a fork, a funnel and a strongly tapered V-like slab rather than a constant-width curved road. A cached raw-decoder inspection on m1 showed the same defect before final simplification. The accepted R01 straight-road source already had compatible slab construction and PBR, so `tools/repair-reststop-curved-road.py` bent that existing generated asset instead of requesting a fourth shape.

The analytic 60-degree bend keeps width constant, retains UV/PBR and transforms corner normals by the inverse-transpose deformation Jacobian. The reference did not specify an exact world-space radius; 60 degrees is explicitly recorded as a local choice. The first derived source inherited one degenerate face; the next revision removed it while preserving retained UVs/normals. Fresh GLB/BLEND checks passed at 278,696 triangles. The first remaining canonical reduction passed at 14,991 triangles with five-view source comparison and fresh FBX/GLB validation.

The selected folder is `manual/R02-r2/source/low1`. Its README names R01 as the original generated source, and includes both the curved high source and the unmodified straight TRELLIS source. Original R02 failure records remain intact; this correction used no additional AI calls. Reuse is appropriate when construction and finish match, but must remain explicit in provenance and still undergo the target reference review.

## Related

- [Restore repeated slat relief after decimation](restore-trellis-slat-relief-after-decimation-2026-09-24.md).
- [Preserve material regions when repairing normal artifacts](preserve-material-regions-when-repairing-trellis-normal-artifacts-2026-09-24.md).

Captured with `ce-compound mode:headless` from retained local evidence, using sequential research under the repository's tool mapping. Existing AGENTS.md already makes the solution store discoverable.
