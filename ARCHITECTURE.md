# ARCHITECTURE.md

## Purpose
This is a Unity 6 shooter project with most gameplay logic in `Assets/ShooterSurvival/Scripts`.

The repo is being shaped so agents can work from stable, versioned context instead of hidden intent.

## Runtime Layers
- Scene and game flow: `Scripts/Game`
- Combat actors: `Scripts/Player`, `Scripts/Enemy`, `Scripts/Weapon`
- Encounter orchestration: `Scripts/Wave`, `Scripts/Barrel`, `Scripts/Walls`, `Scripts/Obstackle`
- Presentation and timing: `Scripts/UI`, `Scripts/UI and VFX`
- Agent/debug harnesses: `Scripts/Harness`
- Editor tooling: `Assets/ShooterSurvival/Editor`

## High-Value Entry Points
- `GameManager`: stage resets, run lifecycle, enemy stat application.
- `PlayerScript`: player state, movement, health, run start, weapon linkage.
- `WeaponScript`: firing loop, damage, fire-rate upgrades.
- `WaveManager`: timed wave progression and victory trigger.
- `CanvasScript`: start flow, game over, win UI, attack debug display.
- `TimeManager`: global runtime gate for active gameplay.

## Player Character Defaults

- `PlayerScript` owns one Inspector group for health, attack, forward movement, fire rate, projectile count, absolute missile speed, and missile duration.
- `EnvironmentVariableTables` reads matching defaults from the shared game-data workbook. Valid Excel values take precedence when the Player toggle is enabled; missing values retain their Inspector fallbacks.
- Child player `WeaponScript` components consume the resolved attack, fire rate, and projectile count. Non-player weapons retain their `WeaponSO` defaults.
- `BulletScript` owns the resolved absolute missile speed and duration baseline. Pooled Water and Bomb projectiles reset elapsed duration per rental; range is the transparent result of `speed * duration` rather than a third authored stat.
- See `docs/player-character-defaults.md` for the exact Excel key/value mapping.

## Protected Game Data

- `Assets/ShooterSurvival/GameData/Editor/Data.xlsx` is the single editable source for enemy stats, upgrades, skins, bonuses, stage patterns, and character defaults. Its `Editor` location and the absence of a StreamingAssets copy keep the raw workbook out of player builds.
- `GameDataWorkbook` is the common stream boundary used by every Excel table loader. In the editor it reads the source workbook; in a player it verifies and decrypts `Resources/GameData/Data.bytes`.
- `GameDataWorkbookEditor` encrypts the workbook and signs it with an RSA private key stored outside the project and version control. The player contains only the public verification key and refuses modified archives.
- `GameDataBuildPreprocessor` regenerates and validates the protected archive before player builds. The Noryangjin map tool's `편의` tab exposes open, locate, regenerate, and validate actions.
- `GameDataWorkbookAssetPostprocessor` reloads environment-variable and monster-growth caches when the editor imports a saved `Data.xlsx`; loaded players and `GameManager` instances re-apply the changed values during active Play Mode and again on `EnteredPlayMode`.
- Enemy balance is chapter-and-tier owned rather than prefab-index-owned. Each chapter has one `Normal`, `Elite`, and `Boss` row with initial/final damage and health. `ChapterEnemyProgression` orders placed encounter enemies along the player route, then interpolates the chapter endpoints across that actual count: the first enemy receives the initial values and the last receives the final values. Pooled inventory is excluded. Legacy scenes call it through `GameManager`; Noryangjin scenes use `ChapterEnemyStatController`, which bootstraps at scene load and participates in live workbook reloads. `ForwardEnemyTierResolver` makes the five Forward prefab identities authoritative, so a stale scene-instance tier override cannot change their tier.
- Client-side protection prevents casual replacement and detects unsigned edits, but a patched client can still bypass local checks. Server-authoritative values are required for strong anti-cheat guarantees.
- See `docs/game-data-workbook.md` for the editing and verification workflow.

## Gameplay Analytics

