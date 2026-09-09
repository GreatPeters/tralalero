---
title: Keep map-tool test speed in editor session state
date: 2026-09-05
category: developer-experience
module: Noryangjin map-tool convenience controls
problem_type: developer_experience
component: tooling
severity: low
applies_when:
  - "An author wants faster local Unity Play Mode testing without changing shipped balance."
tags: [unity, map-tool, time-scale, session-state, editor, testing]
---

# Keep map-tool test speed in editor session state

## Context

The map author needed a 3x test-speed control beside existing convenience tools. The game already owns a separate `TimeManager.timeFactor` for running/paused state; changing that or increasing the physics step would change the behavior being tested.

## Guidance

`NoryangjinMapToolTestSpeed` lives in an Editor folder, uses `SessionState` for the 1x/3x selection, and applies `Time.timeScale` only while a known map-tool scene is playing. Its initialization callback survives normal Play Mode domain reloads. The active selection applies immediately during play and on the next Play Mode entry when selected in Edit Mode.

Keep the selected preference distinct from the actual global clock. On Play Mode exit, restore actual `Time.timeScale=1` only if this helper applied a value. Retain the chosen session selection for the next test. Do not continuously overwrite the clock each frame, write the choice to a scene/prefab, modify `Time.fixedDeltaTime`, or change the gameplay pause multiplier.

The window's `편의 > 테스트 속도 (TimeScale)` toolbar displays both the selected preset and the current/next effective value. It calls the same internal helper that tests and editor automation can invoke.

## Why This Matters

Faster physics cadence with the same fixed step keeps per-step movement and collision sampling unchanged. Keeping the control editor-only prevents a temporary test preference from entering a player build. Separate preference and clock state make automatic cleanup compatible with convenient repeated tests.

## When to Apply

Use this for local authoring controls, not shipping difficulty or player-facing time effects. The selected speed is remembered for the current editor session, not as a persistent project preference.

## Examples

- Seven EditMode tests passed for scene/play guards, supported values and unchanged edit-time clock, fixed step and gameplay factor.
- Live verification entered SR18 with scale 3, switched to 1 and back to 3, then exited with actual scale 1 and selected scale 3. Fixed step remained approximately 0.02.
- Pipeline eval runs in a separate assembly: direct references to internal helper/enum types failed. Narrow reflection through the existing Editor assembly invoked the same implementation without broadening its visibility.
- A desktop pixel capture first included an occluding window, then produced a black image after focus changes. Those images were not treated as complete UI proof; runtime state and the toolbar's selected value were verified separately.

## Related

- [Map-tool usage](../../noryangjin-gameplay-maptool.md)
