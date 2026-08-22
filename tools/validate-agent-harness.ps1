$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

$requiredFiles = @(
    ".github/secret_scanning.yml",
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

$secretScanningConfig = ".github/secret_scanning.yml"
$expectedIgnoredPaths = @(
    "Assets/google-services.json",
    "Assets/StreamingAssets/google-services-desktop.json",
    "Assets/Plugins/Android/FirebaseApp.androidlib/res/values/google-services.xml"
)
$secretScanningLines = Get-Content -LiteralPath $secretScanningConfig

if (-not ($secretScanningLines -match '^\s*paths-ignore:\s*$')) {
    Write-Error "$secretScanningConfig does not define paths-ignore."
}

$configuredIgnoredPaths = @(
    foreach ($line in $secretScanningLines) {
        if ($line -match '^\s*-\s*"([^"]+)"\s*$') {
            $Matches[1]
        }
    }
)

$pathDifferences = @(
    Compare-Object `
        -ReferenceObject @($expectedIgnoredPaths | Sort-Object) `
        -DifferenceObject @($configuredIgnoredPaths | Sort-Object)
)

if ($configuredIgnoredPaths.Count -ne $expectedIgnoredPaths.Count -or $pathDifferences.Count -gt 0) {
    Write-Error "$secretScanningConfig must ignore exactly the reviewed Firebase client configuration files."
}

$broadExclusions = @($configuredIgnoredPaths | Where-Object { $_ -match '[*?\[\]]' })
if ($broadExclusions.Count -gt 0) {
    Write-Error "$secretScanningConfig must not contain wildcard exclusions."
}

$mcpSettings = Get-Content "ProjectSettings/McpUnitySettings.json" | ConvertFrom-Json
if (-not $mcpSettings.Port) {
    Write-Error "Legacy MCP Unity settings do not define a Port."
}

$packageManifest = Get-Content "Packages/manifest.json" | ConvertFrom-Json
$pipelineVersion = $packageManifest.dependencies."com.unity.pipeline"
if (-not $pipelineVersion) {
    Write-Error "Packages/manifest.json does not include the official com.unity.pipeline package."
}

Write-Output (
    "Harness files OK. Unity Pipeline: {0}; legacy MCP Unity port: {1}" -f `
        $pipelineVersion,
        $mcpSettings.Port
)
