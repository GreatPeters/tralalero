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

## Noryangjin Route Gameplay

- `Assets/ShooterSurvival/Scenes/Tools/Noryangjin_MapTool_Mode.unity` is both the authored map-tool layout and, after installation, a runnable route-gameplay scene.
- `NoryangjinForwardGameplayInstaller` composes that scene from the configured `Forward March Mode` scene rather than reconstructing its setup from bare prefabs. It clones the player/weapon rig, Canvas and pre-start/shop UI, Managers, EventSystem, and upgrade services so scene-assigned references stay intact.
- The installer keeps the map scene's real `Original` character as the visible child of `Noryangjin_Player` and disables only the cloned Forward character renderers.
- `PlayerScript` treats the player's current forward and right vectors as the route frame. Normal forward motion and lateral input therefore follow the new local frame after every corner instead of remaining locked to world axes.
- `NoryangjinTurnSpot` is a rendererless trigger placed through the map-tool palette. When the player's root collider enters it, forward and lateral movement pause, the player rotates to an absolute world Y yaw over the configured duration, and movement resumes in the rebased route frame. Attack scheduling remains active during the turn.
- Build Settings keep `Forward March Mode` enabled at index `0` and `Noryangjin_MapTool_Mode` enabled at index `1`; installing Noryangjin gameplay must not replace the project's default boot scene.

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
