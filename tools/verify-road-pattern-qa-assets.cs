var paths=new[]{HighwayEnemyBuilder.Folder+"/TrafficPatrol.prefab",ChapterMascotImporter.PrefabPath("ParkingMarshal"),HighwayAssetImporter.Prefabs+"/HWY_067.prefab",HighwayAssetImporter.Prefabs+"/HWY_069.prefab"};
var rows=new System.Collections.Generic.List<object>();
foreach(var path in paths){var prefab=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(path);int renderers=prefab.GetComponentsInChildren<UnityEngine.Renderer>(true).Length;if(renderers==0)throw new System.InvalidOperationException("Missing visual "+path);rows.Add(new{path,renderers});}
return rows;
