using System;using System.IO;using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine.SceneManagement;
public static class ReloadCycle04PreservedScene {
 public static object Main(){
  if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)throw new Exception("Idle required");
  string copy="outputs/chapter45-detailed-design-2026-10-03/phone-readability-cycle04/recovery/ShoeTower-unsaved-20261003T2025.unity",path="Assets/ShooterSurvival/Scenes/Tools/ShoeTower.unity";
  if(!File.Exists(copy)||SceneManager.GetActiveScene().path!=path)throw new Exception("Preserved copy and expected scene required");
  var s=EditorSceneManager.OpenScene(path,OpenSceneMode.Single);return new{s.path,s.isDirty,preservedCopy=copy,reason="Observed serialization-only URP light additions and TMP cached color/style normalization; no authored placement or logic differences"};
 }
}
