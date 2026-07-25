---
title: "Restore consumed turn spots when restarting Unity runs in place"
date: 2026-07-25
category: integration-issues
module: Unity Noryangjin turn spot runtime
problem_type: integration_issue
component: service_object
symptoms:
  - "A turn spot worked once, then remained unavailable after the player restarted the run."
  - "GameManager restarted gameplay without reloading the scene, so a deactivated turn spot retained its inactive state."
  - "Rejected turn requests still needed to leave the spot available for a later valid activation."
root_cause: logic_error
resolution_type: code_fix
severity: medium
tags:
  - unity
  - noryangjin
  - turn-spot
  - run-lifecycle
  - state-reset
  - game-manager
---

# Restore consumed turn spots when restarting Unity runs in place

## Problem

`NoryangjinTurnSpot` should activate only once per run. Disabling its
`GameObject` after `PlayerScript.RequestWorldYawTurn` succeeded implemented that
local rule, but the game-over and stage-clear paths reuse the loaded scene. A
disabled spot therefore remained unavailable when the player started the next
run.

## Symptoms

- The first accepted turn disabled the spot as intended.
- Restarting from the tap-to-play screen did not recreate the scene object.
- The next run reached the same corner without an active turn trigger.
- A rejected turn request had to remain retryable instead of consuming the spot.

## What Didn't Work

Disabling the object without recording run ownership made the state permanent
for the lifetime of the loaded scene:

```csharp
if (accepted)
    gameObject.SetActive(false);
```

The inactive component cannot reactivate itself, and resetting every
`NoryangjinTurnSpot` found in the scene would also enable spots that were
intentionally authored inactive. Consumption and restoration needed to use the
same explicit set.

## Solution

Track only successfully consumed spots, disable them immediately, and restore
that tracked set at the next run boundary:

```csharp
private static readonly HashSet<NoryangjinTurnSpot> ConsumedTurnSpots = new();

internal bool TryActivate(PlayerScript player)
{
    bool accepted = player != null &&
                    player.RequestWorldYawTurn(
                        targetYawDegrees,
                        turnDurationSeconds,
                        this);
    if (accepted)
    {
        ConsumedTurnSpots.Add(this);
        gameObject.SetActive(false);
    }

    return accepted;
}

public static void ResetAllForNewRun()
{
    foreach (NoryangjinTurnSpot turnSpot in ConsumedTurnSpots)
    {
        if (turnSpot != null)
            turnSpot.ResetForNewRun();
    }

    ConsumedTurnSpots.Clear();
}
```

Call the reset from the shared transition into active gameplay, before movement
and attack resume:

```csharp
public void OnTapToPlay()
{
    ShowTapUI(false);
    NoryangjinTurnSpot.ResetAllForNewRun();
    SetGameRunning(true);
    ApplyUpgradeExtraHelps();
}
```

The regression tests cover both sides of the transaction:

- an accepted request applies the target yaw, disables the spot, and is restored
  by the next-run reset;
- a rejected request leaves the spot active.

## Why This Works

The static set survives while its members are inactive, so the run lifecycle can
reach objects that no longer execute callbacks. Adding a spot only after the
turn request succeeds keeps consumption transactional. Clearing the set after
restoration prevents stale spots from being processed again and avoids enabling
unrelated scene objects that were authored inactive.

`GameManager.OnTapToPlay` is the common boundary for initial play, game-over
retry, and the next stage. Resetting immediately before `SetGameRunning(true)`
ensures every run starts with its previously consumed route triggers restored.

## Prevention

- Treat one-shot scene objects as once per run when replay does not reload the
  scene.
- Pair every persistent disable or consume action with an explicit run-boundary
  reset.
- Record consumption only after the requested gameplay action succeeds.
- Restore only objects recorded as consumed; do not globally enable every
  instance of the component.
- Test accepted, rejected, consumed, and next-run restoration paths together.
- Run Unity-generated runtime and editor solution builds sequentially because
  they share intermediate output paths.

## Verification

- `dotnet build Assembly-CSharp.csproj -nologo`: passed with 0 errors.
- `dotnet build Assembly-CSharp-Editor.csproj -nologo`: passed with 0 errors and
  pre-existing warnings only.
- `tools/validate-agent-harness.ps1`: passed.
- The focused Unity MCP test requests exceeded the editor transport timeout, so
  no Unity Test Runner pass result is claimed.

## Related Issues

- [Make reference-scene gameplay composition transactional and idempotent in authored Unity maps](transactional-reference-scene-gameplay-composition-2026-07-23.md)
- [Generate Unity Map-Tool Sibling Scenes with Fail-Closed Verification](../workflow-issues/generate-unity-map-tool-sibling-scenes-fail-closed-2026-07-15.md)
