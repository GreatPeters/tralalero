// Align existing SR18 turn-trigger volumes with the player collider and authored route.
// Preserves road/shop transforms and all 15 spot roots; no runtime movement changes.
const string path="Assets/ShooterSurvival/Scenes/Tools/Noryangjin_MapTool_Mode_SR18.unity";
if(UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)throw new System.InvalidOperationException("Use Edit Mode.");
var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath(path);
if(!scene.isLoaded)throw new System.InvalidOperationException("Open SR18 first.");
var map=scene.GetRootGameObjects().Single(g=>g.name=="Noryangjin_MapTool").transform;
var all=map.GetComponentsInChildren<IndianOceanAssets.ShooterSurvival.NoryangjinTurnSpot>(true).Where(s=>!s.IsSlopeTransition).ToArray();
var spots=all.Where(s=>!s.name.StartsWith("SR18_Turn_")).OrderBy(s=>s.transform.GetSiblingIndex()).Concat(all.Where(s=>s.name.StartsWith("SR18_Turn_")).OrderBy(s=>s.name)).ToArray();
if(spots.Length!=15||map.Find("Roads").childCount!=230)throw new System.InvalidOperationException("Unexpected SR18 route.");
var player=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<IndianOceanAssets.ShooterSurvival.PlayerScript>(true)).Single();
var capsule=player.GetComponent<CapsuleCollider>();var scale=player.transform.lossyScale;
if(capsule==null||capsule.direction!=1||capsule.height>2*capsule.radius||Vector3.Distance(scale,Vector3.one*scale.x)>.001f)throw new System.InvalidOperationException("Remeasure changed player collider before authoring turn gates.");
float radius=capsule.radius*scale.x,frontOffset=capsule.center.z*scale.z;
const float width=8f,height=2f,depth=.8f,entryLead=.08f;
float centerForward=radius+frontOffset+depth*.5f-entryLead;
float Yaw(Vector3 direction)=>Mathf.Repeat(Mathf.Round(Mathf.Atan2(direction.x,direction.z)*Mathf.Rad2Deg/90)*90,360);
var finish=map.Find("Roads").Cast<Transform>().Single(t=>t.name.StartsWith("SR18_Road_179_")).position;
var expected=spots.Select((s,i)=>new{spot=s,incoming=Yaw(s.transform.position-(i==0?player.transform.position:spots[i-1].transform.position)),outgoing=Yaw((i==spots.Length-1?finish:spots[i+1].transform.position)-s.transform.position)}).ToArray();
foreach(var row in expected){if(Mathf.Abs(Mathf.DeltaAngle(row.spot.transform.eulerAngles.y,row.incoming))>.1f||Mathf.Abs(Mathf.DeltaAngle(row.spot.TargetYawDegrees,row.outgoing))>.1f||Mathf.Abs(row.spot.transform.position.y)>.01f)throw new System.InvalidOperationException("Route/height changed; inspect "+row.spot.name);}
string stamp=System.DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");string temp="Assets/ShooterSurvival/Scenes/Tools/SR18_Turn_Backup_"+stamp+".unity";string backup="tmp/backups/sr18-before-turn-alignment-"+stamp+".unity.tmp";
System.IO.Directory.CreateDirectory("tmp/backups");
if(!UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene,temp,true))throw new System.InvalidOperationException("Live-state backup failed.");
System.IO.File.Copy(temp,backup,false);UnityEditor.AssetDatabase.DeleteAsset(temp);
UnityEditor.Undo.IncrementCurrentGroup();int undo=UnityEditor.Undo.GetCurrentGroup();UnityEditor.Undo.SetCurrentGroupName("Align all SR18 corner turn spots");
var rows=new System.Collections.Generic.List<object>();
try{
 foreach(var row in expected){var spot=row.spot;var box=spot.GetComponent<BoxCollider>();var s=spot.transform.lossyScale;Vector3 oldSize=Vector3.Scale(box.size,s);Vector3 oldCenter=box.center;
  UnityEditor.Undo.RecordObject(box,"Cover full road and align corner entry");box.enabled=true;box.isTrigger=true;box.size=new Vector3(width/s.x,height/s.y,depth/s.z);box.center=new Vector3(0,1/s.y,centerForward/s.z);
  UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(box);
  rows.Add(new{order=rows.Count+1,spot.name,position=new[]{spot.transform.position.x,spot.transform.position.y,spot.transform.position.z},row.incoming,row.outgoing,duration=spot.TurnDurationSeconds,oldWidth=oldSize.x,oldForwardCenter=oldCenter.z*s.z,width,height,depth,centerForward});
 }
 UnityEditor.Undo.CollapseUndoOperations(undo);UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
 if(!UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene))throw new System.InvalidOperationException("Save failed.");
 string folder="map-concepts/sr18-turn-spots-2026-09-05";System.IO.Directory.CreateDirectory(folder);
 var assembly=System.AppDomain.CurrentDomain.GetAssemblies().Single(a=>a.GetName().Name=="Newtonsoft.Json");var serialize=assembly.GetType("Newtonsoft.Json.JsonConvert").GetMethod("SerializeObject",new[]{typeof(object)});
 var report=new{scene=path,backup,reused=15,created=0,radius,frontOffset,entryLead,centerForward,spots=rows};System.IO.File.WriteAllText(folder+"/placement.json",(string)serialize.Invoke(null,new object[]{report}));
 UnityEditor.SceneView.RepaintAll();return new{reused=15,created=0,width,height,depth,centerForward,entryLead,backup,scene.isDirty};
}catch{UnityEditor.Undo.RevertAllDownToGroup(undo);throw;}
