---
title: Restore inactive Unity modal order and actual scroll height
date: 2026-09-14
category: ui-bugs
module: Mobile shops and modal presentation
problem_type: ui_bug
component: development_workflow
symptoms:
  - "Old wallet and back controls rendered above a newly authored upgrade screen."
  - "A ten-card scroll list displayed rows but could not reach its final upgrade."
  - "Pause and settings screens were active but hidden behind the lobby."
root_cause: async_timing
resolution_type: code_fix
severity: high
tags: [unity, ugui, inactive-canvas, scrollrect, layout, mobile, native-verification]
---

# Restore inactive Unity modal order and actual scroll height

## Problem

The Ocean Pop redesign initially looked correct in isolated previews but failed through the real menu lifecycle. Authoring properties on inactive objects did not prove their values after activation and layout reconstruction.

## What did not work

Setting nested `Canvas.overrideSorting` and `sortingOrder` only in the builder left actual Upgrade2/PauseMenu/SettingsMenu canvases with `overrideSorting == false` in Play Mode. Raising the numeric order alone did not cover the shared lobby top bar.

The grid cache compared width, item count, columns and requested card height. Those inputs remained unchanged while the actual content RectTransform height was reset to0. The early return then preserved the broken bounds, although children still drew beyond them. Programmatic ScrollRect movement correctly observed zero scrollable content height.

A global `GameObject.Find("Canvas")` also selected a preview surface rather than the active scene Canvas. A null-coalescing expression on a missing Unity Component bypassed Unity's fake-null semantics and produced a MissingComponentException.

## Solution

- `MobileUILayer.OnEnable` reapplies the intended canvas sorting order on every activation. Result screens receive their own modal layer as well.
- `EquipmentCardGrid.RefreshLayout` computes the required total height before its cache guard and compares the actual `rect.rect.height`. A driven layout reset therefore triggers repair even when all cached inputs match.
- Native proof scripts find the Canvas under `SceneManager.GetActiveScene().GetRootGameObjects()` and invoke the actual entry buttons. The new upgrade header copies the original persistent back callbacks and explicitly closes its own window.
- Builder `GetOrAdd<T>` uses `component != null`, respecting Unity Object semantics. Replaced decorative header objects are removed after the new controls are bound.

## Evidence and limits

`tmp/image-previews/mobile-presentation-2026-09-14/actual-v2/` retains the overlapping wallet, unreachable last row and hidden modal results. `actual-v3/` shows the clean wallet/header, reachable lateral card (+0%→+5%, level0/10), and visible pause/settings modals. A native probe measured the content at0height before repair; setting its correct1916height allowed scrolling to approximately374units.

Five focused MobilePresentationTests cover the grid reset, inactive canvas activation, font coverage, compact coin and audio assets. They compile, but their first native invocation is still pending at the time of this record: the Editor stopped servicing commands during a dirty-scene save step. This does not turn screenshots into a passing test result. Actual Android acceptance remains separate.

## Prevention

Exercise real open/close/back/scroll operations on inactive, scene-authored UI. Inspect both input settings and the resulting bounds/order. A prototype render and an active object in the hierarchy cannot prove that a user sees or reaches a control.

Save a fresh scene backup, then explicitly check and save the active scene immediately before every native test run. Asset or prefab changes can dirty it after an earlier save. `SaveModifiedSceneTask` calls `SaveCurrentModifiedScenesIfUserWantsTo`, which can block the main-thread command dispatcher behind a confirmation dialog. Do not enqueue duplicate test/build jobs after a timeout; inspect request state and preserve the user's scene.

## Related records

- [Resolve the real upgrade panel](resolve-upgrade-panel-from-persistent-button-target-2026-08-31.md)
- [Protect active scenes during tests](../workflow-issues/protect-active-unity-scenes-from-broad-editmode-test-runs-2026-07-18.md)
- [Verify cosmetic previews and preferences](../workflow-issues/verify-unity-cosmetic-previews-and-test-prefs-2026-09-10.md)
- [Active implementation log](../../exec-plans/active/mobile-presentation-2026-09-14.md)
