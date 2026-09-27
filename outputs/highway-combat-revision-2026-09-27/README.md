# Highway compulsory-combat revision — 2026-09-27

The latest user screenshot request supersedes the earlier always-open traffic lane, fast oncoming open-road cars and toll popup. HighWay only; source/scene/workbook backups are under before/. No commits, no paid Meshy generation and no key access.

## Implemented

-351enemy placements versus233(+50.6%), arranged as105ordinary combat groups plus accident groups and the12-car police/tow event. Full front rows require a destroyed member to open a passage. Admission and shared braking preserve formations; actual collider poses guard against curve-induced overlap.
- Vehicle labels require a recognizable body in the gameplay camera, a visible viewport footprint and an unobstructed view. Predictive EnemyContactWarning text is excluded from vehicle actors. Actual collision damage still uses the existing health transaction.
- Toll rewards are two physical trigger pickups. Touching one selects that exit and grants exactly one unique bonus. Missing both stays on the main exit without awarding a bonus. The toll popup is removed; the first branch popup remains.
- The toll canopy, lane signs, lights, booth and pickups are rebuilt. Random gates use substantial navy/gold frames, stars, lamps, raised question panels and scene-owned sparkles. Korean police use the existing white sedan with projected blue/yellow livery, Korean lettering and lightbars.
- Log length3.4→5.1m. Its real oriented collider now drives damage instead of a broad distance square.
- Open route player speed7.8→11.7, with screen-edge streaks. Both revert after the segment or chapter disable/death. Cars in that route approach at5m/s.
- Ordinary bonus A/B concepts were generated with the built-in image tool. They are proposals, not an applied ordinary-bonus redesign. See concepts/README.md and the public concepts.html.

## Balance and verification decisions

Compulsory moving rows have a shorter firing opportunity than parked accidents. Workbook exposure settings are1.6seconds for ordinary moving rows,1.1for the rush route and1.2for the police/tow event; parked accident health retains3.8and the requested type multipliers. The weak lane changes every three rows. All values and health formulas live in Data.xlsx; other chapter rows/caches remain preserved.

play-v1 demonstrated real curve overlap and an overly expensive shooting window. World-pose clearance fixed the overlap. play-v2/v3 exposed a validation-driver mistake: it selected only living cars and abandoned already-open lanes. The driver now reconstructs the row's dead slots, retaining open corridors. Those failed attempts remain separate from accepted evidence. No test-power, invulnerability, health pinning or collision disabling is used in normal route acceptance.

The first shader test incorrectly inspected a material property that the shipped TMP shader does not declare. Source inspection showed that CanvasRenderer supplies unity_GUIZTestMode at draw time. The corrected contract uses a world-space, non-overlay canvas; behavioral visibility is checked through camera/occlusion logic and directed native probes.

Do not interpret complete.json (driver finished) as a chapter clear; result.json.outcome is authoritative.

## Final acceptance

| Scenario | Evidence | Result |
|---|---|---|
| First entry | play-v4/first-accident | Pass, HP960 / ATT48, no damage |
| Jam → Hi-pass | play-v4/jam-hipass | Clear298.3046s;171cars observed, up55.5%from110 |
| Open → Cash | play-v5/open-cash | Clear268.3038s;153cars observed, up48.5%from103 |
| No-fire weaving | no-fire-v1/open-cash | Death72.7966s at617.78m;0projectiles fired/0projectile hits |

Both full clears use initial HP3300/ATT88, ordinary input and enabled collisions. No vehicle overlaps, direction errors, body-culling anomalies, labels without eligible bodies, predictive warning components or damage events without an overlapping vehicle were recorded. Rush reached11.7and returned to7.8with the streaks off. Toll rewards were physically collected around1934.9m, with only the first branch popup counted.

contracts.json passes16directed assertions: near-miss0damage, true contact exactly123HPonce, visible/occluded/off-screen label behavior, warning exclusion, both physical exit choices and rejection of remote/double collection, rush multiplier and disable cleanup. These fixtures are not additional full runs.

82focused test cases pass. The last asynchronous GameDataWorkbookTests rerun stalled and was cancelled; its15existing NUnit methods were then directly invoked and all passed (tests/GameDataWorkbookTests-direct.json). Earlier failed attempts are retained and not counted as clears. Runtime/Editor builds and harness checks pass; the existing SplineSpeed.m_LastIndex warning remains.

All cohorts restored their preferences. original-restored.json additionally verifies the task-wide original bytes, coin37113/jewel0, the original HighWay playModeStartScene, timeScale1and a clean saved scene. RestStop and three Noryangjin scenes match their before hashes. No paid Meshy generation or commit.

Final visual review moved the toll's left supports/bollards onto the median centre(-7.62m), keeping them out of northbound traffic. This is a support-only cosmetic correction; pickup volumes and combat were unchanged. toll-final.png is explicitly a directed native layout capture at1888m, not an additional route clear; toll-visual-v1/restored.json confirms restoration afterward. The served report was opened and visually checked in Chrome; scene switches and image loading passed. Older report pages now link prominently to the latest version to avoid confusing archived captures with current gameplay.

## Reproduce

1. tools/apply-highway-chapter2-workbook.py --apply.
2. Official Unity run_script: InstallHighwayChapter2.Actors (or RefreshActors for a fresh visual rebuild).
3. RefineHighwayCombatPresentation.Apply, then InstallHighwayChapter2.CaptureRoadCards and Verify.
4. Regenerate Data.bytes via GameDataWorkbookEditor.EnsureRuntimeArchiveCurrent(false).
5. Build Runtime/Editor, run focused tests through tools/run-highway-chapter2-checks.ps1, then isolated normal-stat play/probes using tools/reststop-korean-playtest.cs and tools/probe-highway-combat.cs.

Always inspect the inner Pipeline success result before a dependent operation. A test completion acknowledgment can precede final Editor state cleanup; verify clean Edit Mode before snapshotting/entering Play. An unintended lobby entry was stopped and the original task snapshot restored byte-identically before continuing.

Final public report: http://127.0.0.1:8776/highway/combat-revision/. Preference snapshots and raw private state are never copied there. Human difficulty, earned-purchase economy and phone performance remain outside automated acceptance.
