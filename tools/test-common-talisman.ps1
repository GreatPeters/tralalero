$ErrorActionPreference = 'Stop'
foreach ($suite in @('BonusWallChoicePairTests', 'BonusWallCooldownTests', 'BonusAltarRulesTests')) {
    $start = unity command --project-path . run_tests --mode editor --filter $suite --async_tests true --format json | ConvertFrom-Json
    if (-not $start.success) { throw "Cannot start $suite" }
    $deadline = (Get-Date).AddMinutes(2)
    do {
        Start-Sleep -Seconds 2
        $envelope = unity command --project-path . test_status --format json | ConvertFrom-Json
        $result = $envelope.data.result
        if ($result -is [string]) { $result = $result | ConvertFrom-Json }
        if ((Get-Date) -gt $deadline) { throw "Timeout: $suite" }
    } while ($result.status -ne 'completed')
    $result | ConvertTo-Json -Depth 20 | Set-Content -Encoding UTF8 "map-concepts/common-talisman-applied-2026-09-20/tests-$suite.json"
    Write-Output "$suite : $($result.summary.passed)/$($result.summary.total) passed"
    if ($result.summary.failed -ne 0) { throw "Failed: $suite" }
}
