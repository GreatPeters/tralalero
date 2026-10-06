# Chapters 1–5 composition revision — 2026-10-05

Status: applied and verified in Windows Unity Editor. Final preservation audit passed. Device/APK validation remains deferred.

## Reference and scope

The user supplied Desktop `1.jpg` and `2.jpg`, both 1080×2340. Both were visually inspected; hash-identical evidence copies are in `outputs/composition-2026-10-05/references`. They show a low scooter-rider view, close corner/linear storefront perspective, a lower-center foreground anchor and a distant destination. Exact camera height or lens cannot be inferred from those images. The implementation translates that composition to the existing third-person shark while preserving the established toon art and each chapter's identity.

This continues the existing project. It does not replace the game or reproduce the photographic neighborhood, scooter, social UI or brands. The earlier Library materialization failure is resolved by the user's local files; no metadata bypass was used. Latest user authorization permits necessary model generation, but the observed gaps were resolved using existing scene art and simple authored geometry: **AI model generations 0, paid calls 0, new material/mesh assets 0**.

## Applied changes

| Scene/mode | Yaw-relative follow offset, m | Downward pitch | Vertical FOV |
| --- | --- | --- | --- |
| Ch1 outdoors | (0, 4.4, -13) | 4° | 52° |
| Ch1 market/cold auction | (0, 4.6, -14) | 7° | 52° |
| Ch2 highway | (0, 5.4, -16) | 6° | 55° |
| Ch3 rest stop | (0, 4.4, -13) | 4° | 54° |
| Ch4 Jamsil | (0, 4.4, -13) | 3° | 52° |
| Ch5 ordinary interior | (0, 4.2, -12.5) | 7° | 56° |

`NoryangjinMarketAtmosphere` now exposes its indoor offset/pitch with historical defaults. The Revamp scene opts into the new indoor profile. Existing ramp-height limits remain relative to the configured height. Existing 360° rest-stop/cinema defense cameras and the escalator camera are retained; they restore ordinary follow afterward.

Five existing decoration-only Jamsil lakeside gardens, around 580–772m, overlapped the selected left branch. The old-camera reconstruction also hid the shark there; the lower camera amplified the defect. Moving those five roots 10m toward the lake clears the 8m road, benches and pedestrian strip. The other eight gardens, all meshes/materials and collision/movement paths are unchanged. Corrected fixed-pose and actual 675–724m frames show the player, enemies and center firing corridor.

Native review also found incomplete elevator framing: open sky around a moving floor and one rear pane. Each of five existing elevators now has an optional closed travel interior made from 18 native cube renderers with existing toon materials, no Collider and no new model asset. It only appears while riding. The close transfer view uses offset (0, 4.6, -7), looks toward player height 1.5m and follows vertical movement without lag. Landing/interruption restores the exact prior camera. Occluded floor scenery is hidden during enclosed travel; original exterior renderer flags are captured/restored to avoid rear glass crossing the interior. Duration, route endpoints, combat, reward and lift eligibility rules are unchanged.

The RestStop `RST_G09/Sign` panel sat at 4.8m and crossed the new camera at close range, obscuring the forward view in actual 1080×2592 gameplay. Its decoration-only Sign root is raised 2.2m, leaving the gate lanes, hazard components, colliders, text, dimensions and materials unchanged. The panel's bottom is now 6.4m.

Independent review found the visible highway road ending about 30m beyond the authored route. A same-pose reconstruction confirms it also existed with the old camera. A decoration-only extension from world X490 to850 uses 45 renderers sharing five existing 40m road, opposite-lane, white/yellow paint and median meshes. It adds no Collider, behavior, mesh asset or material, and does not change the route length, completion trigger or rewards. The first apply helper rejected a source road's existing MeshCollider before saving; the corrected helper copies only MeshFilter/MeshRenderer to new objects and asserts zero added colliders. The before evidence and failed helper record are not counted as gameplay passes.

## Evidence interpretation

- `before-saved` / `after-v1`: 42 fixed-pose world frames per version. Ch4 center samples at 600/850m were between the selected branches and are excluded from selected-route acceptance. Correct branch fixtures explicitly choose a branch and use the director's current-deck sampling.
- `aspects-v1`: 64 HUD+world frame pairs across 1080×1920, 1080×2340, 1080×2592 and 1080×1440. Actual PNG dimensions verified. These are directed poses, not full combat runs at every ratio.
- Actual route suites drive the existing bounded movement method and ordinary firing. They do not teleport the player, pin HP, disable hazards, force victory or buy upgrades. Ch4/5 use ordinary 60HP/8ATT starts; Ch1/3 use the existing clear-growth cohort 37/46/30 and Ch2 the workbook clear cohort 20/24/10. Growth prefs are restored afterward. These are mechanics/visibility checks, not baseline-player or human-balance certification.
- The synthetic cabin abort fixture checks exact camera restoration and inactive cabin properties only. It is not a normal indoor recovery screenshot.
- The v3 cabin fixture showed a glass crossing at 5F→6F, while actual late-transfer screenshots and renderer probes showed both relevant rear panes hidden. The fixture had skipped settling current-floor visibility before transfer. V4 reproduces the actual visibility order; its result is recorded separately from natural route completion.
- Reward observer v1 included distant portals on parallel roads and could consume a screenshot key before the real approach. Those images/AABB values are excluded as visual proof. V2 filters lateral distance, records renderer visibility and takes closer-spaced native approach frames.

Independent read-only review was performed by `/root/composition23_visual_review` on the two references, camera candidates, corrected branch/HUD frames, representative chapter pairs, aspect frames, actual street/choice footage, transfer frames and rest-stop reward approaches. Defects were evaluated from native pixels, not AABBs alone.

## Practical limits

