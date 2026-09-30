---
title: Verify bullet lanes and reward height in live play
date: 2026-09-28
category: workflow-issues
module: Noryangjin live gameplay review
problem_type: workflow_issue
component: testing_framework
severity: high
applies_when:
  - Static presentation fixtures pass but a live course fails at a new encounter
  - Generated models replace legacy collision bodies or drop rewards from animated parts
  - Automated steering reports a failure that may be caused by the test driver
tags: [unity, playtest, collision, rewards, occlusion, test-harness]
---

# Verify bullet lanes and reward height in live play

## Context

The user required both9999 test power and fast lateral movement, then asked for a live review, a plan and implementation of the findings. Two baseline runs died at the auction wall despite the apparent high attack. Native geometry inspection showed that bullets passed between the four bidders while the much wider shark collided. Separate captures showed unreachable-height coins covering the screen, an inverted TV picture, solid-looking hose water, and a shutter whose visible opening contradicted its timer.

## Guidance

1. **Audit all interacting widths.** The four auction actors occupied lateral−1.8/−0.6/+0.6/+1.8 with capsule radius0.44. Central bullets at about y1m fitted through the0.32m middle gap; the shark did not. All four enemies retained full health just before contact. Place a center actor in a three-person wall instead of altering global enemy HP exchange. A native observer then recorded the center enemy reaching zero HP and the player passing. High debug attack alone cannot test this geometry.
2. **Trace reward positions through the whole spawn chain.** `CoinPickup.Spawn` adds1.1m and its bob adds0.28m, while automatic collection requires a vertical gap under2.5m. Spawning from the upper cargo height violated that gate. Anchor revamp reward Y to the breakable's floor, preserve XZ scatter and the exact `ApplyCoinBonus` amount, and shrink only the visual child. Do not scale the pickup collider or silently award missed coins.
3. **Separate representation from mechanics.** Fix the TV with explicit front-plane UVs; put warning geometry above the replacement floor; replace hose cylinders with animated threads/drops. Preserve active spray timing and damage. Maintain shark clearance while shutter time is positive, and arm the closed blocker when the short closing motion finishes.
4. **Review hidden geometry at boundaries.** Removing combined old-road renderers also removes their planks. The first cold-store pass exposed water before/after the new floor. Extend the replacement surface over the hidden road interval and the camera's visible approach. Several thin curtain strips collectively obscure the view even when no single strip passes the severe-occlusion ray test: evaluate the complete authored group for the opt-in hard-hide path, and restore its original state.
5. **Diagnose the driver separately from the game.** A later3x run hit a fallen lamp because the automated driver avoided its root at x247.2 while its active collider was centered at x248.38 and extended1.21m. Choose the nearest active collider bounds for test steering. Preserve this failed run and label it as a driver limitation; do not weaken the game obstacle to make the bot win.
6. **Treat debug settings as part of the receipt.** Use the existing Editor override selector, record9999 starting HP/attack and sensitivity31.25, and leave both requested toggles ON after restoring the66-key save snapshot. Normal bonus changes may raise the numbers above9999; the option is not invulnerability or a per-frame health pin. Debug outcomes are functionality/presentation evidence, not normal-balance or natural-player win rates.

## Evidence

Baseline folders: `play-debug-review-inside-050620` and `play-debug-review-outside-051550` under `tmp/image-previews/noryangjin-revamp-fix-2026-09-28/`. First corrected1x inside run completed in306.67s; the center-hit observer is `outputs/noryangjin-debug-review-2026-09-28/auction-052703.csv`. The later final-scene3x lamp collision is retained in `play-debug-final-inside-053619`.

`NoryangjinDebugReviewTests` first showed8 failures among14 cases, then passed14/14. Additional camera tests cover combined road colliders, hard-hidden shops, grouped curtains and restoration; existing uphill-support tests remain green. Final run and isolated close/coin receipts belong in `outputs/noryangjin-debug-review-2026-09-28/README.md`.

## Related

- [Reference interior and retargeted pose validation](validate-reference-interiors-and-retargeted-poses-in-native-camera-2026-09-28.md): moderate overlap on native presentation; this entry adds projectile/collector geometry and driver-vs-product diagnosis.
- [Branch validation beyond roots](validate-noryangjin-branches-beyond-trigger-and-root-position-2026-09-28.md).

Recorded with `ce-compound mode:headless`, sequentially under AGENTS.md. No external or session-history research was needed. The existing AGENTS.md solution-discovery rule remains sufficient.
