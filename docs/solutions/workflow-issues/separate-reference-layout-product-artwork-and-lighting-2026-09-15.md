---
title: "Separate reference layout, product artwork and surface lighting in image edits"
date: "2026-09-15"
category: workflow-issues
module: "Reference-based product image editing"
problem_type: workflow_issue
component: development_workflow
severity: medium
applies_when:
  - "Matching an existing product composition while replacing the products."
  - "Relighting reflective packaging while preserving its printed artwork."
tags: [imagegen, reference-layout, product-identity, typography, lighting, verification]
---

# Separate reference layout, product artwork and surface lighting in image edits

## Context

A portfolio image request supplied actual carton/bottle photos and a different brand's arrangement reference. The user repeatedly corrected sparse layouts, flat bottle lighting and failure to follow the reference composition. Prompts describing a mood or a dense group produced attractive alternatives but did not preserve the requested placement.

An in-place replacement pass followed the reference's relative positions more closely, but reformatted label text and merged two overlapping cartons into one long face. A second pass restored the main print orientation and distinguished the carton faces. Exact typography, geometry and reference boundaries remained unverified. This record captures an improved workflow and its limits, not a verified pixel-identical final.

## Guidance

1. Give each image one authority. The arrangement reference is the base edit target when the user says to use its composition. Original product photographs control shape, proportions and printed artwork. Earlier generated variants are not authoritative label sources.
2. Describe a one-to-one replacement map using existing object positions, viewing direction, overlap order and overall occupied area. More objects alone do not reproduce a reference layout.
3. Preserve actual product proportions instead of stretching products into another brand's silhouettes. If the reference contains a jar or short box absent from the real range, recognize that an end-view substitution introduces an inferred view; do not silently claim an exact product photograph.
4. Specify surface shading separately from floor shadows. A silver cylinder needs a bright left reflection, a curved midtone transition and a genuinely shaded right side under a single left light. Caps, pumps and neighboring-object shadows must agree with that light.
5. Compare actual print layout, not just recognized words. Check title scale, rotated fine print, wordmark/volume orientation, large numerals and whether overlapped boxes accidentally became one face.
6. Use a targeted correction pass after the layout is acceptable. Keep object centers and lighting while restoring the original artwork. Recheck because a correction can extend an object or invent end-face branding.
7. Save the exact output path returned by each generation; never infer the output from whichever file is newest. Preserve inspectable PNG copies and prompts in the preview folder.
8. Report fidelity boundaries plainly. A prompt demanding unchanged letters or a source path does not prove unchanged pixels. Do not label generated output as exact production artwork when visual differences remain.

## Why This Matters

Layout, product identity and lighting are separate acceptance criteria. A successful shadow edit can coexist with incorrect labels. A closer composition can introduce impossible geometry. Reviewing these independently avoids treating visual polish as evidence of product accuracy.

## When to Apply

- The user provides different images for the real product and desired arrangement.
- Repeated variants share a mood but miss the requested positions.
- Reflective bottles look pasted in because only the ground received shadows.
- Portfolio or commercial use makes printed detail and proportions important.

## Examples

A weak direction was “make a dense premium group with strong shadows.” A more useful direction was:

> Edit the arrangement reference directly. Keep its object centers, overlap order, camera and negative space. Replace each item using the original product sources. Keep source dimensions and printed orientation. Shade the cylinder itself from bright left to dark right, with coherent cap reflections and mutual shadows.

Observed correction checks included:

- Floor shadows were present while bottle right sides remained uniformly bright.
- Fine print moved from a rotated upper description group to horizontal mid-carton paragraphs.
- Wordmarks and volume labels were turned into new horizontal captions.
- Two overlapping cartons became one continuous face with duplicated numerals.
- A correction fixed the main print direction but still extended the lower carton beyond the reference.

Evidence and prompt history are in `tmp/image-previews/mizon-reference-layout-20260915/prompts.md`. The source attachments remain separate from generated results.

## Follow-up: rejected proportions

The user rejected the previous final for visibly incorrect proportions. A new prompt specifying aspect ratios while retaining that rejected output as a lighting reference still preserved the wrong geometry. Removing the rejected output and using original product photos plus the arrangement reference improved bottle length and cap fraction, but again fused the middle cartons. A local correction with an explicit horizontal offset, front/back overlap and separately placed original artwork made the two objects distinct.

