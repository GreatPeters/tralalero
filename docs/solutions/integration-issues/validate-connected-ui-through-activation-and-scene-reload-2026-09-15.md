---
title: Validate connected UI through activation and scene reload
date: 2026-09-15
last_updated: 2026-09-15
category: integration-issues
module: Harbor game UI and video integration
problem_type: integration_issue
component: development_workflow
symptoms:
  - "Video playback and scene-indicator assertions passed while the screen was entirely blank."
  - "Labels on inactive parchment modals and the HUD remained pale after authoring."
  - "Restored equipment appeared correctly but its bonus was absent from lobby totals."
root_cause: missing_validation
resolution_type: code_fix
severity: high
tags: [unity, ui, video, prefab-scale, inactive-hierarchy, persistence, actual-frame]
---

# Validate connected UI through activation and scene reload

## Problem

An inspected standalone UI template did not prove its behavior after nesting into existing scene UI. Initial integration checks covered data and playback but missed render transforms, saved property state and equipment initialization timing.

## Symptoms

- The standalone A prefab had a serialized zero root scale. Its Canvas had masked this authoring sharp edge in preview workflows. Removing that Canvas and nesting its RectTransform preserved the zero scale; the new full-screen blocking background was all that rendered.
- `GetComponentInParent<Image>()` did not discover inactive modal ancestors, so the authoring contrast rule chose cream text for light parchment. Existing component changes also needed explicit dirty/prefab recording and a save/reload check.
- Cosmetic effects were previously applied only by the run-start path, leaving the new current-stat summary incomplete at lobby startup and after an equip.

## What Didn't Work

- Trusting `VideoPlayer.isPlaying`, correct timestamps, active objects and indicator sprites without inspecting the actual frame. Four failed screenshots were each only a flat background despite passing logic checks.
- Adding Image to every Button: text-only link buttons already have a TMP Graphic and cannot accept another Graphic component.
- Entering Play Mode immediately after receiving test results. Runner scene cleanup was still pending and failed during Play, leaving an empty scene. The original scene was reopened and the affected suite rerun.

## Solution

- Normalize the nested video content's local scale/rotation/position after removing its standalone Canvas components. Keep one safe-area layer and an explicit full-screen raycast blocker.
- Retain text-only buttons, use inactive-aware ancestor queries, explicitly mark modified components dirty and record prefab modifications where applicable. Verify contrast again after saving and reopening scenes. Tile tall parchment centers rather than stretching their grain vertically.
- Synchronize equipped effects during `UpgradeStatManager.SyncFromPurchasedLevels` and after out-of-run equipment changes. Preserve the same modifier keys so refreshes replace rather than stack effects. Chapter multipliers apply once after regular/equipment totals.
- Check saved-scene and test-runner restoration before entering Play. Use a stable Canvas host for video-verification coroutines, since the video object's own coroutines stop when Skip disables it.
- Validate captured image content as well as controls. Preserve failed frames; verify Next/replay/skip, natural boundaries/completion and real chapter scene loads. For discrete story indicators, count actual timed scenes rather than movie files.

## Why This Works

The checks cover the boundaries where Unity behavior changes: standalone-to-nested Canvas, inactive-to-active hierarchy, in-memory-to-saved scene, lobby-to-run and scene-to-scene. They expose failures that component counts and successful compilation cannot detect.

## Prevention

- Inspect real pixels and saved/reloaded properties before accepting a connected screen.
- Verify public gameplay entry paths as well as button wiring: horizontal start bypassed the old Start button's visibility events, so the Pause button needed explicit activation.
- Frame actual equipment renders using alpha bounds when their transparent padding makes thumbnails appear too small. Preserve source icons and point the visual catalog at new trimmed copies.
- Snapshot newly introduced preference keys as well as existing ones; restore and compare key presence and values after directed purchase tests.
- For a lobby-only dim, inspect the actual visibility root before adding a runtime controller. Here `CanvasScript.buttons` is the full-screen `UI` root; a first-child Image with `raycastTarget=false` follows the existing horizontal-start hide/show flow and leaves all controls above it. Verify the real lobby/start frames; a correct alpha value alone does not prove the draw order.
- In Pipeline evaluation snippets, fully qualify `IndianOceanAssets.ShooterSurvival.TimeManager`: the evaluator can resolve the short name to an inaccessible Editor type. A rejected snippet has not run its scheduled checks; correct the diagnostic and rerun before accepting a report. The lobby-dim check exposed this collision and passed after qualification.

## Related Issues

- [Implementation and evidence](../../../map-concepts/harbor-ui-live-2026-09-15/README.md)
- [Inactive modal sorting and scroll bounds](../ui-bugs/restore-inactive-unity-modal-order-and-scroll-height-2026-09-14.md)
- [Separated art and actual alpha](../workflow-issues/validate-real-alpha-and-rebuild-ui-as-separate-assets-2026-09-15.md)
