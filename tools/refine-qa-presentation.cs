if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode) throw new System.InvalidOperationException("Edit Mode required");
if (UnityEngine.Application.companyName != "TralaleroQA") throw new System.InvalidOperationException("QA project required");
var results = new System.Collections.Generic.List<object>();
foreach (var path in new[] { "Assets/ShooterSurvival/Scenes/Tools/Noryangjin_MapTool_Mode_SR18.unity", HighwaySceneBuilder.ScenePath, RestStopChapterBuilder.ScenePath })
{
    UnityEditor.SceneManagement.EditorSceneManager.OpenScene(path);
    if (path == RestStopChapterBuilder.ScenePath) results.Add(RestStopChapterBuilder.RefineSceneryOpenScene());
    results.Add(ChapterPresentationInstaller.ApplyOpenScene());
}
return results;
