using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using Object=UnityEngine.Object;
public static class FinishChapters45Labels {
 public static object Main(){
  if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)throw new Exception("Idle editor required");
  for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)throw new Exception("Unsaved scene");
  var setup=EditorSceneManager.GetSceneManagerSetup();string stamp=DateTime.UtcNow.ToString("yyyyMMddTHHmmssfff"),folder="outputs/chapters45-2026-10-02/labels-final-"+stamp;Directory.CreateDirectory(folder);
  try{foreach(var name in new[]{"Jamsil","ShoeTower"}){
   string path="Assets/ShooterSurvival/Scenes/Tools/"+name+".unity";File.Copy(path,folder+"/"+name+".before.unity");var scene=EditorSceneManager.OpenScene(path);var d=Object.FindFirstObjectByType<Chapter45Director>();
   var behaviours=d.GetComponentsInChildren<MonoBehaviour>(true).Where(x=>x!=null&&! (x is TMPro.TMP_Text)).OrderBy(x=>x.GetInstanceID()).ToArray();
   string before=string.Join("\n",behaviours.Select(x=>EditorJsonUtility.ToJson(x)))+string.Join("\n",d.GetComponentsInChildren<Collider>(true).OrderBy(x=>x.GetInstanceID()).Select(x=>EditorJsonUtility.ToJson(x)));
   var actors=d.GetComponentsInChildren<Chapter45Encounter>(true).SelectMany(e=>e.actors).Where(a=>a!=null).Distinct().ToArray();foreach(var a in actors)a.gameObject.SetActive(false);
   var targets=d.GetComponentsInChildren<Chapter45Target>(true);foreach(var t in targets)if(t.healthLabel!=null)t.healthLabel.color=name=="Jamsil" ? new Color(.97f,.98f,1f,1) : new Color(.035f,.095f,.15f,1);
   string after=string.Join("\n",behaviours.Select(x=>EditorJsonUtility.ToJson(x)))+string.Join("\n",d.GetComponentsInChildren<Collider>(true).OrderBy(x=>x.GetInstanceID()).Select(x=>EditorJsonUtility.ToJson(x)));
   if(before!=after)throw new Exception("Gameplay/collider state changed");EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene))throw new Exception("Save failed");File.WriteAllText(folder+"/"+name+".json",$"{{\"actorsInactiveInLobby\":{actors.Length},\"readableTargetLabels\":{targets.Length},\"gameplayAndCollidersUnchanged\":true}}");
  }}finally{EditorSceneManager.RestoreSceneManagerSetup(setup);}return new{folder,applied=true};
 }
}
