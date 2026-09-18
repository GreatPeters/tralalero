---
title: Validate generated BGM padding and internal silence before export
date: 2026-09-15
category: workflow-issues
module: Local BGM candidate production
problem_type: workflow_issue
component: development_workflow
severity: medium
applies_when:
  - Preparing locally generated music for loop previews and user selection
tags: [audio, bgm, stable-audio, crossfade, silence, loudness]
---

# Validate generated BGM padding and internal silence before export

## Context

A loop-friendly prompt and a successful 64-second WAV export did not guarantee continuous music. A fixed two-second end/start crossfade retained long tail silence in one candidate and an internal gap in another. The first export pass rejected candidate8 after measuring a3.8-second near-silent interval in its processed version.

## Guidance

- Locate quiet intervals in the raw audio before deciding how to repair them. Trim only outer padding; do not automatically delete interior rests or stretch a neighboring phrase into the gap.
- Keep100ms around the first/last content above a -60dB RMS threshold, then apply the loop crossfade. Preserve raw recordings and record trim durations.
- Regenerate a candidate with an excessive internal gap using a separate seed and an explicit continuous accompaniment prompt. Retain rejected files and manifests.
- Apply constant loudness gain with a true-peak ceiling, and report actual loudness. Sparse music can hit the peak ceiling before the target integrated loudness; do not claim exact matching if that happened.
- Decode the exported MP3 and verify its levels, file presence and duration as well as the WAV. Distinguish technical checks from listening approval and loop-sample continuity from musical phrase alignment.

## Why This Matters

Padding can make a usable BGM feel broken on repeat. Internal silence and outer padding need different remedies. Also, lowering an already clipped source does not undo distortion; retain saturation counts instead of making an absolute no-distortion claim based only on the master peak.

## When to Apply

- Generative BGM batches, especially sparse horror, ambient and lullaby arrangements.
- Delivering comparable previews without changing the currently installed game soundtrack.

## Examples

The rejected candidate8 had a raw near-silent interval around23.5–28.0seconds. A fresh seed26092508 with continuous strings/harp replaced it; the accepted master had no measured near-silent interval. Candidate6's outer padding was trimmed before crossfading. Final lengths were56.41–62seconds, so the listening page and README were updated from the initially planned fixed62seconds.

## Related

- [Candidate files, generation settings and verification](../../../map-concepts/emotional-horror-bgm-2026-09-15/README.md)
- `tools/generate-emotional-horror-bgm.py`
- `tools/prepare-emotional-horror-bgm.py`
