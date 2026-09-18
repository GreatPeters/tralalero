---
title: Keep shop reference styling through runtime refresh
date: 2026-09-17
category: ui-bugs
module: Cosmetic and upgrade shops
problem_type: ui_bug
component: development_workflow
severity: high
symptoms:
  - "Shops kept small old typography and thin cards after lobby and story were corrected."
  - "Runtime selection tint weakened authored blue borders."
  - "Increasing the old border size exaggerated corners and made square buttons collapse."
root_cause: incomplete_setup
resolution_type: code_fix
tags: [unity, ui, reference-fidelity, nine-slice, tmp, runtime-state, shops]
---

# Keep shop reference styling through runtime refresh

## Problem

The user repeatedly compared actual cosmetic and upgrade shops against selected references. Previous final fidelity work covered lobby/story, while shops retained the earlier generic Coastal layout, small Gmarket labels and RowPanel cards. An outline-only change could not fix this scope omission.

## Symptoms

- Cosmetic item icons and labels were undersized, price/equipped bands were missing, the live model appeared small and its platform was almost flat.
- Remaining chapter ranks were tiny solid gray dots and disappeared when locked.
- Generic image multipliers reduced decorative borders. `CosmeticShopUI.Refresh` tinted the whole card using the theme, changing the intended blue/ivory palette.

## What Didn't Work

- Enlarging old nine-slice frames globally made their large protected corners dominate. A 100-unit square button with two roughly 100-unit protected borders collapsed its center. Use Simple for a square icon frame, and inspect effective border width in actual Canvas units.
- Reusing a subrect from story chrome carried an opaque blue rectangle outside the button silhouette. The final buttons use a dedicated transparent sprite.
- The existing `Image`, `Text` and `Rect` authoring helpers always create new objects. Assuming they upsert caused duplicate decorations during the first refinement pass. The shop pass now upserts by name and removes earlier duplicates.
- Jua lacks some symbols such as the multiplication close glyph and middle dot. Unsupported symbols produced visible replacement boxes. Use supported X/slash or native vector shapes; do not assume successful font assignment proves glyph coverage.
- Compiling a newly added test during Play Mode interrupted a capture coroutine. Complete compilation in Edit Mode before starting captures, and require a COMPLETE marker.
- Pipeline's combined pipe-delimited test filter selected zero tests. Run each suite separately and reject zero-total results.
- Uniform icon rectangles do not guarantee uniform visible artwork. The armor PNG included 166px of bottom transparency, so preserve-aspect made its illustration much smaller. Native alpha-bounded sub-sprites fix the framing while retaining the original source and gameplay catalog.
- `AssetDatabase.CreateAsset` gave native sprites their asset filename as the object name. The first alpha-fit test caught a lookup using the earlier in-memory name. Use the persisted `ShopIcon_<visualKey>` name consistently and verify after a scene reload.
- A save-scene modal can leave Pipeline test status at running while HTTP still responds. A main-thread eval timeout plus native-window inspection identified it. Save pending changes, then wait for the original test run; do not start overlapping tests or replace the supported Pipeline bridge.

## Solution

`HarborGameUIInstaller.ShopsFaithful` is the final shop styling pass and also provides a narrow three-scene repair. It authors large Jua SDF type, role-specific outline material, suitable nine-slice assets, the correct two-column cosmetic hierarchy, three-line regular upgrades and visible chapter rank states. Purchase components, prices and serialized text references remain the authoritative live bindings.

Runtime presentation is explicit: preserve white image tint on authored card art, put selection and equipped indicators on separate children, change only the price/state band and inactive action fill. Chapter presentation uses distinct empty/filled pip sprites and a locked overlay/label. The real model preview preserves equipment rendering, orbit and demand-render lifetime while adding framing and a substantial lit plinth. Added materials are disposed with the preview.

## Why This Works

Sprite outlines, SDF glyph outlines, type metrics, layout proportions and runtime tint are separate layers. Correcting one layer does not correct the others. A last authoring pass protects the reference layout from generic earlier passes, and runtime refresh must honor the authored art instead of repainting it.

## Prevention

- Make a screen/state matrix before claiming every screen matches: skin/shoes/hats, focused versus equipped, affordable/insufficient, regular scroll bottom, locked/unlocked/max chapter rows.
- Compare equal-width native screenshots to the reference, with original data. Keep demo reference prices separate from actual game balance.
- Re-run the narrow authoring command; check duplicate child names and persistence after scene reload.
- Save fresh preferences including chapter rank keys before transactional tests. Restore both values and key absence after stopping Play Mode. Never borrow an earlier task's snapshot.
- Require nonzero completed native test totals and full capture completion. Keep screenshot failures as iteration evidence.
- For PowerShell capture waits, coerce an empty `Get-Content -Raw` result to `[string]` before testing its COMPLETE marker, and independently count the expected PNGs. An empty/null result must never authorize stopping Play Mode.

## Related

- [Visible TMP glyph and compound-button centering](center-visible-tmp-glyphs-and-compound-button-content-2026-09-17.md): related typography, distinct runtime/scope root cause.
- [Validate connected UI through activation and scene reload](../integration-issues/validate-connected-ui-through-activation-and-scene-reload-2026-09-15.md).
- [Cosmetic preview and test preference verification](../workflow-issues/verify-unity-cosmetic-previews-and-test-prefs-2026-09-10.md).
- Evidence and scripts: `map-concepts/shop-fidelity-2026-09-17/README.md`.
