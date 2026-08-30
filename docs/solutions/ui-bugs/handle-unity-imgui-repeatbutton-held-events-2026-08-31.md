---
title: Handle Unity IMGUI RepeatButton held events without duplicate or inert clicks
date: 2026-08-31
category: ui-bugs
module: Unity Noryangjin map tooling
problem_type: ui_bug
component: tooling
symptoms:
  - "A short press on a one-cell movement button could move on both press and release."
  - "Restricting initialization to MouseDown made every one-cell direction button inert."
  - "Rejecting only the MouseUp frame still allowed a post-release Repaint to start a second step."
root_cause: wrong_api
resolution_type: code_fix
severity: medium
tags: [unity, unity-imgui, repeatbutton, mouse-input, editor-tooling, hold-repeat]
---

# Handle Unity IMGUI RepeatButton held events without duplicate or inert clicks

## Problem

The Noryangjin map tool uses `GUILayout.RepeatButton` for selected-object
one-cell movement. A short press must move exactly once, while a press held for
two seconds must begin repeating every 0.25 seconds.

The first implementation could interpret the release lifecycle as a new press
and move twice. One correction prevented all direction buttons from starting;
a second correction rejected `MouseUp` itself but still allowed a following
`Repaint` to restart the control.

## Symptoms

- A short click moved once on press and once again on release.
- After requiring `rawEventType == EventType.MouseDown` for initialization,
  clicking any direction button produced no movement.
- Direct Editor evaluation showed the first held `Repaint` was rejected and the
  active control remained unset: `firstHeldRepaint=False, activeControl=null`.
- Allowing the first held `Repaint` again restored movement, but a release still
  produced a second step because the release state survived beyond the single
  `MouseUp` event.

## What Didn't Work

Resetting the held control at the start of the `MouseUp` GUI pass allowed the
same release pass to look like a new control activation when
`RepeatButton` still returned `true`.

The next fix assumed that the first pass where `RepeatButton` reports held must
be `MouseDown`:

```csharp
if (!string.Equals(activeControlId, controlId, StringComparison.Ordinal))
{
    if (rawEventType != EventType.MouseDown)
        return false;

    activeControlId = controlId;
    return true;
}
```

That assumption was false for this IMGUI control path. The first held pass can
be `Repaint`, so the control ID was never initialized and every later pass was
rejected for the same reason.

Rejecting only `rawEventType == EventType.MouseUp` was also incomplete. The
window cleared the active control on `MouseUp`; if `RepeatButton` still reported
held on a later repaint, that repaint saw no active ID and initialized a second
step. The release is a state transition, not a one-frame filter.

## Solution

Track a release latch independently from the active control ID. `MouseUp` and
`MouseLeaveWindow` block held evaluation until the next real `MouseDown`.
Every pointer boundary also resets the timing state:

```csharp
heldMoveBlockedUntilNextMouseDown = UpdateHeldMoveBlockState(
    heldMoveBlockedUntilNextMouseDown,
    currentEvent.rawType,
    currentEvent.type);

if (currentEvent.rawType == EventType.MouseDown ||
    currentEvent.rawType == EventType.MouseUp ||
    currentEvent.type == EventType.MouseLeaveWindow)
{
    ResetHeldMove();
}
```

Pass both the widget's held result and the release latch into the timing helper:

```csharp
if (!held || blockedUntilNextMouseDown || string.IsNullOrEmpty(controlId))
    return false;

if (!string.Equals(activeControlId, controlId, StringComparison.Ordinal))
{
    activeControlId = controlId;
    nextTriggerTime = currentTime + SelectedObjectMoveHoldDelaySeconds;
    return true;
}

if (currentTime < nextTriggerTime)
    return false;

nextTriggerTime = currentTime + SelectedObjectMoveRepeatIntervalSeconds;
return true;
```

The resulting contract is:

- first held pass: one movement step;
- release pass: no movement step;
- held for less than two seconds: no additional step;
- held for two seconds or longer: repeat every 0.25 seconds.

## Why This Works

The release latch remains set through `MouseUp` and every following repaint, so
no post-release frame can initialize another step. Only a new physical
`MouseDown` clears the latch. The timing helper no longer guesses which IMGUI
event type represents the first held pass; it starts whenever `RepeatButton`
reports held and the current press lifecycle is unblocked.

The three pure held-move regression methods passed when invoked directly in the
connected Editor. They cover immediate movement, no immediate second step,
release plus post-release repaint, the next real press, the two-second
threshold, and the 0.25-second repeat interval. Unity script recompilation and the
`Assembly-CSharp-Editor.csproj` build also completed with no errors. The normal
Test Runner was intentionally not started because the authored map scene had
unsaved changes.

## Prevention

- Do not assume that an IMGUI widget's first active return occurs on
  `EventType.MouseDown`; test the event types the widget actually exposes.
- Model press and release as a lifecycle. A `MouseUp` guard that lasts for only
  one event does not protect against delayed IMGUI repaints.
- Test first-held `Repaint`, `MouseUp`, post-release `Repaint`, the next
  `MouseDown`, the two-second boundary, and the 0.25-second repeat interval
  together. A timing-only test would miss both duplicate and inert clicks.
- When an authored Unity scene is dirty, invoke pure regression methods or
  evaluate the isolated state machine directly instead of starting the
  in-process Test Runner.

## Related Issues

- [Keep route-aligned enemy editor state synchronized with placement](../logic-errors/keep-route-aligned-enemy-previews-synchronized-2026-08-03.md)
- [Advance Unity map tool cursors after applying road turns](../logic-errors/advance-unity-map-tool-cursor-after-road-turn-2026-06-01.md)
