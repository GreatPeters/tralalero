# Shop reference fidelity — completed 2026-09-17

## User request

Match the selected skin and permanent/chapter upgrade references in the actual game, including frames/outlines, typography, proportions and alignment. Keep the actual equipped-model preview and functional shop data; apply in all three chapters.

## Execution

- Captured fresh scenes/code/materials and 75 preference records before testing.
- Diagnosed omission of shops from the last fidelity pass, generic frame multipliers, legacy fonts/layout and runtime full-card tint.
- Added narrow repeatable shop authoring, new reference-guided native sprites and final rounded SDF typography.
- Implemented runtime equipped/selection bands and chapter lock/rank presentation while preserving transactions.
- Improved actual model framing and platform, normalized source alpha bounds for cosmetic icons.
- Iterated against native frames; fixed excessive corner scaling, unsupported glyphs, duplicate decorations, opaque sprite corners and persisted sprite-name lookup.

## Verification and handoff

38 focused native tests passed, 15 runtime transaction/state checks passed, runtime/editor source build and harness validation passed. Saved scene comparison preserves all 36,362 non-Canvas documents. Original preference values and key absence were restored. Final screenshots and generated PNG sources are under `tmp/image-previews/harbor-ui-all-2026-09-15/9-shop-fidelity-2026-09-17/`.

Detailed record and prompts: `map-concepts/shop-fidelity-2026-09-17/README.md`. Reusable lesson: `docs/solutions/ui-bugs/keep-shop-reference-styling-through-runtime-refresh-2026-09-17.md` (ce-compound headless).

The real preview model's silhouette/material detail differs from the reference illustration. No APK build or device acceptance was requested or claimed.
