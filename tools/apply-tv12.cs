using System;using System.Linq;using UnityEngine;using UnityEditor;using UnityEditor.SceneManagement;
public static class ValidationApplyTv12 {
 public static object Main(){
  if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Edit only");var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(scene.isDirty)throw new Exception("Preserve dirty scene");
  if(scene.name!="ShoeTower")scene=EditorSceneManager.OpenScene("Assets/ShooterSurvival/Scenes/Tools/ShoeTower.unity");
  var d=UnityEngine.Object.FindFirstObjectByType<Chapter45Director>();var h=d.hazards.Single(z=>z.name=="4F falling television");if(h.showSweepPath)throw new Exception("TV already enabled; do not duplicate");
  string before=EditorJsonUtility.ToJson(h);h.showSweepPath=true;EditorUtility.SetDirty(h);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
  return new{applied=true,scene=scene.path,hazard=h.name,before,after=EditorJsonUtility.ToJson(h),visualOptInOnly=true,bodyColliderTimingDamageUnchanged=true};
 }
}
