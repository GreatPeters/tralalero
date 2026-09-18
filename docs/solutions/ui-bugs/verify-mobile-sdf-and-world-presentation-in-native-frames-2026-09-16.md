---
title: Verify mobile SDF outlines and world presentation in native frames
date: 2026-09-16
last_updated: 2026-09-19
category: ui-bugs
module: Harbor UI and opening presentation
problem_type: ui_bug
component: development_workflow
severity: medium
symptoms:
  - "Bonus labels had outline width and color but no visible outline."
  - "A validated workshop mesh was clipped or hidden behind the signal in the portrait camera."
  - "Initial enemies waved at rest and a merchant apron stretched when greeting."
  - "Early video navigation remained prepared but stopped at frame -1."
root_cause: missing_validation
resolution_type: code_fix
tags: [unity, tmp, sdf, shader-variant, camera, rigging, video, native-validation]
---

# Verify mobile SDF outlines and world presentation in native frames

## Problem

Asset inspection and saved-scene checks passed while several visible states remained wrong. The UI refinement required both checking the actual shader and exercising animation, camera and video transitions in Play Mode.

## What did not work

- `_OutlineWidth` alone does not enable an outline in `TMP_SDF-Mobile.shader`; the fragment shader gates it behind `OUTLINE_ON`.
- Increasing font weight amplified the irregular original glyph silhouette. Reusing an existing, more regular licensed Gmarket Sans Bold font and rebaking SDF addressed the shape itself.
- Model bounds inside a viewport do not guarantee the whole model or NPC is visible. Signal posts still obscured the merchant. Runtime moves of statically batched scenery also gave misleading deck positions. Save the candidate through Unity and inspect a fresh Play Mode instance.
- Idle clip names were misleading: old-man/net-man overrides mapped idle to their action clips. Check actual override clip identities, then authored calm poses and staggered phases.
- A broad pelvis weight region accidentally included the glove's inner edge. At frame 61, adjacent vertices at normalized X -0.279 and -0.292 had hips versus forearm weights, stretching a 3.7 cm edge to 1.545 m. Narrowing the region to |X| < .25 removed the strip. Retain seated and raised-arm renders; testing one pose is insufficient.
- A fixed .6-second video wait failed during early prepare/seek. Calling Play before seeking and waiting for a rendered target frame exercised the real navigation state.

## Solution

1. Copy atlas/gradient properties from the actual font material, remove synthetic face dilation, enable `OUTLINE_ON` for light/world labels and use restrained underlay. Assert the shader keyword. Compare Unity texture identity with `==`, not managed-wrapper reference equality after imports.
2. Keep world text units separate from Canvas units. Enemy health values use their existing world scale, reduced by 20%; do not apply a 30-point Canvas minimum to world-space bonus labels.
3. Frame the workshop and signal together from the saved start camera; place the signal farther down the path than the merchant so its post cannot cover him. Validate natural traversal on the new pier and first corner. Floor checks need the existing small-offset tolerance around plank bevels.
4. Preserve corner-body and trigger clearances when moving the first enemy. Change its workbook activation lead together with its scene position; test actual player collider crossings and the second enemy remaining in Waiting.
5. Drive collectible effects from successful `BonusWallChoicePair` claims. Removing the unselected altar must not spawn a second pickup burst. Install shared prefab additions before authored scene overrides to avoid duplicate components.
6. Video Previous decrements one scene boundary. On prepare and seek, start the decoder, seek, and keep controls locked until the frame is ready. Test Previous disabled at zero and page 2 -> 1 while actually playing.
7. Audio focus callbacks can arrive during partial channel teardown; null-check each channel independently. A surviving music channel does not imply ambience and every voice still exist.

## Evidence and prevention

See `map-concepts/harbor-opening-refinement-2026-09-16/README.md`, retained failed candidates, `HarborRefinementTests`, and the directed runtime capture scripts. Fresh snapshots preserve current user state; never restore a previous task's wallet snapshot. Long scene authoring must use `run_script --timeout_ms`; a 5-second `eval` timeout can occur after the underlying mutation has already continued to completion, so inspect before retrying.

Save pending scene edits before starting native tests. In the final run, Pipeline's HTTP status remained reachable but main-thread commands timed out because the test runner's modified-scene prompt waited for Save. Native window inspection distinguished this from a compiler or test deadlock; preserving the pending scene allowed all 10 tests to finish.

Overlap with [sliced UI and binding corrections](normalize-sliced-ui-and-rebind-replaced-screen-fields-2026-09-16.md) is moderate: both require native screenshots, but shader variants, animation weights and decoder startup are distinct causes. Related: [activation and scene reload](../integration-issues/validate-connected-ui-through-activation-and-scene-reload-2026-09-15.md), [real support in generated assets](../workflow-issues/verify-character-anatomy-and-real-support-in-generated-assets-2026-09-13.md).

## 2026-09-17 correction: reference fidelity needs its own acceptance

The user rejected the first visual acceptance. Correct bindings, a shared font and an enabled outline keyword did not establish similarity to the chosen concept. The earlier installer reduced name/nav/wallet type to 59/36/42 and reduced visible panel borders with a 2.8 PPU multiplier. Compare normalized visual proportions with the actual supplied reference, not only whether native text exists or is readable.

