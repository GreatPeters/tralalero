var paths=new[]{"Boat","Boats","Dolphins"};var output=new System.Collections.Generic.Dictionary<string,object>();
foreach(string name in paths)
{
 var prefab=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/ShooterSurvival/Prefabs/Obstacle_Real/"+name+".prefab");
 output[name]=new{stats=prefab.GetComponentsInChildren<ObstacleStats>(true).Select(s=>new{name=s.name,pattern=s.obstaclePattern.ToString(),s.value,position=s.transform.localPosition.ToString(),scale=s.transform.lossyScale.ToString(),A=s.pointA!=null?s.pointA.name:null,B=s.pointB!=null?s.pointB.name:null,components=s.GetComponents<UnityEngine.Component>().Select(c=>c.GetType().Name).ToArray(),projectiles=s.GetComponentsInChildren<IndianOceanAssets.ShooterSurvival.SimpleProjectile>(true).Select(p=>new{name=p.name,json=UnityEditor.EditorJsonUtility.ToJson(p)}).ToArray()}).ToArray(),children=prefab.GetComponentsInChildren<UnityEngine.Transform>(true).Select(t=>new{name=t.name,parent=t.parent!=null?t.parent.name:null,position=t.localPosition.ToString(),components=t.GetComponents<UnityEngine.Component>().Select(c=>c.GetType().Name).ToArray()}).ToArray()};
}
var serialize=System.AppDomain.CurrentDomain.GetAssemblies().First(a=>a.GetName().Name=="Newtonsoft.Json").GetType("Newtonsoft.Json.JsonConvert").GetMethod("SerializeObject",new[]{typeof(object)});
System.IO.File.WriteAllText("map-concepts/skins-reststop-2026-09-12/unused-gimmicks-audit.json",(string)serialize.Invoke(null,new object[]{output}));return "Audit saved";
