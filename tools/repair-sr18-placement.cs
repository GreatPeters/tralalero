if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Edit Mode required");
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if(scene.name!="Noryangjin_MapTool_Mode_SR18")throw new Exception("SR18 required");
var map=scene.GetRootGameObjects().Single(g=>g.name=="Noryangjin_MapTool").transform;var props=map.Find("Props");
foreach(var suffix in new[]{"","_Right"})
{
    var enemy=map.Find("Enemies/SR18_L_E01_T005_Enemy_OldMan"+suffix).GetComponent<IndianOceanAssets.ShooterSurvival.EnemyEventController>();
    var spot=props.Find("SR18_L_E01_Activation"+suffix).GetComponent<IndianOceanAssets.ShooterSurvival.EnemyEventActivationSpot>();
    spot.Targets=new[]{enemy};spot.transform.position=enemy.transform.position;
    EditorUtility.SetDirty(spot);EditorUtility.SetDirty(spot.transform);
    PrefabUtility.RecordPrefabInstancePropertyModifications(spot);PrefabUtility.RecordPrefabInstancePropertyModifications(spot.transform);
}
var enemies=map.Find("Enemies").GetComponentsInChildren<IndianOceanAssets.ShooterSurvival.EnemyEventController>(true);
var spots=props.GetComponentsInChildren<IndianOceanAssets.ShooterSurvival.EnemyEventActivationSpot>(true);
foreach(var enemy in enemies)
{
    var matching=spots.Where(spot=>spot.Targets.Contains(enemy)).ToArray();
    if(matching.Length!=1||matching[0].Targets.Length!=1)throw new Exception("Invalid remaining binding: "+enemy.name);
}
var gantry=props.Cast<Transform>().Single(t=>t.name.Contains("Harbor_lane_signal_gantry"));
gantry.localScale=Vector3.one*240;var position=gantry.position;position.y=.02f;gantry.position=position;
foreach(var collider in gantry.GetComponentsInChildren<Collider>(true)){collider.enabled=false;EditorUtility.SetDirty(collider);PrefabUtility.RecordPrefabInstancePropertyModifications(collider);}
EditorUtility.SetDirty(gantry);PrefabUtility.RecordPrefabInstancePropertyModifications(gantry);
var player=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<IndianOceanAssets.ShooterSurvival.PlayerScript>(true)).Single();
var camera=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Camera>(true)).Single();
var occlusion=camera.GetComponent<IndianOceanAssets.ShooterSurvival.NoryangjinCameraOcclusion>();
var serialized=new SerializedObject(occlusion);var property=serialized.FindProperty("additionalOccluderGroups");
var groups=Enumerable.Range(0,property.arraySize).Select(i=>property.GetArrayElementAtIndex(i).objectReferenceValue as Transform).Where(t=>t!=null).Append(gantry).Distinct().ToArray();
occlusion.Configure(player.transform,map.Find("Roads"));occlusion.ConfigureAdditionalOccluders(groups);EditorUtility.SetDirty(occlusion);
var originalPost=props.Find("Prop_008_STAGE01_NRY_PROPS_008_Seagull_perch_post_X-64_Z-451");
var duplicatePost=props.Find("Prop_008_STAGE01_NRY_PROPS_008_Seagull_perch_post_X-64_Z-451 (1)");
if(originalPost!=null&&duplicatePost!=null&&Vector3.Distance(originalPost.position,duplicatePost.position)<.001f&&Quaternion.Angle(originalPost.rotation,duplicatePost.rotation)<.01f&&originalPost.localScale==duplicatePost.localScale&&originalPost.GetComponent<MeshFilter>().sharedMesh==duplicatePost.GetComponent<MeshFilter>().sharedMesh&&originalPost.GetComponent<Renderer>().sharedMaterials.SequenceEqual(duplicatePost.GetComponent<Renderer>().sharedMaterials))
{duplicatePost.gameObject.SetActive(false);PrefabUtility.RecordPrefabInstancePropertyModifications(duplicatePost.gameObject);}
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);if(!UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene))throw new Exception("Save failed");
return new{enemies=enemies.Length,spots=spots.Length,gantry=gantry.position.ToString(),bounds=gantry.GetComponent<Renderer>().bounds.ToString()};
