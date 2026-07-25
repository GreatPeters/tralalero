---
title: "Make reference-scene gameplay composition transactional and idempotent in authored Unity maps"
date: 2026-07-23
last_updated: 2026-07-25
category: integration-issues
module: Unity Noryangjin gameplay composition
problem_type: integration_issue
component: tooling
symptoms:
  - "The authored map can move the player while its visible direct Original character remains visually static."
  - "Runtime animator lookup can bind to a hidden cloned model instead of the renderer-bearing Original."
  - "Player missiles can spawn about 1.3 metres away from the moving visible mouth because WeaponScript still uses the hidden Sharks/Original pistol muzzle for position."
  - "Water and Bomb projectiles can retain stale pooled rotation instead of facing their travel direction."
  - "A BulletScript on GFX can move or return the child, destroy its visuals, and leave the BulletPooler-owned root unrecycled."
root_cause: incomplete_setup
resolution_type: code_fix
severity: high
related_components:
  - "Player animation"
  - "Player projectile muzzle binding"
  - "Weapon projectile initialization"
  - "Projectile pooling"
  - "PlayMode integration tests"
tags:
  - projectile-muzzle
  - scene-composition
  - animator
  - projectile-pooling
  - transform-ownership
  - idempotency
  - rigidbody
  - playmode-testing
---

# Make reference-scene gameplay composition transactional and idempotent in authored Unity maps

## Problem

The configured movement, attack, start UI, shop UI, and upgrade services from `Forward March Mode` had to be attached to the existing Noryangjin map without rebuilding or replacing its authored `Original` character. After composition, the player moved but the visible character did not visibly walk, player missiles originated from a hidden clone's pistol muzzle instead of the visible mouth, and pooled Water and Bomb projectiles could face incorrectly or separate their `GFX` child from the root owned by `BulletPooler`.

The scene also needed route-relative turns, repeatable installation, reversible Build Settings changes, and verification against the saved authored scene.

## Symptoms

- A name-only search could choose the cloned `Sharks/Original` instead of the map's direct renderer-bearing `Original`.
- Disabling every cloned player renderer could leave the authored character hidden.
- The direct `Noryangjin_Player/Original` had no serialized Animator, while runtime animation lookup selected hidden `Sharks/Original`.
- The active weapon's serialized muzzle was only about `0.22` units from the hidden Forward model's head but about `1.30` units from the visible `Original` mouth.
- A reused projectile kept a stale root rotation because firing initialized translation direction but did not overwrite the rented root's rotation.
- `BulletScript` was attached to `GFX`, so moving or returning its own `gameObject` did not operate on the root registered in the pool's reverse lookup.
- Undo could remove installed scene objects while leaving Noryangjin enabled in Build Settings.
- Preserving `FreezeRotationY` blocked a turn, while releasing it without locking X/Z allowed the Rigidbody to drift.
- World-axis movement continued along `Vector3.forward/right` after a corner instead of following the new route.
- A short transition or pooled projectile could disappear before a timing-sensitive test observed it.

## What Didn't Work

- Rebuilding from bare prefabs was unsafe because the working Forward setup depended on scene-assigned references across the player, Canvas, managers, EventSystem, and upgrade services.
- Searching the whole hierarchy for the first object named `Original` was not idempotent after the Forward hierarchy had been installed.
- Preserving and re-enabling renderers was insufficient; the visible hierarchy still needed its own controller-backed Animator.
- Repairing renderer and Animator ownership was insufficient because the valid serialized `bulletPositions` reference still pointed into the hidden model.
- Using one authored transform for both origin and direction coupled an incorrect hidden-model position to a still-valid local firing axis.
- Adding installer code alone did not update the saved scene. The installer had to run again and save the target.
- Looking only below `Sharks` continued to target the hidden Forward clone.
- Passing `bulletPos.up` only to `SetDirection()` changed translation but did not clear a reused root's stale pose.
- Returning `gameObject` from the child `BulletScript` made the pool treat the child as unknown while the registered root remained outside the queue.
- Registering only created scene objects with Undo left `EditorBuildSettings.scenes` outside the same transaction.
- Replacing Rigidbody constraints with a presumed default discarded authored constraint bits.
- Testing a very short transition or `activeSelf` on a pooled projectile measured scheduling and pool timing rather than the intended behavior.
- Comparing a recorded spawn position with the live animated mouth several frames later measured player movement, not muzzle accuracy.
- An immediate Unity test rerun after refresh repeated a stale result until `Assembly-CSharp-Editor.dll` had actually rebuilt.
- Running runtime and editor `dotnet build` commands concurrently produced a transient shared-intermediate file lock; sequential builds passed.

## Solution

### Compose configured scene instances into one exact target

