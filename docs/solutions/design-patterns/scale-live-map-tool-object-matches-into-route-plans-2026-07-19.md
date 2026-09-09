---
title: Scale Live Map-Tool Object Matches Into Route Plans
category: design-patterns
module: Unity Noryangjin map-tool planning artifacts
problem_type: design_pattern
component: tooling
severity: medium
applies_when:
  - A route-planning workbook must reuse the object language of a currently open Unity map-tool scene
  - The active scene is dirty and must be inspected without being saved or modified
  - A short authored route must be expanded without multiplying every prefab by the same factor
  - Excel must show both exact instance budgets and readable cluster placement
tags:
  - unity
  - map-tool
  - excel
  - noryangjin
  - prefab-matching
  - object-placement
  - route-design
  - visual-regression
date: 2026-07-19
last_updated: 2026-09-06
---

# Scale Live Map-Tool Object Matches Into Route Plans

## Context

The open `Noryangjin_MapTool_Mode` scene contained a short but visually authored route. The initial planning task was to preserve that scene as the first portion of a longer chapter while extending the route and its object language in Excel only. The Unity scene was already dirty, so saving or mutating it was outside that task's trust boundary. A later, explicitly authorized implementation applied the finished plan to `Noryangjin_MapTool_Mode_2` while continuing to treat MapTool 1 as read-only source material.

Windows UI screenshot capture could not attach to the Unity editor because the required interface was unavailable. Read-only Unity scene inspection was therefore used to query the live hierarchy without changing or saving the scene.

The live hierarchy contained 21 road modules and 170 directly placed objects across 16 normalized prefab types. A naive route-length multiplier would have proposed 1,149 objects, overfilling the plan because background coverage assets and foreground props do not scale the same way. Visual workbook review also exposed a separate reporting error: categorical and distance columns had been summed in a totals row even though they were not additive.

## Guidance

