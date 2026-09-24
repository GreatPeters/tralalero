# Review corrections and twenty real attempts

Status: completed in source and the live Editor on 2026-09-23. Branch `fix/mobile-combat-ui-performance`; pre-existing bonus amount correction retained. No merge, push or phone deployment in this task.

## Authorized scope

1. Severe start-gantry obstruction: preserve minor/edge occlusion, fade only severe simultaneous obstruction of shark and immediate route, approximately0.2s to40%opacity and restore after passage.
2. Bonus display/application agreement: already corrected; verify it remains correct.
3. Communicate dangerous enemy contact before impact, preserving existing health-exchange combat rules.
4. Show actual last damage/death cause in the defeat screen.
5. Improve Woman hand/knife fit and apron rendering; retain her current health.
6. Distinguish highway hazard telegraphs from green/pink route guides and improve small/awkward highway enemy appearance.
7. Define attainable chapter-entry growth profiles from the existing upgrade tables, then run20actual attempts with recorded entry stats/policies/outcomes and native screenshots.

Explicit boundaries: do not alter helper count/formation or repeated shop/sign/enemy placement layout. Provide existing repetition screenshots for discussion. No commit/push/phone deployment requested in this task.

## Execution units

-U1: scoped severe-occluder fading and native pass-through verification, including slight occlusion preservation.
-U2: shared player damage provenance; contact warning and defeat recap, with root-player/duplicate-hit/reset tests.
-U3: native Woman rig/material refinement and highway enemy scale/material refinement; whole-clip and scene screenshots.
-U4: distinct highway warnings; preserve authored green/pink route choices.
-U5: chapter entry profile calibration with existing permanent upgrades/costs, no runtime invulnerability or damage cheats.
-U6:20fresh real attempts, 8SR18/6HighWay/6RestStop initially planned, movement at normal input cadence; genuine death/clear/timeout recorded. Later-chapter profile limitations remain explicit.
-U7: restore user state, verify source/data preservation, deliver repetition photos and correction/test gallery; record reusable lessons through ce-compound.

Native source/scene/material mutations used Unity Pipeline. Backups and preference snapshots: `tmp/review-fixes-20-runs-2026-09-22/`. [Final evidence and run ledger](../../../map-concepts/review-fixes-20-runs-2026-09-22/README.md).

## Verification and outcomes

- All seven execution units completed. Real 1x gantry passage confirms the 40% fade and restoration; minor obstruction remains. Woman carry/slash inspected at 16 evaluated poses with handle error <=0.005m. Contact warning and defeat recap checked using real native collision and actual matrix results.
- 20 recorded attempts: 8 SR18, 6 HighWay, 6 RestStop. 1,531 native PNGs. Three clears (two SR18, one HighWay), 17 deaths. RestStop reached and exited the 30-second restaurant holdout; both deepest attempts subsequently died at a crossing car around 189.7s. Full RestStop completion remains unverified.
- The first 12 later-chapter attempts used faulty automatic avoidance: moving-car side choice flipped and toll selection ignored arrival time. Preserve these under `steering-diagnostics/`; the corrected 12 reruns replace them in the final matrix. Total terminal attempts: 32, plus one earlier Editor-paused partial attempt. No weakened enemies or endurance cheats were used to improve outcomes.
- 135 focused EditMode cases pass: ReviewReadability 11, camera occlusion 6, bonus displayed amount 6, altar rules 18, talisman 10, combat feedback 5, route feedback 13, mobile combat 7, seagull 14, FatMan pose 6, enemy event 24, throw direction 9, run balance 6. Raw reports are in `tmp/review-fixes-20-runs-2026-09-22/regression-tests.json`; the final six run-balance cases are recorded separately in this ledger.
- Both runtime and Editor C# builds pass. Final compiler output has one existing third-party `SplineSpeed.m_LastIndex` warning and no errors. Harness and protected runtime archive validation pass; Editor console reports no errors.
- `git diff --check` reports only Unity-authored empty `m_Name: ` fields in serialized assets. Source/doc whitespace passes; these native serialization lines were not hand-edited.
- Final review also found that the standalone Boat paddle would inherit the Guard-shot label. Assign its own Paddle cause; a real source-prefab test covers it. This is a recap classification fix, with attack damage and timing unchanged.
- Original 66 preference records and absent TutorialDone restored and compared. Wallet remains 731 coins / 0 jewels; temporary map test speed restored to 1. SR18 is clean in Edit Mode. Native three-scene audit verifies defeat bindings, no null occluder references and unchanged radius distributions (SR18 44 at0.55, 4 at0.87, 2 at0.82; highway/rest-stop0.57/0.75).
- Workbook SHA-256 remains `4ccf8641bd5fb165846206ce82e84f8c4bdf2b4dd1b4713cb7abe0328800eee0`, identical to the previous fixed exported workbook. Helper formation, weapon and route/holdout scripts have no diff. Repeated scenery/actor root layout was not authored by the installer.
- The browser gallery was inspected at actual rendered size; original repetition screenshots and a clearly marked, unapplied image concept are included. Reusable pose-capture, occluder-reference, steering and provenance lessons were recorded with `ce-compound mode:headless` in the existing moving-gameplay validation entry.

Limits: purchased-level profiles compare growth conditions but do not prove natural campaign affordability. Automated steering and accelerated capture are not human win-rate or device FPS measurements. Visual changes retain the existing low-poly art style; continuous Woman animation was not exported as a new video.
