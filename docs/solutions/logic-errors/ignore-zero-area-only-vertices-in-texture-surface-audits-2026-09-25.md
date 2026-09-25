---
title: Exclude zero-area-only vertices from the texture surface preservation gate
date: 2026-09-25
category: logic-errors
module: Rest-stop TRELLIS geometry normalization audit
problem_type: logic_error
component: testing_framework
symptoms:
  - A visually unchanged truck surface failed a strict texture geometry audit
  - The largest point error belonged to a vertex referenced only by a zero-area triangle
root_cause: logic_error
resolution_type: code_fix
severity: medium
tags: [trellis, glb, geometry-validation, degenerate-faces, regression-test]
---

# Exclude zero-area-only vertices from the texture surface preservation gate

## Problem

The geometry audit compared every source position with the textured output, including unused positions and positions referenced only by collapsed faces. V05's texture normalization removed 13 numerically collapsed triangles. Four discarded auxiliary positions exceeded the point tolerance even though every positive-area surface vertex remained within 2.83e-8 after normalization.

## What Didn't Work

The first report's all-position maximum was 0.0001268, above the unchanged `diagonal * 1e-6` tolerance. A least-squares uniform scale/translation fit did not remove that outlier, so the problem was not a bounding-box alignment error. The worst source vertex's incident face had exactly zero area. The failed report is retained.

A batched controller also wrote a visual-pass note before inspecting this technical failure. The actual image review was valid, but the proof claim was premature. Final selection used the corrected proof only after its regression tests and real-data rerun passed. Dependent approvals must inspect a tool's exit/status before citing it as successful; printing the result is not such an inspection.

## Solution

`tools/verify-reststop-texture-geometry.py` still measures and records **every** source position. For the surface-preservation gate, it uses the union of vertices referenced by source triangles whose area is strictly greater than zero:

```python
source_areas = np.linalg.norm(
    np.cross(source_points[:, 1] - source_points[:, 0],
             source_points[:, 2] - source_points[:, 0]), axis=1
) * 0.5
surface_vertices = np.unique(a.faces[source_areas > 0])
max_surface_distance = float(distances[surface_vertices].max())
```

The numeric surface tolerance was not increased. The existing no-extra-triangles and negligible-missing-area gates remain. The report now distinguishes all-position error, positive-area surface error and the number of zero-area-only/unused positions excluded from the surface gate. No positive-area triangle is excluded by an epsilon threshold.

For V05, all 121,984 positions remain in the diagnostic; 121,895 belong to area-bearing surfaces. The maximum surface error is 2.828e-8. There are no added triangles, and the normalized missing-area total is about 8.3e-21. The selected final folder contains the corrected proof while the first failure remains in the parent texture folder.

## Verification

`tools/test-reststop-texture-geometry.py` exercises the actual audit subprocess on GLB fixtures:

- Uniform normalization preserves a real box surface.
- Removing an internal zero-area line preserves the same box even though its discarded positions are far from the retained vertices.
- Moving a positive-area box vertex while retaining the same bounds still fails.

All three regressions pass. The actual V05 corrected report is `manual/V05-r4/texture1/geometry-normalization-proof-r2.json` under the production output root. Its positive-area result is also preserved with selected `low1`. The same audit subsequently passed V06's textured axle reconstruction.

This proves position/unoriented-triangle surface retention, not material, UV, winding or visual correctness. Keep actual render comparison and semantic tests such as windshield coverage and axle counts.

## Related

- [Thicken unsigned surface bands before retopology](../workflow-issues/thicken-unsigned-surface-bands-before-trellis-retopology-2026-09-25.md).
- [Preserve material regions during normal repair](../workflow-issues/preserve-material-regions-when-repairing-trellis-normal-artifacts-2026-09-24.md).

Captured with `ce-compound mode:headless` from actual failure, isolated diagnostics, regression controls and corrected output. Research ran sequentially; existing AGENTS.md discoverability instructions were sufficient.
