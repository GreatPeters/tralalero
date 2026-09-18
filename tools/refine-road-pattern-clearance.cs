if(UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)throw new System.InvalidOperationException("Edit Mode required");
var active=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if(active.isDirty){string backup="Assets/ShooterSurvival/Scenes/Tools/RoadPatterns_ClearanceBackup_"+System.DateTime.UtcNow.ToString("HHmmss")+".unity";if(!UnityEditor.SceneManagement.EditorSceneManager.SaveScene(active,backup,true))throw new System.IO.IOException("Backup failed");}
var scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene(RestStopChapterBuilder.ScenePath);
var map=scene.GetRootGameObjects().Single(g=>g.name=="Noryangjin_MapTool").transform;
var hall=map.Find("FoodHall_Holdout");int removed=0;
foreach(UnityEngine.Transform child in hall)if(child.name=="Food counter"&&System.Math.Abs(child.localPosition.z)<1){child.gameObject.SetActive(false);removed++;}
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene(HighwaySceneBuilder.ScenePath);
map=scene.GetRootGameObjects().Single(g=>g.name=="Noryangjin_MapTool").transform;
var route=map.GetComponent<HighwayRoute>();var props=map.Find("Props");int moved=0;
foreach(UnityEngine.Transform child in props)
{
 if(child.name=="Recovery_Stop")foreach(var text in child.GetComponentsInChildren<TMPro.TMP_Text>(true)){text.text="회복 쉼터";text.fontSize=7;text.rectTransform.sizeDelta=new UnityEngine.Vector2(6,2);text.transform.localPosition=new UnityEngine.Vector3(-7.5f,3,-.2f);}
 if(child.name=="Korean_Direction_Sign")foreach(var text in child.GetComponentsInChildren<TMPro.TMP_Text>(true)){text.enableAutoSizing=true;text.fontSizeMin=4;text.fontSizeMax=9;text.rectTransform.sizeDelta=new UnityEngine.Vector2(13,1.1f);}
 if(!child.name.StartsWith("Highway_Tree")&&!child.name.StartsWith("Highway_City"))continue;
 float d=route.NearestDistance(child.position);if(!route.forks.Any(f=>d>=f.start&&d<=f.end))continue;
 route.Sample(d,true,out var p,out var fwd);var right=UnityEngine.Vector3.Cross(UnityEngine.Vector3.up,fwd);float lane=UnityEngine.Vector3.Dot(child.position-p,right);
 var bounds=HighwayAssetImporter.BoundsOf(child.gameObject);float extent=UnityEngine.Mathf.Min(30,UnityEngine.Mathf.Max(bounds.extents.x,bounds.extents.z));float clearance=8+extent;
 if(System.Math.Abs(lane)<clearance){child.position+=right*(UnityEngine.Mathf.Sign(lane==0?-1:lane)*clearance-lane);moved++;}
}
var player=UnityEngine.Object.FindFirstObjectByType<IndianOceanAssets.ShooterSurvival.PlayerScript>();
var occlusion=UnityEngine.Camera.main.GetComponent<IndianOceanAssets.ShooterSurvival.NoryangjinCameraOcclusion>();occlusion.enabled=true;
occlusion.ConfigureAdditionalOccluders(props.Cast<UnityEngine.Transform>().Where(t=>t.name.Contains("Gantry")||t.name=="Highway_Tunnel_Rib"||t.name=="Korean_Direction_Sign"||t.name=="Recovery_Stop").ToArray());
foreach(var root in scene.GetRootGameObjects())foreach(var component in root.GetComponentsInChildren<UnityEngine.Component>(true))if(component!=null&&UnityEditor.PrefabUtility.IsPartOfPrefabInstance(component))UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(component);
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);UnityEditor.AssetDatabase.SaveAssets();
var report=new{sideDoorCountersHidden=removed,bypassSceneryMoved=moved,occlusionEnabled=occlusion.enabled};
var serializer=System.AppDomain.CurrentDomain.GetAssemblies().First(a=>a.GetName().Name=="Newtonsoft.Json").GetType("Newtonsoft.Json.JsonConvert").GetMethod("SerializeObject",new[]{typeof(object)});
System.IO.File.WriteAllText("map-concepts/road-patterns-2026-09-13/clearance-refinement.json",(string)serializer.Invoke(null,new object[]{report}));return report;
