# Highway screenshot follow-up — 2026-09-27

The user's vehicle/fork corrections are saved in HighWay. Backups are under before/. Existing unrelated working-copy changes are retained. No commit or paid model generation. [Native screenshots and comparison](http://127.0.0.1:8776/highway/fixes/).

## Changes and findings

- Removed the procedural accident deformation, which compressed the nose and displaced front-wheel vertices. The original sedan now keeps intact wheels/chassis. Its sideways pose, triangle, lamps and smoke communicate the accident.
- Replaced crossing fork paint with clipping against the shared carriageway. The first branch now separates with a smooth220mtransition and holds its own carriageway; left-green/right-red guidance follows both forks. The latest user request supersedes the earlier guide prohibition.
- Added eight early placements, for233total, without renumbering225existing placement IDs. Existing HP/rewards stay unchanged.
- Advanced activation1.5seconds and compensated moving-car starting stations by speed×1.5. traffic-timing-audit.json verifies the nominal original encounter timings (error below0.000001m).
- Native play then exposed a second visibility owner: disabled EnemyEventController still resolved runtime references and installed the pedestrian75m EnemyVisualDistance culler. Bodies vanished while their HP labels stayed visible. Highway vehicles now keep visibility under their chapter controller; other chapters retain the pedestrian culler.
- Added mint compact and black SUV silhouettes from the same Meshy library. Colliders/footprints are rebuilt from their actual geometry. Wreck pools distinguish model variants. No new Meshy generation was necessary;0credits spent.
- Raised the vehicle support pose0.08m. A large road burial was not reproduced by the before sweep: ordinary sampled tire vertices reached about−0.019m, the deformed accident body−0.076m. The corrected body/support sweep has no sampled penetration. This geometric check does not establish one cause for every possible screenshot.
- Straight sound-wall panels sometimes crossed the curved road edge. Native ray checks move affected wall groups outside all road surfaces.
- Fork guides now continue through the selected road and merge, ending30mafter the merge. The road uses its own non-outline asphalt material and disables surface shadow casting, removing black internal overlap seams without changing the shared material in other scenes. Native play-v3 captures verify this cosmetic finish.

## Verification

Runtime/Editor builds pass (one pre-existing SplineSpeed CS0649 runtime warning). Focused receipts live in tests/. The stale225-car road assertion was updated to233. An initial180mbranch transition failed the existing turn/step continuity contract; the220mtransition passes it. At the authored test stations its worst one-metre heading change is5.31degrees and greatest centreline step1.792m.47sound-wall groups were moved out of road surfaces (maximum13m), and the owned scene was saved after the full pass. Workbook audit preserves18unrelated ZIP parts and296other-chapter rows.

play-v1 passed the first-entry accident atHP960/ATT48without damage. It was deliberately stopped during jam/hi-pass when the legacy culler was discovered, and its preferences were restored byte-identically. It is not a completed-route result.

Final play-v2 results:

| Scenario | Initial stats | Outcome |
|---|---|---|
| First-entry accident | HP960 / ATT48 | Passed35.77s, no damage,17cars encountered |
| Jam → Hi-pass | HP3300 / ATT88 | Clear298.30s,110cars encountered |
| Open → Cash | HP3300 / ATT88 | Clear298.30s,103cars encountered |

All three record0wrong-direction/overlap/hidden-body/moving-wall frames. Both full paths encounter two popups, two random gates and one log. Seventy-two focused tests pass. The seven MobileCombatFeedback tests retain the other-chapter pedestrian distance-culling contract. These are automated normal-input/collision runs, not human difficulty or earned-purchase economy tests.

play-v3 is a96-second normal-input visual review through750m after the purely cosmetic asphalt/guide finish; its outcome is explicitly visual-range-covered, not a full clear. The saved route geometry, vehicle data and combat code match the successful play-v2 runs. All three cohorts restored the user's original preferences byte-identically. Final restoration preserves coin37113, jewel0, the original HighWay playModeStartScene and timeScale1; the scene is clean. Four protected RestStop/Noryangjin scene hashes match the before state. Final-state.json records acceptance.

The served page contains only selected native images and non-sensitive result summaries. It was verified through DOM/image loading/button switching, with a rendered narrow-layout screenshot (effective width520px), and the viewport override was reset. Original-resolution PNGs are retained separately under tmp/image-previews/highway-visual-fixes-2026-09-27 without overwriting earlier previews.

## Reproduce

Use tools/apply-highway-chapter2-workbook.py --apply, recompile Unity, then official run_script with tools/install-highway-chapter2.cs: RefreshActors (only when rebuilding silhouettes), RefreshForkGeometry, RefreshForkPaint, Actors, Scenery, ClearRoadsideWalls, RefineRoadSurfaces, CaptureRoadCards and Verify. Regenerate protected Data.bytes through GameDataWorkbookEditor.EnsureRuntimeArchiveCurrent(false). Native Unity APIs author the scene and mesh assets; no scene/asset YAML edits.

The previous chapter2 report is historical. Use this follow-up's captures for the current vehicles/forks. Human win rate, long-term earned-currency balance and physical phone performance are not certified by automated route tests.
