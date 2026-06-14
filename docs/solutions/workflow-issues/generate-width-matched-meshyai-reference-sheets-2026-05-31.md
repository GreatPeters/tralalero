---
title: Generate width-matched MeshyAI reference sheets
date: 2026-05-31
category: docs/solutions/workflow-issues
module: MeshyAI reference image workflow
problem_type: workflow_issue
component: tooling
severity: low
applies_when:
  - "Creating all-in-one MeshyAI reference sheets for modular path tiles"
  - "Path pieces must connect after 3D generation"
  - "Transparent-background cleanup and file size both matter"
tags: [meshyai, image-generation, transparent-png, modular-assets, width-matching]
---

# Generate width-matched MeshyAI reference sheets

## Context
MeshyAI path references need to preserve two constraints at once: they must look like 3D game props, and each open connector must read as the same path width. A purely procedural width-locked drawing fixed the connector geometry but looked too flat compared with the wet dock render style.

## Guidance
For MeshyAI reference sheets, generate the 3D render with the width constraint in the prompt first, then remove a flat chroma-key background locally. Avoid making the final reference by drawing simple procedural shapes unless the user explicitly prioritizes exact silhouettes over render quality.

Use prompt language that says every connector end has the same road/path width, open ends are flush and unobstructed, and the camera is a consistent orthographic 3/4 top-down view. Generate on a flat `#00ff00` background, remove the key with `remove_chroma_key.py`, then place the alpha result on the final 8192 square canvas.

## Why This Matters
Post-scaling individual bitmap assets can make their outer sizes match while their internal path widths remain inconsistent. MeshyAI can then infer separate physical widths for straight, corner, curve, and U-turn pieces. A render-native width constraint gives MeshyAI a stronger visual cue while keeping the 3D material response that procedural redraws lose.

## When to Apply
- Building modular roads, docks, paths, fences, or tile kits from a single MeshyAI image reference.
- The user rejects flat/vector-looking references but still needs connectable geometry.
- Chroma-key cleanup must avoid white or green residue in transparent pixels.

## Examples
Before: redraw every piece as exact procedural masks with wood texture. This verifies width but can look like flat 2D art.

After: generate a 3D sheet with explicit equal-width connector constraints, remove the chroma key, upscale onto an 8192 transparent canvas, and verify alpha edges plus visible green residue counts.

## Related
- [Persist each generated image before starting the next prompt](../workflow-issues/persist-generated-image-before-next-prompt-2026-05-16.md)
