if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode) throw new System.InvalidOperationException("Edit Mode required");
if (UnityEngine.Application.companyName == "TralaleroQA") throw new System.InvalidOperationException("Original project required");
if (!System.IO.File.Exists("tmp/backups/chapters-polish-2026-09-13/original-live-before-merge.unity")) throw new System.InvalidOperationException("Original live scene backup required");
var results = new System.Collections.Generic.List<object>();
if (!System.IO.File.Exists(RestStopChapterBuilder.ScenePath)) results.Add(RestStopChapterBuilder.Create());
else { UnityEditor.SceneManagement.EditorSceneManager.OpenScene(RestStopChapterBuilder.ScenePath); results.Add(RestStopChapterBuilder.RefineSceneryOpenScene()); }
results.Add(ChapterPresentationInstaller.ApplyOpenScene());
UnityEditor.SceneManagement.EditorSceneManager.OpenScene(HighwaySceneBuilder.ScenePath);
results.Add(HighwayFormationRefinement.ApplyOpenScene()); results.Add(ChapterPresentationInstaller.ApplyOpenScene());
UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/ShooterSurvival/Scenes/Tools/Noryangjin_MapTool_Mode_SR18.unity");
results.Add(ChapterPresentationInstaller.ApplyOpenScene());
UnityEditor.AssetDatabase.SaveAssets();
return results;
