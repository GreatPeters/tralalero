# Protected Game Data Workbook

## Goal

Keep the editable Excel workbook easy for developers to find and use from the
Noryangjin map tool while excluding raw data and signing material from player
builds.

## Completed

- Moved `Data.xlsx` from `StreamingAssets` to
  `Assets/ShooterSurvival/GameData/Editor/`.
- Added Excel open/select and protected-data update/validation actions to the
  map tool and `Tools/Data`.
- Migrated gameplay table readers to the shared `GameDataWorkbook` entry point.
- Added an Editor build preprocessor that validates the workbook schema,
  creates a signed/encrypted runtime archive, and rejects stale or invalid data.
- Added fail-closed handling for missing or modified protected player data.
- Added focused EditMode coverage for workbook location, schemas, archive
  round-trips, ciphertext tampering, and gameplay-table reloads.

## Verification

- `dotnet build Assembly-CSharp.csproj -nologo`
- `dotnet build Assembly-CSharp-Editor.csproj -nologo`
- `powershell -ExecutionPolicy Bypass -File tools/validate-agent-harness.ps1`
- Focused `GameDataWorkbookTests` EditMode suite: 10/10 passed on 2026-07-30.
- Historical pre-analytics full-suite baseline: 312/320 passed. The same eight unrelated pre-existing
  Noryangjin integration/map-tool/scene-hash failures remain; both authored
  scene hashes were unchanged by the run.

## Security Boundary

The player archive detects ordinary file replacement and modification. The raw
workbook stays in an Editor-only folder, while the RSA private signing key stays
outside the project and version control at the external local/CI secret path. A
determined user can still patch a client executable or alter process memory.
Competitive or economy-authoritative values require server-side validation.
