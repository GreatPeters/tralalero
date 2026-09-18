# UI text alignment correction

Status: complete. Three scenes saved, 12 native tests and both builds passed, live four-page glyph/layout checks passed at both portrait sizes. All 69 fresh preference records restored exactly (191 coins); original 1080x2340 view restored and SR18 left in clean Edit Mode.

## Measured causes

`probe-edit.txt` records the pre-fix geometry. NextLabel occupied X .09–.82 (center .455), giving a visible left bias. SkipLabel had asymmetric insets. Centered Jua labels used font ascender/descender metrics and their visible glyph centers sat roughly 3.6–5.7 px above their rectangles; numeral 1 also had a 3 px lateral bearing offset. The previous-arrow and label were independently anchored instead of centered as one group.

## Correction

- Centered Jua/Gmarket display labels use TMP `MidlineGeoAligned`, which centers actual mesh geometry on both axes. Deliberately top/left-aligned text remains authored as such.
- Simple story button labels and number tabs get symmetric insets and no wrapping; header counter/skip use matching 62-point type.
- Previous icon/label use a center-anchored HorizontalLayoutGroup plus preferred-size fitting; the button and callback are unchanged. Icon and text share the same center line.
- Story counter formatting uses a single separator space rather than a runtime-only three-space gap.
- The narrow three-scene repair and normal Faithful authoring path call the same alignment pass. Existing artwork/chapter themes and gameplay are preserved.

Fresh baseline: `tmp/backups/ui-text-alignment-2026-09-17`. Native evidence: `tmp/image-previews/ui-text-alignment-2026-09-17`.

## Verification

- `tests-alignment.json`: 7/7 (four real glyph-bound cases plus three saved-scene configurations). `tests-artwork.json`: 5/5 existing artwork/playback/layout regressions.
- `probe-edit-after.txt`: all measured visible labels have zero local glyph-center offset, replacing the prior vertical and bearing offsets. Inactive Previous content is validated in active Play Mode, not inferred from inactive layout sizes.
- Runtime checks cover all four movie scenes: counter/title/caption/Next/tab centers within .35 reference pixels, all captions fit, Previous icon/label share the same visual center line and resolve their preferred-size group, and Previous navigation still works.
- Native captures cover all three chapters at 1080x2340 and the story controls at 1080x1920. Runtime/Editor builds and fresh preference recovery are recorded alongside this file.
- Review collection: `tmp/image-previews/harbor-ui-all-2026-09-15/8-text-alignment-2026-09-17/index.html`.
- Headless compound learning: `docs/solutions/ui-bugs/center-visible-tmp-glyphs-and-compound-button-content-2026-09-17.md`; existing AGENTS discovery guidance required no edit.
