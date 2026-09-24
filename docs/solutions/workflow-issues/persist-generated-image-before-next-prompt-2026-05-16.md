---
title: Persist each generated image before starting the next prompt
date: 2026-05-16
last_updated: 2026-09-24
category: docs/solutions/workflow-issues
module: Stage reference image generation
problem_type: workflow_issue
component: development_workflow
severity: medium
applies_when:
  - Running multi-image generation batches where each output has a target filename
  - Copying generated images from a shared generated_images folder into the project
  - Maintaining a checklist that maps prompts to project asset paths
  - Desktop generated-image thumbnails do not open the focused image viewer
  - Canvas view renders blank or otherwise prevents full-resolution inspection
root_cause: missing_workflow_step
resolution_type: workflow_improvement
tags: [image-generation, batch-assets, workflow, verification, checklist, image-preview, canvas-viewer]
---

# Persist each generated image before starting the next prompt

## Context

The stage reference regeneration pass used the built-in image generation tool, then copied the newest PNG from `C:\Users\ljh\.codex\generated_images\...` into `output/meshy_images` under a stage-specific filename.

During the Stage 05 pass, there was a risk of assigning an image to the wrong target because the next prompt can create a newer PNG before the previous result has been copied and checked. In a batch flow, relying only on "latest generated file" is fragile unless each result is persisted immediately.

The same persistence rule also protects review workflows when the desktop app's image viewer is unavailable. A Windows desktop session displayed valid generated PNG thumbnails, but clicking them did not open Focused view and Canvas rendered an empty page. A full process restart and fresh Chromium caches did not change the behavior, so the stable workspace copy became the reliable inspection and delivery path.

## Guidance

For batch image generation, treat each prompt as a small transaction:

1. Generate exactly one image.
2. Immediately copy that call's exact returned PNG path to the intended workspace filename. Use newest-file selection only when no other generation can finish in that directory.
3. Mark the matching checklist item generated, with visual review pending.
4. Open or inspect that project copy; mark it complete only after the object and isolation constraints pass.
5. Only then move to the next prompt.

Use a deterministic checklist in `docs/design/` for the target filenames, and save generated assets as non-destructive siblings such as `_concept_batch_v1.png` instead of overwriting originals.

### Exact output paths and alpha-aware visual checks

The September 23 rest-stop concept batch returned an `output_hint` containing both a destination directory and a complete PNG path: `saved to <directory> as <file.png> by default`. A regex starting at the first `C:\\` accidentally captured both paths as one invalid filename. When this exact hint format is returned, anchor on ` as ` and ` by default`, validate the extracted file exists, and retain the prompt-to-output mapping. Do not dump the full result object into tool text: `image_url` can contain megabytes of base64. Forward it with `generatedImage(result)` and inspect only small metadata fields.

If independent image calls run concurrently, each completion must persist its own returned path. Use settled results so one failed copy does not discard another in-flight operation. Never repair such a batch by sorting the shared folder by modification time.

The module-board PNG also exposed an alpha-display issue: one viewer showed a black background with bright colored edge pixels, while actual Chrome compositing displayed the objects cleanly. Inspection found the suspect red pixels were mostly alpha 1/255. Before rejecting or regenerating an RGBA artifact, verify it against the actual gallery background in a browser that composites alpha. Preserve the source alpha and avoid flattening or repainting based on an unsuitable preview alone.

Evidence: `map-concepts/reststop-ppt-concepts-2026-09-23/README.md`; six exact-path preview copies decoded, and browser expansion was visually inspected. Captured with `ce-compound mode:headless`; the existing persistence article was updated because the problem, recovery, files and prevention guidance overlap. No additional session history scan or GitHub issue search was used in this documentation pass; GitHub tooling was unavailable. Existing AGENTS.md already points to this knowledge store.

For preview-only work, use the same transaction with a temporary project path:

1. Copy each inspectable PNG to `tmp/image-previews/<topic>/` with a descriptive, non-overwriting filename.
2. Verify the copied file exists and can be opened outside the inline viewer.
3. Include a clickable absolute PNG link in the final response.
4. Treat the inline thumbnail and Canvas view as convenience UI, not the only way to reach the result.

Do not claim the desktop viewer itself is repaired unless Focused view and Canvas have been independently verified. A cache reset can be a diagnostic step, but repeating cache deletion after a fresh restart reproduces the same empty-route warning does not address the underlying app integration problem.

## Why This Matters

