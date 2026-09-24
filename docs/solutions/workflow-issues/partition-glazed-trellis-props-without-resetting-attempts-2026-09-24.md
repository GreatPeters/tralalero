---
title: Partition glazed TRELLIS props without resetting generation attempts
date: 2026-09-24
last_updated: 2026-09-25
category: workflow-issues
module: Rest-stop TRELLIS prop production
problem_type: workflow_issue
component: development_workflow
severity: medium
applies_when:
  - A recovered display case contains multiple glazing layers and internal shelves
  - Whole-mesh texturing is too slow for the preserved per-request timeout
  - A cancelled request must remain counted before a bounded retry
tags: [trellis, glazing, transparency, partition, timeout, cancellation, retry-budget]
---

# Partition glazed TRELLIS props without resetting generation attempts

## Context

S02's decoded shape contained two shelves, but standard postprocessing removed one. A separate cached-geometry recovery preserved both. Its whole-mesh texture request then needed 53 minutes 15 seconds for 8 of 25 steps. One transparent-looking pane was actually four closely spaced surfaces. Assigning transparency only to the outermost surface left an opaque inner layer.

The first timing estimate considered the later reduction in DINO substeps but missed the rescaled-time CFG interval. In this installed sampler, `rescale_t=3`, 25 steps, interval `[0,.9]`, DINO cap `.92`, lock `.35` and 2 substeps give 4 model evaluations in steps 1–6, 5 in step 7, 7 in steps 8–11, then 3 in steps 12–25. Step 7 starts outside CFG but its second substep crosses into the interval. Step 8 took 547 seconds. Do not extrapolate the first easy step across this schedule. `reviews/S02-sampling-cost-schedule.json` records this code-derived 99-evaluation schedule and source-file hashes; it is not a runtime profiler trace.

The selected result is `manual/S02-r11`, with 11,971 triangles. Fresh GLB/FBX checks, visibility probes and rendered review passed. The TRELLIS body is retained; unstable glazing and tray reductions were replaced with clean fitted inserts.

## Guidance

1. Preserve the recovered complete geometry before changing material or partitions. Inspect illuminated cutaways rather than treating an opaque clay render as proof that interior geometry is absent.
2. Inspect every front-to-back surface crossing. S02 has 4 glazing surfaces; alpha `.035` per surface gives sampled combined transmission about `.867`. This is a game-oriented alpha approximation, not physical glass refraction.
3. Classify the curved front/top by ray envelopes. Locate the actual interior back wall from the largest central empty interval, not the rearmost service-box wall. Constrain side glazing spatially; a normal-only filter left small opaque fragments on reconstructed pane detail.
4. Partition faces exactly once into body, glass and shelves. Preserve vertex positions, UVs and corner normals; keep a face-index receipt. A body-only texture input is an intermediate part, never a complete accepted asset.
5. Preserve generation settings and counts. A cancelled first texture still consumes attempt 1. The second texture uses the existing deterministic next seed and the same sampling/texture settings.
6. Confirm a cancellation before retrying. An `/interrupt` response is only a request. This sampler lacked per-step interruption checks. Only after proving a single owned request and exact task-owned launcher/worker command lines were those two processes stopped. Restart the same verified backend; do not terminate unrelated Python processes or another port.
7. Keep a cancellation receipt containing the original request ID, consumed attempt, confirmed worker termination and empty queue. `reststop_texture_retry.py` checks its SHA-256 and matching request before admitting the next remaining attempt. Submitted, ambiguous, edited or foreign-request records cannot authorize replay.
8. After body texturing, fit its normalized bounds back to the original body and apply the same uniform transform to the preserved parts. Check sampled body-surface residuals and the complete triangle count, then review the actual assembled PBR model.
9. Final reduction still needs separate alpha preservation, fresh GLB/FBX imports, rendered views and explicit shelf/visibility probes. A successful partition or a completed texture request alone does not pass final quality.

For S02, both canonical profiles failed the triangle cap. Welding exposed 1,121 non-manifold edge fans. Splitting those fans without moving the surface let a separate local reduction reach 14,531 triangles, but a combined atlas bake produced mottled glazing and shelves. Restoring alpha alone did not fix that appearance. Reducing and baking parts independently removed cross-part texture contamination, but the thin reduced inserts still developed large fins. These numerically valid candidates were rejected.

The final correction preserves the independently baked low body exactly (11,541 triangles, UVs, PBR and corner normals). It fits new glazing to the high source's front profile and reconstructs two clean trays at the high source's measured shelf planes and footprints. Rays through missing lower-pane geometry can hit rear fragments: reject those crossings before imposing a monotonic curve. Near the horizontal roof, a large change in depth over a small height change is valid; a uniform slope guard incorrectly rejected that region. Retain before/after views for both kinds of fitting mistakes.

Enable both the selected high source and low target for rendering before a part-only bake. Hiding unrelated parts is useful, but leaving the target hidden causes Blender's bake operation to fail. The failed r7 candidate is retained; r8 corrected this explicit visibility state.

## Why This Matters