Do not treat numerical prompt constraints as measured acceptance. Source-image estimates were carton height/front-width about 4.07:1 and complete bottle height/diameter about 4.42:1, with the transparent cap about 29% of total height. Compare repeated instances independently: the selected revision still varies in central carton width and fine lettering. It remains a revised candidate, not an exact product composite. If exact geometry and lettering remain mandatory, propose source-preserving compositing rather than continuing to describe generative approximations as finished.

Revised candidate, three prompts and remaining limits: `tmp/image-previews/mizon-proportion-correction-20260915/prompts.md`.

## Follow-up: repeated-instance scale is a separate acceptance gate

The user also rejected different sizes among copies of the same package. Three further built-in edits tried a single-master duplication instruction, explicit pixel-sized carton rectangles, and a fresh layout-reference edit. Numerical coordinates did not force congruent rectangles. The reference-first pass restored a tighter cluster and improved the exposed rear carton width, but the far-right carton remained wider and print scale still drifted. No exact-scale fix was verified.

Review full front-face widths, full lengths, bottle diameters, cap fractions and artwork scale across instances separately from layout. A partial rear carton must retain the master width. Do not infer compliance from a prompt using words such as duplicate, identical or orthographic. Do not describe this generative operation as actual pixel-preserving cutout duplication. If repeated edits still fail the gate, report the remaining defect rather than presenting it as solved.

Evidence: `tmp/image-previews/mizon-consistent-scale-20260915-v2/prompts.md` and `mizon-compact-revised.png` in the same folder. This is a revised candidate with unresolved fidelity limits.

The subsequent review-application pass strengthened bottle right-side shading and replaced the jar-like circular end with an inferred clear-cap/pump view, but merged the center again. A local overlap repair then changed the central layout and reduced the number of central cartons. Add object count and untouched-neighbor positions to the local-edit regression check; visible separation alone does not prove the requested geometry was preserved. Repeated widths and lettering remain unresolved. Evidence: `tmp/image-previews/mizon-applied-review-20260915-v3/prompts.md`, with both first-pass and selected PNGs saved there.

## Verified repeated-scale correction using source masters

Following the proposed direct-cutout workflow and the user's continued instruction to preserve identical product proportions, the next revision used deterministic source compositing. The original PNGs already had alpha channels. Cropping their alpha bounds and applying one uniform source scale per product produced a single carton master and a single bottle master. Every front-facing instance then used the same bitmap with translation only. Lighting changed RGB brightness without moving artwork pixels or changing alpha geometry.

Five carton instances use the same 156 x 627 raster canvas (source scale 0.55); three bottle instances use the same 136 x 593 canvas (source scale 0.40). These canvas dimensions include subpixel padding from uniform scaling. Instance dimensions were checked, and opaque artwork sample colors matched across all unobscured copies. This verifies repeated-instance geometry, not physical carton-to-bottle dimensions or photorealistic lighting. End-on views remain inferred and explicitly identified in the artifact README.

The first composite also exposed a separate shadow defect: semi-transparent overlapping shadow polygons produced artificial dark stripes. Rendering a single union mask before applying opacity removed that stacking artifact. Keep product shape invariants and lighting-layer quality as separate review gates.

Reproducible script, original-derived masters, output, instance table and verification: `tmp/image-previews/mizon-source-composite-20260915-v1/`. Do not feed this flattened image through a generative whole-scene retouch and assume the verified geometry will survive. Future position changes should reuse the same masters.

## Related references

When the user subsequently requested a fresh creation instead of editing, a new built-in generation used only the original product photos and green arrangement reference. Geometry checks from the deterministic composite do not transfer to that output: carton dimensions and print scales varied again, the rear carton was omitted, and end-face branding was invented. Keep verification scoped to a specific artifact and explicitly reclassify a newly generated image as unverified. Evidence: `tmp/image-previews/mizon-fresh-start-20260915-v1/prompts.md`.

- [Persist generated images and provide preview links](persist-generated-image-before-next-prompt-2026-05-16.md)
- [Distinguish generated previews from reusable source assets](validate-real-alpha-and-rebuild-ui-as-separate-assets-2026-09-15.md)
