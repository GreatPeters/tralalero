// Read-only prefab inspection; does not instantiate or change any scene object.
var registry=UnityEditor.AssetDatabase.LoadAssetAtPath<ObstaclePrefabs>("Assets/ShooterSurvival/Prefabs/Obstacle/ObstaclePrefabs.asset");
var defaults=UnityEditor.AssetDatabase.LoadAssetAtPath<NoryangjinMapToolPaletteDefaults>("Assets/ShooterSurvival/Editor/NoryangjinMapToolPaletteDefaults.asset");
var entries=(System.Collections.Generic.List<NoryangjinMapToolPalettePlacementEntry>)typeof(NoryangjinMapToolPaletteDefaults).GetField("entries",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(defaults);
var output=new System.Collections.Generic.List<object>();
foreach(var item in registry.obstaclePrefabs){
 if(item.prefab==null)continue;
 var root=item.prefab.transform;string path=UnityEditor.AssetDatabase.GetAssetPath(item.prefab);var entry=entries.FirstOrDefault(e=>e.prefabPath==path);
 Vector3 scale=Vector3.Scale(root.localScale,entry==null?Vector3.one:entry.scale);
 var targetRoot=Matrix4x4.TRS(Vector3.zero,Quaternion.Euler(0,entry==null?0:entry.yawOffset,0)*root.localRotation,scale);
 var boxes=new System.Collections.Generic.List<object>();
 foreach(var box in item.prefab.GetComponentsInChildren<BoxCollider>(true)){
  var matrix=targetRoot*root.worldToLocalMatrix*box.transform.localToWorldMatrix;
  var extent=box.size*.5f;var vx=matrix.MultiplyVector(new Vector3(extent.x,0,0));var vy=matrix.MultiplyVector(new Vector3(0,extent.y,0));var vz=matrix.MultiplyVector(new Vector3(0,0,extent.z));
  Vector3 size=new Vector3(Mathf.Abs(vx.x)+Mathf.Abs(vy.x)+Mathf.Abs(vz.x),Mathf.Abs(vx.y)+Mathf.Abs(vy.y)+Mathf.Abs(vz.y),Mathf.Abs(vx.z)+Mathf.Abs(vy.z)+Mathf.Abs(vz.z))*2;
  boxes.Add(new{box.name,width=size.x,length=size.z,height=size.y,box.isTrigger});
 }
 output.Add(new{pattern=item.pattern.ToString(),path,scale=scale.ToString("F3"),gameplayParts=item.prefab.GetComponentsInChildren<ObstacleStats>(true).Length,boxShapes=boxes});
}
return output;