- `Scripts/Analytics/GameplayRunTracker` is the Firebase-independent round state machine. It logs one `game_round_start` and one `game_round_end`, preserves each event's client occurrence time, counts only active unscaled play time, sums earned coins, and supports an unfinished-round snapshot.
- `FirebaseAnalyticsRuntime` bootstraps before the first scene and survives scene loads. It initializes Firebase on the Unity main thread, persists the collection preference, checkpoints an active run, and recovers a stale checkpoint as `abandoned`.
- `FirebaseAnalyticsSink` is the only Firebase event adapter. Before native initialization it persists at most 128 events locally in FIFO order; disabling collection clears both the queue and active checkpoint. It never embeds BigQuery credentials or sends data directly to BigQuery.
- Firebase App/Analytics and External Dependency Manager are pinned as local UPM archives in `GooglePackages/`. Android resolution writes the Unity bridge AARs to `Assets/GeneratedLocalRepo/Firebase` and patches the custom Gradle templates in `Assets/Plugins/Android`; `FirebaseAnalyticsSetupValidator` guards that complete build-time chain and the reviewed Firebase destination.
- `CanvasScript` defines the measured round boundary: Tap to Play starts a round; death, win, mode changes, reloads, or quit end it. `MoneyScript.GetCoin` is the single earned-coin ingress.
- `GameplayAnalyticsSceneContext` supplies Inspector-configurable chapter/stage metadata for gameplay scenes without a `GameManager`. Noryangjin scenes derive live stage/max-stage/progress from consumed route-turn checkpoints when no explicit context exists or the context opts in; they use the `1 / 1 / 10` compatibility fallback only when no usable turn spots exist.
- Firebase automatic events supply install/session retention and app engagement. Custom round events add chapter, stage-index progress, coins, virtual-world end position, active play time, and the nine-upgrade snapshot.
- Firebase exports GA4 rows to BigQuery only after the external console integration is linked. Repository SQL under `tools/analytics/bigquery/` validates parameter multiplicity/types and metric bounds before building one-row-per-round logs, D1/D7/D30 retention, app engagement, and round playtime views.
- See `docs/firebase-analytics-bigquery.md` for the event contract, Android setup, security boundary, and query workflow.

## Noryangjin Route Gameplay

