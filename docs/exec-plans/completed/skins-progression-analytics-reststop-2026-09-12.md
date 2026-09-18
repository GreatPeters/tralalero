---
status: complete
date: 2026-09-12
---

# Skins, chapter progression, player logs and Highway rest stop

Implement the user's ten requests in the existing map2 working tree. Preserve prior changes, owned cosmetics, wallet and upgrades. No commit/push or game-video replacement is requested.

## Local acceptance — complete

All units are complete. The user's existing Tra sheet is now connected to the verified Firebase BigQuery dataset, imports one real exported round, and has a daily10:00–11:00 KST refresh trigger. Native export/readback verifies the data, formulas and configuration. A new Android build/device-performance run was not part of this connection step. [Live connection](../../../map-concepts/player-logs-live-2026-09-12/README.md) · [game acceptance](../../../map-concepts/skins-reststop-2026-09-12/README.md).

Final validation:50 related Unity tests pass, C# builds have0 errors, the agent harness validation passes, Noryangjin first clears on attempt18/300.03s, and the rest-stop Highway run clears at300.14s.97 ground probes pass; routes/encounter transforms remain intact. Original57-key preferences and SR18 Edit Mode are restored; the task-owned TRELLIS backend is stopped.

## Direction and acceptance

- Cosmetic models: use the installed TRELLIS2 automation for distinct replacement footwear and temporary accessory replacements. Use the existing Army/Wood/Fire/Cyber/Ice/Zombie/Christmas/Pink shark assets as visual references. Change readable silhouettes and construction, not only color. Preserve the audited four-foot gameplay rig and independent skin/shoe/hat slots.
- Body surfaces: inspect and organize UVs in Blender; reuse/bake the existing skin motifs into actual mapped albedo/normal/material treatments, preserving face/belly/fin regions. Record a neutral/model view and the actual equipped result.
- Shop: large readable preview plus a two-column GridLayout catalog, consistent with the upgrade shop. Clear ownership, selection, cost and equip feedback. Refresh icons from the final models. Verify actual buy/equip/reload/scroll/start gating in both scenes.
- Composition: interpret one BonusWall unit as one left/right choice pair, one enemy as one individual actor, and one gimmick as one encounter station. The authored target is25 bonus units:50 enemies:25 gimmick stations per chapter, preserving Noryangjin's25 existing encounters, with all8 Noryangjin palette patterns represented and usable. Avoid overlapping interactions and preserve reaction space after turns. Three toll lanes form one station; the dolphin is an animated waterside feature with no contact damage.
- Progression: tune actual earned upgrades and encounter stats toward a fresh first run around30seconds, approximately15–20seconds of expected progress per upgrade/run, and a first300-second completion in roughly15–20 runs. This is a measured cohort target, not a forced-death timer or a guarantee for every player/bonus outcome. Keep the full curve and controls in canonical Data.xlsx.
- Highway rest stop: integrate a coherent rest-stop area into HighWay and expose its generated props in the map palette. Include a service building, food/retail frontage, restrooms, parking/charging and sheltered outdoor furniture. Keep the driving lanes readable.
- Player logs: retain Firebase Analytics as the source, prepare a readable Google Sheets workbook and a BigQuery connection/query path for one row per run, day/chapter summaries, survival distribution and upgrade progression. Never fabricate live rows or place administrator credentials in the game. Verify actual cloud connection when account tools are available; isolate any missing-account step from completed local work.

## Implementation units

| Unit | Requests | Work | Verification |
|---|---|---|---|
|U1 Audit and recovery|all|Inventory skins, runtime bindings,8 palette types, both routes and analytics destination; snapshot user state/source files.|Durable source manifests and recoverable before state.|
|U2 Skin production|2,3|Distinct TRELLIS shoes/accessories; mapped body motifs; independent mesh/UV/equipment integration.|Every final variant rendered, fresh-import checks, foot/head mounts and rig motion.|
|U3 Shop presentation|4,5|Two-column grid, clearer hierarchy/state/preview and final model icons.|Portrait screenshots; purchase/equip/slot switch/scroll/reload and input blocking.|
|U4 Composition and gimmicks|6,7|All Noryangjin gimmicks and1:2:1 placement across both chapters.|Exact station/actor counts; type coverage; real contacts and full route travel.|
|U5 Progression|8|Earned-currency upgrade curve, encounter growth, controlled bonus strength and ~300s pacing in Data.xlsx.|Fresh ordinary-input cohorts, seed/upgrade/purchase records, first-clear target and no forced survival timer.|
|U6 Rest stop|9|Generate/import TRELLIS props and author a Highway rest-stop segment/palette.|Source/import manifest, ground contact, visual density and gameplay clearance.|
|U7 Player-log sheets|1|Verified local workbook, BigQuery-backed refresh path and native Google Sheets delivery.|Real-data access/headers/queries, empty-data correctness, refresh limits and private destination.|
|U8 Review and closeout|10|Targeted tests/builds, repeated play, visual review, documented lessons and user-state restoration.|Acceptance report for all10 requests with clear cloud/visual/runtime boundaries.|

## Resolved external constraint

The user supplied the existing Tra sheet and granted connection authority. The authenticated Chrome session verified Firebase's dataset `analytics_547820149` in `asia-northeast3`; a bound Apps Script now queries it successfully. File-upload permission was not needed after completing the existing-sheet setup through Apps Script.
