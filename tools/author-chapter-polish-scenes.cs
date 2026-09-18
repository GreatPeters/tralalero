if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode) throw new System.InvalidOperationException("Edit Mode required");
// This entry point is for the isolated QA project. Its initial scene is disposable.
if (UnityEngine.Application.companyName != "TralaleroQA") throw new System.InvalidOperationException("Run scene-copy authoring only in the isolated QA project");
UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene, UnityEditor.SceneManagement.NewSceneMode.Single);
var restStop = RestStopChapterBuilder.Create();
var restUI = ChapterPresentationInstaller.ApplyOpenScene();
UnityEditor.SceneManagement.EditorSceneManager.OpenScene(HighwaySceneBuilder.ScenePath);
var highwayLayout = HighwayFormationRefinement.ApplyOpenScene();
var highwayUI = ChapterPresentationInstaller.ApplyOpenScene();
UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/ShooterSurvival/Scenes/Tools/Noryangjin_MapTool_Mode_SR18.unity");
var noryangjinUI = ChapterPresentationInstaller.ApplyOpenScene();
UnityEditor.AssetDatabase.SaveAssets();
return new { restStop, restUI, highwayLayout, highwayUI, noryangjinUI };
