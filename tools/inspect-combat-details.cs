using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using IndianOceanAssets.ShooterSurvival;
public static class InspectCombatDetails
{
 public static object Main()
 {
  var scene=SceneManager.GetActiveScene();
  var enemies=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<EnemyEventController>(true)).ToArray();
  var samples=enemies.Where(e=>e.name.Contains("Guard")||e.name.Contains("FatMan")).GroupBy(e=>e.name.Contains("Guard")).Select(g=>g.First());
  var details=samples.Select(e=>new {e.name,source=PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(e.gameObject),meshes=e.GetComponentsInChildren<MeshFilter>(true).Select(m=>new{m.name,mesh=m.sharedMesh?.name,localBounds=m.sharedMesh?.bounds.ToString(),world=m.GetComponent<Renderer>()!=null?m.GetComponent<Renderer>().bounds.ToString():null,pos=m.transform.position.ToString(),rot=m.transform.eulerAngles.ToString(),scale=m.transform.lossyScale.ToString()}).ToArray()}).ToArray();
  var rows=EncounterPlacementTables.Rows.Where(r=>r.kind==EncounterPlacementTables.SheetNames[0]).ToArray();
  var info=enemies.Select(e=>new{e.name,source=PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(e.gameObject),pos=e.transform.position.ToString(),forward=e.transform.forward.ToString(),mode=e.EventMode.ToString(),target=e.TargetPoint!=null?e.TargetPoint.position.ToString():null,stats=rows.FirstOrDefault(r=>r.scene==scene.name&&r.id==e.name)}).ToArray();
  var ships=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<ObstacleStats>(true)).Where(s=>s.obstaclePattern==ObstaclePattern.Ship).Select(s=>new{s.name,children=s.GetComponentsInChildren<Transform>(true).Select(t=>new{t.name,pos=t.localPosition.ToString(),rot=t.localEulerAngles.ToString(),scale=t.localScale.ToString(),world=t.position.ToString(),bounds=t.GetComponent<Renderer>()!=null?t.GetComponent<Renderer>().bounds.ToString():null}).ToArray()});
  var helper=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ShooterSurvival/Prefabs/Entities/Space/TungTungTung.prefab");
  var c=helper.GetComponent<CapsuleCollider>();
  var p=UnityEngine.Object.FindFirstObjectByType<PlayerScript>();
  var report=new{details,enemies=info,ships=ships.ToArray(),helperCollider=c!=null?new{center=c.center.ToString(),c.radius,c.height,c.direction}:null,ignoreEnemyHelper=Physics.GetIgnoreLayerCollision(LayerMask.NameToLayer("Enemy"),LayerMask.NameToLayer("Helper")),player=p!=null?new{pos=p.transform.position.ToString(),speed=p.ForwardMoveSpeed}:null};
  var path="map-concepts/combat-route-fixes-2026-09-20/details.json";
  var json=(string)AppDomain.CurrentDomain.GetAssemblies().First(a=>a.GetName().Name=="Newtonsoft.Json").GetType("Newtonsoft.Json.JsonConvert").GetMethod("SerializeObject",new[]{typeof(object)}).Invoke(null,new[]{(object)report});
  System.IO.File.WriteAllText(path,json);
  return path;
 }
}
