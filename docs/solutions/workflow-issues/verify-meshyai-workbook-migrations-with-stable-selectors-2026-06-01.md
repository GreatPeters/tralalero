---
title: Verify MeshyAI workbook migrations with stable selectors
date: 2026-06-01
category: docs/solutions/workflow-issues
module: MeshyAI asset metadata workflow
problem_type: workflow_issue
component: tooling
severity: low
applies_when:
  - "Verifying numbered MeshyAI image output folders that may contain extra helper PNGs"
  - "Checking Korean Excel workbooks from PowerShell-hosted Python snippets"
  - "Migrating MeshyAI asset rows across images, JSONL, and XLSX files"
tags: [meshyai, excel, powershell, verification, openpyxl]
---

# Verify MeshyAI workbook migrations with stable selectors

## Context
While replacing the Stage01 Noryangjin road modules with three TestFolder MeshyAI roads, the migration verification hit two avoidable script issues. The image folder contained a non-numbered PNG, so blindly parsing every `*.png` filename as a sequence failed. A second check embedded Korean worksheet names in a PowerShell here-string sent to Python, and the sheet names arrived as `?????`.

## Guidance
For `output/meshy_images`, filter active sequence images by the numbered filename contract before parsing:

```python
numbered_pngs = [
    path for path in image_dir.glob("*.png")
    if re.match(r"^\d{3}_", path.name)
]
```

For Korean XLSX files in PowerShell-driven verification, avoid relying on non-ASCII sheet literals inside inline Python snippets. Use known worksheet positions, ASCII sheet names where available, or load `wb.sheetnames` first and select from values returned by `openpyxl`.

Also verify road metadata by the actual asset code pattern used in the workbook. The Noryangjin road rows are `NRY-038`, `NRY-039`, and `NRY-040`, not `NRY-ROAD-*`.

## Why This Matters
The migration itself can be correct while the verification script fails or reports empty matches. That makes completion evidence ambiguous and can lead to unnecessary reruns of Unity import or Excel generation steps.

## When to Apply
- Re-numbering MeshyAI generated images after removing or replacing assets.
- Updating `docs/design/tralalero_meshy_asset_plan_kr.xlsx`.
- Running Python verification snippets through Windows PowerShell.

## Examples
Before: parse every PNG and look for `NRY-ROAD` in the first Excel column.

After: parse only `^\d{3}_` PNGs, count rows in the real asset-list and queue sheets, and assert `NRY-038..040` are the remaining Noryangjin road rows.

## Related
- [Generate width-matched MeshyAI reference sheets](generate-width-matched-meshyai-reference-sheets-2026-05-31.md)
- [Precompute PowerShell conditionals before PSCustomObject literals](precompute-powershell-conditionals-before-pscustomobject-2026-05-24.md)
