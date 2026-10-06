using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using Object=UnityEngine.Object;
public static class RefineChapters45
{
 public static object Main()
 {
  if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Edit Mode required");
  var setup=EditorSceneManager.GetSceneManagerSetup();
  try{foreach(string name in new[]{"Jamsil","ShoeTower"})
  {
   var scene=EditorSceneManager.OpenScene("Assets/ShooterSurvival/Scenes/Tools/"+name+".unity");var d=Object.FindFirstObjectByType<Chapter45Director>();
   foreach(var hazard in d.GetComponentsInChildren<Chapter45Hazard>(true))BoundSweep(hazard,name=="Jamsil"?17:15);
   var targets=d.GetComponentsInChildren<Chapter45Target>(true);
   var pair=targets.First(t=>Mathf.Abs(t.distance-(name=="Jamsil"?1680:1380))<1);
   pair.name="LeftSecurityPanel";pair.blocksAllLanes=false;pair.requiredForGoal=false;pair.lane=-2;pair.halfWidth=1.5f;pair.transform.position+=Vector3.left*2;
   var right=Object.Instantiate(pair.gameObject,pair.transform.parent).GetComponent<Chapter45Target>();right.name="RightSecurityPanel";right.lane=2;right.transform.position+=Vector3.right*4;
   if(name=="ShoeTower")
   {
    var captain=targets.Single(t=>t.captain);var pressure=new Chapter45Hazard[2];
    for(int i=0;i<2;i++)
    {
     var source=d.GetComponentsInChildren<Chapter45Hazard>(true).First(h=>h.floor==2&&!h.manualOnly);
     var hazard=Object.Instantiate(source.gameObject,source.transform.parent).GetComponent<Chapter45Hazard>();hazard.name="CaptainPressure_"+i;
     var origin=hazard.body.position;Vector3 destination=new Vector3(0,236,1569);Vector3 delta=destination-origin;hazard.transform.position+=delta;
     hazard.manualOnly=true;hazard.distance=1569;hazard.safeLane=i==0?-3:3;hazard.warning=i==0?"왼쪽 틈으로 · 오른쪽 바닥 경고":"오른쪽 틈으로 · 왼쪽 바닥 경고";BoundSweep(hazard,0);pressure[i]=hazard;
    }
    captain.pressureHazards=pressure;
    var archive=Object.Instantiate(targets.First(t=>!t.captain).gameObject,targets[0].transform.parent).GetComponent<Chapter45Target>();archive.name="ArchiveSecurityPartition";archive.distance=885;archive.floor=1;archive.choiceIndex=0;archive.requiredChoice=1;archive.blocksAllLanes=true;archive.requiredForGoal=true;archive.lane=0;archive.transform.position=new Vector3(18,95,885);
   }
   d.targets=d.GetComponentsInChildren<Chapter45Target>(true);d.hazards=d.GetComponentsInChildren<Chapter45Hazard>(true);
   foreach(var actor in d.GetComponentsInChildren<IndianOceanAssets.ShooterSurvival.EnemyScript_space>(true))
   {actor.gameObject.layer=LayerMask.NameToLayer("Enemy");var canvas=actor.GetComponentInChildren<Canvas>(true);if(canvas!=null)canvas.transform.localRotation=Quaternion.Euler(0,180,0);}
   foreach(var goal in d.GetComponentsInChildren<Chapter45Goal>(true))goal.gameObject.layer=LayerMask.NameToLayer("Default");
   EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
  }}finally{EditorSceneManager.RestoreSceneManagerSetup(setup);}
  return new{pairedGates=true,archivePartition=true,captainPressure=2,physicallyBoundedSweeps=true};
 }
 static void BoundSweep(Chapter45Hazard hazard,float warningLead)
 {
  hazard.warningDistance=warningLead;float side=hazard.safeLane<0?1:-1;
  // Entire body stays in the warning corridor, away from the published safe lane.
  hazard.sweepFrom=new Vector3(side*12,0,0);hazard.sweepTo=new Vector3(side*2.7f,0,0);
  if(hazard.footprint!=null){var p=hazard.footprint.transform.position;p.x=hazard.body.position.x+side*2.9f;hazard.footprint.transform.position=p;hazard.footprint.transform.localScale=new Vector3(5.4f,.035f,8);}
  if(hazard.body!=null)
  {
   foreach(var c in hazard.body.GetComponentsInChildren<Collider>(true))c.isTrigger=true;
   var b=hazard.body.GetComponent<BoxCollider>();
   if(b!=null&&hazard.body.name=="SecuritySweep"){b.size=new Vector3(1,1,8);}
  }
 }
}
