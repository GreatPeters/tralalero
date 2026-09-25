---
title: Preserve material regions when repairing TRELLIS normal artifacts
date: 2026-09-24
last_updated: 2026-09-25
category: workflow-issues
module: Rest-stop TRELLIS prop production
problem_type: workflow_issue
component: development_workflow
severity: medium
applies_when:
  - A reduced generated prop passes import checks but shows wavy hard-surface shading
  - One generated mesh contains both soft upholstery and rigid frame regions
  - A bounded generation or reduction retry policy has already been exhausted
tags: [trellis, blender, normals, pbr, uv, material-regions, quality-review]
---

# Preserve material regions when repairing TRELLIS normal artifacts

## Context

F08, an olive upholstered chair, passed high-density shape and material review but failed both permitted Blender reduction passes. The under-seat frame showed sawtooth/wavy shading despite valid 14,365-triangle FBX and GLB imports. The upper cushion and lower frame shared the generated material.

## Guidance

1. Keep the bounded attempts and their receipts. Leave the automatic job as `review_needed`; do not reset the attempt ledger or write a false pass. Store an accepted manual correction in a separate selection manifest.
2. Compare neutral and textured views. Separate geometry defects, vertex-normal shading and baked normal-map artifacts before applying a broad geometric repair.
3. Scope normal treatment to the affected construction region. Applying weighted normals across the whole chair introduced cushion faceting. Restricting the modifier to the lower frame preserved the soft cushion but still left artifacts amplified by the baked normal channel.
4. Duplicate the material only for affected lower-frame faces, and disconnect the damaged Normal input on that duplicate. Keep Base Color, Roughness, Metallic, UVs and upper-cushion material connections. This case selected vertices/faces below 0.49 of the chair's height after checking the construction. That threshold is specific to this chair, not a general classifier for props.
5. Preserve geometry and verify the actual exports. The chosen F08-r3 retained 14,365 triangles before and after, packed the Blender data and embedded exported textures. Review front/back/top/neutral views and reopen both FBX and GLB independently.
6. Record remaining source defects honestly. The accepted correction improved the frame's shading; it did not repair every geometric detail. A small rear-leg dent and stronger-than-reference cushion seam/gloss remained minor notes.

## Why this matters

A technically valid reduced mesh can look worse than its high-density source because shading channels interact with the new surface. A global normal fix can improve the rigid portion while damaging soft regions. Region-specific material changes avoid that regression and leave a reversible, evidence-backed correction.

## Evidence

- Script: `tools/repair-reststop-chair.py` (now refuses to overwrite the accepted revision).
- Rejected automatic candidates: `outputs/reststop-production-2026-09-24/assets/F08_020af4bd20/stages/m1_t1/low1/` and `low2/`.
- Rejected/unused manual candidates: `outputs/reststop-production-2026-09-24/manual/F08-r1/` and `F08-r2/`.
- Accepted correction: `outputs/reststop-production-2026-09-24/manual/F08-r3/`, including `repair.json`, `visual-review.json`, `validation.json` and `quality/` renders.
- Selection: `outputs/reststop-production-2026-09-24/final-overrides.json`. The live automatic state remains unchanged.

## Later wall-panel case

B02 reproduced the same failure class on a completely rigid stone wall: both bounded reductions had broad neutral-shading lobes absent from the source. In B02-r1, repairing only the front/back material left trough-like side-edge shading because those edges still used a normal bake authored for the old vertex normals. B02-r2 applied area/angle weighted normals and removed the stale Normal input across the inspected rigid panel. Vertex positions, UVs, other PBR channels and the second reduction's 13,578 triangles remained unchanged. Fresh FBX/GLB and front/back/top/neutral visual checks passed.

Evidence: `tools/repair-reststop-hard-surface.py` and `outputs/reststop-production-2026-09-24/manual/B02-r2/`. The original `Normal.png` is retained for provenance, but the repaired materials intentionally do not link it; do not blindly reapply it when rebuilding a material. The accepted wall still has minor source grout pits and a regular grid in place of the reference's staggered joints. The broader material scope is justified by this object's entirely rigid construction and must not be copied to the upholstered chair or organic characters.

## Later fascia case

B09 reproduced visible triangular depressions in the finished ochre PBR face after both bounded reductions. Unlike neutral-only low-poly faceting that a correct normal map may legitimately compensate, this was visible in the final material too. The same rigid-surface correction restored the broad face while retaining all 14,440 triangles and vertex/UV positions. Five actual GLB views and fresh FBX/GLB validation passed; minor rear-rim waviness was recorded. Evidence is in `outputs/reststop-production-2026-09-24/manual/B09-r1/`. Do not infer the exact bake-pipeline root cause from this successful correction alone.

## Ceiling orientation and repeated fascia artifacts

B12 also required the rigid-surface correction after both bounded reductions introduced new fascia dents. `manual/B12-r1` retained 13,907 triangles, vertex positions, UVs and visible timber-board texture. Fresh FBX/GLB and eight rendered views passed. Its intended visible face is underneath: default upper oblique/top views alone could not establish whether the plank appearance was missing. `reststop-production-review-render.py --underside-only` adds lower oblique, bottom-flat and lower neutral views with lights mirrored below the asset. The sheet builder includes these only when present, and the package keeps those images and revised contact sheets. Preserve upper views too; choosing a useful underside preview does not remove the recorded source artifacts on the concealed upper face.