The installer accepts only `Noryangjin_MapTool_Mode.unity` as its active target, opens `Forward March Mode.unity` additively, validates required source dependencies, and clones the already configured player rig, Canvas, managers, EventSystem, and upgrade services into the target.

Scene-scoped lookup walks `scene.GetRootGameObjects()` instead of using a global name search. Repeated installation reuses the existing target `PlayerScript`, Canvas, managers, EventSystem, and upgrade manager, and rejects a partially present Managers set instead of silently producing duplicates.

### Preserve and animate the authored visual structurally

Search the player's direct children for `Original` before considering other same-named objects:

```csharp
foreach (Transform child in player.transform)
{
    if (string.Equals(child.name, "Original", StringComparison.Ordinal))
        return child.gameObject;
}
```

Restore renderers only inside that subtree and disable cloned renderers outside it:

```csharp
if (originalVisual != null &&
    renderer.transform.IsChildOf(originalVisual))
{
    continue;
}

renderer.enabled = false;
```

Renderer ownership and animation ownership are separate. Copy the working Animator configuration from `Sharks/Original` onto the direct visible `Original`, reusing an existing component so repair and repeated installation remain idempotent:

```csharp
Animator originalAnimator = originalVisual.GetComponent<Animator>();
if (originalAnimator == null)
    originalAnimator = Undo.AddComponent<Animator>(originalVisual);

originalAnimator.runtimeAnimatorController =
    sourceAnimator.runtimeAnimatorController;
originalAnimator.avatar = sourceAnimator.avatar;
originalAnimator.applyRootMotion = sourceAnimator.applyRootMotion;
originalAnimator.updateMode = sourceAnimator.updateMode;
originalAnimator.cullingMode = sourceAnimator.cullingMode;
originalAnimator.enabled = true;
```

At runtime, prefer the active direct `Original` before falling back to a skin beneath `Sharks`:

```csharp
Transform visibleOriginal = transform.Find("Original");
if (visibleOriginal != null && visibleOriginal.gameObject.activeInHierarchy)
    sharkAnim = visibleOriginal.GetComponentInChildren<Animator>(true);

if (sharkAnim == null)
{
    // Existing Sharks fallback.
}
```

Keep the Forward locomotion Animator separate from the visible generic-rig Animator. The saved direct `Original` uses the `Original` controller, and its `original walk` clip deforms the rendered `backleg` bone while the player advances.

### Split visible mouth origin from authored projectile direction

Visual ownership and projectile-origin ownership must move together. During installation, create or reuse one `ProjectileMuzzle` under the visible `Original` model's `headend` bone:

```csharp
Transform muzzle = mouth.Find(OriginalProjectileMuzzleName);
if (muzzle == null)
{
    var muzzleObject = new GameObject(OriginalProjectileMuzzleName);
    Undo.RegisterCreatedObjectUndo(muzzleObject, "Create Projectile Muzzle");
    muzzle = muzzleObject.transform;
    Undo.SetTransformParent(muzzle, mouth, "Attach Projectile Muzzle");
}

Vector3 forward = playerRoot.transform.forward.normalized;
muzzle.SetPositionAndRotation(
    mouth.position + forward * 0.35f,
    Quaternion.FromToRotation(Vector3.up, forward));
```

The child anchor follows the animated mouth and repeated installation reuses it. The saved Noryangjin scene therefore contains exactly one visible-mouth muzzle.

Spawn position and trajectory deliberately have different owners. Player weapons resolve their position from the visible anchor, while the authored `bulletPos.up` continues to define spread and travel direction:

```csharp
Vector3 direction = bulletPos.up.normalized;
Vector3 spawnPosition = ResolveProjectileSpawnPosition(bulletPos);

bullet.transform.position = spawnPosition;
bullet.transform.rotation = BuildProjectileRotation(direction);
bullet.GetComponentInChildren<BulletScript>().SetDirection(direction);
```

`ResolveProjectileSpawnPosition` applies this override only when the weapon belongs to `PlayerScript` and not to `ExtraHelpBuffScript`. Companion weapons retain their own authored muzzles. If an older scene has not yet serialized the anchor, the player path falls back to `headend.position + player.forward * 0.35f`; if no visible mouth exists, it preserves the authored position.

### Make scene and project-setting changes one reversible operation

Capture the original Build Settings list for exception recovery and register the real `EditorBuildSettings` singleton in the same Undo group:

```csharp
EditorBuildSettingsScene[] before = EditorBuildSettings.scenes;

Undo.RegisterCompleteObjectUndo(
    buildSettings,
    "Configure Noryangjin Build Scenes");
```

Normalize the result by removing every existing Forward and Noryangjin entry, then insert one enabled Forward entry at index `0` and one enabled Noryangjin entry at index `1`. Preserve unrelated scenes after them. On failure, restore the captured array and revert the complete Undo group before saving.

Keep `SettingsManager` scene-local. Making its GameObject persistent would also preserve the shared Managers root and create stale `TimeManager` or `BulletPooler` ownership across scene loads.

