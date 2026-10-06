using System;using System.Linq;using System.Collections.Generic;using UnityEngine;using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine.SceneManagement;using IndianOceanAssets.ShooterSurvival;using Object=UnityEngine.Object;
public static class AuditBehaviorCycle02 {
 static string Path(Transform t){return t.parent==null?t.name:Path(t.parent)+"/"+t.name;}
 static float[] V(Vector3 v)=>new[]{v.x,v.y,v.z};
 public static object Main(){
  if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)throw new Exception("Idle required");for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)throw new Exception("Dirty scene");var setup=EditorSceneManager.GetSceneManagerSetup();var results=new List<object>();
  try{foreach(string name in new[]{"Jamsil","ShoeTower"}){
   var s=EditorSceneManager.OpenScene("Assets/ShooterSurvival/Scenes/Tools/"+name+".unity");var d=Object.FindFirstObjectByType<Chapter45Director>();var all=s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
   var models=all.SelectMany(t=>t.GetComponents<Animator>()).Where(a=>a.runtimeAnimatorController!=null&&AssetDatabase.GetAssetPath(a.runtimeAnimatorController).Contains("DetailedCharacters20261003")).Select(a=>new{path=Path(a.transform),controller=AssetDatabase.GetAssetPath(a.runtimeAnimatorController),a.gameObject.activeInHierarchy,position=V(a.transform.position),encounter=a.GetComponentInParent<Chapter45Encounter>()?.name,role=a.GetComponentInParent<Chapter45RoleAction>()?.role.ToString(),floor=a.GetComponentInParent<Chapter45Deck>()?.floor}).ToArray();
   var environment=all.SelectMany(t=>t.GetComponents<MeshFilter>()).Where(m=>m.sharedMesh!=null).Select(m=>new{path=Path(m.transform),asset=AssetDatabase.GetAssetPath(m.sharedMesh),position=V(m.transform.position),m.gameObject.activeInHierarchy}).Where(m=>m.asset.Contains("RetailProduction20261003")||m.asset.Contains("DetailedEnvironment20261003")||m.asset.Contains("RetailTowelMeshy")||m.asset.Contains("TrellisRecovery")).ToArray();
   var hazards=d.hazards.Select(h=>new{h.name,path=Path(h.transform),h.distance,h.warningDistance,h.warningSeconds,h.operationSeconds,h.floor,h.choiceIndex,h.requiredChoice,h.safeLane,h.safeHalfWidth,h.damageFraction,h.fatalContact,h.fallIntoHole,h.manualOnly,h.warning,sweepFrom=V(h.sweepFrom),sweepTo=V(h.sweepTo),position=V(h.transform.position),bodyPosition=h.body==null?null:V(h.body.position)}).ToArray();
   var encounters=d.encounters.Select(e=>new{e.name,e.floor,e.choiceIndex,e.requiredChoice,e.requiredToProceed,e.activationDistance,e.stopDistance,e.health,e.damage,e.statPrefix,e.coinReward,e.announcement,actors=e.actors.Select(a=>new{a.name,path=Path(a.transform),position=V(a.transform.position),role=a.GetComponent<Chapter45RoleAction>()?.role.ToString(),warning=a.GetComponent<Chapter45RoleAction>()?.warningText,callNumber=a.GetComponent<Chapter45RoleAction>()?.callNumber}).ToArray()}).ToArray();
   var lifts=d.lifts.Select(l=>new{l.name,l.afterSegment,l.destinationSegment,l.choiceIndex,l.requiredChoice,l.minimumFloorSeconds,l.requireFloorClear,l.escalator,l.duration,holdout=l.requiredHoldout?.name}).ToArray();
   var choices=d.choices.Select(c=>new{c.name,kind=c.kind.ToString(),c.floor,c.distance,c.endDistance,c.rewardDistance,c.coinReward,c.riskAttackPercent,c.leftName,c.rightName}).ToArray();
   var signs=all.SelectMany(t=>t.GetComponents<TMPro.TMP_Text>()).Where(t=>t.gameObject.activeInHierarchy).Select(t=>new{path=Path(t.transform),t.text}).ToArray();
   results.Add(new{scene=name,models,environment,hazards,encounters,lifts,choices,signs});
  }}finally{EditorSceneManager.RestoreSceneManagerSetup(setup);}
  return new{utc=DateTime.UtcNow.ToString("o"),savedSceneInventory=results,readOnly=true};
 }
}
