using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using IndianOceanAssets.ShooterSurvival;

public static class ApplyCombatRouteFixes
{
 const string Folder="map-concepts/combat-route-fixes-2026-09-20";
 static void Record(GameObject go)
 {
  foreach(var c in go.GetComponentsInChildren<Component>(true))if(c!=null){EditorUtility.SetDirty(c);if(PrefabUtility.IsPartOfPrefabInstance(c))PrefabUtility.RecordPrefabInstancePropertyModifications(c);}
  if(PrefabUtility.IsPartOfPrefabInstance(go))PrefabUtility.RecordPrefabInstancePropertyModifications(go);
 }
 static void ConfigureShooter(EnemyEventController e)
 {
  var animator=e.GetComponentInChildren<Animator>(true);
  string rig=animator!=null&&animator.runtimeAnimatorController!=null?animator.runtimeAnimatorController.name:"";
  bool fat=rig.Contains("FatMan"),guard=rig.Contains("Guard");if(!fat&&!guard)return;
  var body=e.GetComponent<EnemyScript_space>();var so=new SerializedObject(body);
  var shot=e.GetComponentsInChildren<SimpleProjectile>(true).FirstOrDefault(p=>p.name=="Arrow2");if(shot==null)throw new Exception(e.name+": Arrow2 missing");
  so.FindProperty("heldProjectile").objectReferenceValue=shot.transform;
  so.FindProperty("stationaryThrow").boolValue=fat;
  so.FindProperty("throwReleaseDelay").floatValue=fat?.62f:.45f;
  if(fat)
  {
   var pose=e.GetComponent<FatManCratePose>();if(pose==null)pose=e.gameObject.AddComponent<FatManCratePose>();
   pose.Configure(animator,shot.transform);
   e.EventMode=EnemyEventMode.Shoot;e.HideWhileWaiting=false;e.MoveSpeed=0;e.MoveAnimation=EnemyMoveAnimation.None;
   so.FindProperty("throwPoint").objectReferenceValue=shot.transform;
   so.FindProperty("throwSpeed").floatValue=18;
   so.FindProperty("throwApproachSeconds").floatValue=5;
  }
  else
  {
   var gun=e.GetComponentsInChildren<MeshRenderer>(true).First(r=>r.name=="input");
   // Preserve the stock position while shortening this oversized imported mesh.
   gun.transform.localScale=Vector3.one*55f;gun.transform.localRotation=Quaternion.identity;
   var muzzle=gun.transform.Find("Muzzle");if(muzzle==null){muzzle=new GameObject("Muzzle").transform;muzzle.SetParent(gun.transform,false);}
   var bounds=gun.GetComponent<MeshFilter>().sharedMesh.bounds;
   gun.transform.localPosition=Vector3.right*(bounds.min.x*45f);
   muzzle.localPosition=new Vector3(bounds.max.x,bounds.center.y,bounds.center.z);muzzle.localRotation=Quaternion.Euler(0,90,0);
   var aim=e.GetComponent<EnemyGunAim>()??e.gameObject.AddComponent<EnemyGunAim>();aim.gun=gun.transform;aim.muzzle=muzzle;aim.localBarrelAxis=Vector3.right;
   aim.grip=animator.GetBoneTransform(HumanBodyBones.RightHand);aim.localGripPoint=new Vector3(bounds.min.x,bounds.center.y,bounds.center.z);
   so.FindProperty("throwPoint").objectReferenceValue=muzzle;so.FindProperty("hideHeldProjectile").boolValue=true;
  }
  foreach(var collider in shot.GetComponentsInChildren<Collider>(true))collider.enabled=false;
  so.ApplyModifiedPropertiesWithoutUndo();Record(e.gameObject);
 }
 static Vector3 EncounterCenter(EnemyEventController e)
 {
  if(e.HasUsableTarget&&(e.EventMode==EnemyEventMode.AmbushMoveThenShoot||e.EventMode==EnemyEventMode.MoveToTargetThenAttack))return e.TargetPoint.position;
  if(e.HasUsableTarget&&e.EventMode==EnemyEventMode.PatrolBetweenStartAndTarget)return (e.transform.position+e.TargetPoint.position)*.5f;
  return e.transform.position;
 }
 public static string Main()
 {
  if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Edit Mode required");
  string original=SceneManager.GetActiveScene().path;
  var report=new List<string>();
  foreach(string kind in new[]{"Guard","FatMan"})
  {
   string path="Assets/JH/Model/Prefab/Enemy_"+kind+".prefab";var prefab=PrefabUtility.LoadPrefabContents(path);
   try{ConfigureShooter(prefab.GetComponent<EnemyEventController>());PrefabUtility.SaveAsPrefabAsset(prefab,path);}finally{PrefabUtility.UnloadPrefabContents(prefab);}
  }
  EncounterPlacementTables.Reload();
  foreach(string name in new[]{"Noryangjin_MapTool_Mode_SR18","HighWay","RestStop"})
  {
   string path="Assets/ShooterSurvival/Scenes/Tools/"+name+".unity";
   var scene=SceneManager.GetActiveScene().path==path?SceneManager.GetActiveScene():EditorSceneManager.OpenScene(path);
   var map=scene.GetRootGameObjects().First(g=>g.name=="Noryangjin_MapTool").transform;
   var enemies=map.Find("Enemies").GetComponentsInChildren<EnemyEventController>(true);
   var rows=EncounterPlacementTables.Rows.Where(r=>r.scene==name&&r.kind==EncounterPlacementTables.SheetNames[0]).ToDictionary(r=>r.id);
   var road=map.GetComponent<HighwayRoute>();
   var floors=map.Find("Roads").GetComponentsInChildren<MeshCollider>(true);
   float Floor(Vector3 p){foreach(var offset in new[]{Vector3.zero,Vector3.forward*.12f,Vector3.back*.12f,Vector3.left*.12f,Vector3.right*.12f})foreach(var c in floors)if(c.Raycast(new Ray(p+offset+Vector3.up*1.2f,Vector3.down),out var hit,2.4f)&&hit.normal.y>.55f)return hit.point.y+.12f;throw new Exception("Off-road enemy "+p);}
   foreach(var pair in enemies.Where(e=>rows.ContainsKey(e.name)).GroupBy(e=>e.name.EndsWith("_Right")?e.name.Substring(0,e.name.Length-6):e.name))
   {
    var group=pair.ToArray();var baseActor=group.FirstOrDefault(e=>!e.name.EndsWith("_Right"))??group[0];
    Vector3 center=EncounterCenter(baseActor);var clearance=baseActor.GetComponent<EnemyCornerClearance>();
    Vector3 dir=clearance!=null?clearance.Direction:baseActor.transform.forward;
    if(road!=null){float d=road.NearestDistance(center);road.Sample(d,false,out var onRoad,out dir);center.x=onRoad.x;center.z=onRoad.z;}
    else if(clearance!=null){var offset=center-clearance.Corner;var onRoad=clearance.Corner+dir*Vector3.Dot(offset,dir);center.x=onRoad.x;center.z=onRoad.z;}
    else if(name=="RestStop")center=RestStopChapterBuilder.ProjectRoadCenter(center,dir);
    var enabled=group.Where(e=>rows[e.name].enabled).ToArray();
    foreach(var e in group)
    {
     var row=rows[e.name];Vector3 old=e.transform.position;var oldCenter=EncounterCenter(e);
     Vector3 desired=center+Vector3.Cross(Vector3.up,dir)*(enabled.Length>1?(e.name.EndsWith("_Right")?1.1f:-1.1f):0f);
     var delta=desired-oldCenter;
     e.transform.position+=delta;if(e.HasUsableTarget)e.TargetPoint.position+=delta;
     // Stationary throwers stand at the encounter row, never at the ambush entry.
     if(e.name.Contains("FatMan"))e.transform.position=desired;
     var pos=e.transform.position;pos.y=Floor(pos);e.transform.position=pos;
     e.RefreshPlacementAfterAuthoringChange(old,false);
     ConfigureShooter(e);
     e.gameObject.SetActive(row.enabled);
     e.GetComponent<EnemyScript_space>().ApplyStat(row.damage,row.health,row.tier);
     foreach(var spot in map.Find("Props").GetComponentsInChildren<EnemyEventActivationSpot>(true).Where(s=>s.Targets.Contains(e)))
     {spot.gameObject.SetActive(row.enabled);Record(spot.gameObject);}
     if(e.HasUsableTarget){EditorUtility.SetDirty(e.TargetPoint);PrefabUtility.RecordPrefabInstancePropertyModifications(e.TargetPoint);}
     Record(e.gameObject);
    }
   }
   foreach(var ship in map.GetComponentsInChildren<ObstacleStats>(true).Where(s=>s.obstaclePattern==ObstaclePattern.Ship))
   {
    var mesh=ship.GetComponent<MeshFilter>();if(mesh==null)throw new Exception("Ship hull missing");
    float desiredWidth=9.5f;ship.transform.localScale=Vector3.one*(desiredWidth/mesh.sharedMesh.bounds.size.x);
    var pos=ship.transform.position;pos.y=.2f;ship.transform.position=pos;
    ship.shipApproachSeconds=7.5f;
    var fire=ship.firePos;fire.localPosition=new Vector3(mesh.sharedMesh.bounds.max.x*.93f,.00185f,0);
    var ball=ship.projectilePrefab;ball.SetActive(false);
    var renderer=ball.GetComponentInChildren<Renderer>(true);if(renderer!=null){float size=renderer.bounds.size.magnitude;ball.transform.localScale*=1.35f/Mathf.Max(.001f,size);}
    var roadPoint=floors.Select(r=>r.bounds.ClosestPoint(ship.transform.position)).OrderBy(p=>(p-ship.transform.position).sqrMagnitude).First();
    Vector3 toward=Vector3.ProjectOnPlane(roadPoint-ship.transform.position,Vector3.up);
    ship.transform.rotation=Quaternion.LookRotation(toward,Vector3.up)*Quaternion.Euler(0,-90,0);
    ship.firePos.localRotation=Quaternion.Euler(0,90,0);
    Record(ship.gameObject);
   }
   EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene))throw new Exception("Save failed "+path);
   report.Add(name+": active "+enemies.Count(e=>e.gameObject.activeSelf)+", centered rows "+enemies.Where(e=>rows.ContainsKey(e.name)).Select(e=>e.name.Replace("_Right","")).Distinct().Count());
  }
  EditorSceneManager.OpenScene(original);AssetDatabase.SaveAssets();
  string summary=string.Join("\n",report);System.IO.File.WriteAllText(Folder+"/applied.txt",summary);return summary;
 }
}
