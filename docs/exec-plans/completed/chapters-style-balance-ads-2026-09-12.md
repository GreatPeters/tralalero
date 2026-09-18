---
status: completed
date: 2026-09-12
---

# Chapter presentation, progression, content and rewarded-video revision

Completed2026-09-13: original three-scene integration, final17/22/22earned-purchase cohorts,83final native tests, functional UI/ads/coin/jewel/travel checks and an ARM64Android test build with0errors. QA Editor stopped; original user state preserved. Production ad identifiers and physical-device certification remain explicitly outside the completed test integration. See the companion log and chapter-polish acceptance record for evidence.

## User request and decision criteria

Implement the14-request revision after considering alternatives. Improve opening/shop presentation and licensed TMP typography; rework Highway/rest-stop characters and bad props, skin/bone/animate all combat characters; create distinct playable chapter2/3 scenes with automatic transition movies; rebalance base stats, all shops and progression toward20attempts per chapter; investigate CPU saturation; integrate video ads; complete three documented playtest/refinement cycles. The five-chapter/100-run progression direction is the target. Only chapters2/3 are newly requested as authored stages; themes/content for4/5 must come from existing project facts or later user direction.

Open clarifications, asked asynchronously before implementation:

- Does20attempts mean a balance target or a mandatory unlock counter? Provisional direction is a balance target (earned upgrades bring completion around20attempts), preserving route-end completion rather than forcing meaningless replays. Do not finalize a conflicting unlock design before considering the user's reply.
- Is CPU saturation in Editor, local generation, or a device? Investigate both project startup and task-owned generation processes while waiting.
- AdMob production IDs exist or use official test IDs first? Complete test integration without inventing production identifiers.
- Resolved: a primary-licensedCC0 Korean family was found and shipped as Jigmo Game UI; no OFL exception is needed.
- Resolved: original Unity Editor recovered without being killed. The original live scene/preferences were backed up and preserved before native authoring; repeated tests use isolated QA.

## Alternatives considered

### Character presentation

1. Flat billboards/sprites: strongest2D look, but poor turning/camera readability and does not use the requested TRELLIS/bone workflow well.
2. Exaggerated toon3D mascots: large heads, short limbs, simple painted eyes and broad color areas, with real skeletons and separated rigid held props. **Selected** for consistency with Noryangjin and existing movement/attack integration.
3. Recolor current realistic adults: cheapest but does not fix silhouettes or the user's stated problem. Rejected.

### Stage design

1. Clone the entire Noryangjin route with new decoration: familiar but thematically weak.
2. Distinct zone sequences using the existing lane/turn/height system: **Selected**. Highway progresses through approach, lane closure, toll/work site and exit. RestStop progresses through entry parking, storefronts, dining area and rear/service lane. Use50% enemies,25% bonus choice stations,25% gimmicks as the encounter baseline, with readable safe approach windows.
3. New open-world navigation: conflicts with the existing forward shooter and inflates scope. Rejected.

### Video ads

1. Forced interstitial on every failure: disrupts a20-attempt loop. Rejected.
2. Optional rewarded video after a completed round: **Selected**. Offer a clearly labeled coin bonus with one reward per eligible round, grant only on successful reward callback, and support no-fill/offline/cancel states. Balance the ordinary no-ad path to the target so ads are optional.
3. Startup/app-open video: worsens the startup complaint. Rejected.

### CPU and generation

Inspect actual stalls, exceptions, memory and worker count.100% CPU is utilization, not itself a crash diagnosis. Retain the already-proven workbook-style compaction. Run task-owned TRELLIS/Wan/Blender heavy jobs sequentially, bound their thread counts and lower background priority, leave CPU capacity for Unity/OS, and retain results before unloading models. Do not globally throttle unrelated user processes.

## Implementation units

### U1 — Recovery and baseline inventory
Verify live Editor state; preserve a fresh scene/preferences snapshot; audit existing maps, models, fonts/licenses, ads, generated-asset provenance, route support/collision and startup performance. Record reference images and bad-asset inventory. No hidden architecture in chat.

### U2 — Opening and cosmetic presentation
Use the current upgrade shop's visual language, clear content hierarchy and restrained ornamentation. Keep full video frame, subtitles/controls in readable dedicated areas, accurate timing, skip/replay/Next and cleanup. Use one verified license-compliant TMP family with appropriate weights/fallbacks. Review the skin shop in actual gameplay resolution and change confirmed problems.

### U3 — Characters and props
Create new TRELLIS reference images for six Highway roles and a distinct rest-stop roster where absent. Generate with the installed TRELLIS2 automation, then rig in Blender with idle/walk/run/attack/death and separate rigid tools/projectiles. Include melee and ranged roles with clear anticipation. Audit all Highway/rest-stop prop palette items and placed instances, especially vehicle orientation, ground remnants, wheels/support heights and colliders. Repair source transforms/geometry or regenerate only unsuitable assets. Fresh import plus visual multiview/action review is required.

### U4 — Playable chapters and transitions
Author/refine `HighWay` and a separate `RestStop` using existing scene-authoring APIs. Register in build settings, bind route consumers, add chapter3 navigation/map tooling and workbook rows. On route completion, award completion once, stop gameplay, play/skip the appropriate transition and load the next valid scene asynchronously. Generate two Wan candidates per requested transition from independently created input images: shark alarmed by fast highway traffic; ordinary dining/parking rest stop where people react to the arriving shark. Select by actual motion/anatomy/readability, retain candidates and provenance, preserve watermark requirements.

### U5 — Progression and rewarded ads
Rebalance base HP/attack, upgrade increments/prices/caps, special-shop and cosmetic effects in canonicalData.xlsx using the deduplicating graft path. Simulate and play cohorts with actual earned currency/purchases, aiming at20attempts per chapter across the five-chapter design. Include carried progression between authored chapters. Add Google Mobile Ads rewarded test integration, correct initialization/consent ordering, main-thread callbacks, one-shot reward identity and lifecycle cleanup. Production activation requires actual account/app/ad-unit details, never fabricated IDs.

### U6 — Three refinement cycles
For each cycle record baseline build/version, fresh preference snapshot, route/earnings/upgrade behavior, discovered issues, implemented corrections and rerun evidence. Cover Noryangjin, Highway and RestStop as available. Third pass includes transitions, shops, ranged enemies, prop support, CPU/startup behavior and ad success/cancel/no-fill. Restore the user's state after tests. Keep playtest evidence separate from mathematical estimates and editor-only mocks.

## Verification and execution posture

Sequential work in the main thread per project instructions. Existing map2 branch and unrelated work remain intact. No commits, pushes or publication requested. Use official Unity CLI/Pipeline for scenes/assets; no YAML scene editing. Use source-hash guards for workbook installation and regenerate/verify protected Data.bytes. Public behaviors and mutation/reward/transition logic need meaningful tests. Visual changes need actual screenshots; meshes need Blender fresh-import evidence. Do not represent test ad IDs as production ads or simulations as physical-device tests.

Progress, decisions resolving open questions, commands and outcomes go in the companion execution log. The plan records intended design and is not the progress tracker.
