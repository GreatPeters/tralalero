---
title: Preserve workbook contracts when converting Highway actors to vehicles
date: 2026-09-27
category: workflow-issues
module: Unity Highway chapter2
problem_type: workflow_issue
component: development_workflow
severity: high
applies_when:
  - "Replacing authored enemy presentations while preserving workbook stats and rewards"
  - "Verifying a Unity scene with normal growth stats and exact user-state restoration"
tags: [unity, highway, workbook, vehicle-combat, playerprefs, native-validation]
---

# Preserve workbook contracts when converting Highway actors to vehicles

## Context

The user approved a new vehicle-only chapter with two popup choices, car-health collisions, tanker chains, a single log and unique rewards. The existing workspace already contained substantial unrelated changes. Only HighWay was authorized for scene authoring; the workbook, other chapter scenes and PlayerPrefs needed precise preservation.

## Guidance

1. Snapshot the working copy, not HEAD. Back up the scene/workbook and each shared source before changing it. Record hashes of every protected scene. Keep scope anchored to the new canonical document rather than earlier proposals.
2. Extend the existing combat transaction. Vehicle bodies use EnemyScript_space, workbook placement IDs, hit numbers and coin drops. Vehicle-specific callbacks are conditional; human combat in other chapters stays on its existing path. Clear destroyed colliders immediately, while cosmetic wrecks fly through a separate pool.
3. Inspect actual workbook coordinates. These sheets start in column B, not A. A first dry run incorrectly searched the growth chapter in A and failed before writing. Use observed headers/addresses and preserve all non-HighWay row addresses. Patch only owned XML parts instead of re-exporting the workbook.
4. Verify preserved data mechanically. Eighteen unrelated ZIP parts and296existing rows remained unchanged. New health formulas retain caches and reference the current chapter growth curve. Retired placement IDs/flags must agree with the authored hierarchy because one missing ID aborts a whole scene's placement application.
5. Treat visual assets as geometry, not labels. A3.4m-width fit turned the log into a giant trunk; normalize its long axis instead. Body orientation and silhouette need native capture review, even if the root transform travels in the correct direction. An ambiguous rear can be avoided in decorative traffic by selecting a clearer existing model.
6. Match runtime interfaces. All active shark weapons here use BulletKind.Water. An initial Bomb-only unique-effect condition compiled but could never run in ordinary play. The directed probe now fires the production callback with the real default kind and verifies adjacent health changes.
7. Preserve unclipped incoming damage for shields. Legacy fatal contact passed only the player's remaining HP to ApplyDamage. At low HP, that made a large car impact look too small to qualify. Vehicle contact now qualifies the shield using incoming car health before clamping; the low-health probe verifies the car is removed and the player survives.
8. Separate driver defects from balance. A driver that abandons the opened weak lane can hit the remaining queue. Hovering just outside a guessed radius can also clip the actual1.38mplayer capsule. Reserve proper lane centres, include PlayerMove's smoothing in swept lane changes, account for finite projectile range/travel and do not interpret a zero-delta Editor sample as a stopped car. Route progress is not guaranteed to equal world arc length on offset curves. A cash-exit contact occurred almost6progress units behind the player while the actual collider was still overlapping. Predict both car/player world poses through the route sampler before testing their footprints.
9. Restore native preferences exactly. Registry DWORDs can arrive as unsigned Int64 values, so checked Int32 conversion overflows. Preserve their32bits with an unchecked conversion. Store name/kind/value bytes canonically, restore key presence and values, compare exact snapshot bytes, and separately restore the original start-scene asset and selector.
10. Verify asynchronous results. Pipeline's30-second server timeout can happen while native mesh authoring continues. Inspect the completion receipt and live scene state before retrying. A run_tests0/0acknowledgment is not a verdict. Save the owned scene before tests; a modal save prompt blocks the main thread.
11. Regenerate the protected runtime archive after workbook XML changes. Editor-side values can appear correct while the packed player data remains stale. Run GameDataWorkbookEditor.EnsureRuntimeArchiveCurrent(false), inspect completion after any tool timeout, and verify the archive/schema tests before accepting the data change.

## Why This Matters

Compiling a replacement model does not establish gameplay parity. Static layout, live placement activation, actual projectile kinds, shield qualification, the driving policy and state-restoration mechanics each caused distinct failures here. Keeping rejected cohorts made those causes distinguishable from successful coverage or first-gate completion.

The reference page contained both illustrative images and explicit numeric requirements. The implementation uses the requested road dimensions and workbook values; it does not copy illustrative health/coin numbers from generated pictures.

## When to Apply

- Authored Unity scene conversions with data-driven placement.
- A new renderer or enemy wrapper around existing combat code.
- Automated scene trials that must preserve the user's current save.

## Examples

- tools/apply-highway-chapter2-workbook.py and verify-highway-chapter2-workbook.py perform and audit the narrow workbook mutation.
- tools/install-highway-chapter2.cs performs native authoring, records phase completion and verifies saved references.
- tools/probe-highway-chapter2.cs tests timeout/default selection, tanker cascades, collision release, effect limits and every unique reward.
- tools/reststop-korean-playtest.cs has a dedicated HighwayChapter2 path. Its older endurance methods pin HP and disable hazards and are unsuitable for this acceptance test.
- outputs/highway-chapter2-2026-09-27 contains failed cohorts, accepted results, original snapshots and byte-restoration receipts. Preferences are not copied into the report website.

## Related

- [Traffic sessions and live hazards](validate-highway-traffic-across-play-sessions-and-live-hazards-2026-09-26.md)
- [Placement and driver errors versus balance](separate-road-placement-and-driver-errors-from-campaign-balance-2026-09-23.md)
- [Highway combat and coverage separation](audit-highway-branches-with-combat-and-coverage-separated-2026-09-26.md)

No external issue was required or posted. The project already directs agents to search docs/solutions, so no discovery-rule edit was needed.

Final acceptance:63focused tests and14directed assertions passed. Both hi-pass paths use play-v4 evidence, jam/cash uses play-v5, and open/cash uses play-v6. Each route completed at ordinary growth stats in approximately298.3seconds; all final preferences were restored byte-identically. These are route checks, not an earned-purchase economy or phone-performance certification.
