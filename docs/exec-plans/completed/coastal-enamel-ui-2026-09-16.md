---
title: Apply the selected Coastal Enamel UI throughout the game
date: 2026-09-16
status: completed
---

## Authorization and design contract

The user selected `exec-1d132918-ead4-46b7-89e0-e2c910a1dde2.png` (style 1 lobby) and explicitly requested sliced production assets and installation on all screens. This supersedes the earlier concept-only boundary for this task.

- Selected reference set: `tmp/image-previews/harbor-ui-all-2026-09-15/1/`.
- Navy enamel headers, ivory panels, silver trim, yellow primary actions; real gameplay background and character preview.
- Live text, prices, purchases, settings, movie playback, chapter markers and navigation remain independent UI elements.
- Cosmetic catalog uses 2D skin icons; large preview uses actual equipped/selected model.
- Chapter upgrades: +5% attack and +5% health per rank, additive, five ranks per unlocked chapter. Existing wallet/ownership and upgrade levels retained.
- Covers lobby, combat HUD/player health, regular/chapter upgrades, cosmetics, settings/pause, defeat/victory, story/chapter video, four tutorial prompts and insufficient-currency feedback.
- Preserve real levels, models, movies, gameplay/placement, current user state and unrelated edits. No APK deployment or store publication requested.

## Implementation units

1. Snapshot original scenes/shared assets/sources/preferences, inventory all screen roots and callbacks.
2. Extract/rebuild reusable transparent surfaces, icons, backgrounds and 2D skin art; retain provenance, manifest and review sheets.
3. Extend the existing Harbor installer with the selected visual system and screen-specific layout; connect live runtime state, five-rank labels and cosmetic feedback.
4. Install across three gameplay scenes through official Unity Pipeline; verify saved/reloaded asset references.
5. Exercise actual entry/back/close/purchase/tab/scroll/settings/video/tutorial paths; inspect portrait screenshots, two aspect ratios and relevant native tests; restore test state.
6. Record evidence, remaining limitations and reusable corrections.

## Verification

`dotnet build Assembly-CSharp.csproj -nologo`, `dotnet build Assembly-CSharp-Editor.csproj -nologo`, targeted Unity Edit Mode tests and live end-to-end screen checks. Validate alpha and nine-slice borders, readable text, full list reachability and stable video controls. Compare a fresh preference snapshot after testing.

Progress/evidence: `map-concepts/coastal-enamel-ui-2026-09-16/README.md`.
