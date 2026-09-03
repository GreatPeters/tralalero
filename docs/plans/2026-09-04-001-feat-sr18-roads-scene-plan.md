---
title: "feat: Build the SR18 Lighthouse Infinity roads-only scene"
type: feat
status: completed
date: 2026-09-04
deepened: 2026-09-04
---

# feat: Build the SR18 Lighthouse Infinity roads-only scene

## Summary

Create a new build-excluded `Noryangjin_MapTool_Mode_SR18.unity` sibling from the current 51-road Noryangjin map, append the exact 179-placement Lighthouse Infinity route, and retain required runtime/map-tool infrastructure while removing decorative and encounter placements except route-driving turn spots. Build and validate the height-separated crossing decks in a temporary scene before publishing the static SR18 scene.

---

## Problem Frame

The selected SR18 route depends on three visible over/under crossings. The existing Map 2 scene is a separate 150-road/511-prop authored artifact, and the current player freezes Y position, so overwriting that scene or pretending same-height overlaps are completed flyovers would both be incorrect. The roads-only result must preserve the source, express SR18 faithfully as an authoring scene, and state that runtime vertical traversal remains unfinished.

---

## Completed Preconditions

- `feat/enemy-event-controller` was already contained in `master`; the local feature branch was deleted.
- `map2` was created from the resulting `master` and is the active branch.
- Map 1 pre-work SHA-256 is `D3CFFE380E022ED0D45F9A251581861B2066960D48D54F5897181038F81C1315`.
- The official Pipeline server is reachable, but package `0.5.0-exp.1` rejects CLI `1.0.0-beta.8` commands and requires an upgrade.

---

## Requirements

- R1. Preserve `Assets/ShooterSurvival/Scenes/Tools/Noryangjin_MapTool_Mode.unity` byte-for-byte.
- R2. Preserve the existing `Noryangjin_MapTool_Mode_2.unity` scene and its contract unchanged.
- R3. Create `Assets/ShooterSurvival/Scenes/Tools/Noryangjin_MapTool_Mode_SR18.unity` as a new static sibling outside Build Settings.
- R4. Keep the copied 51-road prefix identical to Map 1 and append exactly 179 connected road placements for 230 total.
- R5. Follow SR18 `S27,W17,N24,E26,N12,E9,N13,E10,S6,W17,N18`, with ten new turns and final north heading.
- R6. Use one abstract coordinate frame for every crossing: origin at the current Map 1 endpoint, east-positive X, north-positive Z. Separate the upper route vertically at `(0,-3)`, `(-12,-3)`, and `(18,16)`.
- R7. Preserve existing five turn spots and add ten with yaw `270,0,90,0,90,0,90,180,270,0`, target X zero, and 0.5-second duration.
- R8. Props may contain only the 15 route-driving turn spots. Enemies, Bonuses, Water, root bonus altar, and loose bonus clones must be empty or absent.
- R9. Preserve player, managers, UI, camera, lighting, work-floor/grid, and map-tool roots needed for later manual authoring; make the normal map tool recognize the SR18 scene and document selecting its 600 overlay preset while editing the full route.
- R10. Validate source immutability, route order, counts, prefab provenance, crossings, bounds, content emptiness, and Build Settings exclusion before publishing the target scene.
- R11. Capture a top-view preview and document the roads-only/non-runtime boundary.

---

## Scope Boundaries

- No lighthouse art, scenery, water, enemies, gimmicks, bonuses, encounters, or story branches.
- No changes to shared player movement, physics, weapons, camera behavior, or collision layers. Only the map-tool scene registry may change.
- No change to Map 1, existing Map 2, shared road prefabs, or Build Settings.
- No claim that the elevated SR18 route is currently playable; the player cannot follow Y-changing roads yet.
- No permanent SR18 scene generator. A temporary Pipeline evaluation file may exist only through bake verification.

### Deferred to Follow-Up Work

- Runtime vertical traversal, camera clearance, and projectile behavior on elevated roads.
- Decorative object and encounter placement by the user.

---

## Context & Research

### Relevant Code and Patterns

- `Assets/ShooterSurvival/Editor/NoryangjinMapToolWindow.cs`: authoritative road registry, placement names, direction helpers, turn spots, and work-grid behavior.
- `map-concepts/noryangjin-expansion-2026-09-02/route-manifest.json`: SR18 route, crossings, and upper-span ranges.
- `Assets/Tests/Editor/NoryangjinTurnSpotTests.cs`: serialized and runtime turn behavior.
- `Assets/Tests/Editor/NoryangjinMapToolMode2SceneTests.cs`: additive scene-test isolation pattern; its existing Map 2 contract remains unchanged.

