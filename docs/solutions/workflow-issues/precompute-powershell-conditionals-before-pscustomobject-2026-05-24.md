---
title: Precompute PowerShell conditionals before PSCustomObject literals
date: 2026-05-24
category: docs/solutions/workflow-issues
module: Agent verification scripts
problem_type: workflow_issue
component: tooling
severity: low
applies_when:
  - Writing PowerShell verification scripts that produce PSCustomObject summaries
  - Calculating optional fields such as folder zip counts only when a path exists
tags: [powershell, verification, pscustomobject, scripting]
---

# Precompute PowerShell conditionals before PSCustomObject literals

## Context
During Meshy asset import verification, a summary object tried to calculate a property with this shape:

```powershell
[pscustomobject]@{
  ZipCount=(if (Test-Path -LiteralPath $dir) { ... } else { -1 })
}
```

PowerShell parsed `if` as an invalid command in that expression position, so the verification command emitted errors even though the imported files were present.

## Guidance
Compute conditional values before building the object:

```powershell
$zipCount = -1
if (Test-Path -LiteralPath $dir) {
  $zipCount = (Get-ChildItem -LiteralPath $dir -File -Filter '*.zip').Count
}

[pscustomobject]@{
  ZipCount = $zipCount
}
```

For command fallbacks in this repo's PowerShell environment, avoid shell syntax such as `|| true`. Use PowerShell-native control flow or set `$ErrorActionPreference` intentionally.

## Why This Matters
Verification output must be clean before completion claims. A broken verification script can make a successful file operation look suspect and wastes time rerunning checks.

## When to Apply
- Building `Format-Table` or `Format-List` summaries from filesystem checks.
- Writing hashtable literals for `[pscustomobject]`.
- Porting shell habits from Bash into PowerShell.

## Examples
Prefer this pattern for optional filesystem checks:

```powershell
$folderExists = Test-Path -LiteralPath $dir
$zipCount = -1

if ($folderExists) {
  $zipCount = (Get-ChildItem -LiteralPath $dir -File -Filter '*.zip').Count
}

[pscustomobject]@{
  FolderExists = $folderExists
  ZipCount = $zipCount
}
```

## Related
- [Extract Meshy zips from a short temp path](extract-meshy-zip-in-short-temp-path-2026-05-24.md)
