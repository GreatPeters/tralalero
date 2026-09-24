# Three-chapter campaign balance and damage feedback

All three chapters have actual earned-purchase clears. Final cohort: `tmp/image-previews/campaign-balance-2026-09-23/r8-final-rewards/`. Same-seed comparisons, three complete clear replays, visible reward panels and final user-state restoration are verified.

## Measured progression

| Chapter | Attempts to first clear | First attempt | Clear | Purchase visits after first attempt | Endpoint seconds gained / visit |
| --- | ---: | ---: | ---: | ---: | ---: |
| 1 · Noryangjin | 30 | 34.8 s | 305.5 s | 27 | 10.026 |
| 2 · Highway | 19 | 42.8 s | 298.57 s | 18 | 14.209 |
| 3 · RestStop | 7 | 187.13 s | 299.43 s | 6 | 18.717 |

Chapter1 meets the20–30-attempt target; all stages last about300seconds. Chapter3 inherits substantial growth and clears faster, with an endpoint ratio above the10–15-second goal. The combined endpoint ratio is12.525seconds across51purchase visits. These are pacing indicators, not proof that every upgrade adds10–15seconds. A visit can buy several upgrades; random helpers and bonuses cause substantial variation and plateaus. Do not relabel visits as individual upgrades or these automated results as human win rates.

The driver buys ATT/HP/attack-speed through normal `UpgradeUI.TryBuy`, saves when the next chosen upgrade is unaffordable, and carries earned currency between chapters. Normal movement/collision, recorded seeds, fixed30game-fps at3x, default cosmetics, no free currency/ads/stat overrides. Wallets reconcile; final bank13137. Actual completion is recorded on runs30/49/56. Unchanged accepted chapter1/2records are reused, not counted as additional newly played attempts. Failed/calibration cohorts remain separate.

## Canonical balance

Source `Assets/ShooterSurvival/GameData/Editor/Data.xlsx`, revision `r5-reststop-rewards`, SHA256 `fcdd4fe5c8dfe4947f7adafdf9c0d4a751a6af464a71bcccaf05c70f551558ff`. Protected runtime archive regenerated and verified. ArtifactTool authors/calculates the changes; the graft preserves unrelated parts, styles, formulas and caches.

- Connect21left normal-enemy growth inputs to the formerly disconnected C18; growth16%. Last normal1885, elite3393, Woman3704 with the previous50%buff retained.
- Late ATT/HP price coefficient3.1→2. Goals12seconds/25attempts recorded without attempt locks or adaptive enemy weakening.
- Highway enemy/progress rewards100.
- RestStop first normal2000HP, station growth5%, enemy/progress rewards350. The preceding450-coin trial also cleared in7attempts; reducing rewards did not remove the favorable bonus run.
- Coffee preparation rows0.6s. Existing bonus amount fixes, FatMan attack100/one throw, seagull20%contact, helper formation and lethal major hazards remain.

## Implemented repairs

- Committed HP loss emits exact amount and typed source before HUD refresh. A reserved world-number slot and footer group rapid hits. Receipt-frame/hitch protection prevents premature expiry; enemy popup bursts cannot overwrite player loss.
- Alive-only healing, regeneration/recovery and health bonuses cannot undo fatality. Dead players cannot consume legacy health walls or receive simultaneous victory. Equal-health contact kills both; legacy enemy death is idempotent.
- Friendly water/bomber shots follow their own continuous-road progress, lane/clearance and spread. Stationary defense keeps free aim; the separate Noryangjin/RestStop turn path remains.
- Gate arms warn and move before closing while another lane opens early. Vehicle warnings persist through crossing. The first highway oncoming beat starts at84m between combat passages.
- Three existing RestStop TRELLIS bodies/textures/18-bone rigs receive seven actions, ~3.05m native height, fitted cup/paddle/spatula, recoil and1.1sdeath. This is motion/attachment repair, not new TRELLIS generation. Throws are retimed to workbook preparation and detach after final pose evaluation.
- All25RestStop pairs share one-contact claims and keep patrols in their lanes. Canonical road projection fixes drift that placed an enemy atlane−5.3 outside the player's±4.4range. No highway funnels were added.
- Five bends receive chevron boards and10outer trees, with no active colliders. Existing repeated layouts and helper formation are retained.

## Evidence

- Final cohort: `measured-runs.csv`, `measured-summary.json`, purchases, saved states, timelines, condition hashes, actual clear PNGs and completion flags.
- Versioned summaries in this folder retain [all56measured attempts](measured-runs.csv), [chapter totals](measured-summary.json), [same-seed comparisons](paired-upgrade-results.json), [194test results](regression-summary.json) and [authored workbook changes](workbook-edits.json). Large native captures and checkpoint saves remain in the preview directories.
- Same-seed purchase comparisons: `tmp/image-previews/campaign-balance-2026-09-23/paired-upgrade-checks/results.json`. One checkpoint per chapter gives106.23→106.23s,100.5→100.5s and248.53→274.83s. Two plateau results and a26.3s jump explicitly disprove a guaranteed10–15s per purchase. These are six separate comparison runs, not additional campaign attempts.
- Final damage PNG: `lifecycle-burst-heal-hitch-safe/combined-damage.png` under the preview root. Physical roadblock500→0 and equal contact500/500→bothdead are separate controlled fixtures, not balance runs.
- Native Coffee release0.6s and Tire0.55/0.8s probes have zero detach discontinuity and100HP damage; these fixtures use actorHP10000/attack100.
- Native previews: `showcase-native-v2/CoffeeVendor`, `showcase-native-v2/SnackChef`, `showcase-native-v1/ParkingMarshal`. Nine Blender/FBX/GLB gates pass without issues. Fresh body-only GLB evidence is `reststop-motion-v2/*/fresh-glb-clean`; earlier renders included importer gizmo bounds. [Iteration review](iteration_review.json).
- Source/export: `outputs/campaign-balance-2026-09-23/reststop-motion-v2/`; native tools are separately bound in Unity.
- Native regressions194/194 across22suites. Runtime/editor builds pass; editor build has the existing third-party SplineSpeed warning. Harness and source-diff checks pass. No whole-project test-suite or device FPS/APK acceptance is implied.

Original66preference keys plus11tutorial/workshop keys are restored and all77existence/value records verified: coin1781, jewel0. GameView is restored to index23/1080×2340, timeScale1/captureDeltaTime0, with clean HighWay Edit Mode and the current runtime archive. [Final state](final-state.json). [Three clear replays](clear-replays.json) reproduced305.47/298.57/299.43s and visibly showed the actual transition/reward panels. No merge, push or phone installation is included.

After full RestStop regeneration, rerun motion/pair/corner installers and saved-scene checks. Do not align from shifted actors or infer bad balance from a driver targeting another road. See [damage lifecycle](../../docs/solutions/ui-bugs/keep-player-damage-visible-through-hitches-and-recovery-2026-09-23.md) and [placement/driver diagnosis](../../docs/solutions/workflow-issues/separate-road-placement-and-driver-errors-from-campaign-balance-2026-09-23.md).
