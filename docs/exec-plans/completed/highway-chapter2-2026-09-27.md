---
title: Implement chapter 2 Highway — end of the traffic jam
date: 2026-09-27
status: completed
---

# Scope and authority

Implement the user's pasted request and `outputs/highway-concepts-2026-09-26/site/highway/2챕터-고속도로-기획서.md` revision2. The eleven images referenced by its current `index.html` have been opened directly. Their illustrative numbers and contradictory vehicle headings do not override the document/request. Do not read/follow the retired sibling proposals. No commits, no changes to RestStop or Noryangjin scene files, no TRELLIS.

Backups: `outputs/highway-chapter2-2026-09-27/before/` contains HighWay, Data.xlsx, all protected scene hashes and the exact request. Existing dirty working-copy changes are preserved.

# Execution units

1. Extend workbook-backed placement metadata for vehicle kind/lane/station/phase and add chapter2 settings. Patch only owned XML parts, preserve other sheets and formula caches. Keep existing enemy combat/reward/growth tables as the source of truth.
2. Implement vehicle enemies through `EnemyScript_space`, route traffic admission/following, remaining-health versus fast-road contact, tanker chains, and pooled readable destruction. Vehicle-specific hooks must leave other chapters unchanged.
3. Reuse `HighwayRoute.Fork` with explicit popup choices, automatic routing, timeout and bounded slow motion. Implement four route combinations, unique effects, random bonus walls and reactive news.
4. Implement one isolated primary event at a time: accident groups, staggered police/tow waves, one log from a northbound opposite truck, construction workers/cones, existing deer and fatal potholes. Protect the specified quiet windows and passage.
5. Build the saved native scene through an idempotent `tools/` run_script installer:4.4m lanes on each carriageway, median/paint, shark0.8, camera1.15,2340m route, branch roads, toll exit without gates, consistent Meshy/outline scenery and GmarketHarbor UI.
6. Update scene contracts and focused integration/rule tests. Build Runtime/Editor and run harness validation. Extend `reststop-korean-playtest.cs` for normal-growth first-accident and four-route verification with real collision, retained captures, preference byte comparison and exact play-start restoration. Compare native captures with approved reference images.
7. Review implementation, document actual outcomes/limits and compound reusable findings. No completion claim until required checks and state restoration are verified.

# Interpretation notes

- North/south mean route-forward/route-backward; visual global world-Z can change on bends.
- An accident intentionally blocks the front with destructible vehicles. Its weak side vehicle is the shoot-through passage; ordinary moving hazards must retain a directly open lane.
- Unique effects use the stated default scope of the current Highway run; no contrary preference was received. No RestStop scene edit is needed.
- The HTML lists extra counts; where it differs from the markdown, markdown/request wins. No warnings are added to swarm or single-log events.
- New external models are optional. Inspect existing Meshy props first; show a concept before any Meshy generation and enforce the200-credit cap.

  Subsequent user amendment after completion: future Highway model work has a2,000-credit total cap and may replace polyperfect/ithappy/older production assets to unify the tone. Present the new-model list and estimated credits first, then obtain user confirmation of Codex concept images before Meshy generation. Meshy7.1,15kpolygons, the Noryangjin outline material and at most two regenerations per model remain required. See the latest USER_STATED_REQUIREMENTS entry. The completed implementation below incurred0credits under its original scope.
- Progress/results live in `outputs/highway-chapter2-2026-09-27/README.md`; this plan records the execution contract.

# Completion

Saved HighWay contains the requested chapter. All four normal-growth route combinations clear in approximately298.3seconds; first-entry weak-side accident passage succeeds atHP960/ATT48without damage. Runtime/Editor builds,63focused EditMode tests,14directed runtime assertions, workbook preservation and harness checks pass. Final preferences match the original bytes, playModeStartScene is restored to RestStop and the saved HighWay scene is clean. Protected other-chapter scenes match before hashes. No commit, external model generation or paid credits.

Actual screenshots are paired with reference images at http://127.0.0.1:8776/highway/implementation/. Failed cohorts remain documented; earned20-attempt economy, human difficulty and phone performance are unverified. Detailed receipts and exact accepted cohorts are in the output README and review.
