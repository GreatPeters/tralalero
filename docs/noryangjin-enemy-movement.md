# Noryangjin Enemy Event Authoring

The five Forward enemy prefabs use one `EnemyEventController` per enemy. The
activation spot stores only links; every attack, shot, movement target, and
animation choice belongs to the enemy itself.

Open the Noryangjin map tool and select `적군`. The palette contains
`Enemy_YllowMan`, `Enemy_Guard`, `Enemy_OldMan`, `Enemy_FatMan`, and
`Enemy_Woman`. Placed instances live under `Noryangjin_MapTool/Enemies`.

## Which event to choose

| Map-tool / Inspector label | Runtime mode | Result after the player enters the linked spot |
| --- | --- | --- |
| `공격 반복` | `AttackLoop` | Faces the player on the closest route-aligned 90-degree axis and keeps playing `attack_loop` |
| `공격 한 번` | `AttackOnce` | Faces the player, plays `attack_once` once, then returns to continuously looping `idle` |
| `발사` | `Shoot` | Faces the player and fires the configured held projectile once with `attack_once` |
| `지정 위치 이동 후 공격` | `MoveToTargetThenAttack` | Moves to one target, then faces the player orthogonally and starts `attack_loop` |
| `시작점 ↔ 지정 위치 왕복` | `PatrolBetweenStartAndTarget` | Repeats between the authored start and one target, playing `attack_once` at both endpoints |

Use `발사` for a ranged enemy with a held projectile. FatMan and Guard are the
canonical projectile-ready prefabs. For an enemy that does not shoot, choose
`공격 반복`, `공격 한 번`, or one of the two movement modes; a non-projectile enemy does not
need any shooting checkbox or spot-side option.

Every event waits for a linked activation spot. The prefab default is
`공격 반복`, but the enemy remains in `idle` until a spot activates it.

## One target for both movement modes

Both movement modes expose one `이동 목표` Transform. Assign an existing scene
Transform, or press `이동 목표 만들기` in the normal Inspector or the map-tool
selected-enemy card. The button creates one independent marker four units in
front of the enemy. Select the enemy to drag that marker with the Scene handle.

The target must not be a child of the moving enemy. If the target is missing or
invalid, the enemy rejects activation and the spot remains available instead
of silently consuming the encounter. A zero movement speed is also rejected
unless the enemy is already at the target. If a valid target is destroyed after
movement begins, the enemy cancels movement, returns to `idle`, and logs one
warning instead of throwing every frame.

Choose `없음`, `걷기`, or `달리기` for the movement animation. `없음` keeps
the continuously looping `idle` state while the Transform moves. While moving,
only the Animator visual root faces the actual travel direction. At an attack point the
visual snaps to whichever of the enemy's authored forward, back, right, or left
route axes best faces the player. It never takes a diagonal attack facing, and
the prefab root rotation remains unchanged.

Selecting an enemy always shows `이동 목표`, `이동 속도`, `이동 애니메이션`,
and `도착 판정 거리` in both the Inspector and the map-tool selection panel.
Stationary attack events preserve these values without using them; switching to
a movement event applies the visible settings immediately.

## Activation spot workflow

1. Place and select a Forward enemy.
2. In `적 이벤트`, choose one of the five modes.
3. For a movement mode, set speed, `없음/걷기/달리기`, and the single movement target.
4. Place `적 발동 스팟` where the player should cross.
5. Select the spot, then click each enemy to connect or disconnect it.
6. Press `Esc` to leave connection mode.

The spot Inspector intentionally has no `한 번만 발동`, `oneShot`, or
`on shot` field. It only shows the connection count and SceneView assignment
guidance. A successful activation disables the spot collider for that run; run
reset enables it again. Only the player's root collider can activate it, so
weapon and visual child colliders cannot fire the same spot twice.

Shoot release delay advances only while `TimeManager.isGameRunning` and the
custom time factor are positive. Pausing or stopping gameplay therefore holds
the projectile instead of releasing it in the background.

One spot can connect several enemies with different modes. For example, a
single spot can start Guard in `발사`, Woman in `공격 반복`, and OldMan in
`지정 위치 이동 후 공격` at the same time.

## Shared animation contract

All five prefabs use the shared Humanoid controller at
`Assets/JH/Model/Animatior/ForwardEnemyShared/ForwardEnemyShared.controller`.
It contains exactly these states:

- `idle`
- `attack_loop`
- `walk`
- `run`
- `die`
- `attack_once`

`attack_once` returns to `idle` after one cycle. Every assigned idle clip is
imported with looping enabled, so the `idle` state repeats continuously without
an idle-to-idle transition. Each enemy override controller
keeps its previous idle, attack, and death clips. Only the two missing
locomotion slots use the CC0 Quaternius Universal Animation Library:
`Armature|Walk_Loop` for `walk` and `Armature|Sprint_Loop` for `run`, both at
their authored speed. The source FBX, license, and download/hash record live
under `Assets/ThirdParty/Quaternius/UniversalAnimationLibrary/`. The previous
`ForwardEnemy_Locomotion.anim` asset is retained but no longer assigned.

Rebuild or repair the animation assets with
`Tools/Shooter Survival/Forward Enemy/Build Shared Animator Setup`. The method
edits prefab and animation assets only and must not save the open scene.

Agents use the same authoring contract through official Unity CLI commands:
create and position a target GameObject, set `eventMode`, `moveSpeed`,
`moveAnimation`, and `targetPoint` through serialized-field commands, and
resize the activation spot's `targets` array to assign object references.
Internal UI helpers are not CLI commands. Asset repair remains callable with
`unity command eval "ForwardEnemyMovementSetup.Configure();" --project-path .`
and `unity command eval "ForwardEnemyAnimatorSetup.Configure();" --project-path .`.

## Placement, reset, and repair

Automatic route alignment still points a newly placed enemy root along the
player's nearest authored route section. Map-tool position, height, snap, and
rotation changes update the initialized controller's cached start and route
axes, including Undo/Redo. A run reset returns a moving enemy to that authored
start and restores `idle`.

Legacy serialized mode integers migrate deterministically: old stay/side/
forward/fire values already align with attack-loop/patrol/move/shoot, and the
retired side-entrance value `3` normalizes to `MoveToTargetThenAttack`. The
source-level C# type rename is intentional; Unity scene and prefab references
remain intact through the preserved MonoScript GUIDs and `MovedFrom` metadata.

`ForwardEnemyMovementSetup.Configure` remains the internal repair entry point
for the five controller components and
`Assets/ShooterSurvival/Prefabs/Gameplay/Noryangjin_EnemyMovementTrigger.prefab`.
Despite the legacy utility and prefab filenames, the runtime components shown
in the Inspector are `Enemy Event Controller` and
`Enemy Event Activation Spot`. The repair operation does not save the open
scene.
