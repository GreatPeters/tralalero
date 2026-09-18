$ErrorActionPreference = 'Stop'
$reviewRoot = Join-Path $PSScriptRoot '../tmp/review-two-chapter-20260911'
$previewRoot = Join-Path $PSScriptRoot '../tmp/image-previews/two-chapter-2026-09-11'
$cohortRoot = Join-Path $PSScriptRoot '../tmp/image-previews/sr18-presentation-progression-2026-09-10'
function Read-Json([string]$path) { Get-Content -LiteralPath $path -Raw -Encoding UTF8 | ConvertFrom-Json }
$tests = @()
foreach ($file in Get-ChildItem -LiteralPath $reviewRoot -Filter 'final-tests-*.json') {
    $tests += @((Read-Json $file.FullName).Results)
}
$tests = @($tests | Where-Object { $null -ne $_ } | Sort-Object FullName -Unique)
$cohorts = @{}
foreach ($name in @('nory-baseline-20260911','nory-refined-profile-20260911','highway-bound-route-20260911','nory-final-collision-20260911','highway-final-collision-20260911')) {
    $path = Join-Path $cohortRoot "$name/runs.csv"
    if (Test-Path -LiteralPath $path) { $cohorts[$name] = @(Import-Csv -LiteralPath $path) }
}
$restorePath = Join-Path $reviewRoot 'preferences-restored.json'
$restoration = @{status='pending'}
if (Test-Path -LiteralPath $restorePath) { $restoration = Read-Json $restorePath }
$report = [ordered]@{
    date = '2026-09-11'
    relatedTests = @{total=$tests.Count;passed=@($tests | Where-Object Status -eq 'Passed').Count;failed=@($tests | Where-Object Status -ne 'Passed').Count;results=$tests}
    hazardPhysics = Read-Json (Join-Path $previewRoot 'hazard-physics-v3/result.json')
    shopAndVideo = Read-Json (Join-Path $previewRoot 'ui-functional/result.json')
    chapterTransition = Read-Json (Join-Path $previewRoot 'chapter-transition/result.json')
    cohorts = $cohorts
    userStateRestoration = $restoration
    limits = @('Targeted Editor tests, not the entire repository suite.','Ordinary-input automated steering, not human difficulty certification.','Cohort kills column counts Dead state transitions and may include cleanup/contact.','hazard-physics-v2 is invalid because Editor was paused after a Pipeline timeout.','Video is silent; Korean dialogue is editable UI text.')
}
$target = Join-Path $PSScriptRoot '../map-concepts/two-chapter-2026-09-11/verification.json'
$report | ConvertTo-Json -Depth 16 | Set-Content -LiteralPath $target -Encoding UTF8
[pscustomobject]@{Tests=$tests.Count;Passed=$report.relatedTests.passed;Cohorts=$cohorts.Count;Restoration=$restoration.status}
