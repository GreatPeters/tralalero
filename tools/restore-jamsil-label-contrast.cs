using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using Object=UnityEngine.Object;
public static class RestoreJamsilLabelContrast {
 public static object Main(){
  if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)throw new Exception("Idle editor required");
  for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)throw new Exception("Unsaved scene");
  var setup=EditorSceneManager.GetSceneManagerSetup();string folder="outputs/chapters45-2026-10-02",stamp=DateTime.UtcNow.ToString("yyyyMMddTHHmmssfff");
  try{
   string path="Assets/ShooterSurvival/Scenes/Tools/Jamsil.unity";File.Copy(path,folder+"/Jamsil-before-label-contrast-"+stamp+".unity");var scene=EditorSceneManager.OpenScene(path);var d=Object.FindFirstObjectByType<Chapter45Director>();
   var targets=d.GetComponentsInChildren<Chapter45Target>(true);foreach(var t in targets)if(t.healthLabel!=null)t.healthLabel.color=new Color(.97f,.98f,1f,1);
   EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene))throw new Exception("Save failed");
   File.WriteAllText(folder+"/city-label-contrast-"+stamp+".json","{\"labels\":"+targets.Length+",\"color\":\"light cream over asphalt\",\"scope\":\"TMP color only\"}");return new{labels=targets.Length,applied=true};
  }finally{EditorSceneManager.RestoreSceneManagerSetup(setup);}
 }
}
