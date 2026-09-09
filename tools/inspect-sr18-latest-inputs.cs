var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
var map=scene.GetRootGameObjects().Single(g=>g.name=="Noryangjin_MapTool").transform;
var output=new System.Collections.Generic.List<object>();
foreach(var file in new[]{"Hole","Lights","Buckets"}){
 var asset=UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ShooterSurvival/Prefabs/Obstacle_Real/"+file+".prefab");
 output.Add(new{file,parts=asset.GetComponentsInChildren<ObstacleStats>(true).Select(s=>new{name=s.name,parent=s.transform.parent==null?null:s.transform.parent.name,position=s.transform.position.ToString("F4"),scale=s.transform.lossyScale.ToString("F4"),s.value,bucket=s.bucket==null?null:s.bucket.name,source=UnityEditor.PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(s.gameObject),components=s.GetComponents<Component>().Select(c=>c.GetType().Name).ToArray()}).ToArray()});
}
return new{scene=scene.path,dirty=scene.isDirty,roots=map.Cast<Transform>().Select(t=>new{t.name,count=t.childCount}).ToArray(),assets=output};
