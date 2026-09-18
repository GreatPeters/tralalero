---
title: Curved Korean highway and rest-stop interior holdout
date: 2026-09-13
status: completed
type: feat
---

# Road chapter pattern revision

## Problem and requirements

The existing Highway has five paused right-angle corners separated by long straight runs. RestStop changes scenery but still plays like the same forward runner. The user explicitly requests planning followed by implementation: Korean highway bends, exits and green guidance, oncoming cars, entering a rest-stop building, a stationary 30-second fight against police from all sides, and additional coherent patterns.

## Selected design

- Highway keeps its start/end and chapter progression but replaces abrupt corners with continuous curves. Two signed forks offer a green recovery bypass and a pink main carriageway; both reconnect. Commit the route from the player's lane at the split, clearly show the selected destination, and keep lateral steering available on either route.
- Oncoming cars use visible advance lane warnings and fixed trajectories. Introduce one car, then a staggered pair, then a convoy gap. Never fill all three lanes simultaneously. Cars cannot track the player's last-second dodge. Existing crossing traffic and toll gates remain distinct beats.
- RestStop runs through a new food hall. At its center the player stops translating and the camera pulls up to show four entrances. Automatic aiming targets approaching police while the route/camera frame stays stable. Three ten-second phases alternate doors, attack opposite doors, then surround from all four. At 30 seconds the rear shutter opens and running resumes.
- Existing cartoon police, repaired vehicles, tables, chairs, kiosks and vending machines are reused. The food hall is an authored open-roof interior with doorway gaps, counters and a clear central fighting floor. No new model-generation dependency is needed for this revision.
- Additional variety is meaningful and bounded: healing bypass versus combat mainline, staggered traffic, convoy gaps, shutter-controlled wave directions and a visible timed escape.

## Assumptions and boundaries

- The 30-second holdout counts as play time. RestStop's forward portions are accelerated slightly so its overall target remains approximately 300 seconds. This is a pacing target, not an exact guarantee for every upgrade/input policy.
- Chapters 1, 4 and 5 are outside this revision. Preserve wallet, purchased upgrades, skins, videos and chapter travel.
- Keep all existing placement IDs and their workbook-owned stats. Author geometry locally; adapt curved-road activation positions to the actual road rather than a long tangent outside a bend.
- New pattern parameters belong in the canonical balance workbook and use bounded defaults when unavailable. Preserve signed runtime data and deduplicated workbook styles.
- Testing uses fresh preference backups and the isolated QA project. Never treat an endurance/section probe as evidence of ordinary progression difficulty.

## References and existing contracts

- Ministry of Land, Infrastructure and Transport, [colored road guidance manual](https://www.molit.go.kr/USR/policyData/m_34681/dtl.jsp?id=4366): use colored lines together with arrows/destination signs, not color alone. Game routes are stylized references, not a reproduction of a real interchange.
- `Assets/ShooterSurvival/Scripts/Player/PlayerScript.cs`: route-relative steering, forward motion, projectile route-turn deltas and reset lifecycle.
- `Assets/ShooterSurvival/Scripts/Game/EncounterPlacementController.cs`: run-start settings snapshot and supported activation placement.
- `Assets/ShooterSurvival/Scripts/Enemy/EnemyEventController.cs`: locomotion/attack/death contract; `EnemyScript_space` owns real projectile damage and enemy-health contact exchange.
- `docs/solutions/design-patterns/separate-slope-pitch-from-paused-route-corners-2026-09-05.md` and `validate-player-choice-and-post-turn-response-space-2026-09-09.md`: distinct motion ownership, supported paths and reaction space.
- `docs/solutions/workflow-issues/verify-runtime-presentation-and-progression-beyond-numeric-checks-2026-09-10.md`: real frame cadence, real collision tests and preserved user state.

## Implementation units

### U1. Continuous route and signed forks

Files: new `Scripts/Game/HighwayRoute.cs`, modify `Scripts/Player/PlayerScript.cs` and `Scripts/Game/EncounterPlacementController.cs`; test `Assets/Tests/Editor/RoadChapterPatternTests.cs`.

Store the authored sampled route, advance with ordinary forward speed and retain lane-relative input. A fork owns exactly one decision per run and rejoins without a lane snap. Reset restores branch choice and route distance. Character/camera/projectile rotation follows the smooth route using the existing rotation path.

Verify center/edge road support, curve continuity, left/right branch selection and rejoin, run restart, pause, death, no changes to Noryangjin movement, and activation gates on curved roads.

### U2. Oncoming traffic patterns

Files: new `Scripts/Obstackle/HighwayOncomingTraffic.cs`, shared `Scripts/UI/ChapterPatternHUD.cs`; test `Assets/Tests/Editor/RoadChapterPatternTests.cs`.

Use preauthored reusable car instances and bounded schedules. Warn before launch, lock lane, sweep the travelled segment for contact so fast cars do not tunnel, and damage once per car. Stop all pattern time while gameplay is paused. Branch bypass remains physically outside the mainline traffic.

Verify warning duration, one surviving lane, fixed path after launch, repeated-contact protection, pause/retry/disable cleanup, and no traffic/gate overlap that removes all escape options.

### U3. Food-hall holdout

Files: new `Scripts/Game/RestStopHoldout.cs`, modify `Scripts/Player/PlayerScript.cs`, `Scripts/Weapon/WeaponScript.cs`, `Scripts/Game/ChapterProgression.cs`; test `Assets/Tests/Editor/RoadChapterPatternTests.cs`.

Use explicit owner-scoped movement locking, without stopping combat or run time. Aim only new shots at living police, leaving airborne bullets on their launched trajectory. Preauthor a bounded inactive police pool, use the existing enemy movement/animation/damage paths, and restore camera/visual/input state on success, death, disable or restart. Shutters and door warning lights explain the three waves. Preserve the rear exit and normal chapter victory.

Verify fixed player position with active firing, targets in all four directions, 30 simulation seconds excluding pause, bounded population, successful damage/death/reuse, one completion reward, and full movement/camera restoration on every exit path.

### U4. Native scene authoring and balance

Files: new `Editor/RoadChapterPatternBuilder.cs`, focused tools under `tools/`, HighWay/RestStop scenes, canonical `GameData/Editor/Data.xlsx` and signed `Resources/GameData/Data.bytes`.

Back up the current scenes before focused native authoring. Remap existing highway content to the curved route, replace only roads/turn spots and add fork scenery. Keep placement IDs. Clear the hall's space by relocating overlapping scenery/encounters outside its approach/exit safety envelope. Add readable Korean signs and the current CC0 TMP font. Put new traffic and holdout knobs into the workbook through a new guarded candidate based on the current canonical file.

Verify retained placement counts/IDs, no missing references, support for every lane and both forks, real doors/interior camera sightlines, original data/signature validation, and scene save/reopen.

### U5. Play and review

Files: focused QA probe plus `Editor/Sr18ProgressionPlaytest.cs` only as needed; evidence under `map-concepts/road-patterns-2026-09-13/`; execution status under `docs/exec-plans/active/road-patterns-2026-09-13.md`.

Run native targeted tests and three directed play/review cycles: geometry and branch motion; traffic collision/fairness; complete interior entry, 30-second combat and return to running. Include normal-stat continuous attempts and label any section setup. Inspect actual composited screenshots. Fix observed issues, record practical limits, update architecture/reliability and compound reusable lessons before completion.

## Sequencing

U1 and U3 motion contracts precede native authoring; U2 and U3 feed U4. Apply workbook candidate after its parser checks, then run U5 against saved scenes. Work sequentially in the shared checkout and preserve unrelated edits.
