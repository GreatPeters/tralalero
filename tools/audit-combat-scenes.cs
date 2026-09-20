using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using IndianOceanAssets.ShooterSurvival;
public static class AuditCombatScenes
{
 static Vector3 Center(EnemyEventController e)=>e.HasUsableTarget&&(e.EventMode==EnemyEventMode.AmbushMoveThenShoot||e.EventMode==EnemyEventMode.MoveToTargetThenAttack)?e.TargetPoint.position:e.HasUsableTarget&&e.EventMode==EnemyEventMode.PatrolBetweenStartAndTarget?(e.transform.position+e.TargetPoint.position)*.5f:e.transform.position;
 public static string Main()
 {
  if(EditorApplication.isPlaying)throw new Exception("Edit Mode required");
  string original=SceneManager.GetActiveScene().path;var reports=new List<object>();
  try
  {
   foreach(string name in new[]{"Noryangjin_MapTool_Mode_SR18","HighWay","RestStop"})
   {
    var scene=EditorSceneManager.OpenScene("Assets/ShooterSurvival/Scenes/Tools/"+name+".unity");
    var map=scene.GetRootGameObjects().First(g=>g.name=="Noryangjin_MapTool");
    var controller=map.GetComponent<EncounterPlacementController>();if(controller==null)controller=map.AddComponent<EncounterPlacementController>();
    controller.BeginNewRun();
    var enemies=map.transform.Find("Enemies").GetComponentsInChildren<EnemyEventController>(true).Where(e=>e.gameObject.activeSelf).ToArray();
    var rowErrors=new List<object>();
    var highway=map.GetComponent<HighwayRoute>();
    foreach(var group in enemies.GroupBy(e=>e.name.Replace("_Right","")))
    {
     var pair=group.ToArray();var positions=pair.Select(Center).ToArray();var first=pair[0];var clearance=first.GetComponent<EnemyCornerClearance>();
     Vector3 center=positions.Aggregate(Vector3.zero,(a,b)=>a+b)/positions.Length;Vector3 reference=center;Vector3 direction=first.transform.forward;
     if(highway!=null)highway.Sample(highway.NearestDistance(center),false,out reference,out direction);
     else if(clearance!=null){reference=clearance.Corner;direction=clearance.Direction;}
     float offset=Vector3.Dot(center-reference,Vector3.Cross(Vector3.up,direction));
     float spread=positions.Max(p=>Vector3.Dot(p-center,direction))-positions.Min(p=>Vector3.Dot(p-center,direction));
     if(Mathf.Abs(offset)>.06f||spread>.06f)rowErrors.Add(new{id=group.Key,offset,spread});
    }
    var bosses=enemies.Where(e=>e.GetComponentInChildren<Animator>(true).runtimeAnimatorController.name.Contains("Woman")).Select(e=>new{e.name,health=e.GetComponent<EnemyScript_space>().CurrentHealth,position=e.transform.position.ToString()}).ToArray();
    reports.Add(new{name,active=enemies.Length,applied=controller.AppliedCount,rowErrors,bosses,stationary=enemies.Count(e=>e.GetComponent<EnemyScript_space>().StationaryThrow)});
   }
  }
  finally{EditorSceneManager.OpenScene(original);}
  var json=(string)AppDomain.CurrentDomain.GetAssemblies().First(a=>a.GetName().Name=="Newtonsoft.Json").GetType("Newtonsoft.Json.JsonConvert").GetMethod("SerializeObject",new[]{typeof(object)}).Invoke(null,new[]{(object)reports});
  File.WriteAllText("map-concepts/combat-route-fixes-2026-09-20/scene-audit.json",json);return json;
 }
}
