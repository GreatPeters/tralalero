using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using Object=UnityEngine.Object;
public static class PolishTowerFinalText {
 public static object Main(){
  if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)throw new Exception("Idle editor required");
  for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)throw new Exception("Unsaved scene");
  var setup=EditorSceneManager.GetSceneManagerSetup();string folder="outputs/chapters45-2026-10-02",stamp=DateTime.UtcNow.ToString("yyyyMMddTHHmmssfff");
  try{
   string path="Assets/ShooterSurvival/Scenes/Tools/ShoeTower.unity";File.Copy(path,folder+"/ShoeTower-before-final-text-"+stamp+".unity");var scene=EditorSceneManager.OpenScene(path);var d=Object.FindFirstObjectByType<Chapter45Director>();
   var label=d.GetComponentsInChildren<TMP_Text>(true).Single(t=>t.text.Contains("더 좋은 신발"));
   label.text="더 좋은 신발\n가브릴렐로에게\n바칠 공물";label.fontSize=10;label.enableAutoSizing=false;label.rectTransform.sizeDelta=new Vector2(7.5f,4f);
   var hazards=d.GetComponentsInChildren<Chapter45Hazard>(true);foreach(var h in hazards)h.warning="보안 횡단 · 표시된 위험 구역을 피하세요";
   EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene))throw new Exception("Save failed");
   File.WriteAllText(folder+"/tower-final-text-"+stamp+".json","{\"offeringLines\":3,\"hazardWarnings\":"+hazards.Length+",\"scope\":\"presentation strings and world title font bounds only\"}");return new{applied=true,hazards=hazards.Length};
  }finally{EditorSceneManager.RestoreSceneManagerSetup(setup);}
 }
}