1. Treat the live hierarchy as authoritative when an active scene is dirty. Read `Roads`, `Props`, and `Water` directly, but do not save, modify, or normalize the scene itself.
2. Normalize placed-object names before grouping. Strip coordinate suffixes such as `_X..._Z...` and duplicate suffixes such as `(1)` so scene instances resolve to their source prefab vocabulary.
3. Reconstruct the route order before classifying props. For the Noryangjin reference, the authored path was a bottom entry, a vertical market corridor, and a top exit. Assign each prop to its nearest road module and then to one of those spatial zones.
4. Scale object classes separately. Treat ocean and ground as coverage assets governed by area and camera visibility; treat stalls, tanks, signs, boats, creatures, and scatter as discrete foreground instances. Do not apply one route-length multiplier to every prefab.
5. Preserve the authored scene as phase 1. For this plan, keep the current 21 roads and 170 objects intact, then add 78, 89, 82, and 92 objects in phases 2-5 for a 511-instance chapter target.
6. Encode repeatable visual pairings instead of isolated decoration: market facade with aquarium tanks, gantry with buoys and life rings, fish scatter with seagulls, floating buoy with puffer, and water with boat. Keep the five-unit playable-center clearance for ordinary props, use a measured four-unit edge clearance for quay-bound props, and avoid large objects near turns or within four generated modules of the `+` crossing.
7. Separate exact budgets from readable placement. Use an analysis sheet for prefab paths, current counts, spatial evidence, formulas, and phase budgets. Use a placement sheet for representative cluster markers and comments. In the Noryangjin plan, the first 11 route cells represent the current scene and the remaining 60 cells represent the extension.
8. Verify the workbook in a real spreadsheet renderer. Recalculate formulas, export every sheet to a one-page PDF, inspect the map at useful scale, and count route/object markers. Report categories as not applicable in totals rows and distances with a count-weighted average rather than meaningless sums.
9. When the plan is applied to Unity, leave a dirty MapTool 1 loaded and untouched. Open MapTool 2 additively, make it active only for generation, save only that exact target scene, restore the prior active scene, and close MapTool 2. Capture MapTool 1's actual disk hash immediately before the operation and compare it with the post-run hash; do not embed a historical hash as permanent truth.
10. Validate the copied baseline by prefab identity and count, not only by the total child count. For Noryangjin, MapTool 2 must retain the 170 copied props as the same 16 connected prefab types used by the Excel analysis; otherwise the 511-object total no longer describes the generated scene.
11. Resolve clear-lane placement against the entire route, including nearby parallel legs and the space between module centers. The first implementation checked the preferred side of an anchor road and produced a prop inside another leg's five-unit lane. After choosing a side and offset, reject any non-exempt renderer AABB whose planar distance to any consecutive copied-or-generated route segment is less than five units. This continuous-corridor check catches wide props that cross a lane between pivots or at a corner.
12. Treat high-resolution previews as a composition test. The first generated view made tiny added village modules look like accidental islands and left water coverage too fragmented. Set the generated village budget to zero and use the `1.15` ground scale only for coherent paired market quays; increasing water scale alone does not solve coverage gaps.
13. Lay coverage assets from renderer dimensions, not route-anchor spacing. The original 196-water pass selected every seventh road candidate and scattered tiles at lateral distances of `18`, `32`, and `46` units. The rendered water footprint averaged only about `23.7` by `20.3` units, so the placement algorithm guaranteed holes even though the Excel instance count was correct. Do not satisfy a fixed count by deleting visible corner cells: that made the four corners look like missing water. For this `20`-column by `10`-row envelope, use 18 tiles in each edge row and 20 in each of the eight interior rows (`18 * 2 + 20 * 8 = 196`), span every row across the same min/max X, and stretch edge-row X scale by `19/17` to preserve overlap.
14. Verify the prefab's effective world axes after palette rotation. The first grid attempt used yaw zero, but the prefab's base rotation left each scaled tile about `21.56` units wide and `32.61` units deep while adjacent grid columns were about `22.95` units apart. Predictable vertical seams remained. Applying the map tool's east-facing yaw rotated the effective footprint to about `32.61` by `21.56`, closing both axes without adding objects.
15. Add a renderer-bounds water-coverage test distinct from playable-lane validation. At every generated road module, sample points `-24`, `-8`, `+8`, and `+24` units across the road and require every point to fall inside at least one generated water renderer bound. Group water by rendered rows, require at least one unit of horizontal tile overlap and vertical row overlap, and require every row to span the full coverage width. These checks failed on the sparse layout, the incorrectly rotated grid, and the corner-omission layout before passing on the final rectangle.
16. Distinguish actual water gaps from objects that visually replace the water. In the first screenshot, shallow tile overlap caused some seams, but many rectangular "holes" were the rainy-market ground prefab scattered over the ocean. Grouping all 21 added ground pieces into continuous quay runs made those gray areas read as intentional market platforms while preserving the Excel count.
17. Allocate solid market slots across prefab budgets, not independently inside each budget. Crab tanks, octopus tanks, and three facade types can otherwise resolve to the same platform and along offset. Use one phase-wide ordinal per role, separate facade and tank bands across the quay, and reject renderer-bounds intersections before saving.
18. Make visual guarantees part of the generator's fail-closed contract. Recheck effective water bounds after palette scaling, measure renderer footprints against the playable corridor, and write requested preview artifacts to a durable `outputs/` directory. Unity owns and cleans a project's `Temp/` directory during batch shutdown, so a successful capture log is not proof that a deliverable survived.
19. Preview rendering must never mutate a serialized shared Material, even if code plans to restore its keyword, property, and dirty flag. Directly toggling FlatKit outlines caused Unity to serialize new shader defaults into ten source `.mat` files. Instead, cache `HideAndDontSave` clones for outlined materials, disable outlines only on those clones, swap renderer `sharedMaterials` for capture, and restore the original arrays before destroying the clones. Also suppress non-target renderers with reversible visibility state while explicitly retaining the authored `Original` start context. Restore material references, visibility, and renderer dirty state on both success and failure.
20. Isolate preview tests without touching the user's active authored scene. `EditorSceneManager.NewPreviewScene` cannot be made active, and `SceneManager.CreateScene` is play-mode-only in this Unity version. Create fixtures normally, move their roots immediately into a preview scene, assert that the prior active scene's dirty state did not change, and close the preview scene in `finally`. Use injected small dimensions and GUID-scoped paths for routine tests, and compare multi-megabyte 4K artifacts with streaming hashes rather than allocating whole-file byte arrays.
21. Model modular piers by their imported pivot, not by an assumed centered footprint. `Pier_Long_Fantasy` has a one-ended, asymmetric pivot. Splitting a corner into two half-length pieces therefore creates visible gaps. Let each full-length module own the incoming graph edge; the next full-length outgoing module forms the other arm of an L and overlaps the `8.85`-unit grid edge by roughly `0.59` units. Do not add synthetic connector children.
22. Make the route graph explicit. The 117-module main traversal uses seven orthogonal legs (`E12, N18, W17, S12, W6, N19, E33`) without revisiting a cell. The `+` is one degree-four node with four-module north and south dead-end arms. The complete generated graph is a tree, so the arms remain optional exploration space and cannot shorten the 116-edge start-to-highway main path.
23. Treat prefab-facing corrections as data. Preserve the palette's base rotation and then apply placement yaw. The `M2` sashimi-stall prefab needs an additional local `180`-degree correction so facades on opposite quays face each other instead of facing the water.
24. Measure quay contact from imported geometry. At the `1.15` ground scale, the gray quay half-width is about `7.843` units and the timber deck half-width is about `4.184` units. A lateral center offset of `11.9` units produces about `0.13` units of edge overlap: enough to hide the water seam without covering the central path. Keep adjacent ground anchors at stride one so each side reads as a continuous quay.
25. Make the chapter handoff visually ordered. The final timber module is not a destination: it joins a 90-unit paved highway deck. Place the expressway sign and toll gate on that deck at approximately `+16` and `+38` units, then place the tunnel transition near `+89`. Validate deck contact, projected length, lateral overlap, and tunnel overlap rather than assuming a centered highway pivot; this freeway prefab is also one-ended.
26. Verify rotated coverage axes and preview warm-up. The ocean prefab carries a baked `X=-90` rotation, so its local Y scale controls a world-Z footprint. Size the coverage grid from final renderer bounds, including the copied route, generated main path, both `+` arms, and a highway coverage endpoint, with a 70-unit exterior margin. In a fresh batch editor, render one warm-up frame before reading the first URP preview or the top image can contain cyan/magenta fallback materials.