- Reserve a dedicated display atlas/material for the large prompt. The existing 9-pixel padding and .065 outline served body labels. The companion Gmarket SDF uses 96-point sampling and 24-pixel padding, preserving the same type family. Keep body font settings separate.
- TMP's SDF outline spreads inward as well as outward. Setting a large outline on unchanged face dilation consumed the white interior (retained candidate 1). In the corrected preset, .305 face dilation balances .28 outline; the smaller chapter label uses .07 outline. Validate the rendered interior, atlas texture, padding and shader keyword together.
- Apply the display material after any generic text-material pass, and cover the normal installer path as well as the one-time repair path.
- A combined raster hand/arrow icon cannot provide independently animated parts. Reuse the separate hand and arrows, then verify a complete motion cycle and disable/reopen reset. Sampling two arbitrary sine phases can produce the same X despite valid motion; compare a full-cycle min/max or reset phase first.
- Merchant acceptance must include size relative to the screen and actual post occlusion. The old 66.6 m camera depth made a 2.6 m body tiny. Move the whole supported station, increase model size, and use a scoped copy of the combined pier mesh to open the shop entrance; do not mutate the source FBX or remove walking-floor geometry.

Evidence: `map-concepts/harbor-reference-fidelity-2026-09-17/README.md`, including rejected and accepted native images. This follow-up supersedes the earlier claim that the previous merchant framing and lobby display treatment sufficiently matched the user's reference.

## Follow-up: replace the actual source of the visual mismatch

The user again rejected the comparison, including the story screen. A material fix does not compensate for replacing an integrated curved crest with a square badge, twisted rope artwork with rectangles, or a rounded display face with a wide angular one. The last `ReferenceLobby` pass also did not touch videos; general video refinement still reset caption size to 34.

The subsequent correction uses two text-free production sprites derived from the exact approved references, true alpha in the movie hole, and dynamic TMP/controls over the art. Page tabs use cropped normal/selected sprites from the same sheet so a baked first-tab highlight cannot lie about navigation state. The selected Jua SDF follows a visual comparison of existing KERIS (too irregular) and smoother rounded forms, with license notices retained.

Keep the new authoring pass after generic theme passes and cover every related surface explicitly: lobby, opening story, and chapter transitions. A video frame with fixed top/bottom artwork needs width-scaled fixed control regions, not percentage coordinates that drift on shorter phones. Test layout separation at both 1080x2340 and 1080x1920, then render actual movie frames and all caption pages. Same-width reference/native comparisons are the visual acceptance artifact; functional test counts alone do not demonstrate fidelity.

Evidence and exact imagegen prompts: `map-concepts/harbor-faithful-art-2026-09-17/README.md`.

## Follow-up: sleeve weights, prop grip and settings scope (2026-09-19)

The user supplied new native screenshots showing that settings still retained the small generic typography, the merchant's sleeve stretched during greeting, the old man's shovel crossed his legs, and green magical smoke read as poison.

- The earlier pelvis-mask fix did not cover the sleeve's hard spine mask at normalized `abs(x) < .285`. A 0.11 m edge stretched to 1.113 m in the greeting. Narrowing the torso region and protecting only the front apron reduced this but still left a 0.63 m seam. Diffusing weights over welded position adjacency, retaining anchored body/extremity regions and four normalized influences, removes the sharp mask discontinuity. The greeting raises the upper arm 55 degrees instead of 95. Review both sides and multiple animation phases; a rest-pose import is not motion evidence.
- Settings were omitted from the later Faithful surface pass. Add an explicit final settings pass to both the narrow repair and the normal installer. Use the existing title/material/button system, legible icon/knob sizes, and the real interactive control bindings. Do not treat tiny valid labels as visual acceptance. Check both enabled and disabled toggle label positions.
- Rotate a long prop around its actual hand grip, including its position offset. A rotation at the mesh center alone moves the grip away from the palm. Review idle, walk and attack after applying the same fit to both source prefab and scene overrides.
- Changing a smoke emitter's color does not change its visual meaning. The new approach effect uses gold stars and small glints; pickup uses a short burst above the player so the body does not hide it. The claim guard suppresses repeated bursts and disables nearby glow immediately.
- Current Pipeline `eval_file` expects a statement body; older class-based scripts need adaptation. `Animator.Play` with speed zero followed by `Update(0)` can leave the previous pose in a capture. Evaluate with nonzero speed/delta and use the actual case-sensitive state name (`idle`).
- A review camera must not redefine the saved follow offset with `StableGameplayCamera.Configure` after it is moved for a close-up. Keep close-up rendering separate from the gameplay camera. Native `ScreenCapture.CaptureScreenshot` preserves the actual 1080x2340 backbuffer; default-sized `capture_game_view(source=screen)` can resize it to a landscape image.
- Creating and destroying temporary authoring objects can leave the scene dirty. Reopen the saved scene before native tests; otherwise Unity's save dialog blocks the main thread even though Pipeline's HTTP test-status endpoint still reports running. Resolve the real dialog, then rerun the test on a clean scene.

Evidence: `map-concepts/feedback-2026-09-19/README.md`. This correction supersedes the prior claim that the broad rigid torso mask fully fixed raised-arm deformation. Related but distinct reward lifecycle guidance remains in [Restore authored bonus-choice VFX baselines](restore-authored-bonus-choice-vfx-baselines-on-reenable-2026-08-15.md).

The navigation artwork also needed replacement: duplicate generic icon overlays and extra inset frames survived the earlier repair. The final text-free navigation strip contains the approved icon treatment and accents; split it at actual alpha-column gaps, not equal thirds of the full canvas (outside margins make those cuts cross the frames). Keep button callbacks on the existing objects and render labels as real TMP text.
