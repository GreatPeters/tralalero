---
title: Keep animated hazard contact active through landing
date: 2026-09-20
category: integration-issues
module: Unity Seagull hazard
problem_type: integration_issue
component: service_object
severity: medium
symptoms:
  - "Visible wing contact could miss a small fixed trigger."
  - "The bird became harmless immediately after landing."
  - "Fixed damage was weak at higher player health and gave no spin feedback."
  - "Correct wing contact still felt unavoidable with a road-wide shadow and oversized bird."
root_cause: async_timing
resolution_type: code_fix
tags: [unity, seagull, animated-collider, contact, physics, health, spin, telegraph, scale, dodge]
---

# Keep animated hazard contact active through landing

## Problem

The seagull reused a falling-balloon flow. A small static cube did not follow its wings, and `OnBalloonImpact` disabled contact as soon as a render-frame tween completed. Physics could lose the next contact opportunity, especially for a player reaching the landed bird.

## Symptoms

The user reported unclear contact and no meaningful penalty. The serialized collider was 1.5m across while the animated wings could span roughly 5m. Old damage subtracted a flat 20; the latest request was two visible revolutions plus approximately 20% health.

A later user correction rejected the giant shadow as impossible to evade. That scene override produced 13.824m from a 10.24-unit sprite, despite the smaller source-prefab warning. Matching colliders to a 5m bird solved missed contact but did not make the encounter fair.

## What Didn't Work

- Root Rigidbody ownership alone was insufficient; the collider's location and active lifetime also mattered.
- A generic `GetComponentInParent<PlayerScript>` accepted invisible child/weapon colliders as the player body.
- A probe-only stationary lock with a missing police array was not enough isolation: already-running weapon coroutines survived disabling their components and called the fake holdout. The final probe explicitly stops them.

## Solution

Generate bone-local trigger boxes from the existing sitting and flying skin poses; preserve one root kinematic Rigidbody and no nested bodies. Keep contact active through descent, landing and departure. Turn all shapes off after one accepted contact or final departure, and reset that consumed state on the next run.

Resolve the exact player-root collider, require visible/active bird contact and a running game, then consume the encounter before applying damage. Use maximum HP × 20% consistently with the project's other percentage hazard. Reuse the existing 720-degree cosmetic spin and clear standalone spins during player retry.

Use the same initializer for OnEnable and initial Start so late placement assignments retain the correct impact point. Drive the seagull sequence on the simulation clock rather than allowing a visual tween to finish while gameplay is paused.

Measure the full authored warning in world units, including sprite dimensions and scene overrides. The first avoidable-size pass grew from 0.6m to 1.6m with rig/contact scale 1.4. The user's later +50% request sets the maximum to 2.4m and rig/contact scale to 2.1, with a correspondingly adjusted landing offset. Remove old authoring overrides so regeneration preserves the correction. Do not shrink only the shadow while retaining a large invisible danger area.

The next requested relative increases are different: +50% bird and +30% warning give rig/contact scale 3.15 and maximum warning diameter 3.12m. Record each baseline explicitly instead of compounding the wrong percentage. Keep geometry and real avoidance checks alongside these values.

Warning readability requires checking the source texture alpha too: this black sprite peaks at 0.376 despite a fully opaque SpriteRenderer. A scoped transparent material multiplies the original alpha by 2.2, producing a stronger core without changing shared art.

The subsequent 21m/2.1s warning was rejected because landing still completed after the runner arrived. An eventual collision during descent is not proof of correct arrival timing. The current 28m trigger, 1.0s warning and 0.4s descent complete landing roughly 15m ahead at the verified speed. Replace the fixed short grounded lifetime with a minimum hold followed by waiting for signed road progress past the bird (or consumed contact). This permits earlier landing without premature departure. Test different headings so lateral dodges and route rotation cannot be confused with passing.

For an impact knockback, consume contact and stop the hazard's old movement coroutines before starting the bird arc and player response. Otherwise a descent or ordinary departure can overwrite the hit position or hide the bird early. The current arc owns the root while the Animator holds a sampled spread-wing pose; normal flapping during three fast tumbles obscured the silhouette in the first native capture. Restore the Animator on initialization. Check an injected mid-descent transition, pause and retry as well as real grounded contact, and measure accumulated rotation rather than the final equivalent quaternion.

## Why This Works

Physical shape, visible phase and callback ownership now agree. A landed bird remains a real target for the next physics update. The shared hit flag makes multiple wing/body callbacks one penalty rather than multiplying percent damage.

## Prevention

- Test moving contact, a real miss, and contact after landing separately.
- Start a dodge after the warning appears using the real input movement method. Include the player's body width in the available road space; a far-offset miss alone does not prove the encounter is reasonably avoidable.
- Check saved scene overrides as well as the prefab. Sample flying and sitting skin vertices inside the final warning. The final left/right probes moved 1.5m and retained full HP, while contact still caused one 20% penalty and 720 degrees of spin.
- Measure actual HP delta and accumulated visual rotation; a final identity quaternion alone cannot prove two revolutions.
- Record completed landing distance, runner passage and departure separately. Check the exact reference moment visually; early warning and passing damage tests do not prove that the bird is already grounded in time.
- Check the compound shapes' Rigidbody owner and reject player child colliders.
- Preserve source animation and regenerate the bone-local shapes when the bird mesh or rig changes.
- Stop asynchronous producers in gameplay probes, not just their Update callbacks.

## Related Issues

- [Measured implementation and evidence](../../../map-concepts/seagull-contact-2026-09-20/README.md)
- [Smaller warning and actual dodge evidence](../../../map-concepts/seagull-dodge-size-2026-09-20/README.md)
- [Latest contrast, lead time and 50% enlargement](../../../map-concepts/seagull-warning-readability-2026-09-20/README.md)
- [Completed landing before arrival and passage-based departure](../../../map-concepts/seagull-early-landing-2026-09-20/README.md)
- [Larger footprint and interruption-safe impact tumble](../../../map-concepts/seagull-impact-tumble-2026-09-20/README.md)
- [Earlier callback ownership and real-physics verification](../workflow-issues/verify-runtime-presentation-and-progression-beyond-numeric-checks-2026-09-10.md)
- [Native prefab layer and pivot checks](verify-combat-with-authored-prefab-layers-and-pivots-2026-09-20.md)
