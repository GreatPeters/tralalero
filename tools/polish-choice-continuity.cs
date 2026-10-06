using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using Object=UnityEngine.Object;
public static class PolishChoiceContinuity {
 public static object Main(){
  if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)throw new Exception("Idle editor required");
  for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)throw new Exception("Unsaved scene");
  var setup=EditorSceneManager.GetSceneManagerSetup();string stamp=DateTime.UtcNow.ToString("yyyyMMddTHHmmssfff"),folder="outputs/chapters45-2026-10-02/choice-continuity-"+stamp;Directory.CreateDirectory(folder);
  try {foreach(string name in new[]{"Jamsil","ShoeTower"}){
   string path="Assets/ShooterSurvival/Scenes/Tools/"+name+".unity";File.Copy(path,folder+"/"+name+".before.unity");var scene=EditorSceneManager.OpenScene(path);
   foreach(var story in Object.FindObjectsByType<OpeningStoryUI>(FindObjectsInactive.Include,FindObjectsSortMode.None)){story.gameObject.SetActive(false);story.enabled=true;story.autoPlayMovie=true;}
   foreach(var tutorial in Object.FindObjectsByType<CoastalTutorialUI>(FindObjectsInactive.Include,FindObjectsSortMode.None)){tutorial.gameObject.SetActive(true);tutorial.enabled=false;if(tutorial.panel!=null)tutorial.panel.SetActive(false);}
   var d=Object.FindFirstObjectByType<Chapter45Director>();
   foreach(var c in d.GetComponentsInChildren<Chapter45Choice>(true)){
    if(c.kind==Chapter45Choice.ChoiceKind.RouteFork){c.leftName=name=="Jamsil"?"호숫가":"정비 통로";c.rightName=name=="Jamsil"?"상점가":"보안 기록실";c.coinReward=name=="Jamsil"?760:840;}
    if(c.previewLabel!=null){c.previewLabel.fontSize=15;c.previewLabel.enableAutoSizing=true;c.previewLabel.fontSizeMin=11;c.previewLabel.fontSizeMax=15;c.previewLabel.rectTransform.sizeDelta=new Vector2(13,3);c.previewLabel.text=c.Description;}
   }
   if(d.hud!=null&&d.hud.description!=null){d.hud.description.fontSize=21;d.hud.description.enableAutoSizing=true;d.hud.description.fontSizeMin=18;d.hud.description.fontSizeMax=21;d.hud.description.textWrappingMode=TextWrappingModes.Normal;}
   EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene))throw new Exception("Save failed "+name);
  }} finally {EditorSceneManager.RestoreSceneManagerSetup(setup);}
  File.WriteAllText(folder+"/receipt.json","{\"openingRootInactive\":true,\"manualReplayEnabled\":true,\"twoLineRiskWarning\":true,\"cityCoins\":760,\"towerCoins\":840}");return new{saved=true,folder};
 }
}