- `Assets/ShooterSurvival/Scenes/Tools/Noryangjin_MapTool_Mode.unity` is both the authored map-tool layout and, after installation, a runnable route-gameplay scene.
- `Assets/ShooterSurvival/Scenes/Tools/Noryangjin_MapTool_Mode_2.unity` is a baked authored sibling: it preserves the exact Map 1 gameplay composition and implements the reconciled `outputs/chapter_campaign_reference_orthogonal_20min` route as 150 road modules and 511 props. It has no persistent scene generator; future changes use the normal map-tool editing path.
- `NoryangjinForwardGameplayInstaller` composes that scene from the configured `Forward March Mode` scene rather than reconstructing its setup from bare prefabs. It clones the player/weapon rig, Canvas and pre-start/shop UI, Managers, EventSystem, and upgrade services so scene-assigned references stay intact.
- `PlayerStatusHudBuilder` adds the rebuildable screen-space `PlayerStatusHUD` contract to that Canvas. The compact dark-glass cards use high-contrast white type and a coral health fill to show live current/max health and attack, stay hidden before gameplay, and replace the legacy player-child health Canvas plus `ATT` text only when all HUD references are valid.
- The installer keeps the map scene's real `Original` character as the visible child of `Noryangjin_Player` and disables only the cloned Forward character renderers.
- `PlayerScript` treats the player's current forward and right vectors as the route frame. Normal forward motion and lateral input therefore follow the new local frame after every corner instead of remaining locked to world axes. On a completed `NoryangjinTurnSpot` turn, the spot center becomes the lane origin so the configured `xRange` remains centered on the bridge rather than shifting to the player's trigger-entry offset.
- Noryangjin runtime state stays out of prefab authoring data. `PlayerScript` caches the active weapon and updates health UI only when values change; runtime and map-authored bonus walls roll only their selected bonus, do not keep a `FixedUpdate` callback in Forward mode, and skip the legacy global-volume overlay; and Forward enemy health canvases keep only the renderable Canvas/TMP pair with raycast input disabled. Forward enemies do not search for the legacy `VolumeTag`, while enemy hit feedback resolves the named `Walker-HitPos` anchor so FatMan and Guard do not depend on child ordering. During play, `EnemyScript_space` rotates only the Animator visual root toward the player; the authored enemy root remains unchanged so `EnemyMovementController` keeps its route-relative forward/right axes.
- `NoryangjinTurnSpot` is a rendererless trigger placed through the map-tool palette. When the player's root collider enters it, forward and lateral movement pause, the player rotates to an absolute world Y yaw over the configured duration, and movement resumes in the rebased route frame. Attack scheduling remains active during the turn. `WeaponScript` assigns the firing `PlayerScript` as each player missile's route owner; `BulletScript` applies that owner's incremental turn delta to the in-flight direction and root rotation without changing speed or remaining duration. Enemy and helper projectiles keep their independent trajectories.
- `NoryangjinMapToolWindow` separates placement into `오브젝트`, `적군`, `기믹`, and `보너스`. Its `편의` tab toggles only scene-root screen `Canvas` GameObjects; nested player, enemy, and bonus world-space canvases remain active. The same tab owns independent top-view work-grid radii for horizontal X and vertical Z: both default to 300 main cells, each accepts `50..1200`, and the `300/600/900/1200` presets set both axes together. Changing either axis repaints only the Editor overlay without moving authored objects or changing placement coordinates. The gimmick palette reads the eight canonical gameplay obstacles from `Prefabs/Obstacle/ObstaclePrefabs.asset`, so composite obstacle entries appear once while their single-part source prefabs stay hidden. Its enemy palette exposes the five Forward enemy prefabs under `Noryangjin_MapTool/Enemies`; the bonus palette exposes one reusable `Box_left` altar under `Noryangjin_MapTool/Bonuses` and adds the Noryangjin marker that suppresses legacy global-volume lookup. `AuthoredBonusWall` owns the Inspector-selectable Normal/Elite/Unique grade (`Elite` maps to workbook rarity `Rare`), while `WallScript` rolls the matching stat and value from `Data.xlsx`'s `보너스` sheet. The world-space UI is data-first: one large auto-sized value sits above a compact icon-and-stat badge that fits labels through `ATK SPEED`, without a separate choice title. `BonusChoiceAltarVfx` gives Normal and Elite rolls one coordinated family theme across world effects, badge outline, and stat value (amber attack, teal vitality, blue utility); Unique overrides every family with its larger, faster, denser purple presentation. The pedestal surface uses a procedural harbor-water warp: outer and inner water, a sparse compass layer, and broken foam rotate at different speeds and directions while circular particles spiral inward and rise as droplets. Proximity focus enlarges only the approached altar's warp, energy, value, and badge. All runtime themes use property blocks and authored UI baselines rather than mutating shared materials or prefab values. `Refresh Open Scene Altar Instances` preserves each authored root transform, name, and rarity while replacing stale generated child overrides with the canonical prefab. The five Forward enemy prefabs also reference the canonical `Box_left`: `EnemyScript_space` spawns it at the defeated enemy's ground position with the map-tool presentation transform (final scale `3,3,3`, Y rotation `180°`) and adds a removable `RuntimeBonusWall`, so enemy drops use the same data-driven altar while stage preparation still distinguishes them from persistent map-authored instances. Nearby altars exchange their rolled stat keys so a left/right pair cannot present the same ability. Enemy and bonus placements each use occupancy layers independent from scenery.
- `NoryangjinMapStaticOptimizer` owns the map-specific mobile bake. It applies only `BatchingStatic` to component-free environment placements, clears stale batching flags from enemies and trigger-driven roots, tags map-authoring guides `EditorOnly`, and limits camera overrides to `MapTool_Camera`.
- `MobileUiOptimizerWindow` owns repeatable mobile UI atlas and safe-static maintenance. Its one-click Editor window reads exact serialized UI Sprite references plus the seven runtime-loaded bonus icons, excludes indirect dependencies, Editor references, full-screen backgrounds, RawImage/TMP paths, and oversized sources, then deterministically updates the `HUD_Common`, `Lobby_Setting_Menu`, and `Upgrade` Sprite Atlas V2 assets. The same button invokes `NoryangjinMapStaticOptimizer` so newly added component-free environment placements receive only `BatchingStatic`, while behavior, animation, physics, skinned, effect, light, navigation, and timeline roots remain dynamic. Every run records a local audit report and verifies missing-reference delta, scene hash, atlas configuration, static counts, and second-run idempotence.
- Ocean and Map 2 water optimization is stored as scene-instance overrides against generated two-triangle meshes. The shared Meshy ocean prefab remains unchanged so recovery and non-map scenes keep their source asset.
- Android texture limits and streaming mipmaps are applied only to Noryangjin Meshy textures used by the authored scenes. See `docs/noryangjin-mobile-optimization.md` for budgets, exclusions, and measured scene results.
- Build Settings keep `Noryangjin_MapTool_Mode` enabled at index `0` as the default boot scene and `Forward March Mode` enabled at index `1`; Map 2 is deliberately excluded until its runtime timing and encounter contract is specified. Reinstalling Noryangjin gameplay preserves this order.

## Forward Enemy Animation

