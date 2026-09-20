using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using IndianOceanAssets.ShooterSurvival;
public static class ApplyThrowShipRefinement
{
 static void Record(UnityEngine.Object value){EditorUtility.SetDirty(value);if(PrefabUtility.IsPartOfPrefabInstance(value))PrefabUtility.RecordPrefabInstancePropertyModifications(value);}
 static void Crate(GameObject go)
 {
  var prop=go.GetComponentsInChildren<SimpleProjectile>(true).Single(p=>p.name=="Arrow2").transform;
  var pose=go.GetComponent<FatManCratePose>();if(pose==null)pose=go.AddComponent<FatManCratePose>();
  pose.Configure(go.GetComponentInChildren<Animator>(true),prop);Record(prop);Record(pose);
 }
 public static string Main()
 {
  if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Edit Mode required");
  var scene=SceneManager.GetActiveScene();if(scene.name!="Noryangjin_MapTool_Mode_SR18"||scene.isDirty)throw new Exception("Clean saved SR18 required");
  string path="Assets/JH/Model/Prefab/Enemy_FatMan.prefab";var prefab=PrefabUtility.LoadPrefabContents(path);
  try{Crate(prefab);PrefabUtility.SaveAsPrefabAsset(prefab,path);}finally{PrefabUtility.UnloadPrefabContents(prefab);}
  path="Assets/ShooterSurvival/Prefabs/Obstacle_Real/Ship.prefab";prefab=PrefabUtility.LoadPrefabContents(path);
  try{var s=prefab.GetComponent<ObstacleStats>();s.firePos.localRotation=Quaternion.Euler(0,90,0);PrefabUtility.SaveAsPrefabAsset(prefab,path);}finally{PrefabUtility.UnloadPrefabContents(prefab);}
  var map=scene.GetRootGameObjects().Single(g=>g.name=="Noryangjin_MapTool").transform;
  var enemies=map.GetComponentsInChildren<EnemyEventController>(true).Where(e=>e.GetComponentInChildren<Animator>(true).runtimeAnimatorController.name.Contains("FatMan")).ToArray();
  foreach(var e in enemies)Crate(e.gameObject);
  var roads=map.Find("Roads").GetComponentsInChildren<MeshCollider>(true);
  foreach(var s in map.GetComponentsInChildren<ObstacleStats>(true).Where(o=>o.obstaclePattern==ObstaclePattern.Ship))
  {
   Vector3 origin=s.transform.position;
   Vector3 roadPoint=roads.Select(r=>r.bounds.ClosestPoint(origin)).OrderBy(p=>(p-origin).sqrMagnitude).First();
   Vector3 direction=Vector3.ProjectOnPlane(roadPoint-origin,Vector3.up).normalized;
   s.transform.rotation=Quaternion.LookRotation(direction,Vector3.up)*Quaternion.Euler(0,-90,0);
   s.firePos.localRotation=Quaternion.Euler(0,90,0);Record(s.transform);Record(s.firePos);
  }
  EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene))throw new Exception("Save failed");AssetDatabase.SaveAssets();return "Saved "+enemies.Length+" crates and road-facing ship";
 }
}
