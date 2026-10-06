using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;
public static class DepartmentObserve {
 public static object RestoreCleanup(bool existed,bool value){
  if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Idle editor required");
  if(existed)EditorPrefs.SetBool("PT_ResourcesCleanup",value);else EditorPrefs.DeleteKey("PT_ResourcesCleanup");
  return Main();
 }
 public static object Reimport(string[] paths){
  if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Idle editor required");
  foreach(var path in paths)AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport|ImportAssetOptions.ForceUpdate);
  return new{reimported=paths};
 }
 public static object Main(){
  var d=Object.FindFirstObjectByType<Chapter45Director>();
  return new {scene=SceneManager.GetActiveScene().name,play=EditorApplication.isPlaying,compiling=EditorApplication.isCompiling,
   floor=d!=null?d.CurrentFloor:-1,distance=d!=null?d.Distance:0,batches=UnityEditor.UnityStats.batches,setPass=UnityEditor.UnityStats.setPassCalls,triangles=UnityEditor.UnityStats.triangles,
   editorCleanupExists=EditorPrefs.HasKey("PT_ResourcesCleanup"),editorCleanup=EditorPrefs.GetBool("PT_ResourcesCleanup",false),
   playerCleanupExists=PlayerPrefs.HasKey("PT_ResourcesCleanup"),playerCleanup=PlayerPrefs.GetInt("PT_ResourcesCleanup",-1),
   timeScale=Time.timeScale,captureDeltaTime=Time.captureDeltaTime,limits="One editor frame; not a phone FPS measurement"};
 }
}