- The five Forward enemy prefabs (`Enemy_FatMan`, `Enemy_Guard`, `Enemy_OldMan`, `Enemy_Woman`, and `Enemy_YllowMan`) each own a valid Humanoid Avatar and a character-specific `AnimatorOverrideController`.
- `Assets/JH/Model/Animatior/ForwardEnemyShared/ForwardEnemyShared.controller` is their single shared state machine. Its states are `Idle` (default), `Attack`, and `Die`.
- `EnemyScript_space` is owned only by the five Forward enemy prefabs. It is a Noryangjin encounter component, not the pooled-enemy base used by other modes: chapter progression injects runtime health, damage, and tier; one `EnemySO` supplies hit/death feedback and score; and only FatMan/Guard configure the optional held-projectile fields. The legacy `Space/EnemyType*` prefabs no longer carry this component.
- Runtime animation compatibility stays with `EnemyScript_space`: trigger `act` enters `Attack`, the attack returns to `Idle` after exit time, and trigger `die` enters terminal state `Die`. Health labels refresh only when their value changes rather than formatting text for every enemy on every frame.
- FatMan and Guard resolve their throw direction at the release frame from the release point to the current player root-collider center. The projectile local X axis and rigidbody velocity use that same direction, so rotated or moving enemy instances no longer throw sideways.
- Character clips are assigned under `ForwardEnemyShared/Overrides`. OldMan, Woman, and YllowMan currently reuse their attack clip in the `Idle` slot because their source asset sets do not contain a separate idle clip.
- Rebuild or repair the assets with `Tools/Shooter Survival/Forward Enemy/Build Shared Animator Setup`. The operation is idempotent and must not save or modify an open scene.

## Forward Enemy Movement

- The five Forward enemy prefab roots carry `EnemyMovementController`. Each scene instance selects one mode: stay still, move side-to-side, move forward after a trigger, or enter from the left/right after a trigger.
- The Noryangjin map tool automatically aligns a Forward enemy placed from the enemy palette to the player's travel direction at that map position. It resolves the nearest route section from the player's authored start transform and the turn spots, while preserving each section's exact initial/outgoing direction instead of using a slightly diagonal line between offset trigger centers. The placement preview uses the same result; selected and all-enemy alignment buttons repair existing placements. Copy placement deliberately preserves the source instance rotation so manual encounter overrides remain reusable. If the scene has no player route, the authored palette Y rotation is retained, and every placed enemy can still be rotated manually afterward.
- The default stay-still mode disables its own runtime component, so unchanged enemies do not add a per-frame `Update` callback.
- Movement follows the enemy's authored horizontal local forward/right axes. The placed transform is the center for side-to-side motion and the destination for a side entrance.
- `EnemyMovementActivationTrigger` is a rendererless player-root `BoxCollider` trigger. One trigger can activate multiple forward/entrance enemies, disables only its collider after a successful one-shot activation, and resets for the next run.
- The map-tool palette exposes the trigger as `적 발동 스팟`. Selecting a trigger automatically makes it the active SceneView mapping owner: enemy clicks toggle serialized links, connection lines show the result, and `Esc` clears the spot selection. The serialized target list is hidden from both the map-tool panel and the normal Inspector; enemy movement settings remain editable per instance.
- `EnemyMovementController` delays placement capture until its first runtime update because pooled enemies are enabled before `EnemySpawnerScript` assigns the new spawn transform.
- `GameManager.OnTapToPlay` resets active movement controllers and consumed movement triggers before gameplay resumes.
- Repair the five default prefab components and the trigger prefab with `Tools/맵 제작 도구/노량진 맵 제작/게임플레이/적 이동 기능 연결`. The operation does not save an open scene.

## Current Constraints
- A large portion of gameplay logic is still `MonoBehaviour`-heavy and scene-coupled.
- Pure logic extraction is limited, so most verification still needs runtime harnesses.
- Some naming and folder boundaries are inconsistent (`Obstackle`, mixed UI folders, `_space` variants).

## Agent-Oriented Architecture Rules
- Extract pure logic when a rule can be tested outside a scene.
- Keep scene-coupled logic thin where practical.
- Prefer explicit helper methods over repeated inline behavior.
- Prefer stable, named verification entry points over one-off debug edits.
- Do not hide new operational requirements in comments or chat only.

## Current Harness Footprint
- Runtime combat harness: `Assets/ShooterSurvival/Scripts/Harness/CombatHarness.cs`
- Wave logic utility: `Assets/ShooterSurvival/Scripts/Wave/WaveHarnessUtility.cs`
- EditMode tests: `Assets/Tests/Editor/WaveHarnessUtilityTests.cs`

## Immediate Debt Worth Paying Down
- Normalize game-flow ownership between `GameManager`, `CanvasScript`, and `WaveManager`.
- Reduce duplicated state transitions around play mode, game over, and stage reset.
- Extract more combat calculations into pure utilities so tests can expand beyond wave counting.
- Add a narrow, reliable command surface for common verification tasks.