The low camera intentionally lets the Ch4 tower crown pass behind the top HUD or out of frame on close approach. Ch2 retains a smaller player and greater road breadth; Ch3's low buildings leave more sky than the dense street reference. Near the selected side of a reward pair, the far-side world label can leave the viewport; pre-decision readability is checked from actual approach frames, not inferred from that close crop alone.

The 3:4 status AttackLabel reports TMP text-rectangle overflow, but the native glyphs and value remain inside the outer card in reviewed frames. No UI rebuild was performed based solely on the flag.

This is Windows Unity Editor validation with capture/observer overhead. It does not certify phone performance, notches, human difficulty, unlimited-duration memory behavior or every moment of every aspect ratio. Portrait orientation remains authored; autorotation is off. APK/device work remains deferred. The full 1135-test suite was not rerun, and the historical 52 failures are not counted as current passes.

No commit, push, external backup, purchase, new authentication, installation, engine-session registry write or user-file deletion was performed.


## Independent review qualifications

The final native review confirms G09 forward visibility, B10's two companion bonuses in an earlier approach frame, B13's `attack +13% / attack +14` in near32/28/24/20 frames, and the V4 cabin arriving/landing views. Near16 clips the far B13 value; the review demonstrates a pre-selection reading window, not continuous legibility. A gate arm briefly overlaps one B10 value closer to the gate. The Highway ticker's full chain-explosion sentence was not certified from still captures. These UI behaviors were not changed in this camera revision.

Highway initial checks report 3 jam-cash and 5 open-hipass mandatory-wall heuristic frames. These broad projections are not a native collision oracle; the earlier quality22 investigation remains separate. Both actual routes clear with wrongDirection=0, overlapFrames=0 and hiddenBodyFrames=0. No new human-fairness or mandatory-collision guarantee is claimed.

The first final inventory assertion rejected 32 Noryangjin Canvas rotations. All were independently resolved to unchanged active `WallStatCanvasBillboard` components with `[ExecuteAlways]`; the source faces the current camera during Edit. The native saved-scene diff contains only the intended camera and indoor-profile edits, with no billboard-transform serialization changes. The audit separately identifies these camera-derived in-memory rotations instead of silently excluding arbitrary Canvas transforms.


## Actual Play results

| Run | Result | Conditions |
| --- | --- | --- |
| Ch4 left -> Ch5 B1, Ch4 right -> Ch5 cinema | 4 clears; 44 checks passed, 0 failed, 0 errors | 1240.13 wall seconds; basic 60HP / 8ATT |
| Tower both paths after cabin addition | 2 clears; 24 checks passed, 0 failed, 0 errors | 591.09 wall seconds; basic 60HP / 8ATT |
| Tower cinema after final camera/exterior polish | 1 clears; 13 checks passed, 0 failed, 0 errors | 322.80 wall seconds; basic 60HP / 8ATT |
| Ch1 inside | route=0 elapsed=298.3 hp=3265/13030 completed=True cause=EnemyContact branch=True/False/True | Clear-growth fixture 37/46/30 |
| Ch1 outside | route=1 elapsed=297.2 hp=3134/11330 completed=True cause=EnemyContact branch=True/True/True | Clear-growth fixture 37/46/30 |
| Ch2 jam-cash | clear 298.30s; HP 1137.13; wrong direction / overlap / hidden body = 0 | Workbook clear-growth fixture 20/24/10; initial 3300HP / 88ATT |
| Ch2 open-hipass | clear 268.30s; HP 2844.16; wrong direction / overlap / hidden body = 0 | Workbook clear-growth fixture 20/24/10; initial 3300HP / 88ATT |
| Ch2 open-hipass after end-road finish | clear 268.31s; HP 2844.16; runtime errors 0 | Same clear-growth fixture; ordinary full route |
| Ch3 final tall aspect | clear 162.49s; 30s holdout, 23 spawned / 22 killed; aim quadrants 0–3 | 1080×2592; clear-growth 37/46/30; HP 6810.08/11380 |

Ch1 inside/outside and both Ch3 passes have zero settled roof visibility mismatches. All eight original observers and the added end-road observer have empty error logs. V4 directed cabin fixtures produce 25 frames across five elevators; every landing/abort restores the saved camera and hides the optional cabin. The 81 Ch4/5 checks span progressive implementations (44+24+13), not 81 independent tests of every final scene. Latest cabin properties and final cinema route were rechecked after polish.

## Preservation and deliverables

The audit checks 13645 dependency files/metas (13637 exact after recovery) and 622 code/settings files (619 exact). Only five scenes and the three named sources differ for this revision. 2 shared dynamic font/decal files were restored from this session's verified baseline; their after-Play bytes were retained in `final/shared-after-play`. Existing colliders, gameplay components, protected transforms, material definitions and lighting were checked, with explicitly listed camera fields, closed travel-cabin references, decoration-only placement and derived billboard rotations.

All78 game prefs,9 Editor session settings, locale, original scene/start scene and GameView resolution, dock-relative size and zoom were restored. The original minimized/off-screen window origin differs from the now foreground Unity window; its current native placement was left undisturbed. Attribution of that window-origin change is unproven and screen-position equality is not claimed. The active preservation guard was released only after the checks. Branch/head and all127 pre-existing tracked deletions are preserved. Native registry was read for comparison only; Unity-generated session counters are recorded separately and were never written by the scripts.

Evidence: `outputs/composition-2026-10-05/verification-summary.json`, `highway-end-verification.json`, `final/summary.json`, `final/inventory-comparison.json`, the actual-route directories and PNGs. Gallery: http://127.0.0.1:1626/composition-20261005/ . This pass generated no AI model, used no paid call and added no mesh/material assets; it reused prior authored/generated art, existing road meshes and materials, plus native cube cabin pieces.
