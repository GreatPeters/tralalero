---
title: Resolve the upgrade panel from its persistent button target
date: 2026-08-31
category: ui-bugs
module: Unity upgrade UI tooling
problem_type: ui_bug
component: tooling
symptoms:
  - "The upgrade UI builder modified an inactive duplicate named Upgrade instead of the Upgrade2 panel opened from the main menu."
  - "The visible upgrade panel retained its old layout even though the rebuild command reported success."
  - "A root GridLayoutGroup could compress the workshop layout and an opaque panel could cover the global Back control."
root_cause: logic_error
resolution_type: code_fix
severity: medium
related_components:
  - "testing_framework"
  - "development_workflow"
tags: [unity, upgrade-ui, unityevent, persistent-listener, grid-layout-group, sibling-order, idempotent-builder]
---

# Resolve the upgrade panel from its persistent button target

## Problem

The Noryangjin scene contains two upgrade hierarchies. A name-based editor
builder selected `Canvas/UI/Panel/Upgrade`, but the main-menu upgrade button's
serialized `UnityEvent` activates `Canvas/UI/Upgrade2`. The rebuilt screen could
therefore look correct in the hierarchy and still never appear in the player
flow.

## Symptoms

- Running the rebuild command changed an inactive duplicate instead of the
  shop opened from `Main/Bottom/Upgrade_Button`.
- `Upgrade2` retained its original root `GridLayoutGroup`, which could treat the
  workshop tint, header, card container, and footer as four grid cells.
- The full-screen opaque background could render over the global Back control.
- Index-derived coin/diamond icons could disagree with the `UpgradeRow.priceType`
  used by the actual transaction.

## What Didn't Work

- Selecting a functional UI root by object name alone.
- Adding an inner 3x3 card grid while leaving the old root layout group enabled.
- Assuming a full-screen panel could keep its existing sibling order without
  checking the global navigation layer.
- Assigning currency icons from card indices instead of the row data that
  drives payment.
- Treating a successful first builder run as proof of idempotence.

## Solution

Use the main button's persistent event as the ownership boundary:

```csharp
for (int index = 0; index < button.onClick.GetPersistentEventCount(); index++)
{
    if (button.onClick.GetPersistentMethodName(index) != "SetActive")
        continue;

    GameObject target = button.onClick.GetPersistentTarget(index) as GameObject;
    if (target != null &&
        target.GetComponentsInChildren<UpgradeUI>(true).Length == 9)
    {
        return target.transform;
    }
}
```

The builder then:

- removes the legacy `GridLayoutGroup` from `Upgrade2`;
- moves the nine existing cards into one owned inner grid;
- keeps every `UpgradeUI` and purchase `Button` rather than replacing logic;
- orders `Upgrade2` immediately before `Top`, leaving global Back navigation
  visible above the opaque workshop;
- binds the currency image to `UpgradeUI.Refresh`, which selects the icon from
  the current or next row's `priceType`;
- wraps scene mutation in a dedicated Undo group and rolls it back on failure.

Tests inspect the serialized persistent calls, including target, `SetActive`
method, bool argument, listener state, and listener mode. The main button must
open the selected panel and activate global Back; the Back button must close
the same panel.

## Why This Works

The runtime connection becomes the source of truth for editor ownership, so a
duplicate name or hierarchy refactor cannot silently redirect the builder.
Only the inner container owns automatic card layout, leaving header/footer
anchors deterministic. The explicit sibling order preserves navigation, while
row-driven currency presentation cannot drift from the payment code.

Repeated rebuilds were verified with an unchanged scene SHA-256, so the tool
converges instead of adding duplicate children or rewriting serialized data.

## Prevention

- Resolve functional Unity UI from serialized references or persistent events,
  not global/name-only lookup.
- Give each hierarchy level one layout owner; do not mix a root layout group
  with manually anchored direct children.
- Derive presentation from the same row fields used by the transaction.
- Group multi-object editor migrations into one Undo transaction and revert it
  on exceptions.
- Verify builders twice: assert both semantic hierarchy contracts and an
  unchanged second-run scene hash.
- Test navigation bindings down to persistent bool arguments and enabled call
  state, not just target and method names.

## Related Issues

- [Migrate legacy player status UI to a screen-space HUD safely](../architecture-patterns/migrate-legacy-player-status-ui-to-screen-space-hud-safely-2026-08-18.md)
- [Transactional reference-scene gameplay composition](../integration-issues/transactional-reference-scene-gameplay-composition-2026-07-23.md)
- [Generate Unity map-tool sibling scenes fail closed](../workflow-issues/generate-unity-map-tool-sibling-scenes-fail-closed-2026-07-15.md)
- [Replace duplicated bonus-choice icon auras with soft gradients](replace-duplicated-bonus-choice-icon-auras-with-soft-gradients-2026-08-16.md)
