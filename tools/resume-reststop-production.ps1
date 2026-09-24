param([switch]$CheckOnly)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$out = Join-Path $root 'outputs/reststop-production-2026-09-24'
$python = 'C:/AI/TRELLIS2-AMD/venv/Scripts/python.exe'
$checkpoint = Get-Content -LiteralPath (Join-Path $out 'reboot-checkpoint.json') -Raw | ConvertFrom-Json
$gate = Get-Content -LiteralPath (Join-Path $out 'review-pending.json') -Raw | ConvertFrom-Json
if ($checkpoint.state -ne 'paused_verified' -or $gate.stage -ne 'shape' -or $gate.state -ne 'pending' -or (Test-Path -LiteralPath $gate.result)) {
    throw 'Checkpoint changed; inspect the saved status before resuming.'
}
if (-not (Test-Path -LiteralPath (Join-Path $gate.folder 'model.glb'))) { throw 'Saved shape is missing' }
$manifest = Get-Content -LiteralPath (Join-Path $checkpoint.checkpoint 'manifest.json') -Raw | ConvertFrom-Json
foreach ($entry in $manifest.files) {
    if ($entry.path -match '/quality-ledgers/|/manual-texture-ledgers/|/manual-refinement-ledgers/|/stages/m1/|/effective-settings.json$|/saved-settings.json$') {
        $current = Join-Path $root $entry.path
        if (-not (Test-Path -LiteralPath $current) -or (Get-FileHash -LiteralPath $current -Algorithm SHA256).Hash -ne $entry.sha256) {
            throw "Saved stage or attempt ledger changed: $($entry.path)"
        }
    }
}
$active = @(Get-CimInstance Win32_Process | Where-Object {
    $_.Name -in @('python.exe','blender.exe') -and $_.CommandLine -like "*$root*" -and
    $_.CommandLine -match 'run-reststop-trellis-production.py|run-trellis-uv-math.py|texture-reststop|refine-reststop'
})
if ($active.Count) { throw 'A task generation process is already running; do not launch another.' }
if (@(Get-NetTCPConnection -State Listen -LocalPort 8189 -ErrorAction SilentlyContinue).Count) { throw 'Port 8189 is in use; inspect ownership first.' }
if ($CheckOnly) {
    Write-Output 'Resume preflight passed. No processes started. Next: S10 saved shape review.'
    exit 0
}
$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$runner = Join-Path $PSScriptRoot 'run-reststop-trellis-production.py'
$wrapper = Join-Path $PSScriptRoot 'run-limited-generation.py'
$record = Join-Path $out "runner-resume-$stamp-resource.json"
$arguments = @('-X','utf8',('"' + $wrapper + '"'),'--script',('"' + $runner + '"'),
    '--record',('"' + $record + '"'),'--','--defer','F03,F04,R11','--resume')
$process = Start-Process -FilePath $python -ArgumentList $arguments -WorkingDirectory $root -WindowStyle Hidden -PassThru `
    -RedirectStandardOutput (Join-Path $out "runner-resume-$stamp.log") `
    -RedirectStandardError (Join-Path $out "runner-resume-$stamp.err.log")
Write-Output "Resume launcher PID: $($process.Id). S10 waits for actual visual review; no automatic approval."
