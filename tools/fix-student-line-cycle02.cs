using System;using System.Linq;using UnityEngine;using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine.SceneManagement;
public static class FixStudentLineCycle02 {
 public static object Main(){
  if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)throw new Exception("Idle required");
  for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)throw new Exception("Dirty scene");
  var setup=EditorSceneManager.GetSceneManagerSetup();
  try{
   var scene=EditorSceneManager.OpenScene("Assets/ShooterSurvival/Scenes/Tools/ShoeTower.unity");
   var roles=scene.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<Chapter45RoleAction>(true)).Where(x=>x.role==Chapter45RoleAction.Role.StudentGrab).ToArray();
   if(roles.Length!=4||roles.Any(x=>x.warningText!="머고 임마~!! · 손을 뻗기 전에 옆으로"))throw new Exception("Unexpected student baseline");
   foreach(var r in roles){r.warningText="와 머야~!! · 손을 뻗기 전에 옆으로";EditorUtility.SetDirty(r);}
   EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);scene=EditorSceneManager.OpenScene(scene.path);
   roles=scene.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<Chapter45RoleAction>(true)).Where(x=>x.role==Chapter45RoleAction.Role.StudentGrab).ToArray();
   if(roles.Any(x=>x.warningText!="와 머야~!! · 손을 뻗기 전에 옆으로"))throw new Exception("Reopen mismatch");
   return new{changed=roles.Length,roles=roles.Select(x=>new{x.name,x.warningText}).ToArray(),reopened=true,textOnly=true};
  }finally{EditorSceneManager.RestoreSceneManagerSetup(setup);}
 }
}
