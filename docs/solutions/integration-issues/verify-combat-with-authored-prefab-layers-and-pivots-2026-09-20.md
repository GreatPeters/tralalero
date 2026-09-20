---
title: Verify combat with authored prefab layers and grip pivots
date: 2026-09-20
category: integration-issues
module: Unity forward enemies and helpers
problem_type: integration_issue
component: service_object
severity: high
symptoms:
  - "Tungtung passed through authored enemies despite passing synthetic sweep tests."
  - "A guard's weapon detached visually when aimed or scaled."
  - "An imported ship projectile became nearly invisible at launch."
  - "A two-hand crate passed grip tests but jittered and launched before the visible push."
root_cause: config_error
resolution_type: code_fix
tags: [unity, helper, collision, prefab, layers, gun, grip, projectile, animation, outline]
---

# Verify combat with authored prefab layers and grip pivots

## Problem

Gameplay code assumed that named collision layers, transform pivots and mesh scales described the actual authored assets. Those assumptions survived synthetic tests but failed on real scene objects.

## Symptoms

- Forward enemies use Default, although pooled enemies use Enemy. An Enemy-only sweep found nothing in the real encounter.
- The guard's imported weapon origin sits near its front, far from the right-hand grip. Rotating or shrinking around that origin displaced the stock from the hands.
- CannonBall is a small imported mesh nested under a heavily scaled ship. Setting its detached localScale to one erased its visible world scale.

## What Didn't Work

- A passing sweep test whose synthetic enemy was explicitly assigned Enemy did not validate production content. Native prefab tests initially reported unchanged HP for all four contact cases.
- Guessing the gun's negative X end was its muzzle was wrong. Sampling the attack pose showed the right hand beside min-X, and max-X pointing forward. The sign alone was not the root issue; the grip pivot was.
- `GetComponent<Rigidbody>() ?? AddComponent<Rigidbody>()` encountered Unity's editor fake-null wrapper. Use Unity's `== null` before adding native components.
- A ramp traversal probe hit an unrelated bonus/shop trigger and paused. Isolate interactive triggers while retaining the real player movement, slope spots, height follower and gameplay camera.

## Solution

Sweep the helper's actual capsule over its movement segment, accepting Default and Enemy candidates and filtering them by `EnemyScript_space`. Resolve both HP values from their pre-contact values in one method used by physics callbacks and sweeps. Check current HP/death state before subsequent callbacks, including the equal-health case.

For the guard, retain +X as barrel direction, place the muzzle at mesh max-X, and anchor mesh min-X to the animated right hand after aiming. Restrict this correction to the attack state. Preserve the stock location when reducing the weapon's scale.

Clone carried projectiles, preserve lossyScale on detachment, enable their actual renderer/collider and add a Rigidbody with a Unity null check. Launch with a bounded lifetime on the same simulation clock. Keep the carried template for subsequent throws and run resets.

Validate the saved scenes after the workbook controller applies. Editing a scene alone cannot override runtime placement mode, disabled rows or tier health. Use model identity when a retained placement ID no longer names the instantiated character.

## Why This Works

The checks exercise the same assets and hierarchy as the game. Asset measurements establish semantic points (hand grip, barrel tip, world dimensions) instead of treating a raw transform origin as meaningful. Simultaneous HP exchange preserves large elite/boss remainders and kills both actors on a tie.

## Prevention

The same day's single-hand crate follow-up was rejected: keeping one rim point
on a hand did not stop the box swinging through the torso. It also did not satisfy
the user's explicit two-hand requirement. The final dependency direction is
body-space crate → two grip targets → both arms, with no crate parent under either
hand. Measure palm centres from the actual skin and account for short arm reach.
Preserve the crate's orientation at release; rotating a
wide box with a generic arrow-axis correction can reintroduce penetration.

The subsequent two-hand version also failed visually: restoring bones around an
active goalie clip left imported twists competing with the hand solver. Its
independent projectile timer preceded a visible forward push. The current fix
captures a quiet reference pose in the prefab, disables the Animator while alive
and uses one pose owner for the entire stroke and recovery. Death explicitly
restores the Animator. The crate must finish its forward stroke before release
is requested at the end of `LateUpdate`; timer expiry alone must not launch it.
A consumed flag ensures a single handoff, at the exact final carried transform.
See the [current motion evidence](../../../map-concepts/synced-crate-throw-2026-09-20/README.md).

Validate visible geometry through the whole lifecycle. A zero wrist-anchor error
and a positive head-bone distance are insufficient. This rig's Unity 6 renderer
has an imported scale of 100; `BakeMesh(mesh, true)` compensates for it when the
baked vertices are subsequently transformed by the renderer matrix. The observed
scale behavior matches the [Unity API documentation](https://docs.unity3d.com/ja/6000.0/ScriptReference/SkinnedMeshRenderer.BakeMesh.html).
Test both held and first-flight frames against baked torso/head points. The old
first-frame hiding workaround is superseded by restoring the stable reference at
enable and preventing the competing Animator from running while alive. Retain
the whole recorded timeline, including startup and repeated throws.

Stable palm points alone do not prove stable elbows, cloth or surface rendering.
The legacy `FlatKit/Stylized Surface With Outline` produced internal black
triangles on this bent rig. A native same-pose material comparison isolated it;
the scoped replacement uses the current surface shader without hull outline and
retains the source texture/color. See the existing
[material migration lesson](../workflow-issues/verify-unity-material-keywords-after-bulk-outline-conversions-2026-07-02.md).
Add imported-pose invariance and timer-before-final-pose tests, then record actual
launch progress, carry-to-flight position/rotation, resting elbow jitter and
pause/death transitions. The corrected three-shot run had no early launch or
handoff jump; all 52 focused regression tests passed.

Also distinguish art direction from targeting: the user required a ship to stay
facing the road. Turning its hull toward the moving player at launch contradicted
that requirement even with mathematically valid predictive aim. The saved waiting
heading and actual FirePos-forward trajectory are now checked together.

- Pair logic tests with real prefab/scene probes. Include both Default and Enemy cases.
- Log incoming and outgoing HP, dead state and collider state, not only whether a trigger fired.
- Measure bone positions and transformed mesh bounds in the actual attack pose before changing a weapon's axes or scale.
- Verify one run-start application in each affected map, then reload without saving audit-only changes.
- Keep baseline disk and unsaved scene snapshots before native authoring operations.

## Related Issues

- [Implementation and measured evidence](../../../map-concepts/combat-route-fixes-2026-09-20/README.md)
- [Owner-based route rotation](../design-patterns/curve-owned-projectiles-with-route-turn-deltas-2026-08-30.md): Boombardino now explicitly shares the player's route owner.
- [Slope movement and pitch](../design-patterns/separate-slope-pitch-from-paused-route-corners-2026-09-05.md)
- [Projectile pool return ordering](../runtime-errors/guard-projectile-return-before-deactivation-2026-09-10.md)
