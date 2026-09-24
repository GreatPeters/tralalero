---
title: Keep player damage visible through hitches and recovery
date: 2026-09-23
category: ui-bugs
module: Player damage, recovery and combat presentation
problem_type: ui_bug
component: service_object
severity: high
symptoms:
  - "HP fell after a highway prop collision without readable damage feedback."
  - "A newly added screen notice expired before its first useful rendered frame after a hitch."
  - "Enemy popup bursts obscured player damage, and late healing could undo zero health."
root_cause: async_timing
resolution_type: code_fix
tags: [unity, damage, healing, ui-lifetime, pooling, game-over, playtest]
---

# Keep player damage visible through hitches and recovery

## Problem

The player could lose health without a readable response during motion or a long frame. Health presentation also depended on HUD cache comparisons, while several recovery paths could raise a dead player's health before the game-over observer ran.

## Symptoms

A controlled roadblock collision reduced normal player HP from 500 to 0. The old world number was brief and could be obscured. An initial screen-space replacement also failed: the retained `lifecycle-burst-heal-final` probe recorded 500 → 375, two damage notifications and an empty visible notice. The next probe, with the frame-lifetime correction, retained both the world number and footer notice.

## What didn't work

Moving the notice to a different anchor did not fix its lifetime. Subtracting the full `Time.unscaledDeltaTime` in the same frame as `Show` charged time that had elapsed before the hit existed. A startup or rendering hitch could therefore consume the entire new notification. Using one shared rolling pool also let heavy enemy fire replace the player's number, and individual numbers at the same position overlapped.

## Solution

`PlayerScript.ApplyDamage` reports the committed loss, typed cause and fatality before updating the HUD. The HUD's legacy delta fallback is suppressed for already reported damage, so repeated refreshes do not duplicate delivery. Same-window hits accumulate their exact floating-point amounts; rounding occurs only for display.

Both player indicators skip the receipt frame and cap presentation decay for later hitches:

```csharp
public static float VisibleFrameDelta(float unscaledDelta, bool receivedThisFrame)
    => receivedThisFrame ? 0f : Mathf.Clamp(unscaledDelta, 0f, .1f);
```

This affects visual lifetime only. Movement, projectiles, damage and game timers retain their normal clocks. The existing 64-popup allocation now reserves one slot for player loss and uses 63 for enemy/coin numbers. A 200-popup enemy burst cannot replace the player's number. The footer carries the amount and source without covering the tutorial or imminent road; mixed sources are explicitly identified as a combined burst.

Positive recovery goes through `PlayerScript.Heal`, which rejects dead or zero-health players, invalid amounts and reductions of existing overheal. Regeneration, recovery branches and holdout rewards use that transaction. Health bonuses cannot revive a dead player or grow capacity after death. Explicit run reset remains the revival path. `CanvasScript.YouWin` rejects zero health before awarding progress, and equal-health contact calls enemy death when both sides reach zero.

## Why this works

Health ownership, display delivery and visual lifetime are separate responsibilities. A correct HP subtraction does not prove that a player saw it. A rendered frame must get a chance to present a newly received hit, and a later heal or finish-line callback must not change a committed fatal outcome.

## Prevention

- Test a fresh notice with a large elapsed frame and test popup saturation, rather than only ordinary single hits.
- Combine exact amounts before rounding; `100.4 + 25.4` must display `126` consistently in both indicators.
- Verify real collider delivery as well as direct transaction tests. The roadblock and equal-health probes relocate real actors for isolated contact; they are not ordinary traversal or balance runs.
- Test healing, HP bonuses and finish-line completion immediately after fatal damage, before a separate UI observer can run.
- Keep normal-stat earned-progression cohorts separate from controlled projectile/contact fixtures. Restore preference existence as well as values after testing.

Verification: six damage-notification tests, five popup-pool tests and six healing-lifecycle tests passed. The trigger-level cases also ensure a dead player cannot consume a legacy health wall. Native probes verified roadblock 500 → 0 with one notice, mixed damage 500 → 375 with `-125 HP`, rejected postmortem heal/win, and physical 500/500 contact ending with both HP values at zero and enemy state `Dead`. Evidence and campaign results are indexed in the [campaign record](../../../map-concepts/campaign-balance-2026-09-23/README.md).

## Related

- [Bounded combat presentation pools](../performance-issues/prewarm-bounded-combat-presentation-pools-2026-09-13.md)
- [Verify runtime presentation and earned progression](../workflow-issues/verify-runtime-presentation-and-progression-beyond-numeric-checks-2026-09-10.md)
