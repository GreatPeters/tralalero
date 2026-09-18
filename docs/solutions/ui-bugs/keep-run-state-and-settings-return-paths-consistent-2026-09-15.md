---
title: Keep run state and settings return paths consistent
date: 2026-09-15
category: ui-bugs
module: Harbor settings and player presentation
problem_type: ui_bug
component: development_workflow
symptoms:
  - "Closing in-game settings left the shark in Idle while gameplay resumed."
  - "The obsolete score UI appeared after ResumeGame."
  - "The camera inherited player slope pitch through the visual hierarchy."
root_cause: logic_error
resolution_type: code_fix
severity: high
tags: [unity, settings, retry, animation, camera, lifecycle]
---

# Keep run state and settings return paths consistent

## Problem

New Harbor visuals reused old pause/resume callbacks whose presentation assumptions no longer held. A successful initial start did not prove settings return or scene-reload behavior.

## Symptoms

The native baseline showed Walk after start, Idle after pause, and Idle plus `legacyScore=True` after resume. `ResumeGame` restored the time flags and explicitly enabled scoreParent, but did not restore shark locomotion. Flat-ground camera measurements showed no bob, while the actual hierarchy and slope code showed that pitch changes propagated to the camera.

## What Didn't Work

- Styling the old pause panel without replacing its nested settings return flow retained the same state mismatch.
- Merely shrinking the human diving helmet left holes, floating geometry or a torso collar. A closed cap sized for the shark was authored instead.
- Checking a reload after leaving a run unattended confused a later hazard death with reset failure. The resulting exception also activated the Editor's Error Pause; API calls still succeeded while coroutines stopped advancing.
- Starting a verification coroutine on the outgoing Canvas could silently lose it during LoadScene.

## Solution

- Use one settings panel with explicit resume intent. Lobby close stays in the lobby; in-run close resumes gameplay. Block the start gesture while settings is visible.
- Derive shark Walk/Idle from live run/stationary state and clear death triggers/rebind only on reset. Keep the legacy score hidden whenever the modern HUD is configured.
- Reload into a prepared lobby; do not set running=true after scheduling LoadScene.
- Keep the camera independent of visual/pitch transforms. Follow yaw and gently smooth actual route height; snap on large resets.
- Start reload verification from SceneManager.sceneLoaded on the new Canvas, then allow Start to initialize. Freeze forward movement immediately after starting so later hazards cannot invalidate the reset check.
- Check EditorApplication.isPaused after a failed native assertion. Cancel stale probe coroutines and resume the Editor before scheduling a fresh bounded check; do not assume a reachable Pipeline means frames are advancing.

## Why This Works

The returning screen, gameplay time flags and animation state agree on whether a run exists. Visual effects no longer drive the camera transform. Verification measures the intended transition rather than a later gameplay event.

## Prevention

- Test initial start, pause/close, retry/reload and stationary combat separately.
- Inspect actual frames after saved-scene reload, including icon sprites; a null decorative sprite rendered as a white square despite passing unrelated state checks.
- Store original collider dimensions so rebuilding scene/prefab presentation cannot multiply hitbox growth again.
- Respect a scene collider override when a newly tuned prefab component is inherited. Four real0.67radius overrides were recovered from the before-scene, expanded to0.87, and saved with modifications recorded on both the collider and its tuning component. Only recording the collider is insufficient because runtime Awake reapplies the stored baseline.
- Preserve old chapter ownership as rank1 and use a price schema version so later catalog tuning survives UI rebuilding.

## Related Issues

- [Implementation and evidence](../../../map-concepts/harbor-polish-2026-09-15/README.md)
- [Connected UI activation and reload](../integration-issues/validate-connected-ui-through-activation-and-scene-reload-2026-09-15.md)
