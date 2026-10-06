using System;using System.IO;using System.Linq;using System.Collections.Generic;using UnityEngine;using UnityEngine.SceneManagement;using UnityEditor;using UnityEditor.SceneManagement;using Object=UnityEngine.Object;
public static class ProbeContinuousCycle{
 static string Json(object v)=>(string)AppDomain.CurrentDomain.GetAssemblies().First(a=>a.GetName().Name=="Newtonsoft.Json").GetType("Newtonsoft.Json.JsonConvert").GetMethod("SerializeObject",new[]{typeof(object)}).Invoke(null,new[]{v});
 static float[] V(Vector3 p)=>new[]{p.x,p.y,p.z};
 static string PathOf(Transform t)=>t.parent==null?t.name:PathOf(t.parent)+"/"+t.name;
 public static object Main(){
  if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)throw new Exception("Idle required");for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)throw new Exception("Dirty scene");var setup=EditorSceneManager.GetSceneManagerSetup();var rows=new List<object>();
  try{foreach(string name in new[]{"Jamsil","ShoeTower"}){
   EditorSceneManager.OpenScene("Assets/ShooterSurvival/Scenes/Tools/"+name+".unity");var d=Object.FindFirstObjectByType<Chapter45Director>();var route=new List<object>();foreach(float s in name=="Jamsil"?new[]{700f,800,1000,1100,1250,1360,1400}:new[]{600f,665,710,750,800,900,1210,1260,1340}){d.route.Sample(s,out var p,out var f);route.Add(new{distance=s,p=V(p),forward=V(f)});}
   var root=d.transform.Find("Scenery");object items;
   if(name=="Jamsil"){int index=0;items=root.Find("Jamsil_ReferenceStreet_20261002").GetComponentsInChildren<Transform>(true).Where(t=>t.name.StartsWith("Shop bay ")).Select(t=>new{index=index++,t.name,p=V(t.position),yaw=t.eulerAngles.y}).ToArray();}
   else items=root.Find("ChapterFinish_20261003").GetComponentsInChildren<Chapter45SceneryGroup>(true).Where(g=>g.floor==1||g.floor==2).Select(g=>new{path=PathOf(g.transform),g.name,g.floor,g.startDistance,g.endDistance,p=V(g.transform.position),children=g.transform.Cast<Transform>().Select(t=>new{t.name,p=V(t.position),renderers=t.GetComponentsInChildren<Renderer>(true).Length}).ToArray()}).ToArray();
   rows.Add(new{scene=name,route,items,lights=Object.FindObjectsByType<Light>(FindObjectsInactive.Include,FindObjectsSortMode.None).Select(l=>new{l.name,l.type,l.shadows,l.intensity}).ToArray()});
  }}finally{EditorSceneManager.RestoreSceneManagerSetup(setup);}
  var file="outputs/department-store-2026-10-02/trellis-recovery/continuous-cycle01-probe.json";File.WriteAllText(file,Json(rows));return file;
 }
}
