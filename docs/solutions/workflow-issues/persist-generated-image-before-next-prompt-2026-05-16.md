---
title: Persist each generated image before starting the next prompt
date: 2026-05-16
category: docs/solutions/workflow-issues
module: Stage reference image generation
problem_type: workflow_issue
component: development_workflow
severity: medium
applies_when:
  - Running multi-image generation batches where each output has a target filename
  - Copying generated images from a shared generated_images folder into the project
  - Maintaining a checklist that maps prompts to project asset paths
root_cause: missing_workflow_step
resolution_type: workflow_improvement
tags: [image-generation, batch-assets, workflow, verification, checklist]
---

# Persist each generated image before starting the next prompt

## Context

The stage reference regeneration pass used the built-in image generation tool, then copied the newest PNG from `C:\Users\ljh\.codex\generated_images\...` into `output/meshy_images` under a stage-specific filename.

During the Stage 05 pass, there was a risk of assigning an image to the wrong target because the next prompt can create a newer PNG before the previous result has been copied and checked. In a batch flow, relying only on "latest generated file" is fragile unless each result is persisted immediately.

## Guidance

For batch image generation, treat each prompt as a small transaction:

1. Generate exactly one image.
2. Immediately copy the newest generated PNG to the intended workspace filename.
3. Mark the matching checklist item complete.
4. Open or inspect that project copy.
5. Only then move to the next prompt.

Use a deterministic checklist in `docs/design/` for the target filenames, and save generated assets as non-destructive siblings such as `_concept_batch_v1.png` instead of overwriting originals.

## Why This Matters

When multiple image generations share the same default output directory, "copy the newest file" is only correct if no later image has been generated yet. Skipping the immediate persist step can silently swap visual concepts between filenames, which is hard to detect after dozens of similar stage images.

The checklist plus contact sheet makes the batch auditable: file count proves coverage, unchecked count proves no listed target was skipped, and the contact sheet exposes obvious concept mismatches.

## When to Apply

- Generating multiple game stage references, icons, sprites, or concept variants.
- Saving generated assets from Codex's default generated image directory into the repo.
- Any workflow where the filename carries semantic meaning, such as stage number, variant number, or completion state.

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

## Related

- `docs/design/stage_reference_regeneration_todo_20260516.md`
- `docs/design/stage_reference_regeneration_prompts_20260515.md`
- `output/meshy_images/_analysis/stage_reference_concept_batch_v1_contact_sheet.png`
