# Noryangjin Enemy Movement Authoring

The five Forward enemy prefabs support per-instance movement settings in the
Noryangjin map tool and in the normal Unity Inspector. Trigger-to-enemy mapping
is authored only by selecting objects in the map tool.

Open the map tool and select `적군`. The palette contains
`Enemy_YllowMan`, `Enemy_Guard`, `Enemy_OldMan`, `Enemy_FatMan`, and
`Enemy_Woman`; placed instances are grouped under `Noryangjin_MapTool/Enemies`.
Enemy occupancy is separate from scenery, so an enemy can share a map cell with
an object while two enemies cannot occupy the same enemy cell. `적 발동 스팟`
uses the same enemy-tab ownership, while `회전 스팟` belongs to `기믹`; selection
and deletion follow the active tab so an overlapping road is not removed by
mistake.

The fixed Forward prefab identity decides its tier: YllowMan, Guard, and OldMan
are Normal, FatMan is Elite, and Woman is Boss. Per-chapter damage and health
come from the tier growth values documented in `game-data-workbook.md`, not
from a scene-instance tier override or an enemy-specific weight. Player-route
order determines only each enemy's interpolation point between the chapter's
initial and final values.

## Combat component scope

`EnemyScript_space` belongs only to these five Forward prefabs and is not the
general pooled-enemy component. Its Inspector exposes one `Enemy Data` asset,
the bonus wall, and optional held-projectile settings. Runtime health, damage,
and tier are supplied by `ChapterEnemyStatController`, so they are not authored
per placed enemy. The old `Space/EnemyTypeWalker`, `Rusher`, and `Tank` prefabs
do not carry this component; other game modes continue to use `EnemyScript`.

## Movement modes

| Map-tool label | Runtime mode | Starts |
| --- | --- | --- |
| `가만히` | `StayStill` | Immediately; the controller adds no movement |
| `좌우 이동` | `MoveSideToSide` | Immediately |
| `트리거 후 전진` | `MoveForwardOnTrigger` | When a linked player trigger is entered |
| `트리거 후 옆 등장` | `EnterFromSideOnTrigger` | When a linked player trigger is entered |

Forward and right are the enemy instance's horizontal local axes. When one of
the five Forward enemies is placed from the enemy palette, the map tool
automatically points the root forward along the player's nearest route section.
The player start transform defines the first direction and each turn spot
defines the next direction, so the preview and final placement also follow
bends in the route. Copy placement preserves the source instance rotation so a
manual special-encounter direction can be reused. If no player route exists,
the palette Y value is kept as the fallback. A placed enemy can still be rotated
manually for a special encounter.

At runtime the authored root rotation remains the movement frame, while only
the Animator visual root turns horizontally toward the current player position.
This lets enemies visibly track the player after route bends and lateral movement
without changing forward or side-to-side movement directions.

Use `선택 적을 플레이어 진행 방향으로 정렬` to repair one selected enemy, or
`모든 적을 플레이어 진행 방향으로 정렬` at the top of the enemy tab to update
all existing Forward enemies in one undoable operation. The placed position is
the center of side-to-side motion and the destination of a side entrance.

## Triggered movement workflow

1. Place and select a Forward enemy.
2. In `적 이동 동작`, choose `트리거 후 전진` or `트리거 후 옆 등장`.
3. Configure speed and, for a side entrance, left/right side and entrance
   distance.
4. Place `적 발동 스팟` from the map-tool palette at the point the player
   should cross.
5. Select the trigger, then click each enemy that should be linked. One trigger
   can target several enemies; clicking a linked enemy again removes it.
6. Leave `한 번만 발동` enabled for the normal one-shot encounter behavior.

The trigger accepts only the player's root collider, so weapon and visual child
colliders do not activate it twice. A successful one-shot trigger is restored
when the next run starts.

Selecting a placed enemy activation spot automatically enables SceneView
assignment. Click an enemy to add it to that spot; click the same enemy again
to remove it. The selected spot stays fixed while assigning, and orange
connection lines plus numbered enemy labels show the current mapping. Press
`Esc` to clear the spot selection and return to the selection tool.
Alt/right/middle mouse input remains available for SceneView camera navigation,
and every add/remove operation supports Unity Undo. The serialized target list
is intentionally hidden from the normal Inspector.

## Defaults and repair

`Enemy_FatMan`, `Enemy_Guard`, `Enemy_OldMan`, `Enemy_Woman`, and
`Enemy_YllowMan` carry the controller with `가만히` as the safe prefab default.
Scene instances may override that setting independently.

Run
`Tools/맵 제작 도구/노량진 맵 제작/게임플레이/적 이동 기능 연결`
to restore missing default components or recreate
`Assets/ShooterSurvival/Prefabs/Gameplay/Noryangjin_EnemyMovementTrigger.prefab`.
The command edits prefab assets only and does not save the open scene.
