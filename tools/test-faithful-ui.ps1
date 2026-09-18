$ErrorActionPreference='Stop'
& unity command --project-path . eval --code 'return UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();'
foreach($suite in @('HarborFaithfulArtworkTests','HarborReferenceFidelityTests','HarborRefinementTests','CoastalUIIntegrationTests')){
    & unity command --project-path . run_tests --mode editor --filter $suite --async_tests true | Out-Null
    $deadline=(Get-Date).AddMinutes(2)
    do{
        Start-Sleep -Seconds 2
        $raw=& unity command --project-path . test_status --format json
        $envelope=$raw|ConvertFrom-Json;$result=$envelope.data.result;if($result -is [string]){$result=$result|ConvertFrom-Json}
        if((Get-Date) -gt $deadline){throw "Timeout $suite"}
    }while($result.Summary.Total -eq 0 -or $null -eq $result.Summary)
    $raw|Out-File -LiteralPath "map-concepts/harbor-faithful-art-2026-09-17/tests-$suite.json" -Encoding utf8
    Write-Output "$suite $($result.Summary.Passed)/$($result.Summary.Total)"
    if($result.Summary.Failed -gt 0){$result.Results|Where-Object Status -eq Failed|ConvertTo-Json -Depth 5;throw "$suite failed"}
}
