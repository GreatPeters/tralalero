---
title: Thicken unsigned surface bands before reconstructing open TRELLIS props
date: 2026-09-25
category: workflow-issues
module: Rest-stop TRELLIS topology reconstruction
problem_type: workflow_issue
component: development_workflow
severity: medium
applies_when:
  - A cached source has the correct visible cavity but standard processing closes it
  - Both bounded reductions collapse an open fragmented source into shards
  - A watertight occupancy reconstruction still has thousands of small tunnels
tags: [trellis, blender, retopology, voxel, thin-shell, cavity, retry-ledger]
---

# Thicken unsigned surface bands before reconstructing open TRELLIS props

## Context

T03's three standard shape outputs capped the toilet bowl. Saved cached stage161 retained the recess, so the main agent recovered that geometry without another AI shape request. The reviewed 297,999-triangle source received one texture pass and a local exterior normal-field correction. Both permitted canonical reduction profiles still produced severe lid, seat and shell shards. Their numeric import and sampled cavity checks were insufficient to establish visual quality; both actual render comparisons were rejected.

The original three AI shape attempts and two canonical failures remain consumed. The subsequent topology work is explicitly recorded as separate local reconstruction, with every failed candidate retained. It is not a reset or another automatic canonical retry.

## Guidance

1. **Check the source's surface structure before trusting a volume operation.** This source had consistent winding according to Trimesh, but was not watertight. A signed Blender voxel remesh retained mostly rim fragments. Consistent winding alone does not establish an enclosed solid.
2. **Use unsigned occupancy for the already-reviewed surface when appropriate.** Voxelize actual triangles at a recorded pitch. Derive the subdivision limit from the largest source edge and requested pitch; a copied four-iteration limit failed on this reduced source's long triangles. The successful input needed seven iterations at a pitch of about 0.0025 of asset height.
3. **Give a thin occupancy sheet enough thickness before smoothing.** A one-cell surface band followed by a 0.65-cell Gaussian and a 0.5 isovalue produced a porous surface. It was technically watertight, yet its Euler number was -20,156. There were only four connected components, so component count alone missed the topology problem. Direct reduction stopped at 107,356 triangles. Increasing closing from two to four cells still left an Euler number of -15,642.
4. **Thicken conservatively, then recheck the intended openings.** One cell of dilation before two-cell closing, exterior fill and the same smoothing produced Euler number -38. A separate unweighted reduction of this reconstructed topology reached 14,750 triangles. This deliberately changes the surface slightly; it is not a claim of unchanged vertex positions.
5. **Review geometry before transferring appearance.** The recovered low mesh must retain the complete silhouette and intended cavity before baking the original high-source BaseColor, Roughness, Metallic and tangent normal field. Keep the high source hidden in the editable Blender file; export only the optimized mesh.
6. **Keep geometric, semantic and visual evidence distinct.** Surface-distance samples, cavity rays, fresh imports and actual PBR renders answer different questions. None alone proves the full result.

These pitch, thickness and region values were inspected for this toilet. They are not defaults for every thin prop: dilation can bridge a required gap, soften a lip or thicken a handle. Preserve the prior candidates and verify the actual result instead of silently increasing morphology until a numeric gate passes.

## Evidence and limits

- Selected asset: `outputs/reststop-production-2026-09-24/manual/T03-r6/final`.
- High source: `manual/T03-r3/source`; saved open geometry before material processing: `manual/T03-r2/source` under the same output root.
- Rejected canonical results: `manual/T03-r3/source/low1` and `low2`.
- Failed signed reconstruction: `manual/T03-r4/shape`; failed thin-band reductions and occupancy experiments remain under `T03-r5` and `T03-volume-r1` through `T03-volume-r3`.
- Successful occupancy: `manual/T03-volume-r4/reconstruction.json`, with one-cell dilation, two-cell closing and recorded source hash/grid settings.
- Tools: `reconstruct-reststop-toilet-volume.py`, `reconstruct-reststop-toilet-topology.py`, `verify-reststop-toilet-cavity.py` and `compare-reststop-surface-distance.py` in `tools/`.

The final GLB and FBX passed fresh import checks at 14,750 triangles. All three cavity rows in both formats retained coverage, with maximum median depth drift about 0.00913 of source height. The cavity verifier passed an open-source control and rejected the capped standard output. Surface-distance samples had medians about 0.0034–0.0038 of height and 99th percentiles below 0.0074; these are sampled measurements, not an exhaustive Hausdorff bound. Actual final PBR views retained the lid, seat, tank and ceramic shell. Small edge rounding and strong glossy underside reflections remain documented.

## Related

- [Recover cached geometry before destructive postprocessing](recover-cached-trellis-shape-before-postprocessing-loss-2026-09-24.md).
- [Preserve material regions during normal repair](preserve-material-regions-when-repairing-trellis-normal-artifacts-2026-09-24.md).
- [Keep thin-part repairs within recorded generation budgets](repair-thin-trellis-parts-without-resetting-generation-budgets-2026-09-24.md).

Captured with `ce-compound mode:headless` from retained local candidates and actual validation, sequentially under the repository's tool mapping. Existing AGENTS.md solution-store guidance provides discoverability; no instruction-file edit was needed.