### Treat a route turn as a temporary Rigidbody state

Forward and lateral movement use the player's current flattened `transform.forward` and `transform.right`, not fixed world axes. At turn start, save the exact Rigidbody constraints, locked position, velocity, and angular velocity. Lock X/Z position while temporarily allowing Y rotation:

```csharp
constraintsBeforeWorldYawTurn = playerRigidbody.constraints;

RigidbodyConstraints turnConstraints = constraintsBeforeWorldYawTurn;
turnConstraints |= RigidbodyConstraints.FreezePositionX |
                   RigidbodyConstraints.FreezePositionZ;
turnConstraints &= ~RigidbodyConstraints.FreezeRotationY;

playerRigidbody.linearVelocity = Vector3.zero;
playerRigidbody.angularVelocity = Vector3.zero;
playerRigidbody.constraints = turnConstraints;
```

During the yaw interpolation, apply the locked position to both the Rigidbody and Transform. On completion or cancellation, restore the exact saved constraints and rebase the lane origin and local right vector.

### Make the projectile root the lifecycle owner

The authored muzzle's positive local Y axis is the travel direction, but the composed player's spawn position comes from the visible-mouth anchor. Detach the rented root first, then overwrite its complete pose before the child script captures ownership:

```csharp
Vector3 direction = bulletPos.up.normalized;
Vector3 spawnPosition = ResolveProjectileSpawnPosition(bulletPos);

bullet.transform.parent = null;
bullet.transform.position = spawnPosition;
bullet.transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
bullet.GetComponentInChildren<BulletScript>().SetDirection(direction);
```

Both Water and Bomb prefabs use root forward as their travel axis; their authored `GFX` transform makes `GFX.up` align with that direction. A fresh root rotation therefore fixes both prefabs without projectile-specific Euler offsets.

`BulletScript` lives on `GFX`, but `BulletPooler.reverse` registers the prefab root. Capture that root after detachment and use the same identity for motion, range, and every return path:

```csharp
public void SetDirection(Vector3 dir)
{
    direction = dir;
    projectileRoot = transform.root;
    spawnPosition = projectileRoot.position;
}

private Transform GetProjectileTransform()
{
    return projectileRoot != null ? projectileRoot : transform;
}
```

Range expiry and `EnemyTag`, `BarrelTag`, or `Obstacle` collisions must pass `GetProjectileTransform().gameObject` to the pool. Returning the registered root preserves its `GFX` and `BulletScript` and makes the exact same root reusable.

### Verify durable outcomes instead of transient state

Use the authored `0.5` second turn duration for a mid-turn assertion and bound the completion loop:

```csharp
yield return new WaitForFixedUpdate();
Assert.That(player.IsWorldYawTurnActive, Is.True);

int remainingFrames = 80;
while (player.IsWorldYawTurnActive && remainingFrames-- > 0)
    yield return new WaitForFixedUpdate();
```

Increment a read-only counter only after the weapon successfully rents a projectile. This proves attack initiation, but not orientation or lifecycle correctness:

```csharp
GameObject bullet = bulletPooler.Get(bulletKind, transform);
if (bullet != null)
{
    TotalProjectilesSpawned++;
    // Initialize root pose and child direction.
}
```

For moving-object spatial assertions, capture both positions in the successful firing call:

```csharp
LastProjectileSpawnPosition = spawnPosition;
LastVisibleMouthPositionAtSpawn = visiblePlayerMouth.position;
```

Wait for the spawn counter, then compare those same-frame snapshots. Do not compare the recorded spawn with the mouth's live transform after yielding, because the player and animated head have already moved.

Projectile verification also uses the actual Water and Bomb prefabs. It asserts that a second rental overwrites stale rotation, `root.forward` and `GFX.up` align with travel, range and collision returns enqueue the registered root, and the same intact root is rented again.

The end-to-end test loads the saved Noryangjin scene and verifies pre-start/shop UI, the direct visible `Original`, movement, the `Walk` state, advancing normalized time, a changing rendered `backleg` pose, a successful projectile rent, projectile root/travel alignment, upgrade helpers, turn locking, final yaw, and movement resuming along the new local forward.

After Unity verification, inspect the repository diff. Restore only test-generated files that were known clean before the run; never wholesale-restore an authored scene or prefab that was already modified by the user.

## Why This Works

The installer uses the configured reference scene as the source of truth while keeping every mutation scoped to one explicit target scene. Structural ownership, rather than a repeated name alone, identifies the visual that must survive. Scene objects and Build Settings participate in the same Undo lifecycle, with a separate snapshot covering exceptions.

The visible hierarchy owns the controller that deforms its rendered generic rig, and structural preference prevents animation state from being applied only to a hidden clone.

