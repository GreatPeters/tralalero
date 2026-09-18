---
title: Preserve workbook trigger cardinality at the authoring boundary
date: 2026-09-15
category: integration-issues
module: SR18 map-tool enemy assignments
problem_type: integration_issue
component: tooling
symptoms:
  - "Run start raised an InvalidDataException requiring a one-to-one activation spot."
  - "One trigger referenced both enemies while the other referenced the right enemy again."
root_cause: logic_error
resolution_type: code_fix
severity: high
tags: [unity, map-tool, workbook, activation, cardinality, undo, occlusion]
---

# Preserve workbook trigger cardinality at the authoring boundary

## Problem

The general map-tool editor allowed multi-target toggling, while workbook-managed encounters required exactly one owning trigger per actor and one actor per trigger. Editing connections could create a scene that the runtime correctly rejected.

## Symptoms

The first left spot referenced the right actor; the right spot referenced both. The scene still contained50actors and50spots, so matching object counts did not prove valid wiring.

## What Didn't Work

- Position checks alone cannot detect swapped/shared target references.
- SingleOrDefault obscured duplicate ownership behind a generic LINQ exception.
- Overhead-only occlusion skipped a combined gantry whose bounds included ground-level posts; its camera references were also unassigned.

## Solution

- Repair explicit references while preserving the user's moved actor positions.
- Detect workbook-managed actors in the editor. Swap two occupied one-to-one connections atomically, move ownership into an empty destination, and leave re-clicked bindings intact. Reject ambiguous groups before mutation, preserve Undo and constrain managed spots to the runtime's Props lookup subtree.
- Keep generic multi-target assignment for content that does not use the workbook contract.
- Retain runtime preflight validation and report both owning-spot and target counts.
- Bind the gantry to explicit scenery occlusion and let explicit groups bypass the road-only ground-height filter. Keep floor geometry visible and restore the complete prop group after it clears the view.

## Why This Works

The authoring tool preserves the same cardinality and lookup scope that its runtime consumer requires. Visual grouping is explicit, so a grounded frame can disappear without removing walkable geometry or leaving detached lettering.

## Prevention

- Test ownership uniqueness, not just total actor/spot counts.
- Cover swaps, repeated clicks, empty destinations, malformed input and Undo, alongside existing generic assignment tests.
- Verify complete workbook application, independent activation and retry reset in the saved scene.
- Preserve dirty scene contents before repair and inspect real gameplay frames for occlusion.

## Related Issues

- [Implementation and native evidence](../../../map-concepts/sr18-placement-repair-2026-09-15/README.md)
