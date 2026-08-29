---
title: Curve Owned Projectiles with Incremental Route-Turn Deltas
date: 2026-08-30
category: design-patterns
module: Unity Noryangjin route projectiles
problem_type: design_pattern
component: service_object
severity: medium
applies_when:
  - "A moving shooter rotates through authored route corners while its projectiles remain in flight."
  - "Only projectiles fired by one actor should inherit that actor's route rotation."
  - "Pooled projectiles must preserve speed and remaining lifetime while changing trajectory."
tags:
  - "unity"
  - "projectile"
  - "route-turn"
  - "owner-tracking"
  - "object-pooling"
  - "rotation-delta"
  - "noryangjin"
  - "integration-test"
---

# Curve Owned Projectiles with Incremental Route-Turn Deltas

## Context

Noryangjin route movement pauses the player at a corner and smoothly rotates the
player to an absolute world X/Y rotation. Newly fired missiles already use the
rotating muzzle direction, but missiles fired before entering the corner kept
their original world direction and left the route.

The desired behavior was a curved trajectory that follows the player's turn
without pulling the missile toward the player, restarting its lifetime, changing
its speed, or affecting enemy and helper projectiles.

## Guidance

Record the firing actor when a pooled projectile is initialized. Keep the
one-argument initializer as the ownerless path for non-player projectiles:

```csharp
public void SetDirection(Vector3 direction)
{
    SetDirection(direction, null);
}

public void SetDirection(Vector3 direction, PlayerScript owner)
{
    this.direction = direction;
    projectileRoot = transform.root;
    routeOwner = owner;
    elapsedDuration = 0f;
    ActiveProjectiles.Add(this);
}
```

At each turn step, compute the incremental rotation from the player's previous
rotation to the newly evaluated rotation:

```csharp
Quaternion currentRotation = playerRigidbody != null
    ? playerRigidbody.rotation
    : transform.rotation;
Quaternion rotationDelta = targetRotation * Quaternion.Inverse(currentRotation);

if (playerRigidbody != null)
{
    playerRigidbody.MoveRotation(targetRotation);
    playerRigidbody.rotation = targetRotation;
    transform.rotation = targetRotation;
}
else
{
    transform.rotation = targetRotation;
}

BulletScript.ApplyRouteTurn(this, rotationDelta);
```

Apply that delta only to active projectiles owned by the turning player. Rotate
the travel vector and projectile root in place:

```csharp
if (projectile.routeOwner != owner)
    continue;

projectile.direction = rotationDelta * projectile.direction;
Transform root = projectile.GetProjectileTransform();
root.rotation = rotationDelta * root.rotation;
```

Do not rotate the projectile position around the player. Its normal fixed-step
translation continues from the current position using the updated direction,
which produces the curve while preserving absolute speed and elapsed lifetime.

Register active projectiles in `OnEnable` and remove them in `OnDisable`. Clear
the owner and cached root on disable so the interval between pool activation and
the next `SetDirection` call cannot inherit a previous rental's owner.

## Why This Matters

Using the incremental delta makes immediate and timed turns share one rule. An
instant 90-degree turn applies one 90-degree delta; a smooth turn applies a
series of smaller deltas whose product reaches the same final direction.

Owner filtering prevents a global turn from redirecting enemy throws or helper
shots. Separating trajectory state from lifetime state also keeps pooling and
missile-duration upgrades independent from route presentation.

## When to Apply

- The shooter owns a local route frame that changes during gameplay.
- Projectiles should visually follow a corner after they have already spawned.
- Multiple actors share one projectile implementation or pool.
- The projectile root owns orientation while a child `BulletScript` owns travel
  state.

## Examples

Test both the isolated rule and the real firing chain:

- Immediate turn: an owned missile changes from forward to right, an ownerless
  missile stays forward, and elapsed duration remains unchanged.
- Timed turn: a two-step 90-degree turn produces 45 degrees after the first
  step and 90 degrees after the second.
- Installed-scene integration: fire through `WeaponScript`, rent from the real
  `BulletPooler`, start a player turn, and verify the rented root and private
  travel direction receive the same first-frame delta.

## Related

- [Make reference-scene gameplay composition transactional and idempotent in authored Unity maps](../integration-issues/transactional-reference-scene-gameplay-composition-2026-07-23.md)
- [Preserve externally assigned player position when resetting during a turn](../logic-errors/preserve-external-player-position-when-canceling-turn-on-reset-2026-07-26.md)
- [Restore consumed turn spots when restarting Unity runs in place](../integration-issues/restore-consumed-turn-spots-on-in-place-run-restart-2026-07-25.md)