## Why This Matters

### Inventory-first concept images

The SR18 set-dressing request on 2026-09-05 initially produced attractive AI concept images containing invented buildings, cranes, and giant seafood architecture. The user correctly rejected them as an implementation plan: a generated picture is not an available Unity model.

When the project already supplies an asset vocabulary, inspect the map-tool registry and palette scale/rotation overrides before proposing styles. Use only verified prefab paths in an executable layout proposal. Vary shop density, side selection, empty intervals, and prop clusters instead of silently adding a new modeling requirement.

Inventory alone is insufficient when the user says to reference the props already placed in the map. The follow-up SR18 review rejected the palette-only drafts too: they repeated isolated kiosks and invented protruding wooden pads while the actual map used six facing shops on continuous gray market ground. Inspect and capture the source `Props` hierarchy before planning. Preserve existing instance scale, yaw, child setup, group spacing, ground contact, and foreground/background roles; prefab defaults do not preserve that authored composition. In the observed Map1, 454 of 530 Props were water and 33 were ground, so the total did not imply hundreds of foreground props. The corrected instance-based evidence and ten variants are under `map-concepts/sr18-authored-props-2026-09-05/`.

Do not turn a reference scene's current count into a density limit. The user subsequently clarified that SR18 should have very many shops; the six existing shops were an appearance/composition reference, not a request for a sparse harbor. The first dense replacement contained 417 shops and six interior market rows. That layout was also rejected after application: high density did not authorize covering the existing timber roads or inventing a second set of interior streets. Current artifacts are under `map-concepts/sr18-roadside-market-2026-09-05/`; the 417-shop folder is historical.

### A placement picture is not a larger flow table

The 2026-09-06 full-stage enemy/BonusWall review exposed a presentation mismatch. Replacing a dense flow diagram with four pages of larger text cards still failed the user's request to see a picture. When the requested decision is spatial placement, show recognizable enemies and BonusWall objects on the actual route, with sparse direction arrows. Keep exact counts and chronology in the companion data; do not make the picture another index of E/F identifiers.

Generated illustration is not placement evidence. The first illustrated candidate moved SR18's inward-facing start stub to the bottom edge, added a connector, and misplaced label leaders despite a supplied route reference. Treat the route blueprint as a geometry reference distinct from the asset/style reference; explicitly constrain the ordered route points, start, finish, travel directions and overpasses. Compare the resulting picture with those invariants. If correspondence is not exact, label it as an approximate concept instead of claiming an unchanged map, verified transforms or implemented content. For an implementation-accurate image, use the actual Unity preview scene and prefabs.