### Institutional Learnings

- `docs/solutions/workflow-issues/generate-unity-map-tool-sibling-scenes-fail-closed-2026-07-15.md`: stage in a temporary scene, validate, then publish once.
- `docs/solutions/design-patterns/count-map-tool-content-from-semantic-placement-roots-2026-09-02.md`: direct `Roads` children are the placement-count boundary.
- `docs/solutions/design-patterns/continue-map-tool-layouts-by-selected-renderer-bounds-2026-07-19.md`: keep prefab identity and validate real seams instead of hand-editing YAML.
- `docs/solutions/design-patterns/verify-dynamic-route-plans-at-campaign-and-chapter-scales-2026-07-19.md`: intentional crossings require exactly one upper segment and must not become branches.
- `docs/solutions/tooling-decisions/adopt-official-unity-cli-pipeline-as-codex-editor-control-path-2026-08-23.md`: official Pipeline is the only supported live-editor path.

External research is unnecessary because the repository contains the exact authoring and safety patterns.

---

## Key Technical Decisions

- New sibling, not replacement: branch isolation is not permission to destroy the existing Map 2 asset.
- Transactional publish: copy Map 1 to a temporary Unity scene, mutate and validate there, then move it to the final new asset path.
- Manifest-driven route oracle: both the bake and test read the SR18 route contract, while the test independently reconstructs expected directions and crossings.
- Position contract: determine the current endpoint, pitch, prefab bases, and corner ownership from the live 51-road source; record a world-space placement report before mutation.
- Corner ownership: every leg after the first starts with its turn-road placement; remaining placements on that leg use the outgoing heading.
- Crossing fidelity: route-distance intervals `[70,88)` and `[151,157)` are upper spans of 18 and 6 placements. Placement 70/151 is Uphill, 87/156 is Downhill, and the placements between them are elevated Basic roads.
- Slope normalization: measure each slope prefab's longitudinal renderer/collider extent and scale only that scene instance to the same center-to-center pitch as Basic roads; keep all 179 lattice centers and leg endpoints unchanged.
- Scene-specific new-road mix: 165 Basic, 2 Uphill, 2 Downhill, 8 RightTurn, and 2 LeftTurn = 179. The concept manifest's long Bridge count is not stamped one-to-one because the Bridge prefab has a different physical footprint.
- The source's placement transforms, not historical 21/24-road documentation, determine the continuation pitch and yaw.
- The SR18 test is new and narrow; existing Map 2 and optimizer tests remain valid and unchanged.

---

## Open Questions

### Resolved During Planning

- Scene target: create a new SR18 sibling and preserve both existing authored scenes.
- Crossings: build visible height-separated authoring geometry, not flat collider overlaps.
- Runtime readiness: explicitly deferred because the current player freezes Y.
- Work grid: the 600 preset is EditorWindow overlay state, not a scene asset. Make SR18 a recognized map-tool scene and document selecting 600 when editing; do not claim it is serialized into the scene.

### Deferred to Implementation

- Exact elevated deck Y and normalized slope scale: derive both from connected slope/basic prefab renderer and collider bounds, then require lower-road clearance and exact lattice endpoints in the placement report.
- Exact corner and turn-trigger offsets: characterize the five source corners and reuse their pivot/trigger relationship.
- Pipeline package files changed by the mandatory upgrade: accept only the official package/lock changes reported by Unity.

---

## High-Level Technical Design

> *This illustrates the intended approach and is directional guidance for review, not implementation specification. The implementing agent should treat it as context, not code to reproduce.*

```text
clean Map 1 ──save-as-copy──> temporary SR18 scene
                                  │
                                  ├─ clear authored content
                                  ├─ keep 51 roads + 5 turn spots
                                  ├─ append 179 roads + 10 turn spots
                                  ├─ lift two upper spans over 3 crossings
                                  ├─ expand authoring grid and validate
                                  ▼
                           final SR18 sibling
                  (Map 1 and existing Map 2 untouched)
```

---

## Implementation Units

### U1. Upgrade Pipeline and characterize the source

**Goal:** Establish a supported live-editor channel and a durable pre-bake placement report.

**Requirements:** R1, R2, R6, R9, R10

**Dependencies:** Completed Preconditions

**Files:**
- Modify: `Packages/manifest.json`
- Modify: `Packages/packages-lock.json`
- Create: `map-concepts/noryangjin-expansion-2026-09-02/sr18-unity-placement.json`

