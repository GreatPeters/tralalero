# Later-chapter30-attempt calibration log

- Current user state captured separately in `tmp/later-chapters-30-2026-09-23/`:66base keys,11extra tutorial/workshop keys, original scene, render cap and GameView. Original coin1781/jewel0. Existing branch/work and earlier snapshots preserved.
- Starting workbook hash `fcdd4fe5c8dfe4947f7adafdf9c0d4a751a6af464a71bcccaf05c70f551558ff`. Existing ArtifactTool limitation on Korean-sheet MAX rendersC92/C93asNAME errors; canonical caches/formulas remain valid60and are preserved by the graft. No unrelated formula rewrite.
- Trial1: Highway enemy/progress rewards100→65, all enemy health/attack and chapter1/3data unchanged. Exactly54changed input/dependent cells are all reward values×0.65, with formulas unchanged. Revision`r6-highway-30`, hash`38c96173991a091fc825207c561df7243fcfcac47d459f378b7ffe2ee2e703a5`; native archive validated.
- Cohort`r9-highway-30` reuses unchanged chapter1records/state30 fromR8and plays new Highway attempts with real purchases. First three attempts42.8/42.8/42.8s,390earned each; purchased levels14/17/8then14/18/9. The second attempt saves toward the next chosen upgrade rather than receiving a free purchase.
- QA wall-clock cap may be240rendered frames/s, while gameplay delta remains1/30s and physics step0.02s. Native inspection confirmed those values, ordinary HP and540×1170. After the first three runs, the temporary GameView was reduced to360×780 (same aspect) for faster collection; run verification records actual dimensions. This changes QA presentation speed only, not the player build, gameplay delta, movement input or damage. It is not a device FPS measurement. Final verification restores the original1080×2340and render cap.
- `CampaignSave` and the cohort runner now accept a task-specific snapshot folder so a follow-up cannot overwrite newer user progress with an older task snapshot.

## RestStop bypass correction preparation

- Runtime/source review confirms RestStop can pass live opponents at the outside lanes; the prior clear leaves both last-row opponents alive. Reward reduction alone would not make growth necessary for a player taking that bypass. The user was informed that visible encounter passages will be extended to RestStop before its30-attempt calibration.
- Prepared an opt-in world-frame constraint in `tmp/later-chapters-30-2026-09-23/HighwayEncounterLanes.cs`, tests in the same folder, and native installer/probe tools. Do not import the runtime/test drafts until the live Highway cohort is finished. Highway's continuous-road math is retained; Noryangjin has no manager. Both manual and forward movement will use the RestStop constraint, with parallel/opposite/elevated roads ignored.
- Food-hall outdoor rows11/12are explicitly disabled in the workbook and must stay wide with their rails hidden. All25row references remain editable;23normal active passages are expected. Source layout hazards start at least22.2m after their paired enemy, beyond the6m funnel exit. Native saved-scene spacing/contact tests will verify installation.
- Draft workbook`r7-reststop-30` (2600HP/800attack/150coins) and`r8-reststop-passages-30` (existing combat/150coins) were prepared but not installed or tested. Start the new mandatory-contact baseline with the currently installed2000HP/650attack/350coins before deciding whether a reward reduction is needed; forcing all physical encounters changes both damage and kill income.
- GameView was temporarily maximized to avoid drawing the Scene view during QA. Original maximized state is saved and the final verifier restores it. Snapshot completeness and resume save-root mismatch guards were added during review.

## Second Highway candidate and installed RestStop passages

- Trial1 stopped after34Highway attempts with no clear. Attempts26/29/31/32/33/34reach285s and die to the final boss; early trials also plateau at100s. The old19-attempt clear had six HP-percent pickups, while several late failures had zero or two. Raw permanent levels and coin ratios alone do not predict a stable attempt target.
- Installed revision`r9-highway-smooth-30`: first normal HP540→500, boss HP multiplier2.6→2.0, enemy/progress rewards65→70. Growth7.5%, elite1.5, attacks, duration, chapter1and RestStop economy stay unchanged. Hash`f53ccdefa500bf9309190e04fb64eb947d4da55d806bd6156744ff9c22a38972`; schema/archive verified. Earlier draftsR7/R8were not installed.
- Imported the world-frame constraint and tests after trial1stopped. Installed25shared-mesh RestStop passage definitions,23active outside the food hall. The player common position path covers forward motion and lateral input; stationary defense and discrete turns retain their behavior. Source contact realignment now preserves existing rail references.
-233native regressions pass (including11new passage cases and28workbook/assignment cases); both C#builds and harness pass. Physical left/center/right first-row approaches all reduce ordinary500HPto0viaEnemyContact, at lanes−1.85/0/+1.85. These fixtures suppress outgoing fire to isolate contact, with no HP override, and are not balance runs. A native frame visually confirms rails and actor/capsule spacing.
- Active cohort`r10-later-chapters-30` carries only unchanged accepted chapter1records/state30, then new Highway runs with the second candidate. CLI uses `--save-root tmp/later-chapters-30-2026-09-23 --render-cap 240`. Stop after Highway first clear before starting the RestStop baseline.

## Highway third candidate

