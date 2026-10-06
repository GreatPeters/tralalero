using System;using System.Linq;using UnityEngine;using UnityEditor;using UnityEngine.SceneManagement;
public static class FinalEditorEvidence {
 public static object Main(){
  var scenes=Enumerable.Range(0,SceneManager.sceneCount).Select(i=>SceneManager.GetSceneAt(i)).ToArray();
  bool idle=!EditorApplication.isPlayingOrWillChangePlaymode&&!EditorApplication.isCompiling;
  if(!idle||scenes.Any(s=>s.isDirty)||Time.timeScale!=1||Time.captureDeltaTime!=0)throw new Exception("Editor not restored to clean idle state");
  return new{utc=DateTime.UtcNow.ToString("o"),idle,activeScene=SceneManager.GetActiveScene().name,
   scenes=scenes.Select(s=>new{s.name,s.path,s.isDirty}).ToArray(),timeScale=Time.timeScale,captureDeltaTime=Time.captureDeltaTime,
   editorCleanupExists=EditorPrefs.HasKey("PT_ResourcesCleanup"),editorCleanup=EditorPrefs.GetBool("PT_ResourcesCleanup",false),
   playerCleanupExists=PlayerPrefs.HasKey("PT_ResourcesCleanup"),playerCleanup=PlayerPrefs.GetInt("PT_ResourcesCleanup",-1),readOnly=true};
 }
}