**Approach:**
- Upgrade official Pipeline to the CLI-required version, wait for recompile, and prove a narrow scene command.
- Reject a dirty Map 1 or dirty loaded SR18 target.
- Query the 51 connected road placements and five turns, then record endpoint, pitch, orientation, prefab paths, 179 planned transforms, ten turn-spot transforms, three crossing pairs, half-open upper intervals, normalized slope transforms, bounds, and seam tolerance.
- Keep all coordinates relative to the recorded source endpoint and derive world coordinates once.

**Patterns to follow:**
- `NoryangjinMapToolGridUtility` and road registry in `Assets/ShooterSurvival/Editor/NoryangjinMapToolWindow.cs`

**Test scenarios:**
- Test expectation: none -- this unit records authoritative preflight evidence without changing scene assets.

**Verification:**
- Official `list_open_scenes` succeeds after upgrade.
- Placement report contains 179 sequence entries, ten turns, three crossing pairs, and a north endpoint.
- Map 1 hash remains unchanged.

### U2. Add an independent SR18 scene contract test

**Goal:** Define the target contract before publishing the scene.

**Requirements:** R1–R10

**Dependencies:** U1

**Files:**
- Modify: `Assets/ShooterSurvival/Editor/NoryangjinMapToolWindow.cs`
- Modify/Test: `Assets/Tests/Editor/NoryangjinMapToolGridUtilityTests.cs`
- Create/Test: `Assets/Tests/Editor/NoryangjinSr18SceneTests.cs`
- Create: `Assets/Tests/Editor/NoryangjinSr18SceneTests.cs.meta`

**Approach:**
- Add the SR18 exact path to the map tool's supported-scene registry so active-scene detection, open-scene resolution, placement controls, and turn-spot markers work normally.
- Independently parse the SR18 row from `route-manifest.json` and prove the placement report's leg counts, turn order, upper intervals, crossing segment pairs, endpoint, and source hash before using its measured transforms.
- Compare the saved scene against the validated placement report.
- Use additive scene loading and restore the prior active scene in `finally`.
- Compare the copied 51-road prefix to Map 1 by prefab path and transform.

**Execution note:** Run the focused test before the scene exists and confirm it fails for the missing SR18 target.

**Test scenarios:**
- Happy path: 51 source roads plus 179 SR18 roads equals 230 direct `Roads` children.
- Happy path: SR18 is accepted by the map-tool scene predicate and resolves to itself instead of opening Map 1.
- Happy path: the 179 extension placements match every expected prefab, position, rotation, scale, turn type, and final north heading.
- Integration: copied source roads and five turn spots match Map 1; ten new turn spots match the expected yaw sequence.
- Crossing: exactly the planned three XZ crossings exist, each upper/lower pair has the reported Y separation, and no extra XZ duplicate or collinear retrace exists.
- Clearance: upper renderer/collider bounds clear the lower road by the planned tolerance and consecutive route pieces still meet.
- Content: Props contains only 15 turn spots; Enemies, Bonuses, Water, and loose bonus roots contain no authored placements.
- Safety: Map 1 and existing Map 2 hashes are unchanged after the test; SR18 is not in Build Settings.
- Bounds: the route's measured bounds are reported and the documentation requires the 600 Scene-view overlay while authoring.

**Verification:**
- The focused test initially fails only because the SR18 scene is absent.

### U3. Bake and publish the SR18 roads-only scene

**Goal:** Build the target in a temporary scene and publish it only after complete validation.

**Requirements:** R1–R10

**Dependencies:** U1, U2

**Files:**
- Create: `Assets/ShooterSurvival/Scenes/Tools/Noryangjin_MapTool_Mode_SR18.unity`
- Create: `Assets/ShooterSurvival/Scenes/Tools/Noryangjin_MapTool_Mode_SR18.unity.meta`
- Temporary only: `tmp/sr18-map2-bake.cs`

**Approach:**
- Through official Pipeline `eval_file`, save Map 1 as a copy at a temporary asset path.
- Close or reject any dirty target, open the temporary copy, and make only it active.
- Remove authored content while preserving required infrastructure and the five source turn spots.
- Instantiate the planned connected road prefabs and turn spots through Unity APIs, using the recorded transforms and existing map-tool naming.
- Apply the measured upper deck Y and normalized slope transforms to `[70,88)` and `[151,157)` without moving their lattice centers.
- Run complete in-memory validation, save the temporary scene once, and move it to the final SR18 path.
- Run the independent focused contract test after publication; if it fails, delete the new final scene and meta so no failed artifact remains.
- Restore the original open/active scene state without saving Map 1 and remove the temporary evaluation source.

