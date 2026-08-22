---
title: Persist each generated image before starting the next prompt
date: 2026-05-16
last_updated: 2026-08-17
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
2. Immediately copy the newest generated PNG to the intended workspace filename.
3. Mark the matching checklist item complete.
4. Open or inspect that project copy.
5. Only then move to the next prompt.

Use a deterministic checklist in `docs/design/` for the target filenames, and save generated assets as non-destructive siblings such as `_concept_batch_v1.png` instead of overwriting originals.

For preview-only work, use the same transaction with a temporary project path:

1. Copy each inspectable PNG to `tmp/image-previews/<topic>/` with a descriptive, non-overwriting filename.
2. Verify the copied file exists and can be opened outside the inline viewer.
3. Include a clickable absolute PNG link in the final response.
4. Treat the inline thumbnail and Canvas view as convenience UI, not the only way to reach the result.

Do not claim the desktop viewer itself is repaired unless Focused view and Canvas have been independently verified. A cache reset can be a diagnostic step, but repeating cache deletion after a fresh restart reproduces the same empty-route warning does not address the underlying app integration problem.

## Why This Matters

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

- `docs/design/stage_reference_regeneration_todo_20260516.md`
- `docs/design/stage_reference_regeneration_prompts_20260515.md`
- `output/meshy_images/_analysis/stage_reference_concept_batch_v1_contact_sheet.png`
- `AGENTS.md` (`Generated image previews`)
