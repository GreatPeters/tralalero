if(UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Edit Mode required");
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if(scene.isDirty)throw new InvalidOperationException("Preserve pending edits");
const string path="Assets/ShooterSurvival/Scenes/Tools/Noryangjin_MapTool_Mode_SR18.unity";
if(scene.path!=path){if(scene.path!="")throw new InvalidOperationException("Unexpected open scene");scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene(path);}
var map=scene.GetRootGameObjects().Single(g=>g.name=="Noryangjin_MapTool").transform;
var player=scene.GetRootGameObjects().Single(g=>g.name=="Noryangjin_Player").GetComponent<IndianOceanAssets.ShooterSurvival.PlayerScript>();
var camera=Camera.main;
var lamp=map.Find("Props/Prop_Lights_X-49_Z-139");
var parts=lamp.GetComponentsInChildren<ObstacleStats>(true).Where(s=>s.gameObject.activeInHierarchy).ToArray();
if(parts.Length!=4||parts.Any(p=>p.obstaclePattern!=ObstaclePattern.Light)||camera==null||map.Find("SR18_StageExit")!=null)throw new InvalidOperationException("Unexpected baseline");
var roads=map.Find("Roads");var end=new Vector3(247.99992f,.08f,334.5f);
bool supported=false;foreach(var road in roads.GetComponentsInChildren<MeshCollider>(true))
 if(road.Raycast(new Ray(end+Vector3.up*2,Vector3.down),out var hit,4)&&hit.normal.y>.7f){end.y=hit.point.y+.08f;supported=true;break;}
if(!supported)throw new InvalidOperationException("Exit must be on last road");
string folder="tmp/backups/sr18-runtime-fixes-2026-09-10/"+DateTime.Now.ToString("HHmmss");System.IO.Directory.CreateDirectory(folder);
if(!UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene,folder+"/before.unity",true))throw new Exception("Backup failed");
UnityEditor.Undo.IncrementCurrentGroup();int undo=UnityEditor.Undo.GetCurrentGroup();UnityEditor.Undo.SetCurrentGroupName("SR18 verified runtime fixes");
try{
 foreach(var part in parts){
  UnityEditor.Undo.RecordObject(part,"Legacy lamp becomes scenery");part.enabled=false;UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(part);
  foreach(var collider in part.GetComponents<Collider>()){UnityEditor.Undo.RecordObject(collider,"Disable legacy damage trigger");collider.enabled=false;UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(collider);}
 }
 var occlusion=UnityEditor.Undo.AddComponent<IndianOceanAssets.ShooterSurvival.NoryangjinCameraOcclusion>(camera.gameObject);occlusion.Configure(player.transform,roads);
 var exit=new GameObject("SR18_StageExit");UnityEditor.Undo.RegisterCreatedObjectUndo(exit,"Stage completion trigger");exit.transform.SetParent(map,false);exit.transform.position=end;exit.tag="GameEndTriggerTag";
 var trigger=UnityEditor.Undo.AddComponent<BoxCollider>(exit);trigger.isTrigger=true;trigger.center=new Vector3(0,1,0);trigger.size=new Vector3(8.8f,4,1.2f);
 UnityEditor.EditorUtility.SetDirty(occlusion);UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(occlusion);
 UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
 if(!UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene))throw new Exception("Scene save failed");
 UnityEditor.Undo.CollapseUndoOperations(undo);
 return new{applied=true,backup=folder+"/before.unity",legacyLampsDecorative=parts.Length,exit=end.ToString(),camera=camera.name,roads=roads.childCount,props=map.Find("Props").childCount};
}catch{UnityEditor.Undo.RevertAllDownToGroup(undo);throw;}
