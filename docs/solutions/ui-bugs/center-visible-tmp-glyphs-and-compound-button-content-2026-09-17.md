---
title: Center visible TMP glyphs and compound button content
date: 2026-09-17
category: ui-bugs
module: Story and shared display UI
problem_type: ui_bug
component: development_workflow
severity: medium
symptoms:
  - "Story labels appeared high even with Center alignment."
  - "Next Scene was left of its button center and Previous icon/text were unbalanced."
root_cause: config_error
resolution_type: code_fix
tags: [unity, tmp, typography, alignment, glyph-bounds, layout-group]
---

# Center visible TMP glyphs and compound button content

## Problem

Changing fonts and replacing button artwork preserved old child rectangles and font-metric alignment. The user reported text looking generally misaligned on the story screen.

## Evidence

The pre-fix probe found NextLabel anchors .09–.82 (center .455), SkipLabel's asymmetric insets, Jua glyph centers 3.6–5.7 px above their text rectangles, and numeral 1 about 3 px left of center. A correctly centered text rectangle did not mean its visible glyphs were centered.

## Solution

- For centered display labels, use `TextAlignmentOptions.MidlineGeoAligned` to center actual mesh extents in both axes. TMP's ordinary Center uses vertical ascender/descender metrics. Inspect the local package implementation of `VerticalAlignmentOptions.Geometry` rather than changing font metrics globally.
- Give simple button labels symmetric anchors, zero accidental margins and a consistent header type size.
- Center an icon-plus-label as one preferred-size HorizontalLayoutGroup. Keep the Button/UnityEvent on its original root. Independently centering the label in the remaining percentage region does not center the visible combined content.
- Keep intentionally left/top-aligned copy unchanged. Run this alignment pass after generic typography passes so rebuilds preserve the fix.
- Test visible `characterInfo` bounds for numeral 1, Korean labels, mixed counter text and multi-line subtitles. Confirm actual active layout at runtime: inactive layout groups may not resolve their child sizes until enabled, so a configuration-only test cannot prove final placement.

## Prevention

After a font or artwork replacement, measure glyph bounds relative to the intended text rectangle and the whole compound group relative to its button. Check dynamic page text and both portrait aspect ratios, along with actual button behavior. Do not compensate for a font-wide metric offset by guessing a different per-label Y offset.

Evidence: `map-concepts/ui-text-alignment-2026-09-17/README.md`. Related: [native SDF and artwork validation](verify-mobile-sdf-and-world-presentation-in-native-frames-2026-09-16.md). This is a distinct alignment/configuration cause, not another outline-material correction.