The planning source remains `map-concepts/sr18-full-enemy-bonus-flow-2026-09-06/full-flow-final.json`; illustration prompts and retained candidates are separate from it. This presentation task does not authorize scene or balance changes. Both generated candidates were ultimately replaced by `tools/render-sr18-full-placement-preview.cs`, which clones the real route and props into a preview scene and uses the six existing enemy prefabs and existing BonusWall prefab. Capture one full-route view plus overlapping early/late crops, and disclose the 2.5x presentation scale. The 69 enemy and 14 fixed-wall instances are concept placements, not a runtime placement verification.

Check source state after preview cleanup in a separate editor call, not only before `finally`. This capture reported an unchanged source inside the script, but the subsequent query found the source dirty. A retained `SaveScene(..., saveAsCopy: true)` copy differed from the initially clean disk scene in exactly one Canvas active flag. Only that flag was restored, and dirtiness was cleared only after another retained copy was byte-identical to the original disk scene; the original scene was never saved. Do not generalize this into clearing arbitrary dirty scenes or resetting user edits. The comparison and guarded one-off recovery scripts are retained beside the rendering script. Emit large PNG previews one at a time: returning three 16–19 MB PNGs in one tool response exceeded the IPC frame limit.

### Applying combat to the actual SR18 scene

Only the subsequent explicit application request authorized writing the authored scene. `tools/apply-sr18-encounters.cs` uses the normal palette scales (2.25 for regular enemies, 2.5 for Woman, approximately 3 for altars), not the illustration's extra 2.5x factor. Existing Enemies/Bonuses must be empty; an existing report or target root stops reruns. A Unity save-as-copy backup precedes edits, existing road/prop components and GameObjects are compared separately, and validation failures undo only the new installation before any save. Do not compare a Transform JSON value with a GameObject JSON value merely because the dictionary key is a GameObject; that caused a false preservation failure during development.

Reject authored ramp roots when placing stationary encounters, but do not identify a flat timber road by demanding every triangle normal have `normal.y > .98`. A rough basic plank at `(-10.8, -70.57)` had a valid floor near Y=0 and normal Y=.963; the over-strict check incorrectly reported no deck. The corrected probe excludes `_Uphill`/`_Downhill` roots, restricts expected height to the intended deck, tolerates upward-facing rough triangles (`>.7`) and samples small plank-gap offsets. This preserves the actual route rather than moving roads to satisfy a faulty test.

The applied scene has 69 enemies, 14 fixed walls, 31 activation spots and 27 independent movement targets. Six staggered groups use two spots; every enemy has exactly one activation link. The two upper-deck groups and triggers remain around Y=12, with the following bonus on the returned lower flat. Validate counts, normal scale, source prefab links, map-authored bonus preservation markers, root collider overlaps and both sides of bonus pairs. The scene suite passed 11 tests; a real Play Mode root-collider callback probe verified activation, duplicate rejection, movement and reset without kills or coin collection. These checks do not prove complete physical traversal or final balance. See `map-concepts/sr18-encounters-applied-2026-09-06/README.md` for the backup, manifest and actual screenshots.

### Applying the approved dense layout

After explicit application approval, preserve the live dirty target with `EditorSceneManager.SaveScene(scene, temporaryPath, true)` before editing; retain a recovery copy outside imported assets. Copy source instances with `PrefabUtility.InstantiatePrefab` plus `GetPropertyModifications` / `SetPropertyModifications`, then apply their measured world transforms. Fail closed on structural overrides or non-persistent object references unless the copy path supports them. Exclude rendererless source triggers rather than duplicating already authored turn spots or importing cross-scene enemy links. Stage in a preview scene, check the approved positions, corridor clearance, overlaps and unchanged road signatures, then transfer with Undo and save only the exact target scene.

Preview-only outline suppression does not survive scene reload. If the approved appearance requires it in the authored scene, use dedicated persistent material variants and retain the original shared materials unchanged. SR18 uses 13 variants under `Assets/ShooterSurvival/Materials/Generated/SR18DenseMarket/`, connected prefab placements and one optimized water backdrop, not hundreds of copied water tiles.

