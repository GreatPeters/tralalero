# Highway enemy reconstruction and mandatory encounters

Implemented in the Unity project. Primary local gallery: <http://127.0.0.1:6753/highway-rebuild/>.

## Result

- Reconstructed six new character bodies from newly generated references using the installed local TRELLIS 2 engine. The original roster also had older TRELLIS lineage; the preceding September22 correction had only resized those old assets.
- ConeMechanic, AsphaltWorker, TrafficPatrol, TireBruiser, DeliveryRider and TollgateChief now have distinct faces, builds, clothes and equipment. Saved all six source prefabs and all 50 HighWay placements.
- Each body has 18 bones and seven imported actions: idle, walk, run, attack_loop, attack_once, hit and die. The live hit response is additive so it does not restart an active throw. Death presentation lasts at least the full one-second clip.
- Native hand equipment uses the installed Poly Universal Pack: wrench, farm shovel, Glock, tire, parcel bundle and road cone. These are reused project assets, not claimed as new TRELLIS outputs. Materials and fitting are owned by `tools/import-highway-rebuild.cs`.
- Replaced edge bypasses with 25 visible construction funnels. The local movement half-width narrows from4.4m to1.85m before each encounter. Sideways patrols now walk forward/back within their lanes, closing the middle gap. The existing current-health exchange remains; a straddled pair charges contact damage only once.
- Ordinary hazard avoidance between encounters and the authored green recovery branch remain. The new rule applies to combat passages on the occupied road, not to remote enemies on a different branch.
- Existing enemy/player capsule radii, workbook combat values, bonus fixes, helper formation and unrelated Noryangjin work are preserved. Patrol start/target direction and local passage geometry are intentionally changed.

## Verification

-146focused EditMode cases pass, including new saved-scene and reset contracts. Runtime and Editor C# builds pass. The final gameplay checks produced no Editor errors.
- Saved scene reopened:50actors, each exactly one Animator;34ranged actors with valid owned projectile/launch references;0sideways patrols.
- Left, right and center input probes at the original HP500 / ATT68 all reached enemy contact and HP0. Those three probes suppress traffic only to isolate enemy contact. An additional ordinary run keeps traffic enabled and also reaches enemy-contact gameover. They are not a human win-rate study or a new20-run matrix.
- Four controlled firing probes use a stationary non-shooting player and a test actor at HP10000 / attack100. Patrol release:0.4667s for a0.46s delay; tire0.7667s /0.736s; delivery0.5333s /0.506s; chief0.6667s /0.644s. All release-position errors were0m and all actual hits changed HP500→400. Non-gun objects preserved their release rotation; Arrow2 intentionally changes orientation to its flight axis.
- Both Blender source and fresh GLB pass the installed validation tool's hard gates. All bodies retain UVs, texture and weighted animation. Fresh FBX/GLB motion inspections use30fps explicitly. Body triangle counts: Cone19,710; Asphalt20,394; Patrol20,165; Tire20,259; Delivery20,077; Chief20,413. Delivery's standard GLB inspection flags one tiny degenerate face as a warning; the render-only skinned body uses a separate primitive gameplay collider.
- Native previews use actual prefab materials and imported clips, evaluated through skin matrices to avoid stale same-frame GPU poses. They are fixed-camera animation inspections. Gameplay and firing clips are separate live Play Mode captures.
- Original66preference records, wallet731coins /0jewels, upgrade levels15/8/1 and absent TutorialDone were restored. No APK install, merge or push in this task. Capture playback is fixed30fps simulation evidence, not a device FPS measurement.

## Artifacts and commands

- [Modeling contract](modeling-contract.md), [generated references](references/).
- Reconstruction sources and submitted prompts: `outputs/highway-enemy-rebuild-2026-09-23/production/<role>/`.
- Final animated bodies: `outputs/highway-enemy-rebuild-2026-09-23/rigged/v3/<role>/<role>.blend`, `.fbx`, `.glb`, rig reports, source/fresh metrics and ground checks. Tools are bound in Unity prefabs separately.
- Native assets: `Assets/ShooterSurvival/Models/Highway/Rebuilt20260923/` and `Assets/ShooterSurvival/Prefabs/Highway/Enemies/`.
- Evidence root: `tmp/image-previews/highway-enemy-rebuild-2026-09-23/`. Final artwork/motion is under `showcase-final/`; ordinary gameplay under `gameplay-final-grounded/`; firing probes under `projectile-bindings-fixed/`; multi-view body checks under `multiview/`.
- Generation: `tools/generate-highway-rebuild.py`, using the task-owned installed backend started by `tools/run-trellis-uv-math.py`. The backend was stopped after an empty-queue check; normal app settings were not changed.
- Rigging: `tools/rig-highway-rebuild-v3.py`; batch: `python tools/run-highway-rig-batch.py --revision v3 --rig-script tools/rig-highway-rebuild-v3.py`.
- Native imports and passage construction: `ImportHighwayRebuild.Main(role,"v3")` and `InstallHighwayEncounterLanes.Main` through Unity Pipeline.
- Native clips: `tools/capture-highway-rebuild-showcase.cs`, `tools/record-highway-rebuild.cs`, `tools/verify-highway-projectile-pose.cs`.
- Gallery rebuild: `python -X utf8 tools/build-highway-rebuild-gallery.py`.
- Preserve existing capture folders; the recording tools deliberately refuse to overwrite evidence.

## Rejected attempts and corrections

The first new reference was too tall/realistic and was replaced before reconstruction. v1 skinning stretched the wide worker's vest; v2 added a torso anchor and v3 smoothed the transition. Proxy tools were replaced with authored project assets. Early scene imports accumulated two/three rigs and lost ranged overrides; all visuals and role-specific launch bindings are now explicitly rebuilt and saved-scene tested. A first narrowed passage still allowed center escape through sideways patrols; the repeated center probe passed after the patrol correction. Earlier folders remain as debugging evidence, not final previews.

See the [reusable integration lesson](../../docs/solutions/integration-issues/rebuild-enemy-prefabs-without-stale-scene-rigs-2026-09-23.md) for the causal chains and prevention rules.
