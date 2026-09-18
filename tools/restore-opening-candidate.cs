using UnityEditor;using UnityEditor.SceneManagement;
public static class RestoreOpeningCandidate {public static object Main(){
 var s=UnityEngine.SceneManagement.SceneManager.GetActiveScene();EditorSceneManager.SaveScene(s,"tmp/backups/harbor-opening-refinement-2026-09-16/failed-opening-"+System.DateTime.UtcNow.ToString("HHmmssfff")+".unity",true);
 EditorSceneManager.OpenScene("Assets/ShooterSurvival/Scenes/Tools/Noryangjin_MapTool_Mode_SR18.unity");AssetDatabase.Refresh();return true;
}}