Ground support needs a model-specific footing, not the full visual AABB. An initial 0.2-unit inset included shop awnings and demanded 219 support tiles; the same rule also rejected the original correctly placed shops. Inspection showed a 0.7-unit inset matches the bodies of these three unchanged storefront models. Only two additional tiles were needed. Removing the 217 task-created excess tiles preserved the approved quay silhouette; the final ground count is `33 + 351 - 3 + 2 = 383`. Remeasure this inset if the assets or scales change, and keep this support test separate from runner-lane clearance.

The initial saved SR18 result had 417 shops, 383 ground tiles, 846 direct Props roots including 15 turn spots, and one water backdrop. Its four focused tests passed, but that did not prove the layout respected the user's road. Treat this as a failed validation contract, not a successful final result.

### Road preservation is visual, not only geometric

The user then showed the character running on a partly covered timber path. The generator checked shop overlap and shop footing but never prohibited ground from covering roads; 252 ground Renderer bounds overlapped road bounds on XZ. Copying exact approved coordinates merely reproduced that mistake. Filling a loop with independent market rows also created a new visual route even though no gameplay branches were added.

The corrected `tools/rebuild-sr18-roadside-market.cs` merges collinear flat modules from their actual rendered bounds, then places inward-facing shops at a 12.1-unit pitch on both outer edges. Each shop gets a narrow 9 × 12.2 quay. Reject both shop and ground bounds that overlap any existing timber road, including adjacent legs and crossings. Keep corners and ramps open, retain the original 230 roads and 15 turn spots, and remove unrelated interior rows. The resulting 306 shops and 306 quays support complete storefront bounds, so the previous 0.7-unit footing inset is unnecessary.

`Sr18Market_LeavesTheExistingTimberRoadsVisible` independently checks zero shop/quay road overlap and rejects placement centers farther than 6 units from the nearest road boundary. It failed before the correction and passes afterward; all five SR18 tests, the Editor build and the harness check pass. Counts and serialized placement reports remain useful, but cannot replace these geometric intent checks. Revalidate or intentionally revise the baseline when the user later edits the layout.

Inspect the full map, the originally reported location, and the character's authored camera view. A generic Pipeline `screenshot game` returned success with a blank sky in this editor state; it was not usable evidence. A temporary camera copied from `MapTool_Camera`, with the target scene and scene-culling mask explicitly set, captured the actual saved composition without changing the user's camera. The result and recovery backup are linked from the roadside layout README. Static captures still do not validate runtime traversal or camera behavior throughout the stage.

For the corrected SR18 comparison, clone the current 230 roads and existing prefab instances into a temporary `NewPreviewScene`, render ten full-layout/close-up boards, and close that scene in `finally`. Preserve the user's dirty source scene. Record every proposed building/prop's prefab path and transform alongside the image. Use per-preview `MaterialPropertyBlock` overrides to suppress distance-exaggerated outlines without writing shared material assets. The artifacts and renderer recipe are under `map-concepts/sr18-existing-assets-2026-09-05/` and `tools/render-sr18-existing-asset-concepts.cs`.

Mark older imaginative images as non-executable references and withdraw recommendations that rely on unavailable assets. If new models are desired later, identify them as a separate modeling/texture/optimization task before treating them as ready content.

Route length and prop density are related but not interchangeable. Background surfaces cover space, while foreground props establish rhythm, landmarks, and encounter readability. Scaling both with the same multiplier produces visual noise and an inflated production budget.

The workbook therefore needs two complementary representations: exact instance counts for production and representative clusters for spatial intent. Keeping those layers separate makes the plan measurable without turning the map drawing into an unreadable icon field.

## When to Apply

Apply this pattern when a live Unity map-tool scene is the visual reference for a longer route, especially when the scene contains unsaved authoring work. It is also useful whenever a spreadsheet plan must preserve real prefab identities while forecasting additional content.

Do not use it when the scene hierarchy is stale relative to a checked-in source of truth, or when the task authorizes direct Unity authoring and runtime verification instead of a planning artifact.

## Examples

Before:

- Multiply all 170 placed objects by the route-length ratio.
- Produce a 1,149-object target with excessive water tiles and scatter.
- Show only generic icons without source prefab paths or spatial evidence.
- Sum categories and median distances in a totals row.

After:

- Preserve the existing 170 objects as phase 1.
- Add 341 objects across four extension phases for a 511-object target.
- Limit coverage assets by area while scaling foreground props by landmark and encounter needs.
- Record 16 live prefab types, their source paths, proximity to the road, dominant zones, pairings, and clearances.
- Render the workbook sheets, confirm zero formula errors, and keep route/object summaries synchronized with the generated scene rather than preserving stale totals.

