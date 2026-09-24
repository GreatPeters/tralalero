---
title: Preserve UV seams and corner normals when reducing TRELLIS foliage
date: 2026-09-24
category: workflow-issues
module: Rest-stop TRELLIS prop production
problem_type: workflow_issue
component: development_workflow
severity: medium
applies_when:
  - A foliage asset passes high-detail review but automatic Decimate cannot meet the triangle target
  - Welding generated texture seams introduces non-manifold edge fans
  - Re-unwrapping and selected-to-active baking damage dense overlapping leaves
tags: [trellis, blender, foliage, decimate, uv, corner-normals, non-manifold, recovery]
---

# Preserve UV seams and corner normals when reducing TRELLIS foliage

F03's TRELLIS shape and texture passed visual review, but the first Blender reduction aborted at the saved 15,000-triangle target. The batch stopped under the existing error policy. The approved AI source remained intact.

## Evidence and failed approaches

- The original imported textured mesh had 193,961 triangles, 14,573 zero-area faces and no non-manifold edges. The installed weld-based cleanup produced 167,133 triangles and 3,224 non-manifold edges. Read-only component inspection found 164 components; the count alone did not explain the reduction floor.
- The one remaining canonical `preserve_details` attempt was recorded in a separate shared recovery ledger. Its four internal reductions reached 22,659 → 18,758 → 18,536 → 18,504 triangles, retaining 1,182 non-manifold edges. Both automatic attempts remained recorded; their counters were not reset.
- Manual r1 split those edge fans and reached 14,814 triangles, but its already-reduced surface and rebaked material produced a sparse angular canopy. It was rejected visually.
- Manual r2 reduced the original mesh directly while keeping its UV/PBR data. The appearance improved, but fresh exports contained 2,902 degenerate faces and 56,417 unused FBX vertices. It was not accepted.
- Manual r3 removed those invisible elements while retaining the remaining corner normals by source-loop mapping. Fresh FBX/GLB passed at 11,970 triangles, with zero retained-normal error and zero removed surface area. This was a valid baseline, superseded by r4.

## Accepted correction

Clean zero-area faces, wire edges and unused vertices **before** allocating the reduction budget. Preserve UV boundaries and imported per-corner normals; do not weld coincident seams back together. Apply Decimate to the cleaned visible surface while retaining the original PBR materials, then perform the same attribute-preserving cleanup again.

F03-r4 reached **14,871 triangles** in one direct reduction. Fresh FBX and GLB had no degenerate faces, unused vertices or non-manifold edges. The retained corner-normal error was zero. Five actual GLB views, a source comparison sheet and full-resolution front/hero inspection confirmed coherent leaf coverage and branching stems. Individual leaf tips and root fibres remain more angular than the high-detail source.

The original BaseColor PNG was extracted unchanged. Roughness/Metallic came from the G/B channels of the embedded glTF packed texture. The original material is double-sided and has no normal map; the provided flat `Normal.png` is an unconnected compatibility placeholder. Keep that distinction in the asset README and texture receipt.

## Recovery and accounting

`run-reststop-trellis-production.py --defer F03` resumed the full 90-row batch while F03 was repaired separately. The explicit defer accepts only an existing `failed`/`halted` asset and does not approve or rewrite its original ledger. All generation values were compared with the saved snapshot after restart and matched. Resume now reads that original snapshot, not potentially changed global app preferences. The selected manual result is recorded in `final-overrides.json`.

Use the recovery argument again when restarting this batch while the original halted record remains. Do not clear terminal states, silently replay interrupted attempts, or loosen the global error policy. Check the recorded runner PID before starting a replacement process and retain prior logs.

## Recurrence on F04

The next tree reproduced the same failure. Its remaining canonical attempt stalled at 20,578 triangles with 1,831 non-manifold edges. Applying the already-verified cleanup-first, original-UV/PBR path produced F04-r1 at **14,872 triangles**, with fresh FBX/GLB checks and five-view source comparison passing. No AI regeneration was used. This second input provides independent evidence that the correction is useful for these dense foliage assets. The current batch restart argument is `--defer F03,F04`; earlier single-ID recovery commands above describe the original incident.

## Local evidence

- Original failure: `outputs/reststop-production-2026-09-24/assets/F03_80353d85bf/stages/m1_t1/low1/blender.log`.
- Original ledger: `quality-ledgers/e56ca207d4f37a6709f846bc61b6f1a0799af2d649699a17e622b6b671c36bab.json` under the production output root.
- Remaining attempt: same hash in `reduction-recovery/`, plus `manual/F03-reduction-recovery/low2/failed-state.blend`.
- Rejected/baseline/selected candidates: `manual/F03-r1/` through `manual/F03-r4/`; r4 is selected.
- Diagnostics: `tools/inspect-reststop-reduction-floor.py`, `tools/retry-reststop-halted-reduction.py`.
- Correction: `tools/reduce-reststop-preserve-uv.py --clean-first`, `tools/reststop_mesh_cleanup.py`, `tools/extract-reststop-glb-textures.py`.
- `tools/clean-reststop-preserve-corner-normals.py` supports a cleanup-only follow-up; the failed fan-splitting experiment is retained in `tools/repair-reststop-nonmanifold-reduction.py`.

These are asset/export checks, not Unity scene or wind-animation tests. Captured with `ce-compound mode:headless` using sequential investigation and retained before/after evidence.
