# HighWay three-lane implementation

User authorization: implement the actual HighWay scene using the native Unity preview, not just its proposal website. The earlier planning-only restriction is superseded for this task. Existing five proposals remain rejected.

## Installed

- Current native v3 reference:3.6m playable lanes centred at−4.2/−0.6/+3.0, white dividers at−2.4/+1.2, yellow median edges,1m concrete median at−7.02 and an empty three-lane adjacent roadway.362generated mesh assets in72sections plus2branch marking meshes;72static median colliders.
- All regular Highway traffic directions are−1(head-on); bus added using an existing prefab. Incoming arrival groups preserve a lane and protect the centre encounter passage.
- Five existing log trucks now face/travel toward the player. Their log patterns leave the left escape edge open. Two rear-chase events and background traffic are disabled.
- Toll positions and four fatal potholes align with the new lane centres. Sixteen authored roadblocks are separated from log/toll/hole/fork/encounter windows. Traffic ranges exclude relevant toll/hole/fork approaches. The workbook and obstacle IDs are preserved.
- First recovery road's intersecting Mountain_Ridge and two grove units disabled. Both forks,72enemies,29bonus pairs,2,820m route, native camera/HUD/models and workbook balance retained.

## Commands and retained evidence

- `tools/install-highway-three-lanes.cs`: `Inspect`, `Apply` (one install only, includes final native-reference refinement), `MatchCurrentNativeReference` (in-place geometry/configuration repair), `Verify`, `ReloadAndVerify` through official Unity Pipeline. No hardcoded endpoint.
- `before/`: original dirty-working-copy HighWay and runtime source snapshots plus RestStop/workbook hashes. Do not restore the repository wholesale.
- `installed.json`, `verified.json`: authored configuration and retained scene checks after reopen.
- `play/`: rejected initial harness attempt; Editor's default play-start scene was RestStop. It was stopped and preferences restored. Not counted as Highway verification.
- `play-v2/`: two complete main/bypass runs before the final live-roadblock admission guard. Both completed,61/52regular cars, all three lanes used,0wrong directions,0three-lane contact walls. Normal collisions and normal lateral movement; map-tool9999 HP/attack applied once, no health pin or teleport.
- `play-v3/`: both prior-layout routes cleared with the live-roadblock guard;26/20cars,20logs each, no observed direction/roadblock/wall violations.
- `play-v4/`: rejected. Repeat Play preserved a static suppression deadline, producing zero regular cars; centre truck/static obstacle overlap also revealed a corridor conflict. Both failures remain on disk.
- `play-v5/`: current geometry mainline clear at360.07s,21cars,20logs, all3lanes,1896HP remaining. Bypass trial died near1653m; its driver ignored the visible log-warning period and started dodging only spawned logs.
- `play-v6/`: same saved scene, corrected warning-aware driver, bypass clear at360.10s,21cars,20logs, all3lanes,8870HP remaining. Both forks selected and rewarded. This is a separate replay, not a rewritten v5 result.
- Accepted v5main/v6bypass:0wrong-direction frames for cars/trucks,0overlap frames against live fixed roadblocks,0three-lane vehicle contact walls. Minimum same-lane vehicle gaps49.86m/57.64m. These are observed cohort metrics, not an exhaustive guarantee for every seed.
- Repeat Play without recompilation: deliberately arm5000seconds of suppression, stop/start, verify suppression=false/blockers=0/traffic=1/CombatHarness=1. After exit, harness=0. Receipts in `play-v5/restart-*.json` and `exit-verified.json`.
- Final focused tests22/22 plus existing road-gimmick tests5/5. Runtime and Editor builds pass; pre-existing `SplineSpeed.m_LastIndex` CS0649 remains. Agent-harness validation passes.
- Both final cohort preference restorations verify66common+11extra keys with no mismatches, wallet36431/jewels0, original map-tool start selector restored, TimeScale1 and HighWay clean. RestStop and Data.xlsx hashes match the initial snapshots.
- Unity Test Runner JSON, compilation outcomes and screenshots are recorded alongside the above. A test-start acknowledgment with0/0totals is not a pass.

## Sharp edges found

- Native installation continued after the CLI's30-second timeout. Its receipt/configuration existed; a follow-up save and reopen verified the full result. Do not run Apply twice.
- Existing workbook application reactivates authored hazard roots at run start. The final traffic guard consults the live unbroken collider before spawning, preventing visual traversal through those roadblocks while preserving them for the player.
- Template `Renderer.bounds` is empty while inactive. The first bus/truck spawn now uses source mesh bounds for its following gap.
- `OnDisable` for a spill must reset its phase as well as destroy the truck, otherwise re-enabling could resume Warn with a missing truck. Covered by the final lifecycle test.
- Scene geometry and car direction checks do not certify the campaign's first-clear economy or an Android frame-rate target.9999test HUD values are not new production balance.
- Existing camera tracks the shark laterally; the opposite outer lane can leave the viewport near the player. The gallery states that framing limit. No camera or HUD redesign was performed.

## Visual delivery

- `http://127.0.0.1:8776/three-lanes/applied/`:8native gameplay frames, simple branch flow and verified test conditions. Browser screenshot and tab switching verified.
- `tools/build-highway-applied-report.py`: rebuilds that gallery from accepted v5main/v6bypass evidence, copying only public PNG/result JSON files. Preference backups are outside the server root.
- `tmp/image-previews/highway-applied-2026-09-26/`: original PNG copies for direct inspection.
- Latest scene reloaded and verified with362valid mesh references,72median colliders,72enemies,29bonus pairs and5head-on log trucks.

## Scope

No character regeneration, no paid generation, no global stage-length/balance changes, and no RestStop scene authoring. Changes remain uncommitted in the existing shared working branch.
