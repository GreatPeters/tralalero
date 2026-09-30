param([string]$OutputDirectory = 'outputs/noryangjin-claude-fix-2026-09-28/audio')
# Korean synthetic PA lines for the 2026-09-29 Noryangjin feedback (same SAPI voice as create-noryangjin-voice.ps1).
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Speech
$voice = New-Object System.Speech.Synthesis.SpeechSynthesizer
try {
    $korean = $voice.GetInstalledVoices() | Where-Object { $_.Enabled -and $_.VoiceInfo.Culture.Name -eq 'ko-KR' } | Select-Object -First 1
    if ($null -eq $korean) { throw 'An installed Korean voice is required.' }
    $voice.SelectVoice($korean.VoiceInfo.Name)
    $voice.Rate = 1
    New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
    $clips = [ordered]@{
        'door-closing' = '문이 곧 닫힙니다. 주의해 주세요!'
        'gull-warning' = '갈매기 주의! 그림자를 피하세요!'
        'merchant-union' = '상어 잡아라! 노량진 상인들이 간다!'
        'auction-call-live' = '싱싱한 광어 나왔습니다! 삼만 원, 더 없습니까?'
        'auction-sold-live' = '오만 오천 원! 좋습니다, 낙찰입니다!'
        'auction-scatter' = '상어다! 비켜, 비켜!'
    }
    foreach ($clip in $clips.GetEnumerator()) {
        $path = Join-Path $OutputDirectory ($clip.Key + '.wav')
        if (Test-Path -LiteralPath $path) { continue }
        $voice.SetOutputToWaveFile([System.IO.Path]::GetFullPath($path))
        $voice.Speak($clip.Value)
        $voice.SetOutputToNull()
    }
    Write-Output ('Created ' + $clips.Count + ' clips with ' + $korean.VoiceInfo.Name)
} finally { $voice.Dispose() }