## Hose color bleed requires a different local correction

G02's two canonical reductions both projected light cabinet colors onto the black hose loops. A direct original-UV reduction, successful on foliage, was tested separately and rejected because the hard cabinet became visibly fragmented. Keep the smooth canonical geometry when the actual defect is confined to its material region.

The selected G02-r4 uses the aligned high source in the saved Blender file. For each lower outer candidate face, three nearest-surface samples map through the high mesh's UVs to its original BaseColor. Neutral dark votes identify rubber; saturated pigments are excluded. The lowest detected green-nozzle geometry establishes an upper height boundary so nozzle colors remain outside the correction. The selected 3,781 faces receive a separate dark nonmetallic rubber material; all vertex positions and UVs remain unchanged. The original non-hose PBR material is retained. Intermediate r2/r3 classifier candidates remain unselected.

Fresh FBX/GLB imports passed at 13,893 triangles, and each export passed 612 central opening rays across two heights and two directions. Actual front/back renders confirmed the large hose-color contamination was removed. Small edge/shading and coupling-detail differences remain. The asset README requires retaining the embedded materials rather than reapplying the contaminated baked maps to the rubber region. Evidence: `tools/repair-reststop-hose-material.py` and `outputs/reststop-production-2026-09-24/manual/G02-r4/`. This region classifier is explicitly restricted to the inspected G02 construction.

## Rigid objects can still need separate rounded-surface handling

R07's two reductions created visible triangular dents in a flat yellow cabinet panel. An all-face rigid correction removed those dents, but unnecessarily exposed faceting on the rounded cap and motor barrel. R07-r2 restricts weighted normals and the stale-normal-map removal to 6,551 inspected broad cabinet/base faces, using world-space height and face orientation. Original corner normals and material links remain on the rounded cap, barrel and edges.

`tools/repair-reststop-barrier-normals.py` asserts unchanged vertex positions and loop count, merges the selected weighted normals with the retained original corner normals, and preserves UVs and other PBR channels. The final 13,727-triangle result passed five actual GLB views, full-resolution source comparisons and fresh FBX/GLB checks. Keep its two embedded materials: the original Normal.png is still useful on rounded regions and must not be reapplied to the repaired flat faces. Evidence is in `manual/R07-r1` and selected `manual/R07-r2` under the production output root. Entirely rigid construction alone is not a reason to discard every region's normal bake.

## Compare selective normals against a matching boundary baseline

T06's sink vanity required preserving the curved basin and drain while repairing the flat stone/wood exterior. The first assertions compared raw corner-normal vectors and failed with a difference near 0.553. A no-op `normals_split_custom_set` experiment reproduced that large difference: two imported normals had length about 0.447 rather than 1. Their directions were not changing by 0.553. Compare normalized directions and record normalization of nonunit inputs.

That was only one cause. Mixing weighted exterior normals with original neighboring normals without separating their smooth-fan boundary changed an untreated direction by about 0.0131. Marking only the repair boundary sharp reduced the maximum source-relative drift to 0.001778. A separate round-trip probe showed that changing sharp boundaries can itself change the represented custom normals. Do not silently enlarge a tolerance until the candidate passes.

`tools/repair-reststop-vanity-normals.py` now restores the original smooth/sharp flags, separates the two treatment regions, assigns only normalized original directions, and captures this **boundary-only baseline**. It then assigns the mixed original/weighted field and asserts untreated directions match that baseline within 0.001. In the selected T06-r7, the measured difference from that baseline is zero. The baseline's 0.001778 source-relative drift and the two normalized input vectors are reported separately. This proves the local modifier does not further alter untreated directions; it does not claim bit-identical normals through every importer.

The selected candidate keeps all 14,679 triangles, vertex positions, loop order and UV values. Only 4,056 exterior faces receive the corrected normal field and a material copy with the stale Normal input disconnected. Five actual views, source comparison, full front-side view and fresh GLB/FBX validation passed, including a second import check after the unexpected reboot. Retain both embedded materials. The basin and other untreated surfaces still use their original Normal map. Small source mounting marks remain documented.

Evidence: `reviews/T06-normal-roundtrip-r1.json`, `T06-normal-roundtrip-r2.json`, retained failed execution logs and `manual/T06-r7/{repair.json,visual-review.json,validation.json}` under the production output root. Failed diagnostic candidates consumed no additional AI or canonical reduction attempts. Captured with `ce-compound mode:headless`; the original two reduction failures remain in the automatic ledger.

## Related references

- [Fit bones before transferring TRELLIS skin weights](fit-bones-before-transferring-trellis-skin-weights-2026-09-24.md).
- [Verify imported timing and mesh ownership before Blender previs rendering](verify-imported-timing-and-mesh-ownership-before-blender-previs-render-2026-09-23.md).

Captured with `ce-compound mode:headless` from the retained local script and acceptance evidence, using sequential research under the repository's tool mapping. The existing knowledge-store instruction in AGENTS.md already provides discoverability. This records one completed correction while the rest of the production continues.
