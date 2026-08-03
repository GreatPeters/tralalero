---
title: Use Independent Oracles for Configuration Column Tests
date: 2026-08-01
category: best-practices
module: Unity player speed configuration
problem_type: best_practice
component: testing_framework
severity: medium
applies_when:
  - Testing configuration values resolved through production lookup helpers
  - Verifying that one field or column is authoritative while adjacent fields must be ignored
  - Writing Unity integration tests for authored scene defaults
related_components: [PlayerScript, EnvironmentVariableTables, Data.xlsx]
tags: [independent-test-oracle, unity-testing, excel-configuration, player-speed, regression-testing]
---

# Use Independent Oracles for Configuration Column Tests

## Context

`PlayerScript` was simplified so forward speed resolves from the `playerSpeed`
row in `Data.xlsx`, specifically `value1`. The first focused test derived its
expected speed with the same `EnvironmentVariableTables.TryGetFloat` helper
used by production. That made the test self-confirming: if the helper started
returning `value2`, production and the expected value could change together.

The review corrected the test before shipping. A separate play-mode integration
attempt was inconclusive because MCP Unity emitted a WebSocket error while
reconnecting around play mode; that transport failure was not evidence about
the speed assertion.

## Guidance

Use an independent oracle when the contract names a specific field hidden by a
production convenience accessor. Read the underlying structured value through a
different path and assert the exact field named by the contract.

Keep two behaviors separate:

1. Prove the initial runtime value came from the intended configuration field.
2. After gameplay activity, prove the value still satisfies its runtime
   invariants, such as remaining constant.

This distinguishes a source-selection regression from later unintended state
progression.

## Why This Matters

A test that reuses production selection or transformation logic verifies only
internal agreement. The same defect can corrupt both the actual and expected
values while the test stays green. An independent oracle instead protects the
semantic contract: player forward speed means `playerSpeed.value1`.

## When to Apply

- Production reads one field through a helper that hides a multi-field record.
- A spreadsheet, JSON row, database record, or generated table is the source of
  truth.
- The requirement names a specific field rather than whatever the helper
  returns.
- A refactor removes progression, interpolation, caps, or gains and the resolved
  value must remain constant.

## Examples

Avoid computing the expectation through the same accessor as production:

```csharp
EnvironmentVariableTables.TryGetFloat("playerSpeed", out var expectedSpeed);
InvokeAwake(player);
Assert.That(player.ForwardMoveSpeed, Is.EqualTo(expectedSpeed));
```

Read the raw row and identify the contracted column explicitly:

```csharp
Assert.That(
    EnvironmentVariableTables.TryGetFloat3("playerSpeed", out var playerSpeed),
    Is.True);

InvokeAwake(player);

Assert.That(
    player.ForwardMoveSpeed,
    Is.EqualTo(playerSpeed.value1).Within(0.0001f));
```

For an authored scene, also assert the source is enabled before checking
constancy later in the run:

```csharp
float authoredForwardSpeed = player.ForwardMoveSpeed;

Assert.That(player.UseExcelCharacterDefaults, Is.True);
Assert.That(authoredForwardSpeed, Is.EqualTo(playerSpeed.value1).Within(0.0001f));

// Exercise gameplay.

Assert.That(player.ForwardMoveSpeed, Is.EqualTo(authoredForwardSpeed).Within(0.0001f));
```

## Related

- [Protect active Unity scenes from broad EditMode test runs](../workflow-issues/protect-active-unity-scenes-from-broad-editmode-test-runs-2026-07-18.md)
- [Call Unity CLI Connector commands with params payloads](../workflow-issues/call-unity-cli-connector-commands-with-params-payloads-2026-06-06.md)
