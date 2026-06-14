$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

$requiredFiles = @(
    "AGENTS.md",
    "ARCHITECTURE.md",
    "docs/README.md",
    "docs/QUALITY_SCORE.md",
    "docs/RELIABILITY.md",
    "docs/SECURITY.md",
    "docs/exec-plans/active/codex-harness-foundation.md",
    "Assets/ShooterSurvival/Scripts/Harness/CombatHarness.cs",
    "Assets/ShooterSurvival/Scripts/Wave/WaveHarnessUtility.cs",
    "Assets/Tests/Editor/WaveHarnessUtilityTests.cs",
    "ProjectSettings/McpUnitySettings.json"
)

$missing = @()
foreach ($file in $requiredFiles) {
    if (-not (Test-Path $file)) {
        $missing += $file
    }
}

if ($missing.Count -gt 0) {
    Write-Error ("Missing required harness files:`n - " + ($missing -join "`n - "))
}

$mcpSettings = Get-Content "ProjectSettings/McpUnitySettings.json" | ConvertFrom-Json
if (-not $mcpSettings.Port) {
    Write-Error "MCP Unity settings do not define a Port."
}

Write-Output ("Harness files OK. MCP Unity port: {0}" -f $mcpSettings.Port)
