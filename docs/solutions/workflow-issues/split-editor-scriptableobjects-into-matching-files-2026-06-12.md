---
title: Split editor ScriptableObjects into matching files
date: 2026-06-12
category: workflow-issues
module: Unity editor tooling
problem_type: workflow_issue
component: tooling
severity: medium
applies_when:
  - "A Unity .asset file rewrites to m_Script fileID 0 after refresh"
  - "Editor ScriptableObject defaults need to persist serialized lists"
tags: [unity, editor-tooling, scriptableobject, serialization]
---

# Split editor ScriptableObjects into matching files

## Context

`NoryangjinMapToolPaletteDefaults.asset` kept rewriting to `m_Script: {fileID: 0}` and `entries: []` after Unity refresh. The `NoryangjinMapToolPaletteDefaults` ScriptableObject class lived inside `NoryangjinMapToolWindow.cs`, so Unity could compile the type but could not serialize a stable MonoScript reference for the asset.

## Guidance

Put editor ScriptableObject asset types in a `.cs` file whose filename matches the class name. Helper serializable entry classes can live in the same file if they are only data containers for that asset.

For the Noryangjin map tool, moving `NoryangjinMapToolPaletteDefaults` into `NoryangjinMapToolPaletteDefaults.cs` let the asset reference the generated script GUID instead of losing its script link.

## Why This Matters

An invalid ScriptableObject asset may look editable as YAML, but Unity rewrites it from the deserialized object state on refresh. If the script reference is missing, serialized defaults such as palette entries disappear.

## When to Apply

- Creating or repairing Unity editor `.asset` defaults.
- Persisting map-tool palettes, editor settings, or other ScriptableObject-backed tool state.
- Seeing `m_Script: {fileID: 0}` in a YAML asset that should reference a custom class.

## Examples

Before: `NoryangjinMapToolPaletteDefaults` lived inside `NoryangjinMapToolWindow.cs`, and the asset reset to empty entries.

After: `NoryangjinMapToolPaletteDefaults.cs` owns the ScriptableObject class, and `NoryangjinMapToolPaletteDefaults.asset` references that script GUID.
