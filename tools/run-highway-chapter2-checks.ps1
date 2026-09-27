param([string]$OutputDirectory = "outputs/highway-chapter2-2026-09-27/final-tests", [string]$Filter = "")
$ErrorActionPreference = "Stop"
New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null
$compileDeadline = [DateTime]::UtcNow.AddMinutes(2)
do {
    $compileEnvelope = (& unity command --project-path . recompile_status --format json) | ConvertFrom-Json
    $compileState = [string]$compileEnvelope.data.result
    if ($compileState -notin @('compiling', 'triggered')) { break }
    if ([DateTime]::UtcNow -gt $compileDeadline) { throw 'Unity compilation did not finish before tests.' }
    Start-Sleep -Milliseconds 500
} while ($true)
$sceneEnvelope = (& unity command --project-path . list_open_scenes --format json) | ConvertFrom-Json
$sceneData = $sceneEnvelope.data.result
if ($sceneData -is [string]) { $sceneData = $sceneData | ConvertFrom-Json }
if ($sceneData.scenes | Where-Object isDirty) { throw "Save the owned HighWay changes before starting tests." }
$filters = @(
    "HighwayChapter2RulesTests",
    "HighwayRebuildContractTests",
    "HighwayChapterIntegrationTests",
    "EncounterPlacementTablesTests",
    "HighwayProjectilePathTests",
    "PlayerDamageNotificationTests",
    "ProjectilePoolLifetimeTests",
    "RoadChapterPatternTests.HighwayHasSupportedContinuousCurvesAndBothBranches",
    "WorkbookEnemyAssignmentTests",
    "GameDataWorkbookTests"
)
$summaries = @()
if ($Filter) { $filters = $Filter.Split(';') }
foreach ($filter in $filters) {
    $started = (& unity command --project-path . run_tests --mode editor --filter $filter --async_tests true --format json) | ConvertFrom-Json
    if (-not $started.success) { throw "Cannot start $filter" }
    $deadline = [DateTime]::UtcNow.AddMinutes(4)
    do {
        Start-Sleep -Milliseconds 500
        $envelope = (& unity command --project-path . test_status --format json) | ConvertFrom-Json
        $detail = $envelope.data.result
        if ($detail -is [string]) { $detail = $detail | ConvertFrom-Json }
        if ([DateTime]::UtcNow -gt $deadline) { throw "Timed out waiting for $filter. Inspect Editor state before retrying." }
    } while ($detail.status -ne "completed")
    $envelope | ConvertTo-Json -Depth 30 | Set-Content (Join-Path $OutputDirectory ($filter + ".json")) -Encoding utf8
    $summary = [pscustomobject]@{filter=$filter; total=$detail.summary.total; passed=$detail.summary.passed; failed=$detail.summary.failed}
    $summaries += $summary
    $summary | ConvertTo-Json -Compress
    if ($summary.total -eq 0 -or $summary.failed -gt 0) {
        $detail.results | Where-Object Status -ne "Passed" | ConvertTo-Json -Depth 8
        throw "Focused test failed: $filter"
    }
}
$summaries | ConvertTo-Json | Set-Content (Join-Path $OutputDirectory "summary.json") -Encoding utf8
