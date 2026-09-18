$ErrorActionPreference = 'Stop'
$reportFolder = 'map-concepts/shop-fidelity-2026-09-17/verification'
$initial = unity command --project-path . test_status --format json | ConvertFrom-Json
$initial.data.result | Set-Content -Encoding UTF8 "$reportFolder/ShopFidelityTests.json"
foreach ($suite in @('CosmeticShopIntegrationTests', 'ChapterWorkshopTests', 'CoastalUIIntegrationTests', 'HarborFaithfulArtworkTests', 'UITextAlignmentTests')) {
    $started = unity command --project-path . run_tests --mode editor --filter $suite --async_tests true --format json | ConvertFrom-Json
    if (-not $started.success) { throw "Could not run $suite" }
    $deadline = (Get-Date).AddMinutes(2)
    do {
        Start-Sleep -Milliseconds 600
        $reply = unity command --project-path . test_status --format json | ConvertFrom-Json
        $result = $reply.data.result | ConvertFrom-Json
        if ((Get-Date) -gt $deadline) { throw "Timed out: $suite" }
    } while ($result.status -ne 'completed')
    $reply.data.result | Set-Content -Encoding UTF8 "$reportFolder/$suite.json"
    Write-Output "$suite total=$($result.summary.total) passed=$($result.summary.passed) failed=$($result.summary.failed)"
    if ($result.summary.total -eq 0 -or $result.summary.failed -ne 0) { throw "No valid passing result for $suite" }
}
