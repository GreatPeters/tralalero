---
title: Normalize sliced UI surfaces and rebind replaced screen fields
date: 2026-09-16
category: ui-bugs
module: Coastal Enamel game UI
problem_type: ui_bug
component: development_workflow
severity: medium
symptoms:
  - "New ivory modal sprites tiled corner screws down the whole panel."
  - "Tiny fasteners stretched into spikes on small buttons and tall frames."
  - "Replacing the victory hierarchy left its external clear-reward text reference stale."
root_cause: missing_validation
resolution_type: code_fix
tags: [unity, ui, slicing, sprites, prefab, external-references, visual-review]
---

# Normalize sliced UI surfaces and rebind replaced screen fields

## Problem

A style replacement that reused existing UI structure passed purchase, navigation and binding checks but retained unsuitable image modes and borders. The first real frames revealed problems absent from the component atlas.

## Correction

- Explicitly set every imported scalable surface to `Image.Type.Sliced`. Existing Harbor modals used `Tiled`; replacing the sprite alone repeated the center grain and fasteners.
- Put each entire fastener and its shadow inside the fixed sprite border. A border cutting through the last few fastener pixels stretched those pixels across the center. Increase the border beyond the decoration's bounds, then adjust `pixelsPerUnitMultiplier` to keep the rendered frame restrained.
- Use thin native row/selection shapes for dense lists, keeping substantial frames on major panels. Keep text clear of the fixed bottom border and navigation divider.
- When replacing a sprite with a round built-in handle, reset `Image.type` to `Simple`; `preserveAspect` does not rescue a handle that remains sliced.
- Search for external references to replaced children. `ChapterProgression.clearRewardText` needed a new live label after rebuilding the victory hierarchy. Add an integration assertion for that binding.

## Evidence

The rejected `first-pass/`, improved `refined/`, and final captures are retained in `tmp/image-previews/coastal-enamel-ui-2026-09-16/`. The atlas is not a substitute for live screenshot review. Native tests cover all three saved scenes, real preview/video references, five-rank controls, two-dimensional skin icons and tutorial proximity; directed runtime checks cover actual button debit/equip, reopening, video seeks and resource release.

Same-session preferences were captured before directed purchase tests and restored by `tools/restore-coastal-test-state.cs`; settings values were captured before interactive toggles. Keep capture timing explicit rather than claiming a broader untouched-state snapshot.

Related: [UI activation and reload](../integration-issues/validate-connected-ui-through-activation-and-scene-reload-2026-09-15.md), [actual alpha and reusable sprites](../workflow-issues/validate-real-alpha-and-rebuild-ui-as-separate-assets-2026-09-15.md).
