$ErrorActionPreference = 'Stop'

$projectRoot = Split-Path -Parent $PSScriptRoot
$reportPath = Join-Path $projectRoot 'Library\MobileUiOptimizer\latest-report.json'
$scenePath = Join-Path $projectRoot 'Assets\ShooterSurvival\Scenes\Tools\Noryangjin_MapTool_Mode.unity'

function Invoke-UnityJson {
    param([string[]]$Arguments)

    $raw = & unity @Arguments 2>&1 | Out-String
    if ($LASTEXITCODE -ne 0) {
        throw "Unity command failed: $raw"
    }

    return $raw | ConvertFrom-Json
}

function Test-ProjectBuild {
    & dotnet build (Join-Path $projectRoot 'Assembly-CSharp.csproj') -nologo *> $null
    if ($LASTEXITCODE -ne 0) {
        return 0
    }

    & dotnet build (Join-Path $projectRoot 'Assembly-CSharp-Editor.csproj') -nologo *> $null
    return [int]($LASTEXITCODE -eq 0)
}

function Get-ExistingMissingSpriteReferenceCount {
    $knownGuids = [System.Collections.Generic.HashSet[string]]::new(
        [System.StringComparer]::OrdinalIgnoreCase)

    $metaLines = & rg --no-filename --only-matching 'guid: [0-9a-f]{32}' `
        (Join-Path $projectRoot 'Assets') `
        (Join-Path $projectRoot 'Packages') `
        -g '*.meta' 2>$null
    foreach ($line in $metaLines) {
        $null = $knownGuids.Add($line.Substring(6))
    }

    $sceneText = Get-Content -Raw -LiteralPath $scenePath
    $matches = [regex]::Matches(
        $sceneText,
        'm_Sprite: \{fileID: [-0-9]+, guid: ([0-9a-f]{32}), type: 3\}')

    $missing = [System.Collections.Generic.HashSet[string]]::new(
        [System.StringComparer]::OrdinalIgnoreCase)
    foreach ($match in $matches) {
        $guid = $match.Groups[1].Value
        if (-not $knownGuids.Contains($guid)) {
            $null = $missing.Add($guid)
        }
    }

    return $missing.Count
}

Push-Location $projectRoot
try {
    $null = & unity command --project-path . editor_stop --format json 2>&1
    $play = Invoke-UnityJson @(
        'command', '--project-path', '.', 'editor_play', '--format', 'json')
    Start-Sleep -Milliseconds 1500

    $performance = Invoke-UnityJson @(
        'command', '--project-path', '.', 'get_performance_stats', '--format', 'json')
    $uiProbe = Invoke-UnityJson @(
        'command', '--project-path', '.', 'eval_file',
        '--file', 'tools/MobileUiRuntimeProbe.cs', '--format', 'json')

    $stats = $performance.data.result
    $ui = $uiProbe.data.result.result
    $optimizer = if (Test-Path -LiteralPath $reportPath) {
        Get-Content -Raw -LiteralPath $reportPath | ConvertFrom-Json
    }
    else {
        $null
    }

    $result = [ordered]@{
        draw_calls = [double]$stats.render.drawCalls
        build_passed = Test-ProjectBuild
        new_missing_sprite_refs = if ($optimizer) {
            [double]$optimizer.newMissingSpriteRefs
        } else { 0 }
        atlas_pages = if ($optimizer) { [double]$optimizer.atlasPages } else { 0 }
        idempotent = if ($optimizer) { [int]$optimizer.idempotent } else { 1 }
        visual_contract_passed = if ($optimizer) {
            [int]$optimizer.visualContractPassed
        } else { 1 }
        batches = [double]$stats.render.batches
        set_pass_calls = [double]$stats.render.setPassCalls
        main_thread_ms = [double]$stats.frameTiming.cpuFrameTimeMs
        render_thread_ms = [double]$stats.frameTiming.gpuFrameTimeMs
        active_ui_textures = [double]$ui.activeUiTextures
        atlas_estimated_mb = if ($optimizer) {
            [double]$optimizer.estimatedMemoryMb
        } else { 0 }
        ui_sprites_packed = if ($optimizer) {
            [double]$optimizer.spritesPacked
        } else { 0 }
        screen_memory_mb = [math]::Round(
            [double]$stats.memory.totalAllocatedBytes / 1MB,
            3)
        existing_missing_sprite_refs = Get-ExistingMissingSpriteReferenceCount
    }

    $result | ConvertTo-Json -Compress
}
finally {
    $null = & unity command --project-path . editor_stop --format json 2>&1
    Pop-Location
}
