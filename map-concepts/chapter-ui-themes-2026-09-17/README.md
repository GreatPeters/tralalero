# Chapter-specific lobby plaques

Status: complete. Both road-chapter plaques are saved and visually checked. Eight native tests and both builds passed; menu-return checks passed. Noryangjin scene hash is unchanged. All 69 fresh preference records restored exactly (191 coins), with SR18 reopened in Edit Mode.

## User intent and scope

The user circled the anchor on Highway's lobby and said the Highway and RestStop UI should fit their settings. The new variants preserve the common typography/control layout while differentiating the chapter title plaques:

- Noryangjin: existing cobalt harbor plaque, anchor, rope and wave.
- Highway: expressway-green plaque, white motorway pictogram, dashed lane markings and a yellow road divider.
- RestStop: teal service-sign plaque, coffee-cup crest, wayfinding chevrons and a small gold meal divider.

The common buttons and global opening-story presentation remain shared; that movie still tells the harbor origin story. This pass specifically corrects the chapter lobby identity highlighted by the user.

## Implementation

`HarborGameUIInstaller.ChapterThemes` maps chapter IDs explicitly to their plaque assets. The normal Faithful installer uses this selection, so rebuilding the UI retains each chapter's motif. Missing or unsupported themes fail explicitly rather than quietly inserting the harbor symbol. `ApplyRoadChapterPlaques` changes only the Highway/RestStop plaque Image and aspect fitter; it does not rebuild the rest of their UI or alter gameplay.

Built-in imagegen edited the existing integrated plaque into two chapter variants. The first RestStop candidate's cutlery was reduced to keep it below the live text area. Exact prompts and source PNGs are in [sources/](sources/), including the retained first candidate. Installed PNGs live under `Assets/ShooterSurvival/UI/ChapterThemes/` and retain real alpha.

Fresh baseline: `tmp/backups/chapter-ui-themes-2026-09-17` (current scenes, source/material files and typed preferences). Native images: `tmp/image-previews/chapter-ui-themes-2026-09-17`.

## Verification

- 8 native tests pass: ChapterUIThemeTests 3/3 and HarborFaithfulArtworkTests 5/5. Explicit expected symbols cover all three chapters and the rebuild mapping, along with retained navigation/video bindings.
- Runtime and Editor builds pass (`build-runtime.txt`, `build-editor.txt`).
- Native `checks.txt` in each road chapter confirms the correct plaque, settings/upgrade close-and-return behavior, and retained independent hand hint.
- `preservation.txt` verifies that the Noryangjin scene SHA256 is unchanged. No workbook/gameplay changes were made.
- Fresh preference restoration is recorded in `restoration.txt`; original SR18 is reopened in Edit Mode. Preview collection: `tmp/image-previews/harbor-ui-all-2026-09-15/7-chapter-themes-2026-09-17/index.html`.
- The headless compound record is `docs/solutions/design-patterns/separate-chapter-identity-from-shared-ui-2026-09-17.md`. Existing AGENTS discovery guidance needed no edit.

## Sources

- [Built-in imagegen prompts](sources/prompts.json)
- [Highway plaque](sources/HighwayPlaque.png)
- [RestStop plaque](sources/RestStopPlaque.png)
