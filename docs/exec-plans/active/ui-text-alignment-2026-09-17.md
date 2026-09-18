---
title: Correct visible text alignment in story and shared display UI
date: 2026-09-17
status: complete
---

The user showed story scene 3 and reported general text misalignment. Measurements confirmed NextLabel's asymmetric X anchors .09–.82, mixed header font sizes, and visible Jua glyph centers 3.6–5.7 px above their intended rectangles. Number 1 also had a 3 px lateral bearing offset. The Previous icon/label were positioned independently.

Normalize centered display labels to TMP MidlineGeoAligned, retain deliberately left/top text, give simple video button labels symmetric insets, center the Previous icon and label as one layout group, and standardize the counter spacing/header font size. Integrate the pass into normal authoring and apply narrowly to all three scenes. Preserve artwork/chapter themes/gameplay and fresh state; validate actual glyph bounds and live interactions at tall and short portrait sizes.

Completed: all three scenes updated, 7 alignment tests and 5 existing artwork/layout regressions passed; runtime/Editor builds passed. All four story pages passed real glyph-center/caption checks at 1080x2340 and 1080x1920, with live Previous content and callback checks. Captures, fresh 69-record preference restoration and the compound learning are indexed in `map-concepts/ui-text-alignment-2026-09-17/README.md`.
