// Configure only SR18: eight moving pitch spots + authored-road height follower.
if(UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)throw new System.InvalidOperationException("Edit Mode required.");
var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/ShooterSurvival/Scenes/Tools/Noryangjin_MapTool_Mode_SR18.unity");
var map=scene.GetRootGameObjects().Single(g=>g.name=="Noryangjin_MapTool").transform;var roads=map.Find("Roads");var props=map.Find("Props");
if(roads.childCount!=230||map.GetComponentsInChildren<IndianOceanAssets.ShooterSurvival.NoryangjinTurnSpot>(true).Length!=15)throw new System.InvalidOperationException("Expected 230 roads and 15 existing corners; refusing duplicate installation.");
if(!System.IO.File.Exists("tmp/backups/sr18-before-slope-follow-20260905-111656.unity.tmp"))throw new System.InvalidOperationException("Live backup missing.");
var player=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<IndianOceanAssets.ShooterSurvival.PlayerScript>(true)).Single();var capsule=player.GetComponent<CapsuleCollider>();
var prefab=UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ShooterSurvival/Prefabs/Gameplay/Noryangjin_TurnSpot.prefab");
Transform Road(int index)=>roads.Cast<Transform>().Single(t=>t.name.StartsWith("SR18_Road_"+index.ToString("000")+"_"));
float Pitch(Vector3 delta)=>-Mathf.Atan2(delta.y,new Vector2(delta.x,delta.z).magnitude)*Mathf.Rad2Deg;
UnityEditor.Undo.IncrementCurrentGroup();int undo=UnityEditor.Undo.GetCurrentGroup();UnityEditor.Undo.SetCurrentGroupName("Install SR18 continuous slope transitions");
var records=new System.Collections.Generic.List<object>();
try{
 var follower=UnityEditor.Undo.AddComponent<IndianOceanAssets.ShooterSurvival.NoryangjinRoadHeightFollower>(player.gameObject);follower.Configure(roads,.12f);UnityEditor.EditorUtility.SetDirty(follower);
 foreach(int nextIndex in new[]{70,73,87,90,151,154,156,159}){
  var previous=Road(nextIndex-1);var next=Road(nextIndex);Vector3 incoming=previous.position-Road(nextIndex-2).position;Vector3 outgoing=next.position-previous.position;
  float yaw=Mathf.Repeat(Mathf.Round(Mathf.Atan2(outgoing.x,outgoing.z)*Mathf.Rad2Deg/90)*90,360);float incomingPitch=Pitch(incoming),targetPitch=Pitch(outgoing);
  var go=(GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab,props);UnityEditor.Undo.RegisterCreatedObjectUndo(go,"Create slope pitch spot");
  go.name="SR18_Slope_"+(records.Count+1).ToString("00")+"_"+(Mathf.Abs(targetPitch)<1?"Level":targetPitch<0?"Up":"Down");go.transform.SetPositionAndRotation(previous.position,Quaternion.Euler(0,yaw,0));go.transform.localScale=Vector3.one;
  var spot=go.GetComponent<IndianOceanAssets.ShooterSurvival.NoryangjinTurnSpot>();spot.IsSlopeTransition=true;spot.TargetXDegrees=targetPitch;spot.TargetYawDegrees=yaw;spot.TurnDurationSeconds=.25f;
  Vector3 scaledCenter=Vector3.Scale(capsule.center,player.transform.lossyScale);float radius=capsule.radius*player.transform.lossyScale.x;float forwardCenter=(Quaternion.Euler(incomingPitch,0,0)*scaledCenter).z;
  var box=go.GetComponent<BoxCollider>();box.isTrigger=true;box.size=new Vector3(8,2,.8f);box.center=new Vector3(0,1,radius+forwardCenter+.4f-.08f);
  foreach(UnityEngine.Object changed in new UnityEngine.Object[]{go,go.transform,spot,box}){UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(changed);UnityEditor.EditorUtility.SetDirty(changed);}
  records.Add(new{go.name,nextRoad=nextIndex,world=new[]{previous.position.x,previous.position.y,previous.position.z},incomingPitch,targetPitch,yaw,duration=.25f,centerForward=box.center.z});
 }
 UnityEditor.Undo.CollapseUndoOperations(undo);UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);if(!UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene))throw new System.InvalidOperationException("Save failed");
 string folder="map-concepts/sr18-slope-spots-2026-09-05";System.IO.Directory.CreateDirectory(folder);var assembly=System.AppDomain.CurrentDomain.GetAssemblies().Single(a=>a.GetName().Name=="Newtonsoft.Json");var serialize=assembly.GetType("Newtonsoft.Json.JsonConvert").GetMethod("SerializeObject",new[]{typeof(object)});
 System.IO.File.WriteAllText(folder+"/placement.json",(string)serialize.Invoke(null,new object[]{new{scene=scene.path,corners=15,slopeSpots=8,footOffset=.12f,probeReach=.75f,spots=records}}));
 return new{count=records.Count,props=props.childCount,records,scene.isDirty};
}catch{UnityEditor.Undo.RevertAllDownToGroup(undo);throw;}