**Coverage audit after the 69-image delivery:** completion of a planned PNG list is not proof of scene coverage. Opening the actual rest-stop v4 `.blend` exposed missing cleaning robot, leak pipe, vending machine, parking lamp, circular tree bed and shop-sign substrate, plus a shape mismatch between generic shelves and the animated compact tipping display. Audit the newest authored scene and its animated assemblies, not only the image gallery. Static material batching had removed logical vending/lamp names, so object-name searches alone were insufficient; native hierarchy, source construction code and six targeted scene renders established the findings together. Separate absent images, reusable existing assets, effects/text, and new decorative suggestions. Do not restore user-deleted carts, count repeated objects as new asset types, or claim every proposed missing image requires a new generation when existing project props may be reused. The read-only audit preserved the `.blend` hash and all 69 PNG hashes. Evidence: `map-concepts/reststop-prop-audit-2026-09-24/README.md`, `blender-inventory.json`, `findings.json`, `verification.json`, and `tools/audit-reststop-prop-coverage.py`.

**September 24 completion of the 69-object rest-stop split:** a scene reference can leak into an otherwise explicit single-object prompt. F04 requested one tree but initially included the snack building as well. File decoding, alpha and unclipped bounds all fail to detect this semantic mistake. Visual review rejected the composite; an explicit tree-only isolation edit produced `f04-v2.png`. Preserve the rejected source and receipt (`needs_revision`) for provenance, select the revision in a manifest, and exclude the rejected image from the final archive. Put the specific subject and removals before shared style instructions; distinguish "no background floor" from a legitimate floor-tile asset. A useful isolated-object unit is one reusable object or a connected structural assembly, not a room given a module label.

The accepted delivery contains 63 newly generated objects plus six unchanged single-vehicle images, each as an independent RGBA PNG. Repeated scene instances share one object source; independently placeable items such as chairs/tables and counters/display cases remain separate. The gallery builder resolves revision receipts, records the original scene reference separately from an edit target, and checks the final ZIP against the selected manifest. In this run, all 69 ZIP entries matched their selected PNG hashes; Chrome showed 69 assets, working zone/category filters and an expanded chair against white. A black inline preview is not sufficient grounds to repaint alpha fringes. Neither these PNG checks nor browser verification establish TRELLIS or game-mesh quality. Evidence: `map-concepts/reststop-atomic-assets-2026-09-23/README.md`, `final-manifest.json`, `final-prompts-and-sources.json`, `verification.json`, and `tools/build-reststop-atomic-gallery.py`. Captured with `ce-compound mode:headless`; high overlap with this existing article justified updating it. Session-history and GitHub issue searches were not used; this was a local artifact workflow correction. No instruction-file change was needed because AGENTS.md already directs agents to this store.

**Correction after user review of the September 23 TRELLIS image set:** calling an entire furnished room a single “module” did not make it a suitable one-shot production asset input. Alpha, uncropped bounds, a single viewpoint and a cohesive style are image-quality checks, not evidence that an image-to-3D model will recover occluded furniture, thin legs, interior layout or independent semantic parts. The room composites are now classified as layout references. Author input images at the reusable-object or simple-structural-module level, such as one chair, one table, one counter, one display case, one planter or one wall bay. Repeated furniture should be instanced after conversion. State mesh-generation uncertainty separately from image-generation completion; never imply a scene was conversion-tested when only PNG properties were checked. Existing vehicle cutouts are single-asset candidates, also untested until actual conversion is authorized.

For the September 23 TRELLIS-input image pass, preserve only the selected individual assets in the input ZIP. A review gallery is useful for the user, but a multi-asset contact sheet is not the intended single-asset input. The first parking PNG touched the canvas boundary and was repaired with an ImageGen framing edit. Three vehicle PNGs included exterior contact-shadow patches; separate ImageGen edits removed those patches while preserving the vehicles and alpha. Keep these originals for provenance but exclude them from the final 16-image ZIP. Inspect the visible asset and its alpha on a white browser background; an opaque-foreground bounding box touching the canvas can flag real clipping, while near-transparent colored fringe pixels are a different issue. This workflow used an alpha threshold of 204 only for inspection, matching the cited TRELLIS preprocessing foreground threshold, and did not alter PNG pixels with a script. Evidence: `map-concepts/reststop-trellis-images-2026-09-23/README.md`, `prompts.json`, and `tools/build-reststop-trellis-image-gallery.py`.

## When local PNG links also open a blank viewer

On 2026-09-19 the user reported the same blank image surface after clicking absolute PNG links. The files themselves decoded normally. Repeating the same links was insufficient. A standalone HTML gallery with relative PNG copies, served only from its dedicated folder on 127.0.0.1, displayed correctly in the user's connected Chrome browser. Verify an actual screenshot and a click-to-expand interaction, then preserve the browser tab as a deliverable. Keep the raw PNG links as fallback, but give the browser gallery URL as the primary review surface. Do not claim this repairs the original app or establishes its root cause.