The visible mouth owns only player projectile origin, while the authored weapon muzzle continues to own travel direction. This separation removes the stale hidden-model offset without changing turn-relative firing, spread, or companion weapon behavior. Parenting one idempotent anchor to `headend` keeps the origin attached to the animated pose.

The turn is an explicit temporary physics state: movement and velocity are locked, only the required rotation bit changes, and every original constraint returns afterward. Rebasing the route frame makes the next segment consistent for the player and helpers.

For projectiles, one registered root owns rotation, motion, distance, and pool return:

```text
pooled root = rotation owner = movement owner = range origin = returned object
```

The tests assert stable boundaries—actual rendered-bone motion, root/travel alignment, same-root reuse after range and collision returns, an active transition, exact final yaw, and route-relative resumed displacement—rather than short-lived object visibility or an assumed number of fixed steps per rendered frame.

## Prevention

- Scope Unity editor installers to an exact target scene and validate source dependencies before mutation.
- Prefer configured scene instances when bare prefabs would lose serialized cross-object references.
- Resolve repeated hierarchy names using direct-child and ownership rules, then test repeated installation.
- After replacing or hiding a visual hierarchy, audit serialized Transform references used by weapons, effects, audio, and collision.
- Model projectile origin and direction as separate inputs when the visible model and authored weapon rig own different parts of firing.
- For moving-object tests, capture both sides of a spatial relationship during the same event and frame.
- Verify a preserved visible model's Animator, controller, enabled state, actual animation state, and rendered-bone pose—not only renderer visibility.
- Remember that editor code does not retrofit a serialized scene until the installer executes and saves it.
- Include non-scene project settings in the same Undo group and retain an exception-recovery snapshot.
- Capture and restore exact Rigidbody constraints; change only the bits required by the temporary mode.
- Calculate forward movement, lateral motion, and companion offsets from one route frame.
- Treat the pooled prefab root as the single owner of pose, movement, range origin, and return identity.
- Initialize pooled projectiles in this order: rent root, detach, set position, overwrite rotation, then let child scripts capture the root.
- Test actual projectile prefabs through range expiry and collision returns, then assert the same root and visual hierarchy are reusable.
- Wait for the editor test assembly timestamp to advance after Unity refresh before trusting a rerun.
- Run Unity-generated runtime and editor solution builds sequentially because they share intermediate paths.
- Record a repository baseline before tests that can serialize scenes, fonts, atlases, or generated assets.

## Verification Evidence

- `NoryangjinTurnSpotTests`: 24/24 passed.
  - Existing misconfigured Animators are repaired without duplication.
  - Repeated installation reuses one `ProjectileMuzzle` under the visible `headend`, aligned `0.35` units along player forward.
  - Water and Bomb roots overwrite stale rotation and align both `root.forward` and `GFX.up` with travel.
  - Range, `EnemyTag`, and `Obstacle` returns re-rent the same registered root with `GFX` and `BulletScript` intact.
- `NoryangjinGameplayIntegrationTests`: 1/1 passed against the saved Noryangjin scene.
  - The visible direct `Original` enters `Walk`, advances its animation, and changes a rendered generic-rig `backleg` pose.
  - The hidden Forward muzzle is not used as player origin; same-frame snapshots keep the projectile `0.3` to `0.4` units from the moving visible mouth.
  - Active projectile roots point along their private travel directions.
- The saved scene contains exactly one `ProjectileMuzzle` under the visible `Original/headend`.
- Unity console after the targeted integration run: 0 errors, 0 warnings.
- `dotnet build Assembly-CSharp.csproj -nologo`: 5 pre-existing warnings, 0 errors.
- `dotnet build Assembly-CSharp-Editor.csproj -nologo`: 0 warnings, 0 errors.
- `tools/validate-agent-harness.ps1`: passed with MCP Unity port `8120`.
- The project-wide EditMode run passed 252/258; the six remaining failures are unrelated pre-existing MapTool tests, so they are not counted as animation or projectile regressions.

## Related Issues

- [Generate Unity Map-Tool Sibling Scenes with Fail-Closed Verification](../workflow-issues/generate-unity-map-tool-sibling-scenes-fail-closed-2026-07-15.md)
- [Protect Active Unity Scenes from Broad EditMode Test Runs](../workflow-issues/protect-active-unity-scenes-from-broad-editmode-test-runs-2026-07-18.md)
- [Advance Unity map tool cursors after applying road turns](../logic-errors/advance-unity-map-tool-cursor-after-road-turn-2026-06-01.md)
- [Preserve prefab root transforms in Noryangjin map tool placement](../logic-errors/preserve-prefab-transform-in-noryangjin-map-tool-placement-2026-06-02.md)
- [Continue Unity map-tool layouts by placement-specific geometry](../design-patterns/continue-map-tool-layouts-by-selected-renderer-bounds-2026-07-19.md)
