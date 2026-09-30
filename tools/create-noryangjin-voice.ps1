param([string]$OutputDirectory = 'outputs/noryangjin-revamp-fix-2026-09-28/audio')
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
        'merchant-catch' = '잡아!'
        'merchant-look' = '보고 가!'
        'truck-reverse' = '후진합니다. 후진합니다.'
        'container-warning' = '작업 중입니다. 그림자를 피해 주세요.'
        'broadcast-1' = '활어 코너 고객님들은 상어를 피해 주세요.'
        'broadcast-2' = '오늘 참치 시세 폭락. 상어 때문입니다.'
        'broadcast-3' = '경매장 통로를 비워 주세요.'
        'broadcast-4' = '활어 경매가 시작됩니다. 입찰 준비해 주세요.'
        'auction-call' = '싱싱한 참치 나왔습니다. 삼만 원! 더 없습니까?'
        'auction-sold' = '사만 원! 좋습니다. 낙찰입니다!'
    }
    foreach ($clip in $clips.GetEnumerator()) {
        $path = Join-Path $OutputDirectory ($clip.Key + '.wav')
        if (Test-Path -LiteralPath $path) { continue }
        $voice.SetOutputToWaveFile([System.IO.Path]::GetFullPath($path))
        $voice.Speak($clip.Value)
        $voice.SetOutputToNull()
    }
    Write-Output ('Created ' + $clips.Count + ' Korean synthetic PA/merchant voice clips with ' + $korean.VoiceInfo.Name)
} finally { $voice.Dispose() }
