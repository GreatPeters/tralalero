---
title: "Preserve externally assigned player position when resetting during a turn"
date: 2026-07-26
category: logic-errors
module: Unity player movement and reset lifecycle
problem_type: logic_error
component: service_object
symptoms:
  - "Resetting the player during a timed turn moved the player back to the turn's original locked position."
  - "GameManager assigned the correct spawn position, but PlayerScript.ResetState silently overwrote it."
root_cause: logic_error
resolution_type: code_fix
severity: medium
tags:
  - unity
  - player-reset
  - turn-state
  - spawn-position
  - state-cleanup
  - regression-test
---

# Preserve externally assigned player position when resetting during a turn

## Problem

Restarting while a timed player turn was active could undo the newly assigned
spawn position. `GameManager.ResetPlayerToSpawn` moved the player first and then
called `PlayerScript.ResetState`, but reset canceled the turn and restored the
older `worldYawTurnLockedPosition`.

## Symptoms

- The bug occurred only while a nonzero-duration turn remained active.
- The player briefly received `playerSpawnPoint.position`, then returned to the
  point where the interrupted turn began.
- The active turn and temporary Rigidbody constraints still needed cleanup.

## What Didn't Work

Removing `CancelWorldYawTurn` from reset would preserve the teleport but leave
the turn state and temporary constraints active. Changing cancellation globally
would also weaken valid death, game-over, disable, and ordinary cancellation
paths that intentionally restore the locked turn position.

## Solution

Treat the position present at the reset boundary as caller-owned authoritative
state. Capture it, perform the complete turn cleanup, then reapply it through the
helper that synchronizes both Transform and Rigidbody:

```csharp
public void ResetState()
{
    Vector3 resetPosition = transform.position;
    CancelWorldYawTurn();
    ApplyPlayerPosition(resetPosition);

    isDead = false;
    winDancePlayed = false;
    // Remaining reset state...
}
```

The regression test starts a timed X/Y turn, assigns a different position as a
restart would, calls `ResetState`, and checks all three invariants:

```csharp
player.ResetState();

Assert.That(player.IsWorldYawTurnActive, Is.False);
Assert.That(player.transform.position, Is.EqualTo(expectedSpawnPosition));
Assert.That(rigidbody.constraints, Is.EqualTo(originalConstraints));
```

## Why This Works

`CancelWorldYawTurn` remains the single owner of turn-state cleanup and exact
constraint restoration. Reapplying the position afterward replaces only its
stale positional side effect. Using `ApplyPlayerPosition` keeps
`Transform.position` and `Rigidbody.position` aligned.

The ownership order is explicit:

```text
capture caller-assigned position
-> cancel transient turn state
-> restore caller-assigned position
-> continue reset
```

## Prevention

- Treat reset and cancellation methods as state-ownership boundaries.
- Preserve newer caller-owned state around cleanup that necessarily restores an
  older snapshot.
- Keep Transform and Rigidbody position updates behind one synchronization
  helper.
- Test interrupted timed behavior at restart boundaries, not only immediate and
  normally completed turns.
- Verify active state, final position, and exact original constraints together.
- Recompile Unity scripts before trusting a Test Runner result after editor test
  changes; stale test assemblies can report an older test count.

## Verification

- Unity script recompilation: 0 warnings.
- `NoryangjinTurnSpotTests`: 34 passed, 0 failed.
- `dotnet build Assembly-CSharp.csproj -nologo`: 0 errors.
- `dotnet build Assembly-CSharp-Editor.csproj -nologo`: 0 errors.
- `tools/validate-agent-harness.ps1`: passed with MCP Unity port `8120`.

## Related Issues

- [Make reference-scene gameplay composition transactional and idempotent in authored Unity maps](../integration-issues/transactional-reference-scene-gameplay-composition-2026-07-23.md)
- [Restore consumed turn spots when restarting Unity runs in place](../integration-issues/restore-consumed-turn-spots-on-in-place-run-restart-2026-07-25.md)
