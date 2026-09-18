# Road chapter patterns execution

Plan: `docs/plans/2026-09-13-feat-road-chapter-patterns-plan.md`.

## Status

Completed. The original HighWay and RestStop scenes contain the new patterns. Runtime and Editor builds pass; 123 selected native tests pass. The final archive matches the workbook. See `map-concepts/road-patterns-2026-09-13/README.md` for the concrete results and scope limits.

## Execution and review

1. Read native route, input, enemy, placement and reset contracts; checked the Ministry's colored-guidance reference; wrote the plan before implementation.
2. Authored 469 highway samples, continuous road surfaces, two playable bypasses and five oncoming traffic beats. A first continuity test failed at an offset branch; replaced linear sampling with Catmull-Rom interpolation and rebaked the shared road meshes. Both branches and lateral edges passed support and heading checks.
3. Authored the food hall with a bounded police pool. The first real view was too broad, hiding side doors. Reduced the interior to 26.4 × 43.2, removed road kerbs through it, improved the HUD/signs and cleared side entrances. Moved nine scenery roots clear of bypasses.
4. Adjusted police health through the canonical workbook. The initial 360 was too weak in the section probe; 900 was excessive. Final 700 preserves a difference between entry and late-chapter upgrades. Runtime values come from the signed archive.
5. Found a reused police actor returning to its original door in OnEnable. Added explicit inactive spawn preparation and an actual-position regression test. The accepted run spawned 27 police across all four doors with zero position error.
6. Kept supporting systems connected: route-relative helper travel, stationary helper behavior, helper shot targeting, curved-road activation positions, and live analytics progress without old turn triggers.
7. Verified ordinary-input green-branch selection/recovery/rejoin, a normal-stat oncoming-car opening, a separately labeled full-route endurance traversal, low-upgrade holdout death cleanup, and successful late-chapter holdout completion/pause/camera restoration.
8. Updated an obsolete combat test's historical 27-enemy expectation to the current 25 paired formations without changing the live Noryangjin data. Saved final suite results separately from diagnostic failures.

## Data and validation

- Added controls at balance rows109–118 and environment rows26–34. RestStop speed7.8. Disabled outdoor station11/12 rows (eight records) while preserving all 300 placement IDs.
- Applied only reviewed cells from the Artifact Tool candidate to the native workbook, preserving unrelated content and using deduplicated styles. Final source SHA256 `f62c32c0ec64333a3e7645ce3c508e9a88028c3d68f5d3894c594693acc45d56`.
- Tests: RoadChapterPattern19, NoryangjinTurnSpot48, Sr18CombatPolish12, ChapterPolishIntegration4, GameplayAnalytics25, GameDataWorkbook15.
- `dotnet build Assembly-CSharp.csproj -nologo --no-restore -v:q` and Editor counterpart succeeded. Two existing runtime warnings remain. `tools/validate-agent-harness.ps1` passed.
- Final QA console had zero errors. No new Android build/device run or full 20-attempt progression cohort was performed for these patterns; the older APK and cohorts remain historical.

## Preservation

Fresh scene and63-key preference snapshots were taken. Automatic approval review once rejected opening a dirty original scene because unsaved work could be discarded. A full native copy was saved and verified before reopening, then archived outside Assets. No content was lost and no approval block remains.

Native regression tests changed upgrade cache/level keys. Restored their pre-test snapshot after all tests, and independently verified63 matching keys, coin3007, jewel0 and a valid runtime archive. The original Editor remains in HighWay Edit Mode. The owned QA Editor was stopped after restoring its own initial preferences, using matching PID/start time/project identity.
