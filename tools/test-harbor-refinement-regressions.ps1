$ErrorActionPreference = 'Stop'
$evidence = Join-Path $PSScriptRoot '../map-concepts/harbor-opening-refinement-2026-09-16'
foreach ($suite in @('CoastalUIIntegrationTests', 'PlayerStatusHudTests', 'BonusWallChoicePairTests', 'BonusWallCooldownTests')) {
    $start = (& unity command --project-path . run_tests --mode editor --filter $suite --async_tests true --format json | ConvertFrom-Json)
    if (-not $start.success) { throw "Could not start $suite" }
    $deadline = (Get-Date).AddMinutes(2)
    do {
        Start-Sleep -Seconds 2
        $raw = & unity command --project-path . test_status --format json
        $envelope = $raw | ConvertFrom-Json
        $result = $envelope.data.result
        if ($result -is [string]) { $result = $result | ConvertFrom-Json }
        if ((Get-Date) -gt $deadline) { throw "Timed out: $suite" }
    } while ($result.result -eq 'running' -or $result.Summary.Total -eq 0)
    $raw | Out-File -LiteralPath (Join-Path $evidence "tests-$suite.json") -Encoding utf8
    Write-Output "$suite $($result.Summary.Passed)/$($result.Summary.Total) passed"
    if ($result.Summary.Failed -gt 0) { throw "$suite failed; inspect saved result" }
}
