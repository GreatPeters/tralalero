---
title: Restore TRELLIS slat relief after decimation
date: 2026-09-24
category: workflow-issues
module: Rest-stop generated architectural props
problem_type: workflow_issue
component: development_workflow
severity: high
applies_when:
  - A generated panel contains repeated thin grooves or slats
  - A normal correction improves shading but may conceal lost geometry
  - Comparing raw shape, textured source and reduced meshes at different scales
tags: [trellis, blender, retopology, repeated-geometry, normals, raycast, pbr, scale]
---

# Restore TRELLIS slat relief after decimation

## Context

B03 needed a framed wood panel with narrow vertical slats across its front. TRELLIS m1 omitted part of that field and invented door hardware; m2 had an almost flat front. The third permitted shape restored real periodic front grooves and passed the shape/material gates. Both automatic reductions then damaged that shallow repeated relief.

## Guidance

1. **Inspect the intended face.** Rear slats below support battens do not satisfy a reference showing an unobstructed slat front. Keep the failed shape receipts and bounded attempt count. B03 used three shapes and one TRELLIS texture generation; no counter was reset.
2. **Separate normal artifacts from lost geometry.** Weighted normals reduced the crumpled shading but did not restore the periodic front groove profile. A normal bake can legitimately recover visual detail on a low mesh, so a neutral render alone is not proof of failure. Here ray-cast sections confirmed that the defining repeated geometry had become irregular.
3. **Measure scale before comparing profiles.** The raw shape was about 0.501 units high, while the textured/reduced versions were about 0.999 units high. Absolute-depth plots cannot establish relative detail loss across those scales. Normalize by asset height or compare the retained `Detailed_Source` and `Asset_Optimized` in the same `.blend` coordinate frame.
4. **Do not assume nearest-loop-normal transfer is safe.** B03-r2 transferred source custom normals and rebaked PBR but produced severe shading spikes and black patches on the thin repeated surface. It was rejected. Matching object bounds and unchanged vertices did not make that transfer visually correct.
5. **Retopologize only the failed field from the source geometry.** B03-r3 ray-sampled 1,025 points from the retained high-density front section at 65% height, simplified the profile to 202 knots, and extruded that measured profile along the intended straight slat field. The frame and rear shapes were retained. New UVs and 2K PBR maps were baked from the retained TRELLIS source.
6. **Verify geometry and exports independently.** The reconstructed native section's maximum deviation was 0.0001281 units against a 0.0005 limit. Fresh exported GLB sections retained the periodic grooves. Fresh FBX and GLB both contained 14,210 triangles with no degenerate faces or loose vertices. Use actual triangulation, not `sum(n-2)`: the pre-export polygon estimate included four extra collinear cap triangles.

## Evidence and limits

- `tools/inspect-reststop-slat-profile.py` reads multiple height sections without editing the source.
- `tools/restore-reststop-slat-surface.py` performs the B03-specific front retopology and fresh bake.
- Accepted result: `outputs/reststop-production-2026-09-24/manual/B03-r3/`, including `repair.json`, `validation.json`, `visual-review.json` and the real render views.
- Normalized comparison: `outputs/reststop-production-2026-09-24/reviews/B03-normalized-profile-proof.png`; the older absolute-scale diagnostic is retained but is not the comparison proof.
- Earlier B03-r1 and B03-r2 remain rejected and outside `final-overrides.json`.

This profile extrusion assumes a straight, vertically repeating field. It is not a general retopology method for curved panels, vehicles or organic models. Minor original edge pits, rear irregularity and tiny lower join marks remain documented. The overall 90-asset production was still running when this completed correction was recorded.

## Related

- [Preserve material regions when repairing normal artifacts](preserve-material-regions-when-repairing-trellis-normal-artifacts-2026-09-24.md).
- [Fit bones before transferring skin weights](fit-bones-before-transferring-trellis-skin-weights-2026-09-24.md).

Captured with `ce-compound mode:headless` from retained local evidence, using sequential research under the repository tool mapping. AGENTS.md already documents the searchable solution store.
