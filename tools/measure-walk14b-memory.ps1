$ErrorActionPreference='Stop'
$comparisonRoot=Join-Path $PSScriptRoot '../map-concepts/opening-walk-14b-2026-09-12'
$target=Join-Path $comparisonRoot 'windows-memory.csv'
$deadline=(Get-Date).AddHours(3)
while((Get-Date) -lt $deadline) {
    $listener=Get-NetTCPConnection -State Listen -LocalPort 8190 -ErrorAction SilentlyContinue
    if(-not $listener){break}
    $backendId=$listener.OwningProcess
    $memory=Get-CimInstance Win32_PerfFormattedData_GPUPerformanceCounters_GPUProcessMemory |
        Where-Object {$_.Name -like ('*pid_'+$backendId+'_*')} | Select-Object -First 1
    if($memory){
        [pscustomobject]@{
            unix=[DateTimeOffset]::UtcNow.ToUnixTimeSeconds()
            process_id=$backendId
            dedicated_bytes=$memory.DedicatedUsage
            shared_bytes=$memory.SharedUsage
        } | Export-Csv -LiteralPath $target -Append -NoTypeInformation -Encoding UTF8
    }
    if(Test-Path -LiteralPath (Join-Path $comparisonRoot 'generation-report.json')){break}
    Start-Sleep -Seconds 15
}
'Windows memory sampling finished'