**Test scenarios:**
- Error path: missing prefab, wrong source count, source/target dirty state, invalid placement report, insufficient crossing clearance, or failed in-memory validation leaves no final SR18 scene.
- Integration: a successful bake changes only the new target asset plus planned package/test/report/docs files.
- Repeat safety: if a completed final SR18 scene already exists, abort rather than overwrite it silently.

**Verification:**
- The final scene exists with a Unity-generated meta file, Map 1 and Map 2 hashes are unchanged, and the target is clean.

### U4. Verify and document the authoring handoff

**Goal:** Prove the static scene matches the plan and record what remains for the user and later runtime work.

**Requirements:** R1–R11

**Dependencies:** U3

**Files:**
- Modify: `ARCHITECTURE.md`
- Modify: `DEVELOPMENT_OVERVIEW.md`
- Modify: `MAP_DESIGN_OVERVIEW.md`
- Modify: `docs/README.md`
- Modify: `docs/RELIABILITY.md`
- Modify: `docs/noryangjin-gameplay-maptool.md`
- Create: `docs/noryangjin-sr18-roads-scene.md`
- Create preview: `tmp/image-previews/noryangjin-sr18-map2/`

**Approach:**
- Run the focused SR18 test, existing Map 2 test, static optimizer test, editor build, and agent harness validation.
- Inspect the saved target hierarchy through Pipeline and capture a full-route top view.
- Document the exact road/turn/crossing counts, content emptiness, source hashes, authoring-grid extent, Build Settings exclusion, and non-playable elevated-road limitation.

**Test scenarios:**
- Integration: focused SR18, existing Map 2, and optimizer tests all pass together.
- Visual: the top-view capture shows the lighthouse-infinity silhouette and both raised spans crossing the intended lower roads.
- Safety: final git diff contains no Map 1, existing Map 2, shared prefab, or Build Settings change.

**Verification:**
- All checks pass, docs match measured scene facts, and a clickable full-route preview exists.

---

## System-Wide Impact

- **Interaction graph:** Adds one static, build-excluded authoring scene and one isolated contract test, and extends the map tool's explicit supported-scene registry; runtime systems are copied but not modified.
- **Error propagation:** Pre-publish validation failure deletes the temporary scene. Post-publish contract failure deletes the newly created final SR18 asset.
- **State lifecycle risks:** The bake records and restores open scenes and active scene, refuses dirty source/target state, and hashes both existing authored scenes before and after.
- **API surface parity:** No public or runtime API change.
- **Integration coverage:** Placement report, scene contract test, live hierarchy inspection, and top-view capture provide independent views.
- **Unchanged invariants:** Map 1, existing Map 2, shared prefabs, Build Settings, and runtime movement remain unchanged.

---

## Risks & Dependencies

| Risk | Mitigation |
| --- | --- |
| Pipeline package is too old | Upgrade only the official package, wait for recompile, and require a narrow command before scene work. |
| Abstract modules do not match prefab pivots | Commit the measured world-space placement report, normalize slope instances to the Basic pitch, and validate all leg endpoints and crossings against the manifest. |
| Elevated roads cannot be played | Mark the scene non-runtime and defer player Y-following; do not alter gameplay code in this pass. |
| Temporary bake partially succeeds | Publish only by moving a fully validated temporary scene to a previously absent final path. |
| SR18 exceeds the default overlay | Make SR18 a recognized map-tool scene and document selecting the existing 600 overlay preset while editing it. |
| Existing authored scenes are disturbed | Hash both before/after and fail any diff or dirty-state change. |

---

## Documentation / Operational Notes

- SR18 remains outside Build Settings and requires no production monitoring.
- The user can add objects through the normal Noryangjin map-tool palette after acceptance.
- Vertical runtime support must be planned separately before calling SR18 playable.

---

## Sources & References

- `map-concepts/noryangjin-expansion-2026-09-02/reference/routes/super-radical-18-schematic-4096.png`
- `map-concepts/noryangjin-expansion-2026-09-02/route-manifest.json`
- `Assets/ShooterSurvival/Editor/NoryangjinMapToolWindow.cs`
- `Assets/Tests/Editor/NoryangjinMapToolMode2SceneTests.cs`
- `docs/solutions/workflow-issues/generate-unity-map-tool-sibling-scenes-fail-closed-2026-07-15.md`
- `docs/solutions/design-patterns/continue-map-tool-layouts-by-selected-renderer-bounds-2026-07-19.md`
- `docs/solutions/design-patterns/verify-dynamic-route-plans-at-campaign-and-chapter-scales-2026-07-19.md`
