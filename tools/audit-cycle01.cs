using System;using System.IO;using System.Linq;using System.Collections.Generic;using UnityEngine;using UnityEngine.SceneManagement;using UnityEditor;using UnityEditor.SceneManagement;using TMPro;using IndianOceanAssets.ShooterSurvival;using Object=UnityEngine.Object;
public static class AuditCycle01{
 public static object Main(string name){
  if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)throw new Exception("Idle required");for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)throw new Exception("Dirty scene");
  var setup=EditorSceneManager.GetSceneManagerSetup();var report=new List<object>();
  try{foreach(string scene in new[]{"Jamsil","ShoeTower"}){
   EditorSceneManager.OpenScene("Assets/ShooterSurvival/Scenes/Tools/"+scene+".unity");var d=Object.FindFirstObjectByType<Chapter45Director>();var root=d.transform.Find("Scenery/EnvironmentArt_20261003/ContinuousQualityCycle01_20261003");
   var all=d.transform.Find("Scenery");var meshes=all.GetComponentsInChildren<MeshFilter>(true).Where(f=>f.sharedMesh!=null&&f.GetComponent<TMP_Text>()==null).ToArray();
   Func<MeshFilter,long> tris=f=>Enumerable.Range(0,f.sharedMesh.subMeshCount).Sum(i=>(long)f.sharedMesh.GetIndexCount(i)/3);
   var native=root==null?new MeshFilter[0]:root.GetComponentsInChildren<MeshFilter>(true).Where(f=>f.sharedMesh!=null&&f.transform.parent.name=="Curved timber court architecture").ToArray();
   report.Add(new{scene,cycleApplied=root!=null,sceneryStaticTriangles=meshes.Sum(tris),sceneryRenderers=all.GetComponentsInChildren<Renderer>(true).Length,sceneryColliders=all.GetComponentsInChildren<Collider>(true).Length,newCourtGeometryTriangles=native.Sum(tris),newCourtGeometryRenderers=native.Length,newRootColliders=root==null?0:root.GetComponentsInChildren<Collider>(true).Length,cycleGroups=root==null?0:root.GetComponentsInChildren<Chapter45SceneryGroup>(true).Length});
  }
  var type=AppDomain.CurrentDomain.GetAssemblies().First(a=>a.GetName().Name=="Newtonsoft.Json").GetType("Newtonsoft.Json.JsonConvert");var json=(string)type.GetMethod("SerializeObject",new[]{typeof(object)}).Invoke(null,new object[]{report});File.WriteAllText("outputs/department-store-2026-10-02/trellis-recovery/continuous-cycle01-qa/"+name+".json",json);return report;
  }finally{EditorSceneManager.RestoreSceneManagerSetup(setup);}
 }
}
