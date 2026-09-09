---
title: Guard projectile return before deactivation and track queue membership
date: 2026-09-10
category: runtime-errors
module: Projectile pooling
problem_type: runtime_error
component: service_object
symptoms:
  - "WeaponScript.ShootBullet threw NullReferenceException after sustained combat."
  - "Two registered Water projectile roots had lost their GFX and BulletScript."
root_cause: logic_error
resolution_type: code_fix
severity: high
tags: [unity, projectile, pooling, collision, lifecycle, idempotency]
---

# Guard projectile return before deactivation and track queue membership

## Problem

Real SR18 gameplay intermittently rented an empty projectile root. The prefab itself was intact; previous contacts had corrupted pooled instances. The same code serves Water and Bomb projectiles.

## Symptoms

- `WeaponScript.ShootBullet` could not find the child BulletScript and triggered Error Pause.
- A direct double-return reproducer kept GFX after the first return but removed it on the second.
- Returning a registered root twice also queued it twice, allowing two callers to receive the same active object.

## What Didn't Work

Spawn-count and no-contact probes did not exercise this lifecycle. The old shot counter incremented before initialization, so it could report a shot that threw. A null check at the weapon would hide the symptom but leave a corrupt pool.

## Solution

`BulletScript.ReturnToPool` now claims the return before calling code that deactivates the object:

```csharp
if (returnedToPool) return;
returnedToPool = true;
bulletPooler.ReturnObjectToPool_Bullet(GetProjectileTransform().gameObject);
```

OnDisable also marks it returned; OnEnable/new SetDirection clears the guard. A stale FixedUpdate does not move a returned projectile. The pool independently tracks queued roots with a HashSet: add exactly once on enqueue, remove on rental, reject duplicate or unregistered returns before reparenting or deactivating anything. In particular, never destroy an unrecognized child passed as a return request.

Move the successful shot counter after `SetDirection`. `ProjectilePoolLifetimeTests` covers double root returns, double projectile returns, distinct concurrent rentals and lifetime reset for both Water and Bomb. All4 cases failed against the old implementation and passed after the fix. Existing root-reuse and missile duration tests also passed.

## Why This Works

The first return calls SetActive(false), which invokes OnDisable and clears `projectileRoot`. A queued second return used to fall back to the GFX transform. The pool did not recognize that child and detached/destroyed it, while keeping its now-empty parent in the queue. Claiming before deactivation closes the callback window; queue membership prevents duplicate ownership even for callers that bypass BulletScript.

## Prevention

- Test repeated callbacks after deactivation, not only duplicate calls before it.
- Inspect every registered root for intact components during a continuous run with real collisions.
- Count successful initialization, not attempted work.
- Keep a baseline-health run separate from endurance runs with boosted health.

## Related Issues

- [Route ownership and root cache](../design-patterns/curve-owned-projectiles-with-route-turn-deltas-2026-08-30.md)
- [Continuous-play acceptance boundary](../design-patterns/validate-player-choice-and-post-turn-response-space-2026-09-09.md)
- [Failure evidence and causal reproducer](../../../map-concepts/sr18-live-playtest-2026-09-09/README.md)