- R10paused after global48to preserve QA throughput and complete an authoring review correction. GameView maximization now occurs in Edit Mode before each Play; Unity had undone changes made during Play on exit. Gameplay cadence stays1/30s, physics0.02s.
- `ProjectRoadCenter` now provides canonical road forward through a compatible overload. Both RestStop authoring tools use it. Reverse-facing and saved-scene tests pass; reinstall corrected zero current headings. Total focused native cases234; source and actor capsule dimensions unchanged.
- R10Highway first clear: global55,25chapter attempts,298.57s,42350earned,38850spent,bank4125. This is too early for the working28–32target, so it remains a calibration cohort.
- The reward estimator reconstructs every observed actual purchase order before interpolating income by the fixed upgrade path. Using R10data,58coins predicts30attempts; this is explicitly an estimate, not a native result.
- Installed`r10-highway-30-final`: same500normal HP and2.0boss multiplier, enemy/progress reward58. Hash`282948ba7eee374846fcfd3d2fdbc2f1ac7960e4bb40d8097999d786aa4396c9`; schema/archive verified. Active cohort`r11-highway-final-30` starts new Highway attempts from the same real chapter1checkpoint. Current session is in the latest tool output/store. Still require an actual qualifying Highway clear, then RestStop calibration with carried earnings.

## Highway reward bracket

- R11reward58ended33Highway attempts without a clear and was stopped after global63. Planning interpolation was too optimistic for this seed-dependent income/HP-bonus path; it is not substituted for native validation.
- Current candidate`r11-highway-62` keeps500normal HP/boss2.0and sets enemy/progress rewards62. Hash`b70ae92bc2c4e2ef7757b13a7100dca00ae1d10d170e7335631fc6d0bd5fc62d`; schema/archive verified. Active cohort`r12-highway-62` again carries only the unchanged chapter1checkpoint. Prior70reward cleared25,58did not clear33, so this candidate tests the middle of the observed bracket.
- No RestStop balance cohort has started yet. Its required23passages and234native checks/three physical probes are complete. Use the accepted new Highway exit when available; RestStop still has its original350rewards and2000HP/650attack baseline for that measurement.

## Final boss margin refinement

- R12reward62first cleared Highway on global63:33chapter attempts. Global62(the32nd)left the struck final boss with339HPafter fatal contact. Earlier boss-reaching failures left at least1445HP. The goal remains about30, with a28–32reference-cohort working window.
- Candidate`r12-highway-boss185` changes only balanceC37and enemy HP cachesO101/O102: multiplier2→1.85, final boss HP5673/6524→5247/6034. Hash`1cc3c12685f8277f13e46ba51599cae0525fd700b96f240c1cde2d546b8533e4`; all other cells, formulas, prices, rewards and combat values unchanged.
- Reuse is limited to unaffected early attempts. Global31–53all died before210seconds. A native road-distance audit of5214recorded positions found minimum617.9985m separation from the changed boss; their maximum ordinary projectile range was65.6m, with an additional100m activation/aiming margin. All purchased missile-duration levels are0. The source/candidate cell diff and range proof are retained in`boss-prefix.json`and`.verified.json`under the task snapshot folder. The first JsonUtility draft could not deserialize the dynamic script DTO; the verified run uses the existing assembly-qualified Newtonsoft reflection path.
- `r13-highway-boss-margin` carries this invariant prefix through global53, then reruns every boss-reaching attempt from54with the revised boss health and the exact saved wallet/upgrades. This is targeted causal replay, not a claim that the reused prefix was played again. Active session is in the tool output/store. No RestStop calibration has started yet.

## Highway accepted; RestStop now active

- R13first cleared Highway on global62:32chapter attempts at298.57seconds,254.07HPremaining,bank3712. Attempts24–31were natively replayed and remained deaths, with matching earned coins/purchases. Full wallet reconciliation passes. Enemy/progress reward62, first normal HP500, boss multiplier1.85are now held fixed.
- Active`r14-reststop-required-baseline` carries the accepted first62records and actual state62, then begins new RestStop attempts atglobal63. Existing2000HP/650attack/350rewards are the baseline with23required passages. Run to a real clear and tune only from measured results; do not infer a30-attempt result from the old7-attempt open-road cohort.

## Paused for the user's opening-video replacement request

- User redirected work to replacing all four opening clips with supplied Best4 v5 files. Balance objective remains incomplete. R14 has 97 completed records (35 RestStop deaths); run98 was interrupted at270.07s and is not a completed attempt. Resume from saved state97, preserving the partial run98 as excluded evidence before replay.
- The identified Python driver was stopped after the Editor had already left Play Mode. Original77preferences and Game view index23 were restored and verified: coin1781, jewel0, timeScale1, captureDeltaTime0. The current workbook/archive hash remains`1cc3c12685f8277f13e46ba51599cae0525fd700b96f240c1cde2d546b8533e4`.
- RestStop still needs calibration: required passages plus late projectile activation may be contributing to the final deaths. Inspect actual workbook activation leads16/12m and corridor narrowing before further tuning. Core upgrades have reached60/60/30; do not claim a30-attempt RestStop clear.
