# Ten-run visual, balance and gameplay review

Status: complete. Ten actual attempts,458native screenshots,9curated findings/checks. No gameplay fixes authorized or applied in this review.

User requested about ten playtests and screenshots of visually awkward/AI-looking content, balance problems and opportunities to improve fun.

Method: six SR18 runs and two each of HighWay/RestStop, normal player stats from the user's existing save (observed HP500/ATT68), actual `PlayerMove` steering, 1x game time. Alternate left-biased, attack, health, helper, late-reaction, highway-main and bypass choices. No teleport, health/damage override, disabled enemy/collider or forced reward. Later chapters are launched directly in the Editor for chapter-specific review; this does not claim natural campaign progression to them. A run ends on death, clear, an unhandled modal or a 360-second observation limit; all outcomes are recorded honestly.

Existing authored scenes remain unchanged. Snapshot: `tmp/ten-run-review-2026-09-22/before-prefs.tsv` plus tutorial-related keys in `extra-prefs.tsv`; restore test-earned currency and original key presence after the last run. Raw native captures/timelines: `tmp/image-previews/ten-run-review-2026-09-22/run-NN`. `tools/ten-run-review.cs` captures game rendering with overlay UI, periodic state and actual damage/bonus events. Capture work can introduce hitches, so this pass does not make FPS claims.

Initial observations to confirm across runs:
- Run01 at8s: a large gray foreground surface obscures the player/route.
- Run01: left-biased bonus policy dies at94.7s,7choices,6HP-loss events.
- Run01: displayed ATT+12 pickup produces approximately+1 runtime ATT; trace display vs applied stat rather than guessing from the icon alone.
- Large upper guidance panel and route props compete for preview space; inspect actual legibility and repeated examples.

Run02 confirms repeated flat-label mismatches: ATT+12/+16/+17 each increases runtime ATT by1. The table uses Ratio values0.12–0.18, multiplied by `originalDamage=8`, while display multiplies by100 and omits the percent suffix for Ratio. Current health baseline for this calculation is60 although starting max/current HP is500. This is a confirmed display/application mismatch, not a subjective difficulty judgment.

Steering methodology changed for run03 onward: also avoid a surviving enemy inside5m and visible incoming projectiles inside13m (6m for late-reaction policy). Runs01/02 therefore are not a controlled comparison of bonus policies against later runs; their deaths cannot be used as a human difficulty estimate.

Runs01–04 completed as real deaths: approximately95s,140s,281s,299s. Run04 reached the final Woman boss; about879 boss HP remained when contact killed the player. Run05 is in progress. Starting stats stayed HP500/ATT68 in all starts. No gameplay scene/asset edits have been made.

Read-only overlap exploration is NOT evidence of an unavoidable wall: a sampled lane query against already-played actors reported only24/41 lanes blocked. Do not extrapolate planar radii into a claim that every lane is blocked; vertical capsule positions and death/movement poses matter.

Run05 foreground query identifies the start gantry mesh among the non-hidden renderers intersecting the lower-centre view. This is a root-cause candidate; the screenshot itself is the confirmed obstruction evidence.

Deliverable: curated local browser gallery with original PNG links, severity, observed evidence, reproduction context and suggested improvements. Separate measured defects from subjective visual judgments. Do not fabricate a human enjoyment score from automation.

Final evidence: `map-concepts/ten-run-review-2026-09-22/README.md` and `findings.json`; browser gallery `http://127.0.0.1:6753/ten-run-review/`. All70tracked preference records restored with zero mismatches;731coins/0jewels restored, source workbook hash unchanged, SR18 clean in Edit Mode. Later highway/RestStop content was not reached in these direct-entry runs and is explicitly excluded from coverage. The existing moving-gameplay visual-review learning was updated via ce-compound headless.
