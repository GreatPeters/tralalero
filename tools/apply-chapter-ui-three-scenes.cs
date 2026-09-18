if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode) throw new System.InvalidOperationException("Edit Mode required");
if (UnityEngine.Application.companyName != "TralaleroQA") throw new System.InvalidOperationException("Isolated QA project required");
var results = new System.Collections.Generic.List<object>();
try
{
    results.Add(ChapterPresentationInstaller.ApplyOpenScene());
    foreach (var path in new[] { RestStopChapterBuilder.ScenePath, "Assets/ShooterSurvival/Scenes/Tools/Noryangjin_MapTool_Mode_SR18.unity" })
    {
        UnityEditor.SceneManagement.EditorSceneManager.OpenScene(path);
        results.Add(ChapterPresentationInstaller.ApplyOpenScene());
    }
    return results;
}
catch (System.Exception error)
{
    System.IO.File.WriteAllText("map-concepts/chapters-polish-2026-09-12/presentation-error.txt", error.ToString());
    throw;
}
