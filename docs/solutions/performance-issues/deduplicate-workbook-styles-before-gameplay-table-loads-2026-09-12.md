---
title: Deduplicate workbook styles before gameplay table loads
date: 2026-09-12
category: performance-issues
module: Game data workbook and sheet graft tools
problem_type: performance_issue
component: development_workflow
symptoms:
  - "SR18 Editor startup took18–26seconds even with the opening movie disabled"
  - "Multiple independent table consumers each spent about0.8seconds creating an Excel reader"
root_cause: logic_error
resolution_type: workflow_improvement
severity: high
tags: [unity, startup, excel, styles, performance, artifact-tool]
---

# Deduplicate workbook styles before gameplay table loads

## Problem

Startup became slow after unrelated movie work, but disabling video still took about23seconds. Repeated spreadsheet grafts had accumulated duplicate style definitions that each ExcelDataReader construction parsed again.

## Symptoms

- A975,856-byte XLSX contained27,274,208 uncompressed stylesheet bytes.
-41,988 font definitions represented14 fonts;127,660 cell formats collapsed to57 after remapping their component references.
- Reader construction took760–850ms; opening the file stream took less than0.4ms.

## What Didn't Work

- Treating the larger video as the cause was not supported by the video-off observation.
- Enabling only `Profiler.enabled` captured no Editor frame data. The probe also needs `ProfilerDriver.enabled`, with both original states restored.
- Reusing a ZipInfo object in a second archive mutated the source archive's offsets and broke reread verification. Pass a copy to `writestr`.

## Solution

Compact number formats, fonts, fills and borders, then parent and cell XFs; remap cell/row/column style references. Verify effective format identity across every original XF and preserve all non-style worksheet bytes and unrelated ZIP parts. Artifact Tool's representative before/after renders were pixel-identical. Its public API lacks style-table compaction, so the repair uses narrow OOXML package processing.

Install the schema-validated candidate with a source-hash guard, preserve asset GUIDs, and regenerate/verify the existing protected runtime archive. Fix the four graft writers to use `merge_styles` from `append-encounter-workbook-sheets.py`; it reuses matching definitions and preserves all original indices instead of appending the imported stylesheet wholesale.

## Why This Works

The stylesheet shrank to10,079bytes and the workbook to66,220bytes. Reader creation dropped to about7ms. A comparable profiled SR18 video-on run reached its first movie frame in2.156seconds instead of18.078seconds. Video quality did not need to change. Editor auto-validation/table reloads amplify this cost; runtime readers also benefit, but no device timing was measured.

## Prevention

- Test repeated grafts, including equivalent custom number formats with different IDs and genuinely new styles. Twenty identical grafts must not grow the style table.
- Preserve a semantic format mapping and non-style-byte checks; do not remove styling indiscriminately or flatten formulas.
- Save/reconcile dirty scenes before starting Unity tests. The follow-up15-test run blocked with an unsaved scene marker and no result; its pass status remains unverified.
- Compare matched profiler configurations and label cache/device limits. Editor startup measurements are not APK cold-start measurements.

## Related Issues

- [Evidence and verification limits](../../../map-concepts/startup-performance-2026-09-12/README.md)
- [Workbook mutation and reload contract](../integration-issues/atomic-enemy-stat-workbook-reload-and-pool-safe-reset-2026-08-02.md)
- [Presentation and runtime verification](../workflow-issues/verify-runtime-presentation-and-progression-beyond-numeric-checks-2026-09-10.md)
