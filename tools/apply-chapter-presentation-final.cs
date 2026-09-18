if(UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)throw new System.InvalidOperationException("Edit Mode required");
string original=UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty&&!UnityEditor.SceneManagement.EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene()))throw new System.IO.IOException("Preserve active scene before authoring");
var results=new System.Collections.Generic.List<object>();
foreach(string name in new[]{"Noryangjin_MapTool_Mode_SR18","HighWay","RestStop"}){
 UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/ShooterSurvival/Scenes/Tools/"+name+".unity");
 results.Add(ChapterPresentationInstaller.ApplyOpenScene());
}
UnityEditor.SceneManagement.EditorSceneManager.OpenScene(original);
return results;
