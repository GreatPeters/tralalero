using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
public static class HarborRefinementBaseline { public static object Main() {
if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Exit Play Mode before snapshot");
string folder="tmp/backups/harbor-opening-refinement-2026-09-16";
System.IO.Directory.CreateDirectory(folder+"/scenes");
var setup=UnityEditor.SceneManagement.EditorSceneManager.GetSceneManagerSetup();
for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++){
 var scene=UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
 string path=folder+"/scenes/"+scene.name+"-open-before.unity";
 if(System.IO.File.Exists(path))throw new InvalidOperationException("Preserve existing fresh backup");
 if(!UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene,path,true))throw new Exception("Open-scene backup failed");
 if(scene.isDirty&&!UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene))throw new Exception("Could not preserve user's open changes");
}
ChapterPlaytestPreferences.SnapshotAt(folder+"/playerprefs.tsv");
var extra=new System.Collections.Generic.List<string>();
foreach(string key in new[]{"TutorialDone","soundEnabled","vibrationEnabled"}.Concat(Enumerable.Range(1,3).SelectMany(i=>new[]{ChapterUpgradeService.LevelKey(i),ChapterUpgradeService.OwnedKey(i)})))extra.Add(key+"\tint\t"+PlayerPrefs.HasKey(key)+"\t"+PlayerPrefs.GetInt(key));
foreach(string key in new[]{"soundVolume","moveSensitivity"})extra.Add(key+"\tfloat\t"+PlayerPrefs.HasKey(key)+"\t"+PlayerPrefs.GetFloat(key).ToString("R",System.Globalization.CultureInfo.InvariantCulture));
System.IO.File.WriteAllLines(folder+"/extra-prefs.tsv",extra);
try{
 foreach(string name in new[]{"Noryangjin_MapTool_Mode_SR18","HighWay","RestStop"}){
  var scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/ShooterSurvival/Scenes/Tools/"+name+".unity");
  if(!UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene,folder+"/scenes/"+name+".unity",true))throw new Exception("Scene backup failed");
 }
}finally{UnityEditor.SceneManagement.EditorSceneManager.RestoreSceneManagerSetup(setup);}
return folder;

} }
