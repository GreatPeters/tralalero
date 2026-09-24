---
title: Recover cached TRELLIS geometry before postprocessing destroys thin parts
date: 2026-09-24
last_updated: 2026-09-24
category: workflow-issues
module: Rest-stop TRELLIS prop production
problem_type: workflow_issue
component: development_workflow
severity: medium
applies_when:
  - Multiple exported TRELLIS shapes collapse into fragments despite an intact conditioning image
  - A valid decoded shape may still be present in the current ComfyUI node cache
tags: [trellis, comfyui, cache, intermediate-mesh, decimate, thin-parts, recovery]
---

# Recover cached TRELLIS geometry before postprocessing destroys thin parts

## Context

P04's three standard shape exports all lost the mop shaft and grip, leaving a small jagged head. Reproducing the installed image preprocessing with the existing CPU rembg model showed a complete conditioning image. The damaged final export did not establish that TRELLIS itself had failed to generate the object.

While m3's visual-review gate still held the queue, cached intermediate exports showed complete geometry at nodes 217 (decoded shape), 193 (first hole fill) and 161 (quad reconstruction). The failure was downstream of 161 and before the final 257 export, in the simplification/second-hole-fill chain. The exact failing node inside that interval was not isolated; do not label it a proven neural-generation or single-library defect.

## Guidance

Inspect before consuming more AI attempts, and preserve the current cache before a new workflow can evict it. Keep the original graph's nodes, inputs and image hash unchanged.

`tools/inspect-reststop-cached-shape.py` used two partial-execution requests:

1. Add a standard `PreviewImage` output depending only on the already uploaded image loader. Execute that output alone and read `execution_cached` from the request history. Refuse recovery if required model/shape/decode/postprocess nodes are absent.
2. Add conversion/export/preview nodes consuming the **verified cached** intermediate meshes. Execute only those new outputs, then check the second history also reports the required original nodes cached. Copy the resulting files outside the transient backend cache.

The saved histories verified zero new AI sampling. The queue was empty, a real review gate prevented concurrent generation, and the original graph was identical. This is not permission to blindly resubmit a graph when the cache is missing. An interrupted or cache-miss diagnostic must be audited before any replay.

The diagnostic now takes explicit `--id`, `--mesh-number` (1–3) and `--revision` arguments, and checks these against the still-pending shape gate. This permits investigation at the first malformed output before approving another AI attempt. P04's original diagnostic folder remains unchanged; subsequent invocations use separate asset/attempt/revision folders.

Use a separate local revision to process the recovered geometry. P04's node 161 contained 1,467,446 triangles. Blender Decimate retained its shaft, grip hole, clamp and individual cords at the saved 300,000-face intermediate limit. Export validation removed one invalid face, yielding **297,998 triangles**. Fresh GLB and BLEND checks found no degenerates, unused vertices or nonfinite coordinates, and a subsequent `Mesh.validate` changed no data. Five actual views passed shape review; the cords remained more curled than the reference.

## Why This Matters

Regenerating a new seed repeats the same destructive postprocessing path and can waste the allowed shape budget. A raw intermediate export separates neural reconstruction quality from later mesh cleanup. It also preserves the user's saved generation values and original failed-attempt history.

## When to Apply

- Thin handles, straps or repeated cords disappear in the final mesh.
- The input and conditioning image remain intact.
- The current backend still retains the original nodes; inspect before releasing the review gate.

## Examples

Evidence is under `outputs/reststop-production-2026-09-24/reviews/P04-preprocess/` and `reviews/P04-cached-stages/`, including both submitted partial graphs and their histories. `tools/recover-reststop-mop-shape.py` performs the local intermediate reduction; `tools/verify-reststop-intermediate-shape.py` checks fresh geometry before texturing. The approved intermediate is `manual/P04-r1/source`.

The original ledger remains `review_needed` with three recorded shape attempts. Texturing the recovered shape used the **remaining shared texture budget**, via `texture-reststop-corrected-shape.py`; it did not reset counts or count an unfinished intermediate as a completed game asset.

Further inspection showed that intact appearance did not mean sound topology: nodes 217/193 already contained 199,139 non-manifold edges, and node 161 contained 1,103. Both canonical final reductions destroyed the shaft and bake. Fine direct voxel reconstruction retained too many tiny internal cavities, while a coarser candidate became porous. These rejected candidates and failure-state blends were retained.

The accepted P04-r7 uses unsigned surface occupancy, two-cell gap closing, exterior flood filling, a 0.65-cell smoothed field and nondegenerate marching cubes at a relative pitch of 0.003. This avoids relying on the input's inconsistent interior/winding. The volume output was watertight; scale alignment accounted for TRELLIS texturing normalizing the earlier approximately 0.5-unit shape to approximately 1 unit. Subsequent reduction and 2K rebake from the unchanged accepted material passed at **14,799 triangles**, with zero non-manifold edges/degenerate faces in fresh final imports. Five-view comparison retained the recognizable mop and cord appearance. Some narrow cord gaps merged and minor shaft/grip marks remain.

Implementation is in `tools/reconstruct-reststop-mop-volume.py` and `tools/retopologize-reststop-mop.py`; the final geometry source is `manual/P04-volume-r2/model.glb`. No additional AI requests were used by these final corrections. This distinguishes surface recovery, topology quality and final visual approval instead of treating a successful intermediate render as proof of all three.

## Repeated openings need depth checks as well as appearance

R12's standard linear-grating export looked as though most slots were capped. Three transverse profiles of 2,049 vertical rays each confirmed only five intervals deeper than30% of the object's height. Cached decoded/first-fill stages had23 such intervals and reconstruction had22. This separated actual geometric closure from a merely bright channel floor. These are sampled depth intervals, not a general proof of mesh topology or fluid flow.

The original attempt was retained as `review_needed`; no additional neural shape was requested. `tools/repair-reststop-linear-grating.py` retained the generated lower channel/supports below60% of its height and rebuilt the upper rim/end borders with35 capsule-shaped through-slots, based on the reference's repeated pattern. r1 exposed incomplete end borders; r2 completed those borders but fresh inspection found9,662 zero-area faces. r3 removed those faces/wire edges with zero removed surface area and zero retained-corner-normal error. Fresh BLEND/GLB checks passed at107,564 triangles, and all three profiles measured35 deep intervals. Texture and final reduction were reviewed separately.

The first texture and first canonical final reduction subsequently passed at **14,726 triangles**. `tools/verify-reststop-grating-openings.py` freshly imported GLB and FBX and measured35 deep intervals on each of three transverse rows per format (2,049 rays per row). Five rendered views also preserved the channel, slots and PBR finish. The selected folder is `manual/R12-r3/texture1/low1`, and the package retains the opening receipt plus both detailed profiles. Original shape attempts remained at one.

Read-only evidence comes from `tools/inspect-reststop-grating-depth.py`, `reviews/R12-m1-*-depth.json` and `manual/R12-r3/source/opening-depth.json`. Repeat the semantic depth check on the eventual textured and reduced exports; ordinary triangle/UV checks cannot establish that the drainage slots remained open. Keep global generation and hole-fill settings unchanged when using an asset-specific recovery.

## Related

- [Thin-part repair without resetting budgets](repair-thin-trellis-parts-without-resetting-generation-budgets-2026-09-24.md).
- [Foliage reduction and UV preservation](preserve-uv-seams-and-corner-normals-for-trellis-foliage-2026-09-24.md).

Captured with `ce-compound mode:headless`, sequentially. Existing docs/solutions discovery instructions remain sufficient. This adds intermediate-cache diagnosis to earlier repair guidance; it does not replace the asset-specific visual checks.