Applied MapTool 2 result:

- Continue directly from the copied MapTool 1 exit, then build 117 main-route modules across seven orthogonal legs plus eight dead-end `+`-arm modules. Together with the 21 copied modules this produces 146 road modules while keeping only the 117-module main traversal in the 4:30 timing path.
- Keep the scene phase split exactly: P2 `26 / 78`, P3 `30 / 89`, P4 `32 / 82`, and P5 `37 / 92`, where each pair is generated roads / added props. The eight optional `+`-arm modules are appended to phase 5 and do not reduce the main-path distance.
- Use an `8.85`-unit generated road step and full-length, edge-owned pier modules. Validate all 124 generated graph edges plus the copied-to-generated handoff with dense collider sampling that tolerates only the narrow gaps between individual wood planks.
- Preserve all 170 copied props, add 341 props from the same 16 prefab types, and verify the final 511 direct prop children with connected-prefab, exact-transform, renderer-footprint clearance, market-collision, and turn-exclusion checks.
- Preserve the Excel water budget of 196 added instances while arranging it as a continuous overlapping rectangle behind the copied route, generated route, four-module `+` arms, and highway envelope: 18 tiles in each edge row, 20 in each interior row, and `19/17` X stretch on edge-row tiles. Account for the water prefab's rotated local axes and validate at least one unit of horizontal and vertical rendered overlap.
- Arrange all 20 added rainy-market ground pieces as coherent, edge-contacting quay runs on both sides of straight path segments. Distribute facade/tank budgets into non-overlapping phase-wide slots, and apply the `M2 +180`-degree correction so all stores face inward.
- Capture 3840 by 2160 top and three-quarter previews against a neutral background only after validation succeeds. Use preview-only material clones, warm the first URP frame, retain only the explicit layout and start-context renderers, and restore renderer references, visibility, and dirty state before saving. Persist deliverables under `outputs/chapter_campaign_reference_orthogonal_20min/`; source `.mat` assets remain diff-clean.
- In a disposable project clone, keep MapTool 1 loaded and dirty with an unsaved sentinel, invoke the public builder twice, and compare sorted direct `Roads`/`Props` signatures made from name, prefab path, local position, rotation, and scale. This catches nondeterministic regeneration without relying on unstable Unity scene bytes or file IDs.
- Confirm MapTool 1 is unchanged after both builds by comparing the runtime pre/post SHA-256 values instead of checking against a stale hardcoded digest, and assert that its dirty state, active-scene identity, roots, and sentinel are preserved.

## Related

- [Verify Dynamic Route Plans at Campaign and Chapter Scales](verify-dynamic-route-plans-at-campaign-and-chapter-scales-2026-07-19.md)
- [Prefer Stage Prefab Set Dressing Over Fake Helpers For Reference Matching Unity Scenes](prefer-stage-prefabs-over-fake-helpers-for-reference-matching-unity-scenes-2026-05-26.md)
- [Generate Unity Map-Tool Sibling Scenes with Fail-Closed Verification](../workflow-issues/generate-unity-map-tool-sibling-scenes-fail-closed-2026-07-15.md)
- [Prefer Prefab Placement Previews Over SceneView Line Grids](../developer-experience/prefer-prefab-placement-previews-over-sceneview-line-grids-2026-06-06.md)
- [Verify Unity Material Keywords After Bulk Outline Conversions](../workflow-issues/verify-unity-material-keywords-after-bulk-outline-conversions-2026-07-02.md)
- [Keep Unity Generated Set Dressing Outside the Runner Lane](keep-unity-generated-set-dressing-outside-runner-lane-2026-05-26.md)
- [Keep Generated Map-Tool Layouts Inside Work Grid Bounds](../developer-experience/keep-generated-map-tool-layouts-inside-work-grid-bounds-2026-06-21.md)
- [Protect Active Unity Scenes from Broad EditMode Test Runs](../workflow-issues/protect-active-unity-scenes-from-broad-editmode-test-runs-2026-07-18.md)
- [Preserve Prefab Transform in Noryangjin Map-Tool Placement](../logic-errors/preserve-prefab-transform-in-noryangjin-map-tool-placement-2026-06-02.md)
