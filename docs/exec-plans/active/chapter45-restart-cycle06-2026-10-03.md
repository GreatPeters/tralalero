# Ch4/5 normal Editor restart and persistent-save boundaries — cycle06

The latest parent instruction authorizes normal Unity exit/relaunch only after checking that no user edits, dirty scenes, recent desktop input, or active independent work would be interrupted. Cycle05 is complete; its 193 assertions and route runs are not rerun or represented as current cold-restart evidence.

## Preservation and preflight

- Preserved 272 current source/scene/controller/settings/package hashes, minimal local recovery copies, original Editor state and typed shell-context registry data (48 values). The shell view was later proven different from native Unity; the pretest native PlayerPrefs catalog of78 entries is authoritative for observed game-save restoration. No external backup, file deletion, commit, push, device build, install or authentication changes.
- Current branch `qa/chapters45-full-suite-20261002`, HEAD `295a18cab0ad4156baa9106077bc10a47ac6d734`; 127 inherited tracked deletions remain untouched.
- Two native preflights showed clean idle ShoeTower, no prefab stage, stable Undo group, no active QA callback; desktop input had been idle for over 35 hours. Other local services were idle and left alone.
- Initial native PID 25932 exited through `File/Exit`, with an `EditorApplication.quitting` receipt; existing official Unity CLI reopened PID 59008. All 48 registry values remained exact. No force termination.
- Initial CLI wrapper retained inherited output pipes after successfully launching Unity. The existing native Editor was adopted after Pipeline verification; no second launch. Subsequent launcher receipts use file handles, avoiding that wrapper issue.

## Save contract and coverage

Source inspection found permanent wallet, chapter reward/unlock, upgrades, cosmetics and settings persistence. It found no mid-run checkpoint implementation for route position, floor, lift or cinema timer. `CanvasScript.LoadGame` reloads the scene. This cycle therefore tests permanent data preservation plus clean transient-state reset; it must not be described as successful position-based Continue.

Ten directed boundaries: Ch4 left/right rewards and completion; Ch5 moving lift, B1 landing, active/completed cinema, before white-shoe contact, first completion and replay completion. Earlier fights and starting positions are seeded; the selected native choice/lift/survival/goal logic then runs. Cinema-completion fixture alone uses 300 HP with weapons disabled to isolate timer completion; it is not a balance test. No manual save or forced victory is used. Each boundary is frozen only after evidence capture, followed by normal Editor exit and a fresh native process. The same chapter is explicitly selected by QA after restart.

## Completion gate

Inspect failures before changing game code. Preserve all receipts and raw screenshots. After cases, restore original Editor state, normally close, restore the exact 48-value registry snapshot while Unity is closed, relaunch normally, restore lost SessionState flags, verify again without entering Play. Verify protected/shared/recovery hashes and original persistent-data file. Report startup Unity AI Toolkit account-service errors separately from game errors. Publish a verified local gallery and concise review.

Evidence: `outputs/chapter45-detailed-design-2026-10-03/restart-cycle06/`. Status: initial cold restart passed; directed cases beginning.


## Completed and preservation-scope correction

135 assertions passed across ten directed boundaries, zero final case failures. Thirteen normal exits/reopens completed. The initial48-value shell registry snapshot was later proven to be a DIFFERENT VIEW from native Unity, so it is not game-save restoration evidence. Five changed native reward keys were restored through the actual PlayerPrefs API, then all78 original native observed entries matched in another fresh process. Other42 native registry values stayed identical across this repair/restart; full pretest native registry was not captured, leaving unknown pretest keys outside exact comparison. Editor state and272 protected source hashes match. See [cycle06 review](../../reviews/chapter45-restart-cycle06-2026-10-03.md). Mid-run checkpoint resume remains unimplemented; no game bug reproduced or game asset edited.