Removing glass and uniform shelves from the neural texture input reduced S02's first measured texture steps from roughly 6 minutes to 52 seconds while retaining those parts for assembly. The body-only request completed in 24 minutes 13 seconds; the cancelled whole-mesh request had required 53 minutes 15 seconds for only its first 8 sampling steps. Keeping the original failed attempt and immutable receipt prevents an operational recovery from silently creating an unlimited generation budget.

## When to Apply

- Display cases, cabinets and refrigerators with real interior geometry and transparent panes.
- Multiple nearly parallel pane surfaces whose alpha accumulates.
- Heavy texture requests with phase-dependent sampling cost.
- A dedicated local backend can be stopped only after exact ownership and queue checks.

## Examples

- Complete recovered S02: 293,872 triangles.
- `reviews/S02-parts-fallback-r2`: body 201,640, glass 45,362, shelves 46,870; every original face occurs once.
- `reviews/S02-glass-prototype-r4/visibility-validation.json`: 9 front rays pass through 4 clear surfaces; 6 vertical probes find both opaque shelves.
- `manual/S02-r1/texture1/cancellation-receipt.json`: first request's confirmed cancellation. Its manual ledger remains at consumed attempt 1 before attempt 2 is appended.
- `manual/S02-r3/source`: body-only input and preserved glass/shelf GLBs. `tools/assemble-reststop-display.py` requires a reviewed successful texture history, the exact source hash and source path before assembly.
- `manual/S02-r4/source`: complete assembled PBR source, 293,872 triangles. A uniform factor of 1.997013821 aligns the preserved parts with the normalized textured body. The maximum body-surface residual over 1,001 samples is below 4.24e-8. Fresh GLB/BLEND geometry checks, 9 front-transmission rays and 6 two-shelf probes passed, followed by five-view review. This is a high-detail source, not the final game mesh.
- `manual/S02-r11`: selected 11,971-triangle asset with three retained materials. Fresh GLB and FBX each pass 9 front rays at transmission 0.87 and 6 vertical probes showing both opaque trays. GLB five-view and fresh FBX opposite-view renders pass. `repair.json` records the fitted curve, tray planes/footprints and unchanged body; the README identifies local insert reconstruction and remaining weathered trim.
- `python -X utf8 tools/test-reststop-texture-retry.py`: 6 tests pass, including missing/edited proof, foreign requests, unconfirmed cancellation, still-queued work and submitted-state replay.

## Refrigerator follow-up

S08's standard and cached inner-preserving outputs both lacked the five shelf plates. The recovery exposed inner-wall relief but no spanning shelves. Cutaways and interior horizontal-area measurements prevented treating that relief as a recovered interior.

`prepare-reststop-fridge.py` measures the front panel's planar depth runs, opens only that rectangle, and preserves the exterior/handle/vents. Separate inserts provide a clear door, liner, five shelves with 40 actual holes each, and an emissive strip. Prototype probes verified 18 clear front rays, five solid shelf planes and 200 open holes before texturing. These are explicitly authored local inserts, not claimed as raw TRELLIS output.

The body-only texture used the unchanged generation settings and completed in 172.04 seconds. Manual GPU work can explicitly hold a `texture_source` gate with `--held-stage texture_source`; the default remains `final_compare`. The helper verifies that exact unanswered gate and an empty queue before submission, records the held stage/result path, and the main agent releases it only after the manual request completes. S09's already-saved PBR source was held during S08's request; no cached source was assumed recoverable afterward.

A strict assembly face-count assertion then caught 235,243 source triangles versus 235,240 textured triangles. `verify-reststop-texture-geometry.py` compared every source vertex and the triangle multisets after uniform normalization. All 123,245 vertices matched within 2.159e-8; only three numerically collapsed faces were absent, with total normalized area 1.077e-21 and no extra triangles. The proof includes both file hashes. Assembly allows this specific audited case, not an arbitrary face-count tolerance. Fresh Blender imports subsequently exposed two additional zero-area faces; cleanup removed them with zero visible-area loss and zero retained-normal error.

The first `--fixed-inserts S08` pass reserves 2,664 triangles for unchanged inserts and allocates the remainder of the saved 15,000-triangle total to the body. It welds/splits problematic body edge fans, reduces and bakes only that body, then recombines the inserts. This produced 12,108 body triangles plus 2,664 insert triangles: **14,772 total**, with four materials. The selected `manual/S08-r5/source/low1` passes fresh FBX/GLB checks, all 18 front rays, five shelf checks and 200 holes in each format, five GLB views and a fresh FBX render. The original shape, one manual texture and one bounded Blender pass remain counted.

The intermediate validator now writes failed object names and both format reports before raising. A failed check must leave useful evidence; a long truncated assertion alone is insufficient. The selected asset retains its local-insert disclosure and minor inferred rear/top differences in its README.

## Related

- [Recover cached geometry before postprocessing loss](recover-cached-trellis-shape-before-postprocessing-loss-2026-09-24.md)
- [Preserve opacity through mesh reduction](preserve-opacity-through-trellis-mesh-reduction-2026-09-24.md)
- [Recheck completed history before replaying a lost prompt](../integration-issues/recheck-comfy-history-before-replaying-lost-prompts-2026-09-24.md)
