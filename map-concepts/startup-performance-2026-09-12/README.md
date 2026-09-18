# Startup delay reduced by compacting duplicate workbook styles

The user reported slow game startup after the Flow opening installation and requested a fix. The device/slow-stage clarification was not answered during the work, so measurements used the existing SR18 Unity Editor setup. No APK or physical-device timing is claimed.

## Measured result

| Editor run | Entered Play | First usable probe tick | First movie frame |
|---|---:|---:|---:|
| Old workbook, video on, initial clock-only probe |24.711s|24.723s|26.209s|
| Old workbook, video off, CPU profiling |22.957s|22.973s|—|
| Old workbook, video on, CPU profiling |17.998s|18.002s|18.078s|
| Compacted workbook, video on, same CPU profiling |1.948s|1.955s|2.156s|

The directly comparable profiled video-on first-frame result improved from18.078s to2.156s (about88%). These are individual same-Editor observations, not a multi-device benchmark. Domain/scene reload are disabled in the existing project; caches can differ between runs. The initial clock-only attempt mistakenly enabled only `Profiler.enabled`, so it has zero recorded CPU frames and must not be used for CPU comparisons. The corrected probe additionally enables/restores `ProfilerDriver.enabled`.

Independent ExcelDataReader creation measurements isolate the cause: the original took778.406/849.618/760.413ms, versus7.109/7.500/7.297ms for the compacted candidate. Opening the source stream itself took0.26–0.37ms. The bottleneck was style parsing, not reading1MB from disk.

## Cause and fix

`Data.xlsx` had27,274,208 uncompressed bytes in `xl/styles.xml`:41,988 font records represented only14 distinct fonts;63,116 fills represented13;12,288 borders represented2;127,660 cell XFs referenced these duplicated definitions. The sheet-graft scripts appended every imported style on each update, including styles already present, causing repeated growth. `GameDataWorkbookAutoReload` validates and reloads multiple tables on entering Play, and runtime table loaders also construct ExcelDataReader instances, repeatedly parsing the bloated stylesheet.

`tools/compact-workbook-styles.py` now maps duplicate definitions through number formats, fonts, fills, borders, parent XFs and cell XFs, then remaps worksheet cell/row/column references. Final stylesheet10,079bytes; workbook975,856→66,220bytes; cell XFs127,660→57. All127,660 original effective cell styles and named styles were checked for identical meaning. Worksheet bytes apart from style references, and every other ZIP part, are unchanged. Formulas, cached values, sheet order, layout, validation and data are preserved.

Artifact Tool has no public style-table compaction API (bounded help query found no relevant capability), so package-level repair supplies that missing operation; Artifact Tool inspected/rendered before and after. The representative `환경 변수!A1:F14` preview PNGs have identical SHA256 `7ed7948165bb30bec6d994b05f537f86abfa2d7574a021192b8d207c28dceaf0`. A first repair verifier reused mutable ZipInfo entries and failed while rereading the source; copying ZipInfo fixed that verification issue. The source was not modified by the failed trial.

The accepted workbook was installed through official Unity Pipeline after checking the original hash and validating the schema. Its GUID is unchanged. `Data.bytes` was regenerated with existing signing/encryption and verified against the source. Original workbook and protected archive are retained as `Data-before.xlsx`/`Data-before.bytes`. The movie file, resolution, timing and picture quality were not changed for this optimization.

To prevent recurrence, the existing shared graft helper now merges matching style definitions while preserving original indices. All four current append/combat/equipment/release graft scripts use it. Their preservation assertions allow `styles.xml` to remain unchanged when all incoming formats are already present.

## Verification and remaining limit

- Three Python regression tests pass: value/formula/layout preservation plus independent ZIP reread and idempotence;20 repeated grafts without style growth; adding a new format once without changing existing indices.
- All five affected Python modules compile.
- Full `GameDataWorkbookSchema.Validate` passes on the candidate; installed protected archive matches source and validates.
- Actual SR18 Play with the movie enabled produced the2.156s first-frame measurement, then exited automatically. Fresh56-key preferences restored:3007coins/0gems; original autoplay and profiler flags restored.
- The subsequent Unity `GameDataWorkbookTests`15-case run timed out without results. Editor remained responsive at OS level but Pipeline main-thread calls timed out and the scene title had an unsaved marker. A save-dialog wait is suspected, not visually confirmed; the user was asked whether a save prompt is open. Do not claim those15tests passed or forcibly restart the user's Editor. No further Play or cache mutation was performed after that block.

Source/output hash and full package verification: `../../outputs/startup-performance-2026-09-12/Data.json`. Runtime measurement JSONs are alongside this file. The accepted source is `Assets/ShooterSurvival/GameData/Editor/Data.xlsx`, not a second balance database.
