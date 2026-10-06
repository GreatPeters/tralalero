using System;using System.IO;using System.Linq;using System.Collections.Generic;using UnityEngine;using UnityEngine.SceneManagement;using UnityEditor;using UnityEditor.SceneManagement;using TMPro;using Object=UnityEngine.Object;
public static class InspectRetailLayout{
 public static object Main(){
  if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)throw new Exception("Idle required");for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)throw new Exception("Dirty scene");
  var setup=EditorSceneManager.GetSceneManagerSetup();var rows=new List<object>();
  try{foreach(string name in new[]{"Jamsil","ShoeTower"}){
   var s=EditorSceneManager.OpenScene("Assets/ShooterSurvival/Scenes/Tools/"+name+".unity");var d=Object.FindFirstObjectByType<Chapter45Director>();var scenery=d.transform.Find("Scenery");
   var samples=new List<object>();foreach(float station in name=="Jamsil"?new[]{0f,370,510,750,970,1250,1460,1550,1665,1740,1800}:new[]{0f,45,175,320,530,560,600,650,700,750,800,850,900,950,1000,1080,1120,1180,1280,1400,1500,1576}){d.route.Sample(station,out var p,out var f);samples.Add(new{station,p=p.ToString("R"),f=f.ToString("R")});}
   rows.Add(new{scene=name,routeSegments=d.route.segments.Length,floors=d.route.segments.GroupBy(x=>x.floor).Select(g=>new{floor=g.Key,count=g.Count(),minLength=g.Min(x=>x.Length),maxLength=g.Max(x=>x.Length),from=g.First().start.ToString("R"),to=g.Last().end.ToString("R")}),samples,choices=d.GetComponentsInChildren<Chapter45Choice>(true).Select(c=>new{c.name,serialized=JsonUtility.ToJson(c)}),sceneryGroups=scenery.GetComponentsInChildren<Chapter45SceneryGroup>(true).Select(g=>new{path=PathOf(g.transform),g.floor,g.startDistance,g.endDistance,g.alwaysVisible}),offering=d.GetComponentsInChildren<TMP_Text>(true).Where(t=>t.text.Contains("더 좋은 신발")).Select(t=>new{path=PathOf(t.transform),t.text,t.fontSize,position=t.transform.position.ToString("R"),rotation=t.transform.rotation.ToString("R"),scale=t.transform.lossyScale.ToString("R"),bounds=t.bounds.ToString()}),placeholderCounts=scenery.GetComponentsInChildren<Transform>(true).Where(t=>new[]{"Display volume","Folded textile","Upper arranged display","Merchandise"}.Contains(t.name)).GroupBy(t=>t.name).Select(g=>new{name=g.Key,count=g.Count()})});
  rows[rows.Count-1]=Json(rows[rows.Count-1]);
  }}finally{EditorSceneManager.RestoreSceneManagerSetup(setup);}
  string output="outputs/department-store-2026-10-02/trellis-recovery/layout-before-production.json";
  var json="{\"scenes\":["+string.Join(",",rows.Cast<string>())+"]}";
  File.WriteAllText(output,json);return output;
 }
 static string PathOf(Transform t)=>t.parent==null?t.name:PathOf(t.parent)+"/"+t.name;
 static string Json(object v)=>(string)AppDomain.CurrentDomain.GetAssemblies().First(a=>a.GetName().Name=="Newtonsoft.Json").GetType("Newtonsoft.Json.JsonConvert").GetMethod("SerializeObject",new[]{typeof(object)}).Invoke(null,new[]{v});
}
