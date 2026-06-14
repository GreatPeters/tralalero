---
title: Extract Meshy zips from a short temp path
date: 2026-05-24
category: docs/solutions/workflow-issues
module: MeshyAI asset import workflow
problem_type: workflow_issue
component: tooling
severity: low
applies_when:
  - Windows agents extract Meshy zip downloads into deeply nested Unity asset folders
  - Stage asset folder names are long enough for `Expand-Archive` to approach Windows path limits
tags: [meshy, unity-assets, zip-extraction, windows-paths, workflow]
---

# Extract Meshy zips from a short temp path

## Context
While replacing the Stage01 Noryangjin 048 road module, a Meshy zip was placed directly in the final Unity asset folder:

`Assets/ShooterSurvival/Models/MeshyAI/Stage01_Noryangjin/048_STAGE01_NRY_ROAD_040_Noryangjin_wet_T_junction_pier_road_module`

Extracting the zip into a nested `_extract_tmp_*` folder inside that target failed because the combined Windows path became too long for `Expand-Archive`.

## Guidance
For Meshy zip imports, extract into a short temporary directory first, then move renamed files into the final asset folder.

Use this shape:

```powershell
$tempRoot = Join-Path $env:TEMP ("meshy48_" + (Get-Date -Format 'yyyyMMdd_HHmmss'))
New-Item -ItemType Directory -Path $tempRoot | Out-Null
Expand-Archive -LiteralPath $zip.FullName -DestinationPath $tempRoot -Force
```

After extraction:

- Locate the `.fbx` recursively under the temp directory.
- Use the FBX basename to identify Meshy texture maps.
- Rename files to the numbered prop folder prefix, such as `048_STAGE01_NRY_ROAD_040_Noryangjin_wet_T_junction_pier_road_module.fbx`.
- Rename maps to `_BaseColor.png`, `_Normal.png`, `_Metallic.png`, `_Roughness.png`, and `_Emission.png` when present.
- Move the original `.zip` and `.zip.meta` to `Assets/ShooterSurvival/Models/MeshyAI/_RawDownloads`.
- If a target file already exists, move it to `_previous_YYYYMMDD_HHMMSS` before placing the replacement.

## Why This Matters
Meshy-generated asset names and stage folder names can combine into paths that fail during extraction even though the final target filenames are valid. A short temp path avoids partial extraction failures and keeps the final Unity folder clean.

## When to Apply
- Importing or replacing Meshy zip downloads in `Assets/ShooterSurvival/Models/MeshyAI`.
- Processing numbered prop folders with long stage/module names.
- Re-running an import where existing FBX or texture files may already be present.

## Examples
Direct extraction into the final folder failed with a missing-path error under `_extract_tmp_*`. Extracting the same zip under `$env:TEMP`, then moving the renamed FBX and texture files into the 048 folder, completed successfully.

Verify with explicit file checks instead of inferred file lists:

```powershell
Test-Path -LiteralPath (Join-Path $targetDir ($targetBase + '.fbx'))
Test-Path -LiteralPath (Join-Path $targetDir ($targetBase + '_BaseColor.png'))
(Get-ChildItem -LiteralPath $targetDir -File -Filter '*.zip').Count
```

## Related
- [Persist Generated Images Before Starting the Next Prompt](persist-generated-image-before-next-prompt-2026-05-16.md)
