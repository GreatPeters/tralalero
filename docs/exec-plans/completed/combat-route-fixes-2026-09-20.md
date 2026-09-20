# Combat / route feedback fixes

Status: completed 2026-09-20. All nine corrections are saved. [Implementation and evidence](../../../map-concepts/combat-route-fixes-2026-09-20/README.md).

User-authorized implementation of nine corrections. Preserve earlier talisman work and the initial dirty SR18 scene state; disk and unsaved snapshots are under `map-concepts/combat-route-fixes-2026-09-20/before/`.

1. Guard: valid shooting pose, muzzle, Arrow2 model-axis alignment and firing direction.
2. FatMan: stationary repeating throw pose, activation about five seconds of player travel away, authored straight-ahead throw vector.
3. Woman boss: one centered actor per encounter, no duplicate side boss.
4. Tier health: ensure Elite/Boss health remains higher through workbook/placement/runtime application and contact calculations.
5. Uphill/downhill: keep traversed/front road visible and keep camera above the relevant deck.
6. Tungtung: reliable enemy contact using reciprocal current-health exchange; high-health survivor retains remainder, equal health kills both.
7. Boombardino: owned shots inherit player's incremental route-turn delta like player shots.
8. Formation: center left/right placements on their actual route row with stable spacing; keep movement targets aligned.
9. Ship: appropriate visible scale/facing, CannonBall at FirePos, activation at roughly 7.5 seconds of travel distance.

Verification: focused tests plus native action/trajectory/contact/slope probes. Author scenes/prefabs through official Unity CLI/Pipeline; record source/destination axes and gate distances. No additional permission stop is needed for the requested fixes. Current branch `fix/combat-route-feedback` carries existing uncommitted work intact.

Outcome: both assemblies build; 70 focused tests pass; official agent harness passes. Native shots, real-prefab HP exchange and four slope traversals verified. All 300 workbook placement rows apply and all three maps retain aligned encounter rows. Unity left in Edit Mode on the saved SR18 scene. Mobile-device and full uninterrupted chapter play were not performed.

Implementation choices: FatMan repeats every 2.8 simulation seconds with release at .62; guard release at .45. Ship uses 7.5 seconds, length 9.5m and straight predictive aim. Existing elite progression remains; the misclassified early Woman is Boss with HP 519, final boss HP 2458.

Review corrections retained in the learning: real enemies use Default; helper sweep must include it. Guard grip is mesh min-X, muzzle max-X; shrink/aim around the actual animated hand. A missing native Unity component requires an explicit Unity null check. Scene authoring floor probes need small seam offsets. The first camera probe was paused by a bonus interaction; isolated traversal subsequently passed.