Bind a local server to loopback only, record its actual URL, and keep its helper process hidden. Serve the gallery folder rather than the whole repository. The HTML also works when opened directly in a browser after the server stops. Instructions: [Bonus Wall gallery](../../../map-concepts/bonus-gallery-2026-09-19/README.md).

Use height:auto along with width:100% for responsive img elements carrying width/height attributes; otherwise the fixed height can produce a large empty letterbox. This was corrected and rechecked visually in the gallery.

When multiple image generations share the same default output directory, "copy the newest file" is only correct if no later image has been generated yet. Skipping the immediate persist step can silently swap visual concepts between filenames, which is hard to detect after dozens of similar stage images.

The checklist plus contact sheet makes the batch auditable: file count proves coverage, unchecked count proves no listed target was skipped, and the contact sheet exposes obvious concept mismatches.

Workspace preview copies also decouple the deliverable from desktop UI routing and transient conversation state. The user retains the full-resolution PNG even when the thumbnail is only draggable, the expanded viewer does not open, or Canvas is blank.

## When to Apply

- Generating multiple game stage references, icons, sprites, or concept variants.
- Saving generated assets from Codex's default generated image directory into the repo.
- Any workflow where the filename carries semantic meaning, such as stage number, variant number, or completion state.
- Presenting generated concepts for close inspection in a desktop app where Focused view or Canvas is unreliable.

## Examples

For each generated target, run the copy and checklist update before the next image prompt:

```powershell
$folder = 'C:\Users\ljh\.codex\generated_images\019e2b66-07b3-7fb0-895d-3f89533b484c'
$latest = Get-ChildItem -LiteralPath $folder -File -Filter '*.png' |
  Sort-Object LastWriteTime -Descending |
  Select-Object -First 1

$name = 'stage_05_4_gangnam_concept_batch_v1.png'
Copy-Item -LiteralPath $latest.FullName -Destination (Join-Path 'output\meshy_images' $name) -Force
```

After the batch, verify the result set:

```powershell
(Get-ChildItem -LiteralPath 'output\meshy_images' -File -Filter '*_concept_batch_v1.png').Count
(Select-String -LiteralPath 'docs\design\stage_reference_regeneration_todo_20260516.md' -Pattern '^- \[ \]' | Measure-Object).Count
```

Then build a contact sheet from the project copies and inspect it before reporting completion.

For temporary review variants, preserve and link the files directly:

```powershell
$previewDir = 'tmp\image-previews\shop-hud'
New-Item -ItemType Directory -Path $previewDir -Force | Out-Null
Copy-Item -LiteralPath $generatedPng -Destination (Join-Path $previewDir '01-wood-parchment.png')
```

```markdown
[Wood and parchment HUD preview](C:/absolute/project/path/tmp/image-previews/shop-hud/01-wood-parchment.png)
```

## Related

### 2026-09-15 in-game UI preview recovery

The user reported another blank image/context view when reopening the Harbor in-game UI proposal. The retained `tmp/image-previews/harbor-ingame-ui-revision-2026-09-15/ingame-ui-revision-v1.png` decoded correctly and was visually inspected at 1536×1024. Its original session summary confirmed that it is a static combat/settings/results proposal, not an installed Unity revision (session history).

Opening that exact PNG through Windows file association returned successfully. This verifies the launch request, not the external viewer's rendered window. Deliver the direct absolute PNG link again. Current narrow log searches did not establish the cause of the context-view failure; keep app repair explicitly unresolved and distinguish the reported surface from Unity gameplay. Existing cache-repair records are historical evidence, not grounds to repeat resets or claim a current fix.

The user subsequently clarified that the failing surface is **inside VS Code**. The active Code window uses the long-running `20260912T180244` log directory; newer timestamped directories were empty and did not represent that window. `code --reuse-window <absolute PNG path>` returned successfully, with visual confirmation pending. The installed Codex 26.908.40401 extension has an open-target path that calls `openTextDocument`, but no matching path-open failure was found in the inspected current-day logs. This is a candidate code path, not a confirmed cause; do not patch the installed extension or reset desktop caches on that evidence alone.

- `docs/design/stage_reference_regeneration_todo_20260516.md`
- `docs/design/stage_reference_regeneration_prompts_20260515.md`
- `output/meshy_images/_analysis/stage_reference_concept_batch_v1_contact_sheet.png`
- `AGENTS.md` (`Generated image previews`)
